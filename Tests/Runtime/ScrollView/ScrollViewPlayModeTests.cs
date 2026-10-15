using System.Collections;
using System.Linq;
using System.Text.RegularExpressions;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UIElements;

namespace FutureDigitalPlus.UIKit.ScrollView.Tests
{
    public class ScrollViewPlayModeTests
    {
        private TestPanel _panel;

        [SetUp]
        public void SetUp() => _panel = TestPanel.Create();

        [TearDown]
        public void TearDown() => _panel.Dispose();

        private static float Y(VisualElement cell) => cell.style.translate.value.y.value;

        private static float X(VisualElement cell) => cell.style.translate.value.x.value;

        private IEnumerator ShowList(RowList list, int count)
        {
            _panel.Root.Add(list);
            list.Show(Enumerable.Range(0, count).ToArray());
            yield return TestPanel.Frames();
        }

        [UnityTest]
        public IEnumerator List_BuildsOnlyTheCellsInView_AndStacksThem()
        {
            var list = new RowList();
            yield return ShowList(list, 100);

            Assert.LessOrEqual(list.Cells.Count, 10, "a 400 high view of 50 high rows needs about nine cells, not a hundred");
            Assert.AreEqual(0f, Y(list.Showing(0)), 0.01f);
            Assert.AreEqual(50f, Y(list.Showing(1)), 0.01f);
            Assert.AreEqual(350f, Y(list.Showing(7)), 0.01f);
            Assert.AreEqual("Row 7", list.Showing(7).Button.text);
        }

        [UnityTest]
        public IEnumerator List_JumpTo_PutsTheItemAtTheAlignment()
        {
            var list = new RowList();
            yield return ShowList(list, 100);

            list.Jump(50, 0f);
            Assert.AreEqual(0f, Y(list.Showing(50)), 0.01f, "alignment 0: at the top");

            list.Jump(50, 1f);
            Assert.AreEqual(350f, Y(list.Showing(50)), 0.01f, "alignment 1: the last row of the viewport");

            list.Jump(99, 0f);
            Assert.AreEqual(350f, Y(list.Showing(99)), 0.01f, "the last item cannot go higher than the bottom row");
        }

        [UnityTest]
        public IEnumerator List_Drag_ScrollsWithoutClickingTheRow()
        {
            var list = new RowList();
            yield return ShowList(list, 100);

            var row = list.Showing(6);
            var from = row.Button.worldBound.center;
            Send.Drag(row.Button, from, from + new Vector2(0f, -200f));

            Assert.Greater(list.Scroller.Position, 0f, "dragging up scrolls towards later rows");
            Assert.IsNull(list.Showing(0), "row 0 left the viewport");
            Assert.AreEqual(0, list.Cells.Sum(c => c.Clicks), "a drag is not a click");
            Assert.IsFalse(list.Scroller.Model.IsDragging);
        }

        [UnityTest]
        public IEnumerator List_Tap_ClicksTheRow_WithoutScrolling()
        {
            var list = new RowList();
            yield return ShowList(list, 100);

            var row = list.Showing(2);
            var at = row.Button.worldBound.center;
            Send.PointerDown(row.Button, at);
            Send.PointerMove(row.Button, at + new Vector2(0f, 3f));
            Send.PointerUp(row.Button, at + new Vector2(0f, 3f));

            Assert.AreEqual(1, row.Clicks);
            Assert.AreEqual(0f, list.Scroller.Position, 1e-4f);
        }

        [UnityTest]
        public IEnumerator List_PressReleasedOutside_DoesNotBlockTheNextDrag()
        {
            var outside = new VisualElement();
            outside.style.position = Position.Absolute;
            outside.style.left = 400;
            outside.style.width = 200;
            outside.style.height = 200;
            _panel.Root.Add(outside);
            var list = new RowList();
            yield return ShowList(list, 100);

            var cell = list.Showing(3);
            Send.PointerDown(cell, cell.worldBound.center);
            Send.PointerUp(outside, outside.worldBound.center);

            // A touch has another pointer id: a mouse press still held by the scroller would make it ignore this drag.
            var row = list.Showing(6);
            var from = row.Button.worldBound.center;
            Send.TouchDown(row.Button, from);
            for (var i = 1; i <= 6; i++) Send.TouchMove(row.Button, from + new Vector2(0f, -200f * i / 6f));
            Send.TouchUp(row.Button, from + new Vector2(0f, -200f));
            Assert.Greater(list.Scroller.Position, 0f, "the release outside ended the mouse press, so the touch drag scrolls");
        }

        [UnityTest]
        public IEnumerator Snapping_ClickOnACellThatScrolls_IsNotUndoneByTheSnap()
        {
            var carousel = new SnapCarousel();
            _panel.Root.Add(carousel);
            carousel.Show(Enumerable.Range(0, 10).ToArray());
            yield return TestPanel.Frames();

            var cell = carousel.Showing(1);
            var at = cell.worldBound.center;
            Send.PointerDown(cell, at);
            Send.PointerUp(cell, at);

            var end = Time.realtimeSinceStartup + 1f;
            while (Time.realtimeSinceStartup < end) yield return null;
            Assert.AreEqual(1f, carousel.Scroller.Position, 1e-3f, "the click's ScrollTo(1) ran; the snap of the release would have kept 0");
            Assert.AreEqual(50f, carousel.Showing(1).style.left.value.value, 0.01f, "item 1 is in the middle");
        }

        [UnityTest]
        public IEnumerator List_WithPaddingAndBorder_FillsThePaddingBox()
        {
            var list = new RowList();
            list.style.paddingTop = 20;
            list.style.paddingBottom = 20;
            list.style.borderTopWidth = 5;
            list.style.borderBottomWidth = 5;
            yield return ShowList(list, 100);

            Assert.AreEqual(390f, list.Scroller.ViewportSize, 0.01f, "400 high less two 5 wide borders");
            Assert.AreEqual(0f, Y(list.Showing(0)), 0.01f);
            list.Jump(99, 1f);
            Assert.AreEqual(340f, Y(list.Showing(99)), 0.01f, "the last row ends at the bottom of the padding box");
        }

        [UnityTest]
        public IEnumerator List_ResizeAcrossTheAxis_KeepsTheAnimationRunning()
        {
            var list = new RowList();
            yield return ShowList(list, 100);

            list.Scroller.ScrollTo(50f, 2f);
            yield return TestPanel.Frames();
            list.style.width = 250;
            yield return TestPanel.Frames();

            Assert.IsTrue(list.Scroller.Model.IsAutoScrolling, "a narrower list does not stop the scroll");
            Assert.Greater(list.Scroller.Position, 0f);
        }

        [UnityTest]
        public IEnumerator List_Wheel_Scrolls()
        {
            var list = new RowList();
            yield return ShowList(list, 100);

            Send.Wheel(list, list.worldBound.center, 3f);
            Assert.Greater(list.Scroller.Position, 0f);
        }

        [UnityTest]
        public IEnumerator List_SettlesBackAfterAPullPastTheStart()
        {
            var list = new RowList();
            yield return ShowList(list, 100);

            var from = list.Showing(1).Button.worldBound.center;
            Send.Drag(list.Showing(1).Button, from, from + new Vector2(0f, 150f));
            Assert.Less(list.Scroller.Position, 0f, "pulled past the start");
            Assert.Greater(Y(list.Showing(0)), 0f);

            var end = Time.realtimeSinceStartup + 2f;
            while (Time.realtimeSinceStartup < end && list.Scroller.Position < 0f) yield return null;
            Assert.AreEqual(0f, list.Scroller.Position, 1e-3f, "the scheduled tick ran the elastic return");
            Assert.AreEqual(0f, Y(list.Showing(0)), 0.01f);
        }

        [UnityTest]
        public IEnumerator List_Scrollbar_FollowsAndDrivesThePosition()
        {
            var list = new RowList();
            var bar = new Scroller(0f, 1f, null, SliderDirection.Vertical);
            bar.style.position = Position.Absolute;
            bar.style.right = 0;
            bar.style.top = 0;
            bar.style.bottom = 0;
            list.Add(bar);
            list.Scroller.Scrollbar = bar;
            yield return ShowList(list, 100);

            Assert.AreEqual(DisplayStyle.Flex, bar.style.display.value);
            list.Jump(99, 1f);
            Assert.AreEqual(1f, bar.value, 1e-3f);

            bar.value = 0.5f;
            Assert.AreEqual(49.5f, list.Scroller.Position, 1e-3f, "half way of 0..99");

            var end = bar.worldBound.center;
            Send.PointerDown(bar, end);
            Assert.IsFalse(list.Scroller.Model.IsDragging, "a press on the scrollbar does not drag the content");
            Send.PointerUp(bar, end);
        }

        [UnityTest]
        public IEnumerator List_ShortContent_HidesTheScrollbar_AndDoesNotDrag()
        {
            var list = new RowList();
            var bar = new Scroller(0f, 1f, null, SliderDirection.Vertical);
            list.Add(bar);
            list.Scroller.Scrollbar = bar;
            yield return ShowList(list, 3);

            Assert.AreEqual(DisplayStyle.None, bar.style.display.value);
            Assert.IsFalse(list.Scroller.Draggable);
            Assert.AreEqual(0f, Y(list.Showing(0)), 0.01f);
            Assert.IsNull(list.Showing(3));
        }

        [UnityTest]
        public IEnumerator List_Resize_Relayouts()
        {
            var list = new RowList();
            yield return ShowList(list, 100);
            var before = list.Cells.Count;

            list.style.height = 580;
            yield return TestPanel.Frames();

            Assert.Greater(list.Cells.Count, before, "a taller view needs more cells");
            Assert.AreEqual(0f, Y(list.Showing(0)), 0.01f);
            Assert.AreEqual(500f, Y(list.Showing(10)), 0.01f);
        }

        [UnityTest]
        public IEnumerator List_Loop_IsRefused()
        {
            var list = new RowList();
            yield return ShowList(list, 10);

            LogAssert.Expect(LogType.Error, new Regex("Loop is currently not supported"));
            list.Loop = true;
            Assert.IsFalse(list.Loop);
        }

        [Test]
        public void Carousel_PlacesItemsAroundTheOffset_AndLoops()
        {
            var carousel = new Carousel { Loop = true };
            _panel.Root.Add(carousel);
            carousel.Show(new[] { "a", "b", "c", "d", "e" });

            CarouselCell Cell(string text) => carousel.Cells.First(c => c.IsVisible && c.Text == text);

            Assert.AreEqual(0.5f, Cell("a").LastPosition, 1e-5f, "item 0 sits at the scroll offset");
            Assert.AreEqual(0.7f, Cell("b").LastPosition, 1e-5f);
            Assert.AreEqual(0.3f, Cell("e").LastPosition, 1e-5f, "with Loop the last item comes before the first");

            carousel.Move(-1f);
            Assert.AreEqual(0.5f, Cell("e").LastPosition, 1e-5f);

            carousel.Move(7.5f);
            Assert.AreEqual(0.4f, Cell("c").LastPosition, 1e-5f, "position 7.5 wraps to between c and d");
        }

        [Test]
        public void Carousel_WithoutLoop_HidesSlotsBeforeTheFirstItem()
        {
            var carousel = new Carousel();
            _panel.Root.Add(carousel);
            carousel.Show(new[] { "a", "b", "c" });

            Assert.AreEqual(3, carousel.Cells.Count(c => c.IsVisible));
            Assert.IsFalse(carousel.Cells.Any(c => c.IsVisible && c.LastPosition < 0.5f), "nothing in front of item 0");
        }

        [UnityTest]
        public IEnumerator Grid_PlacesCellsInRowsAndColumns()
        {
            var grid = new TestGrid();
            _panel.Root.Add(grid);
            grid.UpdateContents(Enumerable.Range(0, 10).ToArray());
            yield return TestPanel.Frames();

            Assert.AreEqual(10, grid.DataCount);
            Assert.AreEqual(20f, X(grid.Showing(3)), 0.01f, "column 0 of a centred row of three");
            Assert.AreEqual(110f, X(grid.Showing(4)), 0.01f);
            Assert.AreEqual(200f, X(grid.Showing(5)), 0.01f);
            Assert.AreEqual(0f, Y(grid.Showing(0)), 0.01f);
            Assert.AreEqual(90f, Y(grid.Showing(4)), 0.01f, "row 1 is one cell plus spacing down");
            Assert.AreEqual(270f, Y(grid.Showing(9)), 0.01f);
            Assert.IsNull(grid.Showing(10), "the last row has one item");
        }

        [Test]
        public void Grid_StartAxisCellCount_IsFixedOnceShown()
        {
            var grid = new TestGrid();
            _panel.Root.Add(grid);
            grid.UpdateContents(new[] { 1, 2, 3 });
            Assert.Throws<System.InvalidOperationException>(() => grid.StartAxisCellCount = 4);
        }

        private static IEnumerator Seconds(float seconds)
        {
            var end = Time.realtimeSinceStartup + seconds;
            while (Time.realtimeSinceStartup < end) yield return null;
        }

        [UnityTest]
        public IEnumerator List_UnderAHiddenScreen_StopsTicking_AndGoesOnWhenShown()
        {
            var screen = new VisualElement();
            _panel.Root.Add(screen);
            var list = new RowList();
            screen.Add(list);
            list.Show(Enumerable.Range(0, 100).ToArray());
            yield return TestPanel.Frames();
            Assert.IsTrue(list.Scroller.IsTicking, "a shown list ticks");

            list.Scroller.ScrollTo(50f, 2f);
            yield return TestPanel.Frames(2);
            screen.style.display = DisplayStyle.None;
            yield return TestPanel.Frames();
            Assert.IsFalse(list.Scroller.IsTicking, "display:none on an ancestor pauses the tick");

            var held = list.Scroller.Position;
            yield return Seconds(0.8f);
            Assert.AreEqual(held, list.Scroller.Position, 1e-4f, "the animation waits while the list is hidden");
            Assert.IsTrue(list.Scroller.Model.IsAutoScrolling);

            screen.style.display = DisplayStyle.Flex;
            yield return TestPanel.Frames();
            Assert.IsTrue(list.Scroller.IsTicking, "shown again: the tick resumes");
            Assert.IsTrue(list.Scroller.Model.IsAutoScrolling, "the time spent hidden is not caught up in one step");

            var end = Time.realtimeSinceStartup + 4f;
            while (Time.realtimeSinceStartup < end && list.Scroller.Model.IsAutoScrolling) yield return null;
            Assert.AreEqual(50f, list.Scroller.Position, 1e-3f, "the animation finished after the list came back");
        }

        [UnityTest]
        public IEnumerator List_HiddenItself_StopsTicking_AndResumesWhenShown()
        {
            var list = new RowList();
            yield return ShowList(list, 100);

            list.style.display = DisplayStyle.None;
            yield return TestPanel.Frames();
            Assert.IsFalse(list.Scroller.IsTicking);

            list.style.display = DisplayStyle.Flex;
            yield return TestPanel.Frames();
            Assert.IsTrue(list.Scroller.IsTicking);

            Send.Wheel(list, list.worldBound.center, 3f);
            Assert.Greater(list.Scroller.Position, 0f, "input still scrolls");
        }

        [UnityTest]
        public IEnumerator List_InvisibleButLaidOut_KeepsTicking()
        {
            var list = new RowList();
            yield return ShowList(list, 100);

            list.style.visibility = Visibility.Hidden; // keeps its layout: not a display:none screen
            yield return TestPanel.Frames();
            Assert.IsTrue(list.Scroller.IsTicking);
        }

        [UnityTest]
        public IEnumerator List_Disposed_DoesNotTick()
        {
            var list = new RowList();
            yield return ShowList(list, 100);

            list.Scroller.Dispose();
            Assert.IsFalse(list.Scroller.IsTicking);
            list.Scroller.ScrollTo(10f, 0.1f);
            Assert.IsFalse(list.Scroller.IsTicking, "a disposed scroller stays stopped");
        }
    }
}
