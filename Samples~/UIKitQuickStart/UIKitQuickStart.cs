using FutureDigitalPlus.UIKit.DesignSystem.Theme.Data;
using FutureDigitalPlus.UIKit.Extras;
using UnityEngine;
using UnityEngine.UIElements;

namespace FutureDigitalPlus.UIKit.Samples
{
    /// <summary>
    /// The shortest path to a themed screen with the facade. Put it on an empty GameObject in an empty scene and
    /// press Play. It creates a PanelSettings and a UIDocument, adds a <see cref="UIKitHost"/> (theme, mobile class,
    /// safe area), builds a small card in code with design system classes, fades it in, adds an Extras tooltip and
    /// a button that swaps the Light and Dark themes.
    /// </summary>
    [AddComponentMenu("FDP UI Kit/UI Kit Quick Start Sample")]
    public class UIKitQuickStart : MonoBehaviour
    {
        private const string RuntimeThemeResource = "FutureDigitalPlus/UIKit/Samples/UIKitQuickStart/UnityDefaultRuntimeTheme";
        private const string LightThemeResource = "FutureDigitalPlus/UIKit/DesignSystem/Themes/Light";
        private const string ExtrasStylesResource = "FutureDigitalPlus/UIKit/Samples/UIKitQuickStart/QuickStartExtras";

        private PanelSettings _panelSettings;
        private UIKitHost _host;
        private ThemeData _light;
        private bool _lightOn;

        private void Start()
        {
            _panelSettings = UIKitInstaller.CreatePanelSettings(Resources.Load<ThemeStyleSheet>(RuntimeThemeResource));
            _light = Resources.Load<ThemeData>(LightThemeResource);

            // Inactive while configured, so the UIDocument and the host start with everything set.
            var go = new GameObject("UIKit Quick Start (UIDocument)");
            go.transform.SetParent(transform, false);
            go.SetActive(false);
            var document = go.AddComponent<UIDocument>();
            document.panelSettings = _panelSettings;
            _host = go.AddComponent<UIKitHost>();
            go.SetActive(true);

            Build(document.rootVisualElement);
        }

        private void Build(VisualElement root)
        {
            var screen = new VisualElement();
            screen.AddToClassList("ds-root");
            screen.style.justifyContent = Justify.Center;
            screen.style.alignItems = Align.Center;
            root.Add(screen);

            var card = new VisualElement();
            card.AddToClassList("ds-card");
            card.style.width = 320;
            screen.Add(card);

            var title = new Label("UI Kit quick start");
            title.AddToClassList("ds-h2");
            card.Add(title);

            var body = new Label("Themed by the DesignSystem module, installed with one UIKitHost component.");
            body.AddToClassList("ds-body-2");
            card.Add(body);

            var button = new Button { text = "Switch theme", tooltip = "B:Swap Light and Dark" };
            button.AddToClassList("ds-btn");
            button.AddToClassList("ds-btn--primary");
            button.clicked += () =>
            {
                _lightOn = !_lightOn;
                _host.Theme = _lightOn ? _light : null; // null returns to the base (dark) tokens
            };
            card.Add(button);

            // Extras module: one tooltip element on the root, a manipulator on each element that has a tooltip.
            // The Extras style sheets need theme variables; this small sheet maps them onto the design system tokens.
            var extras = Resources.Load<StyleSheet>(ExtrasStylesResource);
            if (extras != null) root.styleSheets.Add(extras);
            var tooltip = new Tooltip();
            root.Add(tooltip);
            button.AddManipulator(new TooltipManipulator(tooltip));

            // Animation module through the facade helper.
            card.FadeIn(400);
        }

        private void OnDestroy()
        {
            if (_panelSettings != null) Destroy(_panelSettings);
        }
    }
}
