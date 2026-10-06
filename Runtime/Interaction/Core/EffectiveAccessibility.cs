using System;

namespace FutureDigitalPlus.UIKit.Interaction
{
    /// <summary>
    /// The accessibility values that are in force after combining the player's options with what the operating system reports.
    /// Immutable; read it from <c>AccessibilityApplier.Current</c>.
    /// </summary>
    public readonly struct EffectiveAccessibility : IEquatable<EffectiveAccessibility>
    {
        /// <summary>All defaults: no reduced motion, no high contrast, no bold text, design text size.</summary>
        public static readonly EffectiveAccessibility Default = new EffectiveAccessibility(false, false, false, TextScaleSteps.Min);

        /// <summary>True when animation should be removed or shortened to nothing.</summary>
        public readonly bool ReduceMotion;

        /// <summary>True when the player or the OS asked for high contrast.</summary>
        public readonly bool HighContrast;

        /// <summary>True when body text should be bold.</summary>
        public readonly bool BoldText;

        /// <summary>Requested text size in percent of the design size, clamped to 100 .. 200 (not snapped to a step).</summary>
        public readonly int TextScalePercent;

        /// <summary>Creates a value; <paramref name="textScalePercent"/> is clamped to 100 .. 200.</summary>
        public EffectiveAccessibility(bool reduceMotion, bool highContrast, bool boldText, int textScalePercent)
        {
            ReduceMotion = reduceMotion;
            HighContrast = highContrast;
            BoldText = boldText;
            TextScalePercent = TextScaleSteps.Clamp(textScalePercent);
        }

        /// <summary><see cref="TextScalePercent"/> snapped to the nearest step the stylesheet supports (100, 125, 150, 175, 200).</summary>
        public int TextScaleStep => TextScaleSteps.Nearest(TextScalePercent);

        /// <summary>Scales an animation duration: 0 under reduced motion, unchanged otherwise.</summary>
        public int ScaleMotion(int milliseconds) => ReduceMotion ? 0 : milliseconds;

        /// <inheritdoc />
        public bool Equals(EffectiveAccessibility other) =>
            ReduceMotion == other.ReduceMotion && HighContrast == other.HighContrast && BoldText == other.BoldText && TextScalePercent == other.TextScalePercent;

        /// <inheritdoc />
        public override bool Equals(object obj) => obj is EffectiveAccessibility other && Equals(other);

        /// <inheritdoc />
        public override int GetHashCode() => (((ReduceMotion ? 1 : 0) * 31 + (HighContrast ? 1 : 0)) * 31 + (BoldText ? 1 : 0)) * 31 + TextScalePercent;

        /// <summary>Compares two values field by field.</summary>
        public static bool operator ==(EffectiveAccessibility left, EffectiveAccessibility right) => left.Equals(right);

        /// <summary>Compares two values field by field.</summary>
        public static bool operator !=(EffectiveAccessibility left, EffectiveAccessibility right) => !left.Equals(right);

        /// <inheritdoc />
        public override string ToString() =>
            $"reduceMotion={ReduceMotion} highContrast={HighContrast} bold={BoldText} text={TextScalePercent}%";
    }
}
