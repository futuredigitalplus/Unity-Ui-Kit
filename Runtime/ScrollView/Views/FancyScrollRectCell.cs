/*
 * FancyScrollView (https://github.com/setchi/FancyScrollView)
 * Copyright (c) 2020 setchi
 * Licensed under MIT (https://github.com/setchi/FancyScrollView/blob/master/LICENSE)
 */

// FutureDigitalPlus: from Sources/Runtime/ScrollRect/FancyScrollRectCell.cs. The cell moves with style.translate from the viewport's
// top-left corner (upstream: transform.localPosition from the viewport's centre, y up); localPosition keeps upstream's meaning.
// Namespace FutureDigitalPlus.UIKit.ScrollView; doc comments translated to English.

using UnityEngine.UIElements;

namespace FutureDigitalPlus.UIKit.ScrollView
{
    /// <summary>
    /// Base class of the cells of a <see cref="FancyScrollRect{TItemData, TContext}"/>. It moves itself to its slot; make it
    /// <c>CellSize</c> long along the scroll axis. Across the axis it starts at the viewport's edge: give it <c>width: 100%</c>
    /// (vertical) or <c>height: 100%</c> (horizontal) for a full row, or override <see cref="UpdatePosition(float, float)"/>.
    /// Use <see cref="FancyScrollRectCell{TItemData}"/> when the view has no own context.
    /// </summary>
    /// <typeparam name="TItemData">The item data type.</typeparam>
    /// <typeparam name="TContext">The context type.</typeparam>
    public abstract class FancyScrollRectCell<TItemData, TContext> : FancyCell<TItemData, TContext>
        where TContext : class, IFancyScrollRectContext, new()
    {
        /// <inheritdoc/>
        public override void UpdatePosition(float position)
        {
            var (scrollSize, reuseMargin) = Context.CalculateScrollSize();

            var normalizedPosition = (ScrollMath.Lerp(0f, scrollSize, position) - reuseMargin) / (scrollSize - reuseMargin * 2f);

            var start = 0.5f * scrollSize;
            var end = -start;

            UpdatePosition(normalizedPosition, ScrollMath.Lerp(start, end, position));
        }

        /// <summary>Moves the cell to its slot.</summary>
        /// <param name="normalizedPosition">
        /// The cell's place in the viewport, 0 at the start and 1 at the end. With a reuse margin it runs a little past 0..1.
        /// </param>
        /// <param name="localPosition">
        /// Distance of the cell's centre from the viewport's centre along the axis in panel units, positive towards the start (up or left).
        /// </param>
        protected virtual void UpdatePosition(float normalizedPosition, float localPosition)
        {
            var mainAxis = MainAxisOffset(localPosition);
            style.translate = Context.ScrollDirection == ScrollDirection.Horizontal
                ? new Translate(mainAxis, 0)
                : new Translate(0, mainAxis);
        }

        /// <summary>Where the cell's leading edge goes, from the viewport's leading edge, for <paramref name="localPosition"/>.</summary>
        /// <param name="localPosition">The centre offset passed to <see cref="UpdatePosition(float, float)"/>.</param>
        protected float MainAxisOffset(float localPosition)
        {
            var viewport = Context.GetViewportSize();
            var viewportLength = Context.ScrollDirection == ScrollDirection.Horizontal ? viewport.x : viewport.y;
            return viewportLength * 0.5f - localPosition - Context.GetScrollAxisCellSize() * 0.5f;
        }
    }

    /// <summary>Base class of the cells of a <see cref="FancyScrollRect{TItemData}"/>.</summary>
    /// <typeparam name="TItemData">The item data type.</typeparam>
    /// <seealso cref="FancyScrollRectCell{TItemData, TContext}"/>
    public abstract class FancyScrollRectCell<TItemData> : FancyScrollRectCell<TItemData, FancyScrollRectContext>
    {
        /// <inheritdoc/>
        public sealed override void SetContext(FancyScrollRectContext context) => base.SetContext(context);
    }
}
