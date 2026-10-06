using System;

namespace FutureDigitalPlus.UIKit.Foundation
{
    /// <summary>Distances from each screen edge to the safe area. In screen pixels or panel units depending on the producer; the type does not say. Equality is tolerant (0.01).</summary>
    public readonly struct SafeAreaInsets : IEquatable<SafeAreaInsets>
    {
        /// <summary>No inset on any edge.</summary>
        public static readonly SafeAreaInsets Zero = new SafeAreaInsets(0f, 0f, 0f, 0f);

        /// <summary>Distance of the safe area from the left edge.</summary>
        public float Left { get; }

        /// <summary>Distance of the safe area from the top edge.</summary>
        public float Top { get; }

        /// <summary>Distance of the safe area from the right edge.</summary>
        public float Right { get; }

        /// <summary>Distance of the safe area from the bottom edge.</summary>
        public float Bottom { get; }

        /// <summary>Creates insets.</summary>
        public SafeAreaInsets(float left, float top, float right, float bottom)
        {
            Left = left;
            Top = top;
            Right = right;
            Bottom = bottom;
        }

        /// <summary>Equal within 0.01, so sub-pixel jitter from rotation animations does not retrigger layout.</summary>
        public bool Equals(SafeAreaInsets other)
        {
            return Near(Left, other.Left) && Near(Top, other.Top) && Near(Right, other.Right) && Near(Bottom, other.Bottom);
        }

        /// <summary>Equal within 0.01 on every edge.</summary>
        public override bool Equals(object obj)
        {
            return obj is SafeAreaInsets && Equals((SafeAreaInsets)obj);
        }

        /// <summary>
        /// Constant on purpose: <see cref="Equals(SafeAreaInsets)"/> is tolerant, so two equal values can differ in any bit and every
        /// value-based hash would break the rule that equal values have equal hash codes. The struct is never a dictionary key.
        /// </summary>
        public override int GetHashCode()
        {
            return 0;
        }

        /// <summary>For example <c>L0 T47 R0 B34</c>.</summary>
        public override string ToString()
        {
            return "L" + Left + " T" + Top + " R" + Right + " B" + Bottom;
        }

        /// <summary>
        /// A copy in which every edge is at least <paramref name="percent"/> percent of the matching dimension (left and right of <paramref name="width"/>, top and bottom of <paramref name="height"/>).
        /// Televisions and some consoles overscan, so a game keeps important UI inside 5 percent. Negative percent changes nothing.
        /// </summary>
        public SafeAreaInsets WithMinimum(float percent, float width, float height)
        {
            float f = percent > 0f ? percent / 100f : 0f;
            return new SafeAreaInsets(System.Math.Max(Left, f * width), System.Math.Max(Top, f * height), System.Math.Max(Right, f * width), System.Math.Max(Bottom, f * height));
        }

        static bool Near(float a, float b)
        {
            float d = a - b;
            return d < 0.01f && d > -0.01f;
        }
    }
}
