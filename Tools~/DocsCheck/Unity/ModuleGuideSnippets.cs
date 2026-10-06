// Snippets copied verbatim from the module guides (usings hoisted). A compile error here means a guide drifted from the code.
using System;
using UnityEngine;
using UnityEngine.UIElements;
using FutureDigitalPlus.UIKit;
using FutureDigitalPlus.UIKit.Foundation;
using FutureDigitalPlus.UIKit.Interaction;
using FutureDigitalPlus.UIKit.Language;
using FutureDigitalPlus.UIKit.Hud;
using FutureDigitalPlus.UIKit.Controls;
using FutureDigitalPlus.UIKit.Gameplay;
using FutureDigitalPlus.UIKit.DesignSystem.Theme.Data;
using FutureDigitalPlus.UIKit.DesignSystem.Theme.Applier;
using FutureDigitalPlus.UIKit.DesignSystem.Behaviour;
using FutureDigitalPlus.UIKit.DesignSystem.Theme.Palette;
using FutureDigitalPlus.UIKit.DesignSystem.Typography;
using FutureDigitalPlus.UIKit.Extras;

namespace DocsCheck.Existing
{
    // GettingStarted.md section 4b
    public sealed class Boot : MonoBehaviour
    {
        UiShell _shell;

        void OnEnable()
        {
            var root = GetComponent<UIDocument>().rootVisualElement;
            FoundationStyles.InstallDesignSystem(root.parent);     // the ds-* look, on the panel root
            _shell = new UiShell(root);
            _shell.Catalog.Register("Home", () => new HomeScreen());   // a class derived from UiScreen
            _shell.Boot("Home");
        }

        void Update() => _shell.Tick();                             // safe area and back key
        void OnDisable() => _shell?.Dispose();
    }
    sealed class HomeScreen : UiScreen { public override string UxmlName => "Home"; }
}

namespace DocsCheck.Foundation2
{
    // Foundation/README.md quick start
    public sealed class Boot : MonoBehaviour
    {
        UiShell _shell;

        void OnEnable()
        {
            var document = GetComponent<UIDocument>();
            FoundationStyles.InstallDesignSystem(document.rootVisualElement.parent); // optional: the ds-* look on the panel root; skip it for your own skin
            _shell = new UiShell(document.rootVisualElement, new UiShellOptions
            {
                Assets = new ResourcesAssetSource("Screens"),                      // Resources/Screens/Home.uxml and Home.uss
                StateStore = new PlayerPrefsUiStateStore("mygame"),                // the screen stack survives a restart
            });
            _shell.Catalog.Register("Home", () => new HomeScreen());
            _shell.Catalog.Register("Settings", () => new SettingsScreen());
            PauseMenu.Register(_shell.Catalog);                                    // optional ready pause menu
            _shell.Boot("Home");
        }

        void Update() => _shell.Tick();                                            // safe area and back key
        void OnApplicationFocus(bool focus) => _shell?.NotifyApplicationFocus(focus);
        void OnApplicationPause(bool paused) => _shell?.NotifyApplicationPause(paused);
        void OnDisable() => _shell?.Dispose();
    }

    sealed class HomeScreen : UiScreen
    {
        public override string UxmlName => "Home";
        protected override void OnBuild()
        {
            OnClick(Q<Button>("play"), () => Ui.Router.Push("Settings"));
            OnClick(Q<Button>("reset"), () => Ui.Dialogs.Confirm(UiText.FromLiteral("Reset?"), UiText.FromLiteral("All progress is lost."),
                yes => { if (yes) Ui.Toasts.ShowRaw("Done", ToastKind.Success); }));
        }
    }
    sealed class SettingsScreen : UiScreen { }

    static class LoadingSnip
    {
        public static async System.Threading.Tasks.Task Run(UiContext ui)
        {
            using (ui.Loading.Begin(UiText.FromLiteral("Loading"))) { await System.Threading.Tasks.Task.Yield(); }
        }
    }
}

namespace DocsCheck.Interaction2
{
    static class S
    {
        public static void Quick(UIDocument uiDocument, AccessibilityOptions options, Action router, VisualElement rowElement, Action OpenDetails, InputProfile hudInput)
        {
            var root = uiDocument.rootVisualElement;
            InteractionStyles.Install(root);                      // Focus.uss and A11y.uss, below the design system base sheet
            var input = InputProfile.Bind(root);                  // writes fdp-input-* classes, raises ModeChanged
            var a11y  = AccessibilityApplier.Bind(root, options); // writes fdp-reduce-motion, fdp-text-scale-N, fdp-bold-text, fdp-high-contrast
            var back  = new BackKeySource();                      // Escape, Android back, gamepad B, NavigationCancel
            back.Bind(root);
            back.Pressed += () => router.Invoke();

            // a clickable plain element that keyboard and gamepad can reach
            var row = Activate.Register(rowElement, () => OpenDetails());

            // a HUD document that must not react to WASD or the D-pad
            hudInput.NavigationEnabled = false;
        }
    }
}

namespace DocsCheck.Language2
{
    static class S
    {
        public static void Quick(Button playButton, Toggle toggle, VisualElement panelRoot, VisualElement documentRoot)
        {
            // 1. A text source. No package: an in-memory one (code, CSV or JSON). Unity Localization: nothing to do, see below.
            var texts = new DictionaryTextSource("en");
            texts.LoadCsv("table,key,en,de,ar\nUI,PLAY,Play,Spielen,العب\n");
            UiLocalization.Default.Backend = texts;

            // 2. Bind text. The element follows every language change while it is on a panel, and unsubscribes when it leaves.
            playButton.BindText(UiText.FromKey("PLAY").WithFallback("Play"));     // Label, Button, any TextElement
            toggle.BindText(UiText.FromKey("SOUND"), text => toggle.label = text); // any other text property

            // 3. Put the language on the root, so stylesheets can react (fdp-lang-ar, fdp-rtl) and install the mirrored layout.
            LanguageStyles.Install(panelRoot);                  // after the design system base sheet
            LocaleClassApplier.Bind(documentRoot);              // follows UiLocalization.Default until disposed

            // 4. Change the language.
            texts.SetLocale("ar");                              // raises Changed: bound text and root classes update
        }

        public static void Fonts(UIDocument document, string localeCode, string newLocale, DsFontFamily family, string textOfTheNextScreen)
        {
            var panel = DsOsFonts.UseRuntimePanelSettings(document);   // a runtime copy: the project asset is never changed
            var osFonts = new DsOsFonts();
            osFonts.Install(panel, localeCode);                          // adds the system fonts for the language after the game's own
            // when the language changes:
            DsFonts.ApplyLocale(newLocale, osFonts, family, textOfTheNextScreen);
            // when the panel goes away:
            osFonts.Dispose();
        }
    }
}

namespace DocsCheck.Hud2
{
    static class S
    {
        public static void Setup(UIDocument uiDocument)
        {
            var root = uiDocument.rootVisualElement;
            HudStyles.Install(root);                     // once per panel, after the design system base sheet (optional)

            var zones = new HudZones { TitleSafePercent = 0 };   // 5 on a TV
            root.Add(zones);                                     // pass-through: the game still gets pointer events

            var health = zones.Add(HudZone.TopLeft, new MeterBar { Size = MeterBarSize.Large, LabelMode = MeterLabelMode.ValueAndMax });
            health.Set(184f, 240f);                              // animates, shows the damage trail, flips to warning and danger

            var coins = zones.Add(HudZone.TopRight, new CurrencyLabel());
            coins.Value = 1250;                                  // rolls up, shows "+250" for a moment

            var dash = zones.Add(HudZone.BottomCenter, new CooldownIcon());
            dash.StartCooldown(6f);                              // the sweep runs, the seconds show, a flash marks "ready"
            dash.Ready += () => Debug.Log("dash ready");
        }
    }
}

namespace DocsCheck.Controls2
{
    static class S
    {
        public static void Setup(UIDocument uiDocument)
        {
            var root = uiDocument.rootVisualElement;
            ControlsStyles.Install(root);     // once per panel, on the document root or an ancestor of the controls
        }

        static void ShowPage(string s) { }
        static void Handle(string s) { }

        public static void Examples(VisualElement root, VisualElement row, Sprite gemSprite)
        {
            // Tabs with badges
            var tabs = new TabBar();
            tabs.AddTab("home", "Home"); tabs.AddTab("shop", "Shop");
            tabs.RegisterValueChangedCallback(e => ShowPage(e.newValue));
            var badges = new BadgeTree();
            tabs.BindBadges(badges);
            badges.SetCount("shop/offers", 2);          // the Shop tab shows 2; visiting the tab clears it

            // Context menu on a row
            PopupMenu.Attach(row, () => new[] { new MenuEntry("rename", "Rename"), MenuEntry.Separator(), new MenuEntry("delete", "Delete") },
                               item => Handle(item.Id));

            // Reward popup
            RewardPopup.Show(root, "Level complete", new[] { new RewardItem("coins", "Coins", "x250"), new RewardItem("gem", "Gem", "x3", gemSprite, "epic") });
        }
    }
}

namespace DocsCheck.Gameplay2
{
    static class Audio { public static void Play(string s) { } }
    static class S
    {
        static Background IconFor(string id) => default;
        static string RarityFor(string id) => null;
        static void Use(ItemStack s) { }

        public static void Setup(UIDocument uiDocument, VisualElement root, System.Collections.Generic.IReadOnlyList<MenuEntry> items, Action<MenuEntry> onInvoke)
        {
            GameplayStyles.Install(uiDocument.rootVisualElement);     // once per panel

            var bag = new InventoryModel(24, id => id == "potion" ? 10 : 1);   // 24 slots, stack limit per item id
            bag.Add(new ItemStack("potion", 14));                              // fills stacks first, returns what did not fit

            var grid = new SlotGrid { Columns = 6, SlotSize = 56, Spacing = 4 };
            grid.Bind(bag, id => new ItemVisual(IconFor(id), RarityFor(id)));   // icon and rarity per item id
            root.Add(grid);

            grid.Moved += move => Audio.Play("drop");
            grid.MoveRejected += move => Audio.Play("nope");                   // move.Result.Reason says why
            grid.SlotActivated += slot => Use(bag[slot.Index]);
            grid.SlotContext += (slot, position) => PopupMenu.Show(slot, items, position, onInvoke);  // Controls module, optional
        }
    }
}
