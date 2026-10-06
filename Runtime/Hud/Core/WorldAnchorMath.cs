using System;

namespace FutureDigitalPlus.UIKit.Hud
{
    /// <summary>Where a world point lies relative to the screen rectangle.</summary>
    public enum AnchorVisibility
    {
        /// <summary>Inside the rectangle (and in front of the camera).</summary>
        OnScreen,

        /// <summary>In front of the camera but outside the rectangle.</summary>
        OffScreen,

        /// <summary>Behind the camera.</summary>
        Behind,
    }

    /// <summary>What to do with an anchor whose point is not on screen.</summary>
    public enum OffScreenMode
    {
        /// <summary>Hide the anchor.</summary>
        Hide,

        /// <summary>Pin the anchor to the edge of the rectangle, toward the point.</summary>
        Clamp,
    }

    /// <summary>The result of <see cref="WorldAnchorMath.Resolve"/>.</summary>
    public readonly struct AnchorResult
    {
        internal AnchorResult(AnchorVisibility visibility, bool show, float x, float y, float arrowAngle)
        {
            Visibility = visibility;
            Show = show;
            X = x;
            Y = y;
            ArrowAngleDegrees = arrowAngle;
        }

        /// <summary>Where the point lies.</summary>
        public AnchorVisibility Visibility { get; }

        /// <summary>True when the anchor should be shown (on screen, or off screen with <see cref="OffScreenMode.Clamp"/>).</summary>
        public bool Show { get; }

        /// <summary>Horizontal position to place the anchor at (panel space); clamped to the rectangle when off screen.</summary>
        public float X { get; }

        /// <summary>Vertical position to place the anchor at (panel space); clamped to the rectangle when off screen.</summary>
        public float Y { get; }

        /// <summary>
        /// Direction from the rectangle's centre toward the point, in degrees: 0 points right, 90 points down
        /// (panel space, y grows downwards), so it can be used directly as a clockwise element rotation.
        /// </summary>
        public float ArrowAngleDegrees { get; }
    }

    /// <summary>
    /// Pure math for HUD elements that follow a point in the 3D world (name plates, objective markers, off-screen
    /// arrows): clamp to the screen edge, hide, and compute the arrow direction. The projection itself (camera to
    /// panel space) is done by the view; this takes its result.
    /// </summary>
    public static class WorldAnchorMath
    {
        /// <summary>Decides where an anchor goes.</summary>
        /// <param name="x">Projected horizontal position in panel space.</param>
        /// <param name="y">Projected vertical position in panel space (grows downwards).</param>
        /// <param name="depth">Distance in front of the camera; negative or zero means behind it.</param>
        /// <param name="left">Left edge of the rectangle the anchor may use (for example the safe area).</param>
        /// <param name="top">Top edge of the rectangle.</param>
        /// <param name="right">Right edge of the rectangle.</param>
        /// <param name="bottom">Bottom edge of the rectangle.</param>
        /// <param name="margin">Distance kept between a clamped anchor and the rectangle's edge.</param>
        /// <param name="mode">Whether an off-screen anchor is hidden or pinned to the edge.</param>
        public static AnchorResult Resolve(float x, float y, float depth, float left, float top, float right, float bottom,
            float margin, OffScreenMode mode)
        {
            var cx = (left + right) * 0.5f;
            var cy = (top + bottom) * 0.5f;
            // A point that is not finite (a destroyed target, a projection through the camera plane) has no place on the screen: hide it at the centre.
            if (float.IsNaN(x) || float.IsInfinity(x) || float.IsNaN(y) || float.IsInfinity(y) || float.IsNaN(depth))
                return new AnchorResult(AnchorVisibility.OffScreen, false, cx, cy, 0f);
            var dx = x - cx;
            var dy = y - cy;
            var behind = depth <= 0f;

            if (behind)
            {
                // The projection of a point behind the camera is mirrored: flip it so the arrow points the way to turn.
                dx = -dx;
                dy = -dy;
                if (Math.Abs(dx) < 1e-4f && Math.Abs(dy) < 1e-4f) dy = 1f;
            }

            var angle = (float)(Math.Atan2(dy, dx) * (180d / Math.PI));

            if (!behind && x >= left + margin && x <= right - margin && y >= top + margin && y <= bottom - margin)
                return new AnchorResult(AnchorVisibility.OnScreen, true, x, y, angle);

            var halfW = Math.Max(0f, (right - left) * 0.5f - margin);
            var halfH = Math.Max(0f, (bottom - top) * 0.5f - margin);
            var scale = float.MaxValue;
            if (Math.Abs(dx) > 1e-4f) scale = Math.Min(scale, halfW / Math.Abs(dx));
            if (Math.Abs(dy) > 1e-4f) scale = Math.Min(scale, halfH / Math.Abs(dy));

            if (scale == float.MaxValue) scale = 0f;
            var px = cx + dx * scale;
            var py = cy + dy * scale;
            return new AnchorResult(behind ? AnchorVisibility.Behind : AnchorVisibility.OffScreen,
                mode == OffScreenMode.Clamp, px, py, angle);
        }
    }
}
