using System;
using System.Collections.Generic;
using FutureDigitalPlus.UIKit.ScrollView;
using UnityEngine;
using UnityEngine.UIElements;

namespace FutureDigitalPlus.UIKit.Samples.ScrollViewBasics
{
    /// <summary>State the carousel shares with its cells: which item is selected and what a click does.</summary>
    public sealed class CarouselContext
    {
        /// <summary>The selected item (-1: none).</summary>
        public int SelectedIndex = -1;

        /// <summary>Called with the item index when a cell is clicked.</summary>
        public Action<int> CellClicked;
    }

    /// <summary>
    /// A horizontal carousel that loops and snaps: the item in the middle is large and opaque, the others shrink and fade. A click on a
    /// cell scrolls it to the middle. This is the pattern of the upstream "Basic", "FocusOn" and "InfiniteScroll" examples in one view.
    /// </summary>
    public sealed class Carousel : FancyScrollView<string, CarouselContext>
    {
        private readonly FancyScroller _scroller;
        private IList<string> _items = Array.Empty<string>();

        /// <summary>Creates the carousel; five cells across, looping.</summary>
        public Carousel()
        {
            style.flexGrow = 1;
            cellInterval = 0.2f;
            scrollOffset = 0.5f;
            loop = true;

            _scroller = new FancyScroller(this, ScrollDirection.Horizontal)
            {
                MovementType = MovementType.Unrestricted,
                ScrollSensitivity = 1f / cellInterval, // the content follows the finger: one viewport of drag moves five items
                SnapEase = Ease.OutCubic,
            };
            _scroller.ValueChanged += UpdatePosition;
            _scroller.SelectionChanged += UpdateSelection;

            Context.CellClicked = index => _scroller.ScrollTo(index, 0.35f, Ease.OutCubic);
        }

        /// <summary>Called with the item that comes to rest in the middle.</summary>
        public event Action<int> SelectionChanged;

        /// <summary>Shows <paramref name="items"/> with the first one selected.</summary>
        public void Show(IList<string> items)
        {
            _items = items;
            UpdateContents(items);
            _scroller.SetTotalCount(items.Count);
            if (items.Count > 0) _scroller.JumpTo(0);
        }

        /// <summary>Moves the item at <paramref name="index"/> to the middle.</summary>
        public void Select(int index) => _scroller.ScrollTo(index, 0.35f, Ease.OutCubic);

        /// <inheritdoc/>
        protected override FancyCell<string, CarouselContext> CreateCell() => new CarouselCell();

        private void UpdateSelection(int index)
        {
            if (Context.SelectedIndex == index) return;

            Context.SelectedIndex = index;
            Refresh();
            if (index >= 0 && index < _items.Count) SelectionChanged?.Invoke(index);
        }
    }

    /// <summary>One card of the <see cref="Carousel"/>. It places itself in percent of the viewport, so it needs no relayout on resize.</summary>
    public sealed class CarouselCell : FancyCell<string, CarouselContext>
    {
        private readonly Label _label = new Label();

        /// <summary>Builds the card.</summary>
        public CarouselCell()
        {
            style.width = 100;
            style.height = 130;
            style.top = Length.Percent(50);
            style.justifyContent = Justify.Center;
            style.alignItems = Align.Center;
            style.backgroundColor = new Color(0.20f, 0.24f, 0.32f);
            SetRadius(this, 16);
            _label.style.fontSize = 28;
            _label.style.color = Color.white;
            Add(_label);

            this.AddManipulator(new Clickable(() => Context.CellClicked?.Invoke(Index)));
        }

        /// <inheritdoc/>
        public override void UpdateContent(string itemData)
        {
            _label.text = itemData;
            var selected = Context.SelectedIndex == Index;
            style.backgroundColor = selected ? new Color(0.24f, 0.47f, 0.96f) : new Color(0.20f, 0.24f, 0.32f);
        }

        /// <inheritdoc/>
        public override void UpdatePosition(float position)
        {
            // position runs 0..1 across the viewport; 0.5 is the middle.
            var fromCentre = Mathf.Abs(position - 0.5f) * 2f;
            style.left = Length.Percent(position * 100f);
            style.translate = new Translate(Length.Percent(-50), Length.Percent(-50));
            var scale = Mathf.Lerp(1f, 0.6f, fromCentre);
            style.scale = new Scale(new Vector2(scale, scale));
            style.opacity = Mathf.Lerp(1f, 0.25f, fromCentre);
        }

        internal static void SetRadius(VisualElement element, float radius)
        {
            element.style.borderTopLeftRadius = radius;
            element.style.borderTopRightRadius = radius;
            element.style.borderBottomLeftRadius = radius;
            element.style.borderBottomRightRadius = radius;
        }
    }
}
