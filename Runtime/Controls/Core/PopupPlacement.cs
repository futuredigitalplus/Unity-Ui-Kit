using System;

namespace FutureDigitalPlus.UIKit.Controls
{
    /// <summary>The side of an anchor a popup prefers to open on.</summary>
    public enum PopupSide
    {
        /// <summary>Below the anchor, left edges aligned (a dropdown list).</summary>
        Below,

        /// <summary>Above the anchor, left edges aligned.</summary>
        Above,

        /// <summary>To the right of the anchor, top edges aligned (a submenu).</summary>
        Right,

        /// <summary>To the left of the anchor, top edges aligned.</summary>
        Left,
    }

    /// <summary>Where a popup goes, from <see cref="PopupPlacement"/>.</summary>
    public readonly struct PopupPlacementResult
    {
        internal PopupPlacementResult(float x, float y, bool flippedX, bool flippedY)
        {
            X = x;
            Y = y;
            FlippedHorizontally = flippedX;
            FlippedVertically = flippedY;
        }

        /// <summary>Left edge of the popup.</summary>
        public float X { get; }

        /// <summary>Top edge of the popup.</summary>
        public float Y { get; }

        /// <summary>True when the popup opened to the left of its anchor point or side instead of the preferred right.</summary>
        public bool FlippedHorizontally { get; }

        /// <summary>True when the popup opened above its anchor point or side instead of the preferred below.</summary>
        public bool FlippedVertically { get; }
    }

    /// <summary>
    /// Pure math for placing a popup (context menu, dropdown list, submenu) so it stays inside its container: it
    /// flips to the other side of the anchor when it would overflow, and is clamped when it fits on neither side.
    /// </summary>
    public static class PopupPlacement
    {
        /// <summary>Places a popup of <paramref name="width"/> x <paramref name="height"/> whose corner is at a point (a pointer position).</summary>
        public static PopupPlacementResult AtPoint(float x, float y, float width, float height, float containerWidth,
            float containerHeight, float margin = 8f)
        {
            var flipX = false;
            var flipY = false;
            var left = x;
            var top = y;
            if (left + width + margin > containerWidth && x - width >= margin)
            {
                left = x - width;
                flipX = true;
            }

            if (top + height + margin > containerHeight && y - height >= margin)
            {
                top = y - height;
                flipY = true;
            }

            return new PopupPlacementResult(Clamp(left, width, containerWidth, margin), Clamp(top, height, containerHeight, margin), flipX, flipY);
        }

        /// <summary>Places a popup next to an anchor rectangle on the preferred side, flipping to the opposite side when it does not fit.</summary>
        public static PopupPlacementResult BesideRect(float anchorX, float anchorY, float anchorWidth, float anchorHeight,
            float width, float height, float containerWidth, float containerHeight, PopupSide preferred = PopupSide.Below,
            float margin = 8f)
        {
            float left, top;
            var flipX = false;
            var flipY = false;
            switch (preferred)
            {
                case PopupSide.Above:
                case PopupSide.Below:
                    left = anchorX;
                    if (left + width + margin > containerWidth) left = anchorX + anchorWidth - width; // right-align instead
                    var below = preferred == PopupSide.Below;
                    top = below ? anchorY + anchorHeight : anchorY - height;
                    var overflows = below ? top + height + margin > containerHeight : top < margin;
                    if (overflows)
                    {
                        var otherTop = below ? anchorY - height : anchorY + anchorHeight;
                        var otherFits = below ? otherTop >= margin : otherTop + height + margin <= containerHeight;
                        if (otherFits)
                        {
                            top = otherTop;
                            flipY = true;
                        }
                    }

                    break;
                default:
                    top = anchorY;
                    var right = preferred == PopupSide.Right;
                    left = right ? anchorX + anchorWidth : anchorX - width;
                    var horizontalOverflow = right ? left + width + margin > containerWidth : left < margin;
                    if (horizontalOverflow)
                    {
                        var otherLeft = right ? anchorX - width : anchorX + anchorWidth;
                        var otherFits = right ? otherLeft >= margin : otherLeft + width + margin <= containerWidth;
                        if (otherFits)
                        {
                            left = otherLeft;
                            flipX = true;
                        }
                    }

                    break;
            }

            return new PopupPlacementResult(Clamp(left, width, containerWidth, margin), Clamp(top, height, containerHeight, margin), flipX, flipY);
        }

        private static float Clamp(float position, float size, float container, float margin)
        {
            var max = container - size - margin;
            // NaN: the container has not been laid out yet.
            if (float.IsNaN(position) || float.IsNaN(max) || max < margin) return margin;
            return Math.Max(margin, Math.Min(max, position));
        }
    }
}
