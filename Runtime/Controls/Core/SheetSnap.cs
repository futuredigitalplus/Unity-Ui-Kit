using System;
using System.Collections.Generic;

namespace FutureDigitalPlus.UIKit.Controls
{
    /// <summary>Where a released bottom sheet should settle.</summary>
    public readonly struct SheetSnapResult
    {
        internal SheetSnapResult(int index, float target, bool dismiss)
        {
            Index = index;
            Target = target;
            Dismiss = dismiss;
        }

        /// <summary>Index of the snap point to settle at; -1 when <see cref="Dismiss"/> is true.</summary>
        public int Index { get; }

        /// <summary>The visible fraction to settle at (0 when dismissed).</summary>
        public float Target { get; }

        /// <summary>True when the sheet should close.</summary>
        public bool Dismiss { get; }
    }

    /// <summary>
    /// Snap logic for a bottom sheet that can rest at several heights. Positions are the visible fraction of the sheet
    /// (1 fully open, 0 hidden); velocity is in fractions per second, positive while opening. A fast flick moves to the
    /// next snap point in its direction, a slow release settles at the nearest one, and a sheet released low enough or
    /// flicked down from its lowest point is dismissed.
    /// </summary>
    public static class SheetSnap
    {
        /// <summary>Decides where a released sheet settles.</summary>
        /// <param name="visibleFraction">The visible fraction at release.</param>
        /// <param name="velocity">The release velocity in fractions per second (positive = opening).</param>
        /// <param name="snapPoints">Ascending visible fractions the sheet may rest at; at least one.</param>
        /// <param name="dismissable">False when the sheet may not close (then it never dismisses).</param>
        /// <param name="flingVelocity">Speed (fractions per second) from which a release counts as a flick (default 1.2).</param>
        /// <param name="projectionSeconds">How far ahead the release velocity carries the sheet for the nearest-point choice (default 0.12).</param>
        /// <param name="dismissBelowFactor">Release below this share of the lowest snap point dismisses (default 0.5).</param>
        public static SheetSnapResult Resolve(float visibleFraction, float velocity, IReadOnlyList<float> snapPoints,
            bool dismissable = true, float flingVelocity = 1.2f, float projectionSeconds = 0.12f, float dismissBelowFactor = 0.5f)
        {
            if (snapPoints == null || snapPoints.Count == 0) throw new ArgumentException("At least one snap point is required.", nameof(snapPoints));
            var lowest = snapPoints[0];
            const float epsilon = 0.01f;

            if (Math.Abs(velocity) >= flingVelocity)
            {
                if (velocity > 0f)
                {
                    for (var i = 0; i < snapPoints.Count; i++)
                        if (snapPoints[i] > visibleFraction + epsilon) return new SheetSnapResult(i, snapPoints[i], false);
                    return new SheetSnapResult(snapPoints.Count - 1, snapPoints[snapPoints.Count - 1], false);
                }

                for (var i = snapPoints.Count - 1; i >= 0; i--)
                    if (snapPoints[i] < visibleFraction - epsilon) return new SheetSnapResult(i, snapPoints[i], false);
                return dismissable ? new SheetSnapResult(-1, 0f, true) : new SheetSnapResult(0, lowest, false);
            }

            var projected = visibleFraction + velocity * projectionSeconds;
            if (dismissable && projected < lowest * dismissBelowFactor) return new SheetSnapResult(-1, 0f, true);

            var best = 0;
            var bestDistance = float.MaxValue;
            for (var i = 0; i < snapPoints.Count; i++)
            {
                var distance = Math.Abs(snapPoints[i] - projected);
                if (distance >= bestDistance) continue;
                best = i;
                bestDistance = distance;
            }

            return new SheetSnapResult(best, snapPoints[best], false);
        }
    }
}
