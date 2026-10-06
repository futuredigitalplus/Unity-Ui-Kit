using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UIElements;

namespace FutureDigitalPlus.UIKit.Foundation.Tests
{
    /// <summary>PlayMode tests of real-time behaviour: toast timing under a stopped game clock, and the loading overlay's show delay and minimum visible time.</summary>
    public class ToastLoadingPlayModeTests : PlayModeBase
    {
        VisualElement Toast()
        {
            return Layer(UiLayer.Toasts).Q<VisualElement>(UiNames.ToastRoot);
        }

        [UnityTest]
        public IEnumerator Toast_FadesInThenExpires_EvenWhileTheGameIsPausedWithTimeScaleZero()
        {
            yield return CreatePanel();
            UiShell shell = CreateShell();
            shell.Boot("Home");
            Time.timeScale = 0f;

            shell.Context.Toasts.ShowRaw("Paused toast", ToastKind.Info, 0.4f);
            Assert.IsNotNull(Toast());
            yield return Real(0.15f);
            Assert.IsNotNull(Toast(), "still within its time");
            Assert.IsTrue(Toast().ClassListContains(UiNames.ClassVisible), "faded in");

            yield return Real(0.9f);
            Assert.IsNull(Toast(), "gone after its time plus the fade, although the game clock never advanced");
        }

        [UnityTest]
        public IEnumerator Toast_TheNextOneAppearsAfterTheFirstIsGone()
        {
            yield return CreatePanel();
            UiShell shell = CreateShell();
            shell.Boot("Home");
            shell.Context.Toasts.ShowRaw("First", ToastKind.Info, 0.3f);
            shell.Context.Toasts.ShowRaw("Second", ToastKind.Warning, 0.3f);
            Assert.AreEqual("First", Toast().Q<Label>(UiNames.ToastLabel).text);
            yield return Real(0.9f);
            Assert.IsNotNull(Toast());
            Assert.AreEqual("Second", Toast().Q<Label>(UiNames.ToastLabel).text);
            Assert.IsTrue(Toast().ClassListContains("ds-toast--warning"));
        }

        [UnityTest]
        public IEnumerator Toast_ARepeatWhileVisibleExtendsTheLife()
        {
            yield return CreatePanel();
            UiShell shell = CreateShell();
            shell.Boot("Home");
            shell.Context.Toasts.ShowRaw("Same", ToastKind.Info, 0.5f);
            yield return Real(0.35f);
            shell.Context.Toasts.ShowRaw("Same", ToastKind.Info, 0.5f);
            yield return Real(0.35f);
            Assert.IsNotNull(Toast(), "the repeat restarted the timer");
            yield return Real(0.8f);
            Assert.IsNull(Toast());
        }

        [UnityTest]
        public IEnumerator Toast_StaysInsideAVeryNarrowHost()
        {
            yield return CreatePanel(220, 400);
            UiShell shell = CreateShell();
            shell.Boot("Home");
            shell.Context.Toasts.ShowRaw("A message that is long enough to want more width than this panel offers to a toast.", ToastKind.Info, 2f);
            yield return Real(0.4f);
            Rect bounds = Toast().worldBound;
            Assert.GreaterOrEqual(bounds.xMin, -0.5f);
            Assert.LessOrEqual(bounds.xMax, 220f + 0.5f);
        }

        [UnityTest]
        public IEnumerator Loading_ShowsAfterTheDelay_KeepsTheMinimumTime_ButBlocksInputFromTheStart()
        {
            yield return CreatePanel();
            UiShell shell = CreateShell();
            shell.Boot("Home");
            LoadingOverlay loading = (LoadingOverlay)shell.Context.Loading;

            LoadingToken token = loading.Begin(UiText.FromLiteral("Loading"));
            Assert.IsTrue(shell.Context.Input.IsBlocked, "input is blocked at once");
            Assert.IsFalse(loading.IsVisible, "but the graphic waits");
            yield return Real(0.1f);
            Assert.IsFalse(loading.IsVisible, "no flash for work that finishes quickly");

            yield return Real(0.25f);
            Assert.IsTrue(loading.IsVisible, "visible after the show delay");
            VisualElement backdrop = Layer(UiLayer.Loading).Q<VisualElement>(UiNames.LoadingBackdrop);
            Assert.IsTrue(backdrop.ClassListContains(UiNames.ClassVisible));

            token.Dispose();
            Assert.IsTrue(loading.IsVisible, "once shown it stays for the minimum time so it does not flicker");
            yield return Real(0.7f);
            Assert.IsFalse(loading.IsVisible);
            Assert.IsFalse(backdrop.ClassListContains(UiNames.ClassVisible));
            Assert.IsFalse(shell.Context.Input.IsBlocked);
        }

        [UnityTest]
        public IEnumerator Loading_WorkThatFinishesBeforeTheDelayNeverShowsTheGraphic()
        {
            yield return CreatePanel();
            UiShell shell = CreateShell();
            shell.Boot("Home");
            LoadingOverlay loading = (LoadingOverlay)shell.Context.Loading;
            bool everVisible = false;
            LoadingToken token = loading.Begin(UiText.FromLiteral("Quick"));
            for (int i = 0; i < 4; i++)
            {
                yield return null;
                everVisible |= loading.IsVisible;
            }
            token.Dispose();
            yield return Real(0.5f);
            everVisible |= loading.IsVisible;
            Assert.IsFalse(everVisible);
        }

        [UnityTest]
        public IEnumerator Loading_TheSpinnerTurnsOnlyWhileItIsVisible()
        {
            yield return CreatePanel();
            UiShell shell = CreateShell();
            shell.Boot("Home");
            LoadingOverlay loading = (LoadingOverlay)shell.Context.Loading;
            VisualElement spinner = Layer(UiLayer.Loading).Q<VisualElement>(UiNames.LoadingSpinner);
            LoadingToken token = loading.Begin(UiText.Empty);
            yield return Real(0.45f);
            Assert.IsTrue(loading.IsVisible);
            float first = spinner.resolvedStyle.rotate.angle.value;
            yield return Real(0.2f);
            float second = spinner.resolvedStyle.rotate.angle.value;
            Assert.AreNotEqual(first, second, "the scheduler loop turns the spinner");
            token.Dispose();
            yield return Real(0.8f);
            float stopped = spinner.resolvedStyle.rotate.angle.value;
            yield return Real(0.2f);
            Assert.AreEqual(stopped, spinner.resolvedStyle.rotate.angle.value, "the loop stops when the overlay hides");
        }

        [UnityTest]
        public IEnumerator Loading_TheBackdropSwallowsPointerInputWhileItBlocks()
        {
            yield return CreatePanel();
            UiShell shell = CreateShell();
            shell.Boot("Home");
            yield return FoundationPanel.Frames(3);
            Button button = ((ProbeScreen)shell.Router.CurrentScreen).Button;
            Vector2 center = button.worldBound.center;
            Assert.IsTrue(shell.Context.Input.IsOverUiAtPanelPosition(center));

            using (shell.Context.Loading.Begin(UiText.Empty))
            {
                yield return FoundationPanel.Frames(2);
                VisualElement picked = Panel.Panel.Pick(center);
                Assert.IsNotNull(picked);
                Assert.IsTrue(Layer(UiLayer.Loading).Contains(picked), "a tap lands on the loading backdrop, not on the button below it");
            }
        }
    }
}
