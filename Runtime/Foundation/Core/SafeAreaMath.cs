namespace FutureDigitalPlus.UIKit.Foundation
{
    /// <summary>Safe-area arithmetic, with no Unity types. Unity's <c>Screen.safeArea</c> is a pixel rectangle with its origin at the bottom left.</summary>
    public static class SafeAreaMath
    {
        /// <summary>
        /// Insets in screen pixels from the screen size and the safe rectangle (x, y of the bottom left corner, width, height).
        /// An empty or non-finite rectangle gives zero insets, and a rectangle that sticks out of the screen is clamped, so the result is never negative or NaN.
        /// </summary>
        public static SafeAreaInsets FromScreen(float screenWidth, float screenHeight, float safeX, float safeY, float safeWidth, float safeHeight)
        {
            if (!(screenWidth > 0f) || !(screenHeight > 0f) || !(safeWidth > 0f) || !(safeHeight > 0f) || float.IsNaN(safeX) || float.IsNaN(safeY)
                || float.IsInfinity(screenWidth) || float.IsInfinity(screenHeight) || float.IsInfinity(safeWidth) || float.IsInfinity(safeHeight))
                return SafeAreaInsets.Zero;
            float left = Clamp(safeX, 0f, screenWidth);
            float bottom = Clamp(safeY, 0f, screenHeight);
            float right = Clamp(screenWidth - (safeX + safeWidth), 0f, screenWidth);
            float top = Clamp(screenHeight - (safeY + safeHeight), 0f, screenHeight);
            return new SafeAreaInsets(left, top, right, bottom);
        }

        /// <summary>
        /// Scales pixel insets to panel units. <paramref name="unitsPerPixel"/> is the panel root width in units divided by <c>Screen.width</c>;
        /// it is the same for both axes in every scale mode the foundation uses, which avoids guessing the y-axis convention of coordinate conversions.
        /// A non-positive factor gives zero insets.
        /// </summary>
        public static SafeAreaInsets ToPanel(SafeAreaInsets pixels, float unitsPerPixel)
        {
            if (!(unitsPerPixel > 0f) || float.IsInfinity(unitsPerPixel))
                return SafeAreaInsets.Zero;
            return new SafeAreaInsets(pixels.Left * unitsPerPixel, pixels.Top * unitsPerPixel, pixels.Right * unitsPerPixel, pixels.Bottom * unitsPerPixel);
        }

        /// <summary>Keeps only the insets of the listed edges.</summary>
        public static SafeAreaInsets Mask(SafeAreaInsets insets, SafeAreaEdges edges)
        {
            return new SafeAreaInsets(
                (edges & SafeAreaEdges.Left) != 0 ? insets.Left : 0f,
                (edges & SafeAreaEdges.Top) != 0 ? insets.Top : 0f,
                (edges & SafeAreaEdges.Right) != 0 ? insets.Right : 0f,
                (edges & SafeAreaEdges.Bottom) != 0 ? insets.Bottom : 0f);
        }

        static float Clamp(float value, float min, float max)
        {
            if (value < min)
                return min;
            return value > max ? max : value;
        }
    }
}
