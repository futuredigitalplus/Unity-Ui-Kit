using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UIElements;

namespace FutureDigitalPlus.UIKit.Hud.Tests
{
    public class MeterBarTests
    {
        private HudPanel _panel;

        [SetUp]
        public void SetUp() => LogAssert.ignoreFailingMessages = true;

        [TearDown]
        public void TearDown()
        {
            LogAssert.ignoreFailingMessages = false;
            _panel?.Dispose();
        }

        private MeterBar AddBar(bool reduceMotion = false)
        {
            _panel = HudPanel.Create(reduceMotion);
            var bar = new MeterBar { style = { width = 200 } };
            _panel.Root.Add(bar);
            return bar;
        }

        private static float FillPercent(MeterBar bar) => bar.Q<VisualElement>("fill").style.width.value.value;

        private static float TrailPercent(MeterBar bar) => bar.Q<VisualElement>("trail").style.width.value.value;

        [UnityTest]
        public IEnumerator Value_SetsFillWidthInPercent()
        {
            var bar = AddBar();
            yield return HudPanel.Frames();
            bar.Value = 60f;
            Assert.AreEqual(60f, FillPercent(bar), 0.01f);
            bar.MaxValue = 200f;
            Assert.AreEqual(30f, FillPercent(bar), 0.01f);
            Assert.IsTrue(bar.ClassListContains("ds-meter"), "carries the design system skin");
        }

        [UnityTest]
        public IEnumerator Level_TogglesWarningAndDangerClassesOnTheFill()
        {
            var bar = AddBar();
            yield return HudPanel.Frames();
            var fill = bar.Q<VisualElement>("fill");
            bar.Value = 40f;
            Assert.IsTrue(fill.ClassListContains("is-warning"));
            bar.Value = 10f;
            Assert.IsTrue(fill.ClassListContains("is-danger"));
            Assert.IsFalse(fill.ClassListContains("is-warning"));
            bar.Value = 100f;
            Assert.IsFalse(fill.ClassListContains("is-danger"));
            Assert.IsFalse(fill.ClassListContains("is-warning"));
        }

        [UnityTest]
        public IEnumerator HighIsBad_FlipsTheThresholds()
        {
            var bar = AddBar();
            yield return HudPanel.Frames();
            bar.HighIsBad = true;
            bar.Value = 0f;
            Assert.AreEqual(MeterLevel.Normal, bar.Level);
            bar.Value = 95f;
            Assert.AreEqual(MeterLevel.Danger, bar.Level);
        }

        [UnityTest]
        public IEnumerator Trail_StaysThenFollowsAfterADecrease()
        {
            var bar = AddBar();
            yield return HudPanel.Frames();
            bar.Value = 50f;
            Assert.AreEqual(100f, TrailPercent(bar), 0.01f, "the trail waits at the old value");
            bar.Step(0.1f);
            Assert.AreEqual(100f, TrailPercent(bar), 0.01f, "still inside the delay");
            bar.Step(1f);
            bar.Step(1f);
            Assert.AreEqual(50f, TrailPercent(bar), 0.01f, "the trail caught up with the fill");
        }

        [UnityTest]
        public IEnumerator Trail_RunsFromTheSchedulerAndStopsByItself()
        {
            var bar = AddBar();
            yield return HudPanel.Frames();
            bar.Value = 80f;
            var end = Time.realtimeSinceStartup + 4f;
            while (TrailPercent(bar) > 80.01f && Time.realtimeSinceStartup < end) yield return null;
            Assert.AreEqual(80f, TrailPercent(bar), 0.01f);
        }

        [UnityTest]
        public IEnumerator ReducedMotion_HidesTheTrail()
        {
            var bar = AddBar(reduceMotion: true);
            yield return HudPanel.Frames();
            bar.Value = 30f;
            Assert.AreEqual(DisplayStyle.None, bar.Q<VisualElement>("trail").style.display.value);
        }

        [UnityTest]
        public IEnumerator ShowTrailFalse_HidesTheTrail()
        {
            var bar = AddBar();
            yield return HudPanel.Frames();
            bar.ShowTrail = false;
            bar.Value = 30f;
            Assert.AreEqual(DisplayStyle.None, bar.Q<VisualElement>("trail").style.display.value);
        }

        [UnityTest]
        public IEnumerator LabelModes_WriteTheExpectedText()
        {
            var bar = AddBar();
            yield return HudPanel.Frames();
            var label = bar.Q<Label>("label");
            Assert.AreEqual(DisplayStyle.None, label.style.display.value);
            bar.Set(184f, 240f);
            bar.LabelMode = MeterLabelMode.Value;
            Assert.AreEqual("184", label.text);
            bar.LabelMode = MeterLabelMode.ValueAndMax;
            Assert.AreEqual("184 / 240", label.text);
            bar.LabelMode = MeterLabelMode.Percent;
            Assert.AreEqual("77%", label.text);
        }

        [UnityTest]
        public IEnumerator Segments_AddDividers_AndSizeSwitchesTheSkinClass()
        {
            var bar = AddBar();
            yield return HudPanel.Frames();
            bar.Segments = 5;
            Assert.AreEqual(4, bar.Q<VisualElement>("segments").childCount);
            bar.Segments = 0;
            Assert.AreEqual(0, bar.Q<VisualElement>("segments").childCount);
            bar.Size = MeterBarSize.Large;
            Assert.IsTrue(bar.ClassListContains("ds-meter--lg"));
            bar.Size = MeterBarSize.Small;
            Assert.IsTrue(bar.ClassListContains("ds-meter--sm"));
            Assert.IsFalse(bar.ClassListContains("ds-meter--lg"));
        }

        [UnityTest]
        public IEnumerator FillGeometry_MatchesTheValueAfterLayout()
        {
            var bar = AddBar();
            bar.Value = 50f;
            yield return HudPanel.WaitSeconds(0.6f); // lets the width transition finish
            var fill = bar.Q<VisualElement>("fill");
            Assert.AreEqual(bar.contentRect.width * 0.5f, fill.resolvedStyle.width, 2f);
        }

        [UnityTest]
        public IEnumerator SteadyState_AllocatesNothing()
        {
            var bar = AddBar();
            yield return HudPanel.Frames();
            if (!AllocProbe.IsAvailable()) Assert.Ignore("The runtime does not report per-thread allocations.");
            bar.Value = 70f;
            bar.Step(2f);
            bar.Step(2f);

            Assert.AreEqual(0L, AllocProbe.Measure(() => bar.Value = 70f), "setting the same value");
            Assert.AreEqual(0L, AllocProbe.Measure(() => bar.Step(0.016f)), "stepping a resting bar");

            var v = 70f;
            Assert.AreEqual(0L, AllocProbe.Measure(() =>
            {
                v = v > 20f ? v - 0.5f : 70f;
                bar.Value = v;
                bar.Step(0.016f);
            }), "a moving value with a trail (no label)");
        }
    }
}
