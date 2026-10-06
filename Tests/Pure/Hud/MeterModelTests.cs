using NUnit.Framework;

namespace FutureDigitalPlus.UIKit.Hud.Tests
{
    public class MeterModelTests
    {
        [Test]
        public void New_IsFullByDefault()
        {
            var m = new MeterModel(200f);
            Assert.AreEqual(200f, m.Value);
            Assert.AreEqual(1f, m.Fraction);
            Assert.AreEqual(1f, m.TrailFraction);
            Assert.AreEqual(MeterLevel.Normal, m.Level);
        }

        [Test]
        public void SetValue_Clamps()
        {
            var m = new MeterModel(100f);
            m.SetValue(-5f);
            Assert.AreEqual(0f, m.Value);
            m.SetValue(500f);
            Assert.AreEqual(100f, m.Value);
            m.SetValue(float.NaN);
            Assert.AreEqual(0f, m.Value);
        }

        [Test]
        public void ZeroMax_HasZeroFraction()
        {
            var m = new MeterModel(0f);
            Assert.AreEqual(0f, m.Fraction);
        }

        [Test]
        public void Decrease_TrailWaitsThenFollows()
        {
            var m = new MeterModel(100f) { TrailDelaySeconds = 0.5f, TrailSpeed = 1f };
            m.SetValue(60f);
            Assert.AreEqual(1f, m.TrailFraction, 1e-5f);
            Assert.IsTrue(m.IsTrailMoving);

            Assert.IsTrue(m.Tick(0.4f));
            Assert.AreEqual(1f, m.TrailFraction, 1e-5f, "still holding");

            Assert.IsTrue(m.Tick(0.2f)); // 0.1 s of hold left over, then 0.1 s of movement
            Assert.AreEqual(0.9f, m.TrailFraction, 1e-4f);

            Assert.IsFalse(m.Tick(1f));
            Assert.AreEqual(0.6f, m.TrailFraction, 1e-5f);
            Assert.IsFalse(m.IsTrailMoving);
        }

        [Test]
        public void SecondHit_RestartsTheHold()
        {
            var m = new MeterModel(100f) { TrailDelaySeconds = 0.5f, TrailSpeed = 1f };
            m.SetValue(80f);
            m.Tick(0.5f);
            m.SetValue(60f);
            m.Tick(0.4f);
            Assert.AreEqual(1f, m.TrailFraction, 1e-5f, "hold restarted by the second hit");
        }

        [Test]
        public void Increase_DoesNotLeaveTrailBelowValue()
        {
            var m = new MeterModel(100f);
            m.SetValue(20f);
            m.Tick(10f);
            m.SetValue(70f);
            Assert.AreEqual(0.7f, m.TrailFraction, 1e-5f);
            Assert.IsFalse(m.IsTrailMoving);
        }

        [Test]
        public void Set_SnapsTrail()
        {
            var m = new MeterModel(100f);
            m.Set(10f, 50f);
            Assert.AreEqual(10f, m.Value);
            Assert.AreEqual(50f, m.Max);
            Assert.AreEqual(0.2f, m.TrailFraction, 1e-5f);
        }

        [Test]
        public void Tick_WithoutTrail_ReturnsFalse()
        {
            var m = new MeterModel(100f);
            Assert.IsFalse(m.Tick(1f));
        }

        [Test]
        public void Level_HasHysteresis()
        {
            var m = new MeterModel(100f) { WarningThreshold = 0.5f, DangerThreshold = 0.25f, Hysteresis = 0.05f };
            MeterLevel last = MeterLevel.Normal;
            var changes = 0;
            m.LevelChanged += l => { last = l; changes++; };

            m.SetValue(49f);
            Assert.AreEqual(MeterLevel.Warning, m.Level);
            m.SetValue(52f); // inside the band: stays
            Assert.AreEqual(MeterLevel.Warning, m.Level);
            m.SetValue(56f);
            Assert.AreEqual(MeterLevel.Normal, m.Level);
            m.SetValue(20f);
            Assert.AreEqual(MeterLevel.Danger, m.Level);
            m.SetValue(28f); // inside the danger band
            Assert.AreEqual(MeterLevel.Danger, m.Level);
            m.SetValue(31f); // left danger, still below warning
            Assert.AreEqual(MeterLevel.Warning, m.Level);
            Assert.AreEqual(MeterLevel.Warning, last);
            Assert.AreEqual(4, changes, "Warning, Normal, Danger, Warning");
        }

        [Test]
        public void Level_FlickerAroundAThreshold_DoesNotToggle()
        {
            var m = new MeterModel(100f) { WarningThreshold = 0.5f, Hysteresis = 0.05f };
            var changes = 0;
            m.LevelChanged += _ => changes++;
            for (var i = 0; i < 20; i++) m.SetValue(i % 2 == 0 ? 49f : 51f);
            Assert.AreEqual(1, changes);
        }

        [Test]
        public void Level_HighIsBad_UsesTheOtherEnd()
        {
            var m = new MeterModel(100f, 0f) { Mode = MeterThresholdMode.HighIsBad };
            m.SetValue(10f);
            Assert.AreEqual(MeterLevel.Normal, m.Level);
            m.SetValue(60f);
            Assert.AreEqual(MeterLevel.Warning, m.Level);
            m.SetValue(90f);
            Assert.AreEqual(MeterLevel.Danger, m.Level);
        }

        [Test]
        public void Level_StartsCorrectWhenCreatedLow()
        {
            var m = new MeterModel(100f, 10f);
            Assert.AreEqual(MeterLevel.Danger, m.Level);
        }
    }
}
