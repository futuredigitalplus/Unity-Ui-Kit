using System;
using FutureDigitalPlus.UIKit.Foundation;
using UnityEngine;

namespace FutureDigitalPlus.UIKit.Language
{
    /// <summary>
    /// The one place UI code turns a <see cref="UiText"/> into a string. It owns a <see cref="LocalizationKeyResolver"/> over an
    /// <see cref="ILocalizationBackend"/>, tells bound elements when the language changed (<see cref="Changed"/>), and works with
    /// no localization package set up: the backend defaults to <see cref="NullLocalizationBackend"/>, so every lookup returns the
    /// fallback of the <see cref="UiText"/> or a visible <c>[KEY]</c> placeholder and nothing throws. Main thread only.
    /// <code>
    /// UiLocalization.Default.Backend = myBackend;                 // or the Unity Localization adapter registers itself
    /// label.BindText(UiText.FromKey("HOME-PLAY").WithFallback("Play"));
    /// </code>
    /// A game with two independent localizations (an in-game store window, a world-space panel) can create its own instance.
    /// </summary>
    public sealed class UiLocalization
    {
        private static UiLocalization s_default;

        private ILocalizationBackend _backend = NullLocalizationBackend.Instance;
        private LocalizationKeyResolver _resolver;
        private string[] _defaultTables;

        /// <summary>The shared instance most games use.</summary>
        public static UiLocalization Default => s_default ??= new UiLocalization();

        /// <summary>Creates an instance over <paramref name="backend"/> (null means <see cref="NullLocalizationBackend"/>).</summary>
        /// <param name="backend">Where the text comes from.</param>
        /// <param name="defaultTables">The tables tried, in order, for a key that names none; null uses the resolver's default list.</param>
        public UiLocalization(ILocalizationBackend backend = null, string[] defaultTables = null)
        {
            _defaultTables = defaultTables;
            Backend = backend;
        }

        /// <summary>Raised after the language changed or the backend finished loading; bound elements resolve their text again.</summary>
        public event Action Changed;

        /// <summary>Raised once per table the first time it is found missing (after the last change), so a game can log it. The default logs one warning.</summary>
        public event Action<string> TableMissing;

        /// <summary>Whether a table that is not available logs one warning per change of language. Default true.</summary>
        public bool LogMissingTables { get; set; } = true;

        /// <summary>
        /// The text source. Setting it moves the change subscription, forgets which tables were missing and raises
        /// <see cref="Changed"/>. Null means <see cref="NullLocalizationBackend"/>.
        /// </summary>
        public ILocalizationBackend Backend
        {
            get => _backend;
            set
            {
                var next = value ?? NullLocalizationBackend.Instance;
                if (ReferenceEquals(next, _backend)) return;

                _backend.Changed -= OnBackendChanged;
                _backend = next;
                _backend.Changed += OnBackendChanged;
                NotifyChanged();
            }
        }

        /// <summary>
        /// The tables tried, in order, for a key that names none. Null (the default) uses the resolver's default list. Setting it
        /// rebuilds the resolver.
        /// </summary>
        public string[] DefaultTables
        {
            get => _defaultTables;
            set
            {
                _defaultTables = value;
                _resolver = null;
                NotifyChanged();
            }
        }

        /// <summary>The code of the active language of the backend, for example <c>en</c> or <c>ar</c>. Never null.</summary>
        public string CurrentLocale
        {
            get
            {
                try
                {
                    return _backend.CurrentLocale ?? "en";
                }
                catch (Exception)
                {
                    return "en";
                }
            }
        }

        /// <summary>The resolver; created on first use.</summary>
        public LocalizationKeyResolver Resolver
        {
            get
            {
                if (_resolver != null) return _resolver;

                _resolver = new LocalizationKeyResolver(Lookup, _defaultTables);
                _resolver.TableMissing += OnTableMissing;
                return _resolver;
            }
        }

        /// <summary>The display string for <paramref name="text"/>. Never null, never throws.</summary>
        public string Resolve(UiText text) => Resolver.Resolve(text);

        /// <summary>Forgets cached table failures and raises <see cref="Changed"/>. Call it when the backend finished initialising by a route it does not signal.</summary>
        public void NotifyChanged()
        {
            _resolver?.ResetMissing();
            Changed?.Invoke();
        }

        private void OnBackendChanged() => NotifyChanged();

        private LookupStatus Lookup(string table, string key, object[] args, out string text)
        {
            text = null;
            try
            {
                return _backend.TryGet(table, key, args, out text);
            }
            catch (Exception)
            {
                text = null;
                return LookupStatus.TableMissing;
            }
        }

        private void OnTableMissing(string table)
        {
            TableMissing?.Invoke(table);

            if (LogMissingTables)
                Debug.LogWarning("[UiLocalization] String table '" + table + "' is not available; UI text falls back to the texts given in code or to [KEY] placeholders.");
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics() => s_default = null;
    }
}
