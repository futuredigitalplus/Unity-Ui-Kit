using System;

namespace FutureDigitalPlus.UIKit.Interaction
{
    /// <summary>
    /// The player-facing accessibility options of a game. Fill them from the settings screen, store them with the game's own save
    /// system (all fields are plain, so <c>JsonUtility</c> works) and let <c>AccessibilityApplier</c> combine them with the OS values.
    /// </summary>
    [Serializable]
    public sealed class AccessibilityOptions
    {
        /// <summary>The player asked for no animation.</summary>
        public bool ReduceMotion;

        /// <summary>The player asked for high contrast.</summary>
        public bool HighContrast;

        /// <summary>The player asked for bold body text.</summary>
        public bool BoldText;

        /// <summary>The in-game "text size" in percent of the design size, 100 .. 200.</summary>
        public int TextScalePercent = TextScaleSteps.Min;

        /// <summary>Multiply <see cref="TextScalePercent"/> by the OS font scale.</summary>
        public bool FollowSystemFontScale = true;

        /// <summary>Turn bold text on when the OS has it on.</summary>
        public bool FollowSystemBoldText = true;

        /// <summary>Turn reduced motion on when the OS has it on.</summary>
        public bool FollowSystemReduceMotion = true;

        /// <summary>Turn high contrast on when the OS has it on.</summary>
        public bool FollowSystemHighContrast = true;

        /// <summary>Combines these options with the OS values: a flag is on when the player set it, or when the OS has it on and the matching follow option is on.</summary>
        public EffectiveAccessibility Resolve(SystemAccessibility system) => new EffectiveAccessibility(
            ReduceMotion || (FollowSystemReduceMotion && system.ReduceMotion),
            HighContrast || (FollowSystemHighContrast && system.HighContrast),
            BoldText || (FollowSystemBoldText && system.BoldText),
            TextScaleSteps.Effective(TextScalePercent, system.FontScale, FollowSystemFontScale));
    }
}
