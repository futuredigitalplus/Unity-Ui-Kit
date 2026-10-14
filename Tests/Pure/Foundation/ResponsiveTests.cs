using System.Collections.Generic;
using NUnit.Framework;

namespace FutureDigitalPlus.UIKit.Foundation.Tests
{
    [TestFixture]
    public sealed class DeviceFormFactorsTests
    {
        // sizes in units of real screens (px * 160 / dpi)
        [TestCase(410f, 889f, DeviceFormFactor.Phone)]          // iPhone 16
        [TestCase(368f, 655f, DeviceFormFactor.Phone)]          // iPhone SE
        [TestCase(409f, 955f, DeviceFormFactor.Phone)]          // Fold7 cover (tall: a phone layout)
        [TestCase(730f, 1112f, DeviceFormFactor.Pad)]           // iPad mini
        [TestCase(1251f, 1668f, DeviceFormFactor.Pad)]          // iPad Pro 13
        [TestCase(376f, 391f, DeviceFormFactor.FlipCover)]      // Flip6 cover
        [TestCase(407f, 450f, DeviceFormFactor.FlipCover)]      // Flip7 cover
        [TestCase(414f, 488f, DeviceFormFactor.FlipCover)]      // Razr 50 Ultra cover
        [TestCase(467f, 737f, DeviceFormFactor.FoldCover)]      // passport fold cover
        [TestCase(486f, 707f, DeviceFormFactor.FoldCover)]
        [TestCase(856f, 950f, DeviceFormFactor.FoldInner)]      // Fold7 inner
        [TestCase(701f, 841f, DeviceFormFactor.FoldInner)]      // Pixel Fold inner
        [TestCase(950f, 856f, DeviceFormFactor.FoldInner)]      // the same, sideways
        [TestCase(391f, 376f, DeviceFormFactor.FlipCover)]
        public void Classify_ReadsTheShapeOfRealScreens(float width, float height, DeviceFormFactor expected)
        {
            Assert.That(DeviceFormFactors.Classify(width, height), Is.EqualTo(expected));
        }

        [TestCase(599.9f, 2000f, DeviceFormFactor.Phone)]
        [TestCase(600f, 2000f, DeviceFormFactor.Pad)]
        [TestCase(600f, 779.9f, DeviceFormFactor.FoldInner)]
        [TestCase(600f, 780f, DeviceFormFactor.Pad)]
        [TestCase(400f, 519.9f, DeviceFormFactor.FlipCover)]
        [TestCase(400f, 520f, DeviceFormFactor.FoldCover)]
        [TestCase(400f, 699.9f, DeviceFormFactor.FoldCover)]
        [TestCase(400f, 700f, DeviceFormFactor.Phone)]
        public void Classify_UsesTheThresholds(float width, float height, DeviceFormFactor expected)
        {
            Assert.That(DeviceFormFactors.Classify(width, height), Is.EqualTo(expected));
        }

        [TestCase(0f, 100f)]
        [TestCase(100f, 0f)]
        [TestCase(-1f, 100f)]
        [TestCase(float.NaN, 100f)]
        [TestCase(100f, float.PositiveInfinity)]
        public void Classify_WithAnUnusableSize_IsAPhone(float width, float height)
        {
            Assert.That(DeviceFormFactors.Classify(width, height), Is.EqualTo(DeviceFormFactor.Phone));
        }

        [Test]
        public void ClassNames_AreDistinctAndPrefixed()
        {
            var seen = new HashSet<string>();
            foreach (DeviceFormFactor form in DeviceFormFactors.All)
            {
                string name = DeviceFormFactors.ClassName(form);
                Assert.That(name, Does.StartWith(DeviceFormFactors.ClassPrefix));
                Assert.That(seen.Add(name), Is.True, name);
                Assert.That(DeviceFormFactors.DisplayName(form), Is.Not.Empty);
            }
            Assert.That(DeviceFormFactors.ClassName(DeviceFormFactor.FlipCover), Is.EqualTo("fdp-ff-flip-cover"));
        }

        [TestCase(DeviceFormFactor.Phone, DeviceFormFactor.Phone)]
        [TestCase(DeviceFormFactor.Pad, DeviceFormFactor.Pad)]
        [TestCase(DeviceFormFactor.FlipCover, DeviceFormFactor.Phone)]
        [TestCase(DeviceFormFactor.FoldCover, DeviceFormFactor.Phone)]
        [TestCase(DeviceFormFactor.FoldInner, DeviceFormFactor.Pad)]
        public void FallbackOf_GoesToThePhoneOrThePad(DeviceFormFactor form, DeviceFormFactor expected)
        {
            Assert.That(DeviceFormFactors.FallbackOf(form), Is.EqualTo(expected));
        }

        [TestCase(HingeAxis.Horizontal, false, HingeAxis.Horizontal)]
        [TestCase(HingeAxis.Horizontal, true, HingeAxis.Vertical)]
        [TestCase(HingeAxis.Vertical, true, HingeAxis.Horizontal)]
        [TestCase(HingeAxis.None, true, HingeAxis.None)]
        public void HingeAs_TurnsWithTheScreen(HingeAxis upright, bool landscape, HingeAxis expected)
        {
            Assert.That(DeviceFormFactors.HingeAs(upright, landscape), Is.EqualTo(expected));
        }
    }

    [TestFixture]
    public sealed class ResponsiveRulesTests
    {
        static ResponsiveRule Rule(DeviceFormFactor form, ResponsiveOrientation orientation, float scale, float width = 0f)
        {
            return new ResponsiveRule { form = form, orientation = orientation, scale = scale, maxContentWidth = width };
        }

        [TestCase(0f, 1f)]
        [TestCase(-2f, 1f)]
        [TestCase(float.NaN, 1f)]
        [TestCase(float.PositiveInfinity, 1f)]
        [TestCase(0.1f, ResponsiveRule.MinScale)]
        [TestCase(9f, ResponsiveRule.MaxScale)]
        [TestCase(0.85f, 0.85f)]
        public void Scale_RepairsBadValues(float stored, float expected)
        {
            Assert.That(Rule(DeviceFormFactor.Phone, ResponsiveOrientation.Any, stored).Scale, Is.EqualTo(expected).Within(1e-5f));
        }

        [TestCase(-1f, 0f)]
        [TestCase(float.NaN, 0f)]
        [TestCase(float.PositiveInfinity, 0f)]
        [TestCase(560f, 560f)]
        public void MaxContentWidth_RepairsBadValues(float stored, float expected)
        {
            Assert.That(Rule(DeviceFormFactor.Phone, ResponsiveOrientation.Any, 1f, stored).MaxContentWidth, Is.EqualTo(expected));
        }

        [Test]
        public void EmptyTable_ResolvesToIdentity()
        {
            var rules = new ResponsiveRules();
            foreach (DeviceFormFactor form in DeviceFormFactors.All)
            {
                Assert.That(rules.Resolve(form, false).IsIdentity, Is.True);
                Assert.That(rules.Resolve(form, true).IsIdentity, Is.True);
            }
        }

        [Test]
        public void NullList_ResolvesToIdentity()
        {
            var rules = new ResponsiveRules { rules = null };
            Assert.That(rules.Resolve(DeviceFormFactor.Pad, true).IsIdentity, Is.True);
            Assert.That(rules.ContentHash(), Is.EqualTo(new ResponsiveRules().ContentHash()));
        }

        [Test]
        public void Resolve_PrefersTheExactOrientation_ThenAny()
        {
            var rules = new ResponsiveRules();
            rules.Set(Rule(DeviceFormFactor.Pad, ResponsiveOrientation.Any, 0.9f));
            rules.Set(Rule(DeviceFormFactor.Pad, ResponsiveOrientation.Landscape, 0.8f));
            Assert.That(rules.Resolve(DeviceFormFactor.Pad, true).Scale, Is.EqualTo(0.8f).Within(1e-5f));
            Assert.That(rules.Resolve(DeviceFormFactor.Pad, false).Scale, Is.EqualTo(0.9f).Within(1e-5f));
        }

        [Test]
        public void Resolve_FallsBackToThePhoneForCovers_AndThePadForInnerScreens()
        {
            var rules = new ResponsiveRules();
            rules.Set(Rule(DeviceFormFactor.Phone, ResponsiveOrientation.Portrait, 0.95f));
            rules.Set(Rule(DeviceFormFactor.Pad, ResponsiveOrientation.Any, 1.1f, 640f));
            Assert.That(rules.Resolve(DeviceFormFactor.FlipCover, false).Scale, Is.EqualTo(0.95f).Within(1e-5f));
            Assert.That(rules.Resolve(DeviceFormFactor.FlipCover, true).IsIdentity, Is.True);
            Assert.That(rules.Resolve(DeviceFormFactor.FoldInner, true).MaxContentWidth, Is.EqualTo(640f));

            rules.Set(Rule(DeviceFormFactor.FlipCover, ResponsiveOrientation.Any, 0.8f));
            Assert.That(rules.Resolve(DeviceFormFactor.FlipCover, false).Scale, Is.EqualTo(0.8f).Within(1e-5f));
        }

        [Test]
        public void Set_Replaces()
        {
            var rules = new ResponsiveRules();
            rules.Set(Rule(DeviceFormFactor.FoldCover, ResponsiveOrientation.Any, 0.9f));
            rules.Set(Rule(DeviceFormFactor.FoldCover, ResponsiveOrientation.Any, 0.7f));
            Assert.That(rules.rules.Count, Is.EqualTo(1));
            Assert.That(rules.rules[0].Scale, Is.EqualTo(0.7f).Within(1e-5f));
        }

        [Test]
        public void AnIdentityRule_StopsTheFallback()
        {
            var rules = new ResponsiveRules();
            rules.Set(Rule(DeviceFormFactor.Phone, ResponsiveOrientation.Any, 0.9f));
            rules.Set(ResponsiveRule.Identity(DeviceFormFactor.FlipCover, ResponsiveOrientation.Any));
            Assert.That(rules.Resolve(DeviceFormFactor.FlipCover, false).IsIdentity, Is.True);
            Assert.That(rules.Resolve(DeviceFormFactor.FoldCover, false).Scale, Is.EqualTo(0.9f).Within(1e-5f));
            rules.Clear(DeviceFormFactor.FlipCover, ResponsiveOrientation.Any);
            Assert.That(rules.Resolve(DeviceFormFactor.FlipCover, false).Scale, Is.EqualTo(0.9f).Within(1e-5f));
        }

        [Test]
        public void Clear_RemovesOnlyThatEntry()
        {
            var rules = new ResponsiveRules();
            rules.Set(Rule(DeviceFormFactor.Pad, ResponsiveOrientation.Portrait, 0.9f));
            rules.Set(Rule(DeviceFormFactor.Pad, ResponsiveOrientation.Landscape, 0.8f));
            rules.Clear(DeviceFormFactor.Pad, ResponsiveOrientation.Portrait);
            Assert.That(rules.TryFind(DeviceFormFactor.Pad, ResponsiveOrientation.Portrait, out _), Is.False);
            Assert.That(rules.TryFind(DeviceFormFactor.Pad, ResponsiveOrientation.Landscape, out _), Is.True);
        }

        [Test]
        public void ContentHash_ChangesWithEveryEdit_AndCloneIsIndependent()
        {
            var rules = new ResponsiveRules();
            int empty = rules.ContentHash();
            rules.Set(Rule(DeviceFormFactor.Phone, ResponsiveOrientation.Any, 0.9f));
            int one = rules.ContentHash();
            rules.Set(Rule(DeviceFormFactor.Phone, ResponsiveOrientation.Any, 0.9f, 500f));
            int widened = rules.ContentHash();
            Assert.That(one, Is.Not.EqualTo(empty));
            Assert.That(widened, Is.Not.EqualTo(one));

            ResponsiveRules copy = rules.Clone();
            copy.Set(Rule(DeviceFormFactor.Pad, ResponsiveOrientation.Any, 1.2f));
            Assert.That(rules.rules.Count, Is.EqualTo(1));
            Assert.That(copy.ContentHash(), Is.Not.EqualTo(rules.ContentHash()));
        }
    }

    [TestFixture]
    public sealed class ResponsiveCatalogTests
    {
        static ResponsiveScreenSpec Flip7Cover()
        {
            return new ResponsiveScreenSpec { name = "Cover", width = 948, height = 1048, dpi = 373f };
        }

        [Test]
        public void PixelSize_SwapsInLandscape()
        {
            var screen = new ResponsiveScreenSpec { width = 1080, height = 2400, dpi = 420f };
            screen.PixelSize(false, out int w, out int h);
            Assert.That((w, h), Is.EqualTo((1080, 2400)));
            screen.PixelSize(true, out w, out h);
            Assert.That((w, h), Is.EqualTo((2400, 1080)));
        }

        [Test]
        public void Insets_MapTopBottomLeftRight()
        {
            var screen = new ResponsiveScreenSpec { width = 1080, height = 2400, dpi = 420f, safePortrait = new[] { 120, 60, 0, 0 }, safeLandscape = new[] { 0, 40, 120, 10 } };
            SafeAreaInsets portrait = screen.Insets(false);
            Assert.That((portrait.Left, portrait.Top, portrait.Right, portrait.Bottom), Is.EqualTo((0f, 120f, 0f, 60f)));
            SafeAreaInsets landscape = screen.Insets(true);
            Assert.That((landscape.Left, landscape.Top, landscape.Right, landscape.Bottom), Is.EqualTo((120f, 0f, 10f, 40f)));
        }

        [Test]
        public void Insets_WithMissingOrNegativeValues_AreSafe()
        {
            var screen = new ResponsiveScreenSpec { width = 1080, height = 2400, dpi = 420f, safePortrait = null, safeLandscape = new[] { -5, 0, 0, 0 } };
            Assert.That(screen.Insets(false), Is.EqualTo(SafeAreaInsets.Zero));
            Assert.That(screen.Insets(true).Top, Is.EqualTo(0f));
        }

        [Test]
        public void IsTablet_UsesThe600PointRule()
        {
            Assert.That(new ResponsiveScreenSpec { width = 1488, height = 2266, dpi = 326f }.IsTablet, Is.True);
            Assert.That(new ResponsiveScreenSpec { width = 1179, height = 2556, dpi = 460f }.IsTablet, Is.False);
            Assert.That(new ResponsiveScreenSpec { width = 1179, height = 2556, dpi = 0f }.IsTablet, Is.False);
        }

        [Test]
        public void FormFactor_OfRealScreens()
        {
            PanelScaleProfile profile = PanelScaleProfile.CardPortrait();
            Assert.That(Flip7Cover().FormFactor(profile), Is.EqualTo(DeviceFormFactor.FlipCover));
            Assert.That(new ResponsiveScreenSpec { width = 1968, height = 2184, dpi = 368f }.FormFactor(profile), Is.EqualTo(DeviceFormFactor.FoldInner));
            Assert.That(new ResponsiveScreenSpec { width = 1179, height = 2556, dpi = 460f }.FormFactor(profile), Is.EqualTo(DeviceFormFactor.Phone));
            Assert.That(new ResponsiveScreenSpec { width = 1640, height = 2360, dpi = 264f }.FormFactor(profile), Is.EqualTo(DeviceFormFactor.Pad));
        }

        [Test]
        public void Scale_AppliesTheUserScale()
        {
            PanelScaleProfile profile = PanelScaleProfile.CardPortrait();
            float plain = Flip7Cover().Scale(profile, false).PixelsPerUnit;
            float smaller = Flip7Cover().Scale(profile, false, 0.8f).PixelsPerUnit;
            Assert.That(plain, Is.GreaterThan(0f));
            Assert.That(smaller, Is.LessThan(plain));
        }

        [Test]
        public void Validate_ReportsEveryProblem()
        {
            var catalog = new ResponsiveDeviceCatalogData();
            catalog.devices.Add(new ResponsiveDeviceSpec { id = "a", name = "A", screens = { Flip7Cover() } });
            catalog.devices.Add(new ResponsiveDeviceSpec { id = "a", name = "A again", screens = { Flip7Cover() } });
            catalog.devices.Add(new ResponsiveDeviceSpec { id = "", name = "", screens = { Flip7Cover() } });
            catalog.devices.Add(new ResponsiveDeviceSpec { id = "b", name = "B" });
            catalog.devices.Add(new ResponsiveDeviceSpec { id = "c", name = "C", screens = { new ResponsiveScreenSpec { width = 0, height = 10, dpi = 300f } } });
            catalog.devices.Add(null);

            List<string> problems = catalog.Validate();
            Assert.That(problems, Has.Some.Contains("a: duplicate id"));
            Assert.That(problems, Has.Some.Contains("no id"));
            Assert.That(problems, Has.Some.Contains("no name"));
            Assert.That(problems, Has.Some.Contains("b: no screen"));
            Assert.That(problems, Has.Some.Contains("c: screen 0"));
            Assert.That(problems, Has.Some.Contains("null entry"));
        }

        [Test]
        public void Validate_OfASoundCatalog_IsEmpty()
        {
            var catalog = new ResponsiveDeviceCatalogData();
            catalog.devices.Add(new ResponsiveDeviceSpec { id = "a", name = "A", screens = { Flip7Cover() } });
            Assert.That(catalog.Validate(), Is.Empty);
            Assert.That(new ResponsiveDeviceCatalogData { devices = null }.Validate(), Is.Not.Empty);
        }
    }
}
