using System;
using FutureDigitalPlus.UIKit.Controls;
using FutureDigitalPlus.UIKit.Foundation;
using FutureDigitalPlus.UIKit.Interaction;
using UnityEngine;
using UnityEngine.UIElements;

namespace FutureDigitalPlus.UIKit.Samples.GamepadMenu
{
    /// <summary>
    /// A menu that works with a gamepad, a keyboard, a mouse and a finger alike: the pattern for a console, a handheld, a TV or a desktop game. Put it on an
    /// empty GameObject in an empty scene and press Play. The sample builds a main menu and an options page on a <see cref="UiShell"/> and shows, in this order:
    /// the focus ring that appears only while a keyboard or gamepad is in use, focus that lands on the first control of every page and returns to the
    /// button that opened a dialog, back handling (Escape, gamepad B, the Android back button) through <see cref="BackKeySource"/>, a <see cref="SelectField"/> and
    /// a <see cref="TabBar"/> that follow the D-pad, an <see cref="Activate"/> registration that makes a plain element behave like a button, and the
    /// title-safe margin television screens need.
    /// <para>
    /// No gamepad at hand? The "Input device" row pins the input mode to Keyboard, Gamepad or Mouse, so the focus ring and the prompts can be checked with a mouse.
    /// </para>
    /// </summary>
    [AddComponentMenu("FDP UI Kit/Gamepad Menu Sample")]
    public sealed class GamepadMenuSample : MonoBehaviour
    {
        private const string ThemeResource = "FutureDigitalPlus/UIKit/Samples/GamepadMenu/UnityDefaultRuntimeTheme";
        private const string MainScreen = "Main";
        private const string OptionsScreen = "Options";
        private const float TvMarginPercent = 5f;

        private PanelSettings _settings;
        private UIDocument _document;
        private InputProfile _input;
        private BackKeySource _back;
        private UiShell _shell;
        private bool _tvMargin = true;

        private void Start()
        {
            _settings = ScriptableObject.CreateInstance<PanelSettings>();
            _settings.name = "GamepadMenuPanelSettings";
            _settings.themeStyleSheet = Resources.Load<ThemeStyleSheet>(ThemeResource);
            _settings.scaleMode = PanelScaleMode.ScaleWithScreenSize;
            _settings.referenceResolution = new Vector2Int(1280, 720);
            _settings.screenMatchMode = PanelScreenMatchMode.MatchWidthOrHeight;
            _settings.match = 0.5f;

            // Inactive while configured, so the UIDocument builds its tree once with everything set.
            var go = new GameObject("Gamepad menu (UIDocument)");
            go.transform.SetParent(transform, false);
            go.SetActive(false);
            _document = go.AddComponent<UIDocument>();
            _document.panelSettings = _settings;
            go.SetActive(true);

            var root = _document.rootVisualElement;
            FoundationStyles.InstallDesignSystem(root.parent ?? root);
            ControlsStyles.Install(root);
            InteractionStyles.Install(root);

            // The input profile writes fdp-input-mouse / -touch / -keyboard / -gamepad and fdp-input-nav on the root. Focus.uss draws the ring only under fdp-input-nav.
            _input = InputProfile.Bind(root);

            _shell = new UiShell(root, new UiShellOptions
            {
                InputModeSource = new InputProfileSource(_input),
                // Television overscan: keep 5 percent of the screen free at every edge. The default is 5 percent on consoles and 0 elsewhere.
                MinimumMarginPercent = TvMarginPercent,
            });
            _shell.Catalog.Register(MainScreen, () => new MainMenu(_input, ToggleTvMargin, () => _tvMargin));
            _shell.Catalog.Register(OptionsScreen, () => new OptionsPage(ToggleTvMargin, () => _tvMargin));
            _shell.Boot(MainScreen);

            // Escape, the Android back button and gamepad B. The shell closes a dialog first, then pops a page; at the main menu nothing is left to pop,
            // so the game decides: here it asks before quitting. HandleBackPress de-duplicates the same press arriving through two sources.
            _back = new BackKeySource();
            _back.Bind(root);
            _back.Pressed += OnBack;
        }

        private void Update()
        {
            _shell?.Tick();
        }

        private void OnApplicationFocus(bool focus)
        {
            _shell?.NotifyApplicationFocus(focus);
        }

        private void OnDestroy()
        {
            if (_back != null)
            {
                _back.Pressed -= OnBack;
                _back.Dispose();
            }
            _shell?.Dispose();
            _input?.Dispose();
            if (_settings != null)
                Destroy(_settings);
        }

        private void OnBack()
        {
            if (_shell.HandleBackPress())
                return;
            _shell.Context.Dialogs.Confirm(
                UiText.FromLiteral("Quit?"),
                UiText.FromLiteral("This sample only closes the dialog. A game would leave the application here."),
                confirmed => { });
        }

        private void ToggleTvMargin()
        {
            _tvMargin = !_tvMargin;
            _shell.Context.SafeArea.MinimumMarginPercent = _tvMargin ? TvMarginPercent : 0f;
            _shell.Context.SafeArea.Invalidate();
        }

        // ---- pages -----------------------------------------------------------------------------------------------

        /// <summary>The first page: three rows, a card made focusable with <see cref="Activate"/>, and a line that names the input device in use.</summary>
        private sealed class MainMenu : UiScreen
        {
            private static readonly InputDeviceKind?[] Pins =
            {
                null, InputDeviceKind.Keyboard, InputDeviceKind.Gamepad, InputDeviceKind.Pointer,
            };

            private static readonly string[] PinNames = { "Follow the player", "Keyboard", "Gamepad", "Mouse" };

            private readonly InputProfile _input;
            private readonly Action _toggleMargin;
            private readonly Func<bool> _margin;
            private Label _device;
            private ListRow _pin;
            private ListRow _marginRow;
            private int _pinIndex;

            public MainMenu(InputProfile input, Action toggleMargin, Func<bool> margin)
            {
                _input = input;
                _toggleMargin = toggleMargin;
                _margin = margin;
            }

            protected override void OnBuild()
            {
                var column = new VisualElement();
                column.style.flexGrow = 1;
                column.style.justifyContent = Justify.Center;
                column.style.alignSelf = Align.Center;
                column.style.width = 480;
                Root.Add(column);

                var title = new Label("Gamepad menu");
                title.AddToClassList("ds-h1");
                column.Add(title);

                column.Add(Row("Play", "Starts nothing; this is a sample.", () => Ui.Toasts.ShowRaw("Pretend the game started.", ToastKind.Success)));
                column.Add(Row("Options", "A TabBar, a SelectField and a slider.", () => Ui.Router.Push(OptionsScreen)));

                // A plain element is not focusable and ignores Submit. Activate.Register fixes both: click, Enter and gamepad A call the same action.
                var card = new VisualElement();
                card.AddToClassList("ds-card");
                card.style.marginTop = 12;
                card.Add(new Label("A card, not a button") { pickingMode = PickingMode.Ignore });
                Activate.Register(card, () => Ui.Toasts.ShowRaw("The card was activated.", ToastKind.Info));
                column.Add(card);

                _pin = Row("Input device", PinNames[0], CyclePin);
                _pin.style.marginTop = 12;
                column.Add(_pin);
                _marginRow = Row("Title-safe margin", _margin() ? "On" : "Off", () =>
                {
                    _toggleMargin();
                    _marginRow.Value = _margin() ? "On" : "Off";
                });
                column.Add(_marginRow);

                _device = new Label();
                _device.AddToClassList("ds-body-2");
                _device.style.marginTop = 16;
                column.Add(_device);
                ShowDevice(_input.Mode);
                _input.ModeChanged += ShowDevice;
                OnDisposeDo(() => _input.ModeChanged -= ShowDevice);
            }

            protected override void OnShow(object args)
            {
                // Focus the first control, one frame later, when the page is laid out and displayed.
                Root.schedule.Execute(() => UiFocus.FocusFirst(Root));
            }

            private ListRow Row(string title, string value, Action activated)
            {
                var row = new ListRow { Title = title, Value = value, ShowChevron = false };
                row.Clicked += activated;
                return row;
            }

            private void CyclePin()
            {
                _pinIndex = (_pinIndex + 1) % Pins.Length;
                _input.Force(Pins[_pinIndex]);
                _pin.Value = PinNames[_pinIndex];
            }

            private void ShowDevice(InputDeviceKind kind)
            {
                string hint;
                switch (kind)
                {
                    case InputDeviceKind.Gamepad: hint = "D-pad or stick to move, A to select, B to go back."; break;
                    case InputDeviceKind.Keyboard: hint = "Arrow keys or Tab to move, Enter to select, Escape to go back."; break;
                    case InputDeviceKind.Touch: hint = "Tap to select. The Android back button goes back."; break;
                    default: hint = "Click to select. Press an arrow key or a gamepad button to show the focus ring."; break;
                }
                _device.text = "Input: " + kind + ". " + hint;
            }
        }

        /// <summary>The second page: a segmented <see cref="TabBar"/> that follows the left and right D-pad, and a <see cref="SelectField"/> and a slider on each tab.</summary>
        private sealed class OptionsPage : UiScreen
        {
            private readonly Action _toggleMargin;
            private readonly Func<bool> _margin;

            public OptionsPage(Action toggleMargin, Func<bool> margin)
            {
                _toggleMargin = toggleMargin;
                _margin = margin;
            }

            protected override void OnBuild()
            {
                var bar = new TopBar { Title = "Options" };
                bar.BackClicked += () => Ui.Router.Pop();
                Root.Add(bar);

                var body = new VisualElement();
                body.style.alignSelf = Align.Center;
                body.style.width = 520;
                body.style.marginTop = 16;
                Root.Add(body);

                var tabs = new TabBar { Skin = TabBarSkin.Segmented };
                tabs.AddTab("video", "Video");
                tabs.AddTab("audio", "Audio");
                body.Add(tabs);

                var video = new VisualElement();
                var quality = new SelectField { Choices = new[] { "Low", "Medium", "High" }, value = 1 };
                quality.style.marginTop = 12;
                quality.RegisterValueChangedCallback(e => Ui.Toasts.ShowRaw("Quality: " + quality.SelectedText));
                video.Add(quality);
                var margin = new ListRow { Title = "Title-safe margin", Value = _margin() ? "On" : "Off", ShowChevron = false };
                margin.style.marginTop = 12;
                margin.Clicked += () =>
                {
                    _toggleMargin();
                    margin.Value = _margin() ? "On" : "Off";
                };
                video.Add(margin);

                var audio = new VisualElement { style = { display = DisplayStyle.None } };
                var volume = new Slider("Volume", 0f, 100f) { value = 70f };
                volume.style.marginTop = 12;
                audio.Add(volume);

                body.Add(video);
                body.Add(audio);

                tabs.RegisterValueChangedCallback(e =>
                {
                    video.style.display = e.newValue == "video" ? DisplayStyle.Flex : DisplayStyle.None;
                    audio.style.display = e.newValue == "audio" ? DisplayStyle.Flex : DisplayStyle.None;
                });
                tabs.value = "video";
            }

            protected override void OnShow(object args)
            {
                Root.schedule.Execute(() => UiFocus.FocusFirst(Root));
            }
        }

        // ---- the one seam between two modules --------------------------------------------------------------------

        /// <summary>Tells the shell which device the <see cref="InputProfile"/> sees, so the shell's own layout classes and the profile's agree.</summary>
        private sealed class InputProfileSource : IUiInputModeSource
        {
            private readonly InputProfile _profile;

            public InputProfileSource(InputProfile profile)
            {
                _profile = profile;
                _profile.ModeChanged += kind => ModeChanged?.Invoke(Map(kind));
            }

            public UiInputMode Mode => Map(_profile.Mode);

            public event Action<UiInputMode> ModeChanged;

            private static UiInputMode Map(InputDeviceKind kind)
            {
                switch (kind)
                {
                    case InputDeviceKind.Touch: return UiInputMode.Touch;
                    case InputDeviceKind.Keyboard: return UiInputMode.Keyboard;
                    case InputDeviceKind.Gamepad: return UiInputMode.Gamepad;
                    default: return UiInputMode.Pointer;
                }
            }
        }
    }
}
