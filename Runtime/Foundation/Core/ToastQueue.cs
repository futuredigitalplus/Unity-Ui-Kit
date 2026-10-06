using System;
using System.Collections.Generic;

namespace FutureDigitalPlus.UIKit.Foundation
{
    /// <summary>
    /// One toast visible at a time, up to <see cref="MaxWaiting"/> waiting behind it. A toast equal to the visible one restarts the visible
    /// toast's timer (<see cref="Refreshed"/>) instead of stacking; one equal to the last waiting toast is dropped. When the waiting list is
    /// full the oldest waiting toast is dropped, so the player always sees the newest information. Pure; the view owns the timers and calls
    /// <see cref="Hidden"/> when the visible toast finished fading out.
    /// </summary>
    public sealed class ToastQueue
    {
        /// <summary>Most toasts that wait behind the visible one.</summary>
        public const int MaxWaiting = 3;

        readonly List<ToastItem> _waiting = new List<ToastItem>();

        /// <summary>The visible toast, or null.</summary>
        public ToastItem Current { get; private set; }

        /// <summary>Number of toasts waiting.</summary>
        public int WaitingCount
        {
            get { return _waiting.Count; }
        }

        /// <summary>This toast became current; the view shows it and starts its timer.</summary>
        public event Action<ToastItem> Showing;

        /// <summary>The current toast was shown again; the view restarts its timer.</summary>
        public event Action<ToastItem> Refreshed;

        /// <summary>Returns true when the toast was shown or queued, false when it was coalesced into an equal one.</summary>
        public bool Show(ToastItem item)
        {
            if (item == null || item.Text.IsEmpty)
                return false;

            if (Current == null)
            {
                Present(item);
                return true;
            }

            if (Current.Equals(item))
            {
                Current = item;
                Action<ToastItem> refreshed = Refreshed;
                if (refreshed != null)
                    refreshed(item);
                return false;
            }

            if (_waiting.Count > 0 && _waiting[_waiting.Count - 1].Equals(item))
                return false;

            if (_waiting.Count >= MaxWaiting)
                _waiting.RemoveAt(0);
            _waiting.Add(item);
            return true;
        }

        /// <summary>The visible toast is gone. Shows the next waiting one, if any.</summary>
        public void Hidden()
        {
            Current = null;
            if (_waiting.Count == 0)
                return;
            ToastItem next = _waiting[0];
            _waiting.RemoveAt(0);
            Present(next);
        }

        /// <summary>Drops everything (screen change, shutdown). Does not raise events.</summary>
        public void Clear()
        {
            Current = null;
            _waiting.Clear();
        }

        void Present(ToastItem item)
        {
            Current = item;
            Action<ToastItem> showing = Showing;
            if (showing != null)
                showing(item);
        }
    }
}
