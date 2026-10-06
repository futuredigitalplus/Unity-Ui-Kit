using System.Collections;
using FutureDigitalPlus.UIKit.DesignSystem.Theme.Applier;
using FutureDigitalPlus.UIKit.DesignSystem.Theme.Data;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UIElements;

namespace FutureDigitalPlus.UIKit.Tests
{
    /// <summary>Tests of the facade entry point on a real panel (PlayMode; reported as ignored in EditMode).</summary>
    public class UIKitInstallerTests
    {
        private PanelHarness _panel;
        private ThemeData _light;
        private StyleSheet _base;

        [SetUp]
        public void SetUp()
        {
            _light = Resources.Load<ThemeData>("FutureDigitalPlus/UIKit/DesignSystem/Themes/Light");
            _base = Resources.Load<StyleSheet>(UIKitOptions.BaseStylesResource);
            Assert.IsNotNull(_light, "Light theme");
            Assert.IsNotNull(_base, "base stylesheet");
        }

        [TearDown]
        public void TearDown()
        {
            LogAssert.ignoreFailingMessages = false;
            _panel?.Dispose();
            _panel = null;
        }

        private IEnumerator CreatePanel()
        {
            if (!Application.isPlaying) Assert.Ignore("Needs a runtime panel (Play Mode).");
            LogAssert.ignoreFailingMessages = true; // PanelSettings without a runtime theme may log
            _panel = PanelHarness.Create();
            yield return PanelHarness.Frames();
            Assert.IsNotNull(_panel.Root);
        }

        [Test]
        public void Install_NullRoot_ReturnsNull() => Assert.IsNull(UIKitInstaller.Install(null));

        [Test]
        public void Install_OnDetachedElement_AddsBaseStyles_AndTheme_AndDisposeTakesThemOff()
        {
            var root = new VisualElement();
            var kit = UIKitInstaller.Install(root, new UIKitOptions { Theme = _light, Mobile = MobileMode.Off, ApplySafeArea = false });

            Assert.IsTrue(root.styleSheets.Contains(_base));
            Assert.IsTrue(root.styleSheets.Contains(_light.StyleSheet));
            Assert.IsTrue(root.ClassListContains(_light.ScopeClass));
            Assert.AreSame(_light, ThemeRuntime.WornBy(root));
            // The base sheet must come before the theme so the theme wins ties.
            Assert.Less(IndexOf(root, _base), IndexOf(root, _light.StyleSheet));

            kit.Dispose();
            Assert.IsFalse(root.styleSheets.Contains(_base));
            Assert.IsFalse(root.styleSheets.Contains(_light.StyleSheet));
            Assert.IsFalse(root.ClassListContains(_light.ScopeClass));
        }

        private static int IndexOf(VisualElement root, StyleSheet sheet)
        {
            for (var i = 0; i < root.styleSheets.count; i++)
                if (root.styleSheets[i] == sheet) return i;
            return -1;
        }

        [Test]
        public void Install_KeepsABaseSheetTheHostAlreadyAdded()
        {
            var root = new VisualElement();
            root.styleSheets.Add(_base);
            var kit = UIKitInstaller.Install(root, new UIKitOptions { ApplySafeArea = false });
            Assert.AreEqual(1, root.styleSheets.count);

            kit.Dispose();
            Assert.IsTrue(root.styleSheets.Contains(_base), "a sheet the host added is not ours to remove");
        }

        [Test]
        public void SetTheme_SwapsAndClears()
        {
            var root = new VisualElement();
            var kit = UIKitInstaller.Install(root, new UIKitOptions { InstallBaseStyles = false, Mobile = MobileMode.Off, ApplySafeArea = false });
            Assert.IsNull(ThemeRuntime.WornBy(root));

            kit.SetTheme(_light);
            Assert.AreSame(_light, ThemeRuntime.WornBy(root));
            kit.SetTheme(null);
            Assert.IsNull(ThemeRuntime.WornBy(root));
            Assert.AreEqual(0, root.styleSheets.count);
        }

        [Test]
        public void MobileMode_OnAndOff_AreForced()
        {
            var root = new VisualElement();
            var on = UIKitInstaller.Install(root, new UIKitOptions { InstallBaseStyles = false, Mobile = MobileMode.On, ApplySafeArea = false });
            Assert.IsTrue(root.ClassListContains(UIKitInstaller.MobileClass));
            on.Dispose();
            Assert.IsFalse(root.ClassListContains(UIKitInstaller.MobileClass));

            var off = UIKitInstaller.Install(root, new UIKitOptions { InstallBaseStyles = false, Mobile = MobileMode.Off, ApplySafeArea = false });
            Assert.IsFalse(root.ClassListContains(UIKitInstaller.MobileClass));
            off.Dispose();
        }

        [UnityTest]
        public IEnumerator MobileMode_Auto_FollowsTheBreakpoint_AndMakesButtonsTouchSized()
        {
            yield return CreatePanel();
            var button = new Button { text = "b" };
            button.AddToClassList("ds-btn");
            _panel.Root.Add(button);

            var kit = UIKitInstaller.Install(_panel.Root, new UIKitOptions { Mobile = MobileMode.Auto, MobileBreakpoint = 1f, ApplySafeArea = false });
            yield return PanelHarness.Frames();
            Assert.IsFalse(kit.IsMobile, "panel is wider than a 1 unit breakpoint");
            Assert.AreEqual(36f, button.resolvedStyle.height, 0.5f, "desktop button height");

            kit.Options.MobileBreakpoint = 100000f;
            kit.Refresh();
            yield return PanelHarness.Frames();
            Assert.IsTrue(kit.IsMobile, "panel is narrower than a huge breakpoint");
            Assert.AreEqual(48f, button.resolvedStyle.height, 0.5f, "Mobile.uss makes buttons 48 tall");
            kit.Dispose();
        }

        [UnityTest]
        public IEnumerator SafeArea_PadsTheRoot_AndDisposeRemovesIt()
        {
            yield return CreatePanel();
            var kit = UIKitInstaller.Install(_panel.Root, new UIKitOptions { InstallBaseStyles = false, Mobile = MobileMode.Off });
            // Pretend the screen is twice the panel size, with a 100 px notch, 60 px home bar and 20 px sides.
            kit.ScreenSizeSource = () => new Vector2(PanelHarness.Width * 2, PanelHarness.Height * 2);
            kit.SafeAreaSource = () => new Rect(20, 60, PanelHarness.Width * 2 - 40, PanelHarness.Height * 2 - 160);
            yield return PanelHarness.Frames();
            kit.Refresh();

            Assert.AreEqual(10f, _panel.Root.style.paddingLeft.value.value, 0.5f, $"layout={_panel.Root.layout} insets={kit.AppliedInsets}");
            Assert.AreEqual(10f, _panel.Root.style.paddingRight.value.value, 0.5f);
            Assert.AreEqual(50f, _panel.Root.style.paddingTop.value.value, 0.5f);
            Assert.AreEqual(30f, _panel.Root.style.paddingBottom.value.value, 0.5f);
            Assert.AreEqual(50f, kit.AppliedInsets.Top, 0.5f);

            kit.Dispose();
            Assert.AreEqual(StyleKeyword.Null, _panel.Root.style.paddingTop.keyword);
        }

        [UnityTest]
        public IEnumerator SafeArea_Dispose_RestoresThePaddingTheHostHad()
        {
            yield return CreatePanel();
            _panel.Root.style.paddingTop = 7f;
            var kit = UIKitInstaller.Install(_panel.Root, new UIKitOptions { InstallBaseStyles = false, Mobile = MobileMode.Off });
            kit.ScreenSizeSource = () => new Vector2(PanelHarness.Width * 2, PanelHarness.Height * 2);
            kit.SafeAreaSource = () => new Rect(20, 60, PanelHarness.Width * 2 - 40, PanelHarness.Height * 2 - 160);
            yield return PanelHarness.Frames();
            kit.Refresh();
            Assert.AreEqual(50f, _panel.Root.style.paddingTop.value.value, 0.5f, "safe area overwrites while installed");

            kit.Dispose();
            Assert.AreEqual(7f, _panel.Root.style.paddingTop.value.value, 0.01f, "host padding restored");
            Assert.AreEqual(StyleKeyword.Null, _panel.Root.style.paddingLeft.keyword, "unset sides stay unset");
        }

        [UnityTest]
        public IEnumerator Host_InstallsOnTheDocument_AndThemeSwapsAtRuntime()
        {
            if (!Application.isPlaying) Assert.Ignore("Needs a runtime panel (Play Mode).");
            LogAssert.ignoreFailingMessages = true;
            UIKitHost host = null;
            _panel = PanelHarness.Create(go =>
            {
                host = go.AddComponent<UIKitHost>();
                host.Options.Mobile = MobileMode.Off;
                host.Options.ApplySafeArea = false;
            });
            yield return PanelHarness.Frames();

            Assert.IsNotNull(host.Installation, "host installed itself on the UIDocument root");
            Assert.IsTrue(_panel.Root.parent.styleSheets.Contains(_base), "base sheet sits on the panel root");
            Assert.IsNull(ThemeRuntime.WornBy(_panel.Root));

            host.Theme = _light;
            Assert.AreSame(_light, ThemeRuntime.WornBy(_panel.Root));

            host.enabled = false;
            Assert.IsNull(host.Installation);
            Assert.IsFalse(_panelRootSheets.Contains(_base));
        }

        private VisualElementStyleSheetSet _panelRootSheets => _panel.Root.parent.styleSheets;

        [UnityTest]
        public IEnumerator InPanel_BaseSheetGoesOnThePanelRoot_AndTheThemeWinsOverIt()
        {
            yield return CreatePanel();
            var screen = new VisualElement();
            screen.AddToClassList("ds-root");
            _panel.Root.Add(screen);

            var kit = UIKitInstaller.Install(_panel.Root, new UIKitOptions { Theme = _light, Mobile = MobileMode.Off, ApplySafeArea = false });
            yield return PanelHarness.Frames(6);

            Assert.IsTrue(_panel.Root.parent.styleSheets.Contains(_base), "base sheet on the panel root");
            Assert.IsFalse(_panel.Root.styleSheets.Contains(_base), "not on the document root, where its :root tokens would outrank the theme");
            Assert.IsTrue(_panel.Root.styleSheets.Contains(_light.StyleSheet));
            var bg = screen.resolvedStyle.backgroundColor;
            Assert.AreEqual(_light.bgColor.r, bg.r, 0.01f, "the light theme paints the .ds-root background");
            Assert.AreEqual(_light.bgColor.b, bg.b, 0.01f);

            kit.SetTheme(null);
            yield return PanelHarness.Frames(6);
            Assert.AreEqual(Resources.Load<ThemeData>("FutureDigitalPlus/UIKit/DesignSystem/Themes/Dark").bgColor.r, screen.resolvedStyle.backgroundColor.r, 0.01f,
                "without a theme the base (dark) tokens paint");
            kit.Dispose();
            Assert.IsFalse(_panel.Root.parent.styleSheets.Contains(_base), "the last installation removes the base sheet");
        }

        [UnityTest]
        public IEnumerator TwoInstallationsOnOnePanel_ShareTheBaseSheet_UntilTheLastIsDisposed()
        {
            yield return CreatePanel();
            var a = UIKitInstaller.Install(_panel.Root, new UIKitOptions { ApplySafeArea = false });
            var other = new VisualElement();
            _panel.Root.parent.Add(other);
            var b = UIKitInstaller.Install(other, new UIKitOptions { ApplySafeArea = false });
            Assert.AreEqual(1, CountOf(_panel.Root.parent, _base), "one copy of the sheet");

            a.Dispose();
            Assert.IsTrue(_panel.Root.parent.styleSheets.Contains(_base), "still needed by the second installation");
            b.Dispose();
            Assert.IsFalse(_panel.Root.parent.styleSheets.Contains(_base));
        }

        private static int CountOf(VisualElement holder, StyleSheet sheet)
        {
            var n = 0;
            for (var i = 0; i < holder.styleSheets.count; i++) if (holder.styleSheets[i] == sheet) n++;
            return n;
        }
    }
}
