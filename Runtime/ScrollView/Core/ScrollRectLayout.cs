/*
 * FancyScrollView (https://github.com/setchi/FancyScrollView)
 * Copyright (c) 2020 setchi
 * Licensed under MIT (https://github.com/setchi/FancyScrollView/blob/master/LICENSE)
 */

// FutureDigitalPlus: new file. The arithmetic of Sources/Runtime/ScrollRect/FancyScrollRect.cs (ScrollLength, ViewportLength,
// PaddingHeadLength, MaxScrollPosition, AdjustCellIntervalAndScrollOffset, CalculateScrollSize, the two position conversions and the
// scrollbar size), lifted out so it is testable without the engine. Upstream read the cell interval from a field that
// AdjustCellIntervalAndScrollOffset had to refresh first; here it is always computed from the inputs. Two guards: a cell size plus
// spacing of 0 or less counts as 1e-4 (upstream divided by zero), and ToScrollerPosition returns 0 when the content fits the
// viewport (upstream divided by a MaxScrollPosition of 0 or less; the value was never used because such a view is not scrollable).

using System;

namespace FutureDigitalPlus.UIKit.ScrollView
{
    /// <summary>
    /// The geometry of a ScrollRect-style fancy view: cells of a fixed size and spacing, padding at both ends, and the conversion between the
    /// scroller's position (0 at the first item, <c>ItemCount - 1</c> at the end of the content) and the view's cell position. Set the inputs, read the results.
    /// </summary>
    public sealed class ScrollRectLayout
    {
        /// <summary>Cell length along the scroll axis.</summary>
        public float CellSize { get; set; }

        /// <summary>Gap between cells along the scroll axis.</summary>
        public float Spacing { get; set; }

        /// <summary>Space before the first cell.</summary>
        public float PaddingHead { get; set; }

        /// <summary>Space after the last cell.</summary>
        public float PaddingTail { get; set; }

        /// <summary>Extra cells kept alive beyond each edge of the viewport before they are reused (0: reused as soon as they are hidden).</summary>
        public float ReuseCellMarginCount { get; set; }

        /// <summary>Viewport length along the scroll axis.</summary>
        public float ViewportSize { get; set; }

        /// <summary>The number of items.</summary>
        public int ItemCount { get; set; }

        private float Interval => Math.Max(CellSize + Spacing, 1e-4f);

        /// <summary>Distance from one cell to the next as a fraction of the scroll range, the view's <c>CellInterval</c>.</summary>
        public float CellInterval => Interval / (ViewportSize + Interval * (1f + ReuseCellMarginCount * 2f));

        /// <summary>Where the item at the scroll position sits in the scroll range, the view's <c>ScrollOffset</c>.</summary>
        public float ScrollOffset => CellInterval * (1f + ReuseCellMarginCount);

        /// <summary>Length of the scroll range in cells, less one.</summary>
        public float ScrollLength => 1f / Math.Max(CellInterval, 1e-2f) - 1f;

        /// <summary>Length of the viewport in cells.</summary>
        public float ViewportLength => ScrollLength - ReuseCellMarginCount * 2f;

        /// <summary>The head padding in cells, less half a spacing.</summary>
        public float PaddingHeadLength => (PaddingHead - Spacing * 0.5f) / Interval;

        /// <summary>The largest cell position; 0 or less when the whole content fits the viewport.</summary>
        public float MaxScrollPosition => ItemCount
            - ScrollLength
            + ReuseCellMarginCount * 2f
            + (PaddingHead + PaddingTail - Spacing) / Interval;

        /// <summary>False when the whole content fits the viewport.</summary>
        public bool Scrollable => MaxScrollPosition > 0f;

        /// <summary>Length of the scroll range in the units of <see cref="ViewportSize"/>, the viewport plus one cell plus both reuse margins.</summary>
        public float ScrollSize => ViewportSize + Interval + ReuseMargin * 2f;

        /// <summary>Length of one reuse margin in the units of <see cref="ViewportSize"/>.</summary>
        public float ReuseMargin => Interval * ReuseCellMarginCount;

        /// <summary>Length of the content in cells (at least 1).</summary>
        public float ContentLength => Math.Max(ItemCount + (PaddingHead + PaddingTail - Spacing) / Interval, 1);

        /// <summary>A scroller position as the view's cell position.</summary>
        public float ToFancyScrollViewPosition(float position)
        {
            return position / Math.Max(ItemCount - 1, 1) * MaxScrollPosition - PaddingHeadLength;
        }

        /// <summary>A cell position as a scroller position; 0 when the content fits the viewport.</summary>
        public float ToScrollerPosition(float position)
        {
            var max = MaxScrollPosition;
            return max > 0f ? (position + PaddingHeadLength) / max * Math.Max(ItemCount - 1, 1) : 0f;
        }

        /// <summary>The scroller position that shows item <paramref name="position"/> at <paramref name="alignment"/> of the viewport (0 start, 0.5 centre, 1 end), kept inside the content.</summary>
        public float ToScrollerPosition(float position, float alignment)
        {
            var offset = alignment * (ScrollLength - (1f + ReuseCellMarginCount * 2f))
                + (1f - alignment - 0.5f) * Spacing / Interval;
            return ToScrollerPosition(ScrollMath.Clamp(position - offset, 0f, MaxScrollPosition));
        }

        /// <summary>The scrollbar thumb size (0..1) for a visible length of <paramref name="viewportLength"/> cells; 1 when not scrollable.</summary>
        public float ScrollbarSize(float viewportLength) => Scrollable ? ScrollMath.Clamp01(viewportLength / ContentLength) : 1f;

        /// <summary>The thumb size while the content is pulled <paramref name="overscroll"/> (scroller units) past an end: it shrinks like a native scrollbar.</summary>
        public float ShrunkScrollbarSize(float overscroll)
        {
            var visible = ViewportLength - PaddingHeadLength;
            var scale = 1f - ToFancyScrollViewPosition(overscroll) / visible;
            return ScrollbarSize(visible * scale);
        }
    }
}
