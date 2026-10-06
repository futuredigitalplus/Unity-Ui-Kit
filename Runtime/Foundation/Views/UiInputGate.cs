using System;
using UnityEngine;
using UnityEngine.UIElements;

namespace FutureDigitalPlus.UIKit.Foundation
{
    /// <summary>
    /// Implementation of <see cref="IUiInputGate"/>: a counter of blocking reasons (<see cref="Blocks"/>) plus a pick test that only considers elements of
    /// this shell's own root, so two shells in one panel (or a shell next to other UI documents) do not see each other.
    /// Elements with <c>pickingMode = Ignore</c> are skipped by <c>Panel.Pick</c>, so empty areas of the HUD layer let game input through.
    /// </summary>
    public sealed class UiInputGate : IUiInputGate
    {
        readonly VisualElement _root;

        /// <summary>The reasons input is blocked right now.</summary>
        public InputBlockCounter Blocks { get; } = new InputBlockCounter();

        /// <summary>Creates a gate for the shell root <paramref name="root"/>.</summary>
        public UiInputGate(VisualElement root)
        {
            _root = root;
            Blocks.Changed += OnBlocksChanged;
        }

        /// <inheritdoc />
        public bool IsBlocked
        {
            get { return Blocks.IsBlocked; }
        }

        /// <inheritdoc />
        public event Action<bool> BlockedChanged;

        /// <inheritdoc />
        public bool IsPointerOverUi(Vector2 screenPosition)
        {
            if (_root == null || _root.panel == null)
                return false;
            // Screen pixels have their origin at the bottom left; the panel's origin is top left.
            Vector2 flipped = new Vector2(screenPosition.x, UnityEngine.Device.Screen.height - screenPosition.y);
            return IsOverUiAtPanelPosition(RuntimePanelUtils.ScreenToPanel(_root.panel, flipped));
        }

        /// <summary>Same test for a position that is already in panel units (top left origin). Use it for render-texture or world-space panels, where the screen position means nothing.</summary>
        public bool IsOverUiAtPanelPosition(Vector2 panelPosition)
        {
            if (_root == null || _root.panel == null)
                return false;
            VisualElement picked = _root.panel.Pick(panelPosition);
            return picked != null && picked != _root && picked.pickingMode == PickingMode.Position && _root.Contains(picked);
        }

        void OnBlocksChanged(bool blocked)
        {
            Action<bool> handler = BlockedChanged;
            if (handler != null)
                handler(blocked);
        }
    }
}
