using System.Collections.Generic;
using FutureDigitalPlus.UIKit.ScrollView;
using UnityEngine;
using UnityEngine.UIElements;

namespace FutureDigitalPlus.UIKit.Samples.ScrollViewBasics
{
    /// <summary>A grid of colour tiles, four per row, built only for the rows in view (the upstream "GridView" example).</summary>
    public sealed class ColorGrid : FancyGridView<Color>
    {
        private const float Tile = 64f;

        /// <summary>Creates the grid.</summary>
        public ColorGrid()
        {
            style.flexGrow = 1;
            StartAxisCellCount = 4;
            GridCellSize = new Vector2(Tile, Tile);
            StartAxisSpacing = 8f;
            Spacing = 8f;
            PaddingHead = 8f;
            PaddingTail = 8f;
        }

        /// <summary>Shows <paramref name="colors"/>.</summary>
        public void Show(IList<Color> colors) => UpdateContents(colors);

        /// <inheritdoc/>
        protected override void SetupCellTemplate() => Setup(() => new Swatch());

        private sealed class Swatch : FancyGridViewCell<Color>
        {
            private readonly Label _label = new Label();

            public Swatch()
            {
                style.width = Tile;
                style.height = Tile;
                style.justifyContent = Justify.Center;
                style.alignItems = Align.Center;
                CarouselCell.SetRadius(this, 10);
                _label.style.color = new Color(1f, 1f, 1f, 0.85f);
                Add(_label);
            }

            public override void UpdateContent(Color itemData)
            {
                style.backgroundColor = itemData;
                _label.text = Index.ToString();
            }
        }
    }
}
