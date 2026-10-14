using System;
using System.Collections;
using System.IO;
using System.Linq;
using FutureDigitalPlus.UIKit.Samples.ScrollViewBasics;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UIElements;
using Object = UnityEngine.Object;

namespace FutureDigitalPlus.UIKit.Samples.Smoke.Tests
{
    /// <summary>
    /// Runs the Scroll View Basics sample the way a player would: it puts the sample component on a new GameObject, then clicks, drags,
    /// presses the sample's buttons and turns the mouse wheel by sending UI Toolkit events, and checks what the screen shows. The test
    /// framework fails a test on any logged error or exception. Each test saves a screenshot when FDP_SMOKE_OUT names a folder.
    /// </summary>
    public class ScrollViewBasicsSmokeTests
    {
        private const int Width = 1280;
        private const int Height = 720;

        private GameObject _host;
        private UIDocument _document;
        private RenderTexture _target;

        private VisualElement Root => _document.rootVisualElement;

        private IEnumerator Open()
        {
            _host = new GameObject("ScrollViewBasicsSmoke");
            _host.AddComponent<ScrollViewBasicsSample>();
            yield return null; // the sample builds in Start
            yield return RenderOffscreen();
        }

        private IEnumerator RenderOffscreen()
        {
            _document = _host.GetComponentInChildren<UIDocument>();
            Assert.IsNotNull(_document, "the sample creates a UIDocument");
            _target = new RenderTexture(Width, Height, 24);
            _document.panelSettings.targetTexture = _target; // a fixed size, independent of the Game view
            _document.panelSettings.clearColor = true;
            yield return Frames(5);
        }

        [UnityTearDown]
        public IEnumerator TearDown()
        {
            if (_host != null) Object.Destroy(_host);
            yield return null;
            if (_target != null)
            {
                _target.Release();
                Object.Destroy(_target);
            }

            Assert.AreEqual(0, Object.FindObjectsByType<UIDocument>(FindObjectsSortMode.None).Length, "the sample leaves no UIDocument behind");
        }

        [UnityTest]
        public IEnumerator Builds_ThreeViews_WithFewCells()
        {
            yield return Open();

            var list = Root.Q<RowList>();
            var grid = Root.Q<ColorGrid>();
            var carousel = Root.Q<Carousel>();
            Assert.IsNotNull(list, "list");
            Assert.IsNotNull(grid, "grid");
            Assert.IsNotNull(carousel, "carousel");

            Assert.Less(Cells(list), 30, "1,000 rows are shown with a few reused cells");
            Assert.Less(Cells(grid), 120, "400 tiles are shown with a few reused cells");
            Assert.IsNotNull(Visible(list, "Row 1"));
            Assert.IsNotNull(Visible(grid, "0"));
            Assert.AreEqual("Selected: item 1", SelectionLabel().text);
            Assert.AreEqual(50f, Card("1").style.left.value.value, 0.01f, "card 1 starts in the middle");
            Capture("builds");
        }

        [UnityTest]
        public IEnumerator Carousel_ClickACard_ScrollsItToTheMiddle()
        {
            yield return Open();

            var card = Card("3");
            Assert.AreNotEqual(50f, card.style.left.value.value, "card 3 starts off centre");
            Send.Click(card);

            yield return WaitUntil(() => Mathf.Abs(Card("3").style.left.value.value - 50f) < 0.01f, 2f);
            Assert.AreEqual(50f, Card("3").style.left.value.value, 0.01f, "the clicked card came to rest in the middle");
            Assert.AreEqual("Selected: item 3", SelectionLabel().text);
            Capture("carousel-click");
        }

        [UnityTest]
        public IEnumerator Carousel_Drag_SnapsToAnotherCard()
        {
            yield return Open();

            var carousel = Root.Q<Carousel>();
            var centre = carousel.worldBound.center;
            yield return Send.DragOverFrames(Card("1"), centre, centre + new Vector2(-260f, 0f));

            yield return Wait(2f);
            var middle = carousel.Query<CarouselCell>().ToList()
                .FirstOrDefault(c => Shown(c) && Mathf.Abs(c.style.left.value.value - 50f) < 0.01f);
            Assert.IsNotNull(middle, "after the flick, one card rests exactly in the middle (snapped)");
            Assert.AreNotEqual("1", middle.Q<Label>().text, "the drag moved the carousel on");
            Assert.AreEqual("Selected: item " + middle.Q<Label>().text, SelectionLabel().text);
            Capture("carousel-drag");
        }

        [UnityTest]
        public IEnumerator List_Buttons_JumpAndScroll()
        {
            yield return Open();
            var list = Root.Q<RowList>();

            Send.Click(ButtonText("Jump to 500"));
            yield return Frames(2);
            Assert.IsNotNull(Visible(list, "Row 500"), "Jump to 500");
            Assert.IsNull(Visible(list, "Row 1"));

            Send.Click(ButtonText("Bottom"));
            yield return WaitUntil(() => Visible(list, "Row 1000") != null, 2f);
            Assert.IsNotNull(Visible(list, "Row 1000"), "Bottom");
            Assert.AreEqual(1f, list.Q<Scroller>().value, 1e-3f, "the scrollbar is at the end");

            Send.Click(ButtonText("Top"));
            yield return Wait(1f); // the 0.4 s animation has ended
            Assert.IsNotNull(Visible(list, "Row 1"), "Top");
            Assert.AreEqual(0f, list.Q<Scroller>().value, 1e-3f, "the scrollbar is back at the start");
            Capture("list-buttons");
        }

        [UnityTest]
        public IEnumerator List_Drag_ScrollsAndMovesTheScrollbar()
        {
            yield return Open();
            var list = Root.Q<RowList>();

            var row = Visible(list, "Row 5");
            var from = row.worldBound.center;
            yield return Send.DragOverFrames(row, from, from + new Vector2(0f, -240f));
            yield return Wait(1.5f);

            Assert.IsNull(Visible(list, "Row 1"), "row 1 left the top");
            Assert.Greater(list.Q<Scroller>().value, 0f, "the scrollbar followed");
            Capture("list-drag");
        }

        [UnityTest]
        public IEnumerator Grid_Wheel_Scrolls()
        {
            yield return Open();
            var grid = Root.Q<ColorGrid>();

            for (var i = 0; i < 10; i++)
            {
                Send.Wheel(grid, grid.worldBound.center, 3f);
                yield return null;
            }

            yield return Wait(1f);
            var shown = grid.Query<Label>().ToList().Where(l => Shown(l) && grid.worldBound.Overlaps(l.worldBound)).Select(l => int.Parse(l.text)).ToList();
            Assert.IsNull(Visible(grid, "0"), "the first row scrolled away");
            Assert.Greater(shown.Min(), 15, "ten wheel notches scroll well past the first four rows");
            Capture("grid-wheel");
        }

        private Label SelectionLabel() => Root.Query<Label>().ToList().First(l => l.text.StartsWith("Selected:", StringComparison.Ordinal));

        private Button ButtonText(string text) => Root.Query<Button>().ToList().First(b => b.text == text);

        private CarouselCell Card(string text) =>
            Root.Q<Carousel>().Query<CarouselCell>().ToList().First(c => Shown(c) && c.Q<Label>().text == text);

        private static int Cells(VisualElement view) => view.Query<VisualElement>(className: "fdp-fancy-cell").ToList().Count;

        /// <summary>The shown label with <paramref name="text"/> inside <paramref name="view"/> and at least partly in its viewport; null if none.</summary>
        private static Label Visible(VisualElement view, string text) =>
            view.Query<Label>().ToList().FirstOrDefault(l => l.text == text && Shown(l) && view.worldBound.Overlaps(l.worldBound));

        private static bool Shown(VisualElement element)
        {
            for (var e = element; e != null; e = e.parent)
            {
                if (e.style.display == DisplayStyle.None) return false;
            }

            return true;
        }

        private static IEnumerator Frames(int count)
        {
            for (var i = 0; i < count; i++) yield return null;
        }

        private static IEnumerator Wait(float seconds)
        {
            var end = Time.realtimeSinceStartup + seconds;
            while (Time.realtimeSinceStartup < end) yield return null;
        }

        private static IEnumerator WaitUntil(Func<bool> done, float seconds)
        {
            var end = Time.realtimeSinceStartup + seconds;
            while (!done() && Time.realtimeSinceStartup < end) yield return null;
            yield return null;
        }

        private void Capture(string name)
        {
            var folder = Environment.GetEnvironmentVariable("FDP_SMOKE_OUT");
            if (string.IsNullOrEmpty(folder)) return;

            Directory.CreateDirectory(folder);
            var previous = RenderTexture.active;
            RenderTexture.active = _target;
            var texture = new Texture2D(Width, Height, TextureFormat.RGBA32, false);
            texture.ReadPixels(new Rect(0, 0, Width, Height), 0, 0);
            texture.Apply();
            RenderTexture.active = previous;
            File.WriteAllBytes(Path.Combine(folder, name + ".png"), texture.EncodeToPNG());
            Object.Destroy(texture);
        }
    }

    /// <summary>Sends UI Toolkit pointer and wheel events to an element, the way the panel does after reading the mouse.</summary>
    internal static class Send
    {
        public static void Click(VisualElement target)
        {
            var at = target.worldBound.center;
            Pointer<PointerDownEvent>(target, at, EventType.MouseDown);
            Pointer<PointerUpEvent>(target, at, EventType.MouseUp);
        }

        /// <summary>A press, one move per frame like a finger, and a release.</summary>
        public static IEnumerator DragOverFrames(VisualElement target, Vector2 from, Vector2 to, int steps = 8)
        {
            Pointer<PointerDownEvent>(target, from, EventType.MouseDown);
            yield return null;
            for (var i = 1; i <= steps; i++)
            {
                Pointer<PointerMoveEvent>(target, Vector2.Lerp(from, to, i / (float)steps), EventType.MouseDrag);
                yield return null;
            }

            Pointer<PointerUpEvent>(target, to, EventType.MouseUp);
        }

        public static void Wheel(VisualElement target, Vector2 at, float deltaY)
        {
            var wheel = new Event { type = EventType.ScrollWheel, mousePosition = at, delta = new Vector2(0f, deltaY) };
            using (var e = WheelEvent.GetPooled(wheel))
            {
                e.target = target;
                target.SendEvent(e);
            }
        }

        private static void Pointer<T>(VisualElement target, Vector2 at, EventType type) where T : PointerEventBase<T>, new()
        {
            var mouse = new Event { type = type, mousePosition = at, button = 0 };
            using (var e = PointerEventBase<T>.GetPooled(mouse))
            {
                e.target = target;
                target.SendEvent(e);
            }
        }
    }
}
