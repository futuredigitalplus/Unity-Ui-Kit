using System;
using UnityEngine.UIElements;

namespace FutureDigitalPlus.UIKit.Hud
{
    /// <summary>
    /// Runs a step function from the element scheduler only while it has work to do, and sleeps (the scheduled item is
    /// paused) the rest of the time, so an idle HUD element costs no per-frame work. The step receives the elapsed
    /// seconds and returns true while it needs another call.
    /// </summary>
    internal sealed class TickDriver
    {
        private const float MaxStepSeconds = 0.1f;

        private readonly VisualElement _owner;
        private readonly Func<float, bool> _step;
        private readonly int _intervalMs;
        private readonly Action<TimerState> _onTimer;
        private IVisualElementScheduledItem _item;
        private bool _running;

        public TickDriver(VisualElement owner, Func<float, bool> step, int intervalMs = 16)
        {
            _owner = owner;
            _step = step;
            _intervalMs = intervalMs;
            _onTimer = OnTimer;
            owner.RegisterCallback<AttachToPanelEvent>(OnAttach);
        }

        public bool IsRunning => _running;

        /// <summary>Wakes the driver (no effect when it is already running).</summary>
        public void Request()
        {
            if (_running) return;
            _running = true;
            if (_item == null) _item = _owner.schedule.Execute(_onTimer).Every(_intervalMs);
            else _item.Resume();
        }

        /// <summary>Puts the driver to sleep.</summary>
        public void Stop()
        {
            if (!_running) return;
            _running = false;
            _item?.Pause();
        }

        private void OnAttach(AttachToPanelEvent evt)
        {
            if (!_running || _item == null) return;
            _item.Pause();
            _item.Resume();
        }

        private void OnTimer(TimerState state)
        {
            if (!_running) return;
            var dt = Math.Min(state.deltaTime * 0.001f, MaxStepSeconds);
            if (!_step(dt)) Stop();
        }
    }
}
