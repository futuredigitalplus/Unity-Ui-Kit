# Migrating the card game's UI foundation to the kit

This guide is for moving a game's own UI foundation onto the package. It was written for the first host game,
`Color-Solitaire` (`Assets/Scripts/UI/Foundation`, namespace `ColorSolitaire.UI`, and `Assets/UI/Styles`), and then used for the first
real migration (2026-10-05 and 2026-10-06). Sections 1 to 9 are the type, token and class tables of that game; **section 10
records what the real migration taught** (what worked, the gaps found and fixed in 0.2.1, the gaps still open, and a checklist
for the next game). The migration of that game was not finished when this was written: the wiring, the brand theme data and
many screens were done, the swap that deletes the game's own foundation copy was not. Section 10.5 says why that order was a mistake.

**How to read the "Package" columns.** A name marked **(exists)** was found in the package sources on 2026-10-06 (public type or
USS name, checked with a script against `Runtime/**`). A name marked **(planned)** does not exist yet: it is the fixed name of the
global-use plan, or an item that is deliberately left to the game. The modules were still being integrated when this table was
checked (the merged set runs in Unity, see [CompatibilityMatrix.md](CompatibilityMatrix.md); the card game itself was not migrated by this work), so re-check a name against the module's own guide
(`Documentation~/<Module>/README.md`, which is the authority once it exists) before a large edit. This page is the overview and the
non-type mappings (tokens, classes, services, scale).

Source figures below were read from the game on 2026-10-05: 77 public types in namespace `ColorSolitaire.UI` (about 6,100 lines under
`Foundation`, of which about 2,900 are in `Foundation/Pure`), 119 `--color-solitaire-*` custom properties in `Assets/UI/Styles/tokens.uss`
(declared in `:root`, with a second set of overrides for the larger size tier), and a stylesheet set of `base.uss`,
`components.uss`, `icons.uss`, `layout.uss`, `tokens.uss` and `COLOR_SOLITAIRE_Theme.tss`.

## 1. Order of work

1. Reference the package; add `FutureDigitalPlus.UIKit.Foundation.Core` and `FutureDigitalPlus.UIKit.Foundation` to the game's UI asmdef.
2. Move the pure types (section 2). Their tests move with them; the game keeps a thin alias file only if call sites are too many to edit.
3. Map the tokens (section 3) and put what the kit does not own into a game theme (section 3.2).
4. Map the classes (section 4) in UXML and USS, screen by screen.
5. Swap `UIRoot` for a `UiShell` and a small host component (section 5); keep game-specific screens as they are.
6. Replace the scale policy by a preset (section 6). Verify the phone and tablet unit sizes first (section 8).
7. Delete the foundation code that now lives in the package. Keep what is specific to the card game (section 7).

## 2. Types

| Color Solitaire (`ColorSolitaire.UI`) | Package | Notes |
|---|---|---|
| `ScreenStack`, `ScreenEntry`, `ScreenChange`, `ScreenChangeKind` | same names, namespace `FutureDigitalPlus.UIKit.Foundation`, assembly `Foundation.Core` (exists) | pure, tests move as is |
| `RouterStateDocument` | `ScreenStack.Capture` / `ScreenStack.TryParse` (exists; no separate type) | format tag changes to `fdp-nav-v1`; a saved `ui/router` blob from the game is not read, see section 9 |
| `ScreenRouter`, `ScreenCatalog`, `UiScreen`, `UiContext` | same names in `Foundation` (exists) | `ScreenCatalog` gets an instance (`ScreenCatalog.Default` keeps the static registration pattern; a shell asks its own catalog first) |
| `UIRoot` | `UiShell` (exists, plain C#, `Foundation`) and the MonoBehaviour host `UIShellHost` (exists, assembly `FutureDigitalPlus.UIKit.Bootstrap`, [Bootstrap/README.md](Bootstrap/README.md)) | see section 5 |
| `UiRegistry`, `UiRegistryEntry` | `ScreenDefinition` assets or `ResourcesAssetSource` (exists); a registry type is not planned | `IUiAssetSource`, `ResourcesAssetSource`, `CompositeAssetSource` keep their names (exist) |
| `DialogQueue`, `DialogRequest`, `DialogButton`, `DialogButtonRole`, `DialogOutcome` (was `DialogResult`), `DialogCloseReason`, `DialogHandle`, `DialogHandleState`, `DialogPriority` | same names (exist, `Foundation.Core`) | logging goes through `UiLog` |
| `AlertDefinition` | `DialogTemplate` (exists, `Foundation.Core`) | `dialogs.Show(template, callback)` |
| `AlertCatalog` (D1 to D30), `ChromeMetrics` | stay in the game | content and card-table layout, not plumbing |
| `DialogService`, `IDialogService` | same names (exist) | dialogs are built on `ds-backdrop`, `ds-dialog` (`ds-modal` when custom content is present); the extension methods `Confirm`, `Alert`, `Choice`, `Input` (`DialogPresets`) cover the common cases |
| `ToastQueue`, `ToastItem`, `ToastKind`, `ToastService`, `IToastService` | same names (exist) | `ToastKind.Error` becomes `Danger` (class `ds-toast--danger`) |
| `LoadingTokens`, `LoadingToken`, `LoadingOverlay`, `ILoadingOverlay` | same names (exist) | `LoadingToken` is `IDisposable`: `using (ui.Loading.Begin(...)) { ... }` |
| `InputBlockCounter`, `UiInputGate`, `IUiInputGate` | same names (exist) | picking is blocked per panel, not globally |
| `BackDispatcher`, `BackPriority` | same names (exist, `Foundation.Core`) | the shell's `UiShellOptions.BackKeySource` takes an `IBackKeySource` (poll `WasPressedThisFrame`, exists). `LegacyBackKeySource` does not exist: Interaction's `BackKeySource` raises an event instead (`Pressed`, from the legacy Input Manager, the Input System and navigation-cancel events). No adapter joins the two yet; connect them with `back.Pressed += () => shell.HandleBackPress()` (the Gamepad Menu sample does) |
| `FocusTrap`, `UiFocus` | same names (exist, `Foundation`) | |
| `SafeAreaInsets`, `SafeAreaMath`, `SafeAreaEdges`, `SafeAreaEdgesParser`, `SafeAreaPanel`, `SafeAreaProvider` | same names (exist); the facade keeps its own `FutureDigitalPlus.UIKit.SafeAreaInsets` (a pure conversion for one themed document; the facade does not reference Foundation), a different type from the Core one in `FutureDigitalPlus.UIKit.Foundation`: alias one of them in a file that imports both namespaces | the panel no longer pads render-texture or world-space panels (`SafeAreaMode`, `UiPanelKind`) |
| `PanelScale`, `PanelScalePolicy` | `PanelScale`, `PanelScalePolicy`, `PanelScaleProfile`, `PanelScaleInput` (exist, `Foundation.Core`) and `PanelScaleDriver` (exists, `Foundation`: applies the policy to a `PanelSettings` and keeps it current) | section 6 |
| `UiProfile`, `UiFormFactor`, `UiOrientation`, `UiProfileWatcher` | `UiProfile`, `UiSizeClass`, `UiOrientation`, `UiProfileWatcher` (exist) | `UiFormFactor` (phone, tablet) becomes three size classes; section 4 |
| `UiText`, `LocalizationKey`, `LocalizationKeyResolver`, `LookupStatus`, `TableLookup`, `LocaleStyle` | same names (exist, `Foundation.Core`) | the game sets its default table names itself (`UiLocalization.DefaultTables`). `LocaleStyle` writes `fdp-lang-*` |
| `UiLocalization`, `LocalizedLabel`, `LocalizedButton`, `LocalizedTextBinder`, `LocalizedTextExtensions` | same names in `Language` (exist) | the backend is a seam (`ILocalizationBackend`); `DictionaryTextSource` is the one that needs no package |
| `UnityStringTableLookup` | `UnityLocalizationBackend` in `Language.UnityLocalization` (exists) | optional assembly, compiled only with `com.unity.localization` |
| `OsFontOrder` | `DsOsFontOrder` in DesignSystem typography (exists; not in `Foundation.Core`) | |
| `OsFontFallback` | `DsOsFonts` (exists, DesignSystem typography) | |
| `SegmentedControl` | `SegmentedControl`, a `TabBar` with the segmented skin, in Controls (exists) | |
| `UiNames`, `UiTemplates`, `UiStorageKeys`, `UiFallbackViews` | stay in the game, or become the game's own constants (the kit has its own `UiNames` for its element and class names) | `IUiStateStore` replaces the storage seam; `PlayerPrefsUiStateStore` is the default (both exist) |

## 3. Tokens

### 3.1 Mapped to kit tokens

The kit's built-in tokens exist today (**exists**) in `Runtime/DesignSystem/Resources/FutureDigitalPlus/UIKit/DesignSystem/Styles/DesignTokens.uss`.
The tokens of the plan's token contract (`color-success`, `-info`, `-scrim`, `-focus-ring`, `touch-min`, `gutter`, `control-h-*`, `icon-s/m/l`, `row-h`, `dialog-w`, `page-max-w`, `font-size-display`,
`transition-slow`) were found in that file on 2026-10-06 too. Write `var(--fdp-x, <fallback>)` for a token that may be missing in an older checkout.

| `--color-solitaire-*` | `--fdp-*` | Note |
|---|---|---|
| `space-1`, `-2`, `-3`, `-4`, `-5`, `-6`, `-8` | `space-1` ... `space-8` | same values (4, 8, 12, 16, 20, 24, 32); **exists** |
| `space-10` (40px) | none | write 40px inline or define an extra token |
| `radius-s` (6) | `radius-sm` (6) | **exists** |
| `radius-m` (10) | `radius-md` is 8px | nearest, 2px smaller; keep 10 as a game extra token if the look matters |
| `radius-l` (16) | `radius-xl` (16) | **exists** |
| `radius-pill` (999px) | none | write the value inline |
| `border-w`, `border-w-strong` | `border-thin` (1), `border-regular` (2) | **exists** |
| `dur-fast` (120ms) | `transition-fast` (120ms) | **exists** |
| `dur-base` (220ms) | `transition-medium` (200ms) | nearest |
| `dur-slow` (360ms) | `transition-slow` | **exists** (value differs: check) |
| `font-caption` 12, `-small` 14 | `font-size-body-2` 12, `font-size-body-1` 14 | **exists** |
| `font-body` 16 | `font-size-h3` is 16px | different role name, same size |
| `font-subtitle` 18, `-title` 22, `-headline` 28 | `font-size-h2` is 20, `font-size-h1` is 26 | the scales differ: choose the kit scale or keep the game values as extra tokens |
| `font-display` 36 | `font-size-display` | **exists** (value differs: check) |
| `color-bg` | `color-bg` | **exists** |
| `color-surface` | `color-surface` | **exists** |
| `color-surface-raised`, `color-surface-pressed` | `color-surface-elev`, `color-surface-hover` | roles are close, not identical |
| `color-border` | `color-border` | |
| `color-text`, `color-text-muted`, `color-text-on-accent` | `color-text-primary`, `color-text-secondary`, `color-text-on-accent` | |
| `color-primary`, `-pressed` | `color-primary`, `color-primary-press` | |
| `color-secondary`, `-pressed` | `color-secondary`, `color-secondary-press` | |
| `color-danger`, `-pressed` | `color-danger`, `color-danger-press` | |
| `color-warning` | `color-warning` | |
| `color-success`, `color-info`, `color-scrim`, `color-focus-ring` | `color-success`, `color-info`, `color-scrim`, `color-focus-ring` | **exists** |
| `touch-min` 44, `icon-s/m/l`, `row-h`, `dialog-w`, `page-max-w`, `gutter` | `touch-min`, `icon-s/m/l`, `row-h`, `dialog-w`, `page-max-w`, `gutter` | **exists**; the game's values differ for `row-h` (56), `dialog-w` (360 and 440 on the larger tier) and `page-max-w` (560 and 720), so set them in the game theme |
| `color-on-primary` | `color-text-on-accent` | the kit's token is the text colour on every filled accent |

### 3.2 Stays in the game (a game theme with extra tokens)

These are card-game design, not kit roles: all `palette-*` (felt, mint, navy, card back blue, grey ramp), all `suit-*` (12),
`color-bg-alt`, `color-accent`, `color-topbar`, `color-statsbar`, `color-toast`, `color-switch-off`, `color-clear`,
`color-transparent`, `color-shadow`, `color-divider`, the four `color-scroll-*` and the `scroll-*` sizes, `button-h`, `topbar-h`,
`statsbar-h`, `toast-w`, `switch-w/h`, `spinner`, `value-min-w`, and `safe-top/right/bottom/left`. Put the colours in a game theme asset
(a `ThemeData` carries free extra tokens: `extraTokens`, `SetToken`, **exists**); the plain sizes can stay as `:root` variables in the game's own sheet
under names the game owns. The `--color-solitaire-safe-*` variables are declared in `tokens.uss`, but the game applies safe area by padding
(`SafeAreaPanel`), so they can be dropped after the migration.

A game-owned variable name may still start with `--color-solitaire-`: the kit only reserves `--fdp-*` and `--unity-*`.

## 4. Classes and names

| Color Solitaire | Kit | Note |
|---|---|---|
| `color-solitaire-root`, element names `layer-screens`, `layer-hud`, `layer-popups`, `layer-toasts`, `layer-loading`, `layer-debug` | `ds-root` plus the shell's ordered layers (exist; element names `layer-screens`, `layer-hud`, `layer-overlays`, `layer-dialogs`, `layer-toasts`, `layer-loading`) | the shell has no debug layer (add one to the root yourself); `layer-popups` splits into overlays and dialogs |
| `color-solitaire-button`, `--primary`, `--secondary`, `--danger`, `--small`, `--block` | `ds-btn`, `ds-btn--primary`, `--secondary`, `--danger`, `--sm`, `--block` | **exists** |
| `color-solitaire-icon-button` | `ds-btn ds-btn--icon` | **exists** |
| `color-solitaire-dialog` and its parts | `ds-dialog`, `ds-dialog__title`, `ds-dialog__message`, `ds-dialog__actions` (dialogs built by the dialog service) | **exists** (skin) |
| `color-solitaire-scrim` | `ds-backdrop` | **exists** |
| `color-solitaire-toast`, `--success`, `--warning`, `--error` | `ds-toast`, `ds-toast--success`, `--warning`, `--danger` | `error` becomes `danger` |
| `color-solitaire-spinner` | `ds-spinner` | **exists** |
| `color-solitaire-progress`, `__fill` | `ds-progress` | **exists** |
| `color-solitaire-switch`, `color-solitaire-toggle-row` | `ds-toggle` | **exists**; the row layout stays a game or Controls settings-row concern |
| `color-solitaire-segmented`, `__item`, `--selected` | `fdp-tab-bar fdp-tab-bar--segmented` (`SegmentedControl`, **exists**) | |
| `color-solitaire-topbar` and parts | `TopBar` (`fdp-topbar`, `fdp-topbar__row/__start/__title/__end/__back`), **exists** | |
| `color-solitaire-list-row`, `color-solitaire-section-header`, `color-solitaire-stat-cell` | `ListRow` (`fdp-list-row`), `SectionHeader` (`fdp-section-header`), `StatCell` (`fdp-stat-cell`), **exist** | |
| `color-solitaire-loading*` | `fdp-loading`, `fdp-loading__card/__spinner/__message`, `fdp-progress` (**exist**) | |
| `color-solitaire-screen`, `--enter`, `--enter-back`, `--exit`, `--behind` | `fdp-screen`, `fdp-screen--enter`, `--enter-back`, `--exit`, `--behind` (**exist**, same suffixes) | |
| `color-solitaire-safe-area`, `color-solitaire-dialog-safe` | `fdp-safe-area` (`SafeAreaPanel`), `fdp-dialog-safe` (**exist**) | |
| `color-solitaire-compact`, `color-solitaire-regular` | `fdp-compact`, `fdp-regular`, `fdp-expanded` | size classes by width, not by "phone" and "tablet" |
| `color-solitaire-portrait`, `color-solitaire-landscape` | `fdp-portrait`, `fdp-landscape` | |
| `color-solitaire-phone`, `color-solitaire-tablet`, `color-solitaire-ios`, `color-solitaire-android` (`UiNames`) | `fdp-compact` or `fdp-regular`, `fdp-platform-ios`, `fdp-platform-android` | |
| `color-solitaire-lang-ar`, `-he`, `-hi`, `-th`, ... and `color-solitaire-rtl` | `fdp-lang-<code>`, `fdp-rtl` | |
| `color-solitaire-visible`, `color-solitaire-hidden` | the game's own utilities or `display` styles | |
| `color-solitaire-row`, `color-solitaire-col`, `color-solitaire-grow`, `color-solitaire-shrink-0`, `color-solitaire-center`, `color-solitaire-mt-*`, `color-solitaire-p-*`, `color-solitaire-text-*` | stay in the game's `layout.uss` and `base.uss` | utility classes are not part of the kit |
| `color-solitaire-card-tile*`, `color-solitaire-icon--<name>` (game icons), `color-solitaire-page*`, `color-solitaire-split*`, `color-solitaire-statsbar` | stay in the game | card-game content |

## 5. Services and root

| Color Solitaire | Kit |
|---|---|
| `UIRoot` (MonoBehaviour on a `UIDocument`: panel settings, registry, initial screen, back key, resources fallback) | `UIShellHost` (exists): a component next to the `UIDocument`; `Ready` is where the game registers its screens and calls `Shell.Boot(initialScreen)`; `Tick`, focus and pause are forwarded by the host; the back key, theme, input profile, accessibility and language classes are joined by it. State store, asset source and catalog are `UiShellOptions` (`ConfigureShell`) |
| `UIRoot._registry` (`UiRegistry`) | `ScreenDefinition` assets (direct references to UXML and USS) or `ResourcesAssetSource("Screens")` |
| `ColorSolitaireServices.Storage` | `IUiStateStore`; default `PlayerPrefsUiStateStore("<game id>")` (exists); the shell saves and restores the stack through `UiShellOptions.StateStore` |
| `PDebug` logging | `UiLog.Sink` (`Action<UiLogLevel, string, Exception>`) set once by the game (exists) |
| Handle back key in `UIRoot` | the shell's `BackDispatcher` (`UiShell.HandleBackPress`) fed by Interaction's `BackKeySource` (exists; see section 2 for the join) |
| `UIRoot.PixelsPerUnit` | `PanelScaleDriver.PixelsPerUnit` (exists) |

## 6. Panel scale

The game's `PanelScalePolicy.Compute(handheld, dpi, w, h)` keeps `ConstantPhysicalSize` on a handheld with a trusted DPI
(120 to 700), and derives the scale from the window size elsewhere (a phone window is 390 units on the short side, a tablet-sized
window 820, short side up to 1100 px counts as a phone, scale clamped to 1 to 4). The kit reproduces exactly this as the
`PanelScaleProfile.CardPortrait()` preset (exists; `Tests/Pure/Foundation/PanelScalePolicyTests.cs` is the package's side of that regression). Port the game's 11 scale tests as regression tests of the preset and
require them to pass unchanged before deleting the game's policy. Presets for other game types are in the same struct (`DesktopHud()`, `Console()`, `Web()`, `World()`, all exist), and the
profile is a plain struct of numbers, so a game with its own units writes its own.

## 7. What stays in the game

`AlertCatalog` (D1 to D30), `ChromeMetrics` and their tests (`AlertCatalogTests`), the card-table layout, card art tokens, `color-solitaire-card-tile`,
the game icons sheet, all screen classes (`Home`, `Settings`, ...), and the game's `COLOR_SOLITAIRE_Theme.tss` colour decisions. They use
the kit's types instead of the game's foundation types, and nothing in the kit refers to them.

## 8. Verification before and after

1. Before deleting anything, run the game's own EditMode and dotnet tests (`tools/dotnet/UI.Tests`) once to record the baseline.
2. After moving the pure types, the same test files must pass against the package's `Foundation.Core` with only `using` lines changed.
3. Compare screenshots of Home, Settings and one dialog at phone and tablet sizes under both themes with the pre-migration build.
4. Check that no `color-solitaire-` class is left in UXML that the game's sheets do not define (the game's `tools/check_uxml.py` does this for UXML).

## 9. Known breaking points

- A router state saved by the game (`ui/router`, the game's format tag) is not read by the package's snapshot loader. Either drop saved
  navigation state once at migration, or keep a one-time converter in the game.
- Token names change in every USS file; there is no automatic fallback from `--color-solitaire-*` to `--fdp-*`.
- Toast kind `Error` is renamed `Danger`; any code or sheet using `color-solitaire-toast--error` must change.
- The kit's size classes are three (compact, regular, expanded) where the game has two (compact, regular by form factor).
- Safe area is not applied to render-texture and world-space panels by the kit. The game has none today.

## 10. Lessons from the first real migration (Color-Solitaire, 2026-10-05 and 2026-10-06)

What follows was measured or observed while the card game moved onto the kit, not predicted. Probe numbers come from a render-texture
panel of 390 x 844 units on Unity 6000.3.25f1, run through the kit's real `UIShellHost` with the game's own panel settings.

### 10.1 What worked

- **Referencing the kit.** (The first migration used the UPM form; since 0.2.2 the kit can also be a classic `Assets/FutureDigitalPlus/UIKit` folder, see "Classic Assets package" below. Everything in the rest of this guide holds in both forms; only text paths differ.) A `file:` entry with a relative path in `Packages/manifest.json`, the kit as a folder outside `Assets` and
  `Packages`, and asmdef references by assembly name. A game needs twelve assemblies (`DesignSystem`, `Animation`, `Extras`,
  `Foundation.Core`, `Foundation`, `Interaction.Core`, `Interaction`, `Language`, `Language.UnityLocalization`, `Controls.Core`,
  `Controls`, `Bootstrap`); the composition root needs only `Foundation.Core` and `Foundation` (it names the router and context
  types). 23 kit assemblies compiled in the game's Unity run. `Interaction.InputSystem` is `autoReferenced: false` with
  `[assembly: AlwaysLinkAssembly]` and must **not** be referenced: check that its DLL is in `Library/ScriptAssemblies` and that a back press
  reaches the router, because without it the kit falls back to the legacy `UnityEngine.Input`, which throws in an Input System only project.
- **A Unity-free test harness for pure game code.** Compiling `Foundation/Core`, `Interaction/Core`, `Controls/Core`,
  `DesignSystem/Theme/Palette` and `DesignSystem/Typography/DsOsFontOrder.cs` as source into the game's dotnet test project lets pure game
  files use the kit's pure types and be tested in seconds. The kit's `PanelScaleProfile.CardPortrait()` reproduced the game's own scale
  policy on all 264 input combinations tried, so the policy could move without a visual risk.
- **A brand theme as data plus a builder.** The game wrote its palette and metrics as pure C# (with contrast tests), generated the baked
  `ThemeData` from it with `ThemePresets.Upsert` and `ThemeBaker.Bake` in a build step, and verified it with `ThemeBuildCheck`. Baking from the game's own
  Editor assembly in batch mode worked at the first try (probe P8: baked, `ThemeBuildCheck` Ok, the kit's temp folder removed); the builder compares before it writes, so a rerun on an unchanged palette is meant to bake nothing (not yet re-run in Unity at the time of writing).
- **Pinning geometry against the input-mode tier.** Kit controls are 36 px high with a mouse and 48 px with touch. A game whose layout must not change
  between the Editor and a phone writes `.fdp-shell.fdp-shell .x { height: 44px }`; the doubled selector gave 44 px in both modes.
- **`var()` in `transition-duration`** resolves on 6000.3.25f1 (`var(--x)` gave 120 ms, the same as the literal).
- **`ds-toggle`** injects its knob after `DesignSystemBehaviour.EnsureAll` and mirrors it under `fdp-rtl`.
- **The OS CJK font order** of the kit (`DsOsFontOrder`) is identical to the one the game had grown by hand (a test compares them), so the game's copy can be deleted.
- **A duplicate-plumbing scan.** A script that lists the game's public types that also exist in the kit (same name, plus a table of
  renamed twins such as `DialogResult` and `DialogOutcome`) was the first tool to write: it measured the job (67 types), and its
  end state (0, with an empty allow-list) is a definition of "done".
- **A project copy needs the kit as a sibling folder.** The relative manifest path is resolved against the copy's `Packages`, so `<copy>/../UnityPackages` must exist
  and be refreshed after every kit edit; a preflight that checks this saves minutes of import and 100 compile errors.

### 10.2 Gaps found and fixed in 0.2.1

| Gap | Fix | Why it was generic |
|---|---|---|
| A tablet layout written once for "regular or larger" had to be written twice: an iPad Pro 12.9 inch (1024 x 1366 points, short side 1024) is `fdp-expanded`, not `fdp-regular` | `fdp-regular-up` (`UiNames.ClassRegularUp`) and `UiProfile.IsRegularOrLarger` | Every game with a phone layout and a tablet layout has two tiers |
| Arabic and Hebrew screens built from `ListRow` and `TopBar` were left to right (only the chevron and the back arrow flipped) | `fdp-rtl` reverses the rows of `ListRow` and `TopBar` (margins and value alignment follow) | Any game with an RTL locale |
| `DsOsFontOrder` had tests only in the Unity run | `Tools~/PureTests -p:Module=Typography` runs them in seconds | A pure type should be tested without Unity |

### 10.3 Gaps found, not fixed (candidates for the next version)

None of these blocked the migration; each has a workaround in the game.

- `TooltipManipulator` shows a tooltip after a long press but does not stop the click that ends it, so a long press on a button also presses it.
  A `LongPressShown` flag or a click-suppression option would remove a game-side guard.
- `DsOsFonts` cannot warm the system fonts it installs for CJK (`DsFonts.Warm` handles `DsFontFamily` only), so the first CJK screen may hitch (not measured);
  the glyphs can be added by hand through the installed font assets. A `DsOsFonts.Warm(string text)` would make that one call.
- `TabBar` has no icon slot (the game finds the item button named `tab-<id>` and adds a child), raises no change event when the selected tab is tapped
  again, and `Rtl.uss` does not mirror `.fdp-tab-bar` unless it has `ds-tabs`.
- `EmptyStates` lacks the common shape "title, message, icon, one call-to-action button" (the game wrote it: a 40-line view plus a pure spec).
  `ListViewBinder.EmptyState` removes and re-adds the element on every set, even for the same instance.
- `ListRow` has no selected style for a `Button`-based skin (the game uses a class by hand). `.ds-search.is-focused` is in the style sheet but nothing sets it.
- `AccessibilityApplier` follows the operating system by default for font scale, bold text, high contrast and reduce motion. A game with a fixed
  layout proof must switch the first three off in `Ready`; consider defaults that follow only reduce motion. The system-accessibility seam
  (`ISystemAccessibility`) has no reduce-motion bridge in Unity, so "follow the OS" for reduce motion never fires without a native plugin.
- The toast service lifts the `toast` element out of its template container, so a style sheet attached to the template root is lost; the game
  attaches one with a nested `<Style>` inside the element. The kit's toast has an icon slot class but creates no icon.
- The contrast audit requires every accent to be readable as text on the page and on the panel and uses one ink for every filled variant. A brand with
  dark fills and white ink (a common look) fails exactly that check; the game wrote a documented waiver for five accents. A generic, explicit
  waiver list in the audit would be cleaner than a game-side policy.
- `ds-btn--primary` and `ds-btn--secondary` on one element: the later rule in `Buttons.uss` wins silently. `ds-icon` and a game icon class on one element fight over the box size by sheet order.
- `ThemeBaker` writes `Assets/_FutureDigitalPlusUIKitTemp/` and removes it; a consumer must git-ignore it.
- `UiShell.Boot` takes the screen factories from the catalog when it starts: a screen registered afterwards is unknown to the router, with no error. Say so in the
  Foundation guide, or log a warning.
- A `UIShellHost` placed in a project whose game code shares a type name with a kit type (`UiScreen`, `LocalizedButton`, `SafeAreaPanel`, ...) is a trap, see 10.4.

### 10.4 Surprises

- **A twin shadows the kit's type in code.** In a file under `namespace Game.UI.Shell`, `LocalizedButton` resolves to `Game.UI.LocalizedButton` (an enclosing namespace)
  before it resolves to a type imported by a `using` at the top of the file. With both types present, `Root.Q<LocalizedButton>("x")` asks for the game's type
  while the UXML (`<lang:LocalizedButton>`) creates the kit's: null at run time, no compiler warning. Delete the twin; until then alias or qualify.
- **The kit's size classes are three** (compact, regular, expanded) where a game may have two; see 10.2.
- **Bold on a `-unity-font: url(file)` face is synthesized** (about 13 percent wider on a Latin sentence, 12.8 percent in Arabic). A per-script bold needs the Bold file named in a rule;
  and `-unity-font-definition` beats `-unity-font` on one element, so a brand theme that sets a typeface overrides per-script faces.
- **The advanced text generator changes widths a lot**: an Arabic string measured 29 percent narrower and a Devanagari string 8 percent narrower with
  `-unity-text-generator: advanced`. Any change of generator needs the full locale text-fit run.
- **The language class is on the document root and the shell is its child**, so a face set on `.fdp-shell` is not overridden by an inherited one: per-script rules must also
  name `.fdp-lang-<code> .fdp-shell`.
- **A saved router state** of the game's own format is not read by the kit (`fdp-nav-v1`); the game planned a one-time converter of the header so players keep their stack.

### 10.5 Process lessons

- **Do the serial swap before the parallel screen work.** The migration was planned as: wire, then a serial gate that deletes the game's own foundation and builds
  the host, then seven packages that migrate screens in parallel. The packages ran first, on a tree that still had the twins. Each wrote code in "the vocabulary of the
  finished swap", could not compile it against the real tree, and verified it on a private emulation; the result was a mixed tree where the same name means two types and
  some screens fail at run time. The gate is the dependency of everything after it: finish it first.
- **Give the integrator the right to delete.** The swap needs the deletion of about 70 files and their `.meta` files; if the tooling refuses
  deletions, the swap cannot happen and everything depending on it stalls. Take a snapshot and arrange the permission before starting.
- **Measure first.** Baselines (test counts, screenshots, first-deal sizes, the strict text-fit matrix in locale groups) taken before the first edit made every later claim checkable
  (the screenshots were not byte-deterministic, so compare by eye or with a tolerance).
- **Probe before you design.** Nine probes (P1 to P9, see 10.1) settled design questions with numbers instead of arguments; write them as tests that log and pass, and assert only what is a decision.
- **A package edit costs more than a game workaround.** Three kit changes, each with tests and a changelog line; every other gap was solved in the game. Keep that ratio.

### 10.6 Checklist for the next game

1. Decide the rule ("plumbing that two games need goes to the kit") and the owner decisions that change looks (primary colour and ink, danger red, switch, high contrast, text sizes).
2. Snapshot the project (it may not be in git), then record baselines: test counts, a screenshot set, card or layout sizes at first launch, a strict text-fit run in every locale and device profile.
3. **Wire**: relative `file:` entry in the manifest, asmdef references by name (table in 10.1), a layering check that fails for the kit in low layers, no `FDP_UIKIT_AUTO_ATTACH` define, git-ignore the `ThemeBaker` temp folder, a project-copy script that syncs the kit as a sibling.
4. Write the **duplicate scan** and read its list; map renamed twins by hand (section 2).
5. Run the **probes**: pinned metrics against the input-mode tier, bold, text generator, toggle knob, `var()` in transitions, toast placement, high contrast on your brand theme, `ThemeBaker` in batch, the size classes at your device sizes, the Input System adapter loaded and a back press.
6. Write the **brand theme** as pure data with contrast tests, a builder step, and a verify step; list the waivers explicitly; keep game tokens in a game sheet.
7. **The serial gate**: adapt the host (`UIShellHost` with your options, themes registered before `Start`, screens registered in `Ready` before `Boot`, the accessibility `Follow*` flags set to what your layout proof allows, the Input System adapter present), delete the twins, run the scripted rename pass, rebuild generated assets, get the game green again. Only then start parallel screen work.
8. Migrate screens by family with kit classes and elements; one icon family per element; one filled variant per element; no focus rule that changes a border width.
9. Split big test classes into partial files so each package can add its surfaces; rerun the strict text-fit matrix and read the screenshots at every step.
10. End with the duplicate scan at 0, an IL2CPP export that greps the kit types the UXML uses (add a `link.xml` only for a missing type), the docs, and a note for the next game in this file.

### 10.7 Moving the kit from a UPM reference to a classic Assets folder (0.2.2)

The first game was wired with a `file:` package reference (10.1). Since 0.2.2 the kit can also be a plain folder inside `Assets` (see [GettingStarted.md](GettingStarted.md), sections 1a and 1b: "Choose UPM or classic"). Switching a game that already uses the kit:

1. Run `Tools~/make_classic_kit.py --src <kit source> --dst <game>/Assets/FutureDigitalPlus/UIKit`.
2. Remove the `com.futuredigitalplus.uikit` line from `Packages/manifest.json` (and from `testables` if it was there) and the entry from `Packages/packages-lock.json`. Both forms in one project would define every GUID twice.
3. Search the game (not the kit) for text paths: `grep -rn "Packages/com.futuredigitalplus.uikit\|UnityPackages" --include=*.cs --include=*.uss --include=*.uxml --include=*.tss --include=*.py --include=*.sh --include=*.csproj .`. Scene, prefab and asset references need no change (the GUIDs are identical); a `project://database/Packages/...` URL still resolves by GUID but logs a "was moved" message, so change it to the `Assets/FutureDigitalPlus/UIKit/...` path or to the GUID-only form.
4. Tooling that located the kit through the manifest or a sibling folder (a project-copy sync, a dotnet harness that compiles the kit's sources, a layering check that requires the manifest entry, an editor check that asks the Package Manager for the kit's version) now looks in `Assets/FutureDigitalPlus/UIKit` and reads `VERSION`. In the Solitaire game: `tools/sync_copy.sh` and `tools/lib/preflight.sh`, `tools/check_layering.py` ("install form" rule), `check_uxml.py`, `check_kit_duplicates.py`, the `UikitCompile`, `UikitEditorCompile` and `UI.Tests` harnesses, `ColorSolitaireThemeBuilder.CheckKit`, the `.gitignore` (`*.csproj` and `*.pdb.meta` would drop kit files), and the docs.
5. The kit's own tests are now in the game's Test Runner. EditMode: run everything together (the Solitaire game: 0 failures with the kit's 933 added). **PlayMode: run the game's tests and the kit's tests as two separate runs** (`--filter` on the namespace prefix). In the Solitaire game, one PlayMode run of both made two game tests fail (one hit its 180 s timeout, the other found a clock running) and about 40 kit tests fail with "The LinkedList node does not belong to current LinkedList" (UI Toolkit state left behind by the game's tests); each set passes on its own (game 160 passed and 53 skipped, kit 682 passed and 7 skipped), so the cause is the shared session and not the layout. Cause not isolated further.
6. A game keeps editing the kit in the UPM source, never in the copy; after each change run the converter again (`--check` tells whether the copy is current).
