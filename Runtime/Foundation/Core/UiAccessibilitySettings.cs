using System;

namespace FutureDigitalPlus.UIKit.Foundation
{
    /// <summary>The accessibility preferences of the player that UI code reads. Immutable; equal when every value is equal.</summary>
    public readonly struct UiAccessibilitySettings : IEquatable<UiAccessibilitySettings>
    {
        /// <summary>The defaults: nothing reduced, scale 1.</summary>
        public static readonly UiAccessibilitySettings Default = new UiAccessibilitySettings(false, 1f, 1f, false, false);

        /// <summary>The player asked for less motion: animations and transitions take zero time.</summary>
        public bool ReduceMotion { get; }

        /// <summary>The player's UI size multiplier (1 = as designed).</summary>
        public float UiScale { get; }

        /// <summary>The operating system's text size multiplier (1 = default).</summary>
        public float TextScale { get; }

        /// <summary>The player asked for a high contrast look.</summary>
        public bool HighContrast { get; }

        /// <summary>The player asked for bold text.</summary>
        public bool BoldText { get; }

        /// <summary>Creates settings. Scales are clamped to 0.5 to 4 (NaN becomes 1).</summary>
        public UiAccessibilitySettings(bool reduceMotion, float uiScale, float textScale, bool highContrast, bool boldText)
        {
            ReduceMotion = reduceMotion;
            UiScale = Clamp(uiScale);
            TextScale = Clamp(textScale);
            HighContrast = highContrast;
            BoldText = boldText;
        }

        /// <summary>A copy with another <see cref="ReduceMotion"/>.</summary>
        public UiAccessibilitySettings WithReduceMotion(bool value)
        {
            return new UiAccessibilitySettings(value, UiScale, TextScale, HighContrast, BoldText);
        }

        /// <summary>A copy with another <see cref="UiScale"/>.</summary>
        public UiAccessibilitySettings WithUiScale(float value)
        {
            return new UiAccessibilitySettings(ReduceMotion, value, TextScale, HighContrast, BoldText);
        }

        /// <summary>A copy with another <see cref="TextScale"/>.</summary>
        public UiAccessibilitySettings WithTextScale(float value)
        {
            return new UiAccessibilitySettings(ReduceMotion, UiScale, value, HighContrast, BoldText);
        }

        /// <summary>A copy with another <see cref="HighContrast"/>.</summary>
        public UiAccessibilitySettings WithHighContrast(bool value)
        {
            return new UiAccessibilitySettings(ReduceMotion, UiScale, TextScale, value, BoldText);
        }

        /// <summary>A copy with another <see cref="BoldText"/>.</summary>
        public UiAccessibilitySettings WithBoldText(bool value)
        {
            return new UiAccessibilitySettings(ReduceMotion, UiScale, TextScale, HighContrast, value);
        }

        /// <summary>Every value equal.</summary>
        public bool Equals(UiAccessibilitySettings other)
        {
            return ReduceMotion == other.ReduceMotion && UiScale == other.UiScale && TextScale == other.TextScale && HighContrast == other.HighContrast && BoldText == other.BoldText;
        }

        /// <summary>Every value equal.</summary>
        public override bool Equals(object obj)
        {
            return obj is UiAccessibilitySettings && Equals((UiAccessibilitySettings)obj);
        }

        /// <summary>Hash of every value.</summary>
        public override int GetHashCode()
        {
            int hash = ReduceMotion ? 1 : 0;
            hash = hash * 31 + UiScale.GetHashCode();
            hash = hash * 31 + TextScale.GetHashCode();
            hash = hash * 31 + (HighContrast ? 1 : 0);
            return hash * 31 + (BoldText ? 1 : 0);
        }

        static float Clamp(float value)
        {
            if (float.IsNaN(value))
                return 1f;
            return value < 0.5f ? 0.5f : value > 4f ? 4f : value;
        }
    }
}
