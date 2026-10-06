using UnityEngine;

namespace FutureDigitalPlus.UIKit
{
    /// <summary>Safe-area insets in panel units (left/top/right/bottom, all >= 0).</summary>
    public readonly struct SafeAreaInsets
    {
        /// <summary>Left inset in panel units.</summary>
        public readonly float Left;
        /// <summary>Top inset in panel units.</summary>
        public readonly float Top;
        /// <summary>Right inset in panel units.</summary>
        public readonly float Right;
        /// <summary>Bottom inset in panel units.</summary>
        public readonly float Bottom;

        /// <summary>Creates insets from the four edges, in panel units.</summary>
        public SafeAreaInsets(float left, float top, float right, float bottom)
        {
            Left = left; Top = top; Right = right; Bottom = bottom;
        }

        /// <summary>No inset on any edge.</summary>
        public static readonly SafeAreaInsets None = new SafeAreaInsets(0, 0, 0, 0);

        /// <summary>
        /// Converts a screen-space safe area (pixels, origin bottom-left, as <c>Screen.safeArea</c>) into
        /// insets in panel units. Pure function, so it can be unit tested and used with any source.
        /// </summary>
        /// <param name="safeArea">Safe rectangle in screen pixels.</param>
        /// <param name="screenSize">Full screen size in pixels.</param>
        /// <param name="panelSize">Size of the panel root in panel units (what the layout reports).</param>
        public static SafeAreaInsets FromScreen(Rect safeArea, Vector2 screenSize, Vector2 panelSize)
        {
            if (screenSize.x <= 0f || screenSize.y <= 0f ||
                float.IsNaN(panelSize.x) || float.IsNaN(panelSize.y) || panelSize.x <= 0f || panelSize.y <= 0f)
                return None;

            var sx = panelSize.x / screenSize.x;
            var sy = panelSize.y / screenSize.y;
            var left = Mathf.Max(0f, safeArea.xMin) * sx;
            var right = Mathf.Max(0f, screenSize.x - safeArea.xMax) * sx;
            var bottom = Mathf.Max(0f, safeArea.yMin) * sy;
            var top = Mathf.Max(0f, screenSize.y - safeArea.yMax) * sy;
            return new SafeAreaInsets(left, top, right, bottom);
        }

        /// <summary>The four insets as <c>L.. T.. R.. B..</c>.</summary>
        public override string ToString() => $"L{Left:0.#} T{Top:0.#} R{Right:0.#} B{Bottom:0.#}";
    }
}
