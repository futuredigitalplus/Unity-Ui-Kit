using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UIElements;

namespace FutureDigitalPlus.UIKit.Foundation.Tests
{
    /// <summary>PlayMode tests of the profile classes (orientation, size class, platform, input mode) and of the scale driver on a live panel.</summary>
    public class ProfilePlayModeTests : PlayModeBase
    {
        static List<string> ClassesWith(VisualElement root, string prefix)
        {
            List<string> result = new List<string>();
            foreach (string name in root.GetClasses())
            {
                if (name.StartsWith(prefix))
                    result.Add(name);
            }
            return result;
        }

        [UnityTest]
        public IEnumerator Classes_ALandscapeRegularPanelGetsLandscapeAndRegular()
        {
            yield return CreatePanel(800, 600);
            UiShell shell = CreateShell();
            shell.Boot("Home");
            yield return FoundationPanel.Frames(3);
            VisualElement root = shell.Root;
            Assert.IsTrue(root.ClassListContains(UiNames.ClassLandscape));
            Assert.IsFalse(root.ClassListContains(UiNames.ClassPortrait));
            Assert.IsTrue(root.ClassListContains(UiNames.ClassRegular), "600 units on the short side is the first Regular size");
            Assert.IsFalse(root.ClassListContains(UiNames.ClassCompact));
            Assert.IsFalse(root.ClassListContains(UiNames.ClassExpanded));
            Assert.IsTrue(root.ClassListContains(UiNames.ClassRegularUp), "Regular is also regular-up");
        }

        [UnityTest]
        public IEnumerator Classes_APortraitPhoneGetsPortraitAndCompact()
        {
            yield return CreatePanel(390, 780);
            UiShell shell = CreateShell();
            shell.Boot("Home");
            yield return FoundationPanel.Frames(3);
            Assert.IsTrue(shell.Root.ClassListContains(UiNames.ClassPortrait));
            Assert.IsTrue(shell.Root.ClassListContains(UiNames.ClassCompact));
            Assert.IsFalse(shell.Root.ClassListContains(UiNames.ClassRegularUp), "a compact panel is not regular-up");
        }

        [UnityTest]
        public IEnumerator Classes_ALargeWindowGetsExpanded()
        {
            yield return CreatePanel(1280, 900);
            UiShell shell = CreateShell();
            shell.Boot("Home");
            yield return FoundationPanel.Frames(3);
            Assert.IsTrue(shell.Root.ClassListContains(UiNames.ClassExpanded));
            Assert.IsFalse(shell.Root.ClassListContains(UiNames.ClassRegular));
            Assert.IsTrue(shell.Root.ClassListContains(UiNames.ClassRegularUp), "Expanded is also regular-up (a 12.9 inch tablet gets the tablet layout)");
        }

        [UnityTest]
        public IEnumerator Classes_FollowTheGeometry_WhenThePanelScaleChanges()
        {
            yield return CreatePanel(800, 600, 1f);
            UiShell shell = CreateShell();
            shell.Boot("Home");
            yield return FoundationPanel.Frames(3);
            UiProfile last = default;
            int changes = 0;
            shell.Context.Profile.Changed += profile =>
            {
                last = profile;
                changes++;
            };

            Panel.Settings.scale = 2f; // the panel now measures 400 x 300 units
            yield return FoundationPanel.Frames(4);
            Assert.IsTrue(shell.Root.ClassListContains(UiNames.ClassCompact), "300 units on the short side");
            Assert.IsFalse(shell.Root.ClassListContains(UiNames.ClassRegular));
            Assert.AreEqual(UiSizeClass.Compact, last.SizeClass);
            Assert.GreaterOrEqual(changes, 1);

            Panel.Settings.scale = 0.5f; // 1600 x 1200 units
            yield return FoundationPanel.Frames(4);
            Assert.IsTrue(shell.Root.ClassListContains(UiNames.ClassExpanded));
        }

        [UnityTest]
        public IEnumerator Classes_ExactlyOnePlatformClass_AndItNamesTheEditorPlatform()
        {
            yield return CreatePanel();
            UiShell shell = CreateShell();
            shell.Boot("Home");
            List<string> platform = ClassesWith(shell.Root, UiNames.ClassPlatformPrefix);
            Assert.AreEqual(1, platform.Count);
            Assert.AreEqual(UiNames.ClassPlatformPrefix + UiProfileWatcher.PlatformName(UnityEngine.Device.Application.platform), platform[0]);
            yield return null;
        }

        [UnityTest]
        public IEnumerator InputMode_DefaultsToThePlatform_WhenNoSourceIsGiven()
        {
            yield return CreatePanel();
            UiShell shell = CreateShell();
            shell.Boot("Home");
            yield return FoundationPanel.Frames(2);
            string expected = UiNames.ClassInputPrefix + UiProfileWatcher.InputName(UiProfileWatcher.DefaultInputMode(UnityEngine.Device.Application.platform));
            Assert.IsTrue(shell.Root.ClassListContains(expected));
            Assert.AreEqual(1, ClassesWith(shell.Root, UiNames.ClassInputPrefix).FindAll(name => name != UiNames.ClassInputNav).Count);
        }

        [UnityTest]
        public IEnumerator InputMode_FollowsTheSource_AndNavigationClassMarksKeyboardAndGamepad()
        {
            yield return CreatePanel();
            FakeInputModeSource source = new FakeInputModeSource(UiInputMode.Pointer);
            UiShell shell = CreateShell(new UiShellOptions { InputModeSource = source, TransitionsEnabled = false, StateStore = Store });
            shell.Boot("Home");
            yield return FoundationPanel.Frames(2);
            VisualElement root = shell.Root;
            Assert.IsTrue(root.ClassListContains("fdp-input-mouse"));
            Assert.IsFalse(root.ClassListContains(UiNames.ClassInputNav));

            source.Set(UiInputMode.Gamepad);
            Assert.IsTrue(root.ClassListContains("fdp-input-gamepad"));
            Assert.IsFalse(root.ClassListContains("fdp-input-mouse"));
            Assert.IsTrue(root.ClassListContains(UiNames.ClassInputNav));

            source.Set(UiInputMode.Touch);
            Assert.IsTrue(root.ClassListContains("fdp-input-touch"));
            Assert.IsFalse(root.ClassListContains("fdp-input-gamepad"));
            Assert.IsFalse(root.ClassListContains(UiNames.ClassInputNav));

            source.Set(UiInputMode.Keyboard);
            Assert.IsTrue(root.ClassListContains(UiNames.ClassInputNav));
            Assert.AreEqual(UiInputMode.Keyboard, shell.Context.Profile.Profile.InputMode);
        }

        [UnityTest]
        public IEnumerator InputMode_TouchSwitchesTheFoundationTokensToTheTouchTier_AndBack()
        {
            yield return CreatePanel();
            FakeInputModeSource source = new FakeInputModeSource(UiInputMode.Pointer);
            UiShell shell = CreateShell(new UiShellOptions { InputModeSource = source, TransitionsEnabled = false, StateStore = Store });
            shell.Boot("Home");
            ListRow row = new ListRow { Title = "Row" };
            Layer(UiLayer.Overlays).Add(row);
            yield return FoundationPanel.Frames(3);
            Assert.AreEqual(48f, row.resolvedStyle.minHeight.value, 0.01f, "pointer tier");

            source.Set(UiInputMode.Touch);
            yield return FoundationPanel.Frames(3);
            Assert.AreEqual(56f, row.resolvedStyle.minHeight.value, 0.01f, "touch tier");

            source.Set(UiInputMode.Gamepad);
            yield return FoundationPanel.Frames(3);
            Assert.AreEqual(48f, row.resolvedStyle.minHeight.value, 0.01f, "gamepad on a desktop is the pointer tier again");
        }

        [UnityTest]
        public IEnumerator ScaleDriver_TheRuntimeCopyDrivesALiveDocument_WithoutTouchingTheSourceAsset()
        {
            yield return CreatePanel();
            PanelSettings source = Panel.Settings;
            PanelScaleMode originalMode = source.scaleMode;
            float originalScale = source.scale;
            PanelScaleDriver driver = new PanelScaleDriver(source, PanelScaleProfile.CardPortrait());
            try
            {
                driver.EnvironmentSource = () => new PanelScaleEnvironment { Handheld = false, Device = DeviceClass.Desktop, Dpi = 96f, Width = 1600, Height = 1000 };
                driver.Tick();
                Panel.Document.panelSettings = driver.Settings;
                yield return FoundationPanel.Frames(3);
                Assert.AreSame(driver.Settings, Panel.Document.panelSettings);
                Assert.AreEqual(PanelScaleMode.ConstantPixelSize, driver.Settings.scaleMode);
                Assert.AreEqual(originalMode, source.scaleMode);
                Assert.AreEqual(originalScale, source.scale);
            }
            finally
            {
                Panel.Document.panelSettings = source;
                driver.Dispose();
            }
        }

        [UnityTest]
        public IEnumerator ScaleDriver_Finding_DoesPanelSettingsScaleApplyUnderConstantPhysicalSize()
        {
            // Open question of the plan (T 7.2): is PanelSettings.scale honoured when the scale mode is ConstantPhysicalSize? The test measures the panel width in
            // units for scale 1 and scale 2 and logs the ratio. The user scale is folded into referenceDpi until this is known; the assertion only accepts the two
            // possible answers (ignored: ratio 1, honoured: ratio 2) so that an unexpected third behaviour fails.
            yield return CreatePanel();
            Panel.Settings.scaleMode = PanelScaleMode.ConstantPhysicalSize;
            Panel.Settings.referenceDpi = 160f;
            Panel.Settings.fallbackDpi = 160f;
            Panel.Settings.scale = 1f;
            yield return FoundationPanel.Frames(4);
            float widthAtOne = Panel.Root.layout.width;
            Panel.Settings.scale = 2f;
            yield return FoundationPanel.Frames(4);
            float widthAtTwo = Panel.Root.layout.width;
            Assert.Greater(widthAtOne, 0f);
            float ratio = widthAtOne / widthAtTwo;
            Debug.Log("FINDING PanelSettings.scale under ConstantPhysicalSize: width at scale 1 = " + widthAtOne + ", at scale 2 = " + widthAtTwo + ", ratio = " + ratio
                      + (Mathf.Approximately(ratio, 1f) ? " (scale is IGNORED)" : " (scale is HONOURED)"));
            Assert.IsTrue(Mathf.Abs(ratio - 1f) < 0.01f || Mathf.Abs(ratio - 2f) < 0.01f, "ratio " + ratio);
        }
    }
}
