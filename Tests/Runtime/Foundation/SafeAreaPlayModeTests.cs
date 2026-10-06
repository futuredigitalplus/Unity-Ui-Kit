using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UIElements;

namespace FutureDigitalPlus.UIKit.Foundation.Tests
{
    /// <summary>PlayMode tests of the safe area: padding from a fake device, no padding on panels that are not the screen, the television margin and the provider lookup.</summary>
    public class SafeAreaPlayModeTests : PlayModeBase
    {
        // A notch-like safe rectangle on an 800 x 600 screen (origin bottom left): left 44, bottom 21, right 44, top 21.
        static SafeAreaProvider FakeDevice(SafeAreaMode mode, float minimumMarginPercent = 0f)
        {
            return new SafeAreaProvider
            {
                SafeAreaSource = () => new Rect(44f, 21f, 712f, 558f),
                ScreenSizeSource = () => new Vector2Int(800, 600),
                Mode = mode,
                MinimumMarginPercent = minimumMarginPercent
            };
        }

        SafeAreaPanel AddPanel(string edges = "all")
        {
            SafeAreaPanel panel = new SafeAreaPanel { Edges = edges };
            panel.style.position = Position.Absolute;
            panel.style.left = 0;
            panel.style.right = 0;
            panel.style.top = 0;
            panel.style.bottom = 0;
            Layer(UiLayer.Hud).Add(panel);
            return panel;
        }

        [UnityTest]
        public IEnumerator Panel_PadsByTheDeviceInsets_InPanelUnits()
        {
            yield return CreatePanel();
            UiShell shell = CreateShell(new UiShellOptions { SafeArea = FakeDevice(SafeAreaMode.Always), TransitionsEnabled = false, StateStore = Store });
            shell.Boot("Home");
            SafeAreaPanel panel = AddPanel();
            yield return FoundationPanel.Frames(3);
            shell.Tick();

            Assert.AreEqual(44f, panel.resolvedStyle.paddingLeft, 0.01f);
            Assert.AreEqual(44f, panel.resolvedStyle.paddingRight, 0.01f);
            Assert.AreEqual(21f, panel.resolvedStyle.paddingTop, 0.01f);
            Assert.AreEqual(21f, panel.resolvedStyle.paddingBottom, 0.01f);
        }

        [UnityTest]
        public IEnumerator Panel_PadsOnlyTheEdgesItAsksFor()
        {
            yield return CreatePanel();
            UiShell shell = CreateShell(new UiShellOptions { SafeArea = FakeDevice(SafeAreaMode.Always), TransitionsEnabled = false, StateStore = Store });
            shell.Boot("Home");
            SafeAreaPanel panel = AddPanel("top,left");
            yield return FoundationPanel.Frames(3);
            shell.Tick();

            Assert.AreEqual(44f, panel.resolvedStyle.paddingLeft, 0.01f);
            Assert.AreEqual(21f, panel.resolvedStyle.paddingTop, 0.01f);
            Assert.AreEqual(0f, panel.resolvedStyle.paddingRight, 0.01f);
            Assert.AreEqual(0f, panel.resolvedStyle.paddingBottom, 0.01f);
        }

        [UnityTest]
        public IEnumerator Panel_ARenderTextureIsNotTheScreen_SoItGetsNoPadding()
        {
            yield return CreatePanel();
            UiShell shell = CreateShell(new UiShellOptions { SafeArea = FakeDevice(SafeAreaMode.Auto), TransitionsEnabled = false, StateStore = Store });
            shell.Boot("Home");
            SafeAreaPanel panel = AddPanel();
            yield return FoundationPanel.Frames(3);
            shell.Tick();

            Assert.AreEqual(UiPanelKind.RenderTexture, shell.Context.SafeArea.PanelKind);
            Assert.AreEqual(0f, panel.resolvedStyle.paddingLeft, 0.01f);
            Assert.AreEqual(0f, panel.resolvedStyle.paddingTop, 0.01f);
            Assert.AreEqual(0f, shell.Context.SafeArea.PixelInsets.Left);
        }

        [UnityTest]
        public IEnumerator Panel_NeverModeAppliesNothing_EvenOnTheScreen()
        {
            yield return CreatePanel();
            UiShell shell = CreateShell(new UiShellOptions { SafeArea = FakeDevice(SafeAreaMode.Never), TransitionsEnabled = false, StateStore = Store });
            shell.Boot("Home");
            SafeAreaPanel panel = AddPanel();
            yield return FoundationPanel.Frames(3);
            shell.Tick();
            Assert.AreEqual(0f, panel.resolvedStyle.paddingLeft, 0.01f);
        }

        [UnityTest]
        public IEnumerator Panel_TheTelevisionMarginIsMergedWithTheDeviceInsets_TheLargerWins()
        {
            yield return CreatePanel();
            // 5 percent of 800 x 600 is 40 x 30: larger than the device's 21 vertical inset, smaller than its 44 horizontal inset.
            UiShell shell = CreateShell(new UiShellOptions { SafeArea = FakeDevice(SafeAreaMode.Always, 5f), TransitionsEnabled = false, StateStore = Store });
            shell.Boot("Home");
            SafeAreaPanel panel = AddPanel();
            yield return FoundationPanel.Frames(3);
            shell.Tick();

            Assert.AreEqual(44f, panel.resolvedStyle.paddingLeft, 0.01f, "the device inset is larger than the margin");
            Assert.AreEqual(30f, panel.resolvedStyle.paddingTop, 0.01f, "the margin is larger than the device inset");
        }

        [UnityTest]
        public IEnumerator Panel_FollowsChangesOfTheDevice()
        {
            yield return CreatePanel();
            Rect safe = new Rect(0f, 0f, 800f, 600f);
            SafeAreaProvider provider = new SafeAreaProvider
            {
                SafeAreaSource = () => safe,
                ScreenSizeSource = () => new Vector2Int(800, 600),
                Mode = SafeAreaMode.Always,
                MinimumMarginPercent = 0f
            };
            UiShell shell = CreateShell(new UiShellOptions { SafeArea = provider, TransitionsEnabled = false, StateStore = Store });
            shell.Boot("Home");
            SafeAreaPanel panel = AddPanel();
            yield return FoundationPanel.Frames(3);
            shell.Tick();
            Assert.AreEqual(0f, panel.resolvedStyle.paddingTop, 0.01f);

            int changes = 0;
            provider.Changed += insets => changes++;
            safe = new Rect(0f, 0f, 800f, 560f); // a status bar appears: 40 at the top
            shell.Tick();
            yield return null;
            Assert.AreEqual(40f, panel.resolvedStyle.paddingTop, 0.01f);
            Assert.AreEqual(1, changes);
            shell.Tick();
            Assert.AreEqual(1, changes, "an unchanged device raises no event");
        }

        [UnityTest]
        public IEnumerator Panel_TwoShellsKeepTheirOwnInsets()
        {
            yield return CreatePanel();
            UiShell first = CreateShell(new UiShellOptions { SafeArea = FakeDevice(SafeAreaMode.Always), TransitionsEnabled = false, StateStore = Store });
            first.Boot("Home");
            SafeAreaPanel inFirst = AddPanel();

            VisualElement otherHost = new VisualElement();
            otherHost.style.position = Position.Absolute;
            otherHost.style.left = 0;
            otherHost.style.right = 0;
            otherHost.style.top = 0;
            otherHost.style.bottom = 0;
            Panel.Root.Add(otherHost);
            UiShell second = new UiShell(otherHost, new UiShellOptions { SafeArea = new SafeAreaProvider { Mode = SafeAreaMode.Never }, TransitionsEnabled = false });
            try
            {
                SafeAreaPanel inSecond = new SafeAreaPanel();
                second.Context.GetLayer(UiLayer.Hud).Add(inSecond);
                yield return FoundationPanel.Frames(3);
                first.Tick();
                second.Tick();
                Assert.AreEqual(44f, inFirst.resolvedStyle.paddingLeft, 0.01f);
                Assert.AreEqual(0f, inSecond.resolvedStyle.paddingLeft, 0.01f);
            }
            finally
            {
                second.Dispose();
            }
        }

        [UnityTest]
        public IEnumerator Panel_APanelOutsideAnyShellFollowsTheDefaultProvider()
        {
            yield return CreatePanel();
            SafeAreaPanel loose = new SafeAreaPanel();
            Panel.Root.Add(loose);
            yield return FoundationPanel.Frames(2);
            Assert.AreSame(SafeAreaProvider.Default, loose.Provider);
            SafeAreaProvider pinned = new SafeAreaProvider();
            loose.Provider = pinned;
            Assert.AreSame(pinned, loose.Provider);
            loose.Provider = null;
            Assert.AreSame(SafeAreaProvider.Default, loose.Provider);
        }

        [UnityTest]
        public IEnumerator Dialog_TheCardStaysInsideTheSafeAreaAndTheGutter()
        {
            yield return CreatePanel();
            UiShell shell = CreateShell(new UiShellOptions { SafeArea = FakeDevice(SafeAreaMode.Always), TransitionsEnabled = false, StateStore = Store });
            shell.Boot("Home");
            yield return FoundationPanel.Frames(3);
            shell.Tick();
            shell.Context.Dialogs.Alert(UiText.FromLiteral("Hi"), UiText.FromLiteral("Inside the safe area."));
            yield return Real(0.4f);
            Rect card = Layer(UiLayer.Dialogs).Q<VisualElement>(UiNames.DialogPanel).worldBound;
            Assert.GreaterOrEqual(card.xMin, 44f - 0.5f);
            Assert.LessOrEqual(card.xMax, 756f + 0.5f);
            Assert.GreaterOrEqual(card.yMin, 21f - 0.5f);
            Assert.LessOrEqual(card.yMax, 579f + 0.5f);
        }
    }
}
