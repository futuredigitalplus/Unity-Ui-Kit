using System;
using System.Collections.Generic;

namespace FutureDigitalPlus.UIKit.Interaction
{
    /// <summary>
    /// The text sizes the kit's stylesheet supports, as percentages of the design size. USS cannot compute a font size from a
    /// factor (no <c>calc</c>), so A11y.uss ships one pre-generated class per step; a requested size is snapped to the nearest step.
    /// </summary>
    public static class TextScaleSteps
    {
        private static readonly int[] StepValues = { 100, 125, 150, 175, 200 };

        /// <summary>The smallest supported text size (the design size), in percent.</summary>
        public const int Min = 100;

        /// <summary>The largest supported text size, in percent.</summary>
        public const int Max = 200;

        /// <summary>All steps in ascending order: 100, 125, 150, 175, 200.</summary>
        public static IReadOnlyList<int> All => StepValues;

        /// <summary>Limits <paramref name="percent"/> to <see cref="Min"/> .. <see cref="Max"/>.</summary>
        public static int Clamp(int percent) => percent < Min ? Min : percent > Max ? Max : percent;

        /// <summary>The supported step closest to <paramref name="percent"/> (clamped first). Values in the middle of two steps cannot occur with whole percents.</summary>
        public static int Nearest(int percent)
        {
            percent = Clamp(percent);
            var best = StepValues[0];
            for (var i = 1; i < StepValues.Length; i++)
                if (Math.Abs(StepValues[i] - percent) < Math.Abs(best - percent))
                    best = StepValues[i];
            return best;
        }

        /// <summary>The player's own text size multiplied by the operating system font scale (1 = default), rounded and clamped to <see cref="Min"/> .. <see cref="Max"/>.</summary>
        /// <param name="playerPercent">The in-game "text size" setting in percent (100 = design size).</param>
        /// <param name="systemFontScale">The OS font scale, 1 for the default. Values at or below 0 are treated as 1.</param>
        /// <param name="followSystem">False to ignore the OS scale.</param>
        public static int Effective(int playerPercent, float systemFontScale, bool followSystem)
        {
            var scale = followSystem && systemFontScale > 0f ? systemFontScale : 1f;
            return Clamp((int)Math.Round(playerPercent * scale, MidpointRounding.AwayFromZero));
        }
    }
}
