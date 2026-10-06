using NUnit.Framework;

namespace FutureDigitalPlus.UIKit.Foundation.Tests
{
    [TestFixture]
    public sealed class UiProfileTests
    {
        [TestCase(410f, 890f, UiOrientation.Portrait, UiSizeClass.Compact)]
        [TestCase(890f, 410f, UiOrientation.Landscape, UiSizeClass.Compact)]
        [TestCase(1010f, 1450f, UiOrientation.Portrait, UiSizeClass.Expanded)]
        [TestCase(1450f, 1010f, UiOrientation.Landscape, UiSizeClass.Expanded)]
        [TestCase(600f, 900f, UiOrientation.Portrait, UiSizeClass.Regular)]
        [TestCase(599f, 900f, UiOrientation.Portrait, UiSizeClass.Compact)]
        [TestCase(839f, 900f, UiOrientation.Portrait, UiSizeClass.Regular)]
        [TestCase(840f, 900f, UiOrientation.Portrait, UiSizeClass.Expanded)]
        [TestCase(700f, 700f, UiOrientation.Landscape, UiSizeClass.Regular)]
        [TestCase(1920f, 1080f, UiOrientation.Landscape, UiSizeClass.Expanded)]
        public void Compute_ClassifiesOrientationAndSizeClass(float width, float height, UiOrientation orientation, UiSizeClass size)
        {
            UiProfile profile = UiProfile.Compute(width, height, UiInputMode.Pointer);
            Assert.That(profile.Orientation, Is.EqualTo(orientation));
            Assert.That(profile.SizeClass, Is.EqualTo(size));
        }

        [TestCase(0f, 100f)]
        [TestCase(100f, 0f)]
        [TestCase(float.NaN, 100f)]
        [TestCase(100f, float.NaN)]
        [TestCase(-5f, 100f)]
        public void Compute_WithAnUnusableSize_GivesLandscapeCompact(float width, float height)
        {
            UiProfile profile = UiProfile.Compute(width, height, UiInputMode.Touch);
            Assert.That(profile.Orientation, Is.EqualTo(UiOrientation.Landscape));
            Assert.That(profile.SizeClass, Is.EqualTo(UiSizeClass.Compact));
            Assert.That(profile.InputMode, Is.EqualTo(UiInputMode.Touch));
        }

        [TestCase(599f, 900f, false)]
        [TestCase(600f, 900f, true)]
        [TestCase(839f, 900f, true)]
        [TestCase(840f, 900f, true)]
        [TestCase(1366f, 1024f, true)]
        [TestCase(1366f, 500f, false)]
        [TestCase(float.NaN, 900f, false)]
        [TestCase(0f, 0f, false)]
        public void IsRegularOrLarger_IsTrueFromTheRegularTierUp(float width, float height, bool expected)
        {
            Assert.That(UiProfile.Compute(width, height, UiInputMode.Pointer).IsRegularOrLarger, Is.EqualTo(expected));
        }

        [Test]
        public void IsRegularOrLarger_IsTheSameAnswerAsNotCompact()
        {
            foreach (UiSizeClass size in new[] { UiSizeClass.Compact, UiSizeClass.Regular, UiSizeClass.Expanded })
                Assert.That(new UiProfile(UiOrientation.Portrait, size, UiInputMode.Touch).IsRegularOrLarger, Is.EqualTo(size != UiSizeClass.Compact), size.ToString());
        }

        [Test]
        public void TheSizeClassFollowsTheShortSide_SoASplitViewOrAWindowGetsTheRightTier()
        {
            Assert.That(UiProfile.Compute(1366f, 500f, UiInputMode.Pointer).SizeClass, Is.EqualTo(UiSizeClass.Compact), "a wide but short window is compact");
        }

        [Test]
        public void Equality_ComparesAllThreeFields()
        {
            Assert.That(UiProfile.Compute(410f, 890f, UiInputMode.Touch), Is.EqualTo(UiProfile.Compute(400f, 800f, UiInputMode.Touch)));
            Assert.That(UiProfile.Compute(410f, 890f, UiInputMode.Touch), Is.Not.EqualTo(UiProfile.Compute(890f, 410f, UiInputMode.Touch)));
            Assert.That(UiProfile.Compute(410f, 890f, UiInputMode.Touch), Is.Not.EqualTo(UiProfile.Compute(410f, 890f, UiInputMode.Gamepad)));
            Assert.That(UiProfile.Compute(410f, 890f, UiInputMode.Touch).GetHashCode(), Is.EqualTo(UiProfile.Compute(400f, 800f, UiInputMode.Touch).GetHashCode()));
        }

        [Test]
        public void ToString_NamesTheThreeParts()
        {
            Assert.That(UiProfile.Compute(410f, 890f, UiInputMode.Gamepad).ToString(), Is.EqualTo("Portrait/Compact/Gamepad"));
        }
    }
}
