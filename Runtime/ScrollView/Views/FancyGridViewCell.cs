/*
 * FancyScrollView (https://github.com/setchi/FancyScrollView)
 * Copyright (c) 2020 setchi
 * Licensed under MIT (https://github.com/setchi/FancyScrollView/blob/master/LICENSE)
 */

// FutureDigitalPlus: from Sources/Runtime/GridView/FancyGridViewCell.cs. The cell moves with style.translate from the viewport's
// top-left corner (upstream: transform.localPosition from the viewport's centre, y up); the row is still centred across the axis.
// Namespace FutureDigitalPlus.UIKit.ScrollView; doc comments translated to English.

using UnityEngine.UIElements;

namespace FutureDigitalPlus.UIKit.ScrollView
{
    /// <summary>
    /// Base class of the cells of a <see cref="FancyGridView{TItemData, TContext}"/>. It moves itself to its row and column; make it the
    /// grid's <c>GridCellSize</c>. Rows are centred across the scroll axis.
    /// Use <see cref="FancyGridViewCell{TItemData}"/> when the grid has no own context.
    /// </summary>
    /// <typeparam name="TItemData">The item data type.</typeparam>
    /// <typeparam name="TContext">The context type.</typeparam>
    public abstract class FancyGridViewCell<TItemData, TContext> : FancyScrollRectCell<TItemData, TContext>
        where TContext : class, IFancyGridViewContext, new()
    {
        /// <inheritdoc/>
        protected override void UpdatePosition(float normalizedPosition, float localPosition)
        {
            var cellSize = Context.GetCellSize();
            var spacing = Context.GetStartAxisSpacing();
            var groupCount = Context.GetGroupCount();

            var indexInGroup = Index % groupCount;
            var positionInGroup = (cellSize + spacing) * (indexInGroup - (groupCount - 1) * 0.5f);

            var viewport = Context.GetViewportSize();
            var horizontal = Context.ScrollDirection == ScrollDirection.Horizontal;
            var crossAxis = (horizontal ? viewport.y : viewport.x) * 0.5f + positionInGroup - cellSize * 0.5f;
            var mainAxis = MainAxisOffset(localPosition);

            style.translate = horizontal
                ? new Translate(mainAxis, crossAxis)
                : new Translate(crossAxis, mainAxis);
        }
    }

    /// <summary>Base class of the cells of a <see cref="FancyGridView{TItemData}"/>.</summary>
    /// <typeparam name="TItemData">The item data type.</typeparam>
    /// <seealso cref="FancyGridViewCell{TItemData, TContext}"/>
    public abstract class FancyGridViewCell<TItemData> : FancyGridViewCell<TItemData, FancyGridViewContext>
    {
        /// <inheritdoc/>
        public sealed override void SetContext(FancyGridViewContext context) => base.SetContext(context);
    }
}
