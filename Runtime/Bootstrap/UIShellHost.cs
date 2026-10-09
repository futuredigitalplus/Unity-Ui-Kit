using System;
using FutureDigitalPlus.UIKit.Animation;
using FutureDigitalPlus.UIKit.DesignSystem.Theme.Applier;
using FutureDigitalPlus.UIKit.DesignSystem.Theme.Data;
using FutureDigitalPlus.UIKit.Foundation;
using FutureDigitalPlus.UIKit.Interaction;
using FutureDigitalPlus.UIKit.Language;
using UnityEngine;
using UnityEngine.UIElements;

namespace FutureDigitalPlus.UIKit.Bootstrap
{
    /// <summary>
    /// A ready-made host for a game with more than one screen: put it on the GameObject of a <c>UIDocument</c> and the kit is running, with the joins between the
    /// modules done for you. It installs the styles, wears a Light or Dark theme (<see cref="Themes"/> switches by name), binds the input device
    /// (<see cref="Profile"/>) and the accessibility settings (<see cref="Accessibility"/>) to classes on the root, makes the language a class on the root, builds a
    /// <see cref="UiShell"/> (<see cref="Shell"/>: screens, dialogs, toasts, loading overlay, safe area) and sends Escape, gamepad B and the Android back button to
    /// <see cref="UiShell.HandleBackPress"/>. Register the game's screens in <see cref="Ready"/> (or in a <c>Start</c> that runs after this one) and call <c>Shell.Boot</c>.
    /// <para>
    /// Do not put a <c>UIKitHost</c> (the facade) on the same document: the shell pads the safe area and the input profile writes the touch class itself, so the facade steps aside
    /// once it sees a shell, but there is nothing for it to do. The HUD, Controls and Gameplay styles are not installed here (this assembly does not reference them): call
    /// <c>HudStyles.Install(host.Root)</c> and the like from <see cref="Ready"/>.
    /// </para>
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(UIDocument))]
    [AddComponentMenu("FDP UI Kit/UI Shell Host")]
    public sealed class UIShellHost : MonoBehaviour
    {
        [Tooltip("Name of the theme to wear first: \"dark\" or \"light\", or a name registered on Themes before Start.")]
        [SerializeField] private string startTheme = "dark";

        [Tooltip("Resources folder that holds the screens' UXML and USS files: Resources/<folder>/<UxmlName>.uxml. Empty: screens build their elements in code.")]
        [SerializeField] private string screensFolder = "Screens";

        private UIDocument _document;
        private bool _started;
        private BackKeySource _back;
        private InputProfileModeSource _modeSource;
        private IDisposable _locale;
        private VisualElement _root;
        private Func<int, int> _scaler;

        /// <summary>Raised once the kit is running (after <see cref="Shell"/> exists): register screens and boot the first one here. It is raised again after the host was disabled and enabled.</summary>
        public event Action<UIShellHost> Ready;

        /// <summary>True between <see cref="Ready"/> and the next disable.</summary>
        public bool IsReady => Shell != null;

        /// <summary>The document root the kit runs on; null before the kit is ready.</summary>
        public VisualElement Root => Shell != null ? _root : null;

        /// <summary>Screens, dialogs, toasts, loading overlay; null before the kit is ready.</summary>
        public UiShell Shell { get; private set; }

        /// <summary>Which input device the player used last; null before the kit is ready.</summary>
        public InputProfile Profile { get; private set; }

        /// <summary>The system and player accessibility settings as classes on the root; null before the kit is ready.</summary>
        public AccessibilityApplier Accessibility { get; private set; }

        /// <summary>The themes of this host. "dark" and "light" are registered; register a brand theme before <c>Start</c> (or later and call <c>Set</c>).</summary>
        public ThemeManager Themes { get; } = new ThemeManager();

        /// <summary>Optional: change the shell's options (state store, catalog, toast placement, ...) before the shell is built. Set it in <c>Awake</c>.</summary>
        public Action<UiShellOptions> ConfigureShell { get; set; }

        private void Start()
        {
            _started = true;
            Build();
        }

        private void OnEnable()
        {
            if (_started) Build();
        }

        private void OnDisable() => TearDown();

        private void Update() => Shell?.Tick();

        private void OnApplicationFocus(bool focus) => Shell?.NotifyApplicationFocus(focus);

        private void OnApplicationPause(bool paused) => Shell?.NotifyApplicationPause(paused);

        private void Build()
        {
            if (Shell != null) return;
            _document = GetComponent<UIDocument>();
            var root = _document != null ? _document.rootVisualElement : null;
            if (root == null)
            {
                Debug.LogError("[UIShellHost] The UIDocument has no root element (no Panel Settings assigned?).", this);
                return;
            }

            // 1. Styles. The design system sheet goes on the panel root, the rest on the document root (the facade's reason: a class-scoped theme only wins from a descendant).
            FoundationStyles.InstallDesignSystem(root.parent ?? root);
            InteractionStyles.Install(root);
            LanguageStyles.Install(root.parent ?? root);

            _root = root;

            // 2. Themes.
            RegisterTheme("dark", ThemeData.DarkResource);
            RegisterTheme("light", ThemeData.LightResource);
            Themes.Attach(root);
            Themes.Set(startTheme);

            // 3. Input device and accessibility become classes on the root; the Animation module's durations obey "reduce motion".
            Profile = InputProfile.Bind(root);
            Accessibility = AccessibilityApplier.Bind(root, new AccessibilityOptions());
            Accessibility.Changed += OnAccessibilityChanged;
            OnAccessibilityChanged(Accessibility.Current);
            _scaler = Accessibility.ScaleMotion;
            EatkMotion.Scaler = _scaler;

            // 4. The language becomes fdp-lang-<code> and fdp-rtl on the root.
            _locale = LocaleClassApplier.Bind(root);

            // 5. The shell.
            _modeSource = new InputProfileModeSource(Profile);
            var options = new UiShellOptions
            {
                InputModeSource = _modeSource,
                Text = new UiTextSource(UiLocalization.Default.Resolve),
            };
            if (!string.IsNullOrEmpty(screensFolder))
                options.Assets = new ResourcesAssetSource(screensFolder);
            ConfigureShell?.Invoke(options);
            Shell = new UiShell(root, options);
            UiLocalization.Default.Changed += Shell.Context.Text.NotifyChanged;

            // 6. One back signal: Escape, gamepad B, the Android back button.
            _back = new BackKeySource();
            _back.Bind(root);
            _back.Pressed += OnBackPressed;

            Ready?.Invoke(this);
        }

        private void TearDown()
        {
            if (Shell != null) UiLocalization.Default.Changed -= Shell.Context.Text.NotifyChanged;
            if (_back != null)
            {
                _back.Pressed -= OnBackPressed;
                _back.Dispose();
            }

            Shell?.Dispose();
            _locale?.Dispose();
            if (Accessibility != null)
            {
                Accessibility.Changed -= OnAccessibilityChanged;
                Accessibility.Dispose();
            }

            _modeSource?.Detach();
            Profile?.Dispose();
            Themes.Detach(_root);
            if (_scaler != null && EatkMotion.Scaler == _scaler)
                EatkMotion.Scaler = null;

            _back = null;
            _root = null;
            _scaler = null;
            Shell = null;
            _locale = null;
            Accessibility = null;
            _modeSource = null;
            Profile = null;
        }

        private void RegisterTheme(string name, string resource)
        {
            foreach (var existing in Themes.Names)
                if (existing == name) return; // the game registered its own before Start
            Themes.Register(name, Resources.Load<ThemeData>(resource));
        }

        private void OnBackPressed() => Shell?.HandleBackPress();

        private void OnAccessibilityChanged(EffectiveAccessibility a)
        {
            // The shell and the HUD read UiAccessibility; the theme manager switches the high contrast theme.
            UiAccessibility.Set(UiAccessibility.Current
                .WithReduceMotion(a.ReduceMotion).WithHighContrast(a.HighContrast).WithTextScale(a.TextScalePercent / 100f).WithBoldText(a.BoldText));
            Themes.HighContrast = a.HighContrast;
        }
    }
}
