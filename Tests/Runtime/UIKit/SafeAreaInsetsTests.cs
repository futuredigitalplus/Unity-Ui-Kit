using NUnit.Framework;
using UnityEngine;

namespace FutureDigitalPlus.UIKit.Tests
{
    public class SafeAreaInsetsTests
    {
        [Test]
        public void FullScreenSafeArea_GivesNoInsets()
        {
            var i = SafeAreaInsets.FromScreen(new Rect(0, 0, 1000, 2000), new Vector2(1000, 2000), new Vector2(500, 1000));
            Assert.AreEqual(0f, i.Left + i.Top + i.Right + i.Bottom);
        }

        [Test]
        public void NotchAndHomeIndicator_ConvertFromScreenPixelsToPanelUnits()
        {
            // 1000x2000 px screen, 100 px notch on top, 60 px home indicator at the bottom, 20 px each side.
            var safe = new Rect(20, 60, 960, 1840);
            // The panel is half the pixel size (ScaleWithScreenSize, for example).
            var i = SafeAreaInsets.FromScreen(safe, new Vector2(1000, 2000), new Vector2(500, 1000));

            Assert.AreEqual(10f, i.Left, 1e-3f);
            Assert.AreEqual(10f, i.Right, 1e-3f);
            Assert.AreEqual(50f, i.Top, 1e-3f, "Screen.safeArea has its origin at the bottom: top = screenH - yMax");
            Assert.AreEqual(30f, i.Bottom, 1e-3f);
        }

        [Test]
        public void Landscape_NotchGivesSideInsets_AndTheSameDeviceRotatedSwapsThem()
        {
            // Landscape 2000x1000 px: 100 px notch on the left, 60 px on the right, 40 px home indicator at the bottom.
            var landscape = SafeAreaInsets.FromScreen(new Rect(100, 40, 1840, 960), new Vector2(2000, 1000), new Vector2(1000, 500));
            Assert.AreEqual(50f, landscape.Left, 1e-3f);
            Assert.AreEqual(30f, landscape.Right, 1e-3f);
            Assert.AreEqual(20f, landscape.Bottom, 1e-3f);
            Assert.AreEqual(0f, landscape.Top, 1e-3f);

            // Rotated to portrait the notch is on top; the left/right insets disappear.
            var portrait = SafeAreaInsets.FromScreen(new Rect(0, 40, 1000, 1860), new Vector2(1000, 2000), new Vector2(500, 1000));
            Assert.AreEqual(0f, portrait.Left, 1e-3f);
            Assert.AreEqual(0f, portrait.Right, 1e-3f);
            Assert.AreEqual(50f, portrait.Top, 1e-3f);
            Assert.AreEqual(20f, portrait.Bottom, 1e-3f);
        }

        [Test]
        public void DegenerateSizes_GiveNoInsets()
        {
            var safe = new Rect(10, 10, 100, 100);
            Assert.AreEqual(SafeAreaInsets.None.ToString(), SafeAreaInsets.FromScreen(safe, Vector2.zero, new Vector2(10, 10)).ToString());
            Assert.AreEqual(SafeAreaInsets.None.ToString(), SafeAreaInsets.FromScreen(safe, new Vector2(100, 100), new Vector2(float.NaN, float.NaN)).ToString());
            Assert.AreEqual(SafeAreaInsets.None.ToString(), SafeAreaInsets.FromScreen(safe, new Vector2(100, 100), Vector2.zero).ToString());
        }
    }
}
