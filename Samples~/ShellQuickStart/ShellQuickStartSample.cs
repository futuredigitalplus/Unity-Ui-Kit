using System;
using FutureDigitalPlus.UIKit.Controls;
using FutureDigitalPlus.UIKit.Foundation;
using FutureDigitalPlus.UIKit.Language;
using UnityEngine;
using UnityEngine.UIElements;

namespace FutureDigitalPlus.UIKit.Samples.ShellQuickStart
{
    /// <summary>
    /// The plumbing every game with more than one screen needs, in one small file. Put it on an empty GameObject in an empty scene and press Play. It builds a
    /// <see cref="UiShell"/> (screen router, dialogs, toasts, loading overlay, safe area, back handling) with two pages: a home page with a <see cref="TabBar"/>
    /// and buttons that open a confirmation dialog, a toast and a two-second loading overlay, and a settings page, reached with a push and left with the back
    /// button, the top bar arrow, Escape or the Android back button. Every text is a key resolved by the Language module, so the Language row on the settings page
    /// switches English and German while the pages stay open.
    /// </summary>
    [AddComponentMenu("Future Digital Plus UI Kit/Shell Quick Start Sample")]
    public sealed class ShellQuickStartSample : MonoBehaviour
    {
        private const string ThemeResource = "FutureDigitalPlus/UIKit/Samples/ShellQuickStart/UnityDefaultRuntimeTheme";
        private const string HomeScreen = "Home";
        private const string SettingsScreen = "Settings";

        private readonly DictionaryTextSource _texts = new DictionaryTextSource("en");
        private PanelSettings _settings;
        private UiLocalization _localization;
        private UiShell _shell;

        private void Start()
        {
            LoadTexts();
            // A localization of its own (not the process-wide default), so this sample leaves nothing behind when it is destroyed.
            _localization = new UiLocalization(_texts, new[] { DictionaryTextSource.DefaultTable });
            var text = new UiTextSource(_localization.Resolve);
            _localization.Changed += text.NotifyChanged;

            _settings = ScriptableObject.CreateInstance<PanelSettings>();
            _settings.name = "ShellQuickStartPanelSettings";
            _settings.themeStyleSheet = Resources.Load<ThemeStyleSheet>(ThemeResource);
            _settings.scaleMode = PanelScaleMode.ScaleWithScreenSize;
            _settings.referenceResolution = new Vector2Int(1080, 1920);
            _settings.screenMatchMode = PanelScreenMatchMode.MatchWidthOrHeight;
            _settings.match = 0.5f;

            // Inactive while configured, so the UIDocument builds its tree once with everything set.
            var go = new GameObject("Shell quick start (UIDocument)");
            go.transform.SetParent(transform, false);
            go.SetActive(false);
            var document = go.AddComponent<UIDocument>();
            document.panelSettings = _settings;
            go.SetActive(true);

            var root = document.rootVisualElement;
            // The design system sheet goes on the panel root; the shell adds its own sheets. Controls has one more.
            FoundationStyles.InstallDesignSystem(root.parent ?? root);
            ControlsStyles.Install(root);
            LanguageStyles.Install(root.parent ?? root);
            // Right-to-left languages mirror the layout once the locale says so (this sample ships English and German only).
            LocaleClassApplier.Bind(root, _localization);

            _shell = new UiShell(root, new UiShellOptions { Text = text });
            _shell.Catalog.Register(HomeScreen, () => new Home());
            _shell.Catalog.Register(SettingsScreen, () => new Settings(_texts, _localization));
            _shell.Boot(HomeScreen);
        }

        private void Update()
        {
            // Polls the safe area and the back key; cheap, so every frame is fine.
            _shell?.Tick();
        }

        private void OnApplicationFocus(bool focus)
        {
            _shell?.NotifyApplicationFocus(focus);
        }

        private void OnApplicationPause(bool paused)
        {
            _shell?.NotifyApplicationPause(paused);
        }

        private void OnDestroy()
        {
            _shell?.Dispose();
            if (_settings != null)
                Destroy(_settings);
        }

        // ---- pages -----------------------------------------------------------------------------------------------

        private static UiText Key(string key, string fallback) => UiText.FromKey(key).WithFallback(fallback);

        private sealed class Home : UiScreen
        {
            private TabBar _tabs;
            private Label _content;

            protected override void OnBuild()
            {
                var bar = new TopBar { ShowBack = false };
                Ui.SetText(bar.TitleLabel, Key("HOME-TITLE", "Shell quick start"));
                var settings = new Button(() => Ui.Router.Push(SettingsScreen));
                settings.AddToClassList("ds-btn");
                settings.AddToClassList("ds-btn--ghost");
                Ui.SetText(settings, Key("SETTINGS", "Settings"));
                bar.End.Add(settings);
                Root.Add(bar);

                // A tab bar with one item selected, a badge on the third and keyboard, gamepad and pointer selection. The labels follow the locale.
                _tabs = new TabBar();
                _tabs.AddTab("play", Resolve("TAB-PLAY", "Play"));
                _tabs.AddTab("collection", Resolve("TAB-COLLECTION", "Collection"));
                _tabs.AddTab("shop", Resolve("TAB-SHOP", "Shop"));
                _tabs.SetBadge("shop", new BadgeValue(3, false));
                _tabs.RegisterValueChangedCallback(e => ShowTab(e.newValue));
                Root.Add(_tabs);

                _content = new Label();
                _content.AddToClassList("ds-body-1");
                _content.style.marginTop = 24;
                _content.style.marginLeft = _content.style.marginRight = 16;
                Root.Add(_content);

                var actions = new VisualElement();
                actions.style.marginTop = 24;
                actions.style.marginLeft = actions.style.marginRight = 16;
                actions.Add(ActionButton("BTN-TOAST", "Show a toast", ShowToast));
                actions.Add(ActionButton("BTN-DIALOG", "Ask a question", AskQuestion));
                actions.Add(ActionButton("BTN-LOADING", "Load for two seconds", ShowLoading));
                Root.Add(actions);

                // Labels that were built from a key keep up with the locale by themselves; the tab labels are plain strings, so refresh them.
                Ui.Text.Changed += RefreshTabLabels;
                OnDisposeDo(() => Ui.Text.Changed -= RefreshTabLabels);
                _tabs.value = "play";
                ShowTab("play");
            }

            protected override void OnShow(object args)
            {
                // Focus the first control one frame later (the page is displayed by then), so a gamepad can start at once.
                Root.schedule.Execute(() => UiFocus.FocusFirst(Root));
            }

            private string Resolve(string key, string fallback) => Ui.Resolve(Key(key, fallback));

            private void RefreshTabLabels()
            {
                _tabs.SetTabLabel("play", Resolve("TAB-PLAY", "Play"));
                _tabs.SetTabLabel("collection", Resolve("TAB-COLLECTION", "Collection"));
                _tabs.SetTabLabel("shop", Resolve("TAB-SHOP", "Shop"));
            }

            private void ShowTab(string id)
            {
                switch (id)
                {
                    case "collection": Ui.SetText(_content, Key("CONTENT-COLLECTION", "Everything you own.")); break;
                    case "shop": Ui.SetText(_content, Key("CONTENT-SHOP", "Three new offers.")); break;
                    default: Ui.SetText(_content, Key("CONTENT-PLAY", "Pick a game to play.")); break;
                }
            }

            private Button ActionButton(string key, string fallback, Action onClick)
            {
                var button = new Button(onClick);
                button.AddToClassList("ds-btn");
                button.AddToClassList("ds-btn--primary");
                button.AddToClassList("ds-btn--block");
                button.style.marginBottom = 12;
                Ui.SetText(button, Key(key, fallback));
                return button;
            }

            private void ShowToast()
            {
                Ui.Toasts.Show(Key("TOAST-SAVED", "Saved."), ToastKind.Success);
            }

            private void AskQuestion()
            {
                // The dialog queue shows one dialog at a time, traps focus inside it, closes on back and gives focus back to this button.
                Ui.Dialogs.Confirm(
                    Key("DIALOG-TITLE", "Start a new game?"),
                    Key("DIALOG-MESSAGE", "Your current game will be lost."),
                    confirmed => Ui.Toasts.Show(confirmed ? Key("TOAST-STARTED", "New game started.") : Key("TOAST-CANCELLED", "Cancelled."), ToastKind.Info),
                    destructive: true,
                    dedupeKey: "new-game");
            }

            private void ShowLoading()
            {
                // The overlay appears after a short delay, so work that finishes at once never flashes it; it blocks input while it is up.
                var token = Ui.Loading.Begin(Key("LOADING", "Loading..."));
                Root.schedule.Execute(() => Ui.Loading.End(token)).StartingIn(2000);
            }
        }

        private sealed class Settings : UiScreen
        {
            private readonly DictionaryTextSource _texts;
            private readonly UiLocalization _localization;
            private ListRow _language;

            public Settings(DictionaryTextSource texts, UiLocalization localization)
            {
                _texts = texts;
                _localization = localization;
            }

            protected override void OnBuild()
            {
                var bar = new TopBar();
                Ui.SetText(bar.TitleLabel, Key("SETTINGS", "Settings"));
                bar.BackClicked += () => Ui.Router.Pop();
                Root.Add(bar);

                _language = new ListRow();
                Ui.SetText(_language.TitleLabel, Key("LANGUAGE", "Language"));
                _language.Clicked += SwitchLanguage;
                Root.Add(_language);
                ShowLanguage();

                var about = new ListRow();
                Ui.SetText(about.TitleLabel, Key("ABOUT", "About"));
                about.Clicked += () => Ui.Dialogs.Alert(Key("ABOUT", "About"), Key("ABOUT-TEXT", "A sample built with the Future Digital Plus UI Kit."));
                Root.Add(about);
            }

            protected override void OnShow(object args)
            {
                Root.schedule.Execute(() => UiFocus.FocusFirst(Root));
            }

            private void SwitchLanguage()
            {
                _texts.SetLocale(_localization.CurrentLocale == "en" ? "de" : "en");
                ShowLanguage();
            }

            private void ShowLanguage() => _language.Value = _localization.CurrentLocale == "en" ? "English" : "Deutsch";
        }

        // ---- texts -----------------------------------------------------------------------------------------------

        private void LoadTexts()
        {
            var table = DictionaryTextSource.DefaultTable;
            // English is also the fallback of the key, so these lines are optional; they are here so the table is complete.
            foreach (var entry in new[]
            {
                ("HOME-TITLE", "Shell quick start", "Shell-Schnellstart"),
                ("SETTINGS", "Settings", "Einstellungen"),
                ("TAB-PLAY", "Play", "Spielen"),
                ("TAB-COLLECTION", "Collection", "Sammlung"),
                ("TAB-SHOP", "Shop", "Laden"),
                ("CONTENT-PLAY", "Pick a game to play.", "Wähle ein Spiel."),
                ("CONTENT-COLLECTION", "Everything you own.", "Alles, was du besitzt."),
                ("CONTENT-SHOP", "Three new offers.", "Drei neue Angebote."),
                ("BTN-TOAST", "Show a toast", "Hinweis zeigen"),
                ("BTN-DIALOG", "Ask a question", "Eine Frage stellen"),
                ("BTN-LOADING", "Load for two seconds", "Zwei Sekunden laden"),
                ("TOAST-SAVED", "Saved.", "Gespeichert."),
                ("TOAST-STARTED", "New game started.", "Neues Spiel gestartet."),
                ("TOAST-CANCELLED", "Cancelled.", "Abgebrochen."),
                ("DIALOG-TITLE", "Start a new game?", "Neues Spiel starten?"),
                ("DIALOG-MESSAGE", "Your current game will be lost.", "Dein aktuelles Spiel geht verloren."),
                ("LOADING", "Loading...", "Wird geladen ..."),
                ("LANGUAGE", "Language", "Sprache"),
                ("ABOUT", "About", "Über"),
                ("ABOUT-TEXT", "A sample built with the Future Digital Plus UI Kit.", "Ein Beispiel mit dem Future Digital Plus UI Kit."),
                ("FDP-OK", "OK", "OK"),
                ("FDP-CANCEL", "Cancel", "Abbrechen"),
            })
            {
                _texts.Add("en", table, entry.Item1, entry.Item2);
                _texts.Add("de", table, entry.Item1, entry.Item3);
            }
        }
    }
}
