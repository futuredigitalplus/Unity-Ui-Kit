using System.Collections.Generic;
using NUnit.Framework;

namespace FutureDigitalPlus.UIKit.Controls.Tests
{
    public class RewardSequenceTests
    {
        [Test]
        public void RevealsOneByOne_AfterTheInitialDelay()
        {
            var s = new RewardSequence(3, 0.5f, 0.25f);
            var revealed = new List<int>();
            s.Revealed += revealed.Add;

            Assert.IsFalse(s.Tick(0.4f));
            Assert.AreEqual(0, s.RevealedCount);
            Assert.IsTrue(s.Tick(0.2f)); // 0.6: first reward at 0.5
            Assert.AreEqual(1, s.RevealedCount);
            Assert.IsFalse(s.Tick(0.1f)); // 0.7: second at 0.75
            Assert.IsTrue(s.Tick(0.1f)); // 0.8
            Assert.AreEqual(2, s.RevealedCount);
            Assert.IsTrue(s.Tick(1f));
            Assert.IsTrue(s.IsComplete);
            CollectionAssert.AreEqual(new[] { 0, 1, 2 }, revealed);
            Assert.IsFalse(s.WasSkipped);
        }

        [Test]
        public void ALongTick_RevealsSeveralAtOnce_InOrder()
        {
            var s = new RewardSequence(4, 0f, 0.1f);
            var revealed = new List<int>();
            s.Revealed += revealed.Add;
            Assert.IsTrue(s.Tick(10f));
            CollectionAssert.AreEqual(new[] { 0, 1, 2, 3 }, revealed);
        }

        [Test]
        public void Skip_RevealsTheRest_AndFlagsIt()
        {
            var s = new RewardSequence(5, 1f, 1f);
            var revealed = new List<int>();
            s.Revealed += revealed.Add;
            s.Tick(1.1f);
            s.Skip();
            Assert.IsTrue(s.IsComplete);
            Assert.IsTrue(s.WasSkipped);
            CollectionAssert.AreEqual(new[] { 0, 1, 2, 3, 4 }, revealed);
            s.Skip();
            Assert.AreEqual(5, revealed.Count, "a second skip does nothing");
        }

        [Test]
        public void Skip_OnACompleteSequence_IsNotCountedAsSkipped()
        {
            var s = new RewardSequence(1, 0f, 0f);
            s.Tick(1f);
            s.Skip();
            Assert.IsFalse(s.WasSkipped);
        }

        [Test]
        public void EmptySequence_IsCompleteAtOnce()
        {
            var s = new RewardSequence(0);
            Assert.IsTrue(s.IsComplete);
            Assert.IsFalse(s.Tick(1f));
        }

        [Test]
        public void IsRevealed_FollowsTheCount()
        {
            var s = new RewardSequence(3, 0f, 1f);
            s.Tick(1.5f);
            Assert.IsTrue(s.IsRevealed(0));
            Assert.IsTrue(s.IsRevealed(1));
            Assert.IsFalse(s.IsRevealed(2));
            Assert.IsFalse(s.IsRevealed(-1));
            Assert.IsFalse(s.IsRevealed(3));
        }
    }

    public class PresentationQueueTests
    {
        [Test]
        public void FirstItemBecomesCurrentAtOnce_OthersWait()
        {
            var q = new PresentationQueue<string>();
            var shown = new List<string>();
            q.CurrentChanged += shown.Add;
            q.Enqueue("a");
            q.Enqueue("b");
            q.Enqueue("c");
            Assert.AreEqual("a", q.Current);
            Assert.AreEqual(2, q.WaitingCount);
            CollectionAssert.AreEqual(new[] { "a" }, shown);
        }

        [Test]
        public void Complete_MovesOn_ThenDrains()
        {
            var q = new PresentationQueue<string>();
            var drained = 0;
            q.Drained += () => drained++;
            q.Enqueue("a");
            q.Enqueue("b");
            q.Complete();
            Assert.AreEqual("b", q.Current);
            Assert.AreEqual(0, drained);
            q.Complete();
            Assert.IsFalse(q.HasCurrent);
            Assert.AreEqual(1, drained);
            q.Complete();
            Assert.AreEqual(1, drained, "nothing to complete");
        }

        [Test]
        public void EnqueueAfterDrain_StartsAgain()
        {
            var q = new PresentationQueue<int>();
            q.Enqueue(1);
            q.Complete();
            q.Enqueue(2);
            Assert.AreEqual(2, q.Current);
        }

        [Test]
        public void Clear_DropsEverythingWithoutDrained()
        {
            var q = new PresentationQueue<int>();
            var drained = 0;
            q.Drained += () => drained++;
            q.Enqueue(1);
            q.Enqueue(2);
            q.Clear();
            Assert.IsFalse(q.HasCurrent);
            Assert.AreEqual(0, q.WaitingCount);
            Assert.AreEqual(0, drained);
        }
    }

    public class SheetSnapTests
    {
        private static readonly float[] OnePoint = { 1f };
        private static readonly float[] ThreePoints = { 0.35f, 0.7f, 1f };

        [Test]
        public void SlowRelease_SettlesAtTheNearestPoint()
        {
            var r = SheetSnap.Resolve(0.6f, 0f, ThreePoints);
            Assert.IsFalse(r.Dismiss);
            Assert.AreEqual(1, r.Index);
            Assert.AreEqual(0.7f, r.Target);
        }

        [Test]
        public void ReleasedLow_Dismisses()
        {
            var r = SheetSnap.Resolve(0.1f, 0f, ThreePoints);
            Assert.IsTrue(r.Dismiss);
            Assert.AreEqual(-1, r.Index);
            Assert.AreEqual(0f, r.Target);
        }

        [Test]
        public void ReleasedLow_ButNotDismissable_GoesToTheLowestPoint()
        {
            var r = SheetSnap.Resolve(0.1f, 0f, ThreePoints, dismissable: false);
            Assert.IsFalse(r.Dismiss);
            Assert.AreEqual(0, r.Index);
        }

        [Test]
        public void FlickUp_MovesToTheNextPointAbove()
        {
            var r = SheetSnap.Resolve(0.4f, 2f, ThreePoints);
            Assert.AreEqual(1, r.Index, "next above 0.4 is 0.7, not the top");
            r = SheetSnap.Resolve(0.7f, 2f, ThreePoints);
            Assert.AreEqual(2, r.Index);
            r = SheetSnap.Resolve(1f, 2f, ThreePoints);
            Assert.AreEqual(2, r.Index, "already at the top");
        }

        [Test]
        public void FlickDown_MovesToTheNextPointBelow_OrDismissesFromTheLowest()
        {
            var r = SheetSnap.Resolve(1f, -2f, ThreePoints);
            Assert.AreEqual(1, r.Index);
            r = SheetSnap.Resolve(0.7f, -2f, ThreePoints);
            Assert.AreEqual(0, r.Index);
            r = SheetSnap.Resolve(0.35f, -2f, ThreePoints);
            Assert.IsTrue(r.Dismiss);
            r = SheetSnap.Resolve(0.35f, -2f, ThreePoints, dismissable: false);
            Assert.IsFalse(r.Dismiss);
            Assert.AreEqual(0, r.Index);
        }

        [Test]
        public void SinglePoint_SnapsBackOrDismisses()
        {
            Assert.AreEqual(0, SheetSnap.Resolve(0.8f, 0f, OnePoint).Index);
            Assert.IsTrue(SheetSnap.Resolve(0.3f, 0f, OnePoint).Dismiss);
            Assert.IsTrue(SheetSnap.Resolve(1f, -3f, OnePoint).Dismiss);
            Assert.IsFalse(SheetSnap.Resolve(1f, 3f, OnePoint).Dismiss);
        }

        [Test]
        public void ModerateVelocity_ProjectsTheRelease()
        {
            // below the flick speed: 0.5 plus 0.5 * 0.12 projects to 0.56, nearest point is 0.7
            Assert.AreEqual(1, SheetSnap.Resolve(0.5f, 0.5f, ThreePoints).Index);
        }

        [Test]
        public void NoSnapPoints_Throws()
        {
            Assert.Throws<System.ArgumentException>(() => SheetSnap.Resolve(0.5f, 0f, new float[0]));
            Assert.Throws<System.ArgumentException>(() => SheetSnap.Resolve(0.5f, 0f, null));
        }
    }

    public class PopupPlacementTests
    {
        [Test]
        public void AtPoint_FitsWithoutFlipping()
        {
            var r = PopupPlacement.AtPoint(100f, 100f, 200f, 150f, 800f, 600f);
            Assert.AreEqual(100f, r.X);
            Assert.AreEqual(100f, r.Y);
            Assert.IsFalse(r.FlippedHorizontally);
            Assert.IsFalse(r.FlippedVertically);
        }

        [Test]
        public void AtPoint_FlipsLeftAndUpAtTheCorner()
        {
            var r = PopupPlacement.AtPoint(780f, 580f, 200f, 150f, 800f, 600f);
            Assert.IsTrue(r.FlippedHorizontally);
            Assert.IsTrue(r.FlippedVertically);
            Assert.AreEqual(580f, r.X);
            Assert.AreEqual(430f, r.Y);
        }

        [Test]
        public void AtPoint_WhenItFitsOnNeitherSide_IsClamped()
        {
            var r = PopupPlacement.AtPoint(100f, 100f, 700f, 500f, 800f, 600f, 8f);
            Assert.GreaterOrEqual(r.X, 8f);
            Assert.LessOrEqual(r.X + 700f, 800f - 8f + 0.001f);
            Assert.GreaterOrEqual(r.Y, 8f);
        }

        [Test]
        public void AtPoint_LargerThanTheContainer_PinsToTheMargin()
        {
            var r = PopupPlacement.AtPoint(50f, 50f, 1000f, 1000f, 800f, 600f, 8f);
            Assert.AreEqual(8f, r.X);
            Assert.AreEqual(8f, r.Y);
        }

        [Test]
        public void BesideRect_Below_AlignsTheLeftEdge()
        {
            var r = PopupPlacement.BesideRect(100f, 100f, 200f, 40f, 200f, 120f, 800f, 600f);
            Assert.AreEqual(100f, r.X);
            Assert.AreEqual(140f, r.Y);
            Assert.IsFalse(r.FlippedVertically);
        }

        [Test]
        public void BesideRect_Below_FlipsAboveNearTheBottom()
        {
            var r = PopupPlacement.BesideRect(100f, 540f, 200f, 40f, 200f, 200f, 800f, 600f);
            Assert.IsTrue(r.FlippedVertically);
            Assert.AreEqual(340f, r.Y);
        }

        [Test]
        public void BesideRect_Below_RightAlignsNearTheRightEdge()
        {
            var r = PopupPlacement.BesideRect(700f, 100f, 90f, 40f, 200f, 100f, 800f, 600f);
            Assert.AreEqual(590f, r.X, 0.001f);
        }

        [Test]
        public void BesideRect_Right_OpensASubmenu_AndFlipsLeftAtTheEdge()
        {
            var r = PopupPlacement.BesideRect(100f, 100f, 180f, 30f, 180f, 200f, 800f, 600f, PopupSide.Right);
            Assert.AreEqual(280f, r.X);
            Assert.AreEqual(100f, r.Y);
            Assert.IsFalse(r.FlippedHorizontally);

            r = PopupPlacement.BesideRect(600f, 100f, 180f, 30f, 180f, 200f, 800f, 600f, PopupSide.Right);
            Assert.IsTrue(r.FlippedHorizontally);
            Assert.AreEqual(420f, r.X);
        }

        [Test]
        public void BesideRect_Above_And_Left()
        {
            var above = PopupPlacement.BesideRect(100f, 300f, 200f, 40f, 200f, 100f, 800f, 600f, PopupSide.Above);
            Assert.AreEqual(200f, above.Y);
            var left = PopupPlacement.BesideRect(400f, 100f, 100f, 30f, 150f, 100f, 800f, 600f, PopupSide.Left);
            Assert.AreEqual(250f, left.X);
        }

        [Test]
        public void ResultsNeverLeaveTheContainer()
        {
            var rng = new System.Random(7);
            for (var i = 0; i < 500; i++)
            {
                var w = 50f + (float)rng.NextDouble() * 300f;
                var h = 50f + (float)rng.NextDouble() * 300f;
                var x = (float)rng.NextDouble() * 800f;
                var y = (float)rng.NextDouble() * 600f;
                var p = PopupPlacement.AtPoint(x, y, w, h, 800f, 600f, 8f);
                Assert.GreaterOrEqual(p.X, 8f - 0.001f);
                Assert.GreaterOrEqual(p.Y, 8f - 0.001f);
                Assert.LessOrEqual(p.X + w, 800f - 8f + 0.001f);
                Assert.LessOrEqual(p.Y + h, 600f - 8f + 0.001f);
                foreach (PopupSide side in System.Enum.GetValues(typeof(PopupSide)))
                {
                    var b = PopupPlacement.BesideRect(x, y, 80f, 30f, w, h, 800f, 600f, side, 8f);
                    Assert.GreaterOrEqual(b.X, 8f - 0.001f);
                    Assert.GreaterOrEqual(b.Y, 8f - 0.001f);
                    Assert.LessOrEqual(b.X + w, 800f - 8f + 0.001f);
                    Assert.LessOrEqual(b.Y + h, 600f - 8f + 0.001f);
                }
            }
        }
    }
}
