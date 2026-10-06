using System;

namespace FutureDigitalPlus.UIKit.Hud
{
    /// <summary>Warning level of a <see cref="MeterModel"/>.</summary>
    public enum MeterLevel
    {
        /// <summary>No warning.</summary>
        Normal,

        /// <summary>The value crossed the warning threshold.</summary>
        Warning,

        /// <summary>The value crossed the danger threshold.</summary>
        Danger,
    }

    /// <summary>Which end of a meter is the bad end.</summary>
    public enum MeterThresholdMode
    {
        /// <summary>A low value is bad (health, ammo, stamina). The default.</summary>
        LowIsBad,

        /// <summary>A high value is bad (heat, stress, overload).</summary>
        HighIsBad,
    }

    /// <summary>
    /// The logic of a value bar: a value between 0 and a maximum, a "damage trail" that follows a decrease
    /// after a short delay, and warning / danger levels with hysteresis so the level does not flicker when the value
    /// hovers around a threshold. No time source: the caller feeds <see cref="Tick"/> with elapsed seconds.
    /// </summary>
    public sealed class MeterModel
    {
        private float _trailHoldRemaining;

        /// <summary>Creates a model. A negative <paramref name="value"/> means "full".</summary>
        public MeterModel(float max = 100f, float value = -1f)
        {
            Max = SanitizeMax(max);
            Value = value < 0f ? Max : Clamp(value, Max);
            TrailFraction = Fraction;
            UpdateLevel();
        }

        /// <summary>Maximum value, 0 or more.</summary>
        public float Max { get; private set; }

        /// <summary>Current value, between 0 and <see cref="Max"/>.</summary>
        public float Value { get; private set; }

        /// <summary>Value divided by maximum, 0 to 1 (0 when the maximum is 0).</summary>
        public float Fraction => Max <= 0f ? 0f : Value / Max;

        /// <summary>Where the damage trail is, as a fraction. Never below <see cref="Fraction"/>.</summary>
        public float TrailFraction { get; private set; }

        /// <summary>True while the trail is still above the value and needs more <see cref="Tick"/> calls.</summary>
        public bool IsTrailMoving => TrailFraction > Fraction;

        /// <summary>Seconds the trail waits after a decrease before it starts to move (default 0.4).</summary>
        public float TrailDelaySeconds { get; set; } = 0.4f;

        /// <summary>How fast the trail closes the gap, in fractions of the whole bar per second (default 0.6).</summary>
        public float TrailSpeed { get; set; } = 0.6f;

        /// <summary>Which end of the bar is bad.</summary>
        public MeterThresholdMode Mode { get; set; } = MeterThresholdMode.LowIsBad;

        /// <summary>
        /// Fraction (of the good side) below which the level becomes <see cref="MeterLevel.Warning"/>. In
        /// <see cref="MeterThresholdMode.LowIsBad"/> mode that is <c>Fraction &lt; WarningThreshold</c>; in
        /// <see cref="MeterThresholdMode.HighIsBad"/> mode it is <c>1 - Fraction &lt; WarningThreshold</c>. Default 0.5.
        /// </summary>
        public float WarningThreshold { get; set; } = 0.5f;

        /// <summary>Same as <see cref="WarningThreshold"/> for <see cref="MeterLevel.Danger"/>. Default 0.25.</summary>
        public float DangerThreshold { get; set; } = 0.25f;

        /// <summary>How far past a threshold the value must come back before the level relaxes. Default 0.03.</summary>
        public float Hysteresis { get; set; } = 0.03f;

        /// <summary>Current warning level.</summary>
        public MeterLevel Level { get; private set; }

        /// <summary>Raised when <see cref="Level"/> changes.</summary>
        public event Action<MeterLevel> LevelChanged;

        /// <summary>Sets value and maximum together. The trail snaps to the value (a changed maximum is not damage).</summary>
        public void Set(float value, float max)
        {
            Max = SanitizeMax(max);
            Value = Clamp(value, Max);
            TrailFraction = Fraction;
            _trailHoldRemaining = 0f;
            UpdateLevel();
        }

        /// <summary>Sets the value, clamped to 0 .. <see cref="Max"/>. A decrease starts the damage trail; an increase moves it along.</summary>
        public void SetValue(float value)
        {
            var before = Fraction;
            Value = Clamp(value, Max);
            var after = Fraction;
            if (after < before) _trailHoldRemaining = TrailDelaySeconds;
            if (TrailFraction < after) TrailFraction = after;
            UpdateLevel();
        }

        /// <summary>Advances the trail. Returns true while it still has to move after this call.</summary>
        public bool Tick(float deltaSeconds)
        {
            var target = Fraction;
            if (TrailFraction <= target)
            {
                TrailFraction = target;
                return false;
            }

            if (!(deltaSeconds > 0f)) return true;   // also NaN: it would poison the trail
            if (_trailHoldRemaining > 0f)
            {
                if (deltaSeconds <= _trailHoldRemaining)
                {
                    _trailHoldRemaining -= deltaSeconds;
                    return true;
                }

                deltaSeconds -= _trailHoldRemaining;
                _trailHoldRemaining = 0f;
            }

            TrailFraction = Math.Max(target, TrailFraction - TrailSpeed * deltaSeconds);
            return TrailFraction > target;
        }

        // NaN and negative maxima mean an empty meter; without this a NaN maximum turned every fraction into NaN.
        private static float SanitizeMax(float max) => max > 0f ? Math.Min(max, float.MaxValue) : 0f;

        private static float Clamp(float value, float max)
        {
            if (value < 0f || float.IsNaN(value)) return 0f;
            return value > max ? max : value;
        }

        private void UpdateLevel()
        {
            var good = Mode == MeterThresholdMode.LowIsBad ? Fraction : 1f - Fraction;
            var level = Level;
            if (good < DangerThreshold)
            {
                level = MeterLevel.Danger;
            }
            else if (level == MeterLevel.Danger && good <= DangerThreshold + Hysteresis)
            {
                // still inside the hysteresis band: stay in danger
            }
            else
            {
                if (level == MeterLevel.Danger) level = MeterLevel.Warning;
                if (good < WarningThreshold) level = MeterLevel.Warning;
                else if (level == MeterLevel.Warning && good <= WarningThreshold + Hysteresis) { /* stay */ }
                else level = MeterLevel.Normal;
            }

            if (level == Level) return;
            Level = level;
            LevelChanged?.Invoke(level);
        }
    }
}
