using FutureDigitalPlus.UIKit.DesignSystem.Editor.Theming;
using FutureDigitalPlus.UIKit.DesignSystem.Theme.Applier;
using FutureDigitalPlus.UIKit.DesignSystem.Theme.Data;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace FutureDigitalPlus.UIKit.Tests.DesignSystemModule
{
    /// <summary>
    /// Reuse check: a game must be able to bring its own theme without touching the package. Creates a
    /// third theme asset in the host project (a scratch file, deleted afterwards), bakes it and puts it on a
    /// root. Writes only under Assets/.
    /// </summary>
    public class CustomThemeTests
    {
        private string _path;

        [TearDown]
        public void TearDown()
        {
            if (!string.IsNullOrEmpty(_path)) AssetDatabase.DeleteAsset(_path);
            _path = null;
        }

        [Test]
        public void GameOwnedTheme_BakesAndAppliesWithoutAnyPackageChange()
        {
            var theme = ScriptableObject.CreateInstance<ThemeData>();
            theme.scopeSelector = ".theme-fdp-test";
            theme.primaryColor = new Color(1f, 0f, 1f);
            theme.bgColor = new Color(0.1f, 0.2f, 0.3f);

            _path = AssetDatabase.GenerateUniqueAssetPath("Assets/FdpUiKitTestTheme.asset");
            AssetDatabase.CreateAsset(theme, _path);

            Assert.IsTrue(ThemeBaker.Bake(theme), "the baker accepts a theme that lives in the game's Assets");
            Assert.IsNotNull(theme.StyleSheet, "baked stylesheet sub-asset");
            Assert.AreEqual("theme-fdp-test", theme.ScopeClass);

            var uss = theme.GenerateUssString();
            StringAssert.Contains(".theme-fdp-test {", uss);
            StringAssert.Contains("--fdp-color-primary: #FF00FF;", uss);
            StringAssert.Contains("--fdp-color-bg: #" + ColorUtility.ToHtmlStringRGB(theme.bgColor) + ";", uss);

            var root = new VisualElement();
            Assert.IsTrue(ThemeRuntime.Apply(root, theme));
            Assert.AreSame(theme, ThemeRuntime.WornBy(root));
            Assert.IsTrue(root.ClassListContains("theme-fdp-test"));
            Assert.IsTrue(root.styleSheets.Contains(theme.StyleSheet));

            // Swapping to a built-in theme takes the custom one off completely.
            var light = Resources.Load<ThemeData>("FutureDigitalPlus/UIKit/DesignSystem/Themes/Light");
            Assert.IsTrue(ThemeRuntime.Apply(root, light));
            Assert.IsFalse(root.ClassListContains("theme-fdp-test"));
            Assert.IsFalse(root.styleSheets.Contains(theme.StyleSheet));
        }
    }
}
