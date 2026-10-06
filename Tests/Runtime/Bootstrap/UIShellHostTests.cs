using System.Collections;
using System.Collections.Generic;
using FutureDigitalPlus.UIKit.Foundation;
using FutureDigitalPlus.UIKit.Interaction;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UIElements;

namespace FutureDigitalPlus.UIKit.Bootstrap.Tests
{
    public class InputProfileModeSourceTests
    {
        [Test]
        public void Map_RenamesEveryDeviceKind()
        {
            Assert.AreEqual(UiInputMode.Pointer, InputProfileModeSource.Map(InputDeviceKind.Pointer));
            Assert.AreEqual(UiInputMode.Touch, InputProfileModeSource.Map(InputDeviceKind.Touch));
            Assert.AreEqual(UiInputMode.Keyboard, InputProfileModeSource.Map(InputDeviceKind.Keyboard));
            Assert.AreEqual(UiInputMode.Gamepad, InputProfileModeSource.Map(InputDeviceKind.Gamepad));
        }

        [Test]
        public void NullProfile_ReportsPointerAndNeverRaises()
        {
            var source = new InputProfileModeSource(null);
            source.ModeChanged += _ => Assert.Fail("nothing can change");
            Assert.AreEqual(UiInputMode.Pointer, source.Mode);
            source.Detach();
        }

        [Test]
        public void FollowsTheProfile_UntilDetached()
        {
            var profile = InputProfile.Bind(new VisualElement());
            var source = new InputProfileModeSource(profile);
            var seen = new List<UiInputMode>();
            source.ModeChanged += seen.Add;

            profile.Report(InputDeviceKind.Gamepad);
            Assert.AreEqual(UiInputMode.Gamepad, source.Mode);
            CollectionAssert.AreEqual(new[] { UiInputMode.Gamepad }, seen);

            source.Detach();
            profile.Report(InputDeviceKind.Touch);
            Assert.AreEqual(1, seen.Count, "a detached source stays silent");
            profile.Dispose();
        }
    }

    public class UIShellHostTests
    {
        private sealed class Page : UiScreen
        {
            public override string UxmlName => null;

            protected override void OnBuild() => Root.Add(new Label("page"));
        }

        private GameObject _go;
        private PanelSettings _settings;
        private RenderTexture _target;

        [TearDown]
        public void TearDown()
        {
            LogAssert.ignoreFailingMessages = false;
            if (_go != null) Object.Destroy(_go);
            if (_settings != null) Object.Destroy(_settings);
            if (_target != null)
            {
                _target.Release();
                Object.Destroy(_target);
            }

            ScreenCatalog.Default.Clear();
            InputFeed.Reset();
        }

        private UIShellHost Create(System.Action<UIShellHost> beforeStart = null)
        {
            if (!Application.isPlaying) Assert.Ignore("Needs a runtime panel (Play Mode).");
            LogAssert.ignoreFailingMessages = true; // a PanelSettings without a runtime theme logs
            _target = new RenderTexture(640, 480, 0);
            _settings = ScriptableObject.CreateInstance<PanelSettings>();
            _settings.scaleMode = PanelScaleMode.ConstantPixelSize;
            _settings.targetTexture = _target;

            _go = new GameObject("UIShellHostTest");
            _go.SetActive(false);
            var document = _go.AddComponent<UIDocument>();
            document.panelSettings = _settings;
            var host = _go.AddComponent<UIShellHost>();
            host.ConfigureShell = o => { o.RestoreNavigation = false; o.TransitionsEnabled = false; };
            beforeStart?.Invoke(host);
            _go.SetActive(true);
            return host;
        }

        [UnityTest]
        public IEnumerator BuildsTheKit_ThenRaisesReady_AndTheGameBootsItsScreen()
        {
            UIShellHost ready = null;
            var host = Create(h => h.Ready += x => { ready = x; x.Shell.Catalog.Register("Home", () => new Page()); x.Shell.Boot("Home"); });
            Assert.IsFalse(host.IsReady, "nothing is built before Start");
            yield return null;
            yield return null;

            Assert.AreSame(host, ready);
            Assert.IsTrue(host.IsReady);
            Assert.IsNotNull(host.Root.Q(className: "fdp-shell"), "the shell is under the document root");
            Assert.AreEqual("dark", host.Themes.CurrentName);
            Assert.IsNotNull(host.Profile);
            Assert.IsNotNull(host.Accessibility);
            Assert.AreEqual("Home", host.Shell.Router.Current);
        }

        [UnityTest]
        public IEnumerator ABackPress_PopsTheTopScreen()
        {
            var host = Create(h => h.Ready += x =>
            {
                x.Shell.Catalog.Register("Home", () => new Page());
                x.Shell.Catalog.Register("Next", () => new Page());
                x.Shell.Boot("Home");
                x.Shell.Router.Push("Next");
            });
            yield return null;
            yield return null;
            Assert.AreEqual("Next", host.Shell.Router.Current);

            InputFeed.ReportBack();
            Assert.AreEqual("Home", host.Shell.Router.Current, "the back signal reaches the shell");
        }

        [UnityTest]
        public IEnumerator ABrandThemeRegisteredBeforeStart_IsNotReplaced()
        {
            var brand = Resources.Load<FutureDigitalPlus.UIKit.DesignSystem.Theme.Data.ThemeData>(FutureDigitalPlus.UIKit.DesignSystem.Theme.Data.ThemeData.LightResource);
            Assert.IsNotNull(brand);
            var host = Create(h => { h.Themes.Register("dark", brand); });
            yield return null;
            yield return null;
            Assert.AreSame(brand, host.Themes.Current, "the game's own \"dark\" theme wins over the built-in one");
        }

        [UnityTest]
        public IEnumerator DisablingTearsEverythingDown_AndEnablingBuildsItAgain()
        {
            var raised = 0;
            var host = Create(h => h.Ready += _ => raised++);
            yield return null;
            yield return null;
            var shell = host.Shell;
            Assert.IsNotNull(shell);

            _go.SetActive(false);
            Assert.IsFalse(host.IsReady);
            Assert.IsTrue(shell.IsDisposed, "the shell is disposed with the host");
            Assert.IsNull(host.Root);

            _go.SetActive(true);
            yield return null;
            yield return null;
            Assert.IsTrue(host.IsReady);
            Assert.AreNotSame(shell, host.Shell);
            Assert.AreEqual(2, raised);
        }
    }
}
