using System.Globalization;
using NUnit.Framework;

namespace FutureDigitalPlus.UIKit.Hud.Tests
{
    /// <summary>Models fed values a game should never send: NaN, infinities, negative and absurd numbers. None may poison the model or throw.</summary>
    public class HostileInputTests
    {
        private static readonly float[] BadFloats = { float.NaN, float.PositiveInfinity, float.NegativeInfinity, -1f, 0f, float.MaxValue, float.MinValue };

        [Test]
        public void Meter_NaNAndInfiniteInputs_NeverProduceNaNFractions()
        {
            foreach (var max in BadFloats)
            foreach (var value in BadFloats)
            {
                var meter = new MeterModel(max, value);
                Assert.IsFalse(float.IsNaN(meter.Fraction), "ctor " + max + "," + value);
                meter.Set(value, max);
                Assert.IsFalse(float.IsNaN(meter.Fraction), "Set " + value + "," + max);
                meter.SetValue(value);
                Assert.IsFalse(float.IsNaN(meter.Fraction), "SetValue " + value);
                Assert.IsFalse(float.IsNaN(meter.TrailFraction), "trail " + value + "," + max);
            }
        }

        [Test]
        public void Meter_NaNTimeStep_DoesNotPoisonTheTrail()
        {
            var meter = new MeterModel(100f);
            meter.SetValue(10f);
            meter.Tick(float.NaN);
            Assert.IsFalse(float.IsNaN(meter.TrailFraction));
            for (var i = 0; i < 100; i++) meter.Tick(0.1f);
            Assert.AreEqual(0.1f, meter.TrailFraction, 1e-5f, "the trail still catches up after a NaN step");
        }

        [Test]
        public void Cooldown_NaNDurationOrStep_IsReadyOrStillCountsDown()
        {
            var cooldown = new CooldownModel();
            cooldown.Start(float.NaN);
            Assert.IsTrue(cooldown.IsReady, "a NaN duration is no cooldown");

            cooldown.Start(2f);
            Assert.IsFalse(cooldown.Tick(float.NaN));
            Assert.AreEqual(2f, cooldown.Remaining, 1e-5f);
            Assert.IsTrue(cooldown.Tick(5f));
            Assert.IsTrue(cooldown.IsReady);
        }

        [Test]
        public void CountUp_NaNTargetDurationOrStep_NeverTurnsCurrentIntoNaN()
        {
            var count = new CountUpModel();
            count.Snap(10d);
            count.AnimateTo(double.NaN, 1f);
            Assert.AreEqual(10d, count.Current);
            count.AnimateTo(50d, float.NaN);
            Assert.AreEqual(50d, count.Current, "a NaN duration snaps");
            count.AnimateTo(100d, 1f);
            count.Tick(float.NaN);
            Assert.IsFalse(double.IsNaN(count.Current));
            Assert.IsTrue(count.IsRunning);
            count.Tick(2f);
            Assert.AreEqual(100d, count.Current);
        }

        [Test]
        public void Countdown_FormatOfHostileSeconds_ShowsZeroOrACap()
        {
            var units = CountdownUnits.Default;
            Assert.AreEqual("0s", CountdownModel.Format(double.NaN, CountdownStyle.Compact, units));
            Assert.AreEqual("0s", CountdownModel.Format(double.NegativeInfinity, CountdownStyle.Compact, units));
            Assert.AreEqual("00:00:00", CountdownModel.Format(double.NaN, CountdownStyle.Clock, units));
            foreach (var big in new[] { double.PositiveInfinity, double.MaxValue, 1e300 })
            {
                var compact = CountdownModel.Format(big, CountdownStyle.Compact, units);
                var clock = CountdownModel.Format(big, CountdownStyle.Clock, units);
                Assert.IsFalse(compact.StartsWith("-"), compact);
                Assert.IsFalse(clock.StartsWith("-"), clock);
                Assert.IsTrue(compact.EndsWith("h"), compact);
            }
        }

        [Test]
        public void Compact_WithAnEmptySuffixList_FallsBackToGrouping()
        {
            Assert.AreEqual("1,234,567", NumberFormatter.Compact(1234567d, 1, CultureInfo.InvariantCulture, 1000d, new string[0]));
        }

        [Test]
        public void FloatingText_NaNStep_DoesNotMakeTextsImmortal()
        {
            var model = new FloatingTextModel(4);
            model.BeginFrame();
            model.Spawn(0f, 0f, "hit");
            Assert.IsTrue(model.Tick(float.NaN));
            model.Tick(10f);
            Assert.AreEqual(0, model.ActiveCount);
        }

        [Test]
        public void TimedFeed_NaNStep_DoesNotMakeEntriesImmortal()
        {
            var feed = new TimedFeedModel();
            feed.Add("a");
            feed.Tick(float.NaN);
            feed.Tick(1000f);
            Assert.AreEqual(0, feed.Count);
        }

        [Test]
        public void WorldAnchor_NonFinitePosition_IsHiddenAtTheCentre()
        {
            foreach (var bad in new[] { float.NaN, float.PositiveInfinity, float.NegativeInfinity })
            {
                foreach (var result in new[]
                {
                    WorldAnchorMath.Resolve(bad, 10f, 5f, 0f, 0f, 800f, 600f, 8f, OffScreenMode.Clamp),
                    WorldAnchorMath.Resolve(10f, bad, 5f, 0f, 0f, 800f, 600f, 8f, OffScreenMode.Clamp),
                    WorldAnchorMath.Resolve(10f, 10f, float.NaN, 0f, 0f, 800f, 600f, 8f, OffScreenMode.Clamp),
                })
                {
                    Assert.IsFalse(result.Show, "an anchor with no position is not shown");
                    Assert.IsFalse(float.IsNaN(result.X) || float.IsNaN(result.Y), "no NaN position for " + bad);
                    Assert.IsFalse(float.IsInfinity(result.X) || float.IsInfinity(result.Y));
                }
            }
        }
    }
}
