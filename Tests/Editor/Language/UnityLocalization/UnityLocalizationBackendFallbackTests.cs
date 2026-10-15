using System.Collections.Generic;
using FutureDigitalPlus.UIKit.Foundation;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.Localization;
using UnityEngine.Localization.Metadata;
using UnityEngine.Localization.Settings;
using UnityEngine.Localization.Tables;

namespace FutureDigitalPlus.UIKit.Language.Tests
{
    /// <summary>
    /// The adapter's locale fallbacks and its handling of an entry that cannot be formatted, against in-memory locales and tables
    /// handed in with <see cref="UnityLocalizationBackend.Preload(string, StringTable, Locale)"/>. The locales have private-use codes
    /// (x-kit-...) so no culture of the project is reached, and every locale of a chain has its table preloaded, so nothing is loaded.
    /// </summary>
    public class UnityLocalizationBackendFallbackTests
    {
        private const string Table = "KitFallbackTest";
        private readonly List<Object> _created = new List<Object>();
        private Locale _regional;
        private Locale _language;
        private Locale _english;

        [SetUp]
        public void SetUp()
        {
            _english = NewLocale("x-kit-en");
            _language = NewLocale("x-kit-es", _english);
            _regional = NewLocale("x-kit-es-ar", _language);
        }

        [TearDown]
        public void TearDown()
        {
            foreach (var created in _created)
                if (created != null) Object.DestroyImmediate(created);
            _created.Clear();
        }

        private Locale NewLocale(string code, Locale fallback = null)
        {
            var locale = Locale.CreateLocale(new LocaleIdentifier(code));
            if (fallback != null) locale.Metadata.AddMetadata(new FallbackLocale(fallback));
            _created.Add(locale);
            return locale;
        }

        private StringTable NewTable(params string[] keysAndValues)
        {
            var table = ScriptableObject.CreateInstance<StringTable>();
            table.SharedData = ScriptableObject.CreateInstance<SharedTableData>();
            table.SharedData.TableCollectionName = Table;
            _created.Add(table.SharedData);
            _created.Add(table);
            for (var i = 0; i + 1 < keysAndValues.Length; i += 2)
                table.AddEntry(keysAndValues[i], keysAndValues[i + 1]).IsSmart = false;
            return table;
        }

        private UnityLocalizationBackend NewBackend(StringTable regional, StringTable language, StringTable english)
        {
            var backend = new UnityLocalizationBackend { LocaleOverride = _regional };
            backend.Preload(Table, regional, _regional);
            backend.Preload(Table, language, _language);
            backend.Preload(Table, english, _english);
            return backend;
        }

        private static void IgnoreWhenTheProjectTurnedFallbacksOff()
        {
            if (LocalizationSettings.HasSettings && !LocalizationSettings.StringDatabase.UseFallback)
                Assert.Ignore("The project's string database has Use Fallback off; the backend then reads the selected locale only.");
        }

        [Test]
        public void AnEntryTheRegionalTableLacks_ComesFromItsLanguage_ThenFromEnglish()
        {
            IgnoreWhenTheProjectTurnedFallbacksOff();
            var backend = NewBackend(
                NewTable("HELLO", "Che, hola"),
                NewTable("HELLO", "Hola", "PLAY", "Jugar"),
                NewTable("HELLO", "Hello", "PLAY", "Play", "QUIT", "Quit"));

            Assert.AreEqual(LookupStatus.Found, backend.TryGet(Table, "HELLO", null, out var text));
            Assert.AreEqual("Che, hola", text);
            Assert.AreEqual(LookupStatus.Found, backend.TryGet(Table, "PLAY", null, out text));
            Assert.AreEqual("Jugar", text, "es-AR without the entry shows es, not English");
            Assert.AreEqual(LookupStatus.Found, backend.TryGet(Table, "QUIT", null, out text));
            Assert.AreEqual("Quit", text);
            Assert.AreEqual(LookupStatus.EntryMissing, backend.TryGet(Table, "NOPE", null, out text));
            Assert.IsNull(text);
            Assert.AreEqual("x-kit-es-ar", backend.CurrentLocale);
        }

        [Test]
        public void AnEmptyRegionalEntry_AlsoComesFromTheFallback()
        {
            IgnoreWhenTheProjectTurnedFallbacksOff();
            var backend = NewBackend(NewTable("PLAY", string.Empty), NewTable("PLAY", "Jugar"), NewTable("PLAY", "Play"));

            Assert.AreEqual(LookupStatus.Found, backend.TryGet(Table, "PLAY", null, out var text));
            Assert.AreEqual("Jugar", text);
        }

        [Test]
        public void ThroughUiLocalization_ARegionalLocaleShowsItsLanguage_NotTheCodeFallback()
        {
            IgnoreWhenTheProjectTurnedFallbacksOff();
            var backend = NewBackend(NewTable("HELLO", "Che, hola"), NewTable("PLAY", "Jugar"), NewTable("PLAY", "Play"));
            var localization = new UiLocalization(backend, new[] { Table }) { LogMissingTables = false };

            Assert.AreEqual("Jugar", localization.Resolve(UiText.FromKey("PLAY").WithFallback("Play (code)")));
            Assert.AreEqual("Che, hola", localization.Resolve(UiText.FromKey("HELLO")));
            Assert.IsEmpty(localization.Resolver.MissingTables);
        }

        [Test]
        public void AnEntryThatCannotBeFormatted_IsMissing_AndTheRestOfItsTableStillWorks()
        {
            // Formatting goes through LocalizationSettings, which would create a default settings object in a project without one
            // and change what the other tests see. The pure part (a failed entry passes the key on) is in LocaleFallbackLookupTests.
            if (!LocalizationSettings.HasSettings) Assert.Ignore("Formatting an entry needs the project's Localization settings.");
            IgnoreWhenTheProjectTurnedFallbacksOff();

            var backend = NewBackend(
                NewTable("SCORE", "Puntos {1}", "PLAY", "Jugar"), // {1} with one argument: string.Format throws
                NewTable("SCORE", "Puntos: {0}"),
                NewTable("SCORE", "Score: {0}"));
            var localization = new UiLocalization(backend, new[] { Table }) { LogMissingTables = false };

            Assert.AreEqual(LookupStatus.Found, backend.TryGet(Table, "SCORE", new object[] { 7 }, out var text));
            Assert.AreEqual("Puntos: 7", text);
            Assert.AreEqual("Puntos: 7", localization.Resolve(UiText.FromKey("SCORE").WithArgs(7)), "the fallback locale's entry formats");
            Assert.AreEqual("Jugar", localization.Resolve(UiText.FromKey("PLAY")), "one bad entry does not take its table down");
            Assert.IsEmpty(localization.Resolver.MissingTables);
            Assert.AreEqual(1, backend.FormatFailureCount);

            localization.Resolve(UiText.FromKey("SCORE").WithArgs(8));
            Assert.AreEqual(1, backend.FormatFailureCount, "each bad entry is logged once");
        }

        [Test]
        public void AnEntryThatCannotBeFormattedAnywhere_ShowsTheCodeFallback()
        {
            if (!LocalizationSettings.HasSettings) Assert.Ignore("Formatting an entry needs the project's Localization settings.");
            IgnoreWhenTheProjectTurnedFallbacksOff();

            var backend = NewBackend(NewTable("SCORE", "Puntos {1}"), NewTable("SCORE", "Puntos {2}"), NewTable("SCORE", "Score {3}"));
            var localization = new UiLocalization(backend, new[] { Table }) { LogMissingTables = false };

            Assert.AreEqual("Score 7", localization.Resolve(UiText.FromKey("SCORE").WithArgs(7).WithFallback("Score {0}")));
            Assert.IsEmpty(localization.Resolver.MissingTables);
        }
    }
}
