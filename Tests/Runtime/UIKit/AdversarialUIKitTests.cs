using System.Collections;
using FutureDigitalPlus.UIKit.Animation;
using FutureDigitalPlus.UIKit.DesignSystem.Theme.Applier;
using FutureDigitalPlus.UIKit.DesignSystem.Theme.Data;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UIElements;

namespace FutureDigitalPlus.UIKit.Tests
{
    /// <summary>
    /// Hostile-usage tests of the facade: the installer called twice, documents that come and go, animations that overlap or run on elements that are
    /// not in a panel, and a theme that changes or dies while it is worn.
    /// </summary>
    public class AdversarialUIKitTests
    {
        private PanelHarness _panel;
        private ThemeData _light;
        private ThemeData _dark;

        [SetUp]
        public void SetUp()
        {
            _light = Resources.Load<ThemeData>(ThemeData.LightResource);
            _dark = Resources.Load<ThemeData>(ThemeData.DarkResource);
            Assert.IsNotNull(_light);
            Assert.IsNotNull(_dark);
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
            LogAssert.ignoreFailingMessages = true;
            _panel = PanelHarness.Create();
            yield return PanelHarness.Frames();
        }

        private static IEnumerator WaitUntil(System.Func<bool> condition, float seconds = 3f)
        {
            var end = Time.realtimeSinceStartup + seconds;
            while (!condition() && Time.realtimeSinceStartup < end) yield return null;
        }

        private static void PretendNotch(UIKitInstallation kit)
        {
            kit.ScreenSizeSource = () => new Vector2(PanelHarness.Width * 2, PanelHarness.Height * 2);
            kit.SafeAreaSource = () => new Rect(20, 60, PanelHarness.Width * 2 - 40, PanelHarness.Height * 2 - 160);
        }

        [UnityTest]
        public IEnumerator Install_TwiceOnTheSameRoot_LeavesNothingBehindWhateverTheDisposeOrder([Values(false, true)] bool firstFirst)
        {
            yield return CreatePanel();
            var root = _panel.Root;
            root.style.paddingTop = 7f;
            var options = new UIKitOptions { Mobile = MobileMode.Auto, Theme = _light };
            var first = UIKitInstaller.Install(root, options);
            PretendNotch(first);
            first.Refresh();
            var second = UIKitInstaller.Install(root, options);
            PretendNotch(second);
            second.Refresh();
            yield return PanelHarness.Frames();

            if (firstFirst)
            {
                first.Dispose();
                second.Dispose();
            }
            else
            {
                second.Dispose();
                first.Dispose();
            }

            Assert.AreEqual(7f, root.style.paddingTop.value.value, 0.01f, "the padding the host had is back");
            Assert.AreEqual(StyleKeyword.Null, root.style.paddingLeft.keyword, "no leftover safe-area padding");
            Assert.IsNull(ThemeRuntime.WornBy(root));
            Assert.IsFalse(root.ClassListContains(_light.ScopeClass ?? "none"));
            Assert.IsFalse(root.parent.styleSheets.Contains(Resources.Load<StyleSheet>(UIKitOptions.BaseStylesResource)), "no base sheet left");
        }

        [UnityTest]
        public IEnumerator Dispose_TwiceAndThenRefreshAndSetTheme_IsHarmless()
        {
            yield return CreatePanel();
            var kit = UIKitInstaller.Install(_panel.Root, new UIKitOptions { Theme = _light });
            kit.Dispose();
            kit.Dispose();
            kit.Refresh();
            kit.SetTheme(_dark);
            yield return PanelHarness.Frames();

            Assert.IsNull(ThemeRuntime.WornBy(_panel.Root), "SetTheme after Dispose must not dress the root");
            Assert.IsFalse(_panel.Root.ClassListContains(UIKitInstaller.MobileClass));
        }

        [UnityTest]
        public IEnumerator Host_DisabledAndEnabledManyTimes_KeepsExactlyOneBaseSheetAndNoLeftovers()
        {
            if (!Application.isPlaying) Assert.Ignore("Needs a runtime panel (Play Mode).");
            LogAssert.ignoreFailingMessages = true;
            UIKitHost host = null;
            _panel = PanelHarness.Create(go =>
            {
                host = go.AddComponent<UIKitHost>();
                host.Options.Theme = _light;
            });
            yield return PanelHarness.Frames();
            var baseSheet = Resources.Load<StyleSheet>(UIKitOptions.BaseStylesResource);

            for (var i = 0; i < 20; i++)
            {
                host.enabled = false;
                host.enabled = true;
                if (i % 5 == 0) yield return null;
            }
            yield return PanelHarness.Frames();

            var count = 0;
            for (var i = 0; i < _panel.Root.parent.styleSheets.count; i++)
                if (_panel.Root.parent.styleSheets[i] == baseSheet) count++;
            Assert.AreEqual(1, count, "one base sheet");
            Assert.AreEqual(1, _panel.Root.styleSheets.count - CountSheets(_panel.Root, baseSheet), "one theme sheet on the root");

            host.enabled = false;
            Assert.AreEqual(0, CountSheets(_panel.Root.parent, baseSheet), "no base sheet after the last user left");
            Assert.AreEqual(0, _panel.Root.styleSheets.count);
        }

        private static int CountSheets(VisualElement element, StyleSheet sheet)
        {
            var count = 0;
            for (var i = 0; i < element.styleSheets.count; i++)
                if (element.styleSheets[i] == sheet) count++;
            return count;
        }

        [UnityTest]
        public IEnumerator Host_DocumentDestroyedWhileThemed_DoesNotThrowAndReleasesRuntimeSheets()
        {
            if (!Application.isPlaying) Assert.Ignore("Needs a runtime panel (Play Mode).");
            LogAssert.ignoreFailingMessages = true;
            var before = ThemeRuntime.LiveRuntimeSheets;
            var theme = ThemeData.CreateRuntime(_light);
            UIKitHost host = null;
            _panel = PanelHarness.Create(go =>
            {
                host = go.AddComponent<UIKitHost>();
                host.Options.Theme = theme;
            });
            yield return PanelHarness.Frames();
            Assert.AreEqual(before + 1, ThemeRuntime.LiveRuntimeSheets);

            Object.Destroy(_panel.Host);
            yield return PanelHarness.Frames();

            Assert.AreEqual(before, ThemeRuntime.LiveRuntimeSheets, "the generated sheet went with its last wearer");
            Object.Destroy(theme);
        }

        [UnityTest]
        public IEnumerator Host_TwoDocumentsOnOnePanel_ShareTheBaseSheetUntilTheLastOneLeaves()
        {
            yield return CreatePanel();
            var second = new GameObject("SecondDocument");
            second.SetActive(false);
            var document = second.AddComponent<UIDocument>();
            document.panelSettings = _panel.Settings;
            var hostA = _panel.Host.AddComponent<UIKitHost>();
            var hostB = second.AddComponent<UIKitHost>();
            hostA.Options.ApplySafeArea = false;
            hostB.Options.ApplySafeArea = false;
            second.SetActive(true);
            yield return PanelHarness.Frames();
            var baseSheet = Resources.Load<StyleSheet>(UIKitOptions.BaseStylesResource);
            var holder = _panel.Root.parent;

            Assert.IsNotNull(hostA.Installation);
            Assert.IsNotNull(hostB.Installation);
            Assert.AreEqual(1, CountSheets(holder, baseSheet), "one shared base sheet");

            hostB.enabled = false;
            Object.Destroy(second);
            yield return PanelHarness.Frames();
            Assert.AreEqual(1, CountSheets(holder, baseSheet), "the other document still needs it");

            hostA.enabled = false;
            Assert.AreEqual(0, CountSheets(holder, baseSheet));
        }

        [UnityTest]
        public IEnumerator Host_ThemeSwappedEveryFrameForAWhile_EndsWearingTheLastOneWithOneSheet()
        {
            yield return CreatePanel();
            var kit = UIKitInstaller.Install(_panel.Root, new UIKitOptions { InstallBaseStyles = false, ApplySafeArea = false });
            var before = ThemeRuntime.LiveRuntimeSheets;
            ThemeData last = null;
            for (var i = 0; i < 60; i++)
            {
                var runtime = ThemeData.CreateRuntime(i % 2 == 0 ? _light : _dark);
                runtime.SetToken("--fdp-color-primary", new Color(i / 60f, 0.2f, 0.7f));
                kit.SetTheme(runtime);
                if (last != null) Object.Destroy(last);
                last = runtime;
                if (i % 7 == 0) yield return null;
            }
            yield return PanelHarness.Frames();

            Assert.AreSame(last, ThemeRuntime.WornBy(_panel.Root));
            Assert.AreEqual(before + 1, ThemeRuntime.LiveRuntimeSheets, "only the sheet of the worn theme is alive");
            Assert.AreEqual(1, _panel.Root.styleSheets.count);
            kit.Dispose();
            Assert.AreEqual(before, ThemeRuntime.LiveRuntimeSheets);
            Object.Destroy(last);
        }

        [UnityTest]
        public IEnumerator FadeIn_ZeroOrNegativeDuration_EndsOpaqueAndCompletes([Values(0, -1, -100000)] int duration)
        {
            yield return CreatePanel();
            var element = new VisualElement();
            element.style.display = DisplayStyle.None;
            _panel.Root.Add(element);

            var done = false;
            element.FadeIn(duration, () => done = true);
            yield return WaitUntil(() => done);

            Assert.IsTrue(done, "a zero length fade still completes");
            Assert.AreEqual(1f, element.style.opacity.value, 0.01f);
            Assert.IsFalse(float.IsNaN(element.resolvedStyle.opacity));

            done = false;
            element.FadeOut(duration, true, () => done = true);
            yield return WaitUntil(() => done);
            Assert.IsTrue(done);
            Assert.AreEqual(DisplayStyle.None, element.style.display.value);
        }

        [UnityTest]
        public IEnumerator FadeIn_AfterAFadeOutThatHasNotFinished_EndsVisible()
        {
            yield return CreatePanel();
            var element = new VisualElement();
            _panel.Root.Add(element);

            element.FadeOut(200, hide: true);
            yield return new WaitForSecondsRealtime(0.1f);
            element.FadeIn(200);
            yield return new WaitForSecondsRealtime(0.6f);

            Assert.AreEqual(DisplayStyle.Flex, element.style.display.value, "the late FadeOut must not hide an element that was faded in after it");
            Assert.AreEqual(1f, element.style.opacity.value, 0.01f);
        }

        [UnityTest]
        public IEnumerator FadeOut_ThenFadeIn_ThenFadeOutAgain_ErrsOnTheSideOfTheLastCall()
        {
            yield return CreatePanel();
            var element = new VisualElement();
            _panel.Root.Add(element);
            int doneCount = 0;

            for (var i = 0; i < 6; i++)
            {
                if (i % 2 == 0) element.FadeOut(120, true, () => doneCount++);
                else element.FadeIn(120, () => doneCount++);
                yield return new WaitForSecondsRealtime(0.03f);
            }
            yield return new WaitForSecondsRealtime(0.5f);

            // The last call (index 5) was a FadeIn.
            Assert.AreEqual(DisplayStyle.Flex, element.style.display.value);
            Assert.AreEqual(1f, element.style.opacity.value, 0.01f);
        }

        [UnityTest]
        public IEnumerator Animations_OnElementsOutsideAPanel_DoNotThrow()
        {
            yield return CreatePanel();
            var orphan = new VisualElement();

            Assert.DoesNotThrow(() => orphan.FadeIn(50));
            Assert.DoesNotThrow(() => orphan.FadeOut(50));
            Assert.DoesNotThrow(() => orphan.AnimateOpacity(0f, 1f, 50));
            Assert.DoesNotThrow(() => orphan.AnimateOpacityDelayed(0f, 1f, 50, 10), "a delayed animation on an element that has no parent yet");
            Assert.DoesNotThrow(() => orphan.ExecuteIn(() => { }, 10), "ExecuteIn on an element that has no parent yet");
            Assert.DoesNotThrow(() => ((VisualElement)null).FadeIn());
            Assert.DoesNotThrow(() => ((VisualElement)null).FadeOut());
        }

        [UnityTest]
        public IEnumerator Animations_ElementRemovedOrPanelDestroyedMidFlight_NeverThrowAndCanRunAgain()
        {
            yield return CreatePanel();
            var element = new VisualElement();
            _panel.Root.Add(element);
            var done = 0;
            element.FadeOut(300, true, () => done++);
            element.AnimateOpacityDelayed(0f, 1f, 200, 100, () => done++);
            yield return null;
            element.RemoveFromHierarchy();
            yield return new WaitForSecondsRealtime(0.5f);

            _panel.Root.Add(element);
            var again = false;
            element.FadeIn(50, () => again = true);
            yield return WaitUntil(() => again);
            Assert.IsTrue(again);

            element.FadeOut(300);
            Object.Destroy(_panel.Host);
            yield return PanelHarness.Frames();
            Assert.IsNull(element.panel);
        }
    }
}
