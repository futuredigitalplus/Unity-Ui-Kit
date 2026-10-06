using System.Collections;
using FutureDigitalPlus.UIKit.DesignSystem.Theme.Applier;
using FutureDigitalPlus.UIKit.DesignSystem.Theme.Data;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UIElements;

namespace FutureDigitalPlus.UIKit.Tests.DesignSystemModule
{
    /// <summary>
    /// New PlayMode smoke tests: the theme really changes what a live UIDocument paints, and a few
    /// components can be created from C# and styled by the shipped sheets.
    /// </summary>
    public class ThemePanelTests
    {
        private GameObject _host;
        private PanelSettings _settings;
        private RenderTexture _target;
        private UIDocument _document;

        [SetUp]
        public void SetUp()
        {
            _target = new RenderTexture(640, 480, 0);
            _settings = ScriptableObject.CreateInstance<PanelSettings>();
            _settings.scaleMode = PanelScaleMode.ConstantPixelSize;
            _settings.targetTexture = _target;
        }

        [TearDown]
        public void TearDown()
        {
            LogAssert.ignoreFailingMessages = false;
            if (_host != null) Object.Destroy(_host);
            if (_settings != null) Object.Destroy(_settings);
            if (_target != null) { _target.Release(); Object.Destroy(_target); }
        }

        private IEnumerator CreateDocument()
        {
            if (!Application.isPlaying) Assert.Ignore("Needs a runtime panel (Play Mode).");
            LogAssert.ignoreFailingMessages = true; // a bare PanelSettings may log about its missing runtime theme
            _host = new GameObject("DsThemePanelTests");
            _host.SetActive(false);
            _document = _host.AddComponent<UIDocument>();
            _document.panelSettings = _settings;
            _host.SetActive(true);
            yield return null;
            yield return null;
            Assert.IsNotNull(_document.rootVisualElement);
        }

        [UnityTest]
        public IEnumerator Theme_ChangesThePaintedBackground_OfADsRoot()
        {
            yield return CreateDocument();
            var root = _document.rootVisualElement;
            // On Unity 6000.3 the base sheet's `:root` tokens outrank a class-scoped theme that sits on the SAME element,
            // whatever the sheet order, so the base goes on the panel root (the document root's parent) and the theme
            // on the document root below it. The UIKit facade does the same. See Runtime/DesignSystem/SOURCE.md.
            root.parent.styleSheets.Add(Resources.Load<StyleSheet>("FutureDigitalPlus/UIKit/DesignSystem/Styles/DesignSystem"));

            var screen = new VisualElement();
            screen.AddToClassList("ds-root");
            root.Add(screen);

            var dark = Resources.Load<ThemeData>("FutureDigitalPlus/UIKit/DesignSystem/Themes/Dark");
            var light = Resources.Load<ThemeData>("FutureDigitalPlus/UIKit/DesignSystem/Themes/Light");

            ThemeRuntime.Apply(root, dark);
            yield return null;
            yield return null;
            var darkBg = screen.resolvedStyle.backgroundColor;

            ThemeRuntime.Apply(root, light);
            yield return null;
            yield return null;
            var lightBg = screen.resolvedStyle.backgroundColor;

            Assert.AreNotEqual(darkBg, lightBg, "dark and light themes paint different backgrounds");
            Assert.AreEqual(light.bgColor.r, lightBg.r, 0.01f);
            Assert.AreEqual(light.bgColor.g, lightBg.g, 0.01f);
            Assert.AreEqual(light.bgColor.b, lightBg.b, 0.01f);
            Assert.AreEqual(dark.bgColor.r, darkBg.r, 0.01f);
        }

        [UnityTest]
        public IEnumerator Theme_PrimaryButton_PaintsThePrimaryColor_OfTheAppliedTheme()
        {
            // Regression guard for the token rename (--color-primary -> --fdp-color-primary): the baked theme sheets and the
            // base sheet must agree on the names, or a theme silently stops reaching the components.
            yield return CreateDocument();
            var root = _document.rootVisualElement;
            root.parent.styleSheets.Add(Resources.Load<StyleSheet>("FutureDigitalPlus/UIKit/DesignSystem/Styles/DesignSystem"));

            var screen = new VisualElement();
            screen.AddToClassList("ds-root");
            var button = new Button { text = "Go" };
            button.AddToClassList("ds-btn");
            button.AddToClassList("ds-btn--primary");
            screen.Add(button);
            root.Add(screen);

            foreach (var name in new[] { "Dark", "Light" })
            {
                var theme = Resources.Load<ThemeData>("FutureDigitalPlus/UIKit/DesignSystem/Themes/" + name);
                ThemeRuntime.Apply(root, theme);
                yield return new WaitForSeconds(0.5f); // .ds-btn animates background-color (transition-duration is a few hundred ms at most)

                var fill = button.resolvedStyle.backgroundColor;
                Assert.AreEqual(theme.primaryColor.r, fill.r, 0.01f, name + " primary red");
                Assert.AreEqual(theme.primaryColor.g, fill.g, 0.01f, name + " primary green");
                Assert.AreEqual(theme.primaryColor.b, fill.b, 0.01f, name + " primary blue");
            }
        }

        [UnityTest]
        public IEnumerator Components_CreatedFromCode_GetDesignSystemStyling()
        {
            yield return CreateDocument();
            var root = _document.rootVisualElement;
            root.parent.styleSheets.Add(Resources.Load<StyleSheet>("FutureDigitalPlus/UIKit/DesignSystem/Styles/DesignSystem"));

            var button = new Button { text = "Go" };
            button.AddToClassList("ds-btn");
            button.AddToClassList("ds-btn--primary");
            var card = new VisualElement();
            card.AddToClassList("ds-card");
            var toggle = new Toggle("Sound");
            toggle.AddToClassList("ds-toggle");
            root.Add(button);
            root.Add(card);
            root.Add(toggle);

            FutureDigitalPlus.UIKit.DesignSystem.Behaviour.DesignSystemBehaviourBase<UIDocument>.EnsureAll(root);
            yield return null;
            yield return null;

            Assert.AreEqual(36f, button.resolvedStyle.height, 0.5f, ".ds-btn is 36 px tall on desktop");
            Assert.Greater(button.resolvedStyle.backgroundColor.a, 0f, ".ds-btn--primary paints a fill");
            Assert.IsNotNull(toggle.Q(className: "ds-toggle__knob"), "runtime behaviour injected the toggle knob");
        }
    }
}
