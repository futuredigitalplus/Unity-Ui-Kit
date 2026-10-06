using System;
using System.Globalization;

namespace FutureDigitalPlus.UIKit.Hud
{
    /// <summary>How <see cref="CountdownModel.Format(double, CountdownStyle, CountdownUnits)"/> writes the remaining time.</summary>
    public enum CountdownStyle
    {
        /// <summary>Two units that shrink with the time: <c>3d 4h</c>, <c>2h 15m</c>, <c>02:15</c>, <c>45s</c>.</summary>
        Compact,

        /// <summary>Always <c>HH:MM:SS</c>; hours can go past 24 (<c>49:05:09</c>).</summary>
        Clock,
    }

    /// <summary>Unit suffixes of the <see cref="CountdownStyle.Compact"/> style. Replace them to localize.</summary>
    public readonly struct CountdownUnits
    {
        /// <summary>Creates a set of suffixes.</summary>
        public CountdownUnits(string day, string hour, string minute, string second, string separator = " ")
        {
            Day = day;
            Hour = hour;
            Minute = minute;
            Second = second;
            Separator = separator;
        }

        /// <summary>The English suffixes <c>d h m s</c> separated by a space.</summary>
        public static CountdownUnits Default => new CountdownUnits("d", "h", "m", "s");

        /// <summary>Suffix after the day count.</summary>
        public string Day { get; }

        /// <summary>Suffix after the hour count.</summary>
        public string Hour { get; }

        /// <summary>Suffix after the minute count.</summary>
        public string Minute { get; }

        /// <summary>Suffix after the second count.</summary>
        public string Second { get; }

        /// <summary>Text between the two units of a tier.</summary>
        public string Separator { get; }
    }

    /// <summary>
    /// The logic of a countdown to a moment in time (event ends, daily reset, respawn). The clock is injected as a
    /// function returning seconds, so the same model counts wall-clock time (UTC seconds) or game time, and tests
    /// use a fake clock.
    /// </summary>
    public sealed class CountdownModel
    {
        // 100 000 days: far beyond any timer a player reads, far below long.MaxValue.
        private const double MaxFormattedSeconds = 8640000000d;

        private readonly Func<double> _clock;
        private double _end;
        private bool _expiredRaised;

        /// <summary>Creates a model reading time from <paramref name="clock"/> (seconds, any origin, must not run backwards).</summary>
        public CountdownModel(Func<double> clock)
        {
            _clock = clock ?? throw new ArgumentNullException(nameof(clock));
        }

        /// <summary>True between <see cref="Start"/> / <see cref="StartAt"/> and <see cref="Stop"/> or expiry.</summary>
        public bool IsRunning { get; private set; }

        /// <summary>True once the end time has passed (until the next start).</summary>
        public bool IsExpired { get; private set; }

        /// <summary>Raised once by <see cref="Poll"/> when the end time is reached.</summary>
        public event Action Expired;

        /// <summary>Seconds left, never negative; 0 when stopped or expired.</summary>
        public double Remaining => IsRunning ? Math.Max(0d, _end - _clock()) : 0d;

        /// <summary>Remaining time rounded up to whole seconds, so the display shows 1s until the very end.</summary>
        public long RemainingWholeSeconds => (long)Math.Ceiling(Remaining);

        /// <summary>Starts a countdown that ends <paramref name="durationSeconds"/> from now.</summary>
        public void Start(double durationSeconds)
        {
            StartAt(_clock() + Math.Max(0d, durationSeconds));
        }

        /// <summary>Starts a countdown that ends at <paramref name="endTime"/>, expressed on the clock's own timeline.</summary>
        public void StartAt(double endTime)
        {
            _end = endTime;
            IsRunning = true;
            IsExpired = false;
            _expiredRaised = false;
        }

        /// <summary>Stops the countdown without raising <see cref="Expired"/>.</summary>
        public void Stop()
        {
            IsRunning = false;
            IsExpired = false;
        }

        /// <summary>Checks the clock. Returns true, and raises <see cref="Expired"/>, on the first call after the end time.</summary>
        public bool Poll()
        {
            if (!IsRunning || _expiredRaised || _clock() < _end) return false;
            _expiredRaised = true;
            IsRunning = false;
            IsExpired = true;
            Expired?.Invoke();
            return true;
        }

        /// <summary>Writes the remaining time in the given style.</summary>
        public string Format(CountdownStyle style = CountdownStyle.Compact)
        {
            return Format(Remaining, style, CountdownUnits.Default);
        }

        /// <summary>Writes <paramref name="seconds"/> (rounded up to whole seconds, negative counts as 0) in the given style.</summary>
        public static string Format(double seconds, CountdownStyle style, CountdownUnits units)
        {
            // NaN and negative show as zero; a huge or infinite value is capped (the cast of such a double to long is not defined).
            var total = !(seconds > 0d) ? 0L : (seconds >= MaxFormattedSeconds ? (long)MaxFormattedSeconds : (long)Math.Ceiling(seconds));
            var days = total / 86400L;
            var hours = total / 3600L;
            var inv = CultureInfo.InvariantCulture;

            if (style == CountdownStyle.Clock)
                return hours.ToString("00", inv) + ":" + (total / 60L % 60L).ToString("00", inv) + ":" + (total % 60L).ToString("00", inv);

            if (days >= 1L)
                return days.ToString(inv) + units.Day + units.Separator + (total / 3600L % 24L).ToString(inv) + units.Hour;
            if (hours >= 1L)
                return hours.ToString(inv) + units.Hour + units.Separator + (total / 60L % 60L).ToString(inv) + units.Minute;
            if (total >= 60L)
                return (total / 60L).ToString("00", inv) + ":" + (total % 60L).ToString("00", inv);
            return total.ToString(inv) + units.Second;
        }
    }
}
