using NUnit.Framework;

namespace FutureDigitalPlus.UIKit.Hud.Tests
{
    public class CountdownModelTests
    {
        private double _now;

        [SetUp]
        public void SetUp() => _now = 1000d;

        private CountdownModel Create() => new CountdownModel(() => _now);

        [Test]
        public void Start_CountsDownOnTheInjectedClock()
        {
            var c = Create();
            Assert.IsFalse(c.IsRunning);
            Assert.AreEqual(0d, c.Remaining);
            c.Start(90d);
            Assert.AreEqual(90d, c.Remaining, 1e-9);
            _now += 30.5;
            Assert.AreEqual(59.5, c.Remaining, 1e-9);
            Assert.AreEqual(60L, c.RemainingWholeSeconds);
        }

        [Test]
        public void Poll_RaisesExpiredOnce()
        {
            var c = Create();
            var raised = 0;
            c.Expired += () => raised++;
            c.Start(10d);
            Assert.IsFalse(c.Poll());
            _now += 9.9;
            Assert.IsFalse(c.Poll());
            _now += 0.1;
            Assert.IsTrue(c.Poll());
            Assert.IsFalse(c.Poll());
            Assert.AreEqual(1, raised);
            Assert.IsTrue(c.IsExpired);
            Assert.IsFalse(c.IsRunning);
            Assert.AreEqual(0d, c.Remaining);
        }

        [Test]
        public void StartAt_UsesAbsoluteEndTime()
        {
            var c = Create();
            c.StartAt(_now + 5d);
            Assert.AreEqual(5d, c.Remaining, 1e-9);
        }

        [Test]
        public void StartAt_InThePast_ExpiresOnTheFirstPoll()
        {
            var c = Create();
            c.StartAt(_now - 1d);
            Assert.IsTrue(c.Poll());
        }

        [Test]
        public void Stop_DoesNotRaise_AndRestartWorks()
        {
            var c = Create();
            var raised = 0;
            c.Expired += () => raised++;
            c.Start(1d);
            c.Stop();
            _now += 5d;
            Assert.IsFalse(c.Poll());
            c.Start(1d);
            _now += 2d;
            Assert.IsTrue(c.Poll());
            Assert.AreEqual(1, raised);
        }

        [Test]
        public void NullClock_Throws()
        {
            Assert.Throws<System.ArgumentNullException>(() => new CountdownModel(null));
        }

        [TestCase(0d, "0s")]
        [TestCase(-5d, "0s")]
        [TestCase(0.2d, "1s")]
        [TestCase(45d, "45s")]
        [TestCase(59.01d, "01:00")]
        [TestCase(60d, "01:00")]
        [TestCase(135d, "02:15")]
        [TestCase(3599d, "59:59")]
        [TestCase(3600d, "1h 0m")]
        [TestCase(8100d, "2h 15m")]
        [TestCase(86399d, "23h 59m")]
        [TestCase(86400d, "1d 0h")]
        [TestCase(3 * 86400d + 4 * 3600d + 59d, "3d 4h")]
        public void Format_Compact(double seconds, string expected)
        {
            Assert.AreEqual(expected, CountdownModel.Format(seconds, CountdownStyle.Compact, CountdownUnits.Default));
        }

        [TestCase(0d, "00:00:00")]
        [TestCase(61d, "00:01:01")]
        [TestCase(2 * 86400d + 3600d + 5d, "49:00:05")]
        public void Format_Clock(double seconds, string expected)
        {
            Assert.AreEqual(expected, CountdownModel.Format(seconds, CountdownStyle.Clock, CountdownUnits.Default));
        }

        [Test]
        public void Format_CustomUnits()
        {
            var units = new CountdownUnits("T", "Std", "Min", "Sek", " ");
            Assert.AreEqual("2T 3Std", CountdownModel.Format(2 * 86400d + 3 * 3600d, CountdownStyle.Compact, units));
            Assert.AreEqual("30Sek", CountdownModel.Format(30d, CountdownStyle.Compact, units));
        }

        [Test]
        public void Instance_Format_UsesRemaining()
        {
            var c = Create();
            c.Start(135d);
            Assert.AreEqual("02:15", c.Format());
            Assert.AreEqual("00:02:15", c.Format(CountdownStyle.Clock));
        }
    }
}
