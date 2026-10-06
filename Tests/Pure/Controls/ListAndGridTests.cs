using System.Collections.Generic;
using NUnit.Framework;

namespace FutureDigitalPlus.UIKit.Controls.Tests
{
    public class ListDiffTests
    {
        [Test]
        public void SameKeys_AreIdentical()
        {
            var r = ListDiff.Compute(new[] { "a", "b", "c" }, new[] { "a", "b", "c" });
            Assert.IsTrue(r.IsIdentical);
            CollectionAssert.AreEqual(new[] { 0, 1, 2 }, r.NewToOld);
            Assert.AreEqual(0, r.RemovedOld.Length);
            Assert.IsTrue(r.OrderPreserved);
        }

        [Test]
        public void Insert_And_Remove()
        {
            var r = ListDiff.Compute(new[] { "a", "b", "c" }, new[] { "a", "x", "c", "d" });
            CollectionAssert.AreEqual(new[] { 0, -1, 2, -1 }, r.NewToOld);
            CollectionAssert.AreEqual(new[] { 1 }, r.RemovedOld);
            Assert.AreEqual(2, r.InsertedCount);
            Assert.IsFalse(r.IsIdentical);
            Assert.IsTrue(r.OrderPreserved);
        }

        [Test]
        public void Reorder_IsDetected()
        {
            var r = ListDiff.Compute(new[] { "a", "b", "c" }, new[] { "c", "a", "b" });
            CollectionAssert.AreEqual(new[] { 2, 0, 1 }, r.NewToOld);
            Assert.IsFalse(r.OrderPreserved);
            Assert.IsFalse(r.IsIdentical);
        }

        [Test]
        public void DuplicateKeys_MatchInOrderOfAppearance()
        {
            var r = ListDiff.Compute(new[] { "a", "a", "b" }, new[] { "a", "b", "a", "a" });
            CollectionAssert.AreEqual(new[] { 0, 2, 1, -1 }, r.NewToOld);
            Assert.AreEqual(0, r.RemovedOld.Length);
        }

        [Test]
        public void EmptyLists()
        {
            Assert.IsTrue(ListDiff.Compute(new string[0], new string[0]).IsIdentical);
            var cleared = ListDiff.Compute(new[] { "a" }, new string[0]);
            Assert.AreEqual(1, cleared.RemovedOld.Length);
            Assert.IsFalse(cleared.IsIdentical);
            var filled = ListDiff.Compute(new string[0], new[] { "a" });
            Assert.AreEqual(1, filled.InsertedCount);
        }

        [Test]
        public void NullKeys_AreSupported()
        {
            var r = ListDiff.Compute(new string[] { null, "a" }, new string[] { "a", null });
            CollectionAssert.AreEqual(new[] { 1, 0 }, r.NewToOld);
        }

        [Test]
        public void CustomComparer()
        {
            var r = ListDiff.Compute(new[] { "A" }, new[] { "a" }, System.StringComparer.OrdinalIgnoreCase);
            Assert.IsTrue(r.IsIdentical);
        }

        [Test]
        public void Arguments_AreValidated()
        {
            Assert.Throws<System.ArgumentNullException>(() => ListDiff.Compute<string>(null, new string[0]));
            Assert.Throws<System.ArgumentNullException>(() => ListDiff.Compute<string>(new string[0], null));
        }

        [Test]
        public void ManyRandomChanges_AlwaysProduceAValidPlan()
        {
            var rng = new System.Random(42);
            for (var round = 0; round < 200; round++)
            {
                var old = Keys(rng, rng.Next(0, 12));
                var next = Keys(rng, rng.Next(0, 12));
                var r = ListDiff.Compute(old, next);
                var used = new HashSet<int>();
                for (var i = 0; i < next.Count; i++)
                {
                    var o = r.NewToOld[i];
                    if (o < 0) continue;
                    Assert.AreEqual(old[o], next[i]);
                    Assert.IsTrue(used.Add(o), "an old row is reused once");
                }

                foreach (var removed in r.RemovedOld) Assert.IsFalse(used.Contains(removed));
                Assert.AreEqual(old.Count, used.Count + r.RemovedOld.Length);
            }
        }

        private static List<int> Keys(System.Random rng, int count)
        {
            var list = new List<int>();
            for (var i = 0; i < count; i++) list.Add(rng.Next(0, 6));
            return list;
        }
    }

    public class GridLayoutMathTests
    {
        [TestCase(300f, 100f, 0f, 3)]
        [TestCase(320f, 100f, 10f, 3)]
        [TestCase(319f, 100f, 10f, 2)]
        [TestCase(50f, 100f, 0f, 1)]
        [TestCase(0f, 100f, 0f, 1)]
        public void ColumnCount_FitsTheWidth(float width, float min, float spacing, int expected)
        {
            Assert.AreEqual(expected, GridLayoutMath.ColumnCount(width, min, spacing));
        }

        [Test]
        public void ColumnCount_RespectsMinAndMax()
        {
            Assert.AreEqual(2, GridLayoutMath.ColumnCount(100f, 100f, 0f, 2));
            Assert.AreEqual(4, GridLayoutMath.ColumnCount(2000f, 100f, 0f, 1, 4));
        }

        [Test]
        public void CellWidth_SharesTheWidth()
        {
            Assert.AreEqual(100f, GridLayoutMath.CellWidth(320f, 3, 10f), 1e-4f);
            Assert.AreEqual(50f, GridLayoutMath.CellWidth(50f, 1, 10f));
            Assert.AreEqual(0f, GridLayoutMath.CellWidth(5f, 3, 10f));
        }

        [TestCase(0, 3, 0)]
        [TestCase(1, 3, 1)]
        [TestCase(3, 3, 1)]
        [TestCase(4, 3, 2)]
        [TestCase(10, 0, 10)]
        public void RowCount(int items, int columns, int expected)
        {
            Assert.AreEqual(expected, GridLayoutMath.RowCount(items, columns));
        }

        [Test]
        public void ContentHeight()
        {
            Assert.AreEqual(0f, GridLayoutMath.ContentHeight(0, 50f, 10f));
            Assert.AreEqual(50f, GridLayoutMath.ContentHeight(1, 50f, 10f));
            Assert.AreEqual(170f, GridLayoutMath.ContentHeight(3, 50f, 10f));
        }

        [Test]
        public void VisibleRows_CoverTheWindow()
        {
            GridLayoutMath.VisibleRows(0f, 100f, 50f, 0f, 100, 0, out var first, out var last);
            Assert.AreEqual(0, first);
            Assert.AreEqual(2, last);

            GridLayoutMath.VisibleRows(125f, 100f, 50f, 0f, 100, 0, out first, out last);
            Assert.AreEqual(2, first);
            Assert.AreEqual(5, last);
        }

        [Test]
        public void VisibleRows_OverscanAndClamping()
        {
            GridLayoutMath.VisibleRows(500f, 100f, 50f, 0f, 12, 2, out var first, out var last);
            Assert.AreEqual(8, first);
            Assert.AreEqual(12, last, "clamped to the row count");

            GridLayoutMath.VisibleRows(0f, 100f, 50f, 0f, 100, 3, out first, out last);
            Assert.AreEqual(0, first);
            Assert.AreEqual(5, last);

            GridLayoutMath.VisibleRows(0f, 100f, 50f, 0f, 0, 3, out first, out last);
            Assert.AreEqual(0, last);
        }

        [Test]
        public void VisibleRows_ConsidersRowSpacing()
        {
            GridLayoutMath.VisibleRows(0f, 100f, 40f, 10f, 50, 0, out var first, out var last);
            Assert.AreEqual(0, first);
            Assert.AreEqual(2, last);
        }

        [Test]
        public void Move_InsideTheGrid()
        {
            // 3 columns, 8 items:  0 1 2 / 3 4 5 / 6 7
            Assert.AreEqual(1, GridLayoutMath.Move(0, GridDirection.Right, 3, 8));
            Assert.AreEqual(3, GridLayoutMath.Move(0, GridDirection.Down, 3, 8));
            Assert.AreEqual(1, GridLayoutMath.Move(4, GridDirection.Up, 3, 8));
            Assert.AreEqual(3, GridLayoutMath.Move(4, GridDirection.Left, 3, 8));
        }

        [Test]
        public void Move_StopsAtTheEdges_WithoutWrap()
        {
            Assert.AreEqual(0, GridLayoutMath.Move(0, GridDirection.Left, 3, 8));
            Assert.AreEqual(2, GridLayoutMath.Move(2, GridDirection.Right, 3, 8));
            Assert.AreEqual(1, GridLayoutMath.Move(1, GridDirection.Up, 3, 8));
            Assert.AreEqual(7, GridLayoutMath.Move(7, GridDirection.Right, 3, 8), "last item");
        }

        [Test]
        public void Move_WrapRows()
        {
            Assert.AreEqual(3, GridLayoutMath.Move(2, GridDirection.Right, 3, 8, true));
            Assert.AreEqual(2, GridLayoutMath.Move(3, GridDirection.Left, 3, 8, true));
            Assert.AreEqual(0, GridLayoutMath.Move(0, GridDirection.Left, 3, 8, true));
            Assert.AreEqual(7, GridLayoutMath.Move(7, GridDirection.Right, 3, 8, true));
        }

        [Test]
        public void Move_DownFromAShortLastRow_GoesToTheLastItem()
        {
            // 0 1 2 / 3 4 5 / 6 7: index 5 is above nothing; down goes to 7
            Assert.AreEqual(7, GridLayoutMath.Move(5, GridDirection.Down, 3, 8));
            Assert.AreEqual(7, GridLayoutMath.Move(7, GridDirection.Down, 3, 8));
        }

        [Test]
        public void Move_EmptyGrid()
        {
            Assert.AreEqual(-1, GridLayoutMath.Move(0, GridDirection.Down, 3, 0));
        }

        [Test]
        public void Move_NeverLeavesTheRange_ForAnyStart()
        {
            for (var count = 1; count < 12; count++)
            for (var columns = 1; columns < 6; columns++)
            for (var index = 0; index < count; index++)
            foreach (GridDirection d in System.Enum.GetValues(typeof(GridDirection)))
            {
                var next = GridLayoutMath.Move(index, d, columns, count);
                Assert.That(next, Is.InRange(0, count - 1));
            }
        }
    }
}
