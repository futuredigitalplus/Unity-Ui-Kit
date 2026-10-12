# Foundation

The plumbing every UI Toolkit game needs and none wants to rewrite: one **shell** per panel with ordered layers, a **screen router** (push, replace, pop, saved stack), **dialogs**, **toasts**, a **loading overlay**,
**safe-area** handling that knows which panels are the screen, a **panel scale** policy for phones, tablets, desktop windows, consoles and the web, **profile classes** on the root (orientation, size class,
platform, input mode) and the small views that sit on top of them (top bar, list row, banner, pause menu).

Nothing in it knows what game it serves. It was generalised from the UI foundation of the card game (`ColorSolitaire.UI`); the mapping from the old names is at the end of this page.

## Assemblies

| Assembly | Contents | References |
|---|---|---|
| `FutureDigitalPlus.UIKit.Foundation.Core` | Pure logic with no `UnityEngine` reference (`noEngineReferences`): `ScreenStack`, `DialogQueue`, `ToastQueue`, `LoadingTokens`, `InputBlockCounter`, `BackDispatcher`, `UiText` and the key resolver, `UiProfile`, `PanelScalePolicy`, `SafeAreaInsets` and math, `UiAccessibility`, the service interfaces. Unit tested by dotnet in under a second. | nothing |
| `FutureDigitalPlus.UIKit.Foundation` | Views and services on UI Toolkit: `UiShell`, `UiScreen`, `ScreenRouter`, `DialogService`, `ToastService`, `LoadingOverlay`, `SafeAreaPanel`, `UiProfileWatcher`, `PanelScaleDriver`, `ResponsiveRootBinder`, `ResponsiveRulesAsset`, `FocusTrap`, `TopBar`, `ListRow`, `BannerView`, `PauseMenu` and more. Style sheets under `Runtime/Foundation/Resources`. | `Foundation.Core` |
| `FutureDigitalPlus.UIKit.Foundation.Editor` | The Responsive Manager window, its device catalog, preview session and layout check. Editor only, not auto-referenced (a game that implements `IResponsivePreviewHost` references it from its editor assembly). | `Foundation`, `Foundation.Core`, `Location.Editor` |

The two runtime assemblies are auto-referenced. Neither references the DesignSystem assembly: the views skin themselves with the design system's `ds-*` classes and tokens when its base sheet is present, and with
literal fallbacks in every `var(--fdp-x, fallback)` when it is not. Neither references Input System, Unity Localization, Addressables, TextMeshPro, uGUI or any game code, and neither reads `UnityEngine.Input`.

## Quick start

```csharp
using FutureDigitalPlus.UIKit.Foundation;
using UnityEngine;
using UnityEngine.UIElements;

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
```

Loading: `using (ui.Loading.Begin(UiText.FromLiteral("Loading"))) { await Load(); }`. The overlay appears after 250 ms and stays 400 ms, so quick work never flashes it; input is blocked from the first moment.
A screen built in code skips `UxmlName` and fills `Root` in `OnBuild`. Screens can also come from a `ScreenDefinition` asset (create it from the Assets menu, drag the UXML and USS in).

The shell works without the facade. A component that wires a shell, the theme, the input mode, the back key and the accessibility settings together is `UIShellHost` ([../Bootstrap/README.md](../Bootstrap/README.md); the same code, spelled out, is [../Recipes.md](../Recipes.md) recipe 0). A `UIKitHost` on the same document is not needed: the shell pads the safe area and the input profile writes `fdp-input-touch`, and the facade steps aside (no padding, no touch class) as soon as a shell lives under its root.

## The shell

`new UiShell(host, options)` adds one root element (`fdp-shell`) to `host` and six ordered layers under it: **screens**, **hud**, **overlays**, **dialogs**, **toasts**, **loading**. Layers ignore picking
where they are empty, so the game world still receives touches through a HUD. Nothing is a singleton: two shells in one scene or one test do not see each other. A shell does not own a `MonoBehaviour`;
the host forwards `Tick()`, `NotifyApplicationFocus` and `NotifyApplicationPause`.

| Member | Use |
|---|---|
| `Catalog.Register(id, factory)` | screens for this shell; `ScreenCatalog.Default` is the static registration point for code that runs before a shell exists |
| `Boot(id)` | takes the catalogs, raises `Booted`, restores the saved stack or shows `id` |
| `Context` | what screens use: `Router`, `Dialogs`, `Toasts`, `Loading`, `Input` (gate), `Back`, `Profile`, `SafeArea`, `State`, `Resolve(UiText)`, `GetLayer(UiLayer)` |
| `RequestBack()`, `HandleBackPress()` | one back press: loading, then the open dialog, then the screen's `OnBack`, then pop; the second form counts one press per frame |
| `Tick()` | safe-area poll and `IBackKeySource` poll |
| `Dispose()` | closes dialogs, clears toasts, disposes screens, removes the root; removes every callback and scheduled item |

`UiShellOptions`: `Assets`, `StateStore`, `Catalog`, `UseDefaultCatalog`, `BackKeySource`, `InputModeSource`, `Text`, `RestoreNavigation`, `NavigationStateKey`, `InstallStyles`, `ApplyDesignSystemRootClasses`,
`MaxDepth`, `TransitionsEnabled`, `SafeAreaMode`, `MinimumMarginPercent`, `SafeArea`, `ToastPlacement`, `HandleNavigationCancel`. Every member has a working default.

## Screens and the router

`UiScreen`: `Build`, `Show(args)`, `Hide`, `Dispose` are called by the router; override `OnBuild` (query the elements once), `OnShow`, `OnHide`, `OnDispose`, `OnBack()` (return true to consume back),
`SaveState()` and `RestoreState(string)` (a tab, a scroll position; stored in plain text, no secrets). `Listen`, `OnClick` and `OnDisposeDo` register callbacks that are removed when the screen is disposed.
An exception in any override is logged and never thrown into navigation; a missing UXML shows an error label instead of crashing.

`ScreenRouter`: after `Dispose` (the shell does it) every navigation call does nothing, so a late `Push` cannot build a screen into a UI that is gone. `Push` keeps the screen below alive and hidden, `Replace`, `Pop`, `PopTo`, `ResetTo`, `Back()`, a depth cap (default 12; the oldest screen above the root is disposed), a double-tap guard (pushing the
id that is on top does nothing), `CaptureState()`/`TryRestoreState` (the shell saves on every change and on pause). Transitions are USS classes (`fdp-screen--enter`, `--enter-back`, `--exit`, `--behind`) driven
by `transition-*` rules in `Foundation.uss` and last 220 ms (`ScreenRouter.TransitionMilliseconds`); they are skipped when `TransitionsEnabled` is false or `UiMotion` scales motion to zero (reduce motion).
Input is blocked while one runs.

## Dialogs, toasts, loading

- **`DialogService`** (`Context.Dialogs`, `IDisposable`): `Dispose` (the shell calls it) closes every dialog and drops every request that arrives afterwards, so a callback that asks for a follow-up dialog while the shell is going away cannot leave a modal behind; one modal at a time, others wait ordered by `DialogPriority`; a dialog on screen is never pre-empted. The frame is built in code on `ds-backdrop`, `ds-dialog` and `ds-modal`;
  a UXML template named `DialogFrame` in the asset source replaces it. Back, scrim tap (when the request allows it) and Escape close it with the cancel button's index; a `DedupeKey` ignores a double tap; focus is trapped inside
  and returns to the opener. Presets as extension methods: `Confirm`, `Alert`, `Choice`, `Input` (default button texts use the keys `FDP-OK`, `FDP-CANCEL`, `FDP-YES`, `FDP-NO` with English fallbacks). Your own dialogs:
  `new DialogRequest { Id = ..., Title = ..., DedupeKey = ... }.AddButton(DialogButton.Of("KEY", "Fallback", DialogButtonRole.Cancel))`, or keep a `DialogTemplate` and call `CreateRequest(callback, content)`.
  `Content` takes a `VisualElement` for custom bodies.
- **`ToastService`** (`Context.Toasts`): one toast at a time, up to three waiting, equal repeats extend the visible one; kinds `Info`, `Success`, `Warning`, `Danger` map to `ds-toast--*`. Timers run in real time,
  so toasts still expire while the game is paused with `Time.timeScale = 0`. `ToastPlacement.Top` or `Bottom`.
- **`LoadingOverlay`** (`Context.Loading`): token ids are only unique inside one overlay, so an overlay ignores a token another overlay issued (`LoadingToken.IsIssuedBy(overlay)`); see the quick start; `SetProgress(token, 0..1)`, `Update(token, text, spinner)`. A blocking token swallows back.

## Safe area

`SafeAreaPanel` (a `[UxmlElement]`, attribute `edges="top,bottom"`, default all) pads itself by the current insets of the nearest `SafeAreaProvider` (one per shell, `SafeAreaProvider.Default` for panels outside a shell). The
provider polls a few numbers and recomputes only when one changed; rotation, window resize, notch and home indicator all arrive through them.

| Rule | Behaviour |
|---|---|
| Which panels | `SafeAreaMode.Auto` (default): only screen-space panels on the main display. **Render-texture, world-space and other-display panels get zero insets** (`UiPanelKind`); `Always` and `Never` override |
| Television margin | `MinimumMarginPercent` below zero means 5 percent of the panel size on every edge on consoles and tvOS, zero elsewhere; any value from zero up is used as given; merged with the device insets (the larger wins) |
| Units | panel units (`Insets`) and device pixels (`PixelInsets`) |
| Tests | replace `SafeAreaSource` and `ScreenSizeSource` with fakes |

## Panel scale

`PanelScaleDriver(panelSettings, profile)` works on a **runtime copy** of the `PanelSettings` (never the shared asset), re-evaluates when the screen width, height or DPI change and applies the result of the pure
`PanelScalePolicy`. Assign `driver.Settings` to the `UIDocument` before anything is added to its root (a new `PanelSettings` rebuilds the tree); call `driver.Tick()` per frame (your host component does it; [../Recipes.md](../Recipes.md), recipe 9). `driver.UserScale` is the player's "UI size" setting.

Unit contract: **on handhelds one panel unit is 1/160 inch (a density-independent point)**, so a 44 unit button is about 7 mm everywhere. Elsewhere the scale is derived from the window so the short side measures a fixed
number of units.

| Preset | Phone / tablet | Desktop window | Console | Web | Notes |
|---|---|---|---|---|---|
| `PanelScaleProfile.CardPortrait()` | physical size | like the device it simulates: 390 units on the short side up to 1100 px, 820 above | same | same | the proven constants of the card game |
| `DesktopHud()` | physical size | 720 units short side (1080p gives scale 1.5) | 540 | 720 | |
| `Console()` | window derived | 720 | 540 (1080p gives 2, 4K gives 4) | 720 | |
| `Web()` | window derived | 720 | 540 | 720 | |
| `World()` | constant pixel scale 1 | | | | render textures, world-space panels |

Strategies: `PhysicalSize`, `WindowDerived`, `ReferenceResolution` (one number of units on every device), `ConstantPixel`. The scale is clamped to 1..4 (`minScale`, `maxScale`). A profile is a serializable struct: start
from a preset and change fields. The user scale multiplies a pixel scale and is folded into the reference DPI in physical mode (a larger scale gives a smaller reference DPI).

## Profile classes

`UiProfileWatcher` keeps these classes on the shell root, driven by `GeometryChangedEvent` (no polling), so a stylesheet changes layout with plain descendant selectors (USS has no media queries):

| Classes | Rule |
|---|---|
| `fdp-portrait` / `fdp-landscape` | width less than height / otherwise |
| `fdp-compact` / `fdp-regular` / `fdp-expanded` | short side of the panel in units below 600 / below 840 / larger (so split view and resizable windows get the right class) |
| `fdp-regular-up` | with `fdp-regular` and with `fdp-expanded` (`UiProfile.IsRegularOrLarger`): one selector for layouts that have only a phone tier and a tablet tier |
| `fdp-platform-ios`, `-android`, `-windows`, `-macos`, `-linux`, `-webgl`, `-console`, `-xr` | once, from the platform (`-other` when unknown) |
| `fdp-input-mouse`, `-touch`, `-keyboard`, `-gamepad`, and `fdp-input-nav` for keyboard and gamepad | from an `IUiInputModeSource` (the Interaction module provides one), or from the platform when none is given: touch on phones and tablets, gamepad on consoles, mouse elsewhere |

`fdp-input-touch` is the touch-size tier of the design system and the foundation sheets (larger rows, 44 unit minimum targets); `fdp-platform-console` selects the ten-foot layout.
`ListRow` and `TopBar` mirror under `fdp-rtl` (rows list their parts from the right; Unity's `languageDirection` only shapes text and never mirrors a flex row).
Locale (`fdp-lang-*`, `fdp-rtl`) and accessibility (`fdp-reduce-motion`, `fdp-text-scale-*`) classes belong to the Language and Interaction modules.

## Responsive form factors and the Responsive Manager

Profile classes answer "how big is the panel"; flip and fold phones also need "what kind of screen is it". The driver classifies the panel by its size in units at the policy scale
(`DeviceFormFactors.Classify`, size only, so it works on every platform without a plugin), and a game sets a scale multiplier and a column width per form factor:

| Form factor | Rule (short side in units, long side over short side) | Typical screens | Root class |
|---|---|---|---|
| `Phone` | below 600, ratio 1.75 or more | phones, the tall cover screens of book folds, the main screen of flip phones | `fdp-ff-phone` |
| `Pad` | 600 or more, ratio 1.3 or more | tablets | `fdp-ff-pad` |
| `FlipCover` | below 600, ratio below 1.3 | the outer screen of a flip phone (about 380 by 400 units) | `fdp-ff-flip-cover` |
| `FoldCover` | below 600, ratio 1.3 to 1.75 | the wide cover screen of a passport fold | `fdp-ff-fold-cover` |
| `FoldInner` | 600 or more, ratio below 1.3 | the inner screen of a book fold (nearly square) | `fdp-ff-fold-inner` |

- `ResponsiveRules` (Core, serializable) holds `ResponsiveRule` entries: form factor, orientation (`Any`, `Portrait`, `Landscape`), `scale` (0.5 to 2, multiplied with the user scale) and `maxContentWidth`
  (units, 0 for none). `Resolve` takes the exact orientation, then `Any`, then the same for the fallback form factor (covers use the phone's rule, the inner screen the pad's), then a rule that changes nothing.
  An empty table keeps the old behaviour exactly. An explicit scale 1 rule on a cover stops the fallback.
- `ResponsiveRulesAsset` (`Create > FDP UI Kit > Responsive Rules`) stores one table. `driver.Rules = asset.Rules;` Edits are noticed on the next `Tick`, so the Manager changes a running game at once.
- `PanelScaleDriver.FormFactor`, `Rule`, `Landscape` give the result; the user scale never changes the form factor.
- `ResponsiveRootBinder(root, driver)` keeps `fdp-ff-*` on the root, `fdp-posture-half-opened` with `fdp-hinge-horizontal` or `fdp-hinge-vertical` while `Posture` is half opened (a posture source or the
  Manager sets it: the size of a screen cannot show a hinge), and sets `max-width` on every element with `ColumnClass` from the rule. Style with descendant selectors:
  `.fdp-ff-flip-cover .my-row { min-height: 48px; }`.

```csharp
_scale = new PanelScaleDriver(document.panelSettings, PanelScaleProfile.CardPortrait());
var rules = Resources.Load<ResponsiveRulesAsset>("ResponsiveRules");
if (rules != null) _scale.Rules = rules.Rules;
_binder = new ResponsiveRootBinder(shell.Root, _scale) { ColumnClass = "my-page-column" };
```

**Responsive Manager** (`Tools > FDP UI Kit > Responsive Manager`, assembly `FutureDigitalPlus.UIKit.Foundation.Editor`, `Editor/Foundation`):

| Area | What |
|---|---|
| Device list | the kit catalog `Editor/Foundation/Responsive/ResponsiveDevices.json` (phones, tablets, and the cover and main or inner screens of 28 flip and fold profiles with DPI, safe areas and hinge) plus the project's own devices in `ProjectSettings/FdpResponsiveDevices.json` (same format; a device with a kit id replaces it). Pixel sizes held upright; safe areas in pixels as top, bottom, left, right; `hinge` 1 horizontal, 2 vertical when held upright |
| Outside Play mode | every catalog screen with its size in units, form factor and scale under the current rules |
| Play mode preview | `ResponsivePreviewSession` renders the game's panel into a texture of the device's pixel size with the driver fed the device's size and DPI (`PanelScaleEnvironment.PixelExact`), the shell's safe area set to the device's insets, and the binder's posture; portrait or landscape, flat or half opened; overlays for the safe area and the hinge; page up and down in the main list; PNG export |
| Layout check | `ResponsiveLayoutAudit`: single-line text wider than its box, wrapped text taller than its box, text past the side edges, controls below the minimum touch target (44 units by default) |
| Rules | the project's `ResponsiveRulesAsset` (created at `Assets/Resources/ResponsiveRules.asset` on request), per form factor and orientation, with undo |
| Check every device | each catalog screen (optionally both orientations and every game screen) in turn: findings per preview, a Markdown report, PNGs in `Temp/ResponsiveCaptures` |

A game lists its screens by implementing `IResponsivePreviewHost` (`Profile`, `Screens`, `Show(screen)`) in an editor assembly with a parameterless constructor; the Manager finds it with `TypeCache`.

## Views

| Type | What |
|---|---|
| `TopBar` | `[UxmlElement]` safe-area padded bar: back button, title, start and end slots (`Title`, `ShowBack`, `BackClicked`) |
| `ListRow` | focusable row: icon, title, subtitle, value, chevron, selected state; 56 units high under the touch tier |
| `SectionHeader`, `StatCell` | a heading above a group of rows; a caption and a value |
| `BannerView` | inline message with an optional action and dismiss button, kind classes like toasts |
| `PauseMenu` | register on a shell, push `Pause`; pauses time through `Time.timeScale` or your own `PauseHook`; UI timers keep running in real time |
| `FocusTrap`, `UiFocus` | hold keyboard and gamepad focus inside a container, nest across layers (only the newest trap of a panel is active), restore on dispose; `FirstFocusable`, `FocusFirst`, `AbsorbPointer` |
| `UiInputGate` (`IUiInputGate`) | `IsBlocked` and `IsPointerOverUi(screenPosition)` for game input code: ignore the touch when a dialog is open or the pointer is over UI |
| `UiTextSource` | the shell's text resolver: literals, key fallbacks, `[KEY]` placeholders; plug a localization in with `new UiTextSource(UiLocalization.Default.Resolve)` and call `NotifyChanged()` on a locale change |
| `IUiAssetSource` | where UXML and USS come from: `ResourcesAssetSource`, `CompositeAssetSource`, `ScreenDefinitionAssetSource`; write your own for Addressables or bundles |
| `IUiStateStore` | where small state is kept: `PlayerPrefsUiStateStore` is the default implementation |

Styles: `FutureDigitalPlus/UIKit/Foundation/Styles/Foundation.uss` and `Foundation.Tokens.uss` (installed by the shell; `FoundationStyles.Install/Uninstall`). Foundation-only values are `--fdp-foundation-*` custom
properties on `.fdp-shell` (gutter, dialog and toast width, top bar and row height, spinner size); colours, spacing, radii and type come from the design system's `--fdp-*` tokens. Override a foundation token on your own
root class or on `.fdp-shell` in a later sheet. The sheets use no `gap`, `calc`, `@media`, `z-index` or pseudo-elements, and every `var()` has a literal fallback.

## Optional packages and platforms

| Situation | Behaviour |
|---|---|
| No Input System, Localization, Addressables, TextMeshPro, uGUI | everything works; text shows literals, fallbacks and `[KEY]`; back comes from `RequestBack`, navigation-cancel events or your own `IBackKeySource` |
| Design system base sheet absent | views render with fallbacks; `FoundationStyles.InstallDesignSystem(panelRoot)` adds the sheet when it exists and logs a warning when it does not |
| Unity Localization | use the Language module's backend with `UiTextSource` |
| Android back, Escape, gamepad B | by `IBackKeySource` (Interaction module) or `NavigationCancelEvent` (default on) |
| Render pipelines | none referenced; verified neutral by the platform smoke tests |
| WebGL, consoles, XR | no platform calls except `UnityEngine.Device`; not run on those targets |

## Tests

| Where | What | How to run |
|---|---|---|
| `Tests/Pure/Foundation` | pure tests: the ported tests of the card game plus size classes, scale presets, accessibility, log, templates | `dotnet test Tools~/PureTests -p:Module=Foundation` (seconds, no Unity); also compiled by Unity as `FutureDigitalPlus.UIKit.Foundation.Core.Tests` (EditMode) |
| `Tests/Editor/Foundation` | view logic without a panel: shell, router, dialogs, toasts, loading, scale driver and its rules, root binder, device catalog, styles | Unity EditMode |
| `Tests/Runtime/Foundation` | PlayMode on a render-texture panel: the Home, Settings, dialog, toast, loading sample; transitions; back; input gate; safe area on screen, render-texture and fake devices; profile classes; scale driver; `FocusTrap`; pause menu; touch targets | Unity PlayMode |

The Unity tests above ran in Unity 6000.3.25f1 in every variant of [../CompatibilityMatrix.md](../CompatibilityMatrix.md), which also has the current counts. Not verified: real devices (notch, rotation, touch), consoles.

## Known limits

- No debug layer and no `UiRegistry` editor builder (the registry of the card game is replaced by `ScreenDefinition` assets and `ResourcesAssetSource`).
- `Foundation.Addressables` (an Addressables asset source) is not built; implement `IUiAssetSource` (two methods) over your own loader.
- Screen readers: not covered (the Interaction module owns accessibility settings).
- The safe area is the device's rectangle: Device Simulator and `UnityEngine.Device` drive it in the Editor; a window on a desktop has none.
- `DialogService` needs a design system base sheet for its look; without it the frame is unstyled but works.
- World-space panels (Unity 6000.5) are detected (`UiPanelKind.WorldSpace`) but untested.

## From the card game (`ColorSolitaire.UI`) to the kit: names in, names out

Everything moved to namespace `FutureDigitalPlus.UIKit.Foundation` unless the table says otherwise.

| Color Solitaire | Kit | Change |
|---|---|---|
| `ScreenStack`, `ScreenEntry`, `ScreenChange`, `ScreenChangeKind` | same, in `Foundation.Core` | format tag `fdp-nav-v1` (saved `color-solitaire-nav-v1` blobs are not read: the first start shows the initial screen) |
| `RouterStateDocument` | `ScreenStack.Capture` / `TryParse` and `ScreenRouter.CaptureState` / `TryRestoreState` | no separate document type |
| `ScreenRouter`, `UiScreen`, `UiContext` | same | `UiContext` has `Text`, `SafeArea`, `State` and layer access; the router takes a catalog instance |
| `ScreenCatalog` (static) | `ScreenCatalog` instance, `ScreenCatalog.Default` keeps the static pattern | `Register(id, factory)` is now an instance method; `Default.Register(...)` replaces `ScreenCatalog.Register(...)` |
| `UiRegistry`, `UiRegistryEntry`, `UiTemplates` | `ScreenDefinition`, `ResourcesAssetSource`, `FoundationLayout.Fill` | no registry asset and no builder step |
| `UIRoot` | `UiShell` (plain C#) plus a host of your own now, the facade's shell host later | `UIRoot` set panel settings, registry and initial screen; the shell takes options and `Boot(id)` |
| `IUiAssetSource`, `ResourcesAssetSource`, `CompositeAssetSource` | same | `ResourcesAssetSource(folder)` |
| `DialogQueue`, `DialogRequest`, `DialogButton`, `DialogButtonRole`, `DialogCloseReason`, `DialogHandle`, `DialogHandleState`, `DialogPriority` | same, in `Foundation.Core` | |
| `DialogResult` | `DialogOutcome` | renamed: `DialogResult` is a Unity type name |
| `AlertDefinition` | `DialogTemplate` | `CreateRequest(callback, content)`; `AlertCatalog` (D1 to D30) and `ChromeMetrics` stay in the game |
| `DialogService`, `IDialogService` | same | `Show(DialogTemplate, callback, content)` replaces `Show(AlertDefinition, ...)`; constructor takes an optional `UiTextSource` |
| `ToastQueue`, `ToastItem`, `ToastService`, `IToastService` | same | `ToastKind.Error` is `Danger` (`ds-toast--danger`) |
| `LoadingTokens`, `LoadingToken`, `LoadingOverlay`, `ILoadingOverlay` | same | |
| `InputBlockCounter`, `UiInputGate`, `IUiInputGate` | same | one gate per shell |
| `BackDispatcher`, `BackPriority` | same, in `Foundation.Core` | the shell owns it (`Context.Back`); key sources come through `IBackKeySource` |
| `SafeAreaInsets`, `SafeAreaMath`, `SafeAreaEdges`, `SafeAreaEdgesParser` | same, in `Foundation.Core` | `SafeAreaInsets` is `IEquatable` with `WithMinimum` |
| `SafeAreaPanel` | same | no padding on render-texture or world-space panels |
| `SafeAreaProvider` (static) | `SafeAreaProvider` instance; `SafeAreaProvider.Default` | `Poll(root)` is an instance method; `UiShell` owns one |
| `PanelScalePolicy.Compute(handheld, dpi, w, h)`, `PanelScale` | `PanelScalePolicy.Compute(profile, input)` with `PanelScaleProfile.CardPortrait()` | reproduces the same phone and tablet constants; `PanelScaleDriver` applies it |
| `UiProfile`, `UiProfileWatcher` | `UiProfile(Orientation, SizeClass, InputMode)`, `UiProfileWatcher` | `UiFormFactor` (compact, regular) is `UiSizeClass` (compact, regular, expanded); classes `color-solitaire-*` are `fdp-*` |
| `UiText`, `LocalizationKey`, `LocalizationKeyResolver`, `LookupStatus`, `TableLookup` | same, in `Foundation.Core` | the game's default table names (`Core`, `Borderline`, `UI`) are no longer built in: pass them to the resolver; `LocalizationKey.DefaultTable` is `UI` |
| `LocaleStyle` | same | prefix `fdp-lang-`; right-to-left list extended (ar, he, fa, ur, ps, sd, ug, yi, dv, ku) |
| `UiLocalization`, `LocalizedLabel`, `LocalizedButton`, `LocalizedTextBinder`, `LocalizedTextExtensions` | Language module | |
| `UiTextSource` (none) | `UiTextSource` | new: the seam between views and a localization |
| `UnityStringTableLookup` | `UnityLocalizationBackend` in `Language.UnityLocalization` | optional assembly |
| `OsFontOrder`, `OsFontFallback` | `DsOsFontOrder`, `DsOsFonts` in DesignSystem typography | not in Foundation |
| `SegmentedControl` | `TabBar` and `SegmentedControl` in Controls | |
| `FocusTrap`, `UiFocus` | same, in `Foundation` | nests across layers; restores focus when the opener is gone |
| `UiNames`, `UiStorageKeys`, `UiFallbackViews` | `UiNames` (kit element and class names); the rest stay in the game | `IUiStateStore` and `PlayerPrefsUiStateStore` replace the storage keys |
| `PDebug` | `UiLog.Sink` | set once; the shell points an unset sink at the Unity console |
| `color-solitaire-*` classes, `--color-solitaire-*` tokens | `ds-*`, `fdp-*`, `--fdp-*` | see `Documentation~/Migration.md` |
