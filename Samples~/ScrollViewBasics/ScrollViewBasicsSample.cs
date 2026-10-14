using System.Linq;
using UnityEngine;
using UnityEngine.UIElements;

namespace FutureDigitalPlus.UIKit.Samples.ScrollViewBasics
{
    /// <summary>
    /// Put it on an empty GameObject in an empty scene and press Play. It creates a PanelSettings and a UIDocument and shows the three
    /// kinds of view side by side: a looping <see cref="Carousel"/>, a <see cref="RowList"/> of 1,000 rows and a <see cref="ColorGrid"/>.
    /// </summary>
    [AddComponentMenu("FDP UI Kit/Scroll View Basics Sample")]
    public class ScrollViewBasicsSample : MonoBehaviour
    {
        private const string RuntimeThemeResource = "FutureDigitalPlus/UIKit/Samples/ScrollViewBasics/RuntimeTheme";

        private static readonly Color Page = new Color(0.09f, 0.10f, 0.13f);
        private static readonly Color Pane = new Color(0.13f, 0.15f, 0.19f);

        private PanelSettings _panelSettings;

        private void Start()
        {
            _panelSettings = ScriptableObject.CreateInstance<PanelSettings>();
            _panelSettings.themeStyleSheet = Resources.Load<ThemeStyleSheet>(RuntimeThemeResource);
            _panelSettings.scaleMode = PanelScaleMode.ScaleWithScreenSize;
            _panelSettings.referenceResolution = new Vector2Int(1280, 720);
            _panelSettings.match = 0.5f;

            var go = new GameObject("Scroll View Basics (UIDocument)");
            go.transform.SetParent(transform, false);
            var document = go.AddComponent<UIDocument>();
            document.panelSettings = _panelSettings;

            Build(document.rootVisualElement);
        }

        private void OnDestroy()
        {
            if (_panelSettings != null) Destroy(_panelSettings);
        }

        private static void Build(VisualElement root)
        {
            root.style.backgroundColor = Page;
            root.style.paddingLeft = 16;
            root.style.paddingRight = 16;
            root.style.paddingTop = 16;
            root.style.paddingBottom = 16;

            // Carousel across the top.
            var carouselPane = AddPane(root, "Carousel: drag, flick, or click a card");
            carouselPane.style.flexGrow = 0;
            carouselPane.style.flexShrink = 0;
            carouselPane.style.flexBasis = StyleKeyword.Auto;
            carouselPane.style.height = 240;
            var selection = new Label();
            selection.style.color = Color.white;
            carouselPane.Add(selection);
            var carousel = new Carousel();
            carouselPane.Add(carousel);
            carousel.SelectionChanged += index => selection.text = "Selected: item " + (index + 1);
            carousel.Show(Enumerable.Range(1, 12).Select(i => i.ToString()).ToArray());

            // List and grid side by side below.
            var row = new VisualElement();
            row.style.flexDirection = FlexDirection.Row;
            row.style.flexGrow = 1;
            row.style.marginTop = 12;
            root.Add(row);

            var listPane = AddPane(row, "List: 1,000 rows, about 15 row cells");
            listPane.style.marginRight = 12;
            var buttons = new VisualElement();
            buttons.style.flexDirection = FlexDirection.Row;
            listPane.Add(buttons);
            var list = new RowList();
            listPane.Add(list);
            buttons.Add(new Button(() => list.ScrollToItem(0, 0f)) { text = "Top" });
            buttons.Add(new Button(() => list.JumpToItem(499)) { text = "Jump to 500" });
            buttons.Add(new Button(() => list.ScrollToItem(999, 1f)) { text = "Bottom" });
            list.Show(Enumerable.Range(1, 1000).Select(i => "Row " + i).ToArray());

            var gridPane = AddPane(row, "Grid: 400 tiles");
            var grid = new ColorGrid();
            gridPane.Add(grid);
            grid.Show(Enumerable.Range(0, 400).Select(i => Color.HSVToRGB(i / 400f, 0.55f, 0.85f)).ToArray());
        }

        private static VisualElement AddPane(VisualElement parent, string title)
        {
            var pane = new VisualElement();
            pane.style.flexGrow = 1;
            pane.style.flexBasis = 0;
            pane.style.backgroundColor = Pane;
            pane.style.paddingLeft = 12;
            pane.style.paddingRight = 12;
            pane.style.paddingTop = 12;
            pane.style.paddingBottom = 12;
            CarouselCell.SetRadius(pane, 12);

            var heading = new Label(title);
            heading.style.color = Color.white;
            heading.style.unityFontStyleAndWeight = FontStyle.Bold;
            heading.style.marginBottom = 8;
            pane.Add(heading);

            parent.Add(pane);
            return pane;
        }
    }
}
