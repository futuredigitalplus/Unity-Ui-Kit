namespace FutureDigitalPlus.UIKit.Interaction
{
    /// <summary>The USS class names this module writes on a root element. A11y.uss and Focus.uss key on them; any other sheet may do the same.</summary>
    public static class AccessibilityClasses
    {
        /// <summary>Transitions, spinners and shimmer are switched off.</summary>
        public const string ReduceMotion = "fdp-reduce-motion";

        /// <summary>The player asked for high contrast (the matching theme is a separate choice).</summary>
        public const string HighContrast = "fdp-high-contrast";

        /// <summary>Body text is bold.</summary>
        public const string BoldText = "fdp-bold-text";

        /// <summary>Prefix of the text size classes: <c>fdp-text-scale-125</c> .. <c>fdp-text-scale-200</c> (there is none for 100).</summary>
        public const string TextScalePrefix = "fdp-text-scale-";

        /// <summary>Class of the given text size step, or null for the design size (100) which needs no class.</summary>
        public static string TextScale(int step) => step <= TextScaleSteps.Min ? null : TextScalePrefix + step;
    }
}
