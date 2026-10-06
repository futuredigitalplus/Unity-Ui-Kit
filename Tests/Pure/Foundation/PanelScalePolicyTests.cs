using NUnit.Framework;

namespace FutureDigitalPlus.UIKit.Foundation.Tests
{
    [TestFixture]
    public sealed class PanelScalePolicyTests
    {
        static PanelScale Compute(PanelScaleProfile profile, bool handheld, float dpi, int width, int height, float user = 1f, DeviceClass device = DeviceClass.Desktop)
        {
            return PanelScalePolicy.Compute(profile, new PanelScaleInput(handheld, handheld ? DeviceClass.Phone : device, dpi, width, height, user));
        }

        // ---- the proven constants of the first game (CardPortrait), unchanged ----

        [Test]
        public void CardPortrait_APhoneWithARealDpi_KeepsPhysicalSize()
        {
            PanelScale s = Compute(PanelScaleProfile.CardPortrait(), true, 460f, 1179, 2556);
            Assert.IsTrue(s.Physical);
            Assert.AreEqual(160f, s.ReferenceDpi, 0.001f);
            Assert.AreEqual(460f / 160f, s.PixelsPerUnit, 0.001f);
            Assert.AreEqual(1179f / (460f / 160f), s.ShortSideUnits, 0.5f, "the short side of an iPhone 15 Pro in units");
        }

        [TestCase(0f)]
        [TestCase(72f)]
        [TestCase(96f)]
        [TestCase(110f)]
        [TestCase(900f)]
        public void CardPortrait_ADeviceWithAnImplausibleDpi_FallsBackToTheWindowScale(float dpi)
        {
            Assert.IsFalse(Compute(PanelScaleProfile.CardPortrait(), true, dpi, 1179, 2556).Physical);
        }

        [Test]
        public void CardPortrait_TheEditorNeverUsesTheMonitorDpi()
        {
            Assert.IsFalse(Compute(PanelScaleProfile.CardPortrait(), false, 220f, 1334, 750).Physical);
            Assert.IsFalse(Compute(PanelScaleProfile.CardPortrait(), false, 96f, 1334, 750).Physical);
        }

        [Test]
        public void CardPortrait_APhoneSizedWindow_MeasuresAboutAPhoneShortSideInUnits()
        {
            PanelScale s = Compute(PanelScaleProfile.CardPortrait(), false, 96f, 1334, 750);
            Assert.AreEqual(390f, 750f / s.PixelScale, 0.5f);
            Assert.AreEqual(390f, s.ShortSideUnits, 0.5f);
        }

        [Test]
        public void CardPortrait_ATabletSizedWindow_MeasuresAboutATabletShortSideInUnits()
        {
            PanelScale s = Compute(PanelScaleProfile.CardPortrait(), false, 96f, 2388, 1668);
            Assert.AreEqual(820f, 1668f / s.PixelScale, 1f);
        }

        [Test]
        public void CardPortrait_TheScaleNeverLeavesTheSaneRange()
        {
            PanelScaleProfile p = PanelScaleProfile.CardPortrait();
            Assert.AreEqual(1f, Compute(p, false, 96f, 200, 100).PixelScale);
            Assert.AreEqual(4f, Compute(p, false, 96f, 12000, 9000).PixelScale);
            Assert.AreEqual(1f, Compute(p, false, 96f, 0, 0).PixelScale);
        }

        [Test]
        public void CardPortrait_PhoneAndTabletUnitsSplitAtTheSizeClassBreakpoint()
        {
            PanelScaleProfile p = PanelScaleProfile.CardPortrait();
            Assert.Less(p.phoneShortUnits, UiProfile.RegularMinUnits);
            Assert.GreaterOrEqual(p.tabletShortUnits, UiProfile.RegularMinUnits);
        }

        // ---- other presets ----

        [Test]
        public void DesktopHud_AWindowGetsTheDesktopUnits_NotThePhoneOnes()
        {
            PanelScale s = Compute(PanelScaleProfile.DesktopHud(), false, 96f, 1920, 1080);
            Assert.AreEqual(1.5f, s.PixelScale, 0.001f, "1080 / 720");
            Assert.AreEqual(720f, s.ShortSideUnits, 0.5f);
            Assert.IsFalse(s.Physical);
        }

        [Test]
        public void DesktopHud_AHandheldStillGetsPhysicalSize()
        {
            Assert.IsTrue(Compute(PanelScaleProfile.DesktopHud(), true, 400f, 1080, 2400).Physical);
        }

        [TestCase(1920, 1080, 2f)]
        [TestCase(1280, 720, 1.3333f)]
        [TestCase(3840, 2160, 4f)]
        public void Console_TheShortSideIs540Units(int width, int height, float expected)
        {
            PanelScale s = Compute(PanelScaleProfile.Console(), false, 0f, width, height, 1f, DeviceClass.Console);
            Assert.AreEqual(expected, s.PixelScale, 0.001f);
        }

        [Test]
        public void Web_TheShortSideIs720Units()
        {
            Assert.AreEqual(1.5f, Compute(PanelScaleProfile.Web(), false, 96f, 1920, 1080, 1f, DeviceClass.Web).PixelScale, 0.001f);
        }

        [Test]
        public void World_IsAConstantPixelScale_WhateverTheTextureSize()
        {
            PanelScale a = Compute(PanelScaleProfile.World(), false, 0f, 1024, 512, 1f, DeviceClass.WorldSpace);
            PanelScale b = Compute(PanelScaleProfile.World(), false, 0f, 4096, 2048, 1f, DeviceClass.WorldSpace);
            Assert.AreEqual(1f, a.PixelScale);
            Assert.AreEqual(1f, b.PixelScale);
            Assert.IsFalse(a.Physical);
        }

        [Test]
        public void ReferenceResolution_UsesOneUnitCountOnEveryDevice()
        {
            PanelScaleProfile p = PanelScaleProfile.DesktopHud();
            p.strategy = PanelScaleStrategy.ReferenceResolution;
            p.referenceShortUnits = 600f;
            Assert.AreEqual(1.8f, Compute(p, false, 96f, 1920, 1080).PixelScale, 0.001f);
            Assert.AreEqual(1.8f, Compute(p, true, 460f, 1080, 2400).PixelScale, 0.001f, "even on a phone with a real DPI");
        }

        // ---- the player's UI size ----

        [Test]
        public void UserScale_MultipliesThePixelScale()
        {
            PanelScaleProfile p = PanelScaleProfile.DesktopHud();
            Assert.AreEqual(1.5f * 1.25f, Compute(p, false, 96f, 1920, 1080, 1.25f).PixelScale, 0.001f);
        }

        [Test]
        public void UserScale_InPhysicalModeIsFoldedIntoTheReferenceDpi()
        {
            PanelScale s = Compute(PanelScaleProfile.CardPortrait(), true, 320f, 1080, 2400, 1.25f);
            Assert.IsTrue(s.Physical);
            Assert.AreEqual(128f, s.ReferenceDpi, 0.001f, "160 / 1.25");
            Assert.AreEqual(320f / 128f, s.PixelsPerUnit, 0.001f);
        }

        [Test]
        public void UserScale_ANonsensicalValueIsClamped()
        {
            Assert.Greater(Compute(PanelScaleProfile.DesktopHud(), false, 96f, 1920, 1080, -3f).PixelScale, 0f);
            Assert.Greater(Compute(PanelScaleProfile.DesktopHud(), false, 96f, 1920, 1080, 0f).PixelScale, 0f);
        }

        [Test]
        public void ADefaultProfile_DoesNotDivideByZero()
        {
            PanelScale s = Compute(default(PanelScaleProfile), false, 96f, 1920, 1080);
            Assert.IsFalse(float.IsNaN(s.PixelScale) || float.IsInfinity(s.PixelScale));
            Assert.Greater(s.PixelScale, 0f);
        }

        [Test]
        public void ShortSideUnits_IsPixelsDividedByPixelsPerUnit()
        {
            PanelScale s = Compute(PanelScaleProfile.Console(), false, 0f, 1920, 1080, 1f, DeviceClass.Console);
            Assert.AreEqual(1080f / s.PixelsPerUnit, s.ShortSideUnits, 0.001f);
        }
    }
}
