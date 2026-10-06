using System;
using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UIElements;

namespace FutureDigitalPlus.UIKit.Foundation.Tests
{
    /// <summary>
    /// Hostile-usage tests of the foundation services: navigation during navigation, dialogs shown from dialog callbacks, a panel destroyed in the middle of
    /// a transition, services used after they were disposed, and arguments a game should never send but sometimes does.
    /// </summary>
    public class AdversarialPlayModeTests : PlayModeBase
    {
        sealed class ThrowingScreen : UiScreen
        {
            readonly string _where;

            public ThrowingScreen(string where)
            {
                _where = where;
            }

            protected override void OnBuild()
            {
                if (_where == "build")
                    throw new InvalidOperationException("build");
            }

            protected override void OnShow(object args)
            {
                if (_where == "show")
                    throw new InvalidOperationException("show");
            }

            protected override void OnHide()
            {
                if (_where == "hide")
                    throw new InvalidOperationException("hide");
            }

            protected override void OnDispose()
            {
                if (_where == "dispose")
                    throw new InvalidOperationException("dispose");
            }
        }

        VisualElement Outlet
        {
            get { return Layer(UiLayer.Screens); }
        }

        int DisplayedScreens()
        {
            int shown = 0;
            foreach (VisualElement child in Outlet.Children())
            {
                if (child.resolvedStyle.display != DisplayStyle.None && child.style.display != DisplayStyle.None)
                    shown++;
            }
            return shown;
        }

        [UnityTest]
        public IEnumerator Router_RapidNavigationWithTransitions_LeavesOneVisibleScreenAndNoBlock()
        {
            yield return CreatePanel();
            UiShell shell = CreateShell(new UiShellOptions { TransitionsEnabled = true });
            shell.Boot("Home");
            yield return FoundationPanel.Frames(2);

            string[] ids = { "Home", "Settings", "About" };
            System.Random random = new System.Random(7);
            for (int frame = 0; frame < 25; frame++)
            {
                for (int i = 0; i < 4; i++)
                {
                    switch (random.Next(5))
                    {
                        case 0: shell.Router.Push(ids[random.Next(3)]); break;
                        case 1: shell.Router.Pop(); break;
                        case 2: shell.Router.Replace(ids[random.Next(3)]); break;
                        case 3: shell.Router.PopTo(ids[random.Next(3)]); break;
                        default: shell.Router.ResetTo(ids[random.Next(3)]); break;
                    }
                }
                yield return null;
            }
            yield return Real(0.7f);
            yield return FoundationPanel.Frames(2);

            Assert.AreEqual(shell.Router.Depth, Outlet.childCount, "every screen on the stack has its tree and nothing else is left behind");
            Assert.AreEqual(1, DisplayedScreens(), "exactly the top screen is displayed");
            foreach (VisualElement child in Outlet.Children())
            {
                Assert.IsFalse(child.ClassListContains(UiNames.ClassScreenEnter), "enter class left on " + child.name);
                Assert.IsFalse(child.ClassListContains(UiNames.ClassScreenEnterBack), "enter-back class left on " + child.name);
                Assert.IsFalse(child.ClassListContains(UiNames.ClassScreenExit), "exit class left on " + child.name);
                Assert.IsFalse(child.ClassListContains(UiNames.ClassScreenBehind), "behind class left on " + child.name);
            }
            Assert.AreEqual(0, shell.Context.Input.Blocks.Count, "the transition block was released");
        }

        [UnityTest]
        public IEnumerator Router_ChangedHandlerThatNavigates_KeepsTheStackConsistent()
        {
            yield return CreatePanel();
            UiShell shell = CreateShell(new UiShellOptions { TransitionsEnabled = true });
            shell.Boot("Home");
            shell.Router.Changed += (from, to) =>
            {
                if (to == "Settings")
                    shell.Router.Push("About");
                if (to == "About" && from == "Settings")
                    shell.Router.Replace("Home");
            };

            shell.Router.Push("Settings");
            yield return Real(0.6f);

            Assert.AreEqual(shell.Router.Depth, Outlet.childCount);
            Assert.AreEqual("Home", shell.Router.Current);
            Assert.AreEqual(1, DisplayedScreens());
            Assert.AreEqual(0, shell.Context.Input.Blocks.Count);
        }

        [UnityTest]
        public IEnumerator Router_ScreensThatThrowAnywhere_NeverBreakNavigationOrLeakTrees()
        {
            yield return CreatePanel();
            LogAssert.ignoreFailingMessages = true;
            UiShell shell = CreateShell();
            foreach (string where in new[] { "build", "show", "hide", "dispose" })
            {
                string captured = where;
                shell.Catalog.Register("T-" + where, () => new ThrowingScreen(captured));
            }
            shell.Boot("Home");

            foreach (string where in new[] { "build", "show", "hide", "dispose" })
            {
                shell.Router.Push("T-" + where);
                shell.Router.Push("Settings");
                shell.Router.Pop();
                shell.Router.Pop();
            }
            shell.Router.Replace("About");
            shell.Router.ResetTo("Home");

            Assert.AreEqual("Home", shell.Router.Current);
            Assert.AreEqual(shell.Router.Depth, Outlet.childCount);
            yield return null;
        }

        [UnityTest]
        public IEnumerator Router_UsedAfterDispose_DoesNothingAndLeaksNothing()
        {
            yield return CreatePanel();
            UiShell shell = CreateShell(new UiShellOptions { TransitionsEnabled = true });
            shell.Boot("Home");
            shell.Router.Dispose();
            LogAssert.ignoreFailingMessages = true;

            shell.Router.Push("Settings");
            shell.Router.Replace("About");
            shell.Router.ResetTo("Home");
            yield return Real(0.4f);

            Assert.AreEqual(0, Outlet.childCount, "a disposed router must not build screens");
            Assert.AreEqual(0, shell.Context.Input.Blocks.Count, "a disposed router must not hold an input block");
        }

        [UnityTest]
        public IEnumerator Router_PanelDestroyedMidTransition_ShellDisposeReleasesTheBlock()
        {
            yield return CreatePanel();
            UiShell shell = CreateShell(new UiShellOptions { TransitionsEnabled = true });
            shell.Boot("Home");
            yield return FoundationPanel.Frames(2);
            shell.Router.Push("Settings");
            Assert.IsTrue(shell.Context.Input.IsBlocked, "the transition blocks input");

            UnityEngine.Object.Destroy(Panel.Host);
            yield return null;
            yield return null;
            shell.Dispose();

            Assert.IsFalse(shell.Context.Input.IsBlocked);
            Assert.AreEqual(0, shell.Context.Input.Blocks.Count);
        }

        [UnityTest]
        public IEnumerator Router_BackDuringATransition_DoesNotDoublePop()
        {
            yield return CreatePanel();
            UiShell shell = CreateShell(new UiShellOptions { TransitionsEnabled = true });
            shell.Boot("Home");
            shell.Router.Push("Settings");
            shell.Router.Push("About");
            yield return Real(0.5f);

            for (int i = 0; i < 10; i++)
                shell.Router.Back();
            yield return Real(0.5f);

            Assert.AreEqual("Home", shell.Router.Current);
            Assert.AreEqual(1, shell.Router.Depth);
            Assert.AreEqual(1, Outlet.childCount);
        }

        [UnityTest]
        public IEnumerator Router_RestoreStateFromGarbage_ChangesNothing()
        {
            yield return CreatePanel();
            UiShell shell = CreateShell();
            shell.Boot("Home");
            shell.Router.Push("Settings");

            foreach (string garbage in new[] { "", "{", "null", "[]", "{\"stack\":[]}", "\0\0\0", new string('x', 70000), "{\"stack\":[{\"id\":\"Nope\"}]}" })
                Assert.IsFalse(shell.Router.TryRestoreState(garbage), "garbage '" + (garbage.Length > 20 ? garbage.Substring(0, 20) : garbage) + "'");
            Assert.IsFalse(shell.Router.TryRestoreState(null));

            Assert.AreEqual("Settings", shell.Router.Current);
            Assert.AreEqual(2, shell.Router.Depth);
            Assert.AreEqual(2, Outlet.childCount);
            yield return null;
        }

        [UnityTest]
        public IEnumerator Router_ArgumentsFromTheOutsideWorld_AreRefusedWithoutThrowing()
        {
            yield return CreatePanel();
            UiShell shell = CreateShell();
            shell.Boot("Home");
            LogAssert.ignoreFailingMessages = true;

            shell.Router.Push(null);
            shell.Router.Push("");
            shell.Router.Push("DoesNotExist");
            shell.Router.Replace(null);
            shell.Router.ResetTo("");
            Assert.IsFalse(shell.Router.PopTo(null));
            Assert.IsFalse(shell.Router.PopTo("DoesNotExist"));
            Assert.IsFalse(shell.Router.Pop());
            Assert.Throws<ArgumentNullException>(() => shell.Router.Register("X", null));
            Assert.Throws<ArgumentException>(() => shell.Router.Register(null, () => new ProbeScreen("X", Log)));
            Assert.AreEqual("Home", shell.Router.Current);
            Assert.AreEqual(1, Outlet.childCount);
            yield return null;
        }

        [UnityTest]
        public IEnumerator Router_ChangingSceneRootsAcrossShells_DoesNotShareStateBetweenShells()
        {
            yield return CreatePanel();
            UiShell first = CreateShell();
            first.Boot("Home");
            first.Router.Push("Settings");
            first.Dispose();
            Shell = null;

            UiShell second = new UiShell(Panel.Root, new UiShellOptions { TransitionsEnabled = false, UseDefaultCatalog = false });
            Shell = second;
            second.Catalog.Register("Home", () => new ProbeScreen("Home", Log));
            second.Boot("Home");

            Assert.AreEqual("Home", second.Router.Current);
            Assert.AreEqual(1, second.Router.Depth);
            Assert.AreEqual(1, second.Root.parent.Query<VisualElement>(UiNames.Root).ToList().Count, "exactly one shell root under the host");
            yield return null;
        }

        [UnityTest]
        public IEnumerator Dialogs_AChainOfDialogsShownFromCallbacks_ShowsOneAtATimeAndEndsClean()
        {
            yield return CreatePanel();
            UiShell shell = CreateShell();
            shell.Boot("Home");
            int shown = 0;
            int max = 40;
            Action<DialogOutcome> next = null;
            next = outcome =>
            {
                if (shown >= max)
                    return;
                shown++;
                shell.Context.Dialogs.Show(new DialogRequest { Title = UiText.FromLiteral("D" + shown), Callback = next });
            };
            next(default(DialogOutcome));
            yield return FoundationPanel.Frames(2);

            for (int i = 0; i < max + 2; i++)
            {
                Assert.LessOrEqual(Layer(UiLayer.Dialogs).Query<VisualElement>(UiNames.DialogScrim).ToList().Count, 1, "never two dialogs on screen");
                shell.Context.Dialogs.HandleBack();
            }
            yield return FoundationPanel.Frames(2);

            Assert.AreEqual(max, shown);
            Assert.IsFalse(shell.Context.Dialogs.IsModalOpen);
            Assert.AreEqual(0, Layer(UiLayer.Dialogs).Query<VisualElement>(UiNames.DialogScrim).ToList().Count);
            Assert.AreEqual(0, shell.Context.Input.Blocks.Count);
        }

        [UnityTest]
        public IEnumerator Dialogs_ClosedByEveryReasonInsideCallbacks_NeverLeaveABlock()
        {
            yield return CreatePanel();
            UiShell shell = CreateShell();
            shell.Boot("Home");
            IDialogService dialogs = shell.Context.Dialogs;
            DialogHandle second = null;
            DialogHandle third = null;
            DialogHandle first = dialogs.Show(new DialogRequest
            {
                Title = UiText.FromLiteral("first"),
                Callback = outcome =>
                {
                    // The callback cancels a waiting dialog, closes everything and shows a new one.
                    dialogs.Cancel(second);
                    dialogs.CloseAll();
                    dialogs.Show(new DialogRequest { Title = UiText.FromLiteral("after") });
                }
            });
            second = dialogs.Show(new DialogRequest { Title = UiText.FromLiteral("second") });
            third = dialogs.Show(new DialogRequest { Title = UiText.FromLiteral("third") });
            yield return FoundationPanel.Frames(2);

            dialogs.Cancel(first);
            yield return FoundationPanel.Frames(2);

            Assert.AreEqual(DialogHandleState.Closed, second.State);
            Assert.AreEqual(DialogHandleState.Closed, third.State);
            Assert.IsTrue(dialogs.IsModalOpen, "the dialog shown from the callback is open");
            Assert.AreEqual(1, Layer(UiLayer.Dialogs).Query<VisualElement>(UiNames.DialogScrim).ToList().Count);
            dialogs.CloseAll();
            Assert.AreEqual(0, shell.Context.Input.Blocks.Count);
        }

        [UnityTest]
        public IEnumerator Dialogs_ShownFromACallbackDuringShellDispose_DoNotOutliveTheShell()
        {
            yield return CreatePanel();
            UiShell shell = CreateShell();
            shell.Boot("Home");
            IDialogService dialogs = shell.Context.Dialogs;
            dialogs.Show(new DialogRequest
            {
                Title = UiText.FromLiteral("session"),
                Callback = outcome => dialogs.Show(new DialogRequest { Title = UiText.FromLiteral("goodbye") })
            });
            yield return FoundationPanel.Frames(2);

            shell.Dispose();

            Assert.IsFalse(dialogs.IsModalOpen, "a disposed shell has no modal dialog");
            Assert.AreEqual(0, shell.Context.Input.Blocks.Count);
        }

        [UnityTest]
        public IEnumerator Dialogs_SameContentElementInTwoDialogs_MovesWithoutThrowing()
        {
            yield return CreatePanel();
            UiShell shell = CreateShell();
            shell.Boot("Home");
            VisualElement content = new Label("shared");
            IDialogService dialogs = shell.Context.Dialogs;
            dialogs.Show(new DialogRequest { Content = content, Title = UiText.FromLiteral("a") });
            dialogs.Show(new DialogRequest { Content = content, Title = UiText.FromLiteral("b") });
            yield return FoundationPanel.Frames(2);

            dialogs.HandleBack();
            yield return FoundationPanel.Frames(2);

            Assert.IsNotNull(content.panel, "the content is in the second dialog, which is on screen");
            dialogs.CloseAll();
            Assert.AreEqual(0, Layer(UiLayer.Dialogs).Query<VisualElement>(UiNames.DialogScrim).ToList().Count);
        }

        [UnityTest]
        public IEnumerator Dialogs_NullAndEmptyArguments_AreRefusedOrTolerated()
        {
            yield return CreatePanel();
            UiShell shell = CreateShell();
            shell.Boot("Home");
            IDialogService dialogs = shell.Context.Dialogs;

            Assert.Throws<ArgumentNullException>(() => dialogs.Show((DialogRequest)null));
            Assert.Throws<ArgumentNullException>(() => ((DialogService)dialogs).Show((DialogTemplate)null));
            Assert.IsFalse(dialogs.Cancel(null));
            dialogs.Confirm(null, null, null);
            dialogs.Alert(null, null);
            yield return FoundationPanel.Frames(2);
            dialogs.HandleBack();
            dialogs.HandleBack();
            dialogs.CloseAll();
            dialogs.CloseAll();
            dialogs.OnAppResumed();

            Assert.IsFalse(dialogs.IsModalOpen);
            Assert.AreEqual(0, shell.Context.Input.Blocks.Count);
        }

        [UnityTest]
        public IEnumerator Dialogs_ADialogWithNoButtonsThatCannotBeDismissed_IsClosedByCloseAllAndByShellDispose()
        {
            yield return CreatePanel();
            UiShell shell = CreateShell();
            shell.Boot("Home");
            IDialogService dialogs = shell.Context.Dialogs;
            int closed = 0;
            dialogs.Show(new DialogRequest { DismissOnBack = false, Callback = o => closed++ });
            dialogs.Show(new DialogRequest { DismissOnBack = false, Callback = o => closed++ });
            yield return FoundationPanel.Frames(2);

            Assert.IsTrue(dialogs.HandleBack(), "back is consumed");
            Assert.IsTrue(dialogs.IsModalOpen);
            shell.Dispose();

            Assert.AreEqual(2, closed, "every callback ran exactly once");
            Assert.IsFalse(dialogs.IsModalOpen);
        }

        [UnityTest]
        public IEnumerator Toasts_HostileArguments_NeverThrowAndNeverGetStuck()
        {
            yield return CreatePanel();
            UiShell shell = CreateShell();
            shell.Boot("Home");
            IToastService toasts = shell.Context.Toasts;

            toasts.ShowRaw(null);
            toasts.ShowRaw("");
            toasts.Show(null);
            toasts.ShowRaw("nan", ToastKind.Info, float.NaN);
            toasts.ShowRaw("negative", ToastKind.Warning, -5f);
            toasts.ShowRaw("zero", ToastKind.Danger, 0f);
            toasts.ShowRaw("kind", (ToastKind)99, 0.2f);
            for (int i = 0; i < 500; i++)
                toasts.ShowRaw("burst " + (i % 7), ToastKind.Success, 0.2f);
            // One visible toast and three waiting ones, each 0.2 s plus the fade: about two seconds at most.
            float end = Time.realtimeSinceStartup + 6f;
            while (Time.realtimeSinceStartup < end && Layer(UiLayer.Toasts).Query<VisualElement>(UiNames.ToastRoot).ToList().Count > 0)
                yield return null;

            Assert.AreEqual(0, Layer(UiLayer.Toasts).Query<VisualElement>(UiNames.ToastRoot).ToList().Count, "every toast expired");
            toasts.ShowRaw("still works", ToastKind.Info, 0.2f);
            yield return Real(0.1f);
            Assert.AreEqual(1, Layer(UiLayer.Toasts).Query<VisualElement>(UiNames.ToastRoot).ToList().Count);
        }

        [UnityTest]
        public IEnumerator Toasts_UsedAfterShellDispose_DoNotThrow()
        {
            yield return CreatePanel();
            UiShell shell = CreateShell();
            shell.Boot("Home");
            IToastService toasts = shell.Context.Toasts;
            shell.Dispose();

            toasts.ShowRaw("late");
            toasts.Clear();
            yield return Real(0.3f);
            Assert.AreEqual(0, Layer(UiLayer.Toasts).Query<VisualElement>(UiNames.ToastRoot).ToList().Count);
        }

        [UnityTest]
        public IEnumerator Loading_TokenOfAnotherOverlay_DoesNotEndThisOverlaysToken()
        {
            yield return CreatePanel();
            UiShell shell = CreateShell();
            shell.Boot("Home");
            ILoadingOverlay mine = shell.Context.Loading;
            VisualElement host = new VisualElement();
            Panel.Root.Add(host);
            LoadingOverlay other = new LoadingOverlay(host, null, new InputBlockCounter());
            LoadingToken foreign = other.Begin(UiText.FromLiteral("other"));

            LoadingToken own = mine.Begin(UiText.FromLiteral("mine"));
            Assert.AreEqual(foreign.Id, own.Id, "ids are per overlay: this is what makes a mix-up dangerous");

            mine.End(foreign);
            mine.SetProgress(foreign, 0.5f);

            Assert.IsTrue(mine.BlocksInput, "my token is still alive");
            own.Dispose();
            Assert.IsFalse(mine.BlocksInput);
            other.Dispose();
        }

        [UnityTest]
        public IEnumerator Loading_EndTwiceAndUseAfterDispose_IsHarmless()
        {
            yield return CreatePanel();
            UiShell shell = CreateShell();
            shell.Boot("Home");
            ILoadingOverlay loading = shell.Context.Loading;

            LoadingToken token = loading.Begin();
            token.Dispose();
            token.Dispose();
            loading.End(default(LoadingToken));
            loading.Update(default(LoadingToken), null, true);
            loading.SetProgress(token, float.NaN);

            Assert.IsFalse(loading.BlocksInput);
            LoadingToken late = loading.Begin(null, false);
            shell.Dispose();
            late.Dispose();
            Assert.AreEqual(0, shell.Context.Input.Blocks.Count);
        }

        [UnityTest]
        public IEnumerator Shell_DisposedWhileLoadingAndDialogAndToastAreActive_ReleasesEverything()
        {
            yield return CreatePanel();
            UiShell shell = CreateShell(new UiShellOptions { TransitionsEnabled = true });
            shell.Boot("Home");
            shell.Router.Push("Settings");
            LoadingToken token = shell.Context.Loading.Begin(UiText.FromLiteral("x"));
            shell.Context.Dialogs.Alert(UiText.FromLiteral("t"), UiText.FromLiteral("m"));
            shell.Context.Toasts.ShowRaw("hello");
            yield return Real(0.4f);

            shell.Dispose();
            shell.Dispose();
            yield return Real(0.5f);

            Assert.AreEqual(0, shell.Context.Input.Blocks.Count);
            Assert.IsNull(shell.Root.parent);
            Assert.Throws<ObjectDisposedException>(() => shell.Boot("Home"));
            Assert.IsFalse(shell.RequestBack());
            Assert.IsFalse(shell.HandleBackPress());
            shell.Tick();
            shell.NotifyApplicationFocus(true);
            shell.NotifyApplicationPause(true);
            token.Dispose();
        }

        [UnityTest]
        public IEnumerator Shell_BootWithUnknownOrNullStartScreen_StartsEmptyAndStaysUsable()
        {
            yield return CreatePanel();
            LogAssert.ignoreFailingMessages = true;
            UiShell shell = CreateShell();

            shell.Boot(null);
            Assert.IsNull(shell.Router.Current);
            shell.Boot("Home");
            Assert.IsNull(shell.Router.Current, "a second Boot does nothing");
            shell.Router.Push("Home");
            Assert.AreEqual("Home", shell.Router.Current);
            yield return null;
        }

        [UnityTest]
        public IEnumerator Shell_TwoShellsOnTwoPanels_AreIndependent()
        {
            yield return CreatePanel();
            FoundationPanel second = FoundationPanel.Create(400, 300);
            yield return FoundationPanel.Frames(2);
            UiShell a = CreateShell();
            a.Boot("Home");
            UiShell b = new UiShell(second.Root, new UiShellOptions { TransitionsEnabled = false, UseDefaultCatalog = false, StateStore = Store, NavigationStateKey = "b" });
            b.Catalog.Register("Home", () => new ProbeScreen("HomeB", Log));
            b.Boot("Home");

            a.Context.Dialogs.Alert(UiText.FromLiteral("a"), UiText.Empty);
            Assert.IsTrue(a.Context.Input.IsBlocked);
            Assert.IsFalse(b.Context.Input.IsBlocked, "a dialog of one shell does not block the other");
            Assert.IsFalse(b.Context.Dialogs.IsModalOpen);

            b.Dispose();
            second.Dispose();
            yield return null;
            Assert.IsTrue(a.Context.Dialogs.IsModalOpen);
        }

        [UnityTest]
        public IEnumerator Shell_TwoShellsOnTheSameHost_DoNotShareFocusTrapsOrBlocks()
        {
            yield return CreatePanel();
            UiShell a = CreateShell();
            a.Boot("Home");
            UiShell b = new UiShell(Panel.Root, new UiShellOptions { TransitionsEnabled = false, UseDefaultCatalog = false, RestoreNavigation = false });
            b.Catalog.Register("Home", () => new ProbeScreen("HomeB", Log));
            b.Boot("Home");

            a.Context.Dialogs.Alert(UiText.FromLiteral("a"), UiText.Empty);
            b.Context.Dialogs.Alert(UiText.FromLiteral("b"), UiText.Empty);
            yield return FoundationPanel.Frames(2);
            a.Dispose();
            yield return FoundationPanel.Frames(2);

            Assert.IsTrue(b.Context.Dialogs.IsModalOpen);
            b.Context.Dialogs.CloseAll();
            b.Dispose();
            Assert.AreEqual(0, b.Context.Input.Blocks.Count);
        }

        [UnityTest]
        public IEnumerator SafeAreaProvider_NonsenseFromTheDevice_NeverProducesNaNOrNegativeInsets()
        {
            yield return CreatePanel();
            VisualElement root = Panel.Root;
            Rect[] rects =
            {
                new Rect(0, 0, 0, 0),
                new Rect(-50, -50, 5000, 5000),
                new Rect(float.NaN, float.NaN, float.NaN, float.NaN),
                new Rect(10, 10, float.PositiveInfinity, 20),
                new Rect(900, 900, 50, 50),
            };
            Vector2Int[] screens = { new Vector2Int(0, 0), new Vector2Int(-5, 100), new Vector2Int(1080, 1920), new Vector2Int(1, 1) };
            foreach (Rect rect in rects)
            {
                foreach (Vector2Int screen in screens)
                {
                    SafeAreaProvider provider = new SafeAreaProvider { SafeAreaSource = () => rect, ScreenSizeSource = () => screen, Mode = SafeAreaMode.Always };
                    provider.Poll(root);
                    string label = rect + " on " + screen;
                    foreach (float v in new[] { provider.Insets.Left, provider.Insets.Top, provider.Insets.Right, provider.Insets.Bottom })
                    {
                        Assert.IsFalse(float.IsNaN(v), "NaN inset for " + label);
                        Assert.IsFalse(float.IsInfinity(v), "infinite inset for " + label);
                        Assert.GreaterOrEqual(v, 0f, "negative inset for " + label);
                    }
                }
            }
        }

        [Test]
        public void TextSource_ALanguageSwitchedWhileLabelsAreBoundAndRemoved_StaysConsistent()
        {
            string language = "en";
            UiTextSource text = new UiTextSource(t => language + ":" + t.Key);
            VisualElement host = new VisualElement();
            Label[] labels = new Label[20];
            for (int i = 0; i < labels.Length; i++)
            {
                labels[i] = new Label();
                host.Add(labels[i]);
                text.Bind(labels[i], UiText.FromKey("play"));
            }

            for (int round = 0; round < 10; round++)
            {
                language = round % 2 == 0 ? "de" : "en";
                text.NotifyChanged();
            }

            Assert.AreEqual("en:play", labels[15].text);
        }

        [Test]
        public void TextSource_BindingNullsAndAThrowingResolver_NeverThrow()
        {
            LogAssert.ignoreFailingMessages = true;
            UiTextSource text = new UiTextSource(t => throw new InvalidOperationException("resolver"));
            Label label = new Label();
            Assert.DoesNotThrow(() => text.Bind((TextElement)null, UiText.FromLiteral("x")));
            Assert.DoesNotThrow(() => text.Bind(label, null));
            Assert.DoesNotThrow(() => text.Bind(label, UiText.FromKey("k").WithFallback("fallback")));
            Assert.AreEqual("fallback", label.text);
            Assert.DoesNotThrow(() => text.Bind(label, UiText.FromKey("k"), null));
            Assert.AreEqual(string.Empty, text.Resolve(null));
        }

        [UnityTest]
        public IEnumerator Shell_RotatedMidTransition_UpdatesTheProfileAndFinishesTheTransition()
        {
            yield return CreatePanel();
            UiShell shell = CreateShell(new UiShellOptions { TransitionsEnabled = true });
            shell.Boot("Home");
            yield return FoundationPanel.Frames(2);
            Assert.AreEqual(UiOrientation.Landscape, shell.Context.Profile.Profile.Orientation);
            Rect[] safe = { new Rect(0, 0, 800, 600) };
            shell.Context.SafeArea.Mode = SafeAreaMode.Always;   // the test panel renders into a texture, which Auto does not pad
            shell.Context.SafeArea.SafeAreaSource = () => safe[0];
            shell.Context.SafeArea.ScreenSizeSource = () => new Vector2Int(Mathf.RoundToInt(Panel.Root.layout.width), Mathf.RoundToInt(Panel.Root.layout.height));

            shell.Router.Push("Settings");
            yield return null;
            Panel.Root.style.width = 600;
            Panel.Root.style.height = 800;
            safe[0] = new Rect(0, 34, 600, 732);
            shell.Router.Push("About");
            yield return null;
            shell.Router.Pop();
            yield return Real(0.7f);
            yield return FoundationPanel.Frames(3);

            Assert.AreEqual(UiOrientation.Portrait, shell.Context.Profile.Profile.Orientation);
            Assert.IsTrue(shell.Root.ClassListContains(UiNames.ClassPortrait));
            Assert.IsFalse(shell.Root.ClassListContains(UiNames.ClassLandscape));
            Assert.AreEqual("Settings", shell.Router.Current);
            Assert.AreEqual(shell.Router.Depth, Outlet.childCount);
            Assert.AreEqual(1, DisplayedScreens());
            Assert.AreEqual(0, shell.Context.Input.Blocks.Count);
            Assert.Greater(shell.Context.SafeArea.Insets.Bottom + shell.Context.SafeArea.Insets.Top, 0f, "the new safe area was picked up");
        }
    }
}
