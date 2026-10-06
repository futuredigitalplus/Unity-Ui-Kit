# Common recipes

Copy-paste answers to the things every game does with its UI. Each recipe is a complete class: it compiles as written and was run in Unity 6000.3.25f1
(`Tools~/DocsCheck` extracts every block of this page into a scratch project, compiles it and runs recipes 0 to 9 on a real panel; recipe 10 is compiled there and run by the Bootstrap tests; it passed in a project with none of the optional packages and in one with
Input System only, Localization, Addressables and uGUI), it uses only the modules it names, and it ends with the one thing you will want to change.
No real input device or device screen drove them; the [samples](Samples.md) are the longer running versions.

| # | You want | Modules |
|---|---|---|
| [0](#0-one-bootstrap-for-the-whole-kit) | one component that starts the kit in any scene | DesignSystem, Foundation, Interaction, Language |
| [1](#1-a-themed-screen) | a themed screen from UXML | + the shell |
| [2](#2-a-dialog) | confirm, alert, choice, text input, your own dialog | Foundation |
| [3](#3-a-toast-and-a-loading-overlay) | a toast, a loading overlay | Foundation |
| [4](#4-gamepad-and-keyboard-navigation) | a menu a gamepad can drive | Interaction, Foundation, Controls |
| [5](#5-a-hud-bar) | health bar, cooldown and coins over the game | Hud |
| [6](#6-an-inventory-grid) | a backpack with drag and drop | Gameplay, Controls |
| [7](#7-localization-hook) | texts that follow the language | Language |
| [8](#8-a-brand-theme) | your colours, switchable at run time | DesignSystem |
| [9](#9-the-same-ui-on-phone-tablet-and-desktop) | one UI on phone, tablet, desktop and TV | Foundation |
| [10](#10-the-ready-made-host) | recipe 0 without writing it: a component of the kit | Bootstrap |

Before any recipe: a scene with a `UIDocument` whose **Panel Settings** has a *Theme Style Sheet* (Assets > Create > UI Toolkit > Panel Settings Asset, then pick Unity's default runtime theme;
see [GettingStarted.md](GettingStarted.md) step 1) and *Scale Mode* = *Scale With Screen Size*. Without a theme the document has no height and text no font.
Assembly references for your own asmdef: [ChooseModules.md](ChooseModules.md) section 3.

---

## 0. One bootstrap for the whole kit

The modules do not reference each other (so a game can take one), which means the *joins* are yours: the input device to the shell, the back key to the shell,
the accessibility settings to the motion hooks, the language to the root classes. This component does all of them once; recipes 1 to 9 use it.
Put it on the GameObject of the `UIDocument`, assign the document, press Play. Drop the lines (and the assembly references) of the modules you do not use; as written it needs `FutureDigitalPlus.UIKit.` + `DesignSystem`, `Animation`,
`Foundation.Core`, `Foundation`, `Interaction.Core`, `Interaction`, `Language`, `Hud.Core`, `Hud`, `Controls.Core`, `Controls`, `Gameplay.Core` and `Gameplay` in your asmdef.

```csharp
using System;
using FutureDigitalPlus.UIKit.Animation;
using FutureDigitalPlus.UIKit.Controls;
using FutureDigitalPlus.UIKit.DesignSystem.Theme.Applier;
using FutureDigitalPlus.UIKit.DesignSystem.Theme.Data;
using FutureDigitalPlus.UIKit.Foundation;
using FutureDigitalPlus.UIKit.Gameplay;
using FutureDigitalPlus.UIKit.Hud;
using FutureDigitalPlus.UIKit.Interaction;
using FutureDigitalPlus.UIKit.Language;
using UnityEngine;
using UnityEngine.UIElements;

public sealed class GameUi : MonoBehaviour
{
    [SerializeField] UIDocument document;       // Panel Settings: a Theme Style Sheet and Scale With Screen Size
    [SerializeField] string startTheme = "dark"; // "dark" or "light", or a name you register (recipe 8)

    public UiShell Shell { get; private set; }
    public InputProfile Profile { get; private set; }
    public ThemeManager Themes { get; } = new ThemeManager();

    BackKeySource _back;
    AccessibilityApplier _access;
    IDisposable _locale;

    void OnEnable()
    {
        var root = document.rootVisualElement;

        // 1. Styles. The design system sheet goes on the PANEL root, everything else on the document root (see GettingStarted, "Why no <Style>").
        FoundationStyles.InstallDesignSystem(root.parent);
        InteractionStyles.Install(root);
        ControlsStyles.Install(root);
        HudStyles.Install(root);
        GameplayStyles.Install(root);
        LanguageStyles.Install(root.parent);

        // 2. Theme. ThemeManager dresses every attached root and switches by name; recipe 8 adds a brand theme and remembers the player's choice.
        Themes.Register("dark", Resources.Load<ThemeData>(ThemeData.DarkResource));
        Themes.Register("light", Resources.Load<ThemeData>(ThemeData.LightResource));
        Themes.Attach(root);
        Themes.Set(startTheme);

        // 3. Input device and accessibility become classes on the root (fdp-input-nav, fdp-reduce-motion, fdp-text-scale-125, ...).
        Profile = InputProfile.Bind(root);
        _access = AccessibilityApplier.Bind(root, new AccessibilityOptions());
        _access.Changed += OnAccessibilityChanged;
        OnAccessibilityChanged(_access.Current);
        EatkMotion.Scaler = _access.ScaleMotion;           // the Animation module's durations obey "reduce motion"

        // 4. The language becomes fdp-lang-<code> and fdp-rtl on the root; the shell resolves its texts through the same localization.
        _locale = LocaleClassApplier.Bind(root);

        // 5. The shell: screens, dialogs, toasts, loading, safe area. It reads the input device from the profile.
        Shell = new UiShell(root, new UiShellOptions
        {
            Assets = new ResourcesAssetSource("Screens"),        // Resources/Screens/<UxmlName>.uxml and .uss
            InputModeSource = new ProfileModeSource(Profile),
            Text = new UiTextSource(UiLocalization.Default.Resolve),
        });
        UiLocalization.Default.Changed += Shell.Context.Text.NotifyChanged;

        // 6. One back signal: Escape, gamepad B, the Android back button. A dialog or the loading overlay takes it first, then the screen, then the router pops.
        _back = new BackKeySource();
        _back.Bind(root);
        _back.Pressed += () => Shell.HandleBackPress();
    }

    // Your game registers its screens and boots the first one, for example from its own component after this one is enabled:
    //     gameUi.Shell.Catalog.Register("Home", () => new HomeScreen());
    //     gameUi.Shell.Boot("Home");

    void Update() => Shell?.Tick();                                              // safe area and back-key polling, a few numbers per frame
    void OnApplicationFocus(bool focus) => Shell?.NotifyApplicationFocus(focus);
    void OnApplicationPause(bool paused) => Shell?.NotifyApplicationPause(paused);

    void OnDisable()
    {
        if (Shell != null) UiLocalization.Default.Changed -= Shell.Context.Text.NotifyChanged;
        _back?.Dispose();
        Shell?.Dispose();
        _locale?.Dispose();
        _access?.Dispose();
        Profile?.Dispose();
        EatkMotion.Scaler = null;
    }

    void OnAccessibilityChanged(EffectiveAccessibility a)
    {
        // The shell and the HUD read UiAccessibility; the theme manager switches the high contrast theme.
        UiAccessibility.Set(UiAccessibility.Current
            .WithReduceMotion(a.ReduceMotion).WithHighContrast(a.HighContrast).WithTextScale(a.TextScalePercent / 100f).WithBoldText(a.BoldText));
        Themes.HighContrast = a.HighContrast;
    }

    // The shell takes its input mode from an IUiInputModeSource; Interaction's InputProfile is one, after a rename of the enum values.
    sealed class ProfileModeSource : IUiInputModeSource
    {
        readonly InputProfile _profile;

        public ProfileModeSource(InputProfile profile)
        {
            _profile = profile;
            _profile.ModeChanged += kind => ModeChanged?.Invoke(Map(kind));
        }

        public UiInputMode Mode => Map(_profile.Mode);
        public event Action<UiInputMode> ModeChanged;

        static UiInputMode Map(InputDeviceKind kind)
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
```

Notes:

- Do not put a `UIKitHost` on the same document. If you do, the facade steps aside once it sees the shell (it gives its safe-area padding back and stops writing `fdp-input-touch`), so there is nothing for it to do. The facade is for a game that has a single themed screen and no shell (Quick Start sample).
- Recipe 10 is this component as part of the kit, so you do not have to maintain it.
- `Resources/Screens` need not exist until a screen asks for UXML.
- A HUD-only game can drop the shell: keep steps 1 to 4 and put `HudZones` inside a `SafeAreaPanel` (recipe 5).

---

## 1. A themed screen

A screen is a class derived from `UiScreen` plus a UXML file named like it. The shell finds `Resources/Screens/Home.uxml` (and `Home.uss`) through the `ResourcesAssetSource` of recipe 0.
Use design system classes (`ds-card`, `ds-h2`, `ds-btn ds-btn--primary`, ...; the list is in [DesignSystem/COMPONENTS.md](DesignSystem/COMPONENTS.md)) and never reference the design system sheet from the UXML:

```xml
<!-- Assets/Resources/Screens/Home.uxml -->
<ui:UXML xmlns:ui="UnityEngine.UIElements">
    <ui:VisualElement style="flex-grow: 1; justify-content: center; align-items: center;">
        <ui:VisualElement class="ds-card" style="width: 320px;">
            <ui:Label name="title" text="Hello" class="ds-h2" />
            <ui:Button name="play" text="Play" class="ds-btn ds-btn--primary" />
            <ui:Button name="settings" text="Settings" class="ds-btn ds-btn--secondary" />
        </ui:VisualElement>
    </ui:VisualElement>
</ui:UXML>
```

```csharp
using FutureDigitalPlus.UIKit.Foundation;
using UnityEngine;
using UnityEngine.UIElements;

public sealed class HomeScreen : UiScreen
{
    public override string UxmlName => "Home";                    // Resources/Screens/Home.uxml

    protected override void OnBuild()                              // once: query, wire
    {
        Ui.SetText(Q<Label>("title"), UiText.FromKey("HOME-TITLE").WithFallback("Hello"));   // follows the language (recipe 7)
        OnClick(Q<Button>("play"), () => Ui.Router.Push("Game"));
        OnClick(Q<Button>("settings"), () => Ui.Router.Push("Settings"));
    }

    protected override void OnShow(object args)                    // every time it becomes the top screen
    {
        Root.schedule.Execute(() => UiFocus.FocusFirst(Root));     // so a gamepad has a starting point (recipe 4)
    }
}

public sealed class Startup : MonoBehaviour
{
    [SerializeField] GameUi gameUi;

    void Start()
    {
        gameUi.Shell.Catalog.Register("Home", () => new HomeScreen());
        gameUi.Shell.Boot("Home");
    }
}
```

A screen built in code leaves `UxmlName` out and fills `Root` in `OnBuild`. `Router.Push` keeps the screen below alive; `Pop`, `Replace`, `PopTo` and `ResetTo` are the other moves; the back key pops.
Without the shell: put `UIKitHost` next to the `UIDocument`, set its *Theme*, and write plain UXML (GettingStarted, "First screen in the editor").

---

## 2. A dialog

Dialogs are modal, queued by priority, close on back, and give focus back to the button that opened them. `Ui` is the shell's `UiContext`; outside a screen use `gameUi.Shell.Context`.

```csharp
using FutureDigitalPlus.UIKit.Foundation;
using UnityEngine.UIElements;

public static class Dialogs
{
    public static void Examples(UiContext ui)
    {
        // Yes / no. The callback receives true for the confirm button; back, scrim tap and Escape answer false.
        ui.Dialogs.Confirm(UiText.FromKey("RESET-TITLE").WithFallback("Reset progress?"),
                           UiText.FromKey("RESET-BODY").WithFallback("All progress is lost."),
                           yes => { if (yes) ResetProgress(); },
                           destructive: true,                  // red confirm button
                           dedupeKey: "reset");                // a double tap shows one dialog

        // One button.
        ui.Dialogs.Alert(UiText.FromLiteral("Update"), UiText.FromLiteral("A new version is available."));

        // A list of buttons; -1 means "cancelled".
        ui.Dialogs.Choice(UiText.FromLiteral("Difficulty"), UiText.FromLiteral("Pick one."),
                          new[] { UiText.FromLiteral("Easy"), UiText.FromLiteral("Normal"), UiText.FromLiteral("Hard") },
                          index => StartGame(index));

        // A text field; null means "cancelled".
        ui.Dialogs.Input(UiText.FromLiteral("Name your deck"), name => { if (name != null) Rename(name); },
                         initialValue: "My deck", maxLength: 24);

        // Your own body and buttons.
        var body = new Label("Three of your items will be sold.");
        var request = new DialogRequest { Id = "sell", Title = UiText.FromLiteral("Sell items"), Content = body, DedupeKey = "sell",
            Callback = result => { if (result.IsButton && result.ButtonIndex == 1) Sell(); } };
        request.AddButton(DialogButton.Of("FDP-CANCEL", "Cancel", DialogButtonRole.Cancel));   // index 0
        request.AddButton(DialogButton.Of("SELL", "Sell", DialogButtonRole.Destructive));      // index 1
        ui.Dialogs.Show(request);
    }

    static void ResetProgress() { }
    static void StartGame(int difficulty) { }
    static void Rename(string name) { }
    static void Sell() { }
}
```

The default button texts use the keys `FDP-OK`, `FDP-CANCEL`, `FDP-YES`, `FDP-NO` with English fallbacks: add them to your text table (recipe 7) to translate them.
A UXML template named `DialogFrame` in the shell's asset source replaces the frame.

---

## 3. A toast and a loading overlay

```csharp
using System.Threading.Tasks;
using FutureDigitalPlus.UIKit.Foundation;

public static class Feedback
{
    public static void Toasts(UiContext ui)
    {
        ui.Toasts.Show(UiText.FromKey("SAVED").WithFallback("Saved."), ToastKind.Success);   // Info, Success, Warning, Danger
        ui.Toasts.ShowRaw("Connection lost", ToastKind.Danger, seconds: 4f);                 // text that is already a string
    }

    public static async Task LoadWithOverlay(UiContext ui)
    {
        // Appears after 250 ms and stays 400 ms, so quick work never flashes it. Input is blocked from the first moment.
        using (ui.Loading.Begin(UiText.FromLiteral("Loading...")))
        {
            await Task.Delay(1500);
        }
    }
}
```

One toast shows at a time, up to three wait, equal repeats extend the visible one. Toasts and the overlay run on real time, so they still work while `Time.timeScale` is 0.
`ui.Loading.Begin` returns a token; `SetProgress(token, 0.5f)` shows a bar.

---

## 4. Gamepad and keyboard navigation

What a console, handheld, TV or desktop game needs on top of recipe 0, and where each part lives:

| Need | Kit |
|---|---|
| a focus ring for keyboard and gamepad but not for a mouse click | `Focus.uss` under `fdp-input-nav` (installed by `InteractionStyles.Install`, already in recipe 0) |
| focus starts somewhere on every screen | `UiFocus.FocusFirst(Root)` in `OnShow` |
| a plain element that Enter or A activates | `Activate.Register(element, action)` |
| a dropdown that works with a D-pad | `SelectField` (the stock `DropdownField` does not in 6000.3) |
| tabs that follow left and right | `TabBar` / `SegmentedControl` |
| focus stays inside a dialog or a menu | the dialogs do it themselves; for your own panel `new FocusTrap(panel)` |
| one back button | recipe 0 (`BackKeySource` to `Shell.HandleBackPress()`) |
| a HUD that must not react to the D-pad while the game reads it | `Profile.NavigationEnabled = false` |
| a title-safe margin on TVs | `UiShellOptions.MinimumMarginPercent = 5` (the default on consoles) |

```csharp
using FutureDigitalPlus.UIKit.Controls;
using FutureDigitalPlus.UIKit.Foundation;
using FutureDigitalPlus.UIKit.Interaction;
using UnityEngine;
using UnityEngine.UIElements;

public sealed class OptionsScreen : UiScreen
{
    readonly InputProfile _profile;

    public OptionsScreen(InputProfile profile) { _profile = profile; }   // register: Catalog.Register("Options", () => new OptionsScreen(gameUi.Profile))

    protected override void OnBuild()
    {
        var quality = new SelectField { Entries = "Low,Medium,High", Placeholder = "Quality" };  // Submit opens the list, left and right change it
        Root.Add(quality);

        var difficulty = new SegmentedControl();                                                  // left and right select
        difficulty.AddTab("easy", "Easy");
        difficulty.AddTab("hard", "Hard");
        difficulty.value = "easy";
        Root.Add(difficulty);

        var card = new VisualElement();                                                           // not a Button: made reachable and activatable
        card.AddToClassList("ds-card");
        card.Add(new Label("Daily challenge"));
        Root.Add(card);
        OnDisposeDo(Activate.Register(card, () => Ui.Toasts.ShowRaw("Starting...")).Dispose);     // click, Enter and gamepad A; removed when the screen is disposed
    }

    protected override void OnShow(object args)
    {
        Root.schedule.Execute(() => UiFocus.FocusFirst(Root));      // one frame later: the screen is displayed by then
    }

    public override bool OnBack()                                    // return true to consume the back press
    {
        Ui.Router.Pop();
        return true;
    }
}
```

To test without a gamepad, pin the device: `profile.Force(InputDeviceKind.Gamepad)` (a settings option "always show focus" is the same call); the focus ring and the prompts then follow the pin.
The [Gamepad Menu sample](../Samples~/GamepadMenu/README.md) runs all of it.

---

## 5. A HUD bar

`MeterBar`, `CooldownIcon`, `CurrencyLabel` and the other Hud widgets are plain elements; `HudZones` anchors them in nine places and passes pointer events through to the game.
Inside the shell, put the zones into the HUD layer (it already ignores picking where it is empty). Without a shell, add a `SafeAreaPanel` to the document root.

```csharp
using System;
using FutureDigitalPlus.UIKit.Foundation;
using FutureDigitalPlus.UIKit.Hud;
using UnityEngine;
using UnityEngine.UIElements;

public sealed class PlayerHud : MonoBehaviour
{
    [SerializeField] GameUi gameUi;

    MeterBar _health;
    CooldownIcon _dash;
    CurrencyLabel _coins;

    void Start()
    {
        // Under a notch and a home indicator the widgets stay inside the safe area; HudZones adds the extra margin a TV needs.
        var safe = new SafeAreaPanel { name = "hud-safe" };
        safe.style.flexGrow = 1;
        safe.pickingMode = PickingMode.Ignore;
        gameUi.Shell.Context.GetLayer(UiLayer.Hud).Add(safe);

        var zones = new HudZones { TitleSafePercent = 0 };              // 5 on a TV
        safe.Add(zones);

        _health = zones.Add(HudZone.TopLeft, new MeterBar { Size = MeterBarSize.Large, LabelMode = MeterLabelMode.ValueAndMax, MaxValue = 100f, Value = 100f });
        _coins = zones.Add(HudZone.TopRight, new CurrencyLabel { Format = NumberFormatStyle.Grouped });
        _dash = zones.Add(HudZone.BottomCenter, new CooldownIcon { ShowText = true });
        _dash.Ready += () => Debug.Log("dash ready");
    }

    // Call these from your game code; every widget animates by itself and uses unscaled time.
    public void Damage(float amount) => _health.Set(Mathf.Max(0f, _health.Value - amount), _health.MaxValue);   // the trail shows what was lost
    public void AddCoins(long amount) => _coins.Value += amount;                                               // rolls up and shows "+250"
    public void Dash() { if (_dash.IsReady) _dash.StartCooldown(6f); }
}
```

Warning and danger colours flip at `WarningThreshold` and `DangerThreshold` (set them, or `HighIsBad` for heat meters). Damage numbers over an enemy are `FloatingTextLayer.Spawn`, name plates pinned to the 3D world are `WorldAnchorLayer.Pin`
(both in the [Action HUD sample](../Samples~/ActionHud/README.md)). The Hud widgets take no focus, so a gamepad never gets stuck on them; set `Profile.NavigationEnabled = false` while the game itself reads the D-pad.

---

## 6. An inventory grid

The model is plain C# (no UI) and the grid shows it. Drag with a mouse or finger, pick up with Enter or A, split with Shift, open a context menu with right click, long press or Y.

```csharp
using System.Collections.Generic;
using FutureDigitalPlus.UIKit.Controls;
using FutureDigitalPlus.UIKit.Foundation;
using FutureDigitalPlus.UIKit.Gameplay;
using UnityEngine;
using UnityEngine.UIElements;

public sealed class BackpackScreen : UiScreen
{
    readonly InventoryModel _bag;
    readonly Texture2D _icon;

    // The game owns the model; the screen only shows it. Register: Catalog.Register("Backpack", () => new BackpackScreen(bag, icon))
    public BackpackScreen(InventoryModel bag, Texture2D icon) { _bag = bag; _icon = icon; }

    protected override void OnBuild()
    {
        var grid = new SlotGrid { Columns = 6, SlotSize = 56, Spacing = 4 };
        grid.Bind(_bag, id => new ItemVisual(Background.FromTexture2D(_icon), "common"));   // icon and rarity per item id

        grid.SlotActivated += slot => Debug.Log("use " + _bag[slot.Index].ItemId);
        grid.MoveRejected += move => Debug.Log("refused: " + move.Result.Reason);          // why: Locked, TargetFull, Rejected, ...
        grid.SlotContext += (slot, position) =>
            PopupMenu.Show(slot, new List<MenuEntry> { new MenuEntry("drop", "Drop"), new MenuEntry("inspect", "Inspect") }, position, entry => Debug.Log(entry.Id));

        Root.Add(grid);
    }

    public static InventoryModel NewBag()
    {
        var bag = new InventoryModel(24, id => id == "potion" ? 10 : 1);    // 24 slots, stack limit per item id
        bag.Add(new ItemStack("potion", 14));                                // fills stacks first, returns what did not fit
        return bag;
    }
}
```

Rules live in the model, not in the view: `new InventoryModel(24, maxStackOf, canPlace)` takes a veto callback (an equipment slot that accepts only helmets), `SetLocked(slot, true)` locks one, `Transfer` moves between two inventories.
Two grids that should hand items to each other: `SlotGrid.Link(a, b)`. Tooltips are the Extras `Tooltip` on `slot.tooltip` (see the [RPG Inventory sample](../Samples~/RpgInventory/README.md)).
Give the inventory its own area: a slot inside a scrolling parent competes with the parent's touch scrolling.

---

## 7. Localization hook

Every kit text is a `UiText`: a literal, or a key with a fallback. `UiLocalization.Default` turns it into a string through a backend. With no backend the fallback shows, and nothing throws.

```csharp
using FutureDigitalPlus.UIKit.Foundation;
using FutureDigitalPlus.UIKit.Language;
using UnityEngine;
using UnityEngine.UIElements;

public sealed class Texts : MonoBehaviour
{
    [SerializeField] TextAsset csv;                 // table,key,en,de,ar  (a header row, then one row per key)

    readonly DictionaryTextSource _texts = new DictionaryTextSource("en");

    void Awake()
    {
        _texts.LoadCsv(csv.text);                    // or _texts.Add("en", "UI", "PLAY", "Play") per entry, or LoadJson
        UiLocalization.Default.DefaultTables = new[] { "UI" };
        UiLocalization.Default.Backend = _texts;     // before the first screen is built
    }

    public void SetLanguage(string code) => _texts.SetLocale(code);   // bound labels, the shell and the root classes update; "ar" also mirrors the layout (fdp-rtl)

    public static void Bind(Button play, Label score)
    {
        play.BindText(UiText.FromKey("PLAY").WithFallback("Play"));                             // follows every language change while on a panel
        score.BindText(UiText.FromKey("SCORE").WithFallback("Score: {0}"), text => score.text = text);   // any other setter
    }
}
```

In UXML: `<fdp:LocalizedLabel key="HOME-PLAY" fallback="Play" />` with `xmlns:fdp="FutureDigitalPlus.UIKit.Language"`.

Other sources, all through the same seam:

- **Unity Localization** (`com.unity.localization` 1.4 or newer): install the package and open Localization Settings. The adapter assembly compiles itself and registers `UnityLocalizationBackend` before the first scene when nothing set a backend, so the lines above are not needed.
- **Your own tables or a server**: implement `ILocalizationBackend` (`CurrentLocale`, `Changed`, `TryGet(table, key, args, out text)` returning `Found`, `EntryMissing` or `TableMissing`) and assign it to `UiLocalization.Default.Backend`.
- **Fonts for scripts you did not bundle** (Chinese, Japanese, Korean, Arabic, Thai): `DsOsFonts` and `DsFonts.ApplyLocale`, see [Language/README.md](Language/README.md), "Fonts for other scripts". WebGL has no system fonts: bundle them.

---

## 8. A brand theme

A theme is a set of `--fdp-*` USS tokens. Make one from two colours at run time (no Editor step), switch by name, remember the choice, follow the player's high-contrast setting, and read your own tokens from your own USS.

```csharp
using FutureDigitalPlus.UIKit.DesignSystem.Theme.Data;
using UnityEngine;

public sealed class BrandThemes : MonoBehaviour
{
    [SerializeField] GameUi gameUi;
    [SerializeField] Color brand = new Color(0.91f, 0.27f, 0.45f);

    ThemeData _brandTheme;

    void Start()
    {
        // The primary family (hover, press, soft), surfaces, borders, text and focus ring are derived; text and borders are corrected until they pass WCAG AA.
        _brandTheme = ThemeData.FromBrand(primary: brand, background: new Color(0.06f, 0.07f, 0.10f), dark: true);
        _brandTheme.SetToken("--mygame-color-health", new Color(0.9f, 0.2f, 0.2f));   // your own token travels with the theme

        gameUi.Themes.Register("brand", _brandTheme);                // "dark" and "light" were registered by the bootstrap
        gameUi.Themes.Set("brand");                                  // the game's own look, not stored: it is a default, not a choice
        gameUi.Themes.PersistKey = "ui.theme";                       // from here on every Set is stored in PlayerPrefs (the player's choice)...
        gameUi.Themes.Restore();                                     // ...and the stored one, if still registered, wins over the default
    }

    public void UseBrand() => gameUi.Themes.Set("brand");            // every attached root repaints
    public void UseLight() => gameUi.Themes.Set("light");

    void OnDestroy()
    {
        if (_brandTheme != null) Destroy(_brandTheme);               // a run-time theme is an object you own
    }
}
```

Your own stylesheet reads the tokens with a fallback, so it still renders under a theme that lacks them: `background-color: var(--mygame-color-health, #E5484D);`.
`Themes.HighContrast = true` swaps in the high-contrast version of whatever theme is worn (recipe 0 connects it to the accessibility settings). Built-in themes: Dark, Light, two neutral, two high-contrast, all checked against WCAG AA.
A theme asset made in the Editor (Theme Configurator) needs a bake; one made with `FromBrand` does not. Details and limits: [DesignSystem/THEMING.md](DesignSystem/THEMING.md).

---

## 9. The same UI on phone, tablet and desktop

One UI, three layers of adaptation, none of them an `if (Screen.width < ...)` in your code:

1. **Scale.** `PanelScaleDriver` turns the screen into a panel scale: on handhelds one panel unit is 1/160 inch, so a 44 unit button is about 7 mm everywhere; on desktop and console the short side is a fixed number of units.
2. **Layout.** The shell writes `fdp-compact`, `fdp-regular`, `fdp-expanded` (short side below 600, 840, above), `fdp-portrait` or `fdp-landscape` and `fdp-platform-*` on its root. USS has no media queries; you select on those classes.
3. **Safe area.** The shell pads notches and, on TVs, a 5 percent margin. Panels that render into a texture are never padded.

```csharp
using FutureDigitalPlus.UIKit.Foundation;
using UnityEngine;
using UnityEngine.UIElements;

[DefaultExecutionOrder(-100)]                            // runs before the UIDocument builds its tree
public sealed class ScaleBootstrap : MonoBehaviour
{
    [SerializeField] UIDocument document;
    [SerializeField] PanelSettings sourceSettings;       // a project asset with a theme style sheet; it is never changed

    PanelScaleDriver _scale;

    void Awake()
    {
        // Pick the preset for the game: CardPortrait() (phones in portrait), DesktopHud(), Console(), Web(), World() (render textures).
        _scale = new PanelScaleDriver(sourceSettings, PanelScaleProfile.CardPortrait());
        document.panelSettings = _scale.Settings;        // a runtime copy
    }

    void Update() => _scale.Tick();                       // re-evaluates when the window or the DPI changes

    public void SetUiSize(float percent) => _scale.UserScale = percent / 100f;   // the player's "UI size" slider

    void OnDestroy() => _scale.Dispose();
}
```

```css
/* Your own USS: tiles in rows by size class */
.tile { width: 100%; }
.fdp-regular .tile { width: 50%; }
.fdp-expanded .tile { width: 33%; }
.fdp-portrait .sidebar { display: none; }
```

Swapping the panel settings rebuilds the document's tree, so do it before anything is added to `rootVisualElement`: put this component on the same GameObject as the `UIDocument` and the bootstrap
(the execution order above makes it run first), or move the three lines of `Awake` to the top of recipe 0's `OnEnable`.
The [Responsive sample](../Samples~/Responsive/README.md) shows the classes, a fake notch and the numbers for six devices on one page.

---

## 10. The ready-made host

Recipe 0 is what `UIShellHost` (assembly `FutureDigitalPlus.UIKit.Bootstrap`, [Bootstrap/README.md](Bootstrap/README.md)) does for you. Add **Future Digital Plus UI Kit > UI Shell Host** to the GameObject of the `UIDocument`, then
register your screens when it is ready. The HUD, Controls and Gameplay style sheets are the one thing it does not install (it does not reference those modules): install them from `Ready`.

```csharp
using FutureDigitalPlus.UIKit.Bootstrap;
using FutureDigitalPlus.UIKit.Foundation;
using UnityEngine;
using UnityEngine.UIElements;

public sealed class Game : MonoBehaviour
{
    sealed class StartScreen : UiScreen
    {
        public override string UxmlName => null;                 // built in code; return "Start" for Resources/Screens/Start.uxml
        protected override void OnBuild() => Root.Add(new Label("Hello"));
    }

    void Awake() => GetComponent<UIShellHost>().Ready += OnReady;

    void OnReady(UIShellHost host)
    {
        // HudStyles.Install(host.Root);  ControlsStyles.Install(host.Root);  GameplayStyles.Install(host.Root);   // the modules you use
        host.Shell.Catalog.Register("Start", () => new StartScreen());
        host.Shell.Boot("Start");
    }
}
```

`host.Themes` switches the theme by name (recipe 8), `host.Profile` and `host.Accessibility` are the input profile and the accessibility applier, `host.ConfigureShell = options => ...` (set it in `Awake`) changes the shell's options.
Put the panel scale component of recipe 9 on the same GameObject: it swaps the panel settings in `Awake`, before the host builds anything in `Start`.
