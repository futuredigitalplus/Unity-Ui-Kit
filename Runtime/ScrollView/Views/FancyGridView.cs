/*
 * FancyScrollView (https://github.com/setchi/FancyScrollView)
 * Copyright (c) 2020 setchi
 * Licensed under MIT (https://github.com/setchi/FancyScrollView/blob/master/LICENSE)
 */

// FutureDigitalPlus: from Sources/Runtime/GridView/FancyGridView.cs. Setup takes a cell factory instead of a cell prefab, and creates
// groups with new TGroup() instead of a hidden template GameObject; DefaultCellGroup is no longer abstract, so Setup(createCell) works
// without declaring a group class. The serialized fields became properties; StartAxisCellCount is fixed once cells exist (upstream
// broke silently). Namespace FutureDigitalPlus.UIKit.ScrollView; doc comments translated to English.

using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace FutureDigitalPlus.UIKit.ScrollView
{
    /// <summary>
    /// Base class of a scrolling grid: rows of <see cref="StartAxisCellCount"/> cells (columns when it scrolls horizontally), each cell
    /// <see cref="GridCellSize"/>, built only for the rows in view. No loop and no snapping.
    /// Use <see cref="FancyGridView{TItemData}"/> when no own context is needed.
    /// </summary>
    /// <typeparam name="TItemData">The item data type.</typeparam>
    /// <typeparam name="TContext">The context type.</typeparam>
    public abstract class FancyGridView<TItemData, TContext> : FancyScrollRect<TItemData[], TContext>
        where TContext : class, IFancyGridViewContext, new()
    {
        /// <summary>The default group (one row or column of cells); derive from it to customise a group.</summary>
        protected class DefaultCellGroup : FancyCellGroup<TItemData, TContext> { }

        /// <summary>Gap between cells across the scroll axis.</summary>
        protected float startAxisSpacing;

        /// <summary>Cells per row (per column when horizontal).</summary>
        protected int startAxisCellCount = 4;

        /// <summary>Size of one cell in panel units.</summary>
        protected Vector2 cellSize = new Vector2(100f, 100f);

        private Func<FancyCell<TItemData[], TContext>> _createGroup;

        /// <summary>Creates the grid along <paramref name="direction"/>.</summary>
        protected FancyGridView(ScrollDirection direction = ScrollDirection.Vertical) : base(direction) { }

        /// <summary>Gap between cells across the scroll axis, in panel units (default 0). The gap along it is <c>Spacing</c>.</summary>
        public float StartAxisSpacing
        {
            get => startAxisSpacing;
            set
            {
                startAxisSpacing = value;
                if (initialized) Relayout();
            }
        }

        /// <summary>Cells per row, or per column when the grid scrolls horizontally (default 4). Set it before the first items.</summary>
        /// <exception cref="InvalidOperationException">The grid already shows items.</exception>
        public int StartAxisCellCount
        {
            get => startAxisCellCount;
            set
            {
                if (initialized && value != startAxisCellCount)
                {
                    throw new InvalidOperationException("StartAxisCellCount cannot change once the grid has shown items.");
                }

                startAxisCellCount = Math.Max(1, value);
            }
        }

        /// <summary>Size of one cell in panel units (default 100 x 100). Make the cells this size.</summary>
        public Vector2 GridCellSize
        {
            get => cellSize;
            set
            {
                cellSize = value;
                if (initialized) Relayout();
            }
        }

        /// <inheritdoc/>
        protected sealed override FancyCell<TItemData[], TContext> CreateCell()
        {
            if (_createGroup == null)
            {
                throw new InvalidOperationException(GetType().Name + ".SetupCellTemplate must call Setup with a cell factory.");
            }

            return _createGroup();
        }

        /// <inheritdoc/>
        protected override float CellSize => Scroller.ScrollDirection == ScrollDirection.Horizontal
            ? cellSize.x
            : cellSize.y;

        /// <summary>The number of items (not rows).</summary>
        public int DataCount { get; private set; }

        /// <inheritdoc/>
        protected override void Initialize()
        {
            base.Initialize();

            Debug.Assert(startAxisCellCount > 0);

            Context.ScrollDirection = Scroller.ScrollDirection;
            Context.GetGroupCount = () => startAxisCellCount;
            Context.GetStartAxisSpacing = () => startAxisSpacing;
            Context.GetCellSize = () => Scroller.ScrollDirection == ScrollDirection.Horizontal
                ? cellSize.y
                : cellSize.x;

            SetupCellTemplate();
        }

        /// <summary>
        /// Called once, just before the first cells are created. Call <see cref="Setup(Func{FancyCell{TItemData, TContext}})"/> here.
        /// </summary>
        /// <example>
        /// <code><![CDATA[
        /// public class MyGridView : FancyGridView<ItemData>
        /// {
        ///     protected override void SetupCellTemplate() => Setup(() => new MyCell());
        /// }
        /// ]]></code>
        /// </example>
        protected abstract void SetupCellTemplate();

        /// <summary>Sets the cell factory; rows use <see cref="DefaultCellGroup"/>.</summary>
        /// <param name="createCell">Creates one cell.</param>
        protected void Setup(Func<FancyCell<TItemData, TContext>> createCell) => Setup<DefaultCellGroup>(createCell);

        /// <summary>Sets the cell factory and the group type.</summary>
        /// <param name="createCell">Creates one cell.</param>
        /// <typeparam name="TGroup">The group type.</typeparam>
        protected virtual void Setup<TGroup>(Func<FancyCell<TItemData, TContext>> createCell)
            where TGroup : FancyCell<TItemData[], TContext>, new()
        {
            if (createCell == null) throw new ArgumentNullException(nameof(createCell));

            Context.CreateCell = () => createCell();
            _createGroup = () => new TGroup();
        }

        /// <summary>Shows <paramref name="items"/>.</summary>
        /// <param name="items">The items.</param>
        public virtual void UpdateContents(IList<TItemData> items)
        {
            DataCount = items.Count;

            var itemGroups = items
                .Select((item, index) => (item, index))
                .GroupBy(
                    x => x.index / startAxisCellCount,
                    x => x.item)
                .Select(group => group.ToArray())
                .ToArray();

            UpdateContents(itemGroups);
        }

        /// <summary>Jumps to the row of the item at <paramref name="itemIndex"/>.</summary>
        /// <param name="itemIndex">The item index.</param>
        /// <param name="alignment">Where the row ends up in the viewport: 0 start, 0.5 centre, 1 end.</param>
        protected override void JumpTo(int itemIndex, float alignment = 0.5f)
        {
            var groupIndex = itemIndex / startAxisCellCount;
            base.JumpTo(groupIndex, alignment);
        }

        /// <summary>Scrolls to the row of the item at <paramref name="itemIndex"/>.</summary>
        /// <param name="itemIndex">The item index.</param>
        /// <param name="duration">Seconds.</param>
        /// <param name="alignment">Where the row ends up in the viewport: 0 start, 0.5 centre, 1 end.</param>
        /// <param name="onComplete">Called when the move ends.</param>
        protected override void ScrollTo(int itemIndex, float duration, float alignment = 0.5f, Action onComplete = null)
        {
            var groupIndex = itemIndex / startAxisCellCount;
            base.ScrollTo(groupIndex, duration, alignment, onComplete);
        }

        /// <summary>Scrolls to the row of the item at <paramref name="itemIndex"/> with <paramref name="easing"/>.</summary>
        /// <param name="itemIndex">The item index.</param>
        /// <param name="duration">Seconds.</param>
        /// <param name="easing">The easing curve.</param>
        /// <param name="alignment">Where the row ends up in the viewport: 0 start, 0.5 centre, 1 end.</param>
        /// <param name="onComplete">Called when the move ends.</param>
        protected override void ScrollTo(int itemIndex, float duration, Ease easing, float alignment = 0.5f, Action onComplete = null)
        {
            var groupIndex = itemIndex / startAxisCellCount;
            base.ScrollTo(groupIndex, duration, easing, alignment, onComplete);
        }
    }

    /// <summary>Base class of a scrolling grid without an own context.</summary>
    /// <typeparam name="TItemData">The item data type.</typeparam>
    /// <seealso cref="FancyGridView{TItemData, TContext}"/>
    public abstract class FancyGridView<TItemData> : FancyGridView<TItemData, FancyGridViewContext>
    {
        /// <summary>Creates the grid along <paramref name="direction"/>.</summary>
        protected FancyGridView(ScrollDirection direction = ScrollDirection.Vertical) : base(direction) { }
    }
}
