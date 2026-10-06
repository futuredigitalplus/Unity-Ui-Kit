using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UIElements;

namespace FutureDigitalPlus.UIKit.Foundation.Tests
{
    /// <summary>PlayMode tests of the shell on a real panel: the end-to-end flow, screen transitions, back handling through UI events, input gating and teardown.</summary>
    public class ShellPlayModeTests : PlayModeBase
    {
        /// <summary>The acceptance sample: Home, Settings, a dialog, a toast and a loading period, in one flow and with no singleton, no Input System, no Localization.</summary>
        [UnityTest]
        public IEnumerator Sample_HomeThenSettingsThenDialogToastAndLoading_AllWorkTogether()
        {
            yield return CreatePanel();
            UiShell shell = CreateShell(new UiShellOptions { StateStore = Store, TransitionsEnabled = false });
            shell.Boot("Home");
            yield return FoundationPanel.Frames();
            Assert.AreEqual("Home", shell.Router.Current);

            shell.Router.Push("Settings");
            yield return FoundationPanel.Frames();
            Assert.AreEqual("Settings", shell.Router.Current);

            bool? restart = null;
            shell.Context.Dialogs.Confirm(UiText.FromLiteral("Restart?"), UiText.FromLiteral("Progress is lost."), answer => restart = answer);
            yield return FoundationPanel.Frames();
            Button yes = Layer(UiLayer.Dialogs).Query<Button>().ToList()[1];
            Panel.Click(yes);
            yield return FoundationPanel.Frames();
            Assert.AreEqual(true, restart);
            Assert.IsFalse(shell.Context.Dialogs.IsModalOpen);

            shell.Context.Toasts.ShowRaw("Restarted", ToastKind.Success);
            yield return FoundationPanel.Frames();
            Assert.AreEqual("Restarted", Layer(UiLayer.Toasts).Q<Label>(UiNames.ToastLabel).text);

            using (shell.Context.Loading.Begin(UiText.FromLiteral("Loading")))
            {
                yield return FoundationPanel.Frames();
                Assert.IsTrue(shell.Context.Input.IsBlocked);
            }
            Assert.IsFalse(shell.Context.Input.IsBlocked);

            Assert.IsTrue(shell.RequestBack());
            Assert.AreEqual("Home", shell.Router.Current);
        }

        [UnityTest]
        public IEnumerator Transitions_PlayAsClasses_BlockInputWhileTheyRun_AndLeaveACleanTreeBehind()
        {
            yield return CreatePanel();
            UiShell shell = CreateShell(new UiShellOptions { StateStore = Store, TransitionsEnabled = true });
            shell.Boot("Home");
            yield return Real(0.1f);
            VisualElement home = shell.Router.CurrentScreen.Root;

            shell.Router.Push("Settings");
            VisualElement settings = shell.Router.CurrentScreen.Root;
            Assert.IsTrue(settings.ClassListContains(UiNames.ClassScreenEnter), "the new screen starts in its enter state");
            Assert.IsTrue(home.ClassListContains(UiNames.ClassScreenBehind));
            Assert.IsTrue(shell.Context.Input.IsBlocked, "a tap during a transition cannot start a second navigation");

            yield return Real(0.5f);
            Assert.IsFalse(settings.ClassListContains(UiNames.ClassScreenEnter), "the enter class is gone, so the transition ran");
            Assert.AreEqual(DisplayStyle.None, home.style.display.value, "the covered screen is hidden but alive");
            Assert.IsFalse(home.ClassListContains(UiNames.ClassScreenBehind));
            Assert.IsFalse(shell.Context.Input.IsBlocked);

            shell.Router.Pop();
            Assert.IsTrue(settings.ClassListContains(UiNames.ClassScreenExit));
            yield return Real(0.5f);
            Assert.IsNull(settings.parent, "the popped screen is removed after its exit transition");
            Assert.Contains("Settings:dispose", Log);
            Assert.IsFalse(shell.Context.Input.IsBlocked);
        }

        [UnityTest]
        public IEnumerator Transitions_UseTheDurationThatTheRouterConstantNames()
        {
            yield return CreatePanel();
            UiShell shell = CreateShell();
            shell.Boot("Home");
            yield return FoundationPanel.Frames(3);
            IEnumerable<TimeValue> durations = shell.Router.CurrentScreen.Root.resolvedStyle.transitionDuration;
            bool found = false;
            foreach (TimeValue duration in durations)
            {
                found = true;
                Assert.AreEqual((float)ScreenRouter.TransitionMilliseconds, Millis(duration), 0.5f);
            }
            Assert.IsTrue(found, "the screen has a transition-duration from Foundation.uss");
        }

        [UnityTest]
        public IEnumerator Transitions_AreSkippedWhenTheShellTurnsThemOff()
        {
            yield return CreatePanel();
            UiShell shell = CreateShell();
            shell.Boot("Home");
            shell.Router.Push("Settings");
            Assert.IsFalse(shell.Router.CurrentScreen.Root.ClassListContains(UiNames.ClassScreenEnter));
            Assert.IsFalse(shell.Context.Input.IsBlocked);
            Assert.AreEqual(DisplayStyle.None, shell.Router.ScreenAt(0).Root.style.display.value);
            yield return null;
        }

        [UnityTest]
        public IEnumerator Back_ANavigationCancelEventPopsTheScreen()
        {
            yield return CreatePanel();
            UiShell shell = CreateShell();
            shell.Boot("Home");
            shell.Router.Push("Settings");
            yield return FoundationPanel.Frames();
            Panel.Cancel(shell.Root);
            Assert.AreEqual("Home", shell.Router.Current);
        }

        [UnityTest]
        public IEnumerator Back_TwoReportsOfTheSamePressInOneFrame_PopOnlyOnce()
        {
            yield return CreatePanel();
            UiShell shell = CreateShell();
            shell.Boot("Home");
            shell.Router.Push("Settings");
            shell.Router.Push("About");
            yield return FoundationPanel.Frames();
            Panel.Cancel(shell.Root);
            Panel.Cancel(shell.Root);
            Assert.AreEqual("Settings", shell.Router.Current, "Escape and the navigation event of one key press must not pop two screens");
            yield return null;
            Panel.Cancel(shell.Root);
            Assert.AreEqual("Home", shell.Router.Current, "the next frame is a new press");
        }

        [UnityTest]
        public IEnumerator Back_NavigationCancelIsIgnoredWhenTheOptionIsOff()
        {
            yield return CreatePanel();
            UiShell shell = CreateShell(new UiShellOptions { HandleNavigationCancel = false, TransitionsEnabled = false, StateStore = Store });
            shell.Boot("Home");
            shell.Router.Push("Settings");
            yield return FoundationPanel.Frames();
            Panel.Cancel(shell.Root);
            Assert.AreEqual("Settings", shell.Router.Current);
        }

        [UnityTest]
        public IEnumerator Screen_ABuiltButtonReceivesClicks_AndItsListenerIsRemovedWithTheScreen()
        {
            yield return CreatePanel();
            UiShell shell = CreateShell();
            shell.Boot("Home");
            yield return FoundationPanel.Frames();
            Button button = ((ProbeScreen)shell.Router.CurrentScreen).Button;
            Panel.Click(button);
            Assert.AreEqual(1, Log.FindAll(entry => entry == "Home:click").Count);
            shell.Router.Replace("Settings");
            yield return FoundationPanel.Frames();
            Panel.Click(button);
            Assert.AreEqual(1, Log.FindAll(entry => entry == "Home:click").Count, "the disposed screen no longer listens");
        }

        [UnityTest]
        public IEnumerator InputGate_OnlySeesElementsOfItsOwnShell()
        {
            yield return CreatePanel();
            UiShell first = CreateShell();
            first.Catalog.Register("Hud", () => new ProbeScreen("Hud", Log, PickingMode.Ignore));
            first.Boot("Hud");
            yield return FoundationPanel.Frames(3);
            Vector2 buttonCenter = ((ProbeScreen)first.Router.CurrentScreen).Button.worldBound.center;

            VisualElement otherHost = new VisualElement();
            otherHost.style.position = Position.Absolute;
            otherHost.style.left = 0;
            otherHost.style.right = 0;
            otherHost.style.top = 0;
            otherHost.style.bottom = 0;
            otherHost.pickingMode = PickingMode.Ignore;
            Panel.Root.Add(otherHost);
            UiShell second = new UiShell(otherHost, new UiShellOptions { TransitionsEnabled = false });
            try
            {
                yield return FoundationPanel.Frames(3);
                Assert.IsTrue(first.Context.Input.IsOverUiAtPanelPosition(buttonCenter), "the first shell's own button");
                Assert.IsFalse(second.Context.Input.IsOverUiAtPanelPosition(buttonCenter), "another shell's button does not count");
                Assert.IsFalse(first.Context.Input.IsOverUiAtPanelPosition(new Vector2(780f, 580f)), "empty area of the shell lets game input through");
            }
            finally
            {
                second.Dispose();
            }
        }

        [UnityTest]
        public IEnumerator Idle_TheLayersAboveTheScreensCatchNoPointer()
        {
            yield return CreatePanel();
            UiShell shell = CreateShell();
            shell.Boot("Home");
            yield return FoundationPanel.Frames(3);
            for (int x = 0; x <= 10; x++)
            {
                for (int y = 0; y <= 10; y++)
                {
                    VisualElement picked = Panel.Panel.Pick(new Vector2(x * 79f, y * 59f));
                    for (VisualElement up = picked; up != null; up = up.parent)
                    {
                        Assert.AreNotSame(Layer(UiLayer.Loading), up, "the invisible loading card must not catch a tap at " + x + "," + y);
                        Assert.AreNotSame(Layer(UiLayer.Toasts), up, "an idle toast layer must not catch a tap at " + x + "," + y);
                        Assert.AreNotSame(Layer(UiLayer.Dialogs), up, "an idle dialog layer must not catch a tap at " + x + "," + y);
                    }
                }
            }
        }

        [UnityTest]
        public IEnumerator InputGate_AModalDialogCoversTheWholeScreen()
        {
            yield return CreatePanel();
            UiShell shell = CreateShell();
            shell.Boot("Home");
            shell.Context.Dialogs.Alert(UiText.FromLiteral("Hi"), UiText.Empty);
            yield return FoundationPanel.Frames(3);
            Assert.IsTrue(shell.Context.Input.IsBlocked);
            Assert.IsTrue(shell.Context.Input.IsOverUiAtPanelPosition(new Vector2(5f, 5f)), "the scrim absorbs a tap outside the dialog card");
        }

        [UnityTest]
        public IEnumerator TextBinding_FollowsNotifyChanged_WhileAttached_AndStopsAfterDetach()
        {
            yield return CreatePanel();
            string language = "en";
            UiTextSource source = new UiTextSource(text => language + ":" + text.Key);
            UiShell shell = CreateShell(new UiShellOptions { Text = source, TransitionsEnabled = false, StateStore = Store });
            shell.Boot("Home");
            Label label = new Label();
            Layer(UiLayer.Hud).Add(label);
            yield return FoundationPanel.Frames();
            shell.Context.SetText(label, UiText.FromKey("PLAY"));
            Assert.AreEqual("en:PLAY", label.text);

            language = "de";
            source.NotifyChanged();
            Assert.AreEqual("de:PLAY", label.text, "an attached label re-resolves when the locale changes");

            label.RemoveFromHierarchy();
            yield return null;
            language = "fr";
            source.NotifyChanged();
            Assert.AreEqual("de:PLAY", label.text, "a detached label does not");

            Layer(UiLayer.Hud).Add(label);
            yield return FoundationPanel.Frames();
            Assert.AreEqual("fr:PLAY", label.text, "and catches up when it is attached again");
        }

        [UnityTest]
        public IEnumerator Teardown_WhileEverythingIsBusy_LeavesNothingRunning()
        {
            yield return CreatePanel();
            UiShell shell = CreateShell(new UiShellOptions { StateStore = Store, TransitionsEnabled = true });
            shell.Boot("Home");
            yield return Real(0.1f);
            shell.Router.Push("Settings");
            shell.Context.Toasts.ShowRaw("Busy");
            shell.Context.Dialogs.Alert(UiText.FromLiteral("Open"), UiText.Empty);
            LoadingToken token = shell.Context.Loading.Begin(UiText.FromLiteral("Loading"));
            yield return Real(0.3f);

            shell.Dispose();
            Shell = null;

            // Timers that were scheduled must not fire into the disposed shell: any exception would fail the test through the log.
            yield return Real(0.8f);
            token.Dispose();
            Assert.IsNull(shell.Root.panel, "the shell root left the panel");
            Assert.IsFalse(shell.Context.Input.IsBlocked);
            Assert.AreSame(SafeAreaProvider.Default, SafeAreaProvider.For(shell.Root), "the shell no longer serves safe-area panels");
        }

        [UnityTest]
        public IEnumerator Panels_TwoShellsInOnePanelNavigateIndependently()
        {
            yield return CreatePanel();
            UiShell first = CreateShell();
            first.Boot("Home");
            VisualElement otherHost = new VisualElement();
            Panel.Root.Add(otherHost);
            UiShell second = new UiShell(otherHost, new UiShellOptions { TransitionsEnabled = false });
            try
            {
                second.Catalog.Register("Other", () => new ProbeScreen("Other", Log));
                second.Boot("Other");
                first.Router.Push("Settings");
                Assert.AreEqual("Settings", first.Router.Current);
                Assert.AreEqual("Other", second.Router.Current);
                Panel.Cancel(first.Root);
                Assert.AreEqual("Home", first.Router.Current);
                Assert.AreEqual("Other", second.Router.Current);
            }
            finally
            {
                second.Dispose();
            }
            yield return null;
        }
    }
}
