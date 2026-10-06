using NUnit.Framework;

namespace FutureDigitalPlus.UIKit.Interaction.Tests
{
    public class AccessibilityResolveTests
    {
        [Test]
        public void Defaults_ResolveToTheDefaultState()
        {
            var effective = new AccessibilityOptions().Resolve(SystemAccessibility.Default);
            Assert.AreEqual(EffectiveAccessibility.Default, effective);
            Assert.AreEqual(100, effective.TextScaleStep);
            Assert.IsFalse(effective.ReduceMotion);
        }

        [Test]
        public void ADefaultStructAsSystemValue_IsTreatedAsNothingReported()
        {
            var effective = new AccessibilityOptions().Resolve(default(SystemAccessibility));
            Assert.AreEqual(100, effective.TextScalePercent, "a font scale of 0 must not collapse the text");
        }

        [Test]
        public void PlayerFlags_AreCarriedOver()
        {
            var options = new AccessibilityOptions { ReduceMotion = true, HighContrast = true, BoldText = true, TextScalePercent = 150 };
            var effective = options.Resolve(SystemAccessibility.Default);
            Assert.IsTrue(effective.ReduceMotion);
            Assert.IsTrue(effective.HighContrast);
            Assert.IsTrue(effective.BoldText);
            Assert.AreEqual(150, effective.TextScalePercent);
        }

        [Test]
        public void SystemFlags_AreFollowedWhenAllowed()
        {
            var system = new SystemAccessibility(1f, true, true, true);
            var effective = new AccessibilityOptions().Resolve(system);
            Assert.IsTrue(effective.BoldText);
            Assert.IsTrue(effective.ReduceMotion);
            Assert.IsTrue(effective.HighContrast);
        }

        [Test]
        public void SystemFlags_AreIgnoredWhenTheFollowOptionIsOff()
        {
            var options = new AccessibilityOptions
            {
                FollowSystemBoldText = false,
                FollowSystemReduceMotion = false,
                FollowSystemHighContrast = false,
            };
            var effective = options.Resolve(new SystemAccessibility(1f, true, true, true));
            Assert.AreEqual(EffectiveAccessibility.Default, effective);
        }

        [Test]
        public void PlayerFlagWinsOverTheFollowOption()
        {
            var options = new AccessibilityOptions { BoldText = true, FollowSystemBoldText = false };
            Assert.IsTrue(options.Resolve(SystemAccessibility.Default).BoldText);
        }

        [Test]
        public void SystemFontScale_IsMultipliedIn_AndSnappedToAStepForTheClass()
        {
            var effective = new AccessibilityOptions { TextScalePercent = 100 }.Resolve(new SystemAccessibility(1.3f, false, false, false));
            Assert.AreEqual(130, effective.TextScalePercent);
            Assert.AreEqual(125, effective.TextScaleStep);
        }

        [Test]
        public void SystemFontScale_CanBeIgnored()
        {
            var options = new AccessibilityOptions { FollowSystemFontScale = false };
            Assert.AreEqual(100, options.Resolve(new SystemAccessibility(2f, false, false, false)).TextScalePercent);
        }

        [Test]
        public void TextScalePercent_IsClampedByTheConstructor()
        {
            Assert.AreEqual(200, new EffectiveAccessibility(false, false, false, 900).TextScalePercent);
            Assert.AreEqual(100, new EffectiveAccessibility(false, false, false, 10).TextScalePercent);
        }

        [Test]
        public void ScaleMotion_IsZeroUnderReducedMotionOnly()
        {
            Assert.AreEqual(0, new EffectiveAccessibility(true, false, false, 100).ScaleMotion(300));
            Assert.AreEqual(300, new EffectiveAccessibility(false, false, false, 100).ScaleMotion(300));
        }

        [Test]
        public void Equality_ComparesEveryField()
        {
            var a = new EffectiveAccessibility(true, false, true, 150);
            Assert.AreEqual(a, new EffectiveAccessibility(true, false, true, 150));
            Assert.IsTrue(a == new EffectiveAccessibility(true, false, true, 150));
            Assert.IsTrue(a != new EffectiveAccessibility(false, false, true, 150));
            Assert.IsTrue(a != new EffectiveAccessibility(true, true, true, 150));
            Assert.IsTrue(a != new EffectiveAccessibility(true, false, false, 150));
            Assert.IsTrue(a != new EffectiveAccessibility(true, false, true, 175));
            Assert.AreEqual(a.GetHashCode(), new EffectiveAccessibility(true, false, true, 150).GetHashCode());
            Assert.IsFalse(a.Equals("not a state"));
        }

        [Test]
        public void ClassNames_FollowTheScheme()
        {
            Assert.IsNull(AccessibilityClasses.TextScale(100));
            Assert.AreEqual("fdp-text-scale-125", AccessibilityClasses.TextScale(125));
            Assert.AreEqual("fdp-text-scale-200", AccessibilityClasses.TextScale(200));
            Assert.AreEqual("fdp-reduce-motion", AccessibilityClasses.ReduceMotion);
            Assert.AreEqual("fdp-high-contrast", AccessibilityClasses.HighContrast);
            Assert.AreEqual("fdp-bold-text", AccessibilityClasses.BoldText);
        }

        [Test]
        public void EveryStep_HasAClass_ExceptTheDesignSize()
        {
            foreach (var step in TextScaleSteps.All)
            {
                if (step == TextScaleSteps.Min) continue;
                StringAssert.StartsWith(AccessibilityClasses.TextScalePrefix, AccessibilityClasses.TextScale(step));
            }
        }
    }
}
