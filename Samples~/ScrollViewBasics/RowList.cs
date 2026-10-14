using System.Collections.Generic;
using FutureDigitalPlus.UIKit.ScrollView;
using UnityEngine;
using UnityEngine.UIElements;

namespace FutureDigitalPlus.UIKit.Samples.ScrollViewBasics
{
    /// <summary>
    /// A vertical list of rows of a fixed height with spacing, padding and a scrollbar, built only for the rows in view
    /// (the upstream "ScrollRect" example). Public wrappers expose the protected scroll API.
    /// </summary>
    public sealed class RowList : FancyScrollRect<string>
    {
        /// <summary>Height of a row in panel units.</summary>
        public const float RowHeight = 48f;

        /// <summary>Creates the list with a scrollbar on its right edge.</summary>
        public RowList()
        {
            style.flexGrow = 1;
            Spacing = 4f;
            PaddingHead = 8f;
            PaddingTail = 8f;

            var bar = new Scroller(0f, 1f, null, SliderDirection.Vertical);
            bar.style.position = Position.Absolute;
            bar.style.top = 0;
            bar.style.bottom = 0;
            bar.style.right = 0;
            Add(bar);
            Scroller.Scrollbar = bar;
        }

        /// <inheritdoc/>
        protected override float CellSize => RowHeight;

        /// <summary>Shows <paramref name="items"/>.</summary>
        public void Show(IList<string> items) => UpdateContents(items);

        /// <summary>Animates the item at <paramref name="index"/> to <paramref name="alignment"/> (0 top, 0.5 middle, 1 bottom).</summary>
        public void ScrollToItem(int index, float alignment) => ScrollTo(index, 0.4f, Ease.OutCubic, alignment);

        /// <summary>Jumps to the item at <paramref name="index"/>, centred.</summary>
        public void JumpToItem(int index) => JumpTo(index, 0.5f);

        /// <inheritdoc/>
        protected override FancyCell<string, FancyScrollRectContext> CreateCell() => new Row();

        private sealed class Row : FancyScrollRectCell<string>
        {
            private readonly Label _label = new Label();

            public Row()
            {
                style.left = 8;
                style.right = 24; // leaves room for the scrollbar
                style.height = RowHeight;
                style.paddingLeft = 12;
                style.justifyContent = Justify.Center;
                CarouselCell.SetRadius(this, 8);
                _label.style.color = Color.white;
                Add(_label);
            }

            public override void UpdateContent(string itemData)
            {
                _label.text = itemData;
                style.backgroundColor = Index % 2 == 0 ? new Color(0.18f, 0.21f, 0.27f) : new Color(0.22f, 0.25f, 0.32f);
            }
        }
    }
}
