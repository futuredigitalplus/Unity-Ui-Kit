using System.Collections;
using FutureDigitalPlus.UIKit.Animation;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UIElements;

namespace FutureDigitalPlus.UIKit.Interaction.Tests
{
    public class EatkMotionTests
    {
        private InteractionPanel _panel;

        [SetUp]
        public void SetUp()
        {
            LogAssert.ignoreFailingMessages = true;
            EatkMotion.Scaler = null;
        }

        [TearDown]
        public void TearDown()
        {
            EatkMotion.Scaler = null;
            LogAssert.ignoreFailingMessages = false;
            _panel?.Dispose();
            _panel = null;
        }

        [Test]
        public void WithoutAScaler_DurationsPassThrough()
        {
            Assert.AreEqual(300, EatkMotion.Scale(300));
            Assert.AreEqual(0, EatkMotion.Scale(0));
            Assert.AreEqual(-5, EatkMotion.Scale(-5));
        }

        [Test]
        public void TheScaler_IsApplied_ButAPositiveDurationNeverBecomesZero()
        {
            EatkMotion.Scaler = ms => ms / 2;
            Assert.AreEqual(150, EatkMotion.Scale(300));

            EatkMotion.Scaler = ms => 0;
            Assert.AreEqual(1, EatkMotion.Scale(300), "a zero-length value animation is avoided");
            Assert.AreEqual(0, EatkMotion.Scale(0), "a request for 0 stays 0");
        }

        [Test]
        public void TheAccessibilityApplier_CanBeTheScaler()
        {
            var root = new VisualElement();
            var options = new AccessibilityOptions { ReduceMotion = true };
            using (var applier = AccessibilityApplier.Bind(root, options, new NoSystem()))
            {
                EatkMotion.Scaler = applier.ScaleMotion;
                Assert.AreEqual(1, EatkMotion.Scale(500));

                options.ReduceMotion = false;
                applier.Refresh();
                Assert.AreEqual(500, EatkMotion.Scale(500));
            }
        }

        [UnityTest]
        public IEnumerator AnimationsFinishAlmostAtOnce_WhenMotionIsReduced()
        {
            _panel = InteractionPanel.Create();
            var element = new VisualElement();
            element.style.width = 50;
            element.style.height = 50;
            _panel.Screen.Add(element);
            yield return InteractionPanel.Frames();

            EatkMotion.Scaler = ms => 0;
            var done = false;
            element.AnimateOpacity(0f, 1f, 5000, () => done = true);

            var waited = 0f;
            while (!done && waited < 1f)
            {
                waited += Time.unscaledDeltaTime;
                yield return null;
            }
            Assert.IsTrue(done, "a 5 second animation completed within a second under reduced motion");
        }

        private sealed class NoSystem : ISystemAccessibility
        {
            public SystemAccessibility Current => SystemAccessibility.Default;
            public event System.Action Changed { add { } remove { } }
        }
    }
}
