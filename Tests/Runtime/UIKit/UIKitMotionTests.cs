using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UIElements;

namespace FutureDigitalPlus.UIKit.Tests
{
    public class UIKitMotionTests
    {
        private PanelHarness _panel;

        [TearDown]
        public void TearDown()
        {
            LogAssert.ignoreFailingMessages = false;
            _panel?.Dispose();
        }

        private static IEnumerator WaitUntil(System.Func<bool> condition, float seconds = 5f)
        {
            var end = Time.realtimeSinceStartup + seconds;
            while (!condition() && Time.realtimeSinceStartup < end) yield return null;
        }

        [UnityTest]
        public IEnumerator FadeIn_ShowsAndEndsOpaque_ThenFadeOut_Hides()
        {
            if (!Application.isPlaying) Assert.Ignore("Needs a runtime panel (Play Mode).");
            LogAssert.ignoreFailingMessages = true;
            _panel = PanelHarness.Create();
            yield return PanelHarness.Frames();

            var element = new VisualElement();
            element.style.display = DisplayStyle.None;
            _panel.Root.Add(element);

            var shown = false;
            element.FadeIn(80, () => shown = true);
            Assert.AreEqual(DisplayStyle.Flex, element.style.display.value);
            yield return WaitUntil(() => shown);
            Assert.IsTrue(shown, "FadeIn completion callback");
            Assert.AreEqual(1f, element.style.opacity.value, 0.01f);

            var hidden = false;
            element.FadeOut(80, hide: true, onDone: () => hidden = true);
            yield return WaitUntil(() => hidden);
            Assert.IsTrue(hidden, "FadeOut completion callback");
            Assert.AreEqual(DisplayStyle.None, element.style.display.value);
        }
    }
}
