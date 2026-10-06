using System;

namespace FutureDigitalPlus.UIKit.Hud
{
    /// <summary>
    /// The logic of an ability or item cooldown: a duration that counts down to zero and a one-shot "became ready"
    /// edge for the flash. The caller feeds <see cref="Tick"/> with elapsed seconds (scaled or unscaled, its choice).
    /// </summary>
    public sealed class CooldownModel
    {
        /// <summary>Length of the running cooldown in seconds (0 when none ever started).</summary>
        public float Duration { get; private set; }

        /// <summary>Seconds left; 0 when ready.</summary>
        public float Remaining { get; private set; }

        /// <summary>True when nothing is cooling down.</summary>
        public bool IsReady => Remaining <= 0f;

        /// <summary>Remaining divided by duration, 1 right after <see cref="Start"/> down to 0 when ready.</summary>
        public float Fraction => Duration <= 0f || Remaining <= 0f ? 0f : Math.Min(1f, Remaining / Duration);

        /// <summary>Starts (or restarts) a cooldown of <paramref name="durationSeconds"/>. A zero or negative duration makes it ready.</summary>
        public void Start(float durationSeconds)
        {
            Duration = durationSeconds > 0f ? durationSeconds : 0f;   // NaN and negative mean ready at once
            Remaining = Duration;
        }

        /// <summary>Makes the cooldown ready now. Returns true when it was running (the ready edge).</summary>
        public bool Reset()
        {
            var wasRunning = Remaining > 0f;
            Remaining = 0f;
            return wasRunning;
        }

        /// <summary>Takes <paramref name="seconds"/> off the remaining time. Returns true when that made it ready.</summary>
        public bool Reduce(float seconds)
        {
            if (Remaining <= 0f || !(seconds > 0f)) return false;
            Remaining = Math.Max(0f, Remaining - seconds);
            return Remaining <= 0f;
        }

        /// <summary>Advances time. Returns true on exactly the call during which the cooldown reaches zero.</summary>
        public bool Tick(float deltaSeconds)
        {
            return Reduce(deltaSeconds);
        }
    }
}
