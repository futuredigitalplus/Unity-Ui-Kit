using System.Collections.Generic;

namespace FutureDigitalPlus.UIKit.DesignSystem.Theme.Palette
{
    /// <summary>
    /// The colours of the themes the package ships, as plain data. The editor writes these into the theme assets
    /// (<c>ThemePresets</c>), the contrast tests audit them, and <c>DesignTokens.uss</c> mirrors <see cref="Dark"/>
    /// as its defaults. Pure; no UnityEngine.
    /// </summary>
    public static class ThemePalettes
    {
        /// <summary>The default theme, scope <c>:root</c>. Hover and press fills lighten so the dark ink stays readable on them.</summary>
        public static IReadOnlyDictionary<string, Rgba> Dark { get; } = Build(new[]
        {
            (ThemeTokens.Primary, "#22C55E"), (ThemeTokens.PrimaryHover, "#4ADE80"), (ThemeTokens.PrimaryPress, "#86EFAC"),
            (ThemeTokens.Secondary, "#3B82F6"), (ThemeTokens.SecondaryHover, "#60A5FA"), (ThemeTokens.SecondaryPress, "#93C5FD"),
            (ThemeTokens.Tertiary, "#B26CF9"), (ThemeTokens.TertiaryHover, "#C084FC"), (ThemeTokens.TertiaryPress, "#D8B4FE"),
            (ThemeTokens.Warning, "#F59E0B"), (ThemeTokens.WarningHover, "#FBBF24"),
            (ThemeTokens.Danger, "#EF4444"), (ThemeTokens.DangerHover, "#F87171"), (ThemeTokens.DangerPress, "#FCA5A5"),
            (ThemeTokens.Success, "#22C55E"), (ThemeTokens.Info, "#60A5FA"),
            (ThemeTokens.TextPrimary, "#F2F4F7"), (ThemeTokens.TextSecondary, "#A1A7B3"),
            (ThemeTokens.TextDisabled, "#677085"), (ThemeTokens.TextOnAccent, "#0B0F17"),
            (ThemeTokens.Bg, "#0B0F17"), (ThemeTokens.Surface, "#131A24"), (ThemeTokens.SurfaceElev, "#1A2330"),
            (ThemeTokens.SurfaceHover, "#1F2937"), (ThemeTokens.Border, "#263041"), (ThemeTokens.BorderStrong, "#5E6E88"),
            (ThemeTokens.FocusRing, "#F2F4F7"),
        }, softAlpha: 0.16f, overlay: new Rgba(0f, 0f, 0f, 0.6f), scrim: new Rgba(0f, 0f, 0f, 0.6f), selectionAlpha: 0.35f);

        /// <summary>Day mode, scope <c>.theme-light</c>. Accent fills are dark enough for white ink.</summary>
        public static IReadOnlyDictionary<string, Rgba> Light { get; } = Build(new[]
        {
            (ThemeTokens.Primary, "#15803D"), (ThemeTokens.PrimaryHover, "#166534"), (ThemeTokens.PrimaryPress, "#14532D"),
            (ThemeTokens.Secondary, "#2563EB"), (ThemeTokens.SecondaryHover, "#1D4ED8"), (ThemeTokens.SecondaryPress, "#1E40AF"),
            (ThemeTokens.Tertiary, "#9333EA"), (ThemeTokens.TertiaryHover, "#7E22CE"), (ThemeTokens.TertiaryPress, "#6B21A8"),
            (ThemeTokens.Warning, "#B45309"), (ThemeTokens.WarningHover, "#92400E"),
            (ThemeTokens.Danger, "#DC2626"), (ThemeTokens.DangerHover, "#B91C1C"), (ThemeTokens.DangerPress, "#991B1B"),
            (ThemeTokens.Success, "#15803D"), (ThemeTokens.Info, "#2563EB"),
            (ThemeTokens.TextPrimary, "#0F172A"), (ThemeTokens.TextSecondary, "#475569"),
            (ThemeTokens.TextDisabled, "#94A3B8"), (ThemeTokens.TextOnAccent, "#FFFFFF"),
            (ThemeTokens.Bg, "#F8FAFC"), (ThemeTokens.Surface, "#FFFFFF"), (ThemeTokens.SurfaceElev, "#F1F5F9"),
            (ThemeTokens.SurfaceHover, "#E2E8F0"), (ThemeTokens.Border, "#E2E8F0"), (ThemeTokens.BorderStrong, "#78879F"),
            (ThemeTokens.FocusRing, "#0F172A"),
        }, softAlpha: 0.16f, overlay: new Rgba(0f, 0f, 0f, 0.5f), scrim: new Rgba(0f, 0f, 0f, 0.5f), selectionAlpha: 0.35f);

        /// <summary>A grey, brand-free dark theme to start a game from, scope <c>:root</c>.</summary>
        public static IReadOnlyDictionary<string, Rgba> NeutralDark { get; } = Build(new[]
        {
            (ThemeTokens.Primary, "#D4D4D8"), (ThemeTokens.PrimaryHover, "#E4E4E7"), (ThemeTokens.PrimaryPress, "#F4F4F5"),
            (ThemeTokens.Secondary, "#3B82F6"), (ThemeTokens.SecondaryHover, "#60A5FA"), (ThemeTokens.SecondaryPress, "#93C5FD"),
            (ThemeTokens.Tertiary, "#B26CF9"), (ThemeTokens.TertiaryHover, "#C084FC"), (ThemeTokens.TertiaryPress, "#D8B4FE"),
            (ThemeTokens.Warning, "#F59E0B"), (ThemeTokens.WarningHover, "#FBBF24"),
            (ThemeTokens.Danger, "#EF4444"), (ThemeTokens.DangerHover, "#F87171"), (ThemeTokens.DangerPress, "#FCA5A5"),
            (ThemeTokens.Success, "#22C55E"), (ThemeTokens.Info, "#60A5FA"),
            (ThemeTokens.TextPrimary, "#FAFAFA"), (ThemeTokens.TextSecondary, "#D4D4D8"),
            (ThemeTokens.TextDisabled, "#71717A"), (ThemeTokens.TextOnAccent, "#09090B"),
            (ThemeTokens.Bg, "#09090B"), (ThemeTokens.Surface, "#18181B"), (ThemeTokens.SurfaceElev, "#27272A"),
            (ThemeTokens.SurfaceHover, "#3F3F46"), (ThemeTokens.Border, "#3F3F46"), (ThemeTokens.BorderStrong, "#71717A"),
            (ThemeTokens.FocusRing, "#FAFAFA"),
        }, softAlpha: 0.16f, overlay: new Rgba(0f, 0f, 0f, 0.6f), scrim: new Rgba(0f, 0f, 0f, 0.6f), selectionAlpha: 0.35f);

        /// <summary>A grey, brand-free light theme to start a game from, scope <c>.theme-neutral-light</c>.</summary>
        public static IReadOnlyDictionary<string, Rgba> NeutralLight { get; } = Build(new[]
        {
            (ThemeTokens.Primary, "#3F3F46"), (ThemeTokens.PrimaryHover, "#27272A"), (ThemeTokens.PrimaryPress, "#18181B"),
            (ThemeTokens.Secondary, "#2563EB"), (ThemeTokens.SecondaryHover, "#1D4ED8"), (ThemeTokens.SecondaryPress, "#1E40AF"),
            (ThemeTokens.Tertiary, "#9333EA"), (ThemeTokens.TertiaryHover, "#7E22CE"), (ThemeTokens.TertiaryPress, "#6B21A8"),
            (ThemeTokens.Warning, "#B45309"), (ThemeTokens.WarningHover, "#92400E"),
            (ThemeTokens.Danger, "#DC2626"), (ThemeTokens.DangerHover, "#B91C1C"), (ThemeTokens.DangerPress, "#991B1B"),
            (ThemeTokens.Success, "#15803D"), (ThemeTokens.Info, "#2563EB"),
            (ThemeTokens.TextPrimary, "#18181B"), (ThemeTokens.TextSecondary, "#52525B"),
            (ThemeTokens.TextDisabled, "#A1A1AA"), (ThemeTokens.TextOnAccent, "#FFFFFF"),
            (ThemeTokens.Bg, "#FAFAFA"), (ThemeTokens.Surface, "#FFFFFF"), (ThemeTokens.SurfaceElev, "#F4F4F5"),
            (ThemeTokens.SurfaceHover, "#E4E4E7"), (ThemeTokens.Border, "#E4E4E7"), (ThemeTokens.BorderStrong, "#71717A"),
            (ThemeTokens.FocusRing, "#18181B"),
        }, softAlpha: 0.16f, overlay: new Rgba(0f, 0f, 0f, 0.5f), scrim: new Rgba(0f, 0f, 0f, 0.5f), selectionAlpha: 0.35f);

        /// <summary>The high-contrast counterpart of <see cref="Dark"/> (7:1 text, 4.5:1 boundaries), scope <c>.theme-hc-dark</c>.</summary>
        public static IReadOnlyDictionary<string, Rgba> HighContrastDark { get; } =
            ThemeDerivation.HighContrast(Dark, ContrastLevel.High);

        /// <summary>The high-contrast counterpart of <see cref="Light"/>, scope <c>.theme-hc-light</c>.</summary>
        public static IReadOnlyDictionary<string, Rgba> HighContrastLight { get; } =
            ThemeDerivation.HighContrast(Light, ContrastLevel.High);

        // Rarity carries meaning that does not change with the lights, so every theme shares the same four colours.
        private static Dictionary<string, Rgba> Build((string token, string hex)[] opaque, float softAlpha,
                                                       Rgba overlay, Rgba scrim, float selectionAlpha)
        {
            var m = new Dictionary<string, Rgba>();
            foreach (var (token, hex) in opaque) m[token] = Rgba.FromHex(hex);

            m[ThemeTokens.PrimarySoft] = m[ThemeTokens.Primary].WithAlpha(softAlpha);
            m[ThemeTokens.SecondarySoft] = m[ThemeTokens.Secondary].WithAlpha(softAlpha);
            m[ThemeTokens.TertiarySoft] = m[ThemeTokens.Tertiary].WithAlpha(softAlpha);
            m[ThemeTokens.WarningSoft] = m[ThemeTokens.Warning].WithAlpha(softAlpha);
            m[ThemeTokens.DangerSoft] = m[ThemeTokens.Danger].WithAlpha(softAlpha);
            m[ThemeTokens.Selection] = m[ThemeTokens.Primary].WithAlpha(selectionAlpha);
            m[ThemeTokens.Overlay] = overlay;
            m[ThemeTokens.Scrim] = scrim;

            m[ThemeTokens.RarityCommon] = Rgba.FromHex("#22C55E");
            m[ThemeTokens.RarityRare] = Rgba.FromHex("#3B82F6");
            m[ThemeTokens.RarityEpic] = Rgba.FromHex("#A855F7");
            m[ThemeTokens.RarityLegendary] = Rgba.FromHex("#F59E0B");
            return m;
        }
    }
}
