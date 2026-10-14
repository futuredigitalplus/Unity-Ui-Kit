using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace FutureDigitalPlus.UIKit.ScrollView.Tests
{
    /// <summary>A UIDocument rendering into a fixed-size RenderTexture, so layout does not depend on the Game view. Dispose it in TearDown.</summary>
    internal sealed class TestPanel
    {
        public const int Width = 800;
        public const int Height = 600;

        public GameObject Host;
        public UIDocument Document;
        public PanelSettings Settings;
        public RenderTexture Target;

        public VisualElement Root => Document.rootVisualElement;

        public static TestPanel Create()
        {
            var p = new TestPanel();
            p.Target = new RenderTexture(Width, Height, 0);
            p.Settings = ScriptableObject.CreateInstance<PanelSettings>();
            p.Settings.themeStyleSheet = Resources.Load<ThemeStyleSheet>("FutureDigitalPlus/UIKit/Tests/DefaultRuntimeTheme");
            p.Settings.scaleMode = PanelScaleMode.ConstantPixelSize;
            p.Settings.scale = 1f;
            p.Settings.targetTexture = p.Target;

            p.Host = new GameObject("FancyScrollTestHost");
            p.Host.SetActive(false);
            p.Document = p.Host.AddComponent<UIDocument>();
            p.Document.panelSettings = p.Settings;
            p.Host.SetActive(true);
            return p;
        }

        public static IEnumerator Frames(int count = 3)
        {
            for (var i = 0; i < count; i++) yield return null;
        }

        public void Dispose()
        {
            if (Host != null) Object.Destroy(Host);
            if (Settings != null) Object.Destroy(Settings);
            if (Target != null)
            {
                Target.Release();
                Object.Destroy(Target);
            }
        }
    }

    /// <summary>Dispatches pointer and wheel events to an element, the way the panel would after reading a device.</summary>
    internal static class Send
    {
        public static void PointerDown(VisualElement target, Vector2 position)
        {
            var mouse = new Event { type = EventType.MouseDown, mousePosition = position, button = 0 };
            using (var e = PointerDownEvent.GetPooled(mouse))
            {
                e.target = target;
                target.SendEvent(e);
            }
        }

        public static void PointerMove(VisualElement target, Vector2 position)
        {
            var mouse = new Event { type = EventType.MouseDrag, mousePosition = position, button = 0 };
            using (var e = PointerMoveEvent.GetPooled(mouse))
            {
                e.target = target;
                target.SendEvent(e);
            }
        }

        public static void PointerUp(VisualElement target, Vector2 position)
        {
            var mouse = new Event { type = EventType.MouseUp, mousePosition = position, button = 0 };
            using (var e = PointerUpEvent.GetPooled(mouse))
            {
                e.target = target;
                target.SendEvent(e);
            }
        }

        public static void Wheel(VisualElement target, Vector2 position, float deltaY)
        {
            var wheel = new Event { type = EventType.ScrollWheel, mousePosition = position, delta = new Vector2(0f, deltaY) };
            using (var e = WheelEvent.GetPooled(wheel))
            {
                e.target = target;
                target.SendEvent(e);
            }
        }

        public static void TouchDown(VisualElement target, Vector2 position) => Touch<PointerDownEvent>(target, position, TouchPhase.Began);

        public static void TouchMove(VisualElement target, Vector2 position) => Touch<PointerMoveEvent>(target, position, TouchPhase.Moved);

        public static void TouchUp(VisualElement target, Vector2 position) => Touch<PointerUpEvent>(target, position, TouchPhase.Ended);

        private static void Touch<T>(VisualElement target, Vector2 position, TouchPhase phase) where T : PointerEventBase<T>, new()
        {
            var finger = new UnityEngine.Touch { fingerId = 0, position = position, phase = phase };
            using (var e = PointerEventBase<T>.GetPooled(finger, EventModifiers.None))
            {
                e.target = target;
                target.SendEvent(e);
            }
        }

        /// <summary>A press at <paramref name="from"/>, moves in steps to <paramref name="to"/>, and a release there.</summary>
        public static void Drag(VisualElement target, Vector2 from, Vector2 to, int steps = 6)
        {
            PointerDown(target, from);
            for (var i = 1; i <= steps; i++) PointerMove(target, Vector2.Lerp(from, to, i / (float)steps));
            PointerUp(target, to);
        }
    }

    internal sealed class RowCell : FancyScrollRectCell<int>
    {
        public readonly Button Button = new Button();
        public int Clicks;

        public RowCell()
        {
            style.width = Length.Percent(100);
            style.height = RowList.RowHeight;
            Button.style.flexGrow = 1;
            Button.clicked += () => Clicks++;
            Add(Button);
        }

        public override void UpdateContent(int itemData) => Button.text = "Row " + itemData;
    }

    internal sealed class RowList : FancyScrollRect<int>
    {
        public const float RowHeight = 50f;

        public RowList()
        {
            style.width = 300;
            style.height = 400;
        }

        public readonly List<RowCell> Cells = new List<RowCell>();

        protected override float CellSize => RowHeight;

        protected override FancyCell<int, FancyScrollRectContext> CreateCell()
        {
            var cell = new RowCell();
            Cells.Add(cell);
            return cell;
        }

        public void Show(IList<int> items) => UpdateContents(items);

        public void Jump(int index, float alignment) => JumpTo(index, alignment);

        public RowCell Showing(int index) => Cells.Find(c => c.IsVisible && c.Index == index);
    }

    internal sealed class CarouselCell : FancyCell<string>
    {
        public float LastPosition = float.NaN;
        public string Text;

        public override void UpdateContent(string itemData) => Text = itemData;

        public override void UpdatePosition(float position) => LastPosition = position;
    }

    internal sealed class Carousel : FancyScrollView<string>
    {
        public readonly List<CarouselCell> Cells = new List<CarouselCell>();

        protected override FancyCell<string, NullContext> CreateCell()
        {
            var cell = new CarouselCell();
            Cells.Add(cell);
            return cell;
        }

        public void Show(IList<string> items) => UpdateContents(items);

        public void Move(float position) => UpdatePosition(position);
    }

    internal sealed class GridCell : FancyGridViewCell<int>
    {
        public GridCell()
        {
            style.width = 80;
            style.height = 80;
        }

        public override void UpdateContent(int itemData) { }
    }

    internal sealed class TestGrid : FancyGridView<int>
    {
        public readonly List<GridCell> Cells = new List<GridCell>();

        public TestGrid()
        {
            style.width = 300;
            style.height = 400;
            StartAxisCellCount = 3;
            GridCellSize = new Vector2(80f, 80f);
            StartAxisSpacing = 10f;
            Spacing = 10f;
        }

        protected override void SetupCellTemplate() => Setup(() =>
        {
            var cell = new GridCell();
            Cells.Add(cell);
            return cell;
        });

        public GridCell Showing(int index) => Cells.Find(c => c.IsVisible && c.Index == index && c.parent.style.display != DisplayStyle.None);
    }
}

namespace FutureDigitalPlus.UIKit.ScrollView.Tests
{
    internal sealed class SnapCell : FancyCell<int, SnapContext>
    {
        public SnapCell()
        {
            style.width = 100;
            style.height = 100;
            this.AddManipulator(new Clickable(() => Context.Clicked(Index)));
        }

        public override void UpdateContent(int itemData) { }

        public override void UpdatePosition(float position) => style.left = Length.Percent(position * 100f);
    }

    internal sealed class SnapContext
    {
        public System.Action<int> Clicked;
    }

    /// <summary>A horizontal, snapping view whose cells scroll themselves into place when clicked (the FocusOn pattern).</summary>
    internal sealed class SnapCarousel : FancyScrollView<int, SnapContext>
    {
        public readonly FancyScroller Scroller;
        public readonly List<SnapCell> Cells = new List<SnapCell>();

        public SnapCarousel()
        {
            style.width = 500;
            style.height = 100;
            Scroller = new FancyScroller(this, ScrollDirection.Horizontal) { ScrollSensitivity = 5f };
            Scroller.ValueChanged += UpdatePosition;
            Context.Clicked = index => Scroller.ScrollTo(index, 0.3f, Ease.OutCubic);
        }

        public void Show(IList<int> items)
        {
            UpdateContents(items);
            Scroller.SetTotalCount(items.Count);
        }

        public SnapCell Showing(int index) => Cells.Find(c => c.IsVisible && c.Index == index);

        protected override FancyCell<int, SnapContext> CreateCell()
        {
            var cell = new SnapCell();
            Cells.Add(cell);
            return cell;
        }
    }
}
