using FutureDigitalPlus.UIKit.DesignSystem.Theme.Data;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.UIElements;

namespace FutureDigitalPlus.UIKit.Tests.DesignSystemModule
{
    /// <summary>
    /// New smoke tests for this package (upstream ships none). They check that the Resources the
    /// DesignSystem module depends on are present under the paths its USS and code expect.
    /// </summary>
    public class DesignSystemResourcesTests
    {
        private static readonly string[] StyleSheetNames =
        {
            "DesignSystem", "DesignTokens", "Typography", "Icons", "Buttons", "Inputs", "TabsAndFilters",
            "Cards", "Navigation", "Badges", "Controls", "Overlays", "Feedback", "Mobile", "DropdownPopup",
        };

        [TestCaseSource(nameof(StyleSheetNames))]
        public void StyleSheet_LoadsFromResources(string name)
        {
            var sheet = Resources.Load<StyleSheet>("FutureDigitalPlus/UIKit/DesignSystem/Styles/" + name);
            Assert.IsNotNull(sheet, $"FutureDigitalPlus/UIKit/DesignSystem/Styles/{name}.uss is missing from Resources");
        }

        [Test]
        public void BuiltInThemes_Load_AndAreBaked()
        {
            var dark = Resources.Load<ThemeData>("FutureDigitalPlus/UIKit/DesignSystem/Themes/Dark");
            var light = Resources.Load<ThemeData>("FutureDigitalPlus/UIKit/DesignSystem/Themes/Light");

            Assert.IsNotNull(dark, "Dark theme missing");
            Assert.IsNotNull(light, "Light theme missing");
            Assert.IsNull(dark.ScopeClass, "Dark is :root scoped");
            Assert.AreEqual("theme-light", light.ScopeClass);
            Assert.IsNotNull(dark.StyleSheet, "Dark has no baked stylesheet");
            Assert.IsNotNull(light.StyleSheet, "Light has no baked stylesheet");
        }

        [Test]
        public void ThemeData_GeneratesTokenBlock()
        {
            var dark = Resources.Load<ThemeData>("FutureDigitalPlus/UIKit/DesignSystem/Themes/Dark");
            var uss = dark.GenerateUssString();

            StringAssert.Contains("--fdp-color-primary", uss);
            StringAssert.Contains(dark.Scope, uss);
        }

        [Test]
        public void Icons_Load_AsVectorImages()
        {
            // The .svg files use the UI Toolkit importer (svgType 3), so they import as VectorImage assets.
            Assert.IsNotNull(Resources.Load<VectorImage>("FutureDigitalPlus/UIKit/DesignSystem/Icons/arrow_up"));
            Assert.IsNotNull(Resources.Load<VectorImage>("FutureDigitalPlus/UIKit/DesignSystem/Icons/trophy"));

            var all = Resources.LoadAll<VectorImage>("FutureDigitalPlus/UIKit/DesignSystem/Icons");
            Assert.GreaterOrEqual(all.Length, 120, "the module ships 120 SVG icons (VectorImage)");
        }

        [Test]
        public void BlueprintShader_IsInResources()
        {
            Assert.IsNotNull(Resources.Load<Shader>("FutureDigitalPlus/UIKit/DesignSystem/Shaders/DsFxBlueprint"));
        }
    }
}
