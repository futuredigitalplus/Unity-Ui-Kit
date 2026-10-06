using System;
using UnityEngine;
using UnityEngine.UIElements;
using FutureDigitalPlus.UIKit;
using FutureDigitalPlus.UIKit.DesignSystem.Theme.Data;
using FutureDigitalPlus.UIKit.DesignSystem.Theme.Applier;
using FutureDigitalPlus.UIKit.DesignSystem.Theme.Palette;
using FutureDigitalPlus.UIKit.DesignSystem.Behaviour;
using FutureDigitalPlus.UIKit.Animation;
using FutureDigitalPlus.UIKit.Animation.Extensions;
using FutureDigitalPlus.UIKit.Extras;
using FutureDigitalPlus.UIKit.Components;

namespace DocsCheck.Facade
{
    static class S
    {
        public static void Example(UIDocument document, ThemeData myRuntimeTheme, ThemeData lightTheme, VisualElement screenElement)
        {
            var settings = UIKitInstaller.CreatePanelSettings(myRuntimeTheme == null ? null : null);
            var kit = UIKitInstaller.Install(document.rootVisualElement, new UIKitOptions
            {
                Theme = lightTheme,
                Mobile = MobileMode.Auto,
            });
            kit.SetTheme(null);           // back to dark
            screenElement.FadeIn(250);    // Animation module
            kit.Dispose();                // take it all off
        }

        public static void Host(ThemeData myRuntimeTheme, ThemeData lightTheme)
        {
            var go = new GameObject("UI");
            go.SetActive(false);
            var document = go.AddComponent<UIDocument>();
            document.panelSettings = UIKitInstaller.CreatePanelSettings(null);
            var host = go.AddComponent<UIKitHost>();
            host.Options.Theme = lightTheme;      // or host.Theme = ... later, at runtime
            go.SetActive(true);
        }
    }
}

namespace DocsCheck.Theming
{
    static class S
    {
        public static void Where(VisualElement root)
        {
            root.parent.styleSheets.Add(Resources.Load<StyleSheet>("FutureDigitalPlus/UIKit/DesignSystem/Styles/DesignSystem"));
            ThemeRuntime.Apply(root, Resources.Load<ThemeData>(ThemeData.DarkResource));
        }
        public static void Brand(VisualElement root)
        {
            var brand = ThemeData.FromBrand(primary: new Color(0.91f, 0.27f, 0.45f), background: new Color(0.06f, 0.07f, 0.10f), dark: true);
            ThemeRuntime.Apply(root, brand);
            brand.SetToken("--fdp-color-primary", Color.cyan);   // change anything...
            ThemeRuntime.Apply(root, brand);                      // ...and apply again: a new stylesheet is generated, the old one destroyed
            UnityEngine.Object.Destroy(brand);                                // when done; its stylesheet goes with the last root that wears it
        }
        public static void Contrast(ThemeData myTheme)
        {
            var failures = ContrastAudit.Failures(myTheme.ToColorMap(), ContrastLevel.Standard);
        }
        public static void HC(VisualElement root)
        {
            ThemeRuntime.SetHighContrast(root, true);    // the built-in HighContrast* counterpart, or a derived runtime variant for brand themes
            ThemeRuntime.SetHighContrast(root, false);   // back to the theme the root wore before
        }
        public static void Manager(VisualElement root, ThemeData dark, ThemeData light, IThemeModeSource myModeSource)
        {
            var themes = ThemeManager.Default;
            themes.Register("dark", dark); themes.Register("light", light);
            themes.Attach(root);
            themes.PersistKey = "ui.theme";           // optional PlayerPrefs persistence
            themes.Restore() ;                        // select the stored one, if still registered
            themes.Set("light");                      // applies to every attached root, raises Changed
            themes.Follow(myModeSource, "dark", "light");   // IThemeModeSource: Unity has no system dark-mode API; plug a native query or a settings screen in
            themes.HighContrast = true;
        }
    }
}

namespace DocsCheck.DesignSystemReadme
{
    static class S
    {
        public static void Five(VisualElement root)
        {
            // 1. The base stylesheet (tokens + every ds-* component sheet). On the PANEL root (see "Where the theme goes" below).
            var baseSheet = Resources.Load<StyleSheet>("FutureDigitalPlus/UIKit/DesignSystem/Styles/DesignSystem");
            root.parent.styleSheets.Add(baseSheet);

            // 2. Classes in UXML or code: ds-root (top element), ds-card, ds-h2, ds-body-2, ds-btn ds-btn--primary, ds-icon--trophy ...
            var button = new Button { text = "Play" };
            button.AddToClassList("ds-btn");
            button.AddToClassList("ds-btn--primary");

            // 3. A theme (a ThemeData asset, baked into a stylesheet). Dark is the base; Light and Dark ship in Resources.
            var light = Resources.Load<ThemeData>("FutureDigitalPlus/UIKit/DesignSystem/Themes/Light");
            ThemeRuntime.Apply(root, light);          // apply or swap
            ThemeRuntime.Apply(root, null);           // back to the base tokens (same as ThemeRuntime.Clear(root))
            var worn = ThemeRuntime.WornBy(root);     // which theme this root wears, or null

            // 4. Runtime behaviours (toggle knob, tabs, drag and drop, dropdown placement) over a tree built in code.
            DesignSystemBehaviourBase<UIDocument>.EnsureAll(root);   // idempotent; the UIDocument behaviour does it for you too

            // 5. The touch-size tier: add the class "fdp-input-touch" to the root (see MOBILE.md). The UIKit facade decides it automatically.
            root.EnableInClassList("fdp-input-touch", Application.isMobilePlatform);
        }
    }
}

namespace DocsCheck.AnimationDocs
{
    public class Example : MonoBehaviour
    {
        void OnEnable()
        {
            var root = GetComponent<UIDocument>().rootVisualElement;

            var bar = new VisualElement();
            bar.style.height = 8;
            bar.style.backgroundColor = Color.gray;
            root.Add(bar);                                   // add it to the tree first, most helpers need element.parent

            bar.AnimateWidth(0f, 300f, 600);                 // width 0 -> 300 px over 600 ms
            bar.AnimateBackgroundColor("#2F569C", "#D2A00C", 800);
            bar.AnimateOpacity(0f, 1f, 400, () => Debug.Log("faded in"), Easy.OutQuad);
        }
    }
    static class S
    {
        public static void A(Label label, VisualElement panel, Slider slider, VisualElement toast, Label status, VisualElement box, VisualElement row, VisualElement element, Font myMonoFont)
        {
            label.AnimateColor("#BABABA", "#2F569C", 250);
            panel.AnimateOpacityDelayed(0f, 1f, 300, 100);                     // start after 100 ms
            slider.AnimateSlider(0f, 1f, 500, (el, v) => ((Slider)el).SetValueWithoutNotify(v));

            toast.ShowForDuration(250, 1500);
            status.AnimFadeInSequence("Saved", ColorUtil.FromHex("#607FAE"), ColorUtil.FromHex("#BABABA"), 500f, 2000f, 500f);

            var pulse = box.AnimBorderPulse(Color.gray, Color.cyan, original: Color.gray, color1DurationMs: 500, color2DurationMs: 500);
            // later
            pulse.Pause();

            row.HoverBackground(new StyleColor(Color.clear), new Color(0.2f, 0.35f, 0.6f), animate: true);
            var events = box.HoverBorderPulse(Color.gray, Color.cyan, original: Color.gray, includeChildren: true);
            box.UnregisterCallback(events.EventCallbacks.mouseOverEvent, true);
            box.UnregisterCallback(events.EventCallbacks.mouseOutEvent, true);

            var foldout = new AnimatedFoldout { text = "Details" };            // collapsed
            foldout.Add(new Label("content"));                                   // goes into the content container
            foldout.Expand(true);                                                // expand from code, Expand(true, animate: false) for no animation
            foldout.RegisterValueChangedCallback(evt => Debug.Log(evt.newValue));

            MaskedInputField.MonoFont = myMonoFont;     // optional

            var steps = new SingleTask();
            steps.Add(next => element.AnimateOpacity(0f, 1f, 300, next));
            steps.Add(next => element.AnimateWidth(0f, 200f, 300, next));
            steps.Run(() => Debug.Log("done"));
        }
    }
}

namespace DocsCheck.ExtrasDocs
{
    static class S
    {
        public static void E(VisualElement root, Button saveButton, VisualElement panel, Texture blurredTexture)
        {
            var tooltip = new Tooltip();
            root.Add(tooltip);
            saveButton.tooltip = "T:Save the game";
            saveButton.AddManipulator(new TooltipManipulator(tooltip));

            var message = new PopupMessage();
            root.Add(message);
            message.Show("Heading", "Are you sure?", "Yes", "No",
                () => { message.Hide(); /* accepted */ },
                () => message.Hide());                      // PopupMessage does not hide itself

            var popup = new ColorPopup();
            root.Add(popup);
            var field = new ColorField { label = "Fill" };
            field.ColorPopup = popup;
            field.RegisterValueChangedCallback(evt => Debug.Log(evt.newValue));
            panel.Add(field);

            var glass = new BlurPanel();
            glass.SetImage(blurredTexture);
        }
        public static void Popup(VisualElement root)
        {
            var popupMsg = root.Q<PopupMessage>("popup-message");
            var popupEd = root.Q<PopupTextField>("popup-textfield");

            popupMsg.Show("Lable", "message", "Close");

            // 20 = how many characters allowed
            popupEd.Show("label", "message", "empty or text to set", 20,
                t =>
                {   // submit was pressed
                    popupEd.Hide();
                    Debug.Log("Player entered: " + t);
                },
                () =>
                {   // cancel was pressed
                    popupEd.Hide();
                });
        }
        public static void Blur(Texture sourceTexture, RenderTexture existingRenderTexture, VisualElement root, VisualElement someParentElement)
        {
            RenderTexture blurred = BlurSource.Blur(sourceTexture, downsample: 2, iterations: 4);
            RenderTexture world = BlurSource.FromCamera(Camera.main, downsample: 4, iterations: 3);
            BlurSource.Blur(sourceTexture, existingRenderTexture, iterations: 4);
            root.Q<BlurPanel>("scene-toolbar").SetImage(blurred);
            blurred.Release(); UnityEngine.Object.Destroy(blurred);
            root.Q<BlurPanel>("scene-toolbar").SetImage(blurred, someParentElement);
        }
    }
}
