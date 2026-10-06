using FutureDigitalPlus.UIKit.DesignSystem.Theme.Applier;
using FutureDigitalPlus.UIKit.DesignSystem.Theme.Data;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.UIElements;

namespace FutureDigitalPlus.UIKit.Tests.DesignSystemModule
{
    /// <summary>Hostile-usage tests of the theme seam: themes that die while worn, themes applied while high contrast is on, and swaps that must not leak sheets.</summary>
    public class AdversarialThemeTests
    {
        private ThemeData _dark;
        private ThemeData _light;
        private int _sheetsBefore;

        [SetUp]
        public void SetUp()
        {
            _dark = Resources.Load<ThemeData>(ThemeData.DarkResource);
            _light = Resources.Load<ThemeData>(ThemeData.LightResource);
            Assert.IsNotNull(_dark);
            Assert.IsNotNull(_light);
            _sheetsBefore = ThemeRuntime.LiveRuntimeSheets;
        }

        [TearDown]
        public void TearDown()
        {
            Assert.AreEqual(_sheetsBefore, ThemeRuntime.LiveRuntimeSheets, "the test left generated stylesheets alive");
        }

        [Test]
        public void ClearAfterTheWornRuntimeThemeWasDestroyed_StillRemovesTheScopeClassAndSheet()
        {
            var root = new VisualElement();
            var theme = ThemeData.CreateRuntime(_light);
            Assert.IsTrue(ThemeRuntime.Apply(root, theme));
            Assert.IsNotNull(theme.ScopeClass, "the light theme is class scoped");
            Assert.IsTrue(root.ClassListContains(theme.ScopeClass));

            var scope = theme.ScopeClass;
            Object.DestroyImmediate(theme);
            ThemeRuntime.Clear(root);

            Assert.IsFalse(root.ClassListContains(scope), "the scope class of a destroyed theme must not stay on the root");
            Assert.AreEqual(0, root.styleSheets.count);
        }

        [Test]
        public void ApplyingADestroyedTheme_UndressesTheRoot()
        {
            var root = new VisualElement();
            var theme = ThemeData.CreateRuntime(_light);
            var scope = theme.ScopeClass;
            ThemeRuntime.Apply(root, theme);
            Object.DestroyImmediate(theme);

            Assert.IsFalse(ThemeRuntime.Apply(root, theme));
            Assert.IsNull(ThemeRuntime.WornBy(root));
            Assert.AreEqual(0, root.styleSheets.count);
            Assert.IsFalse(root.ClassListContains(scope));
        }

        [Test]
        public void HighContrast_ThemeAppliedWhileItIsOn_IsNeitherDestroyedNorReplacedWhenItIsTurnedOff()
        {
            var root = new VisualElement();
            ThemeRuntime.Apply(root, _dark);
            Assert.IsTrue(ThemeRuntime.SetHighContrast(root, true));
            Assert.IsTrue(ThemeRuntime.IsHighContrast(root));

            var custom = ThemeData.CreateRuntime(_light);
            Assert.IsTrue(ThemeRuntime.Apply(root, custom));
            ThemeRuntime.SetHighContrast(root, false);

            Assert.IsTrue(custom != null, "a theme the game applied must not be destroyed by the high contrast switch");
            Assert.IsFalse(ThemeRuntime.IsHighContrast(root));
            Assert.AreSame(custom, ThemeRuntime.WornBy(root), "the theme chosen last stays");

            ThemeRuntime.Clear(root);
            Object.DestroyImmediate(custom);
        }

        [Test]
        public void HighContrast_RootClearedWhileItIsOn_IsNoLongerInHighContrast()
        {
            var root = new VisualElement();
            var brand = ThemeData.CreateRuntime(_light);
            ThemeRuntime.Apply(root, brand);
            Assert.IsTrue(ThemeRuntime.SetHighContrast(root, true));
            Assert.IsTrue(ThemeRuntime.IsHighContrast(root));

            ThemeRuntime.Clear(root);
            Assert.IsFalse(ThemeRuntime.IsHighContrast(root), "a root that wears nothing is not in high contrast");
            Assert.IsTrue(ThemeRuntime.SetHighContrast(root, false));
            Assert.IsNull(ThemeRuntime.WornBy(root));

            Object.DestroyImmediate(brand);
        }

        [Test]
        public void HighContrast_OnTwiceAndOffTwice_IsIdempotentAndRestoresTheOriginal()
        {
            var root = new VisualElement();
            var brand = ThemeData.CreateRuntime(_dark);
            ThemeRuntime.Apply(root, brand);

            ThemeRuntime.SetHighContrast(root, true);
            ThemeRuntime.SetHighContrast(root, true);
            ThemeRuntime.SetHighContrast(root, false);
            ThemeRuntime.SetHighContrast(root, false);

            Assert.AreSame(brand, ThemeRuntime.WornBy(root));
            ThemeRuntime.Clear(root);
            Object.DestroyImmediate(brand);
        }

        [Test]
        public void RuntimeTheme_ChangedManyTimesOnManyRoots_KeepsOneSheetPerWornRevision()
        {
            var theme = ThemeData.CreateRuntime(_light);
            var roots = new VisualElement[5];
            for (var i = 0; i < roots.Length; i++) roots[i] = new VisualElement();

            for (var round = 0; round < 40; round++)
            {
                theme.SetToken("--fdp-color-primary", new Color(round / 40f, 0.3f, 0.6f));
                // Only some roots follow each change: the others keep wearing an older revision.
                for (var i = 0; i < roots.Length; i++)
                    if ((round + i) % 3 != 0) ThemeRuntime.Apply(roots[i], theme);
            }

            Assert.LessOrEqual(ThemeRuntime.LiveRuntimeSheets - _sheetsBefore, roots.Length, "at most one generated sheet per root");
            foreach (var root in roots) ThemeRuntime.Clear(root);
            Object.DestroyImmediate(theme);
        }

        [Test]
        public void ThemeManager_ChangedHandlerThatSetsAnotherTheme_EndsOnTheLastOne()
        {
            var manager = new ThemeManager();
            manager.Register("dark", _dark);
            manager.Register("light", _light);
            var root = new VisualElement();
            manager.Attach(root);
            manager.Changed += name => { if (name == "dark") manager.Set("light"); };

            manager.Set("dark");

            Assert.AreEqual("light", manager.CurrentName);
            Assert.AreSame(_light, ThemeRuntime.WornBy(root));
            manager.Detach(root);
            ThemeRuntime.Clear(root);
        }

        [Test]
        public void ThemeManager_HighContrastWithRuntimeThemes_SetsAndUnsetsWithoutLeaks()
        {
            var manager = new ThemeManager();
            var a = ThemeData.CreateRuntime(_light);
            var b = ThemeData.CreateRuntime(_dark);
            manager.Register("a", a);
            manager.Register("b", b);
            var roots = new[] { new VisualElement(), new VisualElement() };
            foreach (var r in roots) manager.Attach(r);

            manager.Set("a");
            manager.HighContrast = true;
            manager.Set("b");
            manager.Set("a");
            manager.HighContrast = false;

            foreach (var r in roots)
            {
                Assert.AreSame(a, ThemeRuntime.WornBy(r));
                Assert.IsFalse(ThemeRuntime.IsHighContrast(r));
                manager.Detach(r);
                ThemeRuntime.Clear(r);
            }
            Object.DestroyImmediate(a);
            Object.DestroyImmediate(b);
        }

        [Test]
        public void ThemeManager_NullAndEmptyArguments_AreRefused()
        {
            var manager = new ThemeManager();
            Assert.IsFalse(manager.Register(null, _dark));
            Assert.IsFalse(manager.Register("", _dark));
            Assert.IsFalse(manager.Register("x", null));
            Assert.IsFalse(manager.Set(null));
            Assert.IsFalse(manager.Set("missing"));
            Assert.IsFalse(manager.Unregister(null));
            manager.Attach(null);
            manager.Detach(null);
            manager.Follow(null, null, null);
            Assert.IsFalse(manager.Restore());
        }
    }
}
