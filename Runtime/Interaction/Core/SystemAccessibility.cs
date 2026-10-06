namespace FutureDigitalPlus.UIKit.Interaction
{
    /// <summary>What the operating system says about accessibility. Unity itself reports only the font scale and bold text; a game or a plugin can fill in the rest.</summary>
    public readonly struct SystemAccessibility
    {
        /// <summary>Nothing reported: font scale 1, everything else off.</summary>
        public static readonly SystemAccessibility Default = new SystemAccessibility(1f, false, false, false);

        /// <summary>The OS font scale, 1 for the default size.</summary>
        public readonly float FontScale;

        /// <summary>The OS "bold text" setting.</summary>
        public readonly bool BoldText;

        /// <summary>The OS "reduce motion" setting (iOS Reduce Motion, Android remove animations). Unity does not expose it; a plugin may.</summary>
        public readonly bool ReduceMotion;

        /// <summary>The OS high contrast setting. Unity does not expose it; a plugin may.</summary>
        public readonly bool HighContrast;

        /// <summary>Creates a value.</summary>
        public SystemAccessibility(float fontScale, bool boldText, bool reduceMotion, bool highContrast)
        {
            FontScale = fontScale;
            BoldText = boldText;
            ReduceMotion = reduceMotion;
            HighContrast = highContrast;
        }
    }
}
