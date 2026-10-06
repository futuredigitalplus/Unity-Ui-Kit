using NUnit.Framework;

namespace FutureDigitalPlus.UIKit.Controls.Tests
{
    /// <summary>Controls math fed numbers from a panel that has not been laid out yet (NaN) or that a game computed badly.</summary>
    public class HostileInputTests
    {
        [Test]
        public void PopupPlacement_NaNContainerOrPosition_StaysAtTheMargin()
        {
            var nan = float.NaN;
            var atPoint = PopupPlacement.AtPoint(nan, nan, 100f, 50f, nan, nan);
            Assert.AreEqual(8f, atPoint.X);
            Assert.AreEqual(8f, atPoint.Y);

            var beside = PopupPlacement.BesideRect(nan, nan, nan, nan, 100f, 50f, 800f, 600f);
            Assert.IsFalse(float.IsNaN(beside.X) || float.IsNaN(beside.Y));
            beside = PopupPlacement.BesideRect(10f, 10f, 50f, 20f, 100f, 50f, nan, nan, PopupSide.Right);
            Assert.IsFalse(float.IsNaN(beside.X) || float.IsNaN(beside.Y));
        }

        [Test]
        public void RewardSequence_NaNStep_DoesNotStallTheReveal()
        {
            var sequence = new RewardSequence(3, 0.1f, 0.1f);
            sequence.Tick(float.NaN);
            sequence.Tick(float.NegativeInfinity);
            Assert.AreEqual(0, sequence.RevealedCount);
            sequence.Tick(1f);
            Assert.IsTrue(sequence.IsComplete);
        }

        [Test]
        public void GridMath_NaNAndHugeSizes_StayInsideTheirBounds()
        {
            Assert.AreEqual(1, GridLayoutMath.ColumnCount(float.NaN, 100f, 8f));
            Assert.AreEqual(3, GridLayoutMath.ColumnCount(float.NaN, 100f, 8f, 3, 6));
            var columns = GridLayoutMath.ColumnCount(float.PositiveInfinity, 100f, 8f, 1, 12);
            Assert.That(columns, Is.InRange(1, 12));
            GridLayoutMath.VisibleRows(float.NaN, float.NaN, 40f, 4f, 100, 2, out var first, out var last);
            Assert.That(first, Is.InRange(0, 100));
            Assert.That(last, Is.InRange(first, 100));
            GridLayoutMath.VisibleRows(1e30f, 600f, 40f, 4f, 100, 2, out first, out last);
            Assert.That(first, Is.InRange(0, 100));
            Assert.That(last, Is.InRange(first, 100));
        }

        [Test]
        public void SheetSnap_NaNInput_StillPicksASnapPoint()
        {
            var points = new[] { 0.3f, 0.6f, 1f };
            var result = SheetSnap.Resolve(float.NaN, float.NaN, points);
            Assert.That(result.Index, Is.InRange(-1, 2));
            Assert.IsFalse(float.IsNaN(result.Target));
            Assert.Throws<System.ArgumentException>(() => SheetSnap.Resolve(0.5f, 0f, new float[0]));
            Assert.Throws<System.ArgumentException>(() => SheetSnap.Resolve(0.5f, 0f, null));
        }

        [Test]
        public void BadgeTree_HostilePaths_NeverThrow()
        {
            var tree = new BadgeTree();
            foreach (var path in new[] { null, "", "/", "//", "a//b", "/a/", " ", "a/ /b", new string('x', 5000) })
            {
                tree.SetCount(path, 3);
                tree.SetDot(path, true);
                tree.Add(path, int.MaxValue);
                tree.Add(path, int.MaxValue);
                Assert.GreaterOrEqual(tree.Get(path).Count, 0, "never a negative badge for '" + path + "'");
                tree.Visit(path);
            }
            tree.Clear();
            Assert.IsFalse(tree.Get(null).IsVisible);
        }

        [Test]
        public void ListDiff_HostileKeys_DoNotThrow()
        {
            var old = new string[] { null, "a", "a", null, "b" };
            var next = new string[] { "a", null, null, null, "c", "a", "a" };
            var diff = ListDiff.Compute(old, next);
            Assert.AreEqual(next.Length, diff.NewToOld.Length);
            Assert.IsFalse(ListDiff.Compute(new string[0], new string[0]).InsertedCount > 0);
        }
    }
}
