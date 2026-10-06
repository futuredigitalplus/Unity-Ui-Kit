using System;
using System.Collections.Generic;
using FutureDigitalPlus.UIKit.DesignSystem.Theme.Data;
using UnityEngine;
using UnityEngine.UIElements;

namespace FutureDigitalPlus.UIKit.DesignSystem.Theme.Applier
{
    /// <summary>
    /// Where the OS or the game says whether the user prefers a dark or a light look. Unity has no API for the system
    /// dark-mode setting, so the kit defines the seam and a game plugs a native query (or a settings screen) into it.
    /// </summary>
    public interface IThemeModeSource
    {
        /// <summary>True when dark is preferred, false when light is, null when there is no preference.</summary>
        bool? PrefersDark { get; }

        /// <summary>Raised when <see cref="PrefersDark"/> may have changed.</summary>
        event Action Changed;
    }

    /// <summary>
    /// A named set of themes and the roots that wear the current one. Register themes under names, attach the roots
    /// (UIDocument roots, panel roots) and call <see cref="Set"/>; every attached root is re-dressed through
    /// <see cref="ThemeRuntime.Apply"/>. Roots are held weakly, so a destroyed panel needs no detach. The chosen name can
    /// be persisted in PlayerPrefs, and a <see cref="IThemeModeSource"/> can pick between a dark and a light name.
    /// Main thread only.
    /// </summary>
    public sealed class ThemeManager
    {
        private readonly Dictionary<string, ThemeData> _themes = new(StringComparer.Ordinal);
        private readonly List<WeakReference<VisualElement>> _roots = new();
        private IThemeModeSource _modeSource;
        private string _darkName;
        private string _lightName;
        private bool _highContrast;

        private static ThemeManager s_default;

        /// <summary>A shared instance for games that want exactly one.</summary>
        public static ThemeManager Default => s_default ??= new ThemeManager();

        /// <summary>Raised after a different theme became current, with its name.</summary>
        public event Action<string> Changed;

        /// <summary>The PlayerPrefs key <see cref="Set"/> writes the chosen name to; null (the default) persists nothing.</summary>
        public string PersistKey { get; set; }

        /// <summary>The name of the current theme, or null before the first <see cref="Set"/>.</summary>
        public string CurrentName { get; private set; }

        /// <summary>The current theme, or null.</summary>
        public ThemeData Current => CurrentName != null && _themes.TryGetValue(CurrentName, out var t) ? t : null;

        /// <summary>The registered names.</summary>
        public IReadOnlyCollection<string> Names => _themes.Keys;

        /// <summary>
        /// Turns high contrast on or off for every attached root and for roots attached later (see
        /// <see cref="ThemeRuntime.SetHighContrast"/>).
        /// </summary>
        public bool HighContrast
        {
            get => _highContrast;
            set
            {
                if (_highContrast == value) return;

                _highContrast = value;
                ForEachRoot(root => ThemeRuntime.SetHighContrast(root, value));
            }
        }

        /// <summary>Registers (or replaces) a theme under a name.</summary>
        /// <returns>False when the name is empty or the theme is null.</returns>
        public bool Register(string name, ThemeData theme)
        {
            if (string.IsNullOrEmpty(name) || !theme) return false;

            _themes[name] = theme;
            return true;
        }

        /// <summary>Removes a registered theme. The current theme stays on the roots until the next <see cref="Set"/>.</summary>
        public bool Unregister(string name) => name != null && _themes.Remove(name);

        /// <summary>Starts dressing <paramref name="root"/> with the current theme, now and on every change.</summary>
        public void Attach(VisualElement root)
        {
            if (root == null) return;

            foreach (var weak in _roots)
                if (weak.TryGetTarget(out var existing) && existing == root) return;

            _roots.Add(new WeakReference<VisualElement>(root));

            var theme = Current;
            if (theme) ThemeRuntime.Apply(root, theme);
            if (_highContrast) ThemeRuntime.SetHighContrast(root, true);
        }

        /// <summary>Stops dressing <paramref name="root"/>; its theme stays on it.</summary>
        public void Detach(VisualElement root)
        {
            _roots.RemoveAll(weak => !weak.TryGetTarget(out var r) || r == root);
        }

        /// <summary>
        /// Makes <paramref name="name"/> the current theme: applies it to every attached root, stores the name when
        /// <see cref="PersistKey"/> is set, and raises <see cref="Changed"/> (also when the name was already current but its
        /// theme object was replaced).
        /// </summary>
        /// <returns>False when no theme is registered under the name.</returns>
        public bool Set(string name)
        {
            if (name == null || !_themes.TryGetValue(name, out var theme) || !theme) return false;

            var wasHighContrast = _highContrast;
            CurrentName = name;
            ForEachRoot(root =>
            {
                if (wasHighContrast) ThemeRuntime.SetHighContrast(root, false);
                ThemeRuntime.Apply(root, theme);
                if (wasHighContrast) ThemeRuntime.SetHighContrast(root, true);
            });

            if (!string.IsNullOrEmpty(PersistKey))
            {
                try
                {
                    PlayerPrefs.SetString(PersistKey, name);
                }
                catch (Exception ex)
                {
                    Debug.LogWarning("[ThemeManager] Could not store the theme name: " + ex.Message);
                }
            }

            Changed?.Invoke(name);
            return true;
        }

        /// <summary>
        /// Selects the theme stored under <see cref="PersistKey"/> when it is still registered.
        /// </summary>
        /// <returns>True when a stored theme was found and applied.</returns>
        public bool Restore()
        {
            if (string.IsNullOrEmpty(PersistKey)) return false;

            string stored;
            try
            {
                stored = PlayerPrefs.GetString(PersistKey, null);
            }
            catch (Exception)
            {
                return false;
            }

            return !string.IsNullOrEmpty(stored) && Set(stored);
        }

        /// <summary>
        /// Lets <paramref name="source"/> choose between two registered names: dark preference selects
        /// <paramref name="darkName"/>, light selects <paramref name="lightName"/>, no preference changes nothing. Applies the
        /// current preference at once. Pass null to stop following.
        /// </summary>
        public void Follow(IThemeModeSource source, string darkName, string lightName)
        {
            if (_modeSource != null) _modeSource.Changed -= OnModeChanged;

            _modeSource = source;
            _darkName = darkName;
            _lightName = lightName;

            if (_modeSource == null) return;

            _modeSource.Changed += OnModeChanged;
            OnModeChanged();
        }

        private void OnModeChanged()
        {
            var prefersDark = _modeSource?.PrefersDark;
            if (prefersDark == null) return;

            var name = prefersDark.Value ? _darkName : _lightName;
            if (name != null && name != CurrentName) Set(name);
        }

        private void ForEachRoot(Action<VisualElement> action)
        {
            for (var i = _roots.Count - 1; i >= 0; i--)
            {
                if (_roots[i].TryGetTarget(out var root) && root != null) action(root);
                else _roots.RemoveAt(i);
            }
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics() => s_default = null;
    }
}
