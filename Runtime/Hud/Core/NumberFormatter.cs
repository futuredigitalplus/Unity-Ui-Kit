using System;
using System.Globalization;

namespace FutureDigitalPlus.UIKit.Hud
{
    /// <summary>How <see cref="NumberFormatter.Format"/> writes a number.</summary>
    public enum NumberFormatStyle
    {
        /// <summary>Digits only, no grouping separator (<c>1234567</c>).</summary>
        Plain,

        /// <summary>Digits with the culture's grouping separator (<c>1,234,567</c>).</summary>
        Grouped,

        /// <summary>Short form with a magnitude suffix (<c>1.2M</c>); small numbers stay grouped.</summary>
        Compact,
    }

    /// <summary>
    /// Number formatting for HUD counters: grouping by an injected culture, compact K / M / B / T and signed deltas.
    /// The culture is always a parameter, never read from the thread, so results are the same on every machine
    /// (a <c>null</c> culture means the invariant culture).
    /// </summary>
    public static class NumberFormatter
    {
        private static readonly string[] DefaultSuffixes = { "K", "M", "B", "T" };
        private static readonly string[] DecimalFormats = { "0", "0.#", "0.##", "0.###" };

        /// <summary>Writes <paramref name="value"/> rounded to a whole number with the culture's grouping separator.</summary>
        public static string Group(long value, CultureInfo culture = null)
        {
            return value.ToString("N0", culture ?? CultureInfo.InvariantCulture);
        }

        /// <summary>Writes <paramref name="value"/> rounded to a whole number with the culture's grouping separator.</summary>
        public static string Group(double value, CultureInfo culture = null)
        {
            if (double.IsNaN(value) || double.IsInfinity(value)) return value.ToString(culture ?? CultureInfo.InvariantCulture);
            return Math.Round(value, MidpointRounding.AwayFromZero).ToString("N0", culture ?? CultureInfo.InvariantCulture);
        }

        /// <summary>
        /// Writes <paramref name="value"/> with a magnitude suffix: <c>1234</c> becomes <c>1.2K</c>, <c>999999</c> becomes
        /// <c>1M</c> (rounding carries into the next suffix). Values whose magnitude is below <paramref name="compactFrom"/>
        /// are written grouped and whole.
        /// </summary>
        /// <param name="value">The number to write.</param>
        /// <param name="maxDecimals">Fraction digits kept, 0 to 3. Trailing zeros are dropped.</param>
        /// <param name="culture">Decimal and grouping separators; <c>null</c> is the invariant culture.</param>
        /// <param name="compactFrom">Magnitude from which the short form is used (default 1000).</param>
        /// <param name="suffixes">Suffixes for thousand, million, billion, trillion; <c>null</c> is K, M, B, T. Pass localized ones if needed.</param>
        public static string Compact(double value, int maxDecimals = 1, CultureInfo culture = null,
            double compactFrom = 1000d, string[] suffixes = null)
        {
            culture = culture ?? CultureInfo.InvariantCulture;
            if (double.IsNaN(value) || double.IsInfinity(value)) return value.ToString(culture);

            var magnitude = Math.Abs(value);
            if (magnitude < compactFrom || magnitude < 1000d) return Group(value, culture);

            suffixes = suffixes ?? DefaultSuffixes;
            if (suffixes.Length == 0) return Group(value, culture);
            var decimals = Math.Max(0, Math.Min(3, maxDecimals));
            var tier = 0;
            var scaled = magnitude;
            while (scaled >= 1000d && tier < suffixes.Length)
            {
                scaled /= 1000d;
                tier++;
            }

            scaled = Math.Round(scaled, decimals, MidpointRounding.AwayFromZero);
            if (scaled >= 1000d && tier < suffixes.Length)
            {
                scaled /= 1000d;
                tier++;
            }

            var sign = value < 0d ? culture.NumberFormat.NegativeSign : string.Empty;
            return sign + scaled.ToString(DecimalFormats[decimals], culture) + suffixes[tier - 1];
        }

        /// <summary>Writes <paramref name="value"/> in the requested <paramref name="style"/>.</summary>
        public static string Format(double value, NumberFormatStyle style, CultureInfo culture = null, int maxDecimals = 1)
        {
            switch (style)
            {
                case NumberFormatStyle.Plain:
                    if (double.IsNaN(value) || double.IsInfinity(value)) return value.ToString(culture ?? CultureInfo.InvariantCulture);
                    return Math.Round(value, MidpointRounding.AwayFromZero).ToString("F0", culture ?? CultureInfo.InvariantCulture);
                case NumberFormatStyle.Compact:
                    return Compact(value, maxDecimals, culture);
                default:
                    return Group(value, culture);
            }
        }

        /// <summary>Writes a change with an explicit sign: <c>+25</c>, <c>-10</c>, <c>0</c>.</summary>
        public static string Signed(long delta, CultureInfo culture = null)
        {
            culture = culture ?? CultureInfo.InvariantCulture;
            if (delta > 0) return culture.NumberFormat.PositiveSign + Group(delta, culture);
            return Group(delta, culture);
        }
    }
}
