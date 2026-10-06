using FutureDigitalPlus.UIKit.DesignSystem.Behaviour.UIDocument;
using FutureDigitalPlus.UIKit.DesignSystem.Theme.Applier;
using FutureDigitalPlus.UIKit.DesignSystem.Theme.Data;
using UnityEngine;
using UnityEngine.UIElements;

namespace FutureDigitalPlus.UIKit.Samples
{
    /// <summary>
    /// Smallest useful host for the DesignSystem module. Put it on an empty GameObject in an empty
    /// scene and press Play: it builds a UIDocument with a PanelSettings, loads the sample page,
    /// turns on the design system's runtime behaviours (tabs, toggles, dropdown popups) and wires
    /// one button that swaps between the base (dark) tokens and the bundled Light theme.
    ///
    /// New code written for this package. The page it shows is an excerpt of the upstream
    /// showcase UXML; see ../../Runtime/DesignSystem/SOURCE.md.
    /// </summary>
    [AddComponentMenu("Future Digital Plus UI Kit/Design System Showcase Sample")]
    public class DesignSystemShowcase : MonoBehaviour
    {
        // Resources paths of the sample's own files (the sample folder carries a Resources folder).
        private const string PageResource  = "FutureDigitalPlus/UIKit/Samples/DesignSystemShowcase/DesignSystemShowcase";
        private const string ThemeSheet    = "FutureDigitalPlus/UIKit/Samples/DesignSystemShowcase/UnityDefaultRuntimeTheme";

        // Resources path of the Light ThemeData the DesignSystem module ships.
        private const string LightThemeResource = "FutureDigitalPlus/UIKit/DesignSystem/Themes/Light";

        // Same breakpoint the upstream showcase uses; the `mobile` class on the root flips the
        // design system's spacing and touch-target tokens (see Mobile.uss).
        private const int MobileBreakpoint = 768;

        private UIDocument _document;
        private ThemeData _light;
        private bool _lightOn;

        private void Start()
        {
            var page = Resources.Load<VisualTreeAsset>(PageResource);
            if (page == null)
            {
                Debug.LogError($"[DesignSystemShowcase] Could not load '{PageResource}' from Resources.");
                return;
            }

            // A runtime PanelSettings needs a Theme Style Sheet, otherwise Unity logs a warning
            // and default control text does not draw. The .tss only imports unity-theme://default.
            var panelSettings = ScriptableObject.CreateInstance<PanelSettings>();
            panelSettings.name = "DesignSystemShowcasePanelSettings";
            panelSettings.themeStyleSheet = Resources.Load<ThemeStyleSheet>(ThemeSheet);
            panelSettings.scaleMode = PanelScaleMode.ConstantPixelSize;
            panelSettings.scale = Mathf.Clamp(Screen.dpi > 0f ? Screen.dpi / 96f : 1f, 1f, 3f);
            panelSettings.clearColor = true;
            panelSettings.colorClearValue = new Color(0.043f, 0.058f, 0.090f, 1f);

            // Inactive while it is configured, so UIDocument builds its tree once with everything set.
            var host = new GameObject("DesignSystemShowcase (UIDocument)");
            host.transform.SetParent(transform, false);
            host.SetActive(false);
            _document = host.AddComponent<UIDocument>();
            _document.panelSettings = panelSettings;
            _document.visualTreeAsset = page;
            host.AddComponent<DesignSystemBehaviour>();
            host.SetActive(true);

            _light = Resources.Load<ThemeData>(LightThemeResource);

            var root = _document.rootVisualElement;
            root.RegisterCallback<GeometryChangedEvent>(_ => ApplyMobileClass(root));
            ApplyMobileClass(root);

            // The master stylesheet goes on the PANEL root (the parent of the document root), not on the
            // document root: its `:root` tokens outrank a class-scoped theme that sits on the same element
            // (verified on Unity 6000.3.25f1), while a theme on the document root, one level below, wins.
            // Dropdown popups are added next to the document root too, so they get the tokens from here as
            // well. The parent only exists once the document has attached to its panel, hence the deferred call.
            root.schedule.Execute(() => InstallPanelScopeSheets(root.parent)).StartingIn(0);

            var toggle = root.Q<Button>("theme-toggle");
            if (toggle != null)
                toggle.clicked += () => SetLight(!_lightOn, toggle);
        }

        private void SetLight(bool on, Button label)
        {
            var root = _document.rootVisualElement;
            _lightOn = on;

            // ThemeRuntime adds the theme's baked stylesheet and its scope class (`theme-light`)
            // to the root; passing null takes it off again and returns to the base tokens.
            ThemeRuntime.Apply(root, on ? _light : null);
            label.text = on ? "Switch to Dark theme" : "Switch to Light theme";
        }

        private static void InstallPanelScopeSheets(VisualElement panelScope)
        {
            if (panelScope == null) return;

            // The master sheet @imports the tokens and every component sheet; the popup chrome is a separate
            // file because dropdown menus live outside the document root.
            foreach (var path in new[] { "FutureDigitalPlus/UIKit/DesignSystem/Styles/DesignSystem", "FutureDigitalPlus/UIKit/DesignSystem/Styles/DropdownPopup" })
            {
                var sheet = Resources.Load<StyleSheet>(path);
                if (sheet != null && !panelScope.styleSheets.Contains(sheet))
                    panelScope.styleSheets.Add(sheet);
            }
        }

        private static void ApplyMobileClass(VisualElement root)
        {
            var width = root.layout.width;
            if (float.IsNaN(width) || width <= 0f) return;
            root.EnableInClassList("fdp-input-touch", width < MobileBreakpoint);
        }

        private void OnDestroy()
        {
            if (_document != null && _document.panelSettings != null)
                Destroy(_document.panelSettings);
        }
    }
}
