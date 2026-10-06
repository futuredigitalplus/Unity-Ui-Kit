using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UIElements;

namespace FutureDigitalPlus.UIKit.Foundation.Tests
{
    /// <summary>
    /// EditMode tests of the shell and the router on a detached element: no panel, no layout, no timers. They cover the order of lifecycle calls, the stack
    /// rules as the router executes them, state restore, back handling and teardown. Timing and layout are covered by the PlayMode tests.
    /// </summary>
    public class ShellLogicTests
    {
        List<string> _log;
        MemoryStateStore _store;
        UiShell _shell;

        [SetUp]
        public void SetUp()
        {
            _log = new List<string>();
            _store = new MemoryStateStore();
        }

        [TearDown]
        public void TearDown()
        {
            if (_shell != null)
                _shell.Dispose();
            _shell = null;
            ScreenCatalog.Default.Clear();
            TestEnvironment.ResetStatics();
        }

        UiShell NewShell(UiShellOptions options = null)
        {
            UiShellOptions settings = options ?? new UiShellOptions();
            settings.TransitionsEnabled = false;
            if (settings.StateStore == null)
                settings.StateStore = _store;
            UiShell shell = new UiShell(new VisualElement(), settings);
            shell.Catalog.Register("Home", () => new ProbeScreen("Home", _log));
            shell.Catalog.Register("Settings", () => new ProbeScreen("Settings", _log));
            shell.Catalog.Register("About", () => new ProbeScreen("About", _log));
            return shell;
        }

        [Test]
        public void Boot_ShowsTheInitialScreen()
        {
            _shell = NewShell();
            _shell.Boot("Home");

            Assert.AreEqual("Home", _shell.Router.Current);
            CollectionAssert.AreEqual(new[] { "Home:build", "Home:show:null" }, _log);
            Assert.IsTrue(_shell.IsBooted);
        }

        [Test]
        public void Boot_IsIdempotent()
        {
            _shell = NewShell();
            _shell.Boot("Home");
            _shell.Boot("Settings");
            Assert.AreEqual("Home", _shell.Router.Current);
        }

        [Test]
        public void Boot_RaisesBootedBeforeTheFirstScreen_SoMoreScreensCanBeRegistered()
        {
            _shell = NewShell();
            _shell.Booted += ui => _shell.Catalog.Register("Late", () => new ProbeScreen("Late", _log));
            _shell.Boot("Late");
            Assert.AreEqual("Late", _shell.Router.Current);
        }

        [Test]
        public void Boot_WithAnUnregisteredStartScreen_WarnsAndStartsEmpty()
        {
            _shell = NewShell();
            LogAssert.Expect(LogType.Warning, new Regex("start screen 'Nope' is not registered"));
            _shell.Boot("Nope");
            Assert.IsNull(_shell.Router.Current);
        }

        [Test]
        public void Boot_AlsoTakesTheDefaultCatalog()
        {
            ScreenCatalog.Default.Register("FromDefault", () => new ProbeScreen("FromDefault", _log));
            _shell = NewShell();
            _shell.Boot("FromDefault");
            Assert.AreEqual("FromDefault", _shell.Router.Current);
        }

        [Test]
        public void Boot_IgnoresTheDefaultCatalog_WhenTheOptionIsOff()
        {
            ScreenCatalog.Default.Register("FromDefault", () => new ProbeScreen("FromDefault", _log));
            _shell = NewShell(new UiShellOptions { UseDefaultCatalog = false });
            LogAssert.Expect(LogType.Warning, new Regex("not registered"));
            _shell.Boot("FromDefault");
            Assert.IsNull(_shell.Router.Current);
        }

        [Test]
        public void Push_KeepsTheScreenBelowAliveAndHidden_AndPopDisposesTheTop()
        {
            _shell = NewShell();
            _shell.Boot("Home");
            _log.Clear();

            _shell.Router.Push("Settings", "arg");
            CollectionAssert.AreEqual(new[] { "Settings:build", "Home:hide", "Settings:show:arg" }, _log);
            ScreenRouter router = _shell.Router;
            Assert.AreEqual(2, router.Depth);
            Assert.AreEqual(DisplayStyle.None, router.ScreenAt(0).Root.style.display.value);
            Assert.AreEqual(DisplayStyle.Flex, router.ScreenAt(1).Root.style.display.value);

            _log.Clear();
            Assert.IsTrue(router.Pop());
            CollectionAssert.AreEqual(new[] { "Settings:hide", "Settings:dispose", "Home:show:null" }, _log);
            Assert.AreEqual(DisplayStyle.Flex, router.ScreenAt(0).Root.style.display.value);
            Assert.AreEqual(1, router.Depth);
        }

        [Test]
        public void Push_OfTheScreenOnTop_DoesNothing_SoADoubleTapCannotStackTwoCopies()
        {
            _shell = NewShell();
            _shell.Boot("Home");
            _shell.Router.Push("Settings");
            _shell.Router.Push("Settings");
            Assert.AreEqual(2, _shell.Router.Depth);
            Assert.AreEqual(1, _log.FindAll(entry => entry == "Settings:build").Count);
        }

        [Test]
        public void Push_BeyondTheDepthCap_KeepsTheRootAndDropsTheOldestScreenAboveIt()
        {
            _shell = NewShell(new UiShellOptions { MaxDepth = 2 });
            _shell.Boot("Home");
            _shell.Router.Push("Settings");
            _log.Clear();
            _shell.Router.Push("About");
            Assert.AreEqual(2, _shell.Router.Depth);
            Assert.AreEqual("About", _shell.Router.Current);
            Assert.Contains("Settings:dispose", _log, "the screen trimmed by the cap is disposed, not leaked");
            Assert.IsFalse(_log.Contains("Home:dispose"), "the root stays");
        }

        [Test]
        public void Replace_DisposesTheOldScreen()
        {
            _shell = NewShell();
            _shell.Boot("Home");
            _log.Clear();
            _shell.Router.Replace("Settings");
            Assert.AreEqual("Settings", _shell.Router.Current);
            Assert.AreEqual(1, _shell.Router.Depth);
            Assert.Contains("Home:dispose", _log);
        }

        [Test]
        public void PopTo_AndResetTo_DisposeTheScreensAbove()
        {
            _shell = NewShell();
            _shell.Boot("Home");
            _shell.Router.Push("Settings");
            _shell.Router.Push("About");
            Assert.IsTrue(_shell.Router.PopTo("Home"));
            Assert.AreEqual(1, _shell.Router.Depth);
            Assert.IsFalse(_shell.Router.PopTo("Home"), "already on top");
            Assert.IsFalse(_shell.Router.PopTo("Missing"));

            _shell.Router.Push("Settings");
            _shell.Router.ResetTo("About");
            Assert.AreEqual(1, _shell.Router.Depth);
            Assert.AreEqual("About", _shell.Router.Current);
        }

        [Test]
        public void Push_OfAnUnregisteredScreen_LogsAnError_AndKeepsTheStack()
        {
            _shell = NewShell();
            _shell.Boot("Home");
            LogAssert.Expect(LogType.Error, new Regex("screen 'Ghost' is not registered"));
            _shell.Router.Push("Ghost");
            Assert.AreEqual("Home", _shell.Router.Current);
        }

        [Test]
        public void Push_WhenTheFactoryThrows_LogsTheException_AndKeepsTheStack()
        {
            _shell = NewShell();
            _shell.Catalog.Register("Broken", () => { throw new InvalidOperationException("factory boom"); });
            _shell.Boot("Home");
            LogAssert.Expect(LogType.Exception, new Regex("factory boom"));
            _shell.Router.Push("Broken");
            Assert.AreEqual("Home", _shell.Router.Current);
            Assert.AreEqual(1, _shell.Router.Depth);
        }

        [Test]
        public void Push_WhenBuildThrows_LeavesNoHalfBuiltTreeInTheOutlet()
        {
            _shell = NewShell();
            _shell.Catalog.Register("Broken", () => new ThrowingScreen());
            _shell.Boot("Home");
            LogAssert.Expect(LogType.Exception, new Regex("build boom"));
            _shell.Router.Push("Broken");
            Assert.AreEqual("Home", _shell.Router.Current);
            Assert.AreEqual(1, _shell.Context.GetLayer(UiLayer.Screens).childCount);
        }

        [Test]
        public void ShowThatThrows_IsLogged_AndNavigationContinues()
        {
            _shell = NewShell();
            _shell.Catalog.Register("Shaky", () => new ThrowingScreen(throwInShow: true));
            _shell.Boot("Home");
            LogAssert.Expect(LogType.Exception, new Regex("show boom"));
            _shell.Router.Push("Shaky");
            Assert.AreEqual("Shaky", _shell.Router.Current);
        }

        [Test]
        public void StateRoundTrip_RestoresTheStackAndEachScreensState()
        {
            _shell = NewShell();
            _shell.Boot("Home");
            ((ProbeScreen)_shell.Router.CurrentScreen).State = "tab-2";
            _shell.Router.Push("Settings");
            ((ProbeScreen)_shell.Router.CurrentScreen).State = "scroll-40";
            _shell.SaveNavigationState();
            _shell.Dispose();
            _log.Clear();

            _shell = NewShell();
            _shell.Boot("About");

            Assert.AreEqual("Settings", _shell.Router.Current);
            Assert.AreEqual(2, _shell.Router.Depth);
            CollectionAssert.Contains(_log, "Home:restore:tab-2");
            CollectionAssert.Contains(_log, "Settings:restore:scroll-40");
        }

        [Test]
        public void Boot_WithUnreadableSavedState_StartsTheInitialScreen()
        {
            _store.Save("fdp/navigation", "this is not a stack");
            _shell = NewShell();
            _shell.Boot("Home");
            Assert.AreEqual("Home", _shell.Router.Current);
        }

        [Test]
        public void Boot_WithRestoreOff_IgnoresTheSavedState()
        {
            _shell = NewShell();
            _shell.Boot("Home");
            _shell.Router.Push("Settings");
            _shell.Dispose();

            _shell = NewShell(new UiShellOptions { RestoreNavigation = false });
            _shell.Boot("Home");
            Assert.AreEqual("Home", _shell.Router.Current);
        }

        [Test]
        public void EveryNavigationChange_SavesTheStack()
        {
            _shell = NewShell();
            _shell.Boot("Home");
            int afterBoot = _store.Saves;
            _shell.Router.Push("Settings");
            Assert.Greater(_store.Saves, afterBoot);
            StringAssert.Contains("Settings", _store.Load("fdp/navigation"));
        }

        [Test]
        public void RequestBack_AsksTheScreenFirst_ThenPops_AndReportsFalseAtTheRoot()
        {
            _shell = NewShell();
            _shell.Boot("Home");
            _shell.Router.Push("Settings");
            ProbeScreen settings = (ProbeScreen)_shell.Router.CurrentScreen;

            settings.HandlesBack = true;
            Assert.IsTrue(_shell.RequestBack());
            Assert.AreEqual("Settings", _shell.Router.Current, "the screen consumed the press");

            settings.HandlesBack = false;
            Assert.IsTrue(_shell.RequestBack());
            Assert.AreEqual("Home", _shell.Router.Current, "the router popped");

            Assert.IsFalse(_shell.RequestBack(), "nothing left to consume the press: the game decides");
        }

        [Test]
        public void RequestBack_ClosesAnOpenDialogBeforeTheScreen()
        {
            _shell = NewShell();
            _shell.Boot("Home");
            _shell.Router.Push("Settings");
            bool? answer = null;
            _shell.Context.Dialogs.Confirm(UiText.FromLiteral("Sure?"), UiText.Empty, result => answer = result);

            Assert.IsTrue(_shell.RequestBack());
            Assert.AreEqual(false, answer, "back counts as cancel");
            Assert.AreEqual("Settings", _shell.Router.Current, "the same press did not also pop the screen");
        }

        [Test]
        public void RequestBack_IsSwallowedWhileLoadingBlocksInput()
        {
            _shell = NewShell();
            _shell.Boot("Home");
            _shell.Router.Push("Settings");
            using (_shell.Context.Loading.Begin(UiText.FromLiteral("Loading")))
            {
                Assert.IsTrue(_shell.RequestBack());
                Assert.AreEqual("Settings", _shell.Router.Current);
            }
            Assert.IsTrue(_shell.RequestBack());
            Assert.AreEqual("Home", _shell.Router.Current);
        }

        [Test]
        public void HandleBackPress_CountsOnePressPerFrame()
        {
            _shell = NewShell();
            _shell.Boot("Home");
            _shell.Router.Push("Settings");
            _shell.Router.Push("About");

            Assert.IsTrue(_shell.HandleBackPress());
            Assert.AreEqual("Settings", _shell.Router.Current);
            // Same frame (no yield in an EditMode test): the second report of the same physical press is a duplicate.
            Assert.IsTrue(_shell.HandleBackPress());
            Assert.AreEqual("Settings", _shell.Router.Current);
        }

        [Test]
        public void Tick_PollsTheBackKeySource()
        {
            FakeBackKeySource key = new FakeBackKeySource();
            _shell = NewShell(new UiShellOptions { BackKeySource = key });
            _shell.Boot("Home");
            _shell.Router.Push("Settings");

            _shell.Tick();
            Assert.AreEqual("Settings", _shell.Router.Current, "no key pressed");
            key.Pressed = true;
            _shell.Tick();
            Assert.AreEqual("Home", _shell.Router.Current);
        }

        [Test]
        public void Dispose_DisposesScreens_ClosesDialogs_ReleasesInput_AndDetachesTheRoot()
        {
            VisualElement host = new VisualElement();
            _shell = new UiShell(host, new UiShellOptions { TransitionsEnabled = false, StateStore = _store });
            _shell.Catalog.Register("Home", () => new ProbeScreen("Home", _log));
            _shell.Boot("Home");
            bool? answer = null;
            _shell.Context.Dialogs.Confirm(UiText.FromLiteral("Sure?"), UiText.Empty, result => answer = result);
            LoadingToken token = _shell.Context.Loading.Begin(UiText.FromLiteral("Loading"));
            Assert.IsTrue(_shell.Context.Input.IsBlocked);

            _shell.Dispose();

            Assert.IsTrue(_shell.IsDisposed);
            Assert.Contains("Home:dispose", _log);
            Assert.AreEqual(false, answer, "the open dialog's callback ran once with a cancelled result");
            Assert.IsFalse(_shell.Context.Input.IsBlocked);
            Assert.AreEqual(0, host.childCount);
            Assert.IsFalse(_shell.RequestBack());
            token.Dispose();
            _shell.Dispose();
        }

        [Test]
        public void TwoShells_DoNotShareRoutersCatalogsInputGatesOrSafeAreaProviders()
        {
            UiShell first = new UiShell(new VisualElement(), new UiShellOptions { TransitionsEnabled = false });
            UiShell second = new UiShell(new VisualElement(), new UiShellOptions { TransitionsEnabled = false });
            try
            {
                first.Catalog.Register("A", () => new ProbeScreen("A", _log));
                second.Catalog.Register("B", () => new ProbeScreen("B", _log));
                first.Boot("A");
                second.Boot("B");
                Assert.AreEqual("A", first.Router.Current);
                Assert.AreEqual("B", second.Router.Current);
                Assert.IsFalse(first.Router.IsRegistered("B"));

                first.Context.Loading.Begin(UiText.Empty);
                Assert.IsTrue(first.Context.Input.IsBlocked);
                Assert.IsFalse(second.Context.Input.IsBlocked);
                Assert.AreNotSame(first.Context.SafeArea, second.Context.SafeArea);
                Assert.AreSame(first.Context.SafeArea, SafeAreaProvider.For(first.Context.GetLayer(UiLayer.Dialogs)));
                Assert.AreSame(second.Context.SafeArea, SafeAreaProvider.For(second.Context.GetLayer(UiLayer.Dialogs)));
            }
            finally
            {
                first.Dispose();
                second.Dispose();
            }
        }

        [Test]
        public void Shell_BuildsTheSixLayersInOrder_AndTheRootClasses()
        {
            _shell = NewShell();
            VisualElement root = _shell.Root;
            string[] expected = { UiNames.LayerScreens, UiNames.LayerHud, UiNames.LayerOverlays, UiNames.LayerDialogs, UiNames.LayerToasts, UiNames.LayerLoading };
            Assert.AreEqual(expected.Length, root.childCount);
            for (int i = 0; i < expected.Length; i++)
            {
                Assert.AreEqual(expected[i], root[i].name);
                Assert.IsTrue(root[i].ClassListContains(UiNames.ClassLayer));
                Assert.AreEqual(PickingMode.Ignore, root[i].pickingMode, "layers never take input themselves");
                Assert.AreSame(root[i], _shell.Context.GetLayer((UiLayer)i));
            }
            Assert.IsTrue(root.ClassListContains(UiNames.ClassShell));
            Assert.AreEqual(PickingMode.Ignore, root.pickingMode);
        }

        [Test]
        public void Shell_InstallsItsStyleSheets_AndCanLeaveThemOut()
        {
            _shell = NewShell();
            Assert.AreEqual(2, _shell.Root.styleSheets.count);
            UiShell bare = new UiShell(new VisualElement(), new UiShellOptions { InstallStyles = false });
            try
            {
                Assert.AreEqual(0, bare.Root.styleSheets.count);
            }
            finally
            {
                bare.Dispose();
            }
        }

        [Test]
        public void ScreenDefinition_BuildsAScreenFromDirectReferences()
        {
            VisualTreeAsset layout = ScriptableObject.CreateInstance<VisualTreeAsset>();
            ScreenDefinition definition = ScreenDefinition.Create("Credits", layout);
            try
            {
                _shell = NewShell();
                _shell.Catalog.Register(definition);
                _shell.Boot("Credits");
                Assert.AreEqual("Credits", _shell.Router.Current);
                Assert.IsTrue(_shell.Router.CurrentScreen.Root.ClassListContains("fdp-screen--credits"));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(definition);
                UnityEngine.Object.DestroyImmediate(layout);
            }
        }

        [Test]
        public void AScreenWithoutUxmlOrDefinition_BuildsInCode_WithoutAnError()
        {
            _shell = NewShell();
            _shell.Boot("Home");
            Assert.AreEqual(1, _shell.Router.CurrentScreen.Root.childCount, "just the probe's button, no error label");
        }

        [Test]
        public void AScreenWithAMissingUxml_ShowsAnErrorLabel_InsteadOfThrowing()
        {
            _shell = NewShell();
            _shell.Catalog.Register("NeedsUxml", () => new UxmlScreen("DoesNotExist"));
            _shell.Boot("Home");
            LogAssert.Expect(LogType.Error, new Regex("no UXML for screen 'DoesNotExist'"));
            _shell.Router.Push("NeedsUxml");
            Assert.AreEqual("NeedsUxml", _shell.Router.Current);
            Assert.IsNotNull(_shell.Router.CurrentScreen.Root.Q<Label>(className: "fdp-screen__error"));
        }

        [Test]
        public void UiScreen_ListenersAndCleanups_AreRemovedOnDispose()
        {
            _shell = NewShell();
            int cleaned = 0;
            _shell.Catalog.Register("Cleaner", () => new CleanupScreen(() => cleaned++));
            _shell.Boot("Cleaner");
            _shell.Router.Replace("Home");
            Assert.AreEqual(1, cleaned);
        }

        sealed class ThrowingScreen : UiScreen
        {
            readonly bool _throwInShow;

            public ThrowingScreen(bool throwInShow = false)
            {
                _throwInShow = throwInShow;
            }

            protected override void OnBuild()
            {
                if (!_throwInShow)
                    throw new InvalidOperationException("build boom");
            }

            protected override void OnShow(object args)
            {
                if (_throwInShow)
                    throw new InvalidOperationException("show boom");
            }
        }

        sealed class UxmlScreen : UiScreen
        {
            readonly string _name;

            public UxmlScreen(string name)
            {
                _name = name;
            }

            public override string UxmlName
            {
                get { return _name; }
            }
        }

        sealed class CleanupScreen : UiScreen
        {
            readonly Action _cleanup;

            public CleanupScreen(Action cleanup)
            {
                _cleanup = cleanup;
            }

            protected override void OnBuild()
            {
                OnDisposeDo(_cleanup);
            }
        }
    }
}
