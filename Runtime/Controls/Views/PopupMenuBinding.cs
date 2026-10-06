using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace FutureDigitalPlus.UIKit.Controls
{
    /// <summary>Ways a <see cref="PopupMenuBinding"/> opens its menu.</summary>
    [Flags]
    public enum PopupMenuTriggers
    {
        /// <summary>The menu is never opened by input (open it from code).</summary>
        None = 0,

        /// <summary>A click with the right mouse button.</summary>
        RightClick = 1,

        /// <summary>Holding a touch or pen press without moving.</summary>
        LongPress = 2,

        /// <summary>The keyboard menu key, or Shift+F10, while the element has focus.</summary>
        ContextKey = 4,

        /// <summary>The gamepad button <see cref="PopupMenuBinding.GamepadKey"/> while the element has focus.</summary>
        GamepadButton = 8,

        /// <summary>Every trigger.</summary>
        Default = RightClick | LongPress | ContextKey | GamepadButton,
    }

    /// <summary>
    /// Opens a <see cref="PopupMenu"/> for an element from pointer, touch, keyboard and gamepad input. Created by
    /// <see cref="PopupMenu.Attach"/>; dispose it to stop listening.
    /// </summary>
    public sealed class PopupMenuBinding : IDisposable
    {
        private const float LongPressMoveTolerance = 10f;

        private readonly VisualElement _target;
        private readonly Func<IReadOnlyList<MenuEntry>> _items;
        private readonly Action<MenuEntry> _onInvoke;
        private IVisualElementScheduledItem _longPress;
        private Vector2 _pressPosition;
        private bool _disposed;

        internal PopupMenuBinding(VisualElement target, Func<IReadOnlyList<MenuEntry>> items, Action<MenuEntry> onInvoke,
            PopupMenuTriggers triggers)
        {
            _target = target ?? throw new ArgumentNullException(nameof(target));
            _items = items ?? throw new ArgumentNullException(nameof(items));
            _onInvoke = onInvoke;
            Triggers = triggers;
            // Trickle-down: a Button's own Clickable stops the propagation of a PointerDown at the target, so bubble-phase callbacks never see a press on a button.
            // Trickle-down: a Button's own Clickable stops the propagation of a PointerDown at the target, so bubble-phase callbacks never see a press on a button.
            _target.RegisterCallback<PointerDownEvent>(OnPointerDown, TrickleDown.TrickleDown);
            _target.RegisterCallback<PointerMoveEvent>(OnPointerMove, TrickleDown.TrickleDown);
            _target.RegisterCallback<PointerUpEvent>(OnPointerEnd, TrickleDown.TrickleDown);
            _target.RegisterCallback<PointerCancelEvent>(OnPointerCancel, TrickleDown.TrickleDown);
            _target.RegisterCallback<KeyDownEvent>(OnKeyDown);
        }

        /// <summary>The ways the menu opens; may be changed at any time.</summary>
        public PopupMenuTriggers Triggers { get; set; }

        /// <summary>How long a touch must be held for <see cref="PopupMenuTriggers.LongPress"/> (default 0.5 seconds).</summary>
        public float LongPressSeconds { get; set; } = 0.5f;

        /// <summary>The gamepad button for <see cref="PopupMenuTriggers.GamepadButton"/> (default joystick button 3, Y or Triangle).</summary>
        public KeyCode GamepadKey { get; set; } = KeyCode.JoystickButton3;

        /// <summary>The menu that was opened last and is still open, or null.</summary>
        public PopupMenu Current { get; private set; }

        /// <summary>Opens the menu below the target now.</summary>
        public PopupMenu OpenBelow()
        {
            Current?.Close();
            Current = PopupMenu.ShowBelow(_target, _items(), _onInvoke);
            return Current;
        }

        /// <summary>Opens the menu at a panel position now.</summary>
        public PopupMenu OpenAt(Vector2 panelPosition)
        {
            Current?.Close();
            Current = PopupMenu.Show(_target, _items(), panelPosition, _onInvoke);
            return Current;
        }

        /// <summary>Stops listening and closes the menu if it is open.</summary>
        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;
            CancelLongPress();
            _target.UnregisterCallback<PointerDownEvent>(OnPointerDown, TrickleDown.TrickleDown);
            _target.UnregisterCallback<PointerMoveEvent>(OnPointerMove, TrickleDown.TrickleDown);
            _target.UnregisterCallback<PointerUpEvent>(OnPointerEnd, TrickleDown.TrickleDown);
            _target.UnregisterCallback<PointerCancelEvent>(OnPointerCancel, TrickleDown.TrickleDown);
            _target.UnregisterCallback<KeyDownEvent>(OnKeyDown);
            Current?.Close();
            Current = null;
        }

        private void OnPointerDown(PointerDownEvent evt)
        {
            if (evt.button == 1 && (Triggers & PopupMenuTriggers.RightClick) != 0)
            {
                OpenAt(evt.position);
                evt.StopPropagation();
                return;
            }

            if (evt.button != 0 || (Triggers & PopupMenuTriggers.LongPress) == 0) return;
            if (evt.pointerType != UnityEngine.UIElements.PointerType.touch && evt.pointerType != UnityEngine.UIElements.PointerType.pen) return;
            _pressPosition = evt.position;
            CancelLongPress();
            var at = _pressPosition;
            _longPress = _target.schedule.Execute(() => OpenAt(at)).StartingIn((long)(LongPressSeconds * 1000f));
        }

        private void OnPointerMove(PointerMoveEvent evt)
        {
            if (_longPress != null && Vector2.Distance(evt.position, _pressPosition) > LongPressMoveTolerance) CancelLongPress();
        }

        private void OnPointerEnd(PointerUpEvent evt) => CancelLongPress();

        private void OnPointerCancel(PointerCancelEvent evt) => CancelLongPress();

        private void OnKeyDown(KeyDownEvent evt)
        {
            var contextKey = (Triggers & PopupMenuTriggers.ContextKey) != 0
                && (evt.keyCode == KeyCode.Menu || (evt.keyCode == KeyCode.F10 && evt.shiftKey));
            var gamepad = (Triggers & PopupMenuTriggers.GamepadButton) != 0 && evt.keyCode == GamepadKey;
            if (!contextKey && !gamepad) return;
            OpenBelow();
            evt.StopPropagation();
        }

        private void CancelLongPress()
        {
            _longPress?.Pause();
            _longPress = null;
        }
    }
}
