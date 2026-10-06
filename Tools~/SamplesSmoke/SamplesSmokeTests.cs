using System;
using System.Collections;
using FutureDigitalPlus.UIKit.Controls;
using FutureDigitalPlus.UIKit.Components.Samples.Counter;
using FutureDigitalPlus.UIKit.Components.Samples.EventInterfaces;
using FutureDigitalPlus.UIKit.Components.Samples.Query;
using FutureDigitalPlus.UIKit.Components.Samples.Resources;
using FutureDigitalPlus.UIKit.Components.Samples.UxmlTraits;
using FutureDigitalPlus.UIKit.Foundation;
using FutureDigitalPlus.UIKit.Gameplay;
using FutureDigitalPlus.UIKit.Hud;
using FutureDigitalPlus.UIKit.Language;
using FutureDigitalPlus.UIKit.Samples;
using FutureDigitalPlus.UIKit.Samples.ActionHud;
using FutureDigitalPlus.UIKit.Samples.AnimationModule;
using FutureDigitalPlus.UIKit.Samples.GamepadMenu;
using FutureDigitalPlus.UIKit.Samples.Responsive;
using FutureDigitalPlus.UIKit.Samples.RpgInventory;
using FutureDigitalPlus.UIKit.Samples.ShellQuickStart;
using FutureDigitalPlus.UIKit.Samples.ThemingLanguage;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UIElements;

namespace FutureDigitalPlus.UIKit.Samples.Smoke.Tests
{
    /// <summary>
    /// Every sample, put on an empty GameObject the way its README says: it must build its UI without an error or exception (the test framework fails a test on
    /// any logged error), survive a few dozen frames of its own <c>Update</c>, show the widgets it advertises, and leave no UIDocument behind when destroyed.
    /// The test does not click anything; what a sample does on input is not covered here. Run it with the samples imported next to it (see README.md).
    /// </summary>
    public class SamplesSmokeTests
    {
        private GameObject _host;

        [TearDown]
        public void TearDown()
        {
            if (_host != null)
                UnityEngine.Object.Destroy(_host);
            _host = null;
        }

        [UnityTest]
        public IEnumerator ShellQuickStart_ShowsTheHomePage()
        {
            yield return Run<ShellQuickStartSample>(root =>
            {
                Assert.IsNotNull(root.Q(className: "fdp-shell"), "the shell root");
                Assert.IsNotNull(root.Q<TabBar>(), "the tab bar of the home page");
                Assert.GreaterOrEqual(root.Query<Button>().ToList().Count, 4, "settings plus the three actions");
            });
        }

        [UnityTest]
        public IEnumerator RpgInventory_ShowsTwoFullGrids()
        {
            yield return Run<RpgInventorySample>(root =>
            {
                var grids = root.Query<SlotGrid>().ToList();
                Assert.AreEqual(2, grids.Count);
                foreach (var grid in grids)
                    Assert.AreEqual(20, grid.Slots.Count);
            });
        }

        [UnityTest]
        public IEnumerator ActionHud_ShowsTheWidgets()
        {
            yield return Run<ActionHudSample>(root =>
            {
                Assert.GreaterOrEqual(root.Query<MeterBar>().ToList().Count, 3, "health, stamina, enemy");
                Assert.AreEqual(3, root.Query<CooldownIcon>().ToList().Count);
                Assert.IsNotNull(root.Q<WorldAnchorLayer>());
                Assert.IsNotNull(root.Q<FloatingTextLayer>());
            });
        }

        [UnityTest]
        public IEnumerator GamepadMenu_ShowsTheMainMenu()
        {
            yield return Run<GamepadMenuSample>(root =>
            {
                Assert.GreaterOrEqual(root.Query<ListRow>().ToList().Count, 4, "play, options, input device, margin");
            });
        }

        [UnityTest]
        public IEnumerator Responsive_WritesASizeClassAndShowsSixTiles()
        {
            yield return Run<ResponsiveSample>(root =>
            {
                var shell = root.Q(className: "fdp-shell");
                Assert.IsNotNull(shell);
                Assert.IsTrue(shell.ClassListContains("fdp-compact") || shell.ClassListContains("fdp-regular") || shell.ClassListContains("fdp-expanded"),
                    "one size class on the shell root");
                Assert.AreEqual(6, root.Query(className: "fdp-sample-tile").ToList().Count);
            });
        }

        [UnityTest]
        public IEnumerator ThemingLanguage_ShowsLocalizedTexts()
        {
            yield return Run<ThemingLanguageSample>(root =>
            {
                var title = root.Q<LocalizedLabel>();
                Assert.IsNotNull(title);
                Assert.AreEqual("Theming and language", title.text);
            });
        }


        // ---- the five samples that came with the first version ----

        [UnityTest]
        public IEnumerator UIKitQuickStart_ShowsAThemedCard()
        {
            yield return Run<UIKitQuickStart>(root =>
            {
                Assert.IsNotNull(root.Q(className: "ds-root"), "the themed page");
                Assert.IsNotNull(root.Q(className: "ds-card"), "the card");
                Assert.IsNotNull(root.Q<Button>(), "the Light/Dark switch");
            });
        }

        [UnityTest]
        public IEnumerator DesignSystemShowcase_BuildsThePageAndSwitchesTheTheme()
        {
            yield return Run<DesignSystemShowcase>(root =>
            {
                Assert.IsNotNull(root.Q(className: "ds-root"), "the themed page");
                Assert.IsNotNull(root.Q<Button>("theme-toggle"), "the theme switch");
            });
        }

        [UnityTest]
        public IEnumerator AnimationDemo_BuildsItsPage()
        {
            _host = new GameObject("sample under test");
            _host.SetActive(false);
            var document = _host.AddComponent<UIDocument>();
            document.panelSettings = UIKitInstaller.CreatePanelSettings();
            _host.AddComponent<AnimationToolkitDemo>();
            _host.SetActive(true);
            for (var i = 0; i < 40; i++)
                yield return null;
            Assert.Greater(document.rootVisualElement.childCount, 0, "the demo adds its elements");
            var settings = document.panelSettings;
            UnityEngine.Object.Destroy(_host);
            _host = null;
            UnityEngine.Object.Destroy(settings);
            yield return null;
        }

        [UnityTest]
        public IEnumerator ComponentsSamples_EachComponentBuilds()
        {
            _host = new GameObject("sample under test");
            _host.SetActive(false);
            var document = _host.AddComponent<UIDocument>();
            document.panelSettings = UIKitInstaller.CreatePanelSettings();
            _host.SetActive(true);
            yield return null;
            yield return null;

            var components = new VisualElement[]
            {
                new CounterComponent(), new QueryExampleComponent(), new ResourcesExampleComponent(), new EventInterfacesView(), new UxmlTraitsExampleComponent(),
            };
            foreach (var component in components)
                document.rootVisualElement.Add(component);
            // A component loads its layout when it attaches to the panel; give it up to two seconds, not a fixed number of frames.
            var until = Time.realtimeSinceStartup + 2f;
            while (Time.realtimeSinceStartup < until && Array.Exists(components, c => c.childCount == 0))
                yield return null;
            foreach (var component in components)
            {
                Assert.IsNotNull(component.panel, component.GetType().Name + " is on the panel");
                Assert.Greater(component.childCount, 0, component.GetType().Name + " built its children");
            }

            var settings = document.panelSettings;
            UnityEngine.Object.Destroy(_host);
            _host = null;
            UnityEngine.Object.Destroy(settings);
            yield return null;
        }

        private IEnumerator Run<T>(Action<VisualElement> check) where T : MonoBehaviour
        {
            _host = new GameObject("sample under test");
            _host.AddComponent<T>();
            // Start runs on the first frame; the document builds its tree and the first layout pass needs a few more.
            for (var i = 0; i < 5; i++)
                yield return null;

            var document = UnityEngine.Object.FindFirstObjectByType<UIDocument>();
            Assert.IsNotNull(document, "the sample must create a UIDocument");
            Assert.IsNotNull(document.rootVisualElement.panel, "the document must be on a panel");
            check(document.rootVisualElement);

            for (var i = 0; i < 30; i++)
                yield return null;

            UnityEngine.Object.Destroy(_host);
            _host = null;
            yield return null;
            Assert.IsNull(UnityEngine.Object.FindFirstObjectByType<UIDocument>(), "destroying the sample must remove its document");
        }
    }
}
