using System;
using FutureDigitalPlus.UIKit.Controls;
using FutureDigitalPlus.UIKit.DesignSystem.Theme.Applier;
using FutureDigitalPlus.UIKit.DesignSystem.Theme.Data;
using FutureDigitalPlus.UIKit.DesignSystem.Theme.Palette;
using FutureDigitalPlus.UIKit.Interaction;
using FutureDigitalPlus.UIKit.Language;
using UnityEngine;
using UnityEngine.UIElements;

namespace FutureDigitalPlus.UIKit.Samples.ThemingLanguage
{
    /// <summary>
    /// Everything a player can change about how a UI looks and reads, in one small page: the theme (dark, light, or a brand theme made at run time from two
    /// colours), high contrast, text size, reduced motion, and the language, including a right-to-left one. Put it on an empty GameObject in an empty scene
    /// and press Play.
    /// <para>
    /// The texts come from a <see cref="DictionaryTextSource"/> (English, German, Arabic), the backend that needs no package. A game that uses Unity Localization
    /// swaps one line: <c>UiLocalization.Default.Backend = new UnityLocalizationBackend()</c> (assembly <c>FutureDigitalPlus.UIKit.Language.UnityLocalization</c>,
    /// compiled only when <c>com.unity.localization</c> is installed). The labels keep working unchanged.
    /// </para>
    /// </summary>
    [AddComponentMenu("FDP UI Kit/Theming and Language Sample")]
    public sealed class ThemingLanguageSample : MonoBehaviour
    {
        private const string ThemeResource = "FutureDigitalPlus/UIKit/Samples/ThemingLanguage/UnityDefaultRuntimeTheme";
        private const string DesignSystemResource = "FutureDigitalPlus/UIKit/DesignSystem/Styles/DesignSystem";

        private static readonly (string Name, Color Colour)[] Brands =
        {
            ("Ocean", new Color(0.13f, 0.52f, 0.93f)),
            ("Forest", new Color(0.18f, 0.62f, 0.38f)),
            ("Crimson", new Color(0.86f, 0.18f, 0.28f)),
        };

        private readonly ThemeManager _themes = new ThemeManager();
        private readonly DictionaryTextSource _texts = new DictionaryTextSource("en");

        private PanelSettings _settings;
        private IDisposable _localeClasses;
        private AccessibilityApplier _accessibility;
        private Label _status;
        private Label _textSize;
        private ThemeData _brandTheme;
        private string _brandName;
        private int _brandCounter;
        private bool _dark = true;

        private void Start()
        {
            LoadTexts();
            // The labels look keys up in the default tables; the dictionary backend keeps this sample's texts in its "Default" table.
            UiLocalization.Default.DefaultTables = new[] { DictionaryTextSource.DefaultTable };
            UiLocalization.Default.Backend = _texts;

            _settings = ScriptableObject.CreateInstance<PanelSettings>();
            _settings.name = "ThemingLanguagePanelSettings";
            _settings.themeStyleSheet = Resources.Load<ThemeStyleSheet>(ThemeResource);
            _settings.scaleMode = PanelScaleMode.ScaleWithScreenSize;
            _settings.referenceResolution = new Vector2Int(1280, 720);
            _settings.screenMatchMode = PanelScreenMatchMode.MatchWidthOrHeight;
            _settings.match = 0.5f;

            // Inactive while configured, so the UIDocument builds its tree once with everything set.
            var go = new GameObject("Theming and language (UIDocument)");
            go.transform.SetParent(transform, false);
            go.SetActive(false);
            var document = go.AddComponent<UIDocument>();
            document.panelSettings = _settings;
            go.SetActive(true);

            var root = document.rootVisualElement;
            InstallStyles(root);

            // Locale: the language class, fdp-rtl and the text direction follow the backend's locale from now on.
            _localeClasses = LocaleClassApplier.Bind(root, UiLocalization.Default);
            // Accessibility: the player's choices plus the OS settings become classes (fdp-text-scale-150, fdp-reduce-motion, ...).
            _accessibility = AccessibilityApplier.Bind(root, new AccessibilityOptions());
            // Themes: the manager dresses every attached root. Dark and Light are baked assets of the design system.
            _themes.Register("Dark", Resources.Load<ThemeData>(ThemeData.DarkResource));
            _themes.Register("Light", Resources.Load<ThemeData>(ThemeData.LightResource));
            _themes.Attach(root);
            _themes.Set("Dark");

            Build(root);
            RefreshStatus();
        }

        private void OnDestroy()
        {
            _localeClasses?.Dispose();
            _accessibility?.Dispose();
            // The localization is a process-wide default: put it back the way it was found.
            if (ReferenceEquals(UiLocalization.Default.Backend, _texts))
            {
                UiLocalization.Default.Backend = null;
                UiLocalization.Default.DefaultTables = null;
            }
            if (_brandTheme != null)
                Destroy(_brandTheme);
            if (_settings != null)
                Destroy(_settings);
        }

        private static void InstallStyles(VisualElement root)
        {
            // The design system sheet goes on the panel root so a theme set on the document root (a descendant) wins; the module sheets follow it.
            var holder = root.parent ?? root;
            var designSystem = Resources.Load<StyleSheet>(DesignSystemResource);
            if (designSystem != null && !holder.styleSheets.Contains(designSystem))
                holder.styleSheets.Add(designSystem);
            LanguageStyles.Install(holder);
            ControlsStyles.Install(root);
            InteractionStyles.Install(root);
            root.AddToClassList("ds-root");
        }

        private void Build(VisualElement root)
        {
            var page = new ScrollView { mode = ScrollViewMode.Vertical };
            page.style.flexGrow = 1;
            root.Add(page);

            var card = new VisualElement();
            card.AddToClassList("ds-card");
            card.style.alignSelf = Align.Center;
            card.style.width = 560;
            card.style.maxWidth = Length.Percent(94f);
            card.style.marginTop = 24;
            page.Add(card);

            card.Add(Heading("TITLE", "Theming and language", "ds-h2"));
            card.Add(Heading("BODY", "Change the look and the language while it runs.", "ds-body-2"));

            card.Add(Heading("LANGUAGE", "Language", "ds-h3"));
            var language = new SegmentedControl();
            language.AddTab("en", "English");
            language.AddTab("de", "Deutsch");
            language.AddTab("ar", "العربية");
            language.RegisterValueChangedCallback(e => _texts.SetLocale(e.newValue));
            language.value = "en";
            card.Add(language);

            card.Add(Heading("THEME", "Theme", "ds-h3"));
            var mode = new SegmentedControl();
            mode.AddTab("dark", "Dark");
            mode.AddTab("light", "Light");
            mode.RegisterValueChangedCallback(e =>
            {
                _dark = e.newValue == "dark";
                _themes.Set(_dark ? "Dark" : "Light");
                RefreshStatus();
            });
            mode.value = "dark";
            card.Add(mode);

            // A brand theme: the primary family, surfaces, borders and text are derived from the two colours; the focus ring and selection too.
            var brandRow = new VisualElement();
            brandRow.style.flexDirection = FlexDirection.Row;
            brandRow.style.flexWrap = Wrap.Wrap;
            brandRow.style.marginTop = 8;
            foreach (var brand in Brands)
            {
                var captured = brand;
                var button = new Button(() => ApplyBrand(captured.Name, captured.Colour)) { text = captured.Name };
                button.AddToClassList("ds-btn");
                button.AddToClassList("ds-btn--ghost");
                button.style.marginRight = 8;
                brandRow.Add(button);
            }
            card.Add(brandRow);

            card.Add(Heading("ACCESS", "Accessibility", "ds-h3"));
            var row = new VisualElement();
            row.style.flexDirection = FlexDirection.Row;
            row.style.flexWrap = Wrap.Wrap;
            row.style.alignItems = Align.Center;
            row.Add(ToggleButton("HIGH-CONTRAST", "High contrast", on =>
            {
                _themes.HighContrast = on;
                RefreshStatus();
            }));
            row.Add(ToggleButton("REDUCE-MOTION", "Reduce motion", on =>
            {
                _accessibility.Options.ReduceMotion = on;
                _accessibility.Refresh();
            }));
            card.Add(row);

            var sizeRow = new VisualElement();
            sizeRow.style.flexDirection = FlexDirection.Row;
            sizeRow.style.alignItems = Align.Center;
            sizeRow.style.marginTop = 8;
            sizeRow.Add(StepButton("A-", -1));
            _textSize = new Label();
            _textSize.style.marginLeft = _textSize.style.marginRight = 12;
            sizeRow.Add(_textSize);
            sizeRow.Add(StepButton("A+", +1));
            card.Add(sizeRow);
            ShowTextSize();

            // What the themes look like in use.
            var sample = new VisualElement();
            sample.style.flexDirection = FlexDirection.Row;
            sample.style.marginTop = 16;
            sample.Add(PrimaryButton("BTN-PRIMARY", "Continue", "ds-btn--primary"));
            sample.Add(PrimaryButton("BTN-SECONDARY", "Cancel", "ds-btn--secondary"));
            card.Add(sample);

            _status = new Label();
            _status.AddToClassList("ds-caption");
            _status.style.marginTop = 16;
            card.Add(_status);
        }

        // ---- actions ---------------------------------------------------------------------------------------------

        private void ApplyBrand(string name, Color colour)
        {
            var background = _dark ? new Color(0.06f, 0.07f, 0.1f) : new Color(0.97f, 0.97f, 0.98f);
            var theme = ThemeData.FromBrand(colour, background, _dark);
            if (theme == null)
                return;
            // A new name each time: the manager re-dresses its roots, and the previous brand theme can be destroyed once nothing wears it.
            var previousTheme = _brandTheme;
            var previousName = _brandName;
            _brandTheme = theme;
            _brandName = "Brand " + (++_brandCounter) + " " + name;
            _themes.Register(_brandName, theme);
            _themes.Set(_brandName);
            if (previousTheme != null)
            {
                _themes.Unregister(previousName);
                Destroy(previousTheme);
            }
            RefreshStatus();
        }

        private void StepText(int direction)
        {
            var steps = TextScaleSteps.All;
            var index = 0;
            for (var i = 0; i < steps.Count; i++)
                if (steps[i] == _accessibility.Options.TextScalePercent)
                    index = i;
            _accessibility.Options.TextScalePercent = steps[Mathf.Clamp(index + direction, 0, steps.Count - 1)];
            _accessibility.Refresh();
            ShowTextSize();
        }

        private void ShowTextSize() => _textSize.text = _accessibility.Options.TextScalePercent + "%";

        // The audit that gates the built-in themes also runs on a brand theme at run time: it lists the colour pairs that fall below WCAG AA (or AAA with high contrast).
        private void RefreshStatus()
        {
            var theme = _themes.Current;
            if (theme == null)
                return;
            var level = _themes.HighContrast ? ContrastLevel.High : ContrastLevel.Standard;
            var failures = ContrastAudit.Failures(theme.ToColorMap(), level);
            _status.text = "Theme: " + _themes.CurrentName + ". Contrast checks failing: " + failures.Count + ".";
        }

        // ---- small builders --------------------------------------------------------------------------------------

        private static LocalizedLabel Heading(string key, string fallback, string style)
        {
            var label = new LocalizedLabel { Key = key, Fallback = fallback };
            label.AddToClassList(style);
            label.style.marginTop = 12;
            return label;
        }

        private static LocalizedButton PrimaryButton(string key, string fallback, string variant)
        {
            var button = new LocalizedButton { Key = key, Fallback = fallback };
            button.AddToClassList("ds-btn");
            button.AddToClassList(variant);
            button.style.marginRight = 8;
            return button;
        }

        private static LocalizedButton ToggleButton(string key, string fallback, Action<bool> changed)
        {
            var on = false;
            var button = new LocalizedButton { Key = key, Fallback = fallback };
            button.AddToClassList("ds-btn");
            button.AddToClassList("ds-btn--secondary");
            button.style.marginRight = 8;
            button.clicked += () =>
            {
                on = !on;
                button.EnableInClassList("ds-btn--pressed", on);
                changed(on);
            };
            return button;
        }

        private Button StepButton(string caption, int direction)
        {
            var button = new Button(() => StepText(direction)) { text = caption };
            button.AddToClassList("ds-btn");
            button.AddToClassList("ds-btn--ghost");
            return button;
        }

        // ---- texts -----------------------------------------------------------------------------------------------

        private void LoadTexts()
        {
            var table = DictionaryTextSource.DefaultTable;
            Add("en", table, "TITLE", "Theming and language");
            Add("en", table, "BODY", "Change the look and the language while it runs.");
            Add("en", table, "LANGUAGE", "Language");
            Add("en", table, "THEME", "Theme");
            Add("en", table, "ACCESS", "Accessibility");
            Add("en", table, "HIGH-CONTRAST", "High contrast");
            Add("en", table, "REDUCE-MOTION", "Reduce motion");
            Add("en", table, "BTN-PRIMARY", "Continue");
            Add("en", table, "BTN-SECONDARY", "Cancel");

            Add("de", table, "TITLE", "Design und Sprache");
            Add("de", table, "BODY", "Ändere Aussehen und Sprache während das Spiel läuft.");
            Add("de", table, "LANGUAGE", "Sprache");
            Add("de", table, "THEME", "Design");
            Add("de", table, "ACCESS", "Barrierefreiheit");
            Add("de", table, "HIGH-CONTRAST", "Hoher Kontrast");
            Add("de", table, "REDUCE-MOTION", "Weniger Bewegung");
            Add("de", table, "BTN-PRIMARY", "Weiter");
            Add("de", table, "BTN-SECONDARY", "Abbrechen");

            // Arabic: right-to-left. The mirrored layout comes from Rtl.uss; the glyphs come from the fonts of the device (see the README).
            Add("ar", table, "TITLE", "المظهر واللغة");
            Add("ar", table, "BODY", "غيّر المظهر واللغة أثناء التشغيل.");
            Add("ar", table, "LANGUAGE", "اللغة");
            Add("ar", table, "THEME", "المظهر");
            Add("ar", table, "ACCESS", "إمكانية الوصول");
            Add("ar", table, "HIGH-CONTRAST", "تباين عالٍ");
            Add("ar", table, "REDUCE-MOTION", "حركة أقل");
            Add("ar", table, "BTN-PRIMARY", "متابعة");
            Add("ar", table, "BTN-SECONDARY", "إلغاء");
        }

        private void Add(string locale, string table, string key, string value) => _texts.Add(locale, table, key, value);
    }
}
