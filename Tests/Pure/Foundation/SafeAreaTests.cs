using NUnit.Framework;

namespace FutureDigitalPlus.UIKit.Foundation.Tests
{
    [TestFixture]
    public sealed class SafeAreaTests
    {
        [Test]
        public void FromScreen_IPhoneLandscapeWithNotch()
        {
            // iPhone 15 Pro landscape in pixels: 2556 x 1179, safe area x 132 y 63 w 2292 h 1116 (bottom left origin).
            SafeAreaInsets insets = SafeAreaMath.FromScreen(2556f, 1179f, 132f, 63f, 2292f, 1116f);
            Assert.That(insets.Left, Is.EqualTo(132f).Within(0.01f));
            Assert.That(insets.Right, Is.EqualTo(132f).Within(0.01f));
            Assert.That(insets.Bottom, Is.EqualTo(63f).Within(0.01f));
            Assert.That(insets.Top, Is.EqualTo(0f).Within(0.01f));
        }

        [Test]
        public void FromScreen_IPhonePortraitWithIslandAndHomeIndicator()
        {
            // 1179 x 2556 pixels, safe area y 102 (home indicator), height 2253 (island 201 at the top).
            SafeAreaInsets insets = SafeAreaMath.FromScreen(1179f, 2556f, 0f, 102f, 1179f, 2253f);
            Assert.That(insets.Left, Is.EqualTo(0f));
            Assert.That(insets.Right, Is.EqualTo(0f));
            Assert.That(insets.Bottom, Is.EqualTo(102f));
            Assert.That(insets.Top, Is.EqualTo(201f));
        }

        [Test]
        public void FromScreen_FullScreenGivesZero()
        {
            Assert.That(SafeAreaMath.FromScreen(1920f, 1080f, 0f, 0f, 1920f, 1080f), Is.EqualTo(SafeAreaInsets.Zero));
        }

        [TestCase(0f, 100f, 0f, 0f, 0f, 100f)]
        [TestCase(100f, 100f, 0f, 0f, 0f, 0f)]
        [TestCase(100f, 100f, float.NaN, 0f, 100f, 100f)]
        [TestCase(0f, 0f, 0f, 0f, 0f, 0f)]
        public void FromScreen_WithAnEmptyOrBrokenRectangle_GivesZero(float screenW, float screenH, float x, float y, float w, float h)
        {
            SafeAreaInsets insets = SafeAreaMath.FromScreen(screenW, screenH, x, y, w, h);
            Assert.That(insets, Is.EqualTo(SafeAreaInsets.Zero));
        }

        [Test]
        public void FromScreen_ARectangleLargerThanTheScreen_NeverGivesNegativeInsets()
        {
            SafeAreaInsets insets = SafeAreaMath.FromScreen(100f, 100f, 0f, 0f, 200f, 200f);
            Assert.That(insets.Left, Is.GreaterThanOrEqualTo(0f));
            Assert.That(insets.Right, Is.GreaterThanOrEqualTo(0f));
            Assert.That(insets.Top, Is.GreaterThanOrEqualTo(0f));
            Assert.That(insets.Bottom, Is.GreaterThanOrEqualTo(0f));
        }

        [Test]
        public void ToPanel_ScalesPixelsToPanelUnits()
        {
            // 3x phone at 160 dpi reference: Screen.dpi 460 gives 2.875 pixels per unit, so units per pixel is 1 / 2.875.
            SafeAreaInsets pixels = new SafeAreaInsets(132f, 0f, 132f, 63f);
            SafeAreaInsets units = SafeAreaMath.ToPanel(pixels, 1f / 2.875f);
            Assert.That(units.Left, Is.EqualTo(45.913f).Within(0.01f));
            Assert.That(units.Bottom, Is.EqualTo(21.913f).Within(0.01f));
        }

        [TestCase(0f)]
        [TestCase(-1f)]
        [TestCase(float.NaN)]
        [TestCase(float.PositiveInfinity)]
        public void ToPanel_WithAnUnusableScale_GivesZero(float unitsPerPixel)
        {
            Assert.That(SafeAreaMath.ToPanel(new SafeAreaInsets(10f, 10f, 10f, 10f), unitsPerPixel), Is.EqualTo(SafeAreaInsets.Zero));
        }

        [Test]
        public void Mask_KeepsOnlyTheListedEdges()
        {
            SafeAreaInsets all = new SafeAreaInsets(1f, 2f, 3f, 4f);
            Assert.That(SafeAreaMath.Mask(all, SafeAreaEdges.All), Is.EqualTo(all));
            Assert.That(SafeAreaMath.Mask(all, SafeAreaEdges.None), Is.EqualTo(SafeAreaInsets.Zero));
            Assert.That(SafeAreaMath.Mask(all, SafeAreaEdges.Top | SafeAreaEdges.Bottom), Is.EqualTo(new SafeAreaInsets(0f, 2f, 0f, 4f)));
            Assert.That(SafeAreaMath.Mask(all, SafeAreaEdges.Left), Is.EqualTo(new SafeAreaInsets(1f, 0f, 0f, 0f)));
        }

        [Test]
        public void Insets_AreEqualWithinASubPixel()
        {
            Assert.That(new SafeAreaInsets(10f, 0f, 0f, 0f).Equals(new SafeAreaInsets(10.004f, 0f, 0f, 0f)), Is.True);
            Assert.That(new SafeAreaInsets(10f, 0f, 0f, 0f).Equals(new SafeAreaInsets(10.5f, 0f, 0f, 0f)), Is.False);
        }

        [TestCase(null, SafeAreaEdges.All)]
        [TestCase("", SafeAreaEdges.All)]
        [TestCase("all", SafeAreaEdges.All)]
        [TestCase("none", SafeAreaEdges.None)]
        [TestCase("top,bottom", SafeAreaEdges.Top | SafeAreaEdges.Bottom)]
        [TestCase("Left Right", SafeAreaEdges.Left | SafeAreaEdges.Right)]
        [TestCase("horizontal", SafeAreaEdges.Left | SafeAreaEdges.Right)]
        [TestCase("vertical;left", SafeAreaEdges.Top | SafeAreaEdges.Bottom | SafeAreaEdges.Left)]
        [TestCase("bogus", SafeAreaEdges.None)]
        public void EdgesParser_ReadsTheAttribute(string text, SafeAreaEdges expected)
        {
            Assert.That(SafeAreaEdgesParser.Parse(text), Is.EqualTo(expected));
        }
    }
}
