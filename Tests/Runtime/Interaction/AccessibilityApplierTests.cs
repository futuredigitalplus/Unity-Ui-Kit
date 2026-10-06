using System;
using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine.TestTools;
using UnityEngine.UIElements;

namespace FutureDigitalPlus.UIKit.Interaction.Tests
{
    public class AccessibilityApplierTests
    {
        private sealed class FakeSystem : ISystemAccessibility
        {
            private Action _changed;
            public SystemAccessibility Value = SystemAccessibility.Default;
            public int Subscribers => _changed?.GetInvocationList().Length ?? 0;
            public SystemAccessibility Current => Value;
            public event Action Changed { add { _changed += value; } remove { _changed -= value; } }
            public void Raise() => _changed?.Invoke();
        }

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

        private static string[] A11yClasses(VisualElement root)
        {
            var list = new List<string>();
            foreach (var c in root.GetClasses())
                if (c.StartsWith("fdp-reduce-") || c.StartsWith("fdp-high-") || c.StartsWith("fdp-bold-") || c.StartsWith("fdp-text-scale-")) list.Add(c);
            list.Sort();
            return list.ToArray();
        }

        [UnityTest]
        public IEnumerator Defaults_WriteNoClasses()
        {
            _panel = InteractionPanel.Create();
            yield return InteractionPanel.Frames();
            using (var applier = AccessibilityApplier.Bind(_panel.Root, null, new FakeSystem()))
            {
                CollectionAssert.IsEmpty(A11yClasses(_panel.Root));
                Assert.AreEqual(EffectiveAccessibility.Default, applier.Current);
            }
        }

        [UnityTest]
        public IEnumerator Options_AreWrittenAsClasses_AndRefreshAppliesChanges()
        {
            _panel = InteractionPanel.Create();
            yield return InteractionPanel.Frames();
            var options = new AccessibilityOptions { ReduceMotion = true, HighContrast = true, BoldText = true, TextScalePercent = 150 };
            using (var applier = AccessibilityApplier.Bind(_panel.Root, options, new FakeSystem()))
            {
                CollectionAssert.AreEqual(new[] { "fdp-bold-text", "fdp-high-contrast", "fdp-reduce-motion", "fdp-text-scale-150" }, A11yClasses(_panel.Root));

                options.ReduceMotion = false;
                options.TextScalePercent = 200;
                Assert.AreEqual(150, applier.Current.TextScalePercent, "nothing changes before Refresh");
                applier.Refresh();
                CollectionAssert.AreEqual(new[] { "fdp-bold-text", "fdp-high-contrast", "fdp-text-scale-200" }, A11yClasses(_panel.Root), "exactly one text step class at a time");

                options.TextScalePercent = 100;
                applier.Refresh();
                CollectionAssert.AreEqual(new[] { "fdp-bold-text", "fdp-high-contrast" }, A11yClasses(_panel.Root), "the design size has no class");
            }
        }

        [UnityTest]
        public IEnumerator SystemChange_IsPickedUp_AndRaisesChanged()
        {
            _panel = InteractionPanel.Create();
            yield return InteractionPanel.Frames();
            var system = new FakeSystem();
            using (var applier = AccessibilityApplier.Bind(_panel.Root, null, system))
            {
                var seen = new List<EffectiveAccessibility>();
                applier.Changed += seen.Add;

                system.Value = new SystemAccessibility(1.5f, true, false, false);
                system.Raise();

                CollectionAssert.AreEqual(new[] { "fdp-bold-text", "fdp-text-scale-150" }, A11yClasses(_panel.Root));
                Assert.AreEqual(1, seen.Count);
                Assert.AreEqual(150, seen[0].TextScalePercent);
                Assert.IsTrue(applier.Current.BoldText);

                system.Raise();   // same values again: no event
                Assert.AreEqual(1, seen.Count);
            }
        }

        [UnityTest]
        public IEnumerator Dispose_RemovesTheClasses_AndUnsubscribes()
        {
            _panel = InteractionPanel.Create();
            yield return InteractionPanel.Frames();
            var system = new FakeSystem();
            var applier = AccessibilityApplier.Bind(_panel.Root, new AccessibilityOptions { ReduceMotion = true, TextScalePercent = 125 }, system);
            Assert.AreEqual(1, system.Subscribers);

            applier.Dispose();
            CollectionAssert.IsEmpty(A11yClasses(_panel.Root));
            Assert.AreEqual(0, system.Subscribers);

            system.Value = new SystemAccessibility(2f, true, true, true);
            system.Raise();
            CollectionAssert.IsEmpty(A11yClasses(_panel.Root), "a disposed applier writes nothing");
        }

        [UnityTest]
        public IEnumerator ScaleMotion_AsADelegate_ReadsTheLatestValue()
        {
            _panel = InteractionPanel.Create();
            yield return InteractionPanel.Frames();
            var options = new AccessibilityOptions();
            using (var applier = AccessibilityApplier.Bind(_panel.Root, options, new FakeSystem()))
            {
                Func<int, int> scale = applier.ScaleMotion;
                Assert.AreEqual(300, scale(300));
                options.ReduceMotion = true;
                applier.Refresh();
                Assert.AreEqual(0, scale(300));
            }
        }

        [UnityTest]
        public IEnumerator DefaultSystemSource_ReadsWhatUnityExposes_WithoutThrowing()
        {
            _panel = InteractionPanel.Create();
            yield return InteractionPanel.Frames();
            using (var applier = AccessibilityApplier.Bind(_panel.Root))
            {
                Assert.IsNotNull(applier);
                Assert.GreaterOrEqual(applier.Current.TextScalePercent, 100);
            }

            using (var system = new UnitySystemAccessibility())
                Assert.Greater(system.Current.FontScale, 0f);
        }

        [UnityTest]
        public IEnumerator Bind_Null_ReturnsNull()
        {
            Assert.IsNull(AccessibilityApplier.Bind(null));
            yield return null;
        }
    }
}
