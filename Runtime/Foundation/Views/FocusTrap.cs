using System;
using System.Collections.Generic;
using UnityEngine.UIElements;

namespace FutureDigitalPlus.UIKit.Foundation
{
    /// <summary>
    /// Holds keyboard and gamepad focus inside a container while it exists, and gives focus back to the element that had it before when disposed.
    /// Create it right after the container was added to the panel (a dialog, a sheet, a menu). Focus that leaves the container (a click on the page behind,
    /// the focused control being removed) goes back to the first focusable element inside. Traps nest: only the newest trap of a panel is active, so a
    /// dialog over a sheet over a screen each hold focus in turn, whatever layer they live in.
    /// </summary>
    public sealed class FocusTrap : IDisposable
    {
        const long RecoverIntervalMilliseconds = 200;

        static readonly List<FocusTrap> Open = new List<FocusTrap>();

        readonly VisualElement _container;
        readonly VisualElement _previous;
        readonly IVisualElementScheduledItem _watch;
        bool _disposed;

        /// <summary>Creates the trap and moves focus to the first focusable element inside <paramref name="container"/>.</summary>
        public FocusTrap(VisualElement container)
        {
            if (container == null)
                throw new ArgumentNullException(nameof(container));
            _container = container;
            _previous = container.panel == null ? null : container.panel.focusController.focusedElement as VisualElement;
            Open.Add(this);
            _container.RegisterCallback<FocusOutEvent>(OnFocusOut);
            UiFocus.FocusFirst(_container);
            // Removing the focused control raises no FocusOut on the container, so a slow check catches focus that was lost without an event.
            _watch = _container.schedule.Execute(Recover).Every(RecoverIntervalMilliseconds);
        }

        /// <summary>The container the trap holds focus in.</summary>
        public VisualElement Container
        {
            get { return _container; }
        }

        /// <summary>True when this is the newest trap of its panel, the one that holds focus right now.</summary>
        public bool IsActive
        {
            get { return !_disposed && ReferenceEquals(Newest(_container.panel), this); }
        }

        /// <summary>Releases the trap and returns focus to the element that had it before (or to the next trap below when that element is gone).</summary>
        public void Dispose()
        {
            if (_disposed)
                return;
            _disposed = true;
            Open.Remove(this);
            _watch.Pause();
            _container.UnregisterCallback<FocusOutEvent>(OnFocusOut);
            IPanel panel = _container.panel;
            if (_previous != null && _previous.panel != null)
            {
                _previous.Focus();
                return;
            }
            FocusTrap below = Newest(panel);
            if (below != null && below._container.panel != null)
                UiFocus.FocusFirst(below._container);
        }

        /// <summary>Forgets every trap. Called when the domain reloads and by tests.</summary>
        internal static void ResetStatics()
        {
            Open.Clear();
        }

        static FocusTrap Newest(IPanel panel)
        {
            if (panel == null)
                return null;
            for (int i = Open.Count - 1; i >= 0; i--)
            {
                if (ReferenceEquals(Open[i]._container.panel, panel))
                    return Open[i];
            }
            return null;
        }

        void OnFocusOut(FocusOutEvent evt)
        {
            if (_disposed || !IsActive)
                return;
            VisualElement next = evt.relatedTarget as VisualElement;
            if (next != null && _container.Contains(next))
                return;
            // Refocus after the event that moved focus has finished: setting focus inside a FocusOut handler is ignored by the focus controller.
            _container.schedule.Execute(Recover);
        }

        void Recover()
        {
            if (_disposed || !IsActive || _container.panel == null)
                return;
            VisualElement focused = _container.panel.focusController.focusedElement as VisualElement;
            if (focused != null && _container.Contains(focused))
                return;
            UiFocus.FocusFirst(_container);
        }
    }
}
