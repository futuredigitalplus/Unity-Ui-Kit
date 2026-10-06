using NUnit.Framework;

namespace FutureDigitalPlus.UIKit.Interaction.Tests
{
    public class BackKeyDeduperTests
    {
        [Test]
        public void TheFirstReportOfAFrameIsAccepted_TheRestAreNot()
        {
            var deduper = new BackKeyDeduper();
            Assert.IsTrue(deduper.TryAccept(10));
            Assert.IsFalse(deduper.TryAccept(10));
            Assert.IsFalse(deduper.TryAccept(10));
        }

        [Test]
        public void ANewFrameIsAccepted()
        {
            var deduper = new BackKeyDeduper();
            deduper.TryAccept(10);
            Assert.IsTrue(deduper.TryAccept(11));
            Assert.IsFalse(deduper.TryAccept(11));
        }

        [Test]
        public void FrameZeroAndNegativeFramesWork()
        {
            var deduper = new BackKeyDeduper();
            Assert.IsTrue(deduper.TryAccept(0));
            Assert.IsTrue(deduper.TryAccept(-1));
        }

        [Test]
        public void Reset_AcceptsTheSameFrameAgain()
        {
            var deduper = new BackKeyDeduper();
            deduper.TryAccept(5);
            deduper.Reset();
            Assert.IsTrue(deduper.TryAccept(5));
        }
    }
}
