using FutureDigitalPlus.UIKit.DesignSystem.Behaviour;
using FutureDigitalPlus.UIKit.DesignSystem.Theme.Applier;
using FutureDigitalPlus.UIKit.DesignSystem.Theme.Data;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.UIElements;

namespace FutureDigitalPlus.UIKit.Tests.DesignSystemModule
{
    /// <summary>New smoke tests for the theme seam and the runtime behaviours (no panel needed).</summary>
    public class ThemeRuntimeTests
    {
        private ThemeData _dark;
        private ThemeData _light;

        [SetUp]
        public void SetUp()
        {
            _dark = Resources.Load<ThemeData>("FutureDigitalPlus/UIKit/DesignSystem/Themes/Dark");
            _light = Resources.Load<ThemeData>("FutureDigitalPlus/UIKit/DesignSystem/Themes/Light");
            Assert.IsNotNull(_dark);
            Assert.IsNotNull(_light);
        }

        [Test]
        public void Apply_ClassScopedTheme_AddsSheetAndClass_AndClearRemovesBoth()
        {
            var root = new VisualElement();

            Assert.IsTrue(ThemeRuntime.Apply(root, _light));
            Assert.IsTrue(root.ClassListContains("theme-light"));
            Assert.IsTrue(root.styleSheets.Contains(_light.StyleSheet));
            Assert.AreSame(_light, ThemeRuntime.WornBy(root));

            ThemeRuntime.Clear(root);
            Assert.IsFalse(root.ClassListContains("theme-light"));
            Assert.IsFalse(root.styleSheets.Contains(_light.StyleSheet));
            Assert.IsNull(ThemeRuntime.WornBy(root));
        }

        [Test]
        public void Apply_Null_UndressesTheRoot()
        {
            var root = new VisualElement();
            ThemeRuntime.Apply(root, _light);

            Assert.IsFalse(ThemeRuntime.Apply(root, null));
            Assert.IsFalse(root.ClassListContains("theme-light"));
            Assert.IsFalse(root.styleSheets.Contains(_light.StyleSheet));
        }

        [Test]
        public void Apply_RootScopedTheme_AddsSheetWithoutClass()
        {
            var root = new VisualElement();

            Assert.IsTrue(ThemeRuntime.Apply(root, _dark));
            Assert.IsTrue(root.styleSheets.Contains(_dark.StyleSheet));
            Assert.IsEmpty(root.GetClasses(), "a :root theme has no scope class");
        }

        [Test]
        public void Apply_Twice_DoesNotStackStyleSheets()
        {
            var root = new VisualElement();
            ThemeRuntime.Apply(root, _light);
            ThemeRuntime.Apply(root, _light);

            Assert.AreEqual(1, root.styleSheets.count);
        }

        [Test]
        public void EnsureToggleKnobs_AddsOneKnobPerToggle_AndIsIdempotent()
        {
            var root = new VisualElement();
            var toggle = new Toggle();
            toggle.AddToClassList("ds-toggle");
            root.Add(toggle);

            DesignSystemBehaviourBase<UIDocument>.EnsureToggleKnobs(root);
            DesignSystemBehaviourBase<UIDocument>.EnsureToggleKnobs(root);

            Assert.AreEqual(1, root.Query(className: "ds-toggle__knob").ToList().Count);
        }
    }
}
