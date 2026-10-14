using System;

namespace FutureDigitalPlus.UIKit.Foundation
{
    /// <summary>
    /// What a game wants on one form factor and orientation: a <see cref="scale"/> multiplier on top of the panel scale (1 = as the scale policy decides,
    /// 0.9 = everything 10 percent smaller) and a <see cref="maxContentWidth"/> for the content columns (0 = no limit). Plain serializable values: a rule
    /// asset stores a list of them (<see cref="ResponsiveRules"/>).
    /// </summary>
    [Serializable]
    public struct ResponsiveRule
    {
        /// <summary>The smallest scale multiplier that is used.</summary>
        public const float MinScale = 0.5f;

        /// <summary>The largest scale multiplier that is used.</summary>
        public const float MaxScale = 2f;

        /// <summary>The form factor the rule is for.</summary>
        public DeviceFormFactor form;

        /// <summary>The orientation the rule is for (<see cref="ResponsiveOrientation.Any"/> covers both).</summary>
        public ResponsiveOrientation orientation;

        /// <summary>Multiplier on the panel scale, <see cref="MinScale"/> to <see cref="MaxScale"/>. Read it through <see cref="Scale"/>, which repairs bad values.</summary>
        public float scale;

        /// <summary>Largest width of a content column in units, 0 for no limit. Read it through <see cref="MaxContentWidth"/>.</summary>
        public float maxContentWidth;

        /// <summary>A rule that changes nothing: scale 1 and no width limit.</summary>
        public static ResponsiveRule Identity(DeviceFormFactor form, ResponsiveOrientation orientation)
        {
            return new ResponsiveRule { form = form, orientation = orientation, scale = 1f, maxContentWidth = 0f };
        }

        /// <summary>The scale multiplier, clamped to <see cref="MinScale"/> to <see cref="MaxScale"/>; 1 for 0, a negative value, NaN or infinity (an empty or corrupt asset).</summary>
        public float Scale
        {
            get
            {
                if (!(scale > 0f) || float.IsInfinity(scale))
                    return 1f;
                return Math.Max(MinScale, Math.Min(MaxScale, scale));
            }
        }

        /// <summary>The column width limit in units; 0 (no limit) for 0, a negative value, NaN or infinity.</summary>
        public float MaxContentWidth
        {
            get { return maxContentWidth > 0f && !float.IsInfinity(maxContentWidth) ? maxContentWidth : 0f; }
        }

        /// <summary>True when the rule changes nothing (scale 1, no width limit).</summary>
        public bool IsIdentity
        {
            get { return Math.Abs(Scale - 1f) < 0.0001f && MaxContentWidth <= 0f; }
        }
    }
}
