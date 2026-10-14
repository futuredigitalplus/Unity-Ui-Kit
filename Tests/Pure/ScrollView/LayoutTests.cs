using NUnit.Framework;

namespace FutureDigitalPlus.UIKit.ScrollView.Tests
{
    public class LayoutTests
    {
        [Test]
        public void CellLayout_First_AtPositionZero_PutsTheFirstItemAtTheOffset()
        {
            CellLayout.First(0f, 0.5f, 0.2f, out var firstIndex, out var firstPosition);
            Assert.AreEqual(-2, firstIndex, "two slots before item 0 fit in front of the offset");
            Assert.AreEqual(0.1f, firstPosition, 1e-5f);
            Assert.AreEqual(0.5f, firstPosition + (0 - firstIndex) * 0.2f, 1e-5f, "item 0 sits at the scroll offset");
            Assert.AreEqual(5, CellLayout.PoolSize(firstPosition, 0.2f));
        }

        [Test]
        public void CellLayout_NeedsMoreCells_UntilTheViewportIsCovered()
        {
            Assert.IsTrue(CellLayout.NeedsMoreCells(0.1f, 4, 0.2f));
            Assert.IsFalse(CellLayout.NeedsMoreCells(0.1f, 5, 0.2f));
        }

        [Test]
        public void CellLayout_ItemIndex_WrapsOnlyWhenLooping()
        {
            Assert.AreEqual(-1, CellLayout.ItemIndex(-1, 5, false));
            Assert.AreEqual(4, CellLayout.ItemIndex(-1, 5, true));
            Assert.AreEqual(1, CellLayout.ItemIndex(6, 5, true));
        }

        [Test]
        public void CellLayout_IsShown_HidesMissingItemsAndSlotsPastTheEnd()
        {
            Assert.IsTrue(CellLayout.IsShown(0, 0.5f, 3));
            Assert.IsFalse(CellLayout.IsShown(-1, 0.5f, 3));
            Assert.IsFalse(CellLayout.IsShown(3, 0.5f, 3));
            Assert.IsFalse(CellLayout.IsShown(1, 1.01f, 3));
        }

        [Test]
        public void CellLayout_PoolIndex_KeepsACellOnItsItem()
        {
            Assert.AreEqual(CellLayout.PoolIndex(3, 0, 5), CellLayout.PoolIndex(2, 1, 5), "item 3 keeps its cell when the view moves one slot");
        }

        private static ScrollRectLayout List(int items) => new ScrollRectLayout
        {
            CellSize = 100f,
            ViewportSize = 500f,
            ItemCount = items,
        };

        [Test]
        public void ScrollRect_Geometry_OfAPlainList()
        {
            var l = List(20);
            Assert.AreEqual(100f / 600f, l.CellInterval, 1e-6f);
            Assert.AreEqual(l.CellInterval, l.ScrollOffset, 1e-6f);
            Assert.AreEqual(5f, l.ScrollLength, 1e-4f);
            Assert.AreEqual(5f, l.ViewportLength, 1e-4f);
            Assert.AreEqual(15f, l.MaxScrollPosition, 1e-4f);
            Assert.IsTrue(l.Scrollable);
            Assert.AreEqual(600f, l.ScrollSize, 1e-4f);
            Assert.AreEqual(0f, l.ReuseMargin);
            Assert.AreEqual(0.25f, l.ScrollbarSize(l.ViewportLength), 1e-5f);
        }

        [Test]
        public void ScrollRect_PositionConversions_RoundTrip()
        {
            var l = List(20);
            l.PaddingHead = 30f;
            l.Spacing = 10f;
            foreach (var p in new[] { 0f, 1f, 7.5f, 12f })
            {
                Assert.AreEqual(p, l.ToFancyScrollViewPosition(l.ToScrollerPosition(p)), 1e-4f);
            }

            Assert.AreEqual(19f, l.ToScrollerPosition(l.MaxScrollPosition - l.PaddingHeadLength), 1e-3f, "the end of the content is the scroller's last position");
        }

        [Test]
        public void ScrollRect_Alignment_PutsTheItemAtTheStartOrEnd_AndClamps()
        {
            var l = List(20);
            Assert.AreEqual(l.ToScrollerPosition(10f), l.ToScrollerPosition(10f, 0f), 1e-4f, "alignment 0: item 10 at the top");
            Assert.AreEqual(l.ToScrollerPosition(6f), l.ToScrollerPosition(10f, 1f), 1e-4f, "alignment 1: item 10 is the last of five rows");
            Assert.AreEqual(0f, l.ToScrollerPosition(0f, 0.5f), 1e-4f, "cannot centre the first item: clamped");
            Assert.AreEqual(19f, l.ToScrollerPosition(19f, 0.5f), 1e-3f, "cannot centre the last item: clamped");
        }

        [Test]
        public void ScrollRect_ShortContent_IsNotScrollable_AndConvertsToZero()
        {
            var l = List(3);
            Assert.IsFalse(l.Scrollable);
            Assert.AreEqual(0f, l.ToScrollerPosition(1f));
            Assert.AreEqual(1f, l.ScrollbarSize(l.ViewportLength));

            l.ItemCount = 5;
            Assert.AreEqual(0f, l.MaxScrollPosition, 1e-4f, "five rows exactly fill the viewport");
            Assert.AreEqual(0f, l.ToScrollerPosition(2f), "no division by zero");
        }

        [Test]
        public void ScrollRect_ZeroCellSize_DoesNotDivideByZero()
        {
            var l = new ScrollRectLayout { ViewportSize = 500f, ItemCount = 5 };
            Assert.IsFalse(float.IsNaN(l.CellInterval));
            Assert.IsFalse(float.IsNaN(l.PaddingHeadLength));
            Assert.IsFalse(float.IsInfinity(l.MaxScrollPosition));
        }

        [Test]
        public void ScrollRect_ReuseMargin_WidensTheScrollRange()
        {
            var l = List(20);
            l.ReuseCellMarginCount = 1f;
            Assert.AreEqual(100f, l.ReuseMargin, 1e-4f);
            Assert.AreEqual(800f, l.ScrollSize, 1e-4f);
            Assert.AreEqual(5f, l.ViewportLength, 1e-4f, "the visible length does not change");
        }
    }
}
