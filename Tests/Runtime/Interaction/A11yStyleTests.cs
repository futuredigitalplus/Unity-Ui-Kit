using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UIElements;

namespace FutureDigitalPlus.UIKit.Interaction.Tests
{
    /// <summary>A11y.uss: reduced motion, text size steps and bold text, driven by the classes <see cref="AccessibilityApplier"/> writes.</summary>
    public class A11yStyleTests
    {
        private InteractionPanel _panel;

        [SetUp]
        public void SetUp() => LogAssert.ignoreFailingMessages = true;

        [TearDown]
        public void TearDown()
        {
            LogAssert.ignoreFailingMessages = false;
            _panel?.Dispose();
            _panel = null;
        }

        private static List<float> Durations(VisualElement e)
        {
            var list = new List<float>();
            foreach (var t in e.resolvedStyle.transitionDuration)
                list.Add(t.unit == TimeUnit.Millisecond ? t.value / 1000f : t.value);
            return list;
        }

        [UnityTest]
        public IEnumerator ReduceMotion_ZeroesTokenDrivenAndLiteralTransitions()
        {
            _panel = InteractionPanel.Create();
            var button = InteractionPanel.WithClasses(new Button { text = "Play" }, "ds-btn ds-btn--primary");
            var drawer = InteractionPanel.WithClasses(new VisualElement(), "ds-drawer");
            _panel.Screen.Add(button);
            _panel.Screen.Add(drawer);
            yield return InteractionPanel.Frames();

            Assert.IsTrue(Durations(button).Exists(d => d > 0f), "baseline: the button transitions");
            Assert.IsTrue(Durations(drawer).Exists(d => d > 0f), "baseline: the drawer transitions");

            using (var a11y = AccessibilityApplier.Bind(_panel.Root, new AccessibilityOptions { ReduceMotion = true }, new FakeSystem()))
            {
                yield return InteractionPanel.Frames();
                Assert.IsTrue(_panel.Root.ClassListContains("fdp-reduce-motion"));
                Assert.IsTrue(Durations(button).TrueForAll(d => d == 0f), "token driven: " + string.Join(",", Durations(button)));
                Assert.IsTrue(Durations(drawer).TrueForAll(d => d == 0f), "literal durations: " + string.Join(",", Durations(drawer)));
            }
        }

        [UnityTest]
        public IEnumerator BoldText_MakesBodyTextBold()
        {
            _panel = InteractionPanel.Create();
            var label = InteractionPanel.WithClasses(new Label("Body"), "ds-body-1");
            _panel.Screen.Add(label);
            yield return InteractionPanel.Frames();
            Assert.AreNotEqual(FontStyle.Bold, label.resolvedStyle.unityFontStyleAndWeight);

            using (AccessibilityApplier.Bind(_panel.Root, new AccessibilityOptions { BoldText = true }, new FakeSystem()))
            {
                yield return InteractionPanel.Frames();
                Assert.AreEqual(FontStyle.Bold, label.resolvedStyle.unityFontStyleAndWeight);
            }
        }

        [UnityTest]
        public IEnumerator TextScale_EnlargesTheType_AndNoControlClips([Values(150, 200)] int percent)
        {
            _panel = InteractionPanel.Create(1200, 800);
            var s = _panel.Screen;
            var controls = new List<(string name, VisualElement element)>();
            void Add(string name, VisualElement e) { controls.Add((name, e)); s.Add(e); }
            Add("ds-btn", InteractionPanel.WithClasses(new Button { text = "Continue" }, "ds-btn ds-btn--primary"));
            Add("ds-btn--sm", InteractionPanel.WithClasses(new Button { text = "Continue" }, "ds-btn ds-btn--ghost ds-btn--sm"));
            Add("ds-btn--lg", InteractionPanel.WithClasses(new Button { text = "Continue" }, "ds-btn ds-btn--primary ds-btn--lg"));
            Add("ds-tab", InteractionPanel.WithClasses(new Button { text = "Statistics" }, "ds-tab"));
            Add("ds-chip", InteractionPanel.WithClasses(new Label("New"), "ds-chip"));
            Add("ds-badge", InteractionPanel.WithClasses(new Label("Rare"), "ds-badge"));
            Add("ds-tag", InteractionPanel.WithClasses(new Label("Tag"), "ds-tag"));
            Add("ds-input", InteractionPanel.WithClasses(new TextField { value = "Player name" }, "ds-input"));
            Add("ds-dropdown", InteractionPanel.WithClasses(new DropdownField(new List<string> { "Medium" }, 0), "ds-dropdown"));
            var body = InteractionPanel.WithClasses(new Label("Body"), "ds-body-1");
            s.Add(body);
            yield return InteractionPanel.Frames(4);
            var baseSize = body.resolvedStyle.fontSize;

            using (AccessibilityApplier.Bind(_panel.Root, new AccessibilityOptions { TextScalePercent = percent }, new FakeSystem()))
            {
                yield return InteractionPanel.Frames(4);
                Assert.IsTrue(_panel.Root.ClassListContains("fdp-text-scale-" + percent));
                Assert.Greater(body.resolvedStyle.fontSize, baseSize * (percent / 100f) * 0.9f, "the type grew");

                var heights = controls.ConvertAll(c => c.element.resolvedStyle.height);
                foreach (var c in controls)
                {
                    c.element.style.height = StyleKeyword.Auto;
                    c.element.style.minHeight = 0;
                }
                yield return InteractionPanel.Frames(3);
                for (var i = 0; i < controls.Count; i++)
                    Assert.GreaterOrEqual(heights[i] + 0.5f, controls[i].element.layout.height, $"{controls[i].name} clips its text at {percent} percent");
            }
        }

        [UnityTest]
        public IEnumerator TextScale_KeepsTheTouchTierMinimums()
        {
            _panel = InteractionPanel.Create();
            var button = InteractionPanel.WithClasses(new Button { text = "Play" }, "ds-btn");
            _panel.Screen.Add(button);
            _panel.Root.AddToClassList("fdp-input-touch");
            yield return InteractionPanel.Frames();
            var touchHeight = button.resolvedStyle.height;
            Assert.GreaterOrEqual(touchHeight, 47.5f, "baseline: the touch tier makes buttons 48 tall");

            using (AccessibilityApplier.Bind(_panel.Root, new AccessibilityOptions { TextScalePercent = 125 }, new FakeSystem()))
            {
                yield return InteractionPanel.Frames(4);
                Assert.GreaterOrEqual(button.resolvedStyle.height, touchHeight - 0.5f, "a larger text size must not shrink a touch target");
            }
        }

        private sealed class FakeSystem : ISystemAccessibility
        {
            public SystemAccessibility Current => SystemAccessibility.Default;
            public event System.Action Changed { add { } remove { } }
        }
    }
}
