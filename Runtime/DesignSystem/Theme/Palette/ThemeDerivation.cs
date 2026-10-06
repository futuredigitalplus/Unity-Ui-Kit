using System.Collections.Generic;

namespace FutureDigitalPlus.UIKit.DesignSystem.Theme.Palette
{
    /// <summary>
    /// Palette derivation without any UnityEngine dependency: a high-contrast variant of any palette, and a complete
    /// neutral and primary palette from two brand colours. Palettes are dictionaries keyed by token name
    /// (<see cref="ThemeTokens"/>), so the same code serves runtime themes, baked themes and the tests.
    /// </summary>
    public static class ThemeDerivation
    {
        private static readonly Rgba Black = new Rgba(0f, 0f, 0f);
        private static readonly Rgba White = new Rgba(1f, 1f, 1f);

        private static readonly (string rest, string hover, string press, string soft)[] Families =
        {
            (ThemeTokens.Primary,   ThemeTokens.PrimaryHover,   ThemeTokens.PrimaryPress,   ThemeTokens.PrimarySoft),
            (ThemeTokens.Secondary, ThemeTokens.SecondaryHover, ThemeTokens.SecondaryPress, ThemeTokens.SecondarySoft),
            (ThemeTokens.Tertiary,  ThemeTokens.TertiaryHover,  ThemeTokens.TertiaryPress,  ThemeTokens.TertiarySoft),
            (ThemeTokens.Danger,    ThemeTokens.DangerHover,    ThemeTokens.DangerPress,    ThemeTokens.DangerSoft),
            (ThemeTokens.Warning,   ThemeTokens.WarningHover,   null,                       ThemeTokens.WarningSoft),
        };

        /// <summary>True when the palette's page background is dark (luminance below 0.5); a palette without a background counts as dark.</summary>
        public static bool IsDark(IReadOnlyDictionary<string, Rgba> palette) =>
            !palette.TryGetValue(ThemeTokens.Bg, out var bg) || ColorMath.Luminance(bg) < 0.5f;

        /// <summary>
        /// A copy of <paramref name="source"/> that meets <paramref name="level"/>. Hues survive: accents are moved along
        /// their lightness only as far as needed. At <see cref="ContrastLevel.High"/> the neutrals (backgrounds, text,
        /// borders) are replaced by fixed black-and-white values; at <see cref="ContrastLevel.Standard"/> they are kept and
        /// only corrected where they fail.
        /// </summary>
        public static Dictionary<string, Rgba> HighContrast(IReadOnlyDictionary<string, Rgba> source, ContrastLevel level = ContrastLevel.High)
        {
            var m = new Dictionary<string, Rgba>();
            foreach (var pair in source) m[pair.Key] = pair.Value;

            var dark = IsDark(source);
            var high = level == ContrastLevel.High;
            var text = ContrastAudit.RequiredText(level);
            var nonText = ContrastAudit.RequiredNonText(level);

            if (high)
            {
                if (dark)
                {
                    Set(m, ThemeTokens.Bg, "#000000");
                    Set(m, ThemeTokens.Surface, "#0A0A0A");
                    Set(m, ThemeTokens.SurfaceElev, "#141414");
                    Set(m, ThemeTokens.SurfaceHover, "#262626");
                    Set(m, ThemeTokens.TextPrimary, "#FFFFFF");
                    Set(m, ThemeTokens.TextSecondary, "#E5E5E5");
                    Set(m, ThemeTokens.TextDisabled, "#A3A3A3");
                    Set(m, ThemeTokens.TextOnAccent, "#000000");
                    Set(m, ThemeTokens.Border, "#A3A3A3");
                    Set(m, ThemeTokens.BorderStrong, "#FFFFFF");
                }
                else
                {
                    Set(m, ThemeTokens.Bg, "#FFFFFF");
                    Set(m, ThemeTokens.Surface, "#FFFFFF");
                    Set(m, ThemeTokens.SurfaceElev, "#F2F2F2");
                    Set(m, ThemeTokens.SurfaceHover, "#E0E0E0");
                    Set(m, ThemeTokens.TextPrimary, "#000000");
                    Set(m, ThemeTokens.TextSecondary, "#1A1A1A");
                    Set(m, ThemeTokens.TextDisabled, "#525252");
                    Set(m, ThemeTokens.TextOnAccent, "#FFFFFF");
                    Set(m, ThemeTokens.Border, "#4D4D4D");
                    Set(m, ThemeTokens.BorderStrong, "#000000");
                }
            }

            // The surface that is hardest for the text colours: the lightest one in a dark theme, the darkest in a light one.
            var hardest = HardestSurface(m, dark);
            var ink = m.TryGetValue(ThemeTokens.TextOnAccent, out var onAccent) ? onAccent : (dark ? Black : White);

            if (!high)
            {
                Fix(m, ThemeTokens.TextPrimary, hardest, text);
                Fix(m, ThemeTokens.TextSecondary, hardest, text);
            }

            // Accent fills: legible as text on the surfaces, and legible with the on-accent ink on top.
            var away = ColorMath.Luminance(ink) < 0.5f ? White : Black;
            foreach (var family in Families)
            {
                if (!m.TryGetValue(family.rest, out var rest)) continue;

                rest = FitAccent(rest, ink, hardest, text);
                m[family.rest] = rest;

                m[family.hover] = FitAccent(ColorMath.Mix(rest, away, 0.25f), ink, hardest, text);
                if (family.press != null)
                    m[family.press] = FitAccent(ColorMath.Mix(rest, away, 0.5f), ink, hardest, text);
                m[family.soft] = rest.WithAlpha(high ? 0.2f : 0.16f);
            }

            foreach (var token in new[] { ThemeTokens.Success, ThemeTokens.Info })
                if (m.TryGetValue(token, out var accent))
                    m[token] = FitAccent(accent, ink, hardest, text);

            // Boundaries and the focus ring.
            Fix(m, ThemeTokens.BorderStrong, hardest, nonText);
            var ring = m.TryGetValue(ThemeTokens.TextPrimary, out var tp) ? tp : (dark ? White : Black);
            m[ThemeTokens.FocusRing] = ColorMath.EnsureRatio(ring, hardest, nonText);

            if (m.TryGetValue(ThemeTokens.Primary, out var primary))
                m[ThemeTokens.Selection] = primary.WithAlpha(high ? 0.45f : 0.35f);

            return m;
        }

        /// <summary>
        /// The colours of a complete theme made from a brand <paramref name="primary"/> and a page background: surfaces,
        /// borders, text, the primary family, focus ring and selection. Accents other than primary are not produced; the
        /// caller keeps those from a template. Text and boundary colours are adjusted until they pass the standard audit.
        /// The primary colour itself is never changed, so a brand colour that is too weak for text on a surface is reported
        /// by <see cref="ContrastAudit"/> instead of silently altered.
        /// </summary>
        public static Dictionary<string, Rgba> FromBrand(Rgba primary, Rgba bg, bool dark)
        {
            var m = new Dictionary<string, Rgba>();
            var toward = dark ? White : Black;

            var bgOpaque = bg.WithAlpha(1f);
            m[ThemeTokens.Bg] = bgOpaque;
            m[ThemeTokens.Surface] = dark ? ColorMath.Mix(bgOpaque, White, 0.04f) : White;
            m[ThemeTokens.SurfaceElev] = dark ? ColorMath.Mix(bgOpaque, White, 0.08f) : ColorMath.Mix(bgOpaque, Black, 0.03f);
            m[ThemeTokens.SurfaceHover] = dark ? ColorMath.Mix(bgOpaque, White, 0.12f) : ColorMath.Mix(bgOpaque, Black, 0.08f);

            var hardest = HardestSurface(m, dark);

            var tp = ColorMath.EnsureRatio(ColorMath.Mix(bgOpaque, toward, 0.95f), hardest, ColorMath.TextAaa);
            var ts = ColorMath.EnsureRatio(ColorMath.Mix(tp, bgOpaque, 0.3f), hardest, ColorMath.TextAa);
            m[ThemeTokens.TextPrimary] = tp;
            m[ThemeTokens.TextSecondary] = ts;
            m[ThemeTokens.TextDisabled] = ColorMath.Mix(tp, bgOpaque, 0.6f);

            m[ThemeTokens.Border] = ColorMath.Mix(bgOpaque, tp, 0.12f);
            m[ThemeTokens.BorderStrong] = ColorMath.EnsureRatio(ColorMath.Mix(bgOpaque, tp, 0.5f), hardest, ColorMath.NonTextAa);
            m[ThemeTokens.FocusRing] = tp;

            var p = primary.WithAlpha(1f);
            var onAccent = ColorMath.BestInk(p);

            // Hover and press move AWAY from the ink, so the ink keeps its contrast in every state: a dark ink sits on fills
            // that lighten, a light ink on fills that darken.
            var away = ColorMath.Luminance(onAccent) < 0.5f ? White : Black;
            m[ThemeTokens.Primary] = p;
            m[ThemeTokens.PrimaryHover] = ColorMath.Mix(p, away, 0.2f);
            m[ThemeTokens.PrimaryPress] = ColorMath.Mix(p, away, 0.4f);
            m[ThemeTokens.PrimarySoft] = p.WithAlpha(0.16f);
            m[ThemeTokens.Selection] = p.WithAlpha(0.35f);
            m[ThemeTokens.TextOnAccent] = onAccent;

            return m;
        }

        // The accent has to stay readable as text on the surfaces AND carry the ink on top of it as a fill.
        private static Rgba FitAccent(Rgba accent, Rgba ink, Rgba surface, float min)
        {
            var fitted = ColorMath.EnsureRatio(accent, ink, min);
            return ColorMath.EnsureRatio(fitted, surface, min);
        }

        private static void Fix(Dictionary<string, Rgba> m, string token, Rgba against, float min)
        {
            if (m.TryGetValue(token, out var c)) m[token] = ColorMath.EnsureRatio(c, against, min);
        }

        private static void Set(Dictionary<string, Rgba> m, string token, string hex) => m[token] = Rgba.FromHex(hex);

        private static Rgba HardestSurface(Dictionary<string, Rgba> m, bool dark)
        {
            var best = dark ? Black : White;
            var first = true;

            foreach (var token in new[] { ThemeTokens.Bg, ThemeTokens.Surface, ThemeTokens.SurfaceElev, ThemeTokens.SurfaceHover })
            {
                if (!m.TryGetValue(token, out var c)) continue;

                var lum = ColorMath.Luminance(c);
                var bestLum = ColorMath.Luminance(best);
                if (first || (dark ? lum > bestLum : lum < bestLum)) best = c;
                first = false;
            }

            return best;
        }
    }
}
