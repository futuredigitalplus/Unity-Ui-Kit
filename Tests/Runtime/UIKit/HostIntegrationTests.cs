using System.Collections;
using FutureDigitalPlus.UIKit.DesignSystem.Theme.Applier;
using FutureDigitalPlus.UIKit.DesignSystem.Theme.Data;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UIElements;

namespace FutureDigitalPlus.UIKit.Tests
{
    /// <summary>
    /// What three scratch host games (2D URP, 3D Built-in, HDRP with a gamepad) found when they followed GettingStarted.md: the facade next to a shell, screens
    /// built after the install, and a text size set while a theme is worn.
    /// </summary>
    public class HostIntegrationTests
    {
        private PanelHarness _panel;

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
        }

        [UnityTest]
        public IEnumerator SafeArea_IsPaddedByTheHost_UntilAShellAppears_ThenLeftToTheShell()
        {
            yield return CreatePanel();
            var root = _panel.Root;
            var kit = UIKitInstaller.Install(root, new UIKitOptions { Mobile = MobileMode.Off });
            // a phone with a notch on top and a home indicator at the bottom
            kit.ScreenSizeSource = () => new Vector2(PanelHarness.Width, PanelHarness.Height);
            kit.SafeAreaSource = () => new Rect(0, 30, PanelHarness.Width, PanelHarness.Height - 30 - 40);
            kit.Refresh();
            yield return PanelHarness.Frames();
            Assert.Greater(root.resolvedStyle.paddingTop, 0f, "without a shell the host pads the root");
            Assert.Greater(kit.AppliedInsets.Top, 0f);

            // the shell's root class is the only link between the facade and the Foundation module
            var shell = new VisualElement { name = "shell-root" };
            shell.AddToClassList("fdp-shell");
            root.Add(shell);
            kit.Refresh();
            yield return PanelHarness.Frames();
            Assert.AreEqual(0f, root.resolvedStyle.paddingTop, 0.01f, "with a shell under the root the host gives its padding back");
            Assert.AreEqual(0f, kit.AppliedInsets.Top, 0.01f);

            kit.Dispose();
        }

        [UnityTest]
        public IEnumerator DefaultSafeArea_IsNotAppliedToAPanelThatRendersIntoATexture()
        {
            yield return CreatePanel();
            var oldDevice = UIKitInstallation.DeviceSafeArea;
            UIKitInstallation.DeviceSafeArea = () => new Rect(0, 100, 400, 500); // a notch the in-game monitor must not inherit
            try
            {
                var kit = UIKitInstaller.Install(_panel.Root, new UIKitOptions { Mobile = MobileMode.Off });
                kit.Refresh();
                yield return PanelHarness.Frames();
                Assert.AreEqual(0f, kit.AppliedInsets.Top, 0.01f, "a RenderTexture panel has no notch");
                Assert.AreEqual(0f, _panel.Root.resolvedStyle.paddingTop, 0.01f);

                kit.SafeAreaSource = () => new Rect(0, 30, Screen.width, Screen.height - 30);
                kit.Refresh();
                Assert.Greater(kit.AppliedInsets.Bottom, 0f, "a source the caller assigns is applied to any panel");
                kit.Dispose();
            }
            finally
            {
                UIKitInstallation.DeviceSafeArea = oldDevice;
            }
        }

        [UnityTest]
        public IEnumerator TouchClass_IsLeftToTheInputProfile_OnceAShellAppears()
        {
            yield return CreatePanel();
            var root = _panel.Root;
            var kit = UIKitInstaller.Install(root, new UIKitOptions { Mobile = MobileMode.On, ApplySafeArea = false });
            Assert.IsTrue(kit.IsMobile, "without a shell the host writes the class");

            var shell = new VisualElement { name = "shell-root" };
            shell.AddToClassList("fdp-shell");
            root.Add(shell);
            kit.Options.Mobile = MobileMode.Off;
            root.EnableInClassList(UIKitInstaller.MobileClass, true); // what the input profile does for a touch device
            kit.Refresh();
            Assert.IsTrue(kit.IsMobile, "with a shell under the root the host no longer touches the class");

            kit.Dispose();
        }

        [UnityTest]
        public IEnumerator Behaviours_AreScannedAgain_SoAToggleAddedLaterGetsItsKnob()
        {
            yield return CreatePanel();
            var kit = UIKitInstaller.Install(_panel.Root, new UIKitOptions { Mobile = MobileMode.Off, ApplySafeArea = false });

            var toggle = new Toggle("Sound");
            toggle.AddToClassList("ds-toggle");
            _panel.Root.Add(toggle);
            Assert.IsNull(toggle.Q(className: "ds-toggle__knob"), "nothing has scanned yet");

            var end = Time.realtimeSinceStartup + 2f;
            while (toggle.Q(className: "ds-toggle__knob") == null && Time.realtimeSinceStartup < end) yield return null;
            Assert.IsNotNull(toggle.Q(className: "ds-toggle__knob"), "an element added after the install gets its behaviour from the periodic scan");

            kit.Dispose();
        }

        [UnityTest]
        public IEnumerator TextScaleStep_StillWins_WhenAThemeIsAppliedAfterIt()
        {
            yield return CreatePanel();
            var root = _panel.Root;
            var a11y = Resources.Load<StyleSheet>("FutureDigitalPlus/UIKit/Interaction/Styles/A11y");
            Assert.IsNotNull(a11y, "A11y sheet");
            root.styleSheets.Add(a11y);                                       // like InteractionStyles.Install
            var kit = UIKitInstaller.Install(root, new UIKitOptions
            {
                Theme = Resources.Load<ThemeData>("FutureDigitalPlus/UIKit/DesignSystem/Themes/Light"),   // added after A11y, so it is last in the list
                Mobile = MobileMode.Off,
                ApplySafeArea = false,
            });
            var label = new Label("Hello");
            label.AddToClassList("ds-body-1");
            root.Add(label);
            yield return PanelHarness.Frames();
            var normal = label.resolvedStyle.fontSize;

            root.AddToClassList("fdp-text-scale-150");
            yield return PanelHarness.Frames();
            Assert.Greater(label.resolvedStyle.fontSize, normal, "a text size step enlarges text under a worn theme (the theme sheet comes last)");

            kit.SetTheme(Resources.Load<ThemeData>("FutureDigitalPlus/UIKit/DesignSystem/Themes/Dark"));   // re-adds a theme sheet last, again
            yield return PanelHarness.Frames();
            Assert.Greater(label.resolvedStyle.fontSize, normal, "and after a theme switch");

            kit.Dispose();
        }
    }
}
