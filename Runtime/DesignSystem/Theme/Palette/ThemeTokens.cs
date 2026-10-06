namespace FutureDigitalPlus.UIKit.DesignSystem.Theme.Palette
{
    /// <summary>
    /// The names of the colour tokens a theme defines (USS custom properties). Pure strings, shared by the palette
    /// derivation, the contrast audit and the tests so none of them spells a token twice.
    /// </summary>
    public static class ThemeTokens
    {
        /// <summary>Brand colour; fills of primary buttons.</summary>
        public const string Primary = "--fdp-color-primary";
        /// <summary>Hover fill of <see cref="Primary"/>.</summary>
        public const string PrimaryHover = "--fdp-color-primary-hover";
        /// <summary>Pressed fill of <see cref="Primary"/>.</summary>
        public const string PrimaryPress = "--fdp-color-primary-press";
        /// <summary>Translucent tint of <see cref="Primary"/>.</summary>
        public const string PrimarySoft = "--fdp-color-primary-soft";

        /// <summary>Second accent.</summary>
        public const string Secondary = "--fdp-color-secondary";
        /// <summary>Hover fill of <see cref="Secondary"/>.</summary>
        public const string SecondaryHover = "--fdp-color-secondary-hover";
        /// <summary>Pressed fill of <see cref="Secondary"/>.</summary>
        public const string SecondaryPress = "--fdp-color-secondary-press";
        /// <summary>Translucent tint of <see cref="Secondary"/>.</summary>
        public const string SecondarySoft = "--fdp-color-secondary-soft";

        /// <summary>Third accent.</summary>
        public const string Tertiary = "--fdp-color-tertiary";
        /// <summary>Hover fill of <see cref="Tertiary"/>.</summary>
        public const string TertiaryHover = "--fdp-color-tertiary-hover";
        /// <summary>Pressed fill of <see cref="Tertiary"/>.</summary>
        public const string TertiaryPress = "--fdp-color-tertiary-press";
        /// <summary>Translucent tint of <see cref="Tertiary"/>.</summary>
        public const string TertiarySoft = "--fdp-color-tertiary-soft";

        /// <summary>Warning accent.</summary>
        public const string Warning = "--fdp-color-warning";
        /// <summary>Hover fill of <see cref="Warning"/>.</summary>
        public const string WarningHover = "--fdp-color-warning-hover";
        /// <summary>Translucent tint of <see cref="Warning"/>.</summary>
        public const string WarningSoft = "--fdp-color-warning-soft";

        /// <summary>Danger accent.</summary>
        public const string Danger = "--fdp-color-danger";
        /// <summary>Hover fill of <see cref="Danger"/>.</summary>
        public const string DangerHover = "--fdp-color-danger-hover";
        /// <summary>Pressed fill of <see cref="Danger"/>.</summary>
        public const string DangerPress = "--fdp-color-danger-press";
        /// <summary>Translucent tint of <see cref="Danger"/>.</summary>
        public const string DangerSoft = "--fdp-color-danger-soft";

        /// <summary>Success accent.</summary>
        public const string Success = "--fdp-color-success";
        /// <summary>Informational accent.</summary>
        public const string Info = "--fdp-color-info";

        /// <summary>Main text colour.</summary>
        public const string TextPrimary = "--fdp-color-text-primary";
        /// <summary>Secondary text colour.</summary>
        public const string TextSecondary = "--fdp-color-text-secondary";
        /// <summary>Disabled text colour (exempt from contrast rules).</summary>
        public const string TextDisabled = "--fdp-color-text-disabled";
        /// <summary>Text drawn on an accent fill.</summary>
        public const string TextOnAccent = "--fdp-color-text-on-accent";

        /// <summary>Page background.</summary>
        public const string Bg = "--fdp-color-bg";
        /// <summary>Card and panel surface.</summary>
        public const string Surface = "--fdp-color-surface";
        /// <summary>Raised surface.</summary>
        public const string SurfaceElev = "--fdp-color-surface-elev";
        /// <summary>Hovered surface.</summary>
        public const string SurfaceHover = "--fdp-color-surface-hover";
        /// <summary>Decorative divider colour.</summary>
        public const string Border = "--fdp-color-border";
        /// <summary>Boundary colour of controls (inputs, toggles); must be visible.</summary>
        public const string BorderStrong = "--fdp-color-border-strong";
        /// <summary>Overlay behind popups.</summary>
        public const string Overlay = "--fdp-color-overlay";
        /// <summary>Scrim behind modal dialogs and sheets.</summary>
        public const string Scrim = "--fdp-color-scrim";
        /// <summary>Rarity colour: common.</summary>
        public const string RarityCommon = "--fdp-color-rarity-common";
        /// <summary>Rarity colour: rare.</summary>
        public const string RarityRare = "--fdp-color-rarity-rare";
        /// <summary>Rarity colour: epic.</summary>
        public const string RarityEpic = "--fdp-color-rarity-epic";
        /// <summary>Rarity colour: legendary.</summary>
        public const string RarityLegendary = "--fdp-color-rarity-legendary";
        /// <summary>Focus indicator for keyboard and gamepad navigation.</summary>
        public const string FocusRing = "--fdp-color-focus-ring";
        /// <summary>Text selection highlight in input fields.</summary>
        public const string Selection = "--fdp-color-selection";
    }
}
