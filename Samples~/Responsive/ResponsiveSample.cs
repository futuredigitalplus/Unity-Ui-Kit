using System;
using FutureDigitalPlus.UIKit.Foundation;
using UnityEngine;
using UnityEngine.UIElements;

namespace FutureDigitalPlus.UIKit.Samples.Responsive
{
    /// <summary>
    /// One UI for a phone, a tablet, a foldable, a desktop window and a TV. Put it on an empty GameObject in an empty scene, press Play and resize the Game view
    /// (or use the Device Simulator). The page shows what the foundation does about size: the <see cref="PanelScaleDriver"/> turns the screen's size and DPI
    /// into a panel scale (and the player's UI size multiplier on top), the shell writes <c>fdp-compact</c>, <c>fdp-regular</c>, <c>fdp-expanded</c>, <c>fdp-portrait</c> and
    /// <c>fdp-landscape</c> on its root so plain USS selectors can reflow the layout, a <see cref="SafeAreaPanel"/> keeps content out of the notch, and the touch tier
    /// (<c>fdp-input-touch</c>) enlarges hit areas.
    /// <para>
    /// A desktop has no notch, so the sample can fake one: its <see cref="SafeAreaProvider"/> reads a rectangle from a function, and the function returns either the
    /// device safe area or a made-up one. The same function is how a game tests safe-area handling without a phone.
    /// </para>
    /// </summary>
    [AddComponentMenu("FDP UI Kit/Responsive Sample")]
    public sealed class ResponsiveSample : MonoBehaviour
    {
        private const string ThemeResource = "FutureDigitalPlus/UIKit/Samples/Responsive/UnityDefaultRuntimeTheme";
        private const string StylesResource = "FutureDigitalPlus/UIKit/Samples/Responsive/Responsive";
        private const string TouchClass = "fdp-input-touch";

        private static readonly float[] UserScales = { 0.8f, 1f, 1.2f, 1.4f, 1.6f };

        private PanelSettings _settings;
        private PanelScaleDriver _driver;
        private UiShell _shell;
        private SafeAreaProvider _safeArea;
        private Func<Rect> _deviceSafeArea;
        private bool _fakeNotch;
        private int _userScaleIndex = 1;
        private int _profileIndex;

        private void Start()
        {
            _settings = ScriptableObject.CreateInstance<PanelSettings>();
            _settings.name = "ResponsivePanelSettings";
            _settings.themeStyleSheet = Resources.Load<ThemeStyleSheet>(ThemeResource);
            _settings.scaleMode = PanelScaleMode.ConstantPixelSize;

            // The driver works on a copy of the settings and changes its scale when the screen changes. Start with the profile that fits the device class.
            _profileIndex = Application.isMobilePlatform ? 0 : 1;
            _driver = new PanelScaleDriver(_settings, Profiles[_profileIndex].Create());

            // Inactive while configured, so the UIDocument builds its tree once with everything set.
            var go = new GameObject("Responsive (UIDocument)");
            go.transform.SetParent(transform, false);
            go.SetActive(false);
            var document = go.AddComponent<UIDocument>();
            document.panelSettings = _driver.Settings;
            go.SetActive(true);

            var root = document.rootVisualElement;
            FoundationStyles.InstallDesignSystem(root.parent ?? root);

            // The provider reads the safe area from a function. Keep the device's own and put a switchable fake in front of it.
            _safeArea = new SafeAreaProvider();
            _deviceSafeArea = _safeArea.SafeAreaSource;
            _safeArea.SafeAreaSource = () => _fakeNotch ? FakeNotchArea() : _deviceSafeArea();

            _shell = new UiShell(root, new UiShellOptions { SafeArea = _safeArea });
            _shell.Catalog.Register("Home", () => new HomeScreen(this));
            _shell.Boot("Home");
        }

        private void Update()
        {
            // Rotation, a resized window, another monitor: the driver notices and applies a new scale; the shell notices the new panel size by itself.
            _driver?.Tick();
            _shell?.Tick();
        }

        private void OnDestroy()
        {
            _shell?.Dispose();
            _driver?.Dispose();
            if (_settings != null)
                Destroy(_settings);
        }

        // A notch at the top and a home indicator at the bottom, in screen pixels with the origin at the bottom left.
        private static Rect FakeNotchArea()
        {
            // UnityEngine.Device.Screen is what the provider reads too; it follows the Device Simulator, UnityEngine.Screen does not.
            float width = UnityEngine.Device.Screen.width;
            float height = UnityEngine.Device.Screen.height;
            return new Rect(0f, height * 0.03f, width, height * 0.91f);
        }

        // ---- the profiles a game picks from ----------------------------------------------------------------------

        private static readonly (string Name, Func<PanelScaleProfile> Create)[] Profiles =
        {
            ("CardPortrait (phones and tablets, portrait)", PanelScaleProfile.CardPortrait),
            ("DesktopHud (windows, landscape)", PanelScaleProfile.DesktopHud),
            ("Console (television, window derived)", PanelScaleProfile.Console),
            ("Web (browser window)", PanelScaleProfile.Web),
        };

        // Devices that cannot be plugged in at once: the same policy evaluated for each (handheld, class, dpi, width, height).
        private static readonly (string Name, bool Handheld, DeviceClass Device, float Dpi, int Width, int Height, int Profile)[] Devices =
        {
            ("Phone, portrait", true, DeviceClass.Phone, 460f, 1170, 2532, 0),
            ("Tablet, landscape", true, DeviceClass.Tablet, 264f, 2732, 2048, 0),
            ("Foldable, unfolded", true, DeviceClass.Tablet, 420f, 2176, 1812, 0),
            ("Desktop window", false, DeviceClass.Desktop, 96f, 1280, 720, 1),
            ("Desktop 4K", false, DeviceClass.Desktop, 163f, 3840, 2160, 1),
            ("Television 1080p", false, DeviceClass.Console, 96f, 1920, 1080, 2),
        };

        // ---- the page --------------------------------------------------------------------------------------------

        private sealed class HomeScreen : UiScreen
        {
            private readonly ResponsiveSample _owner;
            private Label _info;
            private Label _insets;
            private Button _notchButton;
            private Button _touchButton;
            private Button _scaleButton;
            private Button _profileButton;
            private VisualElement _notchShape;

            public HomeScreen(ResponsiveSample owner)
            {
                _owner = owner;
            }

            protected override void OnBuild()
            {
                // On the shell root, so the fake notch (a child of the shell root, not of this page) is styled too.
                var sheet = Resources.Load<StyleSheet>(StylesResource);
                if (sheet != null && !Ui.Root.styleSheets.Contains(sheet))
                    Ui.Root.styleSheets.Add(sheet);

                // Content goes inside a SafeAreaPanel. The notch shape and the frame show where the safe area is.
                var safe = new SafeAreaPanel();
                safe.style.flexGrow = 1;
                Root.Add(safe);
                var frame = new VisualElement();
                frame.AddToClassList("fdp-sample-safe-frame");
                safe.Add(frame);
                var page = new ScrollView { mode = ScrollViewMode.Vertical };
                page.AddToClassList("fdp-sample-page");
                frame.Add(page);

                _notchShape = new VisualElement { pickingMode = PickingMode.Ignore };
                _notchShape.AddToClassList("fdp-sample-fake-notch");
                _notchShape.style.display = DisplayStyle.None;
                Ui.Root.Add(_notchShape);
                OnDisposeDo(() => _notchShape.RemoveFromHierarchy());

                page.Add(Heading("Responsive", "ds-h1"));

                // 1. What the foundation knows right now.
                var now = Card();
                _info = new Label();
                _insets = new Label();
                _insets.AddToClassList("ds-caption");
                now.Add(_info);
                now.Add(_insets);
                page.Add(now);

                // 2. The switches.
                var controls = Card();
                var row = new VisualElement();
                row.AddToClassList("fdp-sample-row");
                _notchButton = AddButton(row, ToggleNotch);
                _touchButton = AddButton(row, ToggleTouch);
                _scaleButton = AddButton(row, CycleUserScale);
                _profileButton = AddButton(row, CycleProfile);
                controls.Add(row);
                page.Add(controls);

                // 3. One content, three layouts, chosen by the class the shell writes on its root.
                var layout = Card();
                layout.Add(Heading("Tiles reflow by size class", "ds-h3"));
                var tiles = new VisualElement();
                tiles.AddToClassList("fdp-sample-tiles");
                for (var i = 1; i <= 6; i++)
                {
                    var tile = new Label("Tile " + i);
                    tile.AddToClassList("fdp-sample-tile");
                    tiles.Add(tile);
                }
                layout.Add(tiles);
                page.Add(layout);

                // 4. The same scale policy for devices that are not here.
                var table = Card();
                table.Add(Heading("Scale policy by device", "ds-h3"));
                foreach (var device in Devices)
                    table.Add(DeviceRow(device));
                page.Add(table);

                Ui.Profile.Changed += OnProfileChanged;
                Ui.SafeArea.Changed += OnInsetsChanged;
                _owner._driver.Applied += OnScaleApplied;
                OnDisposeDo(() =>
                {
                    Ui.Profile.Changed -= OnProfileChanged;
                    Ui.SafeArea.Changed -= OnInsetsChanged;
                    _owner._driver.Applied -= OnScaleApplied;
                });
                Refresh();
            }

            private void ToggleNotch()
            {
                _owner._fakeNotch = !_owner._fakeNotch;
                _notchShape.style.display = _owner._fakeNotch ? DisplayStyle.Flex : DisplayStyle.None;
                Ui.SafeArea.Invalidate();
                Refresh();
            }

            private void ToggleTouch()
            {
                // The facade's touch tier is this one class on the document root: every design system control grows to the touch minimum under it.
                var on = !Ui.Root.parent.ClassListContains(TouchClass);
                Ui.Root.parent.EnableInClassList(TouchClass, on);
                Refresh();
            }

            private void CycleUserScale()
            {
                _owner._userScaleIndex = (_owner._userScaleIndex + 1) % UserScales.Length;
                _owner._driver.UserScale = UserScales[_owner._userScaleIndex];
                Refresh();
            }

            private void CycleProfile()
            {
                _owner._profileIndex = (_owner._profileIndex + 1) % Profiles.Length;
                _owner._driver.Profile = Profiles[_owner._profileIndex].Create();
                Refresh();
            }

            private void OnProfileChanged(UiProfile profile) => Refresh();

            private void OnInsetsChanged(SafeAreaInsets insets) => Refresh();

            private void OnScaleApplied(PanelScale scale) => Refresh();

            private void Refresh()
            {
                var layout = Ui.Root.layout;
                var profile = Ui.Profile.Profile;
                var scale = _owner._driver.Scale;
                _info.text = "Panel " + Mathf.RoundToInt(layout.width) + " x " + Mathf.RoundToInt(layout.height) + " units, "
                             + profile.SizeClass + ", " + profile.Orientation + ", input " + profile.InputMode + ". Scale "
                             + (scale.Physical ? "physical at " + scale.ReferenceDpi.ToString("0") + " dpi" : scale.PixelScale.ToString("0.00") + "x")
                             + ", " + scale.PixelsPerUnit.ToString("0.00") + " pixels per unit.";
                var insets = Ui.SafeArea.Insets;
                _insets.text = "Safe area (units): left " + insets.Left.ToString("0") + ", top " + insets.Top.ToString("0")
                               + ", right " + insets.Right.ToString("0") + ", bottom " + insets.Bottom.ToString("0") + ".";
                _notchButton.text = "Fake notch: " + (_owner._fakeNotch ? "on" : "off");
                _touchButton.text = "Touch targets: " + (Ui.Root.parent.ClassListContains(TouchClass) ? "on" : "off");
                _scaleButton.text = "UI size: " + Mathf.RoundToInt(UserScales[_owner._userScaleIndex] * 100f) + "%";
                _profileButton.text = "Profile: " + Profiles[_owner._profileIndex].Name;
            }

            private static Label DeviceRow(in (string Name, bool Handheld, DeviceClass Device, float Dpi, int Width, int Height, int Profile) device)
            {
                var profile = Profiles[device.Profile].Create();
                var input = new PanelScaleInput(device.Handheld, device.Device, device.Dpi, device.Width, device.Height, 1f);
                var scale = PanelScalePolicy.Compute(profile, input);
                var text = device.Name + " (" + device.Width + " x " + device.Height + " px, " + device.Dpi.ToString("0") + " dpi): "
                           + (scale.Physical ? "physical size, " : scale.PixelScale.ToString("0.00") + "x, ")
                           + scale.ShortSideUnits.ToString("0") + " units on the short side.";
                var label = new Label(text);
                label.AddToClassList("ds-body-2");
                return label;
            }

            private static VisualElement Card()
            {
                var card = new VisualElement();
                card.AddToClassList("fdp-sample-card");
                return card;
            }

            private static Label Heading(string text, string style)
            {
                var label = new Label(text);
                label.AddToClassList(style);
                return label;
            }

            private static Button AddButton(VisualElement parent, Action onClick)
            {
                var button = new Button(onClick);
                button.AddToClassList("ds-btn");
                button.AddToClassList("ds-btn--secondary");
                parent.Add(button);
                return button;
            }
        }
    }
}
