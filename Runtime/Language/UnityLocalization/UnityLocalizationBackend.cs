using System;
using System.Collections.Generic;
using FutureDigitalPlus.UIKit.Foundation;
using UnityEngine;
using UnityEngine.Localization;
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
    /// It never throws; without a Localization settings asset every lookup reports a missing table.
    /// This assembly only exists when the <c>com.unity.localization</c> package is installed.
    /// </summary>
    public sealed class UnityLocalizationBackend : ILocalizationBackend
    {
        private readonly Dictionary<string, StringTable> _loaded = new Dictionary<string, StringTable>(StringComparer.Ordinal);
        private readonly HashSet<string> _loading = new HashSet<string>(StringComparer.Ordinal);
        private readonly HashSet<string> _used = new HashSet<string>(StringComparer.Ordinal);
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

        /// <inheritdoc />
        public LookupStatus TryGet(string table, string key, object[] args, out string text)
        {
            text = null;
            try
            {
                Subscribe();
                _used.Add(table);

                // A table handed in with Preload is used even when the project has no Localization settings (tests, prototypes).
                if (!_loaded.TryGetValue(table, out var stringTable))
                {
                    if (!LocalizationSettings.HasSettings) return LookupStatus.TableMissing;

                    stringTable = TryLoad(table);
                    if (stringTable == null) return LookupStatus.TableMissing;
                }

                var entry = stringTable.GetEntry(key);
                if (entry == null) return LookupStatus.EntryMissing;

                var hasArgs = args != null && args.Length > 0;
                var value = entry.IsSmart || hasArgs
                    ? (hasArgs ? entry.GetLocalizedString(args) : entry.GetLocalizedString())
                    : entry.Value;
                if (string.IsNullOrEmpty(value)) return LookupStatus.EntryMissing;

                text = value;
                return LookupStatus.Found;
            }
            catch (Exception)
            {
                return LookupStatus.TableMissing;
            }
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
