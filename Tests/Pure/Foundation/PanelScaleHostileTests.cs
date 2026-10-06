using NUnit.Framework;

namespace FutureDigitalPlus.UIKit.Foundation.Tests
{
    /// <summary>The scale policy fed a minimised window, a device that reports no DPI, a corrupt saved user scale and a profile nobody filled in.</summary>
    public class PanelScaleHostileTests
    {
        static readonly float[] Floats = { float.NaN, float.PositiveInfinity, float.NegativeInfinity, -1f, 0f, 1f, 1e30f };

        static PanelScaleProfile[] Profiles()
        {
            PanelScaleProfile nan = PanelScaleProfile.DesktopHud();
            nan.phoneShortUnits = float.NaN;
            nan.desktopShortUnits = float.NaN;
            nan.minScale = float.NaN;
            nan.maxScale = float.NaN;
            nan.referenceDpi = float.NaN;
            return new[] { default(PanelScaleProfile), PanelScaleProfile.CardPortrait(), PanelScaleProfile.DesktopHud(), PanelScaleProfile.Console(), PanelScaleProfile.Web(), nan };
        }

        [Test]
        public void EveryCombination_ProducesAFinitePositiveScale()
        {
            foreach (PanelScaleProfile profile in Profiles())
            foreach (float user in Floats)
            foreach (float dpi in Floats)
            foreach (int size in new[] { -5, 0, 1, 600, 100000 })
            foreach (bool handheld in new[] { false, true })
            {
                PanelScale scale = PanelScalePolicy.Compute(profile, new PanelScaleInput(handheld, DeviceClass.Phone, dpi, size, 800, user));
                string label = profile.strategy + " user " + user + " dpi " + dpi + " size " + size + " handheld " + handheld;
                Assert.IsFalse(float.IsNaN(scale.PixelScale) || float.IsInfinity(scale.PixelScale), "PixelScale " + label);
                Assert.IsFalse(float.IsNaN(scale.PixelsPerUnit) || float.IsInfinity(scale.PixelsPerUnit), "PixelsPerUnit " + label);
                Assert.IsFalse(float.IsNaN(scale.ReferenceDpi) || float.IsInfinity(scale.ReferenceDpi), "ReferenceDpi " + label);
                Assert.IsFalse(float.IsNaN(scale.ShortSideUnits) || float.IsInfinity(scale.ShortSideUnits), "ShortSideUnits " + label);
                Assert.Greater(scale.PixelScale, 0f, "PixelScale " + label);
                Assert.Greater(scale.PixelsPerUnit, 0f, "PixelsPerUnit " + label);
                Assert.Greater(scale.ReferenceDpi, 0f, "ReferenceDpi " + label);
            }
        }
    }
}
