using NUnit.Framework;

namespace FutureDigitalPlus.UIKit.Interaction.Tests
{
    public class TextScaleStepsTests
    {
        [Test]
        public void All_ListsTheFiveSteps()
        {
            CollectionAssert.AreEqual(new[] { 100, 125, 150, 175, 200 }, TextScaleSteps.All);
        }

        [TestCase(100, 100)]
        [TestCase(112, 100)]
        [TestCase(113, 125)]
        [TestCase(125, 125)]
        [TestCase(137, 125)]
        [TestCase(138, 150)]
        [TestCase(160, 150)]
        [TestCase(163, 175)]
        [TestCase(199, 200)]
        [TestCase(200, 200)]
        public void Nearest_SnapsToTheClosestStep(int percent, int expected)
        {
            Assert.AreEqual(expected, TextScaleSteps.Nearest(percent));
        }

        [TestCase(0, 100)]
        [TestCase(99, 100)]
        [TestCase(-50, 100)]
        [TestCase(201, 200)]
        [TestCase(1000, 200)]
        public void Nearest_ClampsOutOfRangeInput(int percent, int expected)
        {
            Assert.AreEqual(expected, TextScaleSteps.Nearest(percent));
            Assert.AreEqual(expected, TextScaleSteps.Clamp(percent));
        }

        [Test]
        public void Effective_MultipliesPlayerAndSystemScale()
        {
            Assert.AreEqual(100, TextScaleSteps.Effective(100, 1f, true));
            Assert.AreEqual(130, TextScaleSteps.Effective(100, 1.3f, true));
            Assert.AreEqual(180, TextScaleSteps.Effective(120, 1.5f, true));
        }

        [Test]
        public void Effective_CanIgnoreTheSystemScale()
        {
            Assert.AreEqual(120, TextScaleSteps.Effective(120, 1.5f, false));
        }

        [TestCase(0f)]
        [TestCase(-2f)]
        public void Effective_TreatsAnInvalidSystemScaleAsOne(float systemScale)
        {
            Assert.AreEqual(140, TextScaleSteps.Effective(140, systemScale, true));
        }

        [Test]
        public void Effective_StaysInsideTheSupportedRange()
        {
            Assert.AreEqual(200, TextScaleSteps.Effective(150, 2f, true));
            Assert.AreEqual(100, TextScaleSteps.Effective(50, 1f, true));
        }
    }
}
