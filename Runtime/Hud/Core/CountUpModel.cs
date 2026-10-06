using System;

namespace FutureDigitalPlus.UIKit.Hud
{
    /// <summary>Easing curve of a <see cref="CountUpModel"/>.</summary>
    public enum CountEasing
    {
        /// <summary>Constant speed.</summary>
        Linear,

        /// <summary>Fast start, slow end (quadratic).</summary>
        EaseOutQuad,

        /// <summary>Faster start, slower end (cubic). The default.</summary>
        EaseOutCubic,

        /// <summary>Slow start and end (quadratic).</summary>
        EaseInOutQuad,
    }

    /// <summary>
    /// The logic of a counter that rolls from its current number to a new one over a duration (score, currency).
    /// The caller feeds <see cref="Tick"/> with elapsed seconds; use unscaled time so a paused game still rolls.
    /// </summary>
    public sealed class CountUpModel
    {
        private double _from;
        private float _elapsed;
        private float _duration;
        private CountEasing _easing;

        /// <summary>The number to show right now.</summary>
        public double Current { get; private set; }

        /// <summary>The number the counter is heading for.</summary>
        public double Target { get; private set; }

        /// <summary>True while <see cref="Current"/> has not reached <see cref="Target"/>.</summary>
        public bool IsRunning { get; private set; }

        /// <summary>Jumps to <paramref name="value"/> without animation.</summary>
        public void Snap(double value)
        {
            if (double.IsNaN(value)) return;
            Current = value;
            Target = value;
            IsRunning = false;
        }

        /// <summary>
        /// Rolls from the current number to <paramref name="target"/>. Called again while running, it continues from
        /// where it is. A zero or negative duration snaps.
        /// </summary>
        public void AnimateTo(double target, float durationSeconds, CountEasing easing = CountEasing.EaseOutCubic)
        {
            if (double.IsNaN(target)) return;
            if (!(durationSeconds > 0f) || target.Equals(Current))
            {
                Snap(target);
                return;
            }

            _from = Current;
            Target = target;
            _duration = durationSeconds;
            _elapsed = 0f;
            _easing = easing;
            IsRunning = true;
        }

        /// <summary>Advances time. Returns true when <see cref="Current"/> changed.</summary>
        public bool Tick(float deltaSeconds)
        {
            if (!IsRunning) return false;
            if (deltaSeconds > 0f) _elapsed += deltaSeconds;   // negative and NaN steps change nothing
            var before = Current;
            if (_elapsed >= _duration)
            {
                Current = Target;
                IsRunning = false;
            }
            else
            {
                Current = _from + (Target - _from) * Ease(_elapsed / _duration, _easing);
            }

            return !Current.Equals(before);
        }

        /// <summary>Evaluates an easing curve at <paramref name="t"/> in 0 .. 1.</summary>
        public static float Ease(float t, CountEasing easing)
        {
            if (t <= 0f) return 0f;
            if (t >= 1f) return 1f;
            switch (easing)
            {
                case CountEasing.Linear: return t;
                case CountEasing.EaseOutQuad: return 1f - (1f - t) * (1f - t);
                case CountEasing.EaseInOutQuad: return t < 0.5f ? 2f * t * t : 1f - 2f * (1f - t) * (1f - t);
                default:
                    var u = 1f - t;
                    return 1f - u * u * u;
            }
        }
    }
}
