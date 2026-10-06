using NUnit.Framework;

namespace FutureDigitalPlus.UIKit.Hud.Tests
{
    public class CooldownModelTests
    {
        [Test]
        public void New_IsReady()
        {
            var c = new CooldownModel();
            Assert.IsTrue(c.IsReady);
            Assert.AreEqual(0f, c.Fraction);
            Assert.IsFalse(c.Tick(1f), "no edge without a cooldown");
        }

        [Test]
        public void Start_CountsDown_AndReportsTheReadyEdgeOnce()
        {
            var c = new CooldownModel();
            c.Start(2f);
            Assert.IsFalse(c.IsReady);
            Assert.AreEqual(1f, c.Fraction);

            Assert.IsFalse(c.Tick(0.5f));
            Assert.AreEqual(0.75f, c.Fraction, 1e-5f);
            Assert.AreEqual(1.5f, c.Remaining, 1e-5f);

            Assert.IsTrue(c.Tick(5f), "reaching zero is the edge");
            Assert.IsTrue(c.IsReady);
            Assert.AreEqual(0f, c.Remaining);
            Assert.IsFalse(c.Tick(1f), "only once");
        }

        [Test]
        public void Start_ZeroDuration_IsReady()
        {
            var c = new CooldownModel();
            c.Start(0f);
            Assert.IsTrue(c.IsReady);
        }

        [Test]
        public void Reduce_And_Reset()
        {
            var c = new CooldownModel();
            c.Start(10f);
            Assert.IsFalse(c.Reduce(4f));
            Assert.AreEqual(6f, c.Remaining, 1e-5f);
            Assert.IsTrue(c.Reduce(100f));
            Assert.IsFalse(c.Reduce(1f));

            c.Start(3f);
            Assert.IsTrue(c.Reset());
            Assert.IsFalse(c.Reset());
            Assert.IsTrue(c.IsReady);
        }

        [Test]
        public void Restart_ResetsTheFraction()
        {
            var c = new CooldownModel();
            c.Start(4f);
            c.Tick(3f);
            c.Start(8f);
            Assert.AreEqual(1f, c.Fraction);
        }
    }

    public class CountUpModelTests
    {
        [Test]
        public void Snap_SetsValueWithoutRunning()
        {
            var c = new CountUpModel();
            c.Snap(42);
            Assert.AreEqual(42d, c.Current);
            Assert.IsFalse(c.IsRunning);
        }

        [Test]
        public void AnimateTo_ReachesTargetExactlyAtTheEnd()
        {
            var c = new CountUpModel();
            c.AnimateTo(100d, 1f, CountEasing.Linear);
            Assert.IsTrue(c.IsRunning);
            Assert.IsTrue(c.Tick(0.25f));
            Assert.AreEqual(25d, c.Current, 1e-6);
            Assert.IsTrue(c.Tick(0.25f));
            Assert.AreEqual(50d, c.Current, 1e-6);
            Assert.IsTrue(c.Tick(5f));
            Assert.AreEqual(100d, c.Current);
            Assert.IsFalse(c.IsRunning);
            Assert.IsFalse(c.Tick(1f));
        }

        [Test]
        public void AnimateTo_ZeroDuration_Snaps()
        {
            var c = new CountUpModel();
            c.AnimateTo(10d, 0f);
            Assert.AreEqual(10d, c.Current);
            Assert.IsFalse(c.IsRunning);
        }

        [Test]
        public void AnimateTo_WhileRunning_ContinuesFromCurrent()
        {
            var c = new CountUpModel();
            c.AnimateTo(100d, 1f, CountEasing.Linear);
            c.Tick(0.5f);
            c.AnimateTo(0d, 1f, CountEasing.Linear);
            c.Tick(0.5f);
            Assert.AreEqual(25d, c.Current, 1e-6);
        }

        [Test]
        public void AnimateTo_SameValue_DoesNotRun()
        {
            var c = new CountUpModel();
            c.Snap(5d);
            c.AnimateTo(5d, 1f);
            Assert.IsFalse(c.IsRunning);
        }

        [TestCase(CountEasing.Linear)]
        [TestCase(CountEasing.EaseOutQuad)]
        [TestCase(CountEasing.EaseOutCubic)]
        [TestCase(CountEasing.EaseInOutQuad)]
        public void Ease_IsMonotonicAndHitsTheEnds(CountEasing e)
        {
            Assert.AreEqual(0f, CountUpModel.Ease(0f, e));
            Assert.AreEqual(1f, CountUpModel.Ease(1f, e));
            var prev = 0f;
            for (var i = 1; i <= 100; i++)
            {
                var v = CountUpModel.Ease(i / 100f, e);
                Assert.GreaterOrEqual(v, prev - 1e-6f);
                prev = v;
            }
        }

        [Test]
        public void EaseOut_IsAheadOfLinearAtTheMiddle()
        {
            Assert.Greater(CountUpModel.Ease(0.5f, CountEasing.EaseOutCubic), 0.5f);
            Assert.AreEqual(0.5f, CountUpModel.Ease(0.5f, CountEasing.EaseInOutQuad), 1e-6f);
        }
    }
}
