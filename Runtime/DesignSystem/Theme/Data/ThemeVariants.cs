using FutureDigitalPlus.UIKit.DesignSystem.Theme.Applier;
using FutureDigitalPlus.UIKit.DesignSystem.Theme.Palette;
using UnityEngine;

namespace FutureDigitalPlus.UIKit.DesignSystem.Theme.Data
{
    /// <summary>
    /// Runtime variants of a theme: a high-contrast version and a version with larger text. Each returns a new runtime
    /// theme (see <see cref="ThemeData.CreateRuntime"/>) that the caller applies with <see cref="ThemeRuntime.Apply"/> and
    /// destroys when done; the source theme is not changed.
    /// </summary>
    public static class ThemeVariants
    {
        /// <summary>The Resources path of the baked high-contrast counterpart of the built-in Dark theme, when one was generated.</summary>
        public const string HighContrastDarkResource = "FutureDigitalPlus/UIKit/DesignSystem/Themes/HighContrastDark";

        /// <summary>The Resources path of the baked high-contrast counterpart of the built-in Light theme, when one was generated.</summary>
        public const string HighContrastLightResource = "FutureDigitalPlus/UIKit/DesignSystem/Themes/HighContrastLight";

        /// <summary>
        /// A runtime theme with <paramref name="source"/>'s hues and a palette that meets <paramref name="level"/> (see
        /// <see cref="ThemeDerivation.HighContrast"/>). At <see cref="ContrastLevel.High"/> border widths grow by one pixel.
        /// </summary>
        /// <param name="source">A baked or runtime theme.</param>
        /// <param name="level">Standard keeps the neutrals and fixes failures; High uses black-and-white neutrals and 7:1 text.</param>
        /// <returns>The new theme, or null when <paramref name="source"/> is null.</returns>
        public static ThemeData HighContrast(ThemeData source, ContrastLevel level = ContrastLevel.High)
        {
            var variant = ThemeData.CreateRuntime(source);
            if (!variant) return null;

            variant.ApplyColorMap(ThemeDerivation.HighContrast(source.ToColorMap(), level));

            if (level == ContrastLevel.High)
            {
                variant.borderThin = source.borderThin + 1f;
                variant.borderRegular = source.borderRegular + 1f;
            }

            variant.name = source.name + " (high contrast)";
            return variant;
        }

        /// <summary>
        /// The high-contrast theme to wear instead of <paramref name="source"/>: the baked built-in counterpart when
        /// <paramref name="source"/> is the built-in Dark or Light theme and the counterpart exists, otherwise a derived
        /// runtime variant (<see cref="HighContrast"/>). A returned runtime theme belongs to the caller.
        /// </summary>
        /// <param name="source">The theme to find a counterpart for.</param>
        /// <param name="level">The level used when a variant has to be derived.</param>
        /// <returns>A theme to apply, or null when <paramref name="source"/> is null.</returns>
        public static ThemeData HighContrastFor(ThemeData source, ContrastLevel level = ContrastLevel.High)
        {
            if (!source) return null;

            if (level == ContrastLevel.High)
            {
                string path = null;
                if (source == Resources.Load<ThemeData>(ThemeData.DarkResource)) path = HighContrastDarkResource;
                else if (source == Resources.Load<ThemeData>(ThemeData.LightResource)) path = HighContrastLightResource;

                var baked = path == null ? null : Resources.Load<ThemeData>(path);
                if (baked && baked.StyleSheet) return baked;
            }

            return HighContrast(source, level);
        }

        /// <summary>
        /// A runtime theme whose six body and heading font sizes (and the display size) are multiplied by
        /// <paramref name="factor"/>, clamped to 0.75 to 2. Controls whose height is a token or a minimum grow with the text;
        /// for a plain "make everything bigger" use the panel scale instead.
        /// </summary>
        /// <param name="source">A baked or runtime theme.</param>
        /// <param name="factor">The multiplier, for example 1.25.</param>
        /// <returns>The new theme, or null when <paramref name="source"/> is null.</returns>
        public static ThemeData WithTextScale(ThemeData source, float factor)
        {
            var variant = ThemeData.CreateRuntime(source);
            if (!variant) return null;

            var k = Mathf.Clamp(factor, 0.75f, 2f);
            variant.fontSizeH1 = Mathf.Round(source.fontSizeH1 * k);
            variant.fontSizeH2 = Mathf.Round(source.fontSizeH2 * k);
            variant.fontSizeH3 = Mathf.Round(source.fontSizeH3 * k);
            variant.fontSizeBody1 = Mathf.Round(source.fontSizeBody1 * k);
            variant.fontSizeBody2 = Mathf.Round(source.fontSizeBody2 * k);
            variant.fontSizeCaption = Mathf.Round(source.fontSizeCaption * k);
            variant.fontSizeDisplay = Mathf.Round(source.fontSizeDisplay * k);
            variant.name = source.name + " (text x" + k.ToString("0.##", System.Globalization.CultureInfo.InvariantCulture) + ")";
            return variant;
        }
    }
}
