using FutureDigitalPlus.UIKit.Foundation;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.Localization.Settings;
using UnityEngine.Localization.Tables;

namespace FutureDigitalPlus.UIKit.Language.Tests
{
    /// <summary>
    /// The Unity Localization adapter, against in-memory string tables (no Localization settings asset needed: a table handed to
    /// <see cref="UnityLocalizationBackend.Preload(string, StringTable)"/> is used as it is). This assembly only exists when the package is installed.
    /// </summary>
    public class UnityLocalizationBackendTests
    {
        private StringTable _table;

        [SetUp]
        public void SetUp()
        {
            _table = ScriptableObject.CreateInstance<StringTable>();
            _table.SharedData = ScriptableObject.CreateInstance<SharedTableData>();
            _table.SharedData.TableCollectionName = "UI";
            AddPlain("PLAY", "Play");
            AddPlain("EMPTY", string.Empty);
        }

        [TearDown]
        public void TearDown()
        {
            if (_table != null)
            {
                if (_table.SharedData != null) Object.DestroyImmediate(_table.SharedData);
                Object.DestroyImmediate(_table);
            }
        }

        private void AddPlain(string key, string value)
        {
            var entry = _table.AddEntry(key, value);
            entry.IsSmart = false;
        }

        private static void IgnoreWhenTheProjectHasLocalizationSettings()
        {
            if (LocalizationSettings.HasSettings) Assert.Ignore("The project has Localization settings; this test needs the plain, settings-free path.");
        }

        [Test]
        public void APreloadedTableAnswersLookups()
        {
            IgnoreWhenTheProjectHasLocalizationSettings();
            var backend = new UnityLocalizationBackend();
            backend.Preload("UI", _table);

            Assert.AreEqual(LookupStatus.Found, backend.TryGet("UI", "PLAY", null, out var text));
            Assert.AreEqual("Play", text);
        }

        [Test]
        public void AnUnknownKeyAndAnEmptyValueAreMissingEntries()
        {
            IgnoreWhenTheProjectHasLocalizationSettings();
            var backend = new UnityLocalizationBackend();
            backend.Preload("UI", _table);

            Assert.AreEqual(LookupStatus.EntryMissing, backend.TryGet("UI", "NOPE", null, out var text));
            Assert.IsNull(text);
            Assert.AreEqual(LookupStatus.EntryMissing, backend.TryGet("UI", "EMPTY", null, out text));
        }

        [Test]
        public void ATableThatWasNeverLoaded_IsMissing_WithoutSettings_AndNothingThrows()
        {
            IgnoreWhenTheProjectHasLocalizationSettings();
            var backend = new UnityLocalizationBackend();

            Assert.AreEqual(LookupStatus.TableMissing, backend.TryGet("Nope", "PLAY", null, out var text));
            Assert.IsNull(text);
            Assert.AreEqual("en", backend.CurrentLocale);
        }

        [Test]
        public void ThroughUiLocalization_ALookupFindsTheText_AndAMissingOneShowsItsFallback()
        {
            IgnoreWhenTheProjectHasLocalizationSettings();
            var backend = new UnityLocalizationBackend();
            backend.Preload("UI", _table);
            var localization = new UiLocalization(backend, new[] { "UI" }) { LogMissingTables = false };

            Assert.AreEqual("Play", localization.Resolve(UiText.FromKey("PLAY")));
            Assert.AreEqual("Fb", localization.Resolve(UiText.FromKey("NOPE").WithFallback("Fb")));
            Assert.AreEqual("[NOPE]", localization.Resolve(UiText.FromKey("NOPE")));
        }
    }
}
