/*
 * FancyScrollView (https://github.com/setchi/FancyScrollView)
 * Copyright (c) 2020 setchi
 * Licensed under MIT (https://github.com/setchi/FancyScrollView/blob/master/LICENSE)
 */

// FutureDigitalPlus: from Sources/Runtime/ScrollRect/FancyScrollRect.cs. A UI Toolkit element that owns its FancyScroller (upstream:
// [RequireComponent(typeof(Scroller))]) with the view itself as the viewport, and is public as Scroller. The serialized fields became
// properties that relayout at once. New: a change of the view's size relayouts (GeometryChangedEvent), since UI Toolkit lays out after
// construction; a change across the scroll axis only moves the cells and keeps a fling or animation running. The viewport is the
// view's padding box, where the cells are. The arithmetic moved to ScrollRectLayout (Core). OnValidate's checks (no loop, no snap, no unrestricted movement) run
// whenever the scroller is refreshed and log the same errors. Namespace FutureDigitalPlus.UIKit.ScrollView; doc comments translated to English.

using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace FutureDigitalPlus.UIKit.ScrollView
{
    /// <summary>
    /// Base class of a ScrollRect-style list: cells of a fixed size (<see cref="CellSize"/>) in a row or column, with spacing and padding,
    /// scrolled by drag, wheel and an optional scrollbar, with inertia and a rubber band. Only enough cells to cover the viewport exist.
    /// No loop and no snapping (use <see cref="FancyScrollView{TItemData, TContext}"/> for those).
    /// Use <see cref="FancyScrollRect{TItemData}"/> when no own context is needed.
    /// </summary>
    /// <typeparam name="TItemData">The item data type.</typeparam>
    /// <typeparam name="TContext">The context type.</typeparam>
    public abstract class FancyScrollRect<TItemData, TContext> : FancyScrollView<TItemData, TContext>
        where TContext : class, IFancyScrollRectContext, new()
    {
        /// <summary>USS class of a ScrollRect-style view.</summary>
        public new const string UssClassName = "fdp-fancy-scroll-rect";

        private readonly ScrollRectLayout _layout = new ScrollRectLayout();

        /// <summary>Extra cells kept beyond each edge before they are reused (0: reused as soon as hidden).</summary>
        protected float reuseCellMarginCount;

        /// <summary>Space before the first cell.</summary>
        protected float paddingHead;

        /// <summary>Space after the last cell.</summary>
        protected float paddingTail;

        /// <summary>Gap between cells along the scroll axis.</summary>
        protected float spacing;

        /// <summary>Creates the view and its scroller (elastic, no snap) along <paramref name="direction"/>.</summary>
        protected FancyScrollRect(ScrollDirection direction = ScrollDirection.Vertical)
        {
            AddToClassList(UssClassName);
            Scroller = new FancyScroller(this, direction)
            {
                SnapEnabled = false,
                MovementType = MovementType.Elastic,
            };
            RegisterCallback<GeometryChangedEvent>(OnGeometryChanged);
        }

        /// <summary>
        /// The scroller (direction, elasticity, inertia, scrollbar, and the position in scroller units). Convert a cell position with
        /// <see cref="ToScrollerPosition(float)"/> before you set <see cref="FancyScroller.Position"/>.
        /// </summary>
        public FancyScroller Scroller { get; }

        /// <summary>Cell length along the scroll axis, in panel units. Make the cells this long.</summary>
        protected abstract float CellSize { get; }

        /// <summary>False when all items fit the viewport.</summary>
        protected virtual bool Scrollable => Layout.MaxScrollPosition > 0f;

        /// <summary>Extra cells kept beyond each edge before they are reused (default 0).</summary>
        public float ReuseCellMarginCount
        {
            get => reuseCellMarginCount;
            set
            {
                reuseCellMarginCount = Mathf.Max(0f, value);
                if (initialized) Relayout();
            }
        }

        /// <summary>Space before the first cell, in panel units (default 0).</summary>
        public float PaddingHead
        {
            get => paddingHead;
            set
            {
                paddingHead = value;
                if (initialized) Relayout();
            }
        }

        /// <summary>Space after the last cell, in panel units (default 0).</summary>
        public float PaddingTail
        {
            get => paddingTail;
            set
            {
                paddingTail = value;
                if (initialized) Relayout();
            }
        }

        /// <summary>Gap between cells along the scroll axis, in panel units (default 0).</summary>
        public float Spacing
        {
            get => spacing;
            set
            {
                spacing = value;
                if (initialized) Relayout();
            }
        }

        /// <summary>Always false: a ScrollRect-style view does not loop. Setting true logs an error.</summary>
        public override bool Loop
        {
            get => false;
            set
            {
                if (value) Debug.LogError("Loop is currently not supported in FancyScrollRect.");
            }
        }

        private ScrollRectLayout Layout
        {
            get
            {
                _layout.CellSize = CellSize;
                _layout.Spacing = spacing;
                _layout.PaddingHead = paddingHead;
                _layout.PaddingTail = paddingTail;
                _layout.ReuseCellMarginCount = reuseCellMarginCount;
                _layout.ViewportSize = Scroller.ViewportSize;
                _layout.ItemCount = ItemsSource.Count;
                return _layout;
            }
        }

        private float ViewportLength => Layout.ViewportLength;

        private float PaddingHeadLength => Layout.PaddingHeadLength;

        /// <inheritdoc/>
        protected override void Initialize()
        {
            base.Initialize();

            Context.ScrollDirection = Scroller.ScrollDirection;
            Context.CalculateScrollSize = () =>
            {
                var layout = Layout;
                return (layout.ScrollSize, layout.ReuseMargin);
            };
            Context.GetViewportSize = () => FancyScroller.PaddingBoxSize(this);
            Context.GetScrollAxisCellSize = () => CellSize;

            AdjustCellIntervalAndScrollOffset();
            Scroller.ValueChanged += OnScrollerValueChanged;
        }

        private void OnGeometryChanged(GeometryChangedEvent evt)
        {
            if (!initialized) return;

            var horizontal = Scroller.ScrollDirection == ScrollDirection.Horizontal;
            var oldLength = horizontal ? evt.oldRect.width : evt.oldRect.height;
            var newLength = horizontal ? evt.newRect.width : evt.newRect.height;
            if (oldLength != newLength) Relayout();
            else if (evt.oldRect.size != evt.newRect.size) base.Relayout(); // across the axis only: move the cells, keep the scroll running
        }

        /// <summary>Called when the scroller moves: places the cells and shrinks the scrollbar past the ends.</summary>
        /// <param name="p">The scroller position.</param>
        private void OnScrollerValueChanged(float p)
        {
            base.UpdatePosition(ToFancyScrollViewPosition(Scrollable ? p : 0f));

            if (Scroller.Scrollbar != null)
            {
                if (p > ItemsSource.Count - 1)
                {
                    ShrinkScrollbar(p - (ItemsSource.Count - 1));
                }
                else if (p < 0f)
                {
                    ShrinkScrollbar(-p);
                }
            }
        }

        /// <summary>Shrinks the scrollbar thumb by how far the content is pulled past an end.</summary>
        /// <param name="offset">Distance past the end, in scroller units.</param>
        private void ShrinkScrollbar(float offset)
        {
            Scroller.Scrollbar.Adjust(Layout.ShrunkScrollbarSize(offset));
        }

        /// <inheritdoc/>
        protected override void Refresh()
        {
            AdjustCellIntervalAndScrollOffset();
            RefreshScroller();
            base.Refresh();
        }

        /// <inheritdoc/>
        protected override void Relayout()
        {
            AdjustCellIntervalAndScrollOffset();
            RefreshScroller();
            base.Relayout();
        }

        /// <summary>Updates the scroller from the layout: draggable, sensitivity, position and scrollbar.</summary>
        protected void RefreshScroller()
        {
            CheckScrollerSettings();

            Scroller.Draggable = Scrollable;
            Scroller.ScrollSensitivity = ToScrollerPosition(ViewportLength - PaddingHeadLength);
            Scroller.Position = ToScrollerPosition(currentPosition);

            if (Scroller.Scrollbar != null)
            {
                Scroller.Scrollbar.style.display = Scrollable ? DisplayStyle.Flex : DisplayStyle.None;
                UpdateScrollbarSize(ViewportLength);
            }
        }

        /// <inheritdoc/>
        protected override void UpdateContents(IList<TItemData> items)
        {
            AdjustCellIntervalAndScrollOffset();
            base.UpdateContents(items);

            Scroller.SetTotalCount(items.Count);
            RefreshScroller();
        }

        /// <summary>Scrolls to cell position <paramref name="position"/> (0 is the first item at the start of the viewport).</summary>
        /// <param name="position">The cell position.</param>
        protected new void UpdatePosition(float position)
        {
            Scroller.Position = ToScrollerPosition(position, 0.5f);
        }

        /// <summary>Jumps to the item at <paramref name="itemIndex"/>.</summary>
        /// <param name="itemIndex">The item index.</param>
        /// <param name="alignment">Where the item ends up in the viewport: 0 start, 0.5 centre, 1 end.</param>
        protected virtual void JumpTo(int itemIndex, float alignment = 0.5f)
        {
            Scroller.Position = ToScrollerPosition(itemIndex, alignment);
        }

        /// <summary>Scrolls to the item at <paramref name="index"/> in <paramref name="duration"/> seconds.</summary>
        /// <param name="index">The item index.</param>
        /// <param name="duration">Seconds.</param>
        /// <param name="alignment">Where the item ends up in the viewport: 0 start, 0.5 centre, 1 end.</param>
        /// <param name="onComplete">Called when the move ends.</param>
        protected virtual void ScrollTo(int index, float duration, float alignment = 0.5f, Action onComplete = null)
        {
            Scroller.ScrollTo(ToScrollerPosition(index, alignment), duration, onComplete);
        }

        /// <summary>Scrolls to the item at <paramref name="index"/> in <paramref name="duration"/> seconds with <paramref name="easing"/>.</summary>
        /// <param name="index">The item index.</param>
        /// <param name="duration">Seconds.</param>
        /// <param name="easing">The easing curve.</param>
        /// <param name="alignment">Where the item ends up in the viewport: 0 start, 0.5 centre, 1 end.</param>
        /// <param name="onComplete">Called when the move ends.</param>
        protected virtual void ScrollTo(int index, float duration, Ease easing, float alignment = 0.5f, Action onComplete = null)
        {
            Scroller.ScrollTo(ToScrollerPosition(index, alignment), duration, easing, onComplete);
        }

        /// <summary>Sizes the scrollbar thumb for <paramref name="viewportLength"/> visible cells.</summary>
        /// <param name="viewportLength">The visible length in cells.</param>
        protected void UpdateScrollbarSize(float viewportLength)
        {
            Scroller.Scrollbar.Adjust(Layout.ScrollbarSize(viewportLength));
        }

        /// <summary>A scroller position as this view's cell position.</summary>
        /// <param name="position">The scroller position.</param>
        protected float ToFancyScrollViewPosition(float position) => Layout.ToFancyScrollViewPosition(position);

        /// <summary>A cell position as a scroller position.</summary>
        /// <param name="position">The cell position.</param>
        protected float ToScrollerPosition(float position) => Layout.ToScrollerPosition(position);

        /// <summary>The scroller position that shows <paramref name="position"/> at <paramref name="alignment"/> of the viewport.</summary>
        /// <param name="position">The cell position.</param>
        /// <param name="alignment">0 start, 0.5 centre, 1 end.</param>
        protected float ToScrollerPosition(float position, float alignment) => Layout.ToScrollerPosition(position, alignment);

        /// <summary>Sets <see cref="FancyScrollView{TItemData, TContext}.cellInterval"/> and <see cref="FancyScrollView{TItemData, TContext}.scrollOffset"/> from the cell size, spacing and viewport.</summary>
        protected void AdjustCellIntervalAndScrollOffset()
        {
            var layout = Layout;
            cellInterval = layout.CellInterval;
            scrollOffset = layout.ScrollOffset;
            Context.ScrollDirection = Scroller.ScrollDirection;
        }

        private void CheckScrollerSettings()
        {
            if (Scroller.SnapEnabled)
            {
                Scroller.SnapEnabled = false;
                Debug.LogError("Snap is currently not supported in FancyScrollRect.");
            }

            if (Scroller.MovementType == MovementType.Unrestricted)
            {
                Scroller.MovementType = MovementType.Elastic;
                Debug.LogError("MovementType.Unrestricted is currently not supported in FancyScrollRect.");
            }
        }
    }

    /// <summary>Base class of a ScrollRect-style list without an own context.</summary>
    /// <typeparam name="TItemData">The item data type.</typeparam>
    /// <seealso cref="FancyScrollRect{TItemData, TContext}"/>
    public abstract class FancyScrollRect<TItemData> : FancyScrollRect<TItemData, FancyScrollRectContext>
    {
        /// <summary>Creates the view along <paramref name="direction"/>.</summary>
        protected FancyScrollRect(ScrollDirection direction = ScrollDirection.Vertical) : base(direction) { }
    }
}
