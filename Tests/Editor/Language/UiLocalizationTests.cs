using System;
using System.Text.RegularExpressions;
using FutureDigitalPlus.UIKit.Foundation;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace FutureDigitalPlus.UIKit.Language.Tests
{
    public class UiLocalizationTests
    {
        private sealed class ThrowingBackend : ILocalizationBackend
        {
            public string CurrentLocale => throw new InvalidOperationException("no locale");

            public event Action Changed;

            public LookupStatus TryGet(string table, string key, object[] args, out string text) =>
                throw new InvalidOperationException("no tables");

            public void Raise() => Changed?.Invoke();
        }

        [Test]
        public void WithoutABackend_TheFallbackOrAPlaceholderShows_AndNothingThrows()
        {
            var localization = new UiLocalization { LogMissingTables = false };

            Assert.AreEqual("Play", localization.Resolve(UiText.FromKey("HOME-PLAY").WithFallback("Play")));
            Assert.AreEqual("[HOME-PLAY]", localization.Resolve(UiText.FromKey("HOME-PLAY")));
            Assert.AreEqual("Already localized", localization.Resolve(UiText.FromLiteral("Already localized")));
            Assert.AreEqual(string.Empty, localization.Resolve(UiText.Empty));
            Assert.AreEqual(string.Empty, localization.Resolve(null));
            Assert.AreEqual("en", localization.CurrentLocale);
        }

        [Test]
        public void ABackendThatThrows_NeverReachesTheCaller()
        {
            var localization = new UiLocalization(new ThrowingBackend()) { LogMissingTables = false };

            Assert.AreEqual("Fb", localization.Resolve(UiText.FromKey("K").WithFallback("Fb")));
            Assert.AreEqual("[K]", localization.Resolve(UiText.FromKey("K")));
            Assert.AreEqual("en", localization.CurrentLocale, "a locale that throws reads as en");
        }

        [Test]
        public void ATextComesFromTheBackend()
        {
            var source = new DictionaryTextSource("de");
            source.Add("de", "UI", "PLAY", "Spielen");
            var localization = new UiLocalization(source, new[] { "UI" });

            Assert.AreEqual("Spielen", localization.Resolve(UiText.FromKey("PLAY")));
            Assert.AreEqual("Spielen", localization.Resolve(UiText.FromKey("UI:PLAY")));
            Assert.AreEqual("de", localization.CurrentLocale);
        }

        [Test]
        public void SettingTheBackend_RaisesChanged_AndMovesTheSubscription()
        {
            var first = new DictionaryTextSource("en");
            var second = new DictionaryTextSource("de");
            var localization = new UiLocalization(first) { LogMissingTables = false };
            var raised = 0;
            localization.Changed += () => raised++;

            first.NotifyChanged();
            Assert.AreEqual(1, raised, "the backend raises Changed");

            localization.Backend = first;
            Assert.AreEqual(1, raised, "the same backend again is not a change");

            localization.Backend = second;
            Assert.AreEqual(2, raised, "a new backend is a change");

            first.NotifyChanged();
            Assert.AreEqual(2, raised, "the old backend is not followed any more");
            second.SetLocale("fr");
            Assert.AreEqual(3, raised);

            localization.Backend = null;
            Assert.AreEqual(4, raised);
            Assert.IsInstanceOf<NullLocalizationBackend>(localization.Backend);
        }

        [Test]
        public void AMissingTableIsReportedOncePerChange()
        {
            var localization = new UiLocalization(new DictionaryTextSource(), new[] { "UI" });
            var reported = 0;
            localization.TableMissing += table => reported++;

            LogAssert.Expect(LogType.Warning, new Regex("String table 'UI' is not available"));
            localization.Resolve(UiText.FromKey("A"));
            localization.Resolve(UiText.FromKey("B"));
            Assert.AreEqual(1, reported, "asked twice, reported once");

            localization.NotifyChanged();
            LogAssert.Expect(LogType.Warning, new Regex("String table 'UI' is not available"));
            localization.Resolve(UiText.FromKey("A"));
            Assert.AreEqual(2, reported, "a change makes the next lookup report again");
        }

        [Test]
        public void LoggingCanBeSwitchedOff()
        {
            var localization = new UiLocalization(new DictionaryTextSource(), new[] { "UI" }) { LogMissingTables = false };
            localization.Resolve(UiText.FromKey("A"));
            LogAssert.NoUnexpectedReceived();
        }

        [Test]
        public void ChangingTheDefaultTables_RebuildsTheResolver_AndRaisesChanged()
        {
            var source = new DictionaryTextSource();
            source.Add("en", "Menu", "PLAY", "Play");
            var localization = new UiLocalization(source, new[] { "UI" }) { LogMissingTables = false };
            var raised = 0;
            localization.Changed += () => raised++;

            Assert.AreEqual("[PLAY]", localization.Resolve(UiText.FromKey("PLAY")));

            localization.DefaultTables = new[] { "Menu" };
            Assert.AreEqual(1, raised);
            Assert.AreEqual("Play", localization.Resolve(UiText.FromKey("PLAY")));
        }

        [Test]
        public void TwoInstancesAreIndependent()
        {
            var a = new DictionaryTextSource("en");
            a.Add("en", "UI", "K", "from a");
            var b = new DictionaryTextSource("en");
            b.Add("en", "UI", "K", "from b");

            var first = new UiLocalization(a, new[] { "UI" });
            var second = new UiLocalization(b, new[] { "UI" });

            Assert.AreEqual("from a", first.Resolve(UiText.FromKey("K")));
            Assert.AreEqual("from b", second.Resolve(UiText.FromKey("K")));
            Assert.AreNotSame(first, second);
        }

        [Test]
        public void TheDefaultInstanceIsOneObject_WithTheNullBackend()
        {
            Assert.AreSame(UiLocalization.Default, UiLocalization.Default);
            Assert.IsNotNull(UiLocalization.Default.Backend);
        }

        [Test]
        public void TheNullBackendNeverRaisesAndAlwaysReportsAMissingTable()
        {
            var backend = NullLocalizationBackend.Instance;
            Action handler = () => Assert.Fail("never raised");
            backend.Changed += handler;
            backend.Changed -= handler;

            Assert.AreEqual(LookupStatus.TableMissing, backend.TryGet("UI", "K", null, out var text));
            Assert.IsNull(text);
            Assert.AreEqual("en", backend.CurrentLocale);
        }
    }
}
