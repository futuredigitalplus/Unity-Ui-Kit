/*
 * FancyScrollView (https://github.com/setchi/FancyScrollView)
 * Copyright (c) 2020 setchi
 * Licensed under MIT (https://github.com/setchi/FancyScrollView/blob/master/LICENSE)
 */

// FutureDigitalPlus: from Sources/Runtime/Core/FancyScrollView.cs. A UI Toolkit VisualElement instead of a MonoBehaviour: the serialized
// fields became the properties CellInterval, ScrollOffset and Loop (a change relayouts at once, replacing the editor-only LateUpdate
// check), the cell container is created by the view (CellContainer), and the abstract CellPrefab became CreateCell. The position
// arithmetic moved to CellLayout (Core), unchanged. Namespace FutureDigitalPlus.UIKit.ScrollView; doc comments translated to English.

using System;
using System.Collections.Generic;
using UnityEngine.UIElements;

namespace FutureDigitalPlus.UIKit.ScrollView
{
    /// <summary>
    /// Base class of a scroll view whose cells place themselves from a normalized position (carousels, wheels, cover flows), with
    /// infinite scrolling (<see cref="Loop"/>) and snapping. It shows only enough cells to cover the viewport and reuses them while it scrolls.
    /// Drive it with a <see cref="FancyScroller"/>: subscribe <see cref="UpdatePosition(float)"/> to <see cref="FancyScroller.ValueChanged"/>.
    /// Use <see cref="FancyScrollView{TItemData}"/> when no <see cref="Context"/> is needed.
    /// </summary>
    /// <typeparam name="TItemData">The item data type.</typeparam>
    /// <typeparam name="TContext">The type of <see cref="Context"/>.</typeparam>
    public abstract class FancyScrollView<TItemData, TContext> : VisualElement where TContext : class, new()
    {
        /// <summary>USS class of the view.</summary>
        public const string UssClassName = "fdp-fancy-scroll";

        /// <summary>USS class of <see cref="CellContainer"/>.</summary>
        public const string CellContainerUssClassName = UssClassName + "__cells";

        private readonly List<FancyCell<TItemData, TContext>> _pool = new List<FancyCell<TItemData, TContext>>();

        /// <summary>Distance between cells as a fraction of the viewport (0.01..1).</summary>
        protected float cellInterval = 0.2f;

        /// <summary>Where the item at the scroll position sits in the viewport (0..1); at 0.5 and position 0 the first item is in the middle.</summary>
        protected float scrollOffset = 0.5f;

        /// <summary>True: the last item is followed by the first one and the first is preceded by the last one.</summary>
        protected bool loop;

        /// <summary>True once <see cref="Initialize"/> has run (just before the first cells are created).</summary>
        protected bool initialized;

        /// <summary>The current scroll position.</summary>
        protected float currentPosition;

        /// <summary>Creates the view with an empty <see cref="CellContainer"/>. The view clips its cells (<c>overflow: hidden</c>).</summary>
        protected FancyScrollView()
        {
            AddToClassList(UssClassName);
            style.overflow = Overflow.Hidden;

            CellContainer = new VisualElement { name = "fancy-cells", pickingMode = PickingMode.Ignore };
            CellContainer.AddToClassList(CellContainerUssClassName);
            CellContainer.style.position = Position.Absolute;
            CellContainer.style.left = 0;
            CellContainer.style.top = 0;
            CellContainer.style.right = 0;
            CellContainer.style.bottom = 0;
            Add(CellContainer);
        }

        /// <summary>The parent of the cells. It fills the view; elements added to the view later draw on top of the cells.</summary>
        public VisualElement CellContainer { get; }

        /// <summary>Distance between cells as a fraction of the viewport, 0.01..1 (default 0.2: five cells across).</summary>
        public float CellInterval
        {
            get => cellInterval;
            set
            {
                cellInterval = ScrollMath.Clamp(value, 1e-2f, 1f);
                if (initialized) Relayout();
            }
        }

        /// <summary>Where the item at the scroll position sits in the viewport, 0..1 (default 0.5, the middle).</summary>
        public float ScrollOffset
        {
            get => scrollOffset;
            set
            {
                scrollOffset = ScrollMath.Clamp01(value);
                if (initialized) Relayout();
            }
        }

        /// <summary>True: cells wrap around, for infinite scrolling (pair it with <see cref="MovementType.Unrestricted"/>).</summary>
        public virtual bool Loop
        {
            get => loop;
            set
            {
                loop = value;
                if (initialized) Relayout();
            }
        }

        /// <summary>Creates one cell. Called as the view needs more cells to cover the viewport.</summary>
        protected abstract FancyCell<TItemData, TContext> CreateCell();

        /// <summary>The items.</summary>
        protected IList<TItemData> ItemsSource { get; set; } = new List<TItemData>();

        /// <summary>One instance shared by the view and all its cells, to pass information and keep state.</summary>
        protected TContext Context { get; } = new TContext();

        /// <summary>Called once, just before the first cells are created.</summary>
        protected virtual void Initialize() { }

        /// <summary>Shows <paramref name="itemsSource"/>.</summary>
        /// <param name="itemsSource">The items.</param>
        protected virtual void UpdateContents(IList<TItemData> itemsSource)
        {
            ItemsSource = itemsSource;
            Refresh();
        }

        /// <summary>Places every cell again.</summary>
        protected virtual void Relayout() => UpdatePosition(currentPosition, false);

        /// <summary>Places every cell again and refills them all.</summary>
        protected virtual void Refresh() => UpdatePosition(currentPosition, true);

        /// <summary>Scrolls to <paramref name="position"/> (0 is the first item).</summary>
        /// <param name="position">The scroll position.</param>
        protected virtual void UpdatePosition(float position) => UpdatePosition(position, false);

        private void UpdatePosition(float position, bool forceRefresh)
        {
            if (!initialized)
            {
                Initialize();
                initialized = true;
            }

            currentPosition = position;

            CellLayout.First(position, scrollOffset, cellInterval, out var firstIndex, out var firstPosition);

            if (CellLayout.NeedsMoreCells(firstPosition, _pool.Count, cellInterval))
            {
                ResizePool(firstPosition);
            }

            UpdateCells(firstPosition, firstIndex, forceRefresh);
        }

        private void ResizePool(float firstPosition)
        {
            var addCount = CellLayout.PoolSize(firstPosition, cellInterval) - _pool.Count;
            for (var i = 0; i < addCount; i++)
            {
                var cell = CreateCell();
                if (cell == null)
                {
                    throw new InvalidOperationException(string.Format(
                        "{0}.CreateCell returned null; it must return a new FancyCell<{1}, {2}>.",
                        GetType().Name, typeof(TItemData).FullName, typeof(TContext).FullName));
                }

                cell.style.position = Position.Absolute;
                CellContainer.Add(cell);
                cell.SetContext(Context);
                cell.Initialize();
                cell.SetVisible(false);
                _pool.Add(cell);
            }
        }

        private void UpdateCells(float firstPosition, int firstIndex, bool forceRefresh)
        {
            for (var i = 0; i < _pool.Count; i++)
            {
                var index = firstIndex + i;
                var position = firstPosition + i * cellInterval;
                var cell = _pool[CellLayout.PoolIndex(firstIndex, i, _pool.Count)];

                index = CellLayout.ItemIndex(index, ItemsSource.Count, loop);

                if (!CellLayout.IsShown(index, position, ItemsSource.Count))
                {
                    cell.SetVisible(false);
                    continue;
                }

                if (forceRefresh || cell.Index != index || !cell.IsVisible)
                {
                    cell.Index = index;
                    cell.SetVisible(true);
                    cell.UpdateContent(ItemsSource[index]);
                }

                cell.UpdatePosition(position);
            }
        }
    }

    /// <summary>Base class of a <see cref="FancyScrollView{TItemData, TContext}"/> without a context.</summary>
    /// <typeparam name="TItemData">The item data type.</typeparam>
    /// <seealso cref="FancyScrollView{TItemData, TContext}"/>
    public abstract class FancyScrollView<TItemData> : FancyScrollView<TItemData, NullContext> { }
}
