using System;
using System.Collections;
using System.Globalization;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UIElements;

namespace FutureDigitalPlus.UIKit.Hud.Tests
{
    public class CounterTests
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
        public IEnumerator CountUp_BeforeItIsOnAPanel_SnapsToTheValue()
        {
            _panel = HudPanel.Create();
            var label = new CountUpLabel { Value = 1500 };
            Assert.AreEqual("1,500", label.text);
            _panel.Root.Add(label);
            yield return HudPanel.Frames();
            Assert.IsFalse(label.Step(0f));
        }

        [UnityTest]
        public IEnumerator CountUp_RollsToTheNewValue()
        {
            _panel = HudPanel.Create();
            var label = new CountUpLabel { DurationSeconds = 1f, Culture = CultureInfo.InvariantCulture };
            _panel.Root.Add(label);
            yield return HudPanel.Frames();

            label.Value = 1000;
            Assert.AreEqual("0", label.text, "starts from the old number");
            Assert.IsTrue(label.Step(0.5f));
            Assert.Greater(label.DisplayedValue, 0d);
            Assert.Less(label.DisplayedValue, 1000d);
            Assert.IsFalse(label.Step(5f));
            Assert.AreEqual("1,000", label.text);
        }

        [UnityTest]
        public IEnumerator CountUp_PrefixSuffixAndFormat()
        {
            _panel = HudPanel.Create();
            var label = new CountUpLabel { Prefix = "x", Suffix = " pts", Format = NumberFormatStyle.Compact };
            _panel.Root.Add(label);
            yield return HudPanel.Frames();
            label.SnapTo(1234567);
            Assert.AreEqual("x1.2M pts", label.text);
        }

        [UnityTest]
        public IEnumerator CountUp_UsesTheInjectedCulture()
        {
            _panel = HudPanel.Create();
            var label = new CountUpLabel { Culture = new CultureInfo("de-DE") };
            _panel.Root.Add(label);
            yield return HudPanel.Frames();
            label.SnapTo(1234567);
            Assert.AreEqual("1.234.567", label.text);
        }

        [UnityTest]
        public IEnumerator CountUp_ReducedMotion_ChangesAtOnce()
        {
            _panel = HudPanel.Create(reduceMotion: true);
            var label = new CountUpLabel { DurationSeconds = 5f };
            _panel.Root.Add(label);
            yield return HudPanel.Frames();
            label.Value = 42;
            Assert.AreEqual("42", label.text);
        }

        [UnityTest]
        public IEnumerator CountUp_RunsFromTheSchedulerWhileTheGameIsPaused()
        {
            _panel = HudPanel.Create();
            var label = new CountUpLabel { DurationSeconds = 0.2f };
            _panel.Root.Add(label);
            yield return HudPanel.Frames();
            var scale = Time.timeScale;
            Time.timeScale = 0f;
            try
            {
                label.Value = 100;
                yield return HudPanel.WaitSeconds(0.6f);
                Assert.AreEqual("100", label.text);
            }
            finally
            {
                Time.timeScale = scale;
            }
        }

        [UnityTest]
        public IEnumerator Currency_ShowsTheChangeNote_ThenHidesIt()
        {
            _panel = HudPanel.Create();
            var currency = new CurrencyLabel { DeltaMilliseconds = 80, DurationSeconds = 0f };
            _panel.Root.Add(currency);
            yield return HudPanel.Frames();

            var delta = currency.Q<Label>("delta");
            currency.Value = 25;
            Assert.AreEqual("+25", delta.text);
            Assert.IsTrue(delta.ClassListContains("fdp-currency-label__delta--show"));
            Assert.IsTrue(delta.ClassListContains("fdp-currency-label__delta--gain"));
            Assert.AreEqual("25", currency.Q<CountUpLabel>("count").text);

            yield return HudPanel.WaitSeconds(0.4f);
            Assert.IsFalse(delta.ClassListContains("fdp-currency-label__delta--show"));

            currency.Value = 15;
            Assert.AreEqual("-10", delta.text);
            Assert.IsTrue(delta.ClassListContains("fdp-currency-label__delta--loss"));
            Assert.IsFalse(delta.ClassListContains("fdp-currency-label__delta--gain"));
        }

        [UnityTest]
        public IEnumerator Currency_SnapToAndShowDeltaFalse_AreSilent()
        {
            _panel = HudPanel.Create();
            var currency = new CurrencyLabel { ShowDelta = false };
            _panel.Root.Add(currency);
            yield return HudPanel.Frames();
            currency.Value = 10;
            Assert.IsFalse(currency.Q<Label>("delta").ClassListContains("fdp-currency-label__delta--show"));
            currency.ShowDelta = true;
            currency.SnapTo(500);
            Assert.IsFalse(currency.Q<Label>("delta").ClassListContains("fdp-currency-label__delta--show"));
            Assert.AreEqual("500", currency.Q<CountUpLabel>("count").text);
        }

        [UnityTest]
        public IEnumerator Countdown_CountsDownOnTheInjectedClock_AndExpiresOnce()
        {
            _panel = HudPanel.Create();
            double now = 100d;
            var label = new CountdownLabel { Clock = () => now };
            var expired = 0;
            label.Expired += () => expired++;
            _panel.Root.Add(label);
            yield return HudPanel.Frames();

            label.StartIn(125d);
            Assert.AreEqual("02:05", label.text);
            now += 10d;
            label.Step(0f);
            Assert.AreEqual("01:55", label.text);
            Assert.IsTrue(label.IsRunning);

            now += 200d;
            label.Step(0f);
            Assert.AreEqual("0s", label.text);
            Assert.IsFalse(label.IsRunning);
            Assert.AreEqual(1, expired);
            label.Step(0f);
            Assert.AreEqual(1, expired, "raised once");
        }

        [UnityTest]
        public IEnumerator Countdown_ExpiredTextAndClockFormat()
        {
            _panel = HudPanel.Create();
            double now = 0d;
            var label = new CountdownLabel { Clock = () => now, ExpiredText = "Ended", Format = CountdownStyle.Clock };
            _panel.Root.Add(label);
            yield return HudPanel.Frames();
            label.StartIn(3725d);
            Assert.AreEqual("01:02:05", label.text);
            now += 4000d;
            label.Step(0f);
            Assert.AreEqual("Ended", label.text);
        }

        [UnityTest]
        public IEnumerator Countdown_StartInThePast_ExpiresImmediately()
        {
            _panel = HudPanel.Create();
            double now = 50d;
            var label = new CountdownLabel { Clock = () => now };
            var expired = 0;
            label.Expired += () => expired++;
            _panel.Root.Add(label);
            yield return HudPanel.Frames();
            label.StartAt(10d);
            Assert.AreEqual(1, expired);
        }

        [UnityTest]
        public IEnumerator Countdown_StopDoesNotRaiseExpired()
        {
            _panel = HudPanel.Create();
            double now = 0d;
            var label = new CountdownLabel { Clock = () => now };
            var expired = 0;
            label.Expired += () => expired++;
            _panel.Root.Add(label);
            yield return HudPanel.Frames();
            label.StartIn(5d);
            label.Stop();
            now += 10d;
            label.Step(0f);
            Assert.AreEqual(0, expired);
            Assert.AreEqual("0s", label.text);
        }

        [UnityTest]
        public IEnumerator Countdown_RunsFromTheScheduler()
        {
            _panel = HudPanel.Create();
            var label = new CountdownLabel();
            var expired = 0;
            label.Expired += () => expired++;
            _panel.Root.Add(label);
            yield return HudPanel.Frames();
            label.StartIn(0.4);
            yield return HudPanel.WaitSeconds(1.2f);
            Assert.AreEqual(1, expired);
        }

        [Test]
        public void Countdown_UtcSeconds_IsWallClockTime()
        {
            var expected = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            Assert.AreEqual(expected, CountdownLabel.UtcSeconds(), 2d);
        }
    }
}
