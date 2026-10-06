using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UIElements;

namespace FutureDigitalPlus.UIKit.Hud.Tests
{
    public class RadialAndCooldownTests
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

        [UnityTest]
        public IEnumerator Radial_ValueIsClamped()
        {
            _panel = HudPanel.Create();
            var radial = new RadialProgress();
            _panel.Root.Add(radial);
            yield return HudPanel.Frames();
            radial.Value = 3f;
            Assert.AreEqual(1f, radial.Value);
            radial.Value = -1f;
            Assert.AreEqual(0f, radial.Value);
        }

        [UnityTest]
        public IEnumerator Radial_PaintsTheRingInTheElementColour_AndTheTrackInTheTint()
        {
            _panel = HudPanel.Create();
            var radial = new RadialProgress { Thickness = 12f };
            radial.style.position = Position.Absolute;
            radial.style.left = 100;
            radial.style.top = 100;
            radial.style.width = 100;
            radial.style.height = 100;
            radial.style.color = Color.red;
            radial.style.unityBackgroundImageTintColor = Color.blue;
            radial.Value = 0.5f; // from the top, clockwise: the right half is the ring, the left half the track
            _panel.Root.Add(radial);
            yield return HudPanel.Frames(5);

            // ring radius 44, centre (150, 150): the ring crosses (194, 150) on the right and (106, 150) on the left
            var right = _panel.ReadPixel(194, 150);
            var left = _panel.ReadPixel(106, 150);
            var middle = _panel.ReadPixel(150, 150);
            Assert.Greater(right.r, 0.6f, "right half is the fill colour");
            Assert.Less(right.b, 0.4f);
            Assert.Greater(left.b, 0.6f, "left half is the track colour");
            Assert.Less(left.r, 0.4f);
            Assert.AreEqual(0f, middle.a, 0.05f, "the centre is empty");
        }

        [UnityTest]
        public IEnumerator Radial_Smoothing_MovesTowardTheValueFromTheScheduler()
        {
            _panel = HudPanel.Create();
            var radial = new RadialProgress { SmoothSeconds = 0.2f };
            _panel.Root.Add(radial);
            yield return HudPanel.Frames();
            radial.Value = 1f;
            yield return HudPanel.WaitSeconds(0.6f);
            Assert.IsFalse(radial.Step(0f), "reached the value and went idle");
        }

        [UnityTest]
        public IEnumerator Radial_ReducedMotion_SnapsAtOnce()
        {
            _panel = HudPanel.Create(reduceMotion: true);
            var radial = new RadialProgress { SmoothSeconds = 5f };
            _panel.Root.Add(radial);
            yield return HudPanel.Frames();
            radial.Value = 1f;
            Assert.IsFalse(radial.Step(0f), "no animation under reduced motion");
        }

        [UnityTest]
        public IEnumerator Radial_Repaint_AllocatesNothingInSteadyState()
        {
            _panel = HudPanel.Create();
            var radial = new RadialProgress { SmoothSeconds = 1f };
            _panel.Root.Add(radial);
            yield return HudPanel.Frames();
            if (!AllocProbe.IsAvailable()) Assert.Ignore("The runtime does not report per-thread allocations.");
            radial.Value = 0.5f;
            radial.Step(2f);
            Assert.AreEqual(0L, AllocProbe.Measure(() => radial.Value = 0.5f), "same value");
            var v = 0.2f;
            Assert.AreEqual(0L, AllocProbe.Measure(() =>
            {
                v = v > 0.9f ? 0.1f : v + 0.01f;
                radial.Value = v;
                radial.Step(0.016f);
            }), "moving value (repaint is marked, painting happens on the render thread of the panel)");
        }

        [UnityTest]
        public IEnumerator Cooldown_StartsRunsAndBecomesReady()
        {
            _panel = HudPanel.Create();
            var icon = new CooldownIcon();
            _panel.Root.Add(icon);
            yield return HudPanel.Frames();
            var ready = 0;
            icon.Ready += () => ready++;

            Assert.IsTrue(icon.IsReady);
            Assert.IsTrue(icon.ClassListContains("fdp-cooldown-icon--ready"));

            icon.StartCooldown(12f);
            Assert.IsFalse(icon.IsReady);
            Assert.IsFalse(icon.ClassListContains("fdp-cooldown-icon--ready"));
            Assert.AreEqual("12", icon.Q<Label>("remaining").text);

            icon.Step(3f);
            Assert.AreEqual("9.0", icon.Q<Label>("remaining").text);

            icon.Step(100f);
            Assert.IsTrue(icon.IsReady);
            Assert.AreEqual(1, ready);
            Assert.AreEqual(string.Empty, icon.Q<Label>("remaining").text);
            Assert.IsTrue(icon.ClassListContains("fdp-cooldown-icon--ready"));
        }

        [UnityTest]
        public IEnumerator Cooldown_ResetAndReduce_RaiseReady()
        {
            _panel = HudPanel.Create();
            var icon = new CooldownIcon();
            _panel.Root.Add(icon);
            yield return HudPanel.Frames();
            var ready = 0;
            icon.Ready += () => ready++;

            icon.StartCooldown(10f);
            icon.ReduceCooldown(4f);
            Assert.AreEqual(6f, icon.RemainingSeconds, 0.001f);
            icon.ReduceCooldown(10f);
            Assert.AreEqual(1, ready);
            icon.StartCooldown(10f);
            icon.ResetCooldown();
            Assert.AreEqual(2, ready);
            icon.ResetCooldown();
            Assert.AreEqual(2, ready, "no edge when already ready");
        }

        [UnityTest]
        public IEnumerator Cooldown_FlashesOnReady_ButNotUnderReducedMotion()
        {
            _panel = HudPanel.Create();
            var icon = new CooldownIcon();
            _panel.Root.Add(icon);
            yield return HudPanel.Frames();
            icon.StartCooldown(1f);
            icon.Step(2f);
            Assert.IsTrue(icon.ClassListContains("fdp-cooldown-icon--flash"));
            yield return HudPanel.WaitSeconds(0.4f);
            Assert.IsFalse(icon.ClassListContains("fdp-cooldown-icon--flash"), "the flash ends by itself");
            _panel.Dispose();

            _panel = HudPanel.Create(reduceMotion: true);
            icon = new CooldownIcon();
            _panel.Root.Add(icon);
            yield return HudPanel.Frames();
            icon.StartCooldown(1f);
            icon.Step(2f);
            Assert.IsFalse(icon.ClassListContains("fdp-cooldown-icon--flash"));
        }

        [UnityTest]
        public IEnumerator Cooldown_ShowTextFalse_WritesNoText()
        {
            _panel = HudPanel.Create();
            var icon = new CooldownIcon { ShowText = false };
            _panel.Root.Add(icon);
            yield return HudPanel.Frames();
            icon.StartCooldown(20f);
            Assert.AreEqual(string.Empty, icon.Q<Label>("remaining").text);
        }

        [UnityTest]
        public IEnumerator Cooldown_RunsFromTheScheduler()
        {
            _panel = HudPanel.Create();
            var icon = new CooldownIcon();
            _panel.Root.Add(icon);
            yield return HudPanel.Frames();
            icon.StartCooldown(0.3f);
            yield return HudPanel.WaitSeconds(0.8f);
            Assert.IsTrue(icon.IsReady);
        }
    }
}
