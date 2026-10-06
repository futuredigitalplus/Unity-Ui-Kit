using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UIElements;

namespace FutureDigitalPlus.UIKit.Controls.Tests
{
    public class CollectionTests
    {
        private ControlsPanel _panel;

        [SetUp]
        public void SetUp() => LogAssert.ignoreFailingMessages = true;

        [TearDown]
        public void TearDown()
        {
            LogAssert.ignoreFailingMessages = false;
            _panel?.Dispose();
        }

        private sealed class Row
        {
            public string Id;
            public string Text;
        }

        private static List<Row> Rows(int count, string suffix = "")
        {
            var rows = new List<Row>(count);
            for (var i = 0; i < count; i++) rows.Add(new Row { Id = "r" + i, Text = "Row " + i + suffix });
            return rows;
        }

        // ---- ListViewBinder ----

        private static ScrollView ScrollOf(ListViewBinder<Row> list) => list.ListView.Q<ScrollView>();

        private ListViewBinder<Row> AddList(out int[] bindCount)
        {
            _panel = ControlsPanel.Create();
            var counter = new int[1];
            bindCount = counter;
            var list = new ListViewBinder<Row>(() => new Label(), (element, row) =>
            {
                ((Label)element).text = row.Text;
                counter[0]++;
            }, row => row.Id, 32f, (a, b) => a.Text == b.Text)
            {
                style = { width = 300, height = 200 },
            };
            _panel.Root.Add(list);
            return list;
        }

        [UnityTest]
        public IEnumerator List_ShowsRows_AndAnIdenticalListDoesNothing()
        {
            var list = AddList(out var binds);
            list.SetItems(Rows(50));
            yield return ControlsPanel.Frames(4);
            Assert.AreEqual(50, list.Items.Count);
            Assert.Greater(binds[0], 0);
            var before = binds[0];
            list.SetItems(Rows(50));
            yield return ControlsPanel.Frames(2);
            Assert.AreEqual(before, binds[0], "same keys and same content: no rebind");
        }

        [UnityTest]
        public IEnumerator List_ReplacingTheData_KeepsTheScrollOffset()
        {
            var list = AddList(out _);
            list.SetItems(Rows(100));
            yield return ControlsPanel.Frames(4);
            ScrollOf(list).scrollOffset = new Vector2(0f, 300f);
            yield return ControlsPanel.Frames(3);
            var offset = ScrollOf(list).scrollOffset.y;
            Assert.Greater(offset, 100f, "the list scrolled");

            list.SetItems(Rows(100, "!"));
            yield return ControlsPanel.Frames(3);
            Assert.AreEqual(offset, ScrollOf(list).scrollOffset.y, 1f);
        }

        [UnityTest]
        public IEnumerator List_KeepsTheSelectionByKey_WhenRowsMove()
        {
            var list = AddList(out _);
            var rows = Rows(10);
            list.SetItems(rows);
            yield return ControlsPanel.Frames(3);
            Assert.IsTrue(list.SelectKey("r3"));
            Assert.AreEqual("r3", list.SelectedKey);

            rows.Reverse();
            list.SetItems(rows);
            yield return ControlsPanel.Frames(3);
            Assert.AreEqual("r3", list.SelectedKey, "the same row stays selected after it moved");
            Assert.IsFalse(list.SelectKey("missing"));
        }

        [UnityTest]
        public IEnumerator List_EmptyState_ReplacesTheListWhenThereAreNoRows()
        {
            var list = AddList(out _);
            list.EmptyState = EmptyStates.Create("Nothing here", "Come back later");
            list.SetItems(Rows(0));
            yield return ControlsPanel.Frames(2);
            Assert.AreEqual(DisplayStyle.None, list.ListView.style.display.value);
            Assert.AreEqual(DisplayStyle.Flex, list.Q<VisualElement>(null, "fdp-collection__empty").style.display.value);
            Assert.IsNotNull(list.Q<Label>(null, "ds-empty__title"));

            list.SetItems(Rows(3));
            yield return ControlsPanel.Frames(2);
            Assert.AreEqual(DisplayStyle.Flex, list.ListView.style.display.value);
            Assert.AreEqual(DisplayStyle.None, list.Q<VisualElement>(null, "fdp-collection__empty").style.display.value);
        }

        [UnityTest]
        public IEnumerator List_Loading_ShowsSkeletonRowsInsteadOfTheList()
        {
            var list = AddList(out _);
            list.SetSkeleton(4, 30f);
            list.IsLoading = true;
            yield return ControlsPanel.Frames(2);
            Assert.AreEqual(DisplayStyle.None, list.ListView.style.display.value);
            Assert.AreEqual(4, list.Query<VisualElement>(className: "fdp-collection__skeleton-row").ToList().Count);
            Assert.IsTrue(list.Q<VisualElement>(null, "fdp-collection__skeleton-row").ClassListContains("ds-skeleton"));

            list.SetItems(Rows(5));
            list.IsLoading = false;
            yield return ControlsPanel.Frames(2);
            Assert.AreEqual(DisplayStyle.Flex, list.ListView.style.display.value);
        }

        // ---- VirtualGrid ----

        private VirtualGrid<Row> AddGrid(out int[] bindCount, int width = 400, int height = 300)
        {
            _panel = ControlsPanel.Create();
            var counter = new int[1];
            bindCount = counter;
            var grid = new VirtualGrid<Row>(() => new Button { focusable = true }, (cell, row, index) =>
            {
                ((Button)cell).text = row.Text;
                counter[0]++;
            }, row => row.Id, (a, b) => a.Text == b.Text)
            {
                MinCellWidth = 90f,
                Spacing = 10f,
                FixedCellHeight = 60f,
                style = { width = width, height = height },
            };
            _panel.Root.Add(grid);
            return grid;
        }

        [UnityTest]
        public IEnumerator Grid_ColumnsFollowTheWidth_AndOnlyVisibleCellsAreBuilt()
        {
            var grid = AddGrid(out _);
            grid.SetItems(Rows(1000));
            yield return ControlsPanel.Frames(4);
            Assert.AreEqual(4, grid.Columns, "(400 + 10) / (90 + 10)");
            Assert.AreEqual(1000, grid.Count);
            Assert.IsNotNull(grid.CellAt(0));
            Assert.IsNull(grid.CellAt(999), "far below the viewport: not built");
            var built = grid.ScrollView.contentContainer.childCount;
            Assert.Less(built, 60, "a pool for the visible rows, not 1000 cells");
        }

        [UnityTest]
        public IEnumerator Grid_ResizingChangesTheColumns()
        {
            var grid = AddGrid(out _);
            grid.SetItems(Rows(40));
            yield return ControlsPanel.Frames(4);
            Assert.AreEqual(4, grid.Columns);
            grid.style.width = 210;
            yield return ControlsPanel.Frames(4);
            Assert.AreEqual(2, grid.Columns);
        }

        [UnityTest]
        public IEnumerator Grid_ScrollingBindsTheNewCells_AndRecyclesTheOldOnes()
        {
            var grid = AddGrid(out _);
            grid.SetItems(Rows(1000));
            yield return ControlsPanel.Frames(4);
            grid.ScrollToIndex(600);
            yield return ControlsPanel.Frames(3);
            var cell = grid.CellAt(600);
            Assert.IsNotNull(cell);
            Assert.AreEqual("Row 600", ((Button)cell).text);
            Assert.IsNull(grid.CellAt(0));
        }

        [UnityTest]
        public IEnumerator Grid_ReplacingTheDataKeepsTheScroll_AndAnIdenticalListDoesNothing()
        {
            var grid = AddGrid(out var binds);
            grid.SetItems(Rows(500));
            yield return ControlsPanel.Frames(4);
            grid.ScrollToIndex(300);
            yield return ControlsPanel.Frames(3);
            var offset = grid.ScrollView.scrollOffset.y;
            var before = binds[0];

            grid.SetItems(Rows(500));
            Assert.AreEqual(before, binds[0], "identical: no rebind");
            grid.SetItems(Rows(500, "!"));
            yield return ControlsPanel.Frames(3);
            Assert.AreEqual(offset, grid.ScrollView.scrollOffset.y, 1f);
            Assert.AreEqual("Row 300!", ((Button)grid.CellAt(300)).text);
        }

        [UnityTest]
        public IEnumerator Grid_ArrowKeysMoveFocusByCell_AndScrollTheTargetIntoView()
        {
            var grid = AddGrid(out _);
            grid.SetItems(Rows(200));
            yield return ControlsPanel.Frames(4);
            Assert.IsTrue(grid.FocusIndex(0));
            Assert.AreSame(grid.CellAt(0), grid.panel.focusController.focusedElement);

            Send.Move(grid.CellAt(0), NavigationMoveEvent.Direction.Right);
            Assert.AreSame(grid.CellAt(1), grid.panel.focusController.focusedElement);
            Send.Move(grid.CellAt(1), NavigationMoveEvent.Direction.Down);
            Assert.AreSame(grid.CellAt(5), grid.panel.focusController.focusedElement);

            var index = 5;
            for (var i = 0; i < 20; i++)
            {
                var from = grid.CellAt(index);
                Assert.IsNotNull(from, "the focused cell exists at step " + i);
                Send.Move(from, NavigationMoveEvent.Direction.Down);
                index += 4;
            }

            Assert.AreEqual(85, index);
            var focused = (VisualElement)grid.panel.focusController.focusedElement;
            Assert.AreEqual(85, grid.ItemIndexOf(focused));
            Assert.Greater(grid.ScrollView.scrollOffset.y, 500f, "scrolled along with the focus");
        }

        [UnityTest]
        public IEnumerator Grid_EmptyAndLoadingStates()
        {
            var grid = AddGrid(out _);
            grid.EmptyState = EmptyStates.Create("No items");
            grid.SetItems(Rows(0));
            yield return ControlsPanel.Frames(2);
            Assert.AreEqual(DisplayStyle.None, grid.ScrollView.style.display.value);
            grid.IsLoading = true;
            yield return ControlsPanel.Frames(2);
            Assert.GreaterOrEqual(grid.Query<VisualElement>(className: "fdp-collection__skeleton-row").ToList().Count, 1);
            grid.IsLoading = false;
            grid.SetItems(Rows(10));
            yield return ControlsPanel.Frames(2);
            Assert.AreEqual(DisplayStyle.Flex, grid.ScrollView.style.display.value);
        }
    }
}
