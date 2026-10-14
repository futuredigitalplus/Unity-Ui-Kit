using System;
using System.Collections.Generic;
using FutureDigitalPlus.UIKit.Foundation;
using UnityEngine;
using UnityEngine.Localization;
using UnityEngine.Localization.Metadata;
using UnityEngine.Localization.Settings;
using UnityEngine.Localization.Tables;
using UnityEngine.ResourceManagement.AsyncOperations;

namespace FutureDigitalPlus.UIKit.Language
{
    /// <summary>
    /// The Unity Localization side of <see cref="ILocalizationBackend"/>. It reads entries from the string tables of the selected
    /// locale, loads each table once (asynchronously; on platforms other than WebGL a first lookup of a table that is not loaded
    /// yet waits for it, see <see cref="SynchronousFirstLoad"/>), answers <see cref="LookupStatus.TableMissing"/> while a table is
    /// loading and raises <see cref="Changed"/> when it arrived, and reloads the tables it used when the selected locale changes.
    /// An entry the selected locale's table lacks (or has empty) comes from the locale's fallbacks, as Unity Localization's own
    /// <c>LocalizedString</c> does when the string database's Use Fallback is on: es-AR, then es, then en, each locale once (see
    /// <see cref="LocaleFallbackLookup"/>). An entry that cannot be formatted (a malformed Smart String or <c>{0}</c> pattern, wrong
    /// arguments) is logged once and treated as missing; it never makes its whole table missing.
    /// It never throws; without a Localization settings asset every lookup reports a missing table.
    /// This assembly only exists when the <c>com.unity.localization</c> package is installed.
    /// </summary>
    public sealed class UnityLocalizationBackend : ILocalizationBackend
    {
        private readonly Dictionary<string, StringTable> _loaded = new Dictionary<string, StringTable>(StringComparer.Ordinal);
        private readonly HashSet<string> _loading = new HashSet<string>(StringComparer.Ordinal);
        private readonly HashSet<string> _used = new HashSet<string>(StringComparer.Ordinal);
        private readonly Dictionary<string, StringTable> _fallbackTables = new Dictionary<string, StringTable>(StringComparer.Ordinal);
        private readonly HashSet<string> _fallbackLoading = new HashSet<string>(StringComparer.Ordinal);
        private readonly HashSet<string> _reportedFormatFailures = new HashSet<string>(StringComparer.Ordinal);
        private bool _subscribed;
        private int _generation;

        /// <summary>
        /// Creates the backend and starts following the selected locale when a Localization settings asset exists.
        /// </summary>
        public UnityLocalizationBackend()
        {
            Subscribe();
        }

        /// <inheritdoc />
        public event Action Changed;

        /// <summary>
        /// When true (the default) the first lookup of a table that is not loaded yet blocks until it is, which avoids showing a
        /// placeholder for one frame. Ignored on WebGL, where blocking is not allowed and a lookup answers
        /// <see cref="LookupStatus.TableMissing"/> until <see cref="Changed"/>.
        /// </summary>
        public bool SynchronousFirstLoad { get; set; } = true;

        /// <inheritdoc />
        public string CurrentLocale
        {
            get
            {
                try
                {
                    if (LocaleOverride != null) return LocaleOverride.Identifier.Code;
                    if (!LocalizationSettings.HasSettings) return "en";

                    var locale = LocalizationSettings.SelectedLocale;
                    return locale == null ? "en" : locale.Identifier.Code;
                }
                catch (Exception)
                {
                    return "en";
                }
            }
        }

        /// <summary>
        /// Hands the backend a table that is already loaded (a test, a table the game loaded itself). It is used for lookups of
        /// <paramref name="name"/> until the selected locale changes.
        /// </summary>
        /// <param name="name">The table collection name.</param>
        /// <param name="table">The table of the selected locale.</param>
        public void Preload(string name, StringTable table)
        {
            if (string.IsNullOrEmpty(name) || table == null) return;

            _loaded[name] = table;
            _used.Add(name);
        }

        /// <summary>
        /// Hands the backend a table of <paramref name="locale"/> that is already loaded. A table of the selected locale (or of no
        /// locale) is used as <see cref="Preload(string, StringTable)"/> uses it; a table of another locale is read when a lookup follows
        /// the fallback chain to that locale. Used until the selected locale changes.
        /// </summary>
        /// <param name="name">The table collection name.</param>
        /// <param name="table">The table of <paramref name="locale"/>.</param>
        /// <param name="locale">The locale the table belongs to; null means the selected locale.</param>
        public void Preload(string name, StringTable table, Locale locale)
        {
            if (locale == null || locale == SelectedLocale())
            {
                Preload(name, table);
                return;
            }

            if (string.IsNullOrEmpty(name) || table == null) return;

            _fallbackTables[FallbackKey(locale, name)] = table;
            _used.Add(name);
        }

        /// <summary>
        /// The locale lookups read and whose fallback chain they follow, instead of the selected locale of the Localization settings.
        /// For tests without a settings asset; null (the default) follows the settings.
        /// </summary>
        internal Locale LocaleOverride { get; set; }

        /// <inheritdoc />
        public LookupStatus TryGet(string table, string key, object[] args, out string text)
        {
            text = null;
            StringTable selectedTable;
            Locale selected;
            try
            {
                Subscribe();
                _used.Add(table);
                selected = SelectedLocale();

                // A table handed in with Preload is used even when the project has no Localization settings (tests, prototypes).
                if (!_loaded.TryGetValue(table, out selectedTable))
                {
                    if (!LocalizationSettings.HasSettings) return LookupStatus.TableMissing;

                    selectedTable = TryLoad(table);
                    if (selectedTable == null) return LookupStatus.TableMissing;
                }
            }
            catch (Exception)
            {
                // Only a table that could not be loaded is a missing table: the resolver stops asking it until the locale changes.
                return LookupStatus.TableMissing;
            }

            try
            {
                // The selected locale's table, then the tables of its fallback locales (es-AR, es, en), as Unity Localization does.
                // An entry that is missing, empty or cannot be formatted passes the key on; it never makes the table missing.
                return LocaleFallbackLookup.Find(
                    selected,
                    UseFallback() ? FallbacksOf : (Func<Locale, IEnumerable<Locale>>)null,
                    (Locale locale, out string value) =>
                        ReadEntry(locale == selected ? selectedTable : FallbackTable(table, locale), table, key, args, out value),
                    out text);
            }
            catch (Exception)
            {
                text = null;
                return LookupStatus.EntryMissing;
            }
        }

        // One entry of one table. A formatting failure (a malformed Smart String or {0} pattern, too few or wrong arguments) is this
        // entry's problem: it is logged once and the entry counts as missing, so the fallback locale's text or the text given in code
        // shows instead. Its raw pattern is not shown: it would put markup such as {0:plural:...} on screen.
        private LookupStatus ReadEntry(StringTable stringTable, string table, string key, object[] args, out string text)
        {
            text = null;
            if (stringTable == null) return LookupStatus.EntryMissing;

            var entry = stringTable.GetEntry(key);
            if (entry == null) return LookupStatus.EntryMissing;

            var hasArgs = args != null && args.Length > 0;
            string value;
            if (entry.IsSmart || hasArgs)
            {
                try
                {
                    value = hasArgs ? entry.GetLocalizedString(args) : entry.GetLocalizedString();
                }
                catch (Exception ex)
                {
                    ReportFormatFailure(stringTable, table, key, ex);
                    return LookupStatus.EntryMissing;
                }
            }
            else
            {
                value = entry.Value;
            }

            if (string.IsNullOrEmpty(value)) return LookupStatus.EntryMissing;

            text = value;
            return LookupStatus.Found;
        }

        /// <summary>Formatting failures seen so far, one per locale, table and key; each is logged once.</summary>
        internal int FormatFailureCount => _reportedFormatFailures.Count;

        private void ReportFormatFailure(StringTable stringTable, string table, string key, Exception ex)
        {
            var code = LocaleCode(stringTable);
            if (!_reportedFormatFailures.Add(code + "\n" + table + "\n" + key)) return;

            Debug.LogWarning("[UnityLocalizationBackend] Entry '" + key + "' of table '" + table + "' (" + code + ") could not be formatted ("
                             + ex.GetType().Name + ": " + ex.Message + "). The fallback locale's text or the text given in code shows instead.");
        }

        private static string LocaleCode(StringTable stringTable)
        {
            try
            {
                var code = stringTable.LocaleIdentifier.Code;
                return string.IsNullOrEmpty(code) ? "?" : code;
            }
            catch (Exception)
            {
                return "?";
            }
        }

        // The selected locale for the fallback walk, without waiting for it (WebGL cannot wait): null until it is known, and then the
        // selected locale's table alone answers.
        private Locale SelectedLocale()
        {
            if (LocaleOverride != null) return LocaleOverride;

            try
            {
                if (!LocalizationSettings.HasSettings) return null;

                var selected = LocalizationSettings.SelectedLocaleAsync;
                return selected.IsDone ? selected.Result : null;
            }
            catch (Exception)
            {
                return null;
            }
        }

        // The project's "Use Fallback" of the string database, which LocalizedString honours too. Without settings (tables handed in
        // with Preload) the fallbacks the locales declare are followed.
        private static bool UseFallback()
        {
            try
            {
                return !LocalizationSettings.HasSettings || LocalizationSettings.StringDatabase.UseFallback;
            }
            catch (Exception)
            {
                return false;
            }
        }

        // The FallbackLocale metadata of the locale, in order. With Localization settings, Locale.GetFallbacks also covers a locale
        // without that metadata (its culture's parent, en-GB to en, when that locale exists). LocaleFallbackLookup guards against cycles.
        private static IEnumerable<Locale> FallbacksOf(Locale locale)
        {
            var fallbacks = new List<Locale>();
            if (locale == null) return fallbacks;

            if (LocalizationSettings.HasSettings)
            {
                foreach (var fallback in locale.GetFallbacks())
                    if (fallback != null) fallbacks.Add(fallback);
                return fallbacks;
            }

            var entries = locale.Metadata?.MetadataEntries;
            if (entries == null) return fallbacks;
            for (var i = 0; i < entries.Count; i++)
                if (entries[i] is FallbackLocale fallback && fallback.Locale != null) fallbacks.Add(fallback.Locale);
            return fallbacks;
        }

        private static string FallbackKey(Locale locale, string name) => locale.Identifier.Code + "\n" + name;

        // The table of a fallback locale: handed in with Preload, already loaded, or loaded now (synchronously like the selected
        // locale's tables; on WebGL it is missing until it arrived and Changed was raised). Null when the locale has no such table.
        private StringTable FallbackTable(string name, Locale locale)
        {
            if (locale == null) return null;

            var cacheKey = FallbackKey(locale, name);
            if (_fallbackTables.TryGetValue(cacheKey, out var cached)) return cached;
            if (!LocalizationSettings.HasSettings || _fallbackLoading.Contains(cacheKey)) return null;

            var handle = LocalizationSettings.StringDatabase.GetTableAsync(name, locale);

#if !UNITY_WEBGL
            if (SynchronousFirstLoad && !handle.IsDone) handle.WaitForCompletion();
#endif

            if (handle.IsDone)
            {
                var loaded = handle.Status == AsyncOperationStatus.Succeeded ? handle.Result : null;
                _fallbackTables[cacheKey] = loaded; // null too: a locale without this table is not asked to load it again
                return loaded;
            }

            _fallbackLoading.Add(cacheKey);
            var generation = _generation;
            handle.Completed += done =>
            {
                if (generation != _generation) return;

                _fallbackLoading.Remove(cacheKey);
                var arrived = done.Status == AsyncOperationStatus.Succeeded ? done.Result : null;
                _fallbackTables[cacheKey] = arrived;
                if (arrived != null) Changed?.Invoke();
            };
            return null;
        }

        private StringTable TryLoad(string name)
        {
            var handle = LocalizationSettings.StringDatabase.GetTableAsync(name);

#if !UNITY_WEBGL
            if (SynchronousFirstLoad && !handle.IsDone) handle.WaitForCompletion();
#endif

            if (handle.IsDone)
            {
                if (handle.Status != AsyncOperationStatus.Succeeded || handle.Result == null) return null;

                _loaded[name] = handle.Result;
                return handle.Result;
            }

            if (_loading.Add(name))
            {
                var generation = _generation;
                handle.Completed += done =>
                {
                    _loading.Remove(name);
                    if (generation != _generation || done.Status != AsyncOperationStatus.Succeeded || done.Result == null) return;

                    _loaded[name] = done.Result;
                    Changed?.Invoke();
                };
            }

            return null;
        }

        private void Subscribe()
        {
            if (_subscribed) return;

            try
            {
                if (!LocalizationSettings.HasSettings) return;

                LocalizationSettings.SelectedLocaleChanged += OnLocaleChanged;
                _subscribed = true;

                var init = LocalizationSettings.InitializationOperation;
                if (!init.IsDone) init.Completed += _ => Changed?.Invoke();
            }
            catch (Exception ex)
            {
                Debug.LogWarning("[UnityLocalizationBackend] Could not follow the selected locale: " + ex.Message);
            }
        }

        private void OnLocaleChanged(Locale locale)
        {
            // The loaded tables belong to the old locale. Reload the ones the UI used, then tell the UI once.
            _generation++;
            _loaded.Clear();
            _loading.Clear();
            _fallbackTables.Clear();
            _fallbackLoading.Clear();

            var wanted = new List<string>(_used);
            if (wanted.Count == 0)
            {
                Changed?.Invoke();
                return;
            }

            var pending = wanted.Count;
            var generation = _generation;
            foreach (var name in wanted)
            {
                var tableName = name;
                var handle = LocalizationSettings.StringDatabase.GetTableAsync(tableName);
                handle.Completed += done =>
                {
                    if (generation != _generation) return;

                    if (done.Status == AsyncOperationStatus.Succeeded && done.Result != null) _loaded[tableName] = done.Result;
                    if (--pending == 0) Changed?.Invoke();
                };
            }
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Register()
        {
            // A game that assigned its own backend first keeps it; this only fills the empty default.
            if (!(UiLocalization.Default.Backend is NullLocalizationBackend)) return;

            try
            {
                if (LocalizationSettings.HasSettings) UiLocalization.Default.Backend = new UnityLocalizationBackend();
            }
            catch (Exception)
            {
                // No settings asset in this build: the null backend stays and labels show their fallbacks.
            }
        }
    }
}
