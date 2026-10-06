using System.Collections;
using FutureDigitalPlus.UIKit.Animation;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UIElements;

namespace FutureDigitalPlus.UIKit.Tests.AnimationModule
{
    /// <summary>
    /// New tests that need a live runtime panel (the UI Toolkit scheduler drives the animations), so they run in Play Mode only.
    /// In Edit Mode they are reported as ignored.
    /// </summary>
    public class AnimationPlayModeTests
    {
        private const float TimeoutSeconds = 5f;

        private GameObject _host;
        private PanelSettings _panelSettings;

        [TearDown]
        public void TearDown()
        {
            LogAssert.ignoreFailingMessages = false;
            if (_host != null) Object.DestroyImmediate(_host);
            if (_panelSettings != null) Object.DestroyImmediate(_panelSettings);
        }

        private IEnumerator CreateRoot(System.Action<VisualElement> onReady)
        {
            // A bare PanelSettings has no theme style sheet; Unity may log about that, which must not fail these tests.
            LogAssert.ignoreFailingMessages = true;
            _host = new GameObject("EATK_Test_Host");
            _panelSettings = ScriptableObject.CreateInstance<PanelSettings>();
            var document = _host.AddComponent<UIDocument>();
            document.panelSettings = _panelSettings;
            yield return null;
            Assert.IsNotNull(document.rootVisualElement, "UIDocument produced no root element");
            onReady(document.rootVisualElement);
        }

        private static IEnumerator WaitUntil(System.Func<bool> condition)
        {
            var end = Time.realtimeSinceStartup + TimeoutSeconds;
            while (!condition() && Time.realtimeSinceStartup < end) yield return null;
        }

        [UnityTest]
        public IEnumerator AnimateOpacity_RunsToTheEndValue_AndInvokesCallback()
        {
            if (!Application.isPlaying) Assert.Ignore("Needs a runtime panel (Play Mode).");

            VisualElement root = null;
            yield return CreateRoot(r => root = r);

            var element = new VisualElement();
            root.Add(element);

            var done = false;
            element.AnimateOpacity(0f, 1f, 100, () => done = true);
            yield return WaitUntil(() => done);

            Assert.IsTrue(done, "completion callback was not invoked");
            Assert.AreEqual(1f, element.style.opacity.value, 0.01f);
        }

        [UnityTest]
        public IEnumerator AnimateWidth_RunsToTheEndValue()
        {
            if (!Application.isPlaying) Assert.Ignore("Needs a runtime panel (Play Mode).");

            VisualElement root = null;
            yield return CreateRoot(r => root = r);

            var element = new VisualElement();
            root.Add(element);

            var done = false;
            element.AnimateWidth(10f, 120f, 100, () => done = true, Easy.Linear);
            yield return WaitUntil(() => done);

            Assert.IsTrue(done, "completion callback was not invoked");
            Assert.AreEqual(120f, element.style.width.value.value, 0.5f);
        }

        [UnityTest]
        public IEnumerator ShowForDuration_FadesInAndBackOut()
        {
            if (!Application.isPlaying) Assert.Ignore("Needs a runtime panel (Play Mode).");

            VisualElement root = null;
            yield return CreateRoot(r => root = r);

            var element = new VisualElement();
            root.Add(element);
            yield return null;

            // Upstream's ShowForDuration ignores its 'callback' argument, so completion is detected by waiting for the
            // whole sequence: fade in (50 ms) + display (50 ms) + fade out (500 ms, fixed in the toolkit).
            element.ShowForDuration(50, 50);
            var end = Time.realtimeSinceStartup + 1.5f;
            while (Time.realtimeSinceStartup < end) yield return null;

            Assert.Less(element.style.opacity.value, 0.05f, "element should be faded out again");
        }
    }
}
