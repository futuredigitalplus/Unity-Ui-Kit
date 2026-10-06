using System.Collections.Generic;

namespace FutureDigitalPlus.UIKit.DesignSystem.Theme.Palette
{
    /// <summary>How strict the audit and the derived high-contrast palettes are.</summary>
    public enum ContrastLevel
    {
        /// <summary>WCAG AA: 4.5:1 for text, 3:1 for control boundaries and focus rings.</summary>
        Standard = 0,

        /// <summary>The high-contrast level: 7:1 for text (WCAG AAA), 4.5:1 for control boundaries and focus rings.</summary>
        High = 1,
    }

    /// <summary>What a <see cref="ContrastCheck"/> measures.</summary>
    public enum ContrastCheckKind
    {
        /// <summary>Text on a surface; needs the text minimum.</summary>
        Text = 0,

        /// <summary>Text on an accent fill; needs the text minimum.</summary>
        TextOnFill = 1,

        /// <summary>An accent colour used as text or icon on a surface; needs the text minimum.</summary>
        Accent = 2,

        /// <summary>A control boundary or focus indicator; needs the non-text minimum.</summary>
        NonText = 3,
    }

    /// <summary>One measured pair of tokens.</summary>
    public readonly struct ContrastCheck
    {
        /// <summary>What kind of pair this is.</summary>
        public readonly ContrastCheckKind Kind;
        /// <summary>The foreground token (for example <c>--fdp-color-text-primary</c>).</summary>
        public readonly string Foreground;
        /// <summary>The background token.</summary>
        public readonly string Background;
        /// <summary>The measured contrast ratio, 1 to 21.</summary>
        public readonly float Ratio;
        /// <summary>The ratio the pair has to reach.</summary>
        public readonly float Required;

        /// <summary>True when <see cref="Ratio"/> reaches <see cref="Required"/> (a 0.005 tolerance absorbs 8-bit rounding of the stored colours).</summary>
        public bool Passed => Ratio + 0.005f >= Required;

        /// <summary>Creates a result.</summary>
        public ContrastCheck(ContrastCheckKind kind, string foreground, string background, float ratio, float required)
        {
            Kind = kind;
            Foreground = foreground;
            Background = background;
            Ratio = ratio;
            Required = required;
        }

        /// <inheritdoc />
        public override string ToString() => string.Format(System.Globalization.CultureInfo.InvariantCulture,
            "{0} on {1}: {2:0.00} (needs {3:0.0}) {4}", Foreground, Background, Ratio, Required, Passed ? "ok" : "FAIL");
    }

    /// <summary>
    /// Measures a palette against the contrast rules the design system promises: text on every surface, text on every
    /// accent fill in rest, hover and press, accent colours as text, the strong border and the focus ring. Disabled text
    /// is exempt (WCAG 1.4.3). Tokens a palette does not define are skipped, so a theme baked before a token existed
    /// can still be audited. Pure; the Unity layer feeds it colours through <c>ThemeData.ToColorMap()</c>.
    /// </summary>
    public static class ContrastAudit
    {
        private static readonly string[] TextTokens = { ThemeTokens.TextPrimary, ThemeTokens.TextSecondary };
        private static readonly string[] TextSurfaces = { ThemeTokens.Bg, ThemeTokens.Surface, ThemeTokens.SurfaceElev };
        private static readonly string[] Accents =
        {
            ThemeTokens.Primary, ThemeTokens.Secondary, ThemeTokens.Tertiary, ThemeTokens.Danger,
            ThemeTokens.Warning, ThemeTokens.Success, ThemeTokens.Info,
        };
        private static readonly string[] AccentSurfaces = { ThemeTokens.Bg, ThemeTokens.Surface };
        private static readonly string[] Fills =
        {
            ThemeTokens.Primary, ThemeTokens.PrimaryHover, ThemeTokens.PrimaryPress,
            ThemeTokens.Secondary, ThemeTokens.SecondaryHover, ThemeTokens.SecondaryPress,
            ThemeTokens.Tertiary, ThemeTokens.TertiaryHover, ThemeTokens.TertiaryPress,
            ThemeTokens.Danger, ThemeTokens.DangerHover, ThemeTokens.DangerPress,
            ThemeTokens.Warning, ThemeTokens.WarningHover,
        };

        /// <summary>The ratio a text pair has to reach at <paramref name="level"/>.</summary>
        public static float RequiredText(ContrastLevel level) =>
            level == ContrastLevel.High ? ColorMath.TextAaa : ColorMath.TextAa;

        /// <summary>The ratio a boundary or focus indicator has to reach at <paramref name="level"/>.</summary>
        public static float RequiredNonText(ContrastLevel level) =>
            level == ContrastLevel.High ? ColorMath.NonTextHighContrast : ColorMath.NonTextAa;

        /// <summary>Runs every check the palette has the tokens for.</summary>
        public static List<ContrastCheck> Run(IReadOnlyDictionary<string, Rgba> colors, ContrastLevel level = ContrastLevel.Standard)
        {
            var results = new List<ContrastCheck>(80);
            var text = RequiredText(level);
            var nonText = RequiredNonText(level);

            foreach (var fg in TextTokens)
                foreach (var bg in TextSurfaces)
                    Add(results, colors, ContrastCheckKind.Text, fg, bg, text);

            foreach (var fill in Fills)
                Add(results, colors, ContrastCheckKind.TextOnFill, ThemeTokens.TextOnAccent, fill, text);

            foreach (var accent in Accents)
                foreach (var bg in AccentSurfaces)
                    Add(results, colors, ContrastCheckKind.Accent, accent, bg, text);

            foreach (var bg in AccentSurfaces)
            {
                Add(results, colors, ContrastCheckKind.NonText, ThemeTokens.BorderStrong, bg, nonText);
                Add(results, colors, ContrastCheckKind.NonText, ThemeTokens.FocusRing, bg, nonText);
            }

            return results;
        }

        /// <summary>The checks of <see cref="Run"/> that failed.</summary>
        public static List<ContrastCheck> Failures(IReadOnlyDictionary<string, Rgba> colors, ContrastLevel level = ContrastLevel.Standard)
        {
            var failed = new List<ContrastCheck>();
            foreach (var check in Run(colors, level))
                if (!check.Passed) failed.Add(check);
            return failed;
        }

        private static void Add(List<ContrastCheck> into, IReadOnlyDictionary<string, Rgba> colors,
                                ContrastCheckKind kind, string fg, string bg, float required)
        {
            if (!colors.TryGetValue(fg, out var f) || !colors.TryGetValue(bg, out var b)) return;

            // Translucent colours are measured the way they are seen: composited over the background.
            into.Add(new ContrastCheck(kind, fg, bg, ColorMath.Ratio(f, b, compositeForeground: true), required));
        }
    }
}
