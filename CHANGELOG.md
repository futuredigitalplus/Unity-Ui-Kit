# Changelog

All notable changes to this package are documented here. The format follows
[Keep a Changelog](https://keepachangelog.com/en/1.1.0/) and the package uses
[Semantic Versioning](https://semver.org/).

## [Unreleased]

### Added
- ScrollView module (`Runtime/ScrollView`): setchi/FancyScrollView (MIT, commit `8fce45dd`) ported from uGUI to UI Toolkit, moved in from the stand-alone package `com.futuredigitalplus.scrollview` 0.1.0, which it replaces. `FutureDigitalPlus.UIKit.ScrollView.Core` (engine-free: `ScrollerModel`, `CellLayout`, `ScrollRectLayout`, `Ease` and `EaseFunctions`, the enums) and `FutureDigitalPlus.UIKit.ScrollView` (`FancyScrollView<T>` for position-driven cells with loop and snap, `FancyScrollRect` for lists of fixed-size rows, `FancyGridView`, the cells, the contexts, `FancyScroller`). Namespace `FutureDigitalPlus.UIKit.ScrollView` (was `FutureDigitalPlus.FancyScroll`), an accepted name hazard in the lint allow-list: kit `Runtime` and `Editor` code now writes UI Toolkit's class as `UnityEngine.UIElements.ScrollView` (`VirtualGrid`, `DialogService`, `DesignSystemBehaviourBase`, `ThemeConfiguratorWindow`, `ResponsiveLayoutAudit`), so the kit still compiles as one unit (see the "Rename record" of `Runtime/ScrollView/SOURCE.md`). `.meta` GUIDs are kept. Tests: `Tests/Pure/ScrollView` (39, also `dotnet test Tools~/PureTests -p:Module=ScrollView`) and `Tests/Runtime/ScrollView` (18 PlayMode). Sample `Scroll View Basics` (`Samples~/ScrollViewBasics`; like every kit sample it ships no `.meta` files, so the stand-alone package's scene is not carried over: put the component on an empty GameObject) and `Tools~/SamplesSmoke/ScrollViewBasicsSmokeTests.cs` (6 tests that click, drag, scroll and press the sample's buttons). Guide: `Documentation~/ScrollView/README.md`; licence in `THIRD_PARTY_NOTICES.md` section 5 and `Runtime/ScrollView/LICENSE.md`.
- Responsive form factors (Foundation): `DeviceFormFactor` (`Phone`, `Pad`, `FlipCover`, `FoldCover`, `FoldInner`), `DeviceFormFactors.Classify` (by the panel size in units: short side 600, ratios 1.3 and 1.75, measured on current flip and fold phones), `DevicePosture`, `HingeAxis`, `ResponsiveRule` and `ResponsiveRules` (scale multiplier and column width per form factor and orientation, with fallback from the covers to the phone and from the inner screen to the pad), `ResponsiveRulesAsset`, and the device catalog types `ResponsiveDeviceCatalogData`, `ResponsiveDeviceSpec`, `ResponsiveScreenSpec`.
- `PanelScaleDriver.Rules`, `FormFactor`, `Rule`, `Landscape`, `Environment` and the static `Active` list; `PanelScaleEnvironment.PixelExact` (apply a physical scale as a constant pixel scale, for previews drawn on a monitor). Without rules the driver behaves as before.
- `ResponsiveRootBinder`: `fdp-ff-*`, `fdp-posture-half-opened`, `fdp-hinge-horizontal` and `fdp-hinge-vertical` classes on the root, and the rule's column width on elements with `ColumnClass`.
- Responsive Manager (`Tools > FDP UI Kit > Responsive Manager`, new editor assembly `FutureDigitalPlus.UIKit.Foundation.Editor`): device catalog of phones, tablets and 28 flip and fold profiles (`Editor/Foundation/Responsive/ResponsiveDevices.json`, extendable in `ProjectSettings/FdpResponsiveDevices.json`), overview of units and scale per device, Play mode preview of the game's panel per device, orientation and posture with safe-area and hinge overlays, layout check (`ResponsiveLayoutAudit`), rule editor with undo, a check of every device with a Markdown report and PNG export, and `IResponsivePreviewHost` for a game's screen list.
- Tests: `Tests/Pure/Foundation/ResponsiveTests.cs` (classifier on real screen sizes and thresholds, rules, screen specs, catalog validation) and `Tests/Editor/Foundation/ResponsiveEditorTests.cs` (driver rules, pixel-exact mode, registries, binder classes and columns, catalog load and merge).

### Changed
- Display name and Unity editor menus are `FDP UI Kit` (Tools, Assets, Window, Add Component, and Create). They were `Future Digital Plus UI Kit`. Tooltips, bake warnings, and stylesheet comments that still named the old top-level Design System menu now say `Tools > FDP UI Kit > Design System`.

## [0.2.2] - 2026-10-06

The kit can be installed as a "classic Assets package" (a plain folder `Assets/FutureDigitalPlus/UIKit`, no `manifest.json` entry) as well as a UPM package, from the same source. No API change; the UPM install behaves as before.

### Added
- `Tools~/make_classic_kit.py` (Python 3.9+, standard library): turns the UPM source into the classic layout. Copies everything except `package.json`, keeps every `.meta` file byte for byte (GUIDs unchanged), keeps `Samples~`, `Documentation~` and `Tools~` hidden from Unity, writes `VERSION` (kit version and the package metadata) and `.fdp-classic-manifest.json` (sha256 of every written file), rewrites the one constant that must name the install folder (`TestAssetPaths.KitAssetRoot`) and adds a banner to the README. Deterministic, idempotent (a second run changes nothing), refuses to overwrite a copy with local changes unless `--force`, mirrors removals, `--check` for CI (exit 3 on a difference), `--dry-run`, `--exclude`. Tests: `Tools~/test_make_classic_kit.py` (12 cases, no Unity). Three Unity-generated `.meta` templates it needs are in `Tools~/classic/` (`*.meta.tpl`).
- `FutureDigitalPlus.UIKit.Location.Editor` (`Editor/Location`, new assembly): `KitLocation` finds the kit by the GUID of its `Runtime` folder and returns `AssetRoot`, `FullPath`, `Install` (`Package` or `Classic`), `IsClassic`, `IsWritable`, `AssetPath(relative)` and `FullPathOf(relative)`, in either layout. The only editor code that knows both locations. Tests: `Tests/Editor/Location` (4 cases, one of which fails when any `Runtime/` or `Editor/` source hard-codes `Packages/com.futuredigitalplus.uikit` or `Assets/FutureDigitalPlus/UIKit`).
- `Tools~/Matrix/run-matrix.sh --classic`: runs any variant with the kit installed as a classic Assets package (the converter writes it into the variant project; no manifest entry, no `testables`).
- Tests: `AssetDatabaseAssetSourceTests.Kit_Asset_Root_Constant_Matches_The_Install` and `DesignSystemEditorTests.ThemePreviewUxml_StyleSheetReference_ResolvesByGuid_InEitherLayout`. EditMode is 933 in a clean project (927 before, 6 added); PlayMode is unchanged at 678 passed and 8 skipped.
- Documentation: "Choose UPM or classic" and "Install as a classic Assets package" in `README.md`, `Documentation~/GettingStarted.md` (section 1b), `Documentation~/Upgrading.md` ("Updating a classic copy"), `Documentation~/Migration.md`, and the classic results in `Documentation~/Verification.md` and `Documentation~/CompatibilityMatrix.md`.

- Repository files: the kit lives in its own git repository (`git@github.com:futuredigitalplus/Unity-Ui-Kit.git`), whose root is the package root, so a git install needs no `?path=`. `.gitignore` (Unity output, editor files, secrets; keeps `.meta`, `~` folders and DLLs), `.gitattributes` (LF for text, binary for DLLs and images, upstream CRLF stylesheets verbatim), `Documentation~/CONTRIBUTING.md` and `Documentation~/RELEASING.md` (tags, HTTPS and SSH install lines, the classic copy, how a game updates, the kit repository inside another repository). The install lines in `README.md`, `Documentation~/GettingStarted.md` and `Documentation~/Upgrading.md` no longer name the Solitaire repository paths. Open item 1: the package licence is still an owner decision.

### Changed
- `ThemePresets.ThemesFolder` and the editable check use `KitLocation` instead of `PackageInfo.FindForAssembly` and the id fallback. In a registry or git package the themes are still read-only; in the classic folder they are writable.
- The editor tests that read the kit's own files (`PackageFiles`, `ModuleTests`, `ThemingEditorTests`, `InteractionStyleSheetTests`, `DesignSystemEditorTests`) find the kit with `KitLocation`, so the same tests pass in both layouts.
- `Editor/DesignSystem/Theme/ThemePreview.uxml` refers to `DesignSystem.uss` by GUID only (`project://database/?fileID=...&guid=...&type=3`): a URL with a path of the other layout would still resolve but log a "was moved" message on import.
- `DsFx.cginc` comment: both include paths (`Packages/com.futuredigitalplus.uikit/...` and `Assets/FutureDigitalPlus/UIKit/...`).
- `Tools~/Matrix/matrix.py prepare` takes `--classic`.

### Verified (Unity 6000.3.25f1, macOS arm64)
- Classic layout, clean `Library`: 0 compiler errors, 0 warnings from the kit; EditMode 934 of 934 (933 of the kit and the scratch project's own smoke test); PlayMode 678 passed, 8 skipped, 0 failed; iOS, Android and macOS player scripts compile (18 assemblies each); variants URP 2D and "all optional packages" in the classic layout match the UPM numbers. The UPM layout of this version: 928 EditMode, 678 passed and 8 skipped in PlayMode. Numbers in `Documentation~/Verification.md`.
- Finding for host games: in the Solitaire game (about 214 PlayMode tests of its own) one PlayMode run of the game's tests together with the kit's 689 made two game tests and about 40 kit tests fail (UI Toolkit state left behind by the game's tests; "The LinkedList node does not belong to current LinkedList"); run separately, the game's 160 and the kit's 682 pass (7 and 53 skipped). EditMode together: 8,394 passed, 0 failed. Run the two PlayMode sets as separate runs (`--filter`), see `Documentation~/Migration.md` 10.7. Not a defect of the layout (the kit alone passes in both layouts), not isolated further.

## [0.2.1] - 2026-10-06

Three generic gaps found while the first host game moved its UI onto the kit. No breaking change; nothing is renamed.

### Added
- `fdp-regular-up` (`UiNames.ClassRegularUp`) on the shell root while the size class is Regular or Expanded, and `UiProfile.IsRegularOrLarger`. For layouts that have two tiers ("phone" and "tablet or larger"): without it an iPad Pro (short side 1024 units, Expanded) would need every tablet rule written twice, once for `fdp-regular` and once for `fdp-expanded`. Tests: `UiProfileTests` (9 new cases), `ProfilePlayModeTests` (the three class checks).
- `Tools~/PureTests -p:Module=Typography`: `DsOsFontOrder` and `DsOsFontOrderTests` (43 cases) now also run in the Unity-free pure run, in seconds. 596 pure tests in all.

### Fixed
- Right-to-left layout of `ListRow` and `TopBar`: with `fdp-rtl` the row lists icon, text, value and chevron from the right (row-reverse, the margins and the value alignment follow) and the bar lists back button, title and action slot from the right. Before, only the chevron and the back arrow were flipped, so Arabic and Hebrew screens built from these views were left-to-right. Tests: `RtlPlayModeTests` (3, on a render texture).

### Documentation
- `Documentation~/Migration.md` section 10: the lessons of the first real migration (what worked, the gaps fixed here and the ones still open, surprises, process lessons, a checklist for the next game).

## [0.2.0] - 2026-10-06

First release for global use: seven new modules, a ready-made shell host, collision-proof names, and the fixes of four reviews (adversarial API use, three host games, licences, documentation). Breaking changes are listed under Changed (no consumer exists yet).

### Added
- **Bootstrap** module (`FutureDigitalPlus.UIKit.Bootstrap`): `UIShellHost`, a component next to a `UIDocument` that installs the styles, wears the Dark and Light themes, binds the input profile, accessibility and language classes, builds a `UiShell`, sends Escape, gamepad B and the Android back button to it, and raises `Ready` for the game to register its screens; `InputProfileModeSource`. Guide: `Documentation~/Bootstrap/README.md`, recipe 10; tests in `Tests/Runtime/Bootstrap`.
- `Foundation.Core` (`FutureDigitalPlus.UIKit.Foundation.Core`, pure C#, `noEngineReferences`): `ScreenStack`, `DialogQueue` with `DialogRequest`/`DialogTemplate`/`DialogOutcome`, `ToastQueue`, `LoadingTokens`, `InputBlockCounter`, `BackDispatcher`, `UiText` with `LocalizationKeyResolver`, `UiProfile` (orientation, size class `Compact`/`Regular`/`Expanded`, input mode), `PanelScalePolicy` with `PanelScaleProfile` presets (`CardPortrait`, `DesktopHud`, `Console`, `Web`, `World`), `SafeAreaInsets` and math (`WithMinimum` for TV margins), `UiAccessibility` and `UiMotion`, `LocaleStyle`, `UiLog`, and the service interfaces. Ported from the card game's foundation and generalised; 227 pure tests.
- `Foundation` (`FutureDigitalPlus.UIKit.Foundation`): `UiShell` (ordered layers, no singleton), `UiScreen`, `ScreenRouter` (saved stack, transitions as USS classes), `DialogService` with `Confirm`/`Alert`/`Choice`/`Input` presets, `ToastService`, `LoadingOverlay`, `SafeAreaPanel` and `SafeAreaProvider` (no padding on render-texture and world-space panels), `PanelScaleDriver` (works on a runtime copy of `PanelSettings`), `UiProfileWatcher` (`fdp-portrait`, `fdp-compact`, `fdp-platform-*`, `fdp-input-*`), `FocusTrap` and `UiFocus` (nested traps), `UiInputGate`, `UiTextSource`, `ScreenDefinition`, asset sources, `PlayerPrefsUiStateStore`, `TopBar`, `ListRow`, `SectionHeader`, `StatCell`, `BannerView`, `PauseMenu`; style sheets `Foundation.uss` and `Foundation.Tokens.uss`.
- `Tools~/PureTests`: `dotnet test` runs the pure tests of every Core assembly in seconds, with no Unity.
- `Documentation~/Foundation/README.md`: guide, and the table that maps the card game's `ColorSolitaire.UI` names to the kit.
- New module `Interaction` (`Interaction.Core` pure C#, `Interaction`, optional `Interaction.InputSystem`): `InputProfile` (last-used device as classes `fdp-input-mouse|touch|keyboard|gamepad` and `fdp-input-nav`, `ModeChanged`, `Force`, `NavigationEnabled`), `Activate.Register` (Submit and click fire once), `BackKeySource` (Escape, Android back, gamepad B, `NavigationCancelEvent`, one press once per frame), `AccessibilityApplier` (`fdp-reduce-motion`, `fdp-text-scale-125..200`, `fdp-bold-text`, `fdp-high-contrast`; options plus OS font scale and bold text), `ISystemAccessibility`, `InteractionStyles` with `Focus.uss` (focus ring only for keyboard and gamepad) and `A11y.uss`. Works with the legacy Input Manager, the Input System, or both; nothing is required of the game.
- `Components`: event interfaces `IOnNavigationSubmit`, `IOnNavigationCancel`, `IOnFocusIn`, `IOnFocusOut`, `IOnPointerDown`, `IOnPointerUp`, `IOnPointerMove`, `IOnKeyDown`.
- `Animation`: `EatkMotion.Scaler` / `EatkMotion.Scale`, the reduced-motion hook for every animation the module starts.
- `Extras`: `TooltipManipulator.NavigationClass`, `LongPressDelay`, `LongPressLinger`; `PopupPanel.BackRegistrar`; `Slider2D.KeyStepFraction`.
- New module `Hud` (`Hud.Core` pure C#, `Hud`): `MeterBar` (damage trail, segments, thresholds with hysteresis, `ds-meter` skin), `RadialProgress` (`Painter2D`), `CooldownIcon`, `CountUpLabel`, `CurrencyLabel`, `CountdownLabel` (injectable clock), `FloatingTextLayer` (pooled), `WorldAnchorLayer` and `WorldAnchor` (any `Camera`, no Cinemachine dependency), `TimedFeed`, `HudZones`, `NumberFormatter`; `HudStyles.Install`. Element scheduler only, no steady-state allocations, unscaled time, reduced motion respected.
- New module `Controls` (`Controls.Core` pure C#, `Controls`, needs `Foundation` for `FocusTrap` only): `TabBar` and `SegmentedControl` (badges from a `BadgeTree`), `PopupMenu` (submenus, edge flip, type-ahead, `Attach` for right click, long press, menu key and gamepad button) with `MenuEntry` and `MenuModel`, `SelectField` (gamepad-friendly select), `BottomSheet` (snap points, drag, flick, back), `ListViewBinder<T>` and `VirtualGrid<T>` (stable keys, scroll kept, empty and loading states, keyboard and gamepad focus movement), `EmptyStates`, `RewardPopup` and `RewardPopupQueue`; `ControlsStyles.Install`.
- New module `Gameplay` (`Gameplay.Core` pure C#, `Gameplay`): `ItemStack`, `InventoryModel` (stack limits, merge, split, swap, locked slots, drop rule, moves between inventories, a reason for every refusal), `PickupMode`, `ItemSlot`, `SlotGrid` (pointer drag with the `ds-drag-ghost`, keyboard and gamepad pick-up, split by modifier, linked grids, hover, focus, activation and context events); `GameplayStyles.Install`.
- Documentation: `Documentation~/Hud`, `Documentation~/Controls`, `Documentation~/Gameplay`.
- `DesignSystem` theming: runtime themes (`ThemeData.CreateRuntime`, `FromBrand`, `SetToken`, `extraTokens`, `ComputeRevision`; `ThemeRuntime.Apply` builds a stylesheet by patching a baked one, a new clone per change, destroyed with its last user), `ThemeManager` (named themes, attached roots, persistence, `IThemeModeSource`), `ThemeVariants` (high contrast, text scale), `ThemeRuntime.SetHighContrast`, `fdp-no-transition` for theme swaps, a pure palette layer (`ThemePalettes`, `ContrastAudit`, `ThemeDerivation`, `ColorMath`), four more built-in themes (`NeutralDark`, `NeutralLight`, `HighContrastDark`, `HighContrastLight`), `ThemeBuildCheck` (fails a build with an unbaked or stale theme), a contrast audit foldout in the theme inspector, `Lists.uss` (Unity list, tree, foldout, tab and group-box controls), new tokens (`--fdp-color-success`, `-info`, `-scrim`, `-focus-ring`, `-selection`, `--fdp-font-size-display`, `--fdp-transition-slow`, control and layout metrics). Guide: `Documentation~/DesignSystem/THEMING.md`.
- New module `Language` (`FutureDigitalPlus.UIKit.Language`, optional `Language.UnityLocalization`): `UiLocalization` (null backend by default: fallbacks and `[KEY]`, never throws), `ILocalizationBackend`, `DictionaryTextSource` (code, CSV, JSON), `LocalizedTextBinder`, `BindText`/`BindKey`, `LocalizedLabel`/`LocalizedButton` (UXML), `LocaleClassApplier` (`fdp-lang-<code>`, `fdp-rtl`, `languageDirection`), `LanguageStyles` + `Rtl.uss` (about 100 selectors that mirror the design system under `.fdp-rtl`), the Unity Localization adapter (`UnityLocalizationBackend`, `BindText(TextElement, LocalizedString)`). Guide: `Documentation~/Language/README.md`.
- `DesignSystem` fonts: `DsOsFonts` (operating-system CJK and, when shapeable, Arabic, Hebrew, Thai, Devanagari, Bengali, Tamil and Khmer fonts as a per-panel fallback, by family name, no font files), `DsOsFontOrder`, `DsFonts.ApplyLocale`, `DsFonts.FallbacksOf`, `DsFonts.PrepareRuntimeTextSettings`.
- `Extras`: `BlurSource` (`Blur`, `FromCamera`, `IsSupported`, `TargetSize`, `ClampIterations`), a render pipeline independent Kawase blur for `BlurPanel` (`Hidden/FutureDigitalPlus/UIKit/Blur/Kawase`, `Graphics.Blit`; verified on a step image in Built-in, URP 3D and HDRP in the analysis prototype).
- `DesignSystem`: `DsGoogleFonts.CacheDirectory` (settable, falls back to `Application.temporaryCachePath`), `CacheSubfolder`, `IsSupported` and `IsSupportedOn`; the download refuses up front on consoles and tvOS unless a folder was chosen.
- `Tools~/Matrix`: `run-matrix.sh`, 14 variants (pipelines, colour space, input handling, optional packages), IL2CPP export and player compile scripts, one table.
- `Tools~/CompileCheck`: Roslyn compile of all assemblies without Unity, XML-doc check (CS1591) for the new modules, name-hazard check, dotnet runner for the lint tests.
- `Tests/Editor/Lint`: rule tests on the package (USS features, `--fdp-*` variables, class prefixes, resource paths, `.meta` files, name hazards) with an allow-list.
- `Tests/Runtime/Platform`: render pipeline smoke tests (themed panel paints, shaders load), `Wait`, culture, font cache and blur tests.
- `Documentation~/Platforms.md`: verified matrix, not-verified list, per platform notes, what ships in a build.
- `Tools~/rename/`: the rename script, the mapping table and the generator rebuild recipe.
- Tests that guard the token rename: the baked theme sheets carry only `--fdp-*` names, and `ds-btn--primary` paints the applied theme's primary colour.
- `Documentation~/Upgrading.md`: "Re-syncing after the rename".
- `Documentation~/ChooseModules.md` (modules by game type, platform and optional package, minimum assembly sets) and
  `Documentation~/Migration.md` (card game foundation: type, token and class tables).
- Six samples for global use (`Samples~`, listed in `package.json`): Shell Quick Start, RPG Inventory, Action HUD, Gamepad Menu, Responsive, Theming and Language. Each is one component that builds its own
  document in code, works in any render pipeline and with any input backend, and has a README. They compile in Unity and with Roslyn (`Tools~/CompileCheck --samples`), and `Tools~/SamplesSmoke` runs them in PlayMode in eight matrix variants; no input was driven and nothing ran on a device.
- `Tools~/SamplesSmoke`: one PlayMode smoke test per new sample and a script that copies the samples and the tests into a scratch project.
- `Documentation~/Samples.md` (the eleven samples, what each shows, what was verified); `ChooseModules.md` and `Migration.md` re-checked against the module sources (the tables mark what exists and what is
  planned); `GettingStarted.md` section 4b (shell, HUD, inventory, gamepad, responsive, theming and language), `Upgrading.md` (new modules, checks that need no Unity), `Verification.md` (what was run for the new modules and what is still to do).
- `Documentation~/Recipes.md`: ten copy-paste recipes (a bootstrap component that joins the modules, themed screen, dialogs, toasts and loading, gamepad navigation, HUD bar, inventory grid, localization hook, brand theme, one UI on every screen size); every block is compiled and the recipes are run in Unity by the documentation check. A "Which modules do I need?" table in the README. Known issue documented: a file with `using FutureDigitalPlus.UIKit;` and a `UIComponent` class does not compile (generator).

### Changed
- `DesignSystem` behaviours: nav items, rail items, sheet rows, bottom-nav items and tab items are focusable and take Submit (a `ClickEvent` is raised from it); Cancel closes an open drawer; scroll bars flash on focus; drag and drop has a keyboard and gamepad alternative; spinners and the skeleton shimmer respect `fdp-reduce-motion`; the spinner loop idles at 100 ms when nothing spins (was 16 ms always).
- `Extras`: tooltip also on keyboard and gamepad focus and on a long press with a finger; popup keeps focus inside and returns it, closes on Cancel; colour field and `Slider2D` work with Submit, arrows and the D-pad; tooltip placement is in the parent's local space (world-space panels). The seven Extras sheets take their colours from the DesignSystem tokens with the old demo palette as fallback (a control no longer has no colours without a sample); `--fdp-font-bold` is no longer read.
- `MaskedInputField` places the caret on `PointerUpEvent` (finger and pen).
- `DesignSystem`: contrast fixes (WCAG AA over every shipped theme). Dark hover and press fills are lighter, Dark `tertiary` and `border-strong` changed, Light `primary`, `warning` and `border-strong` changed; control boundaries use `--fdp-color-border-strong`; filled secondary, tertiary and danger buttons use the on-accent ink. The two built-in themes look slightly different.
- `DesignSystem`: `Mobile.uss` (the `fdp-input-touch` tier) no longer sets font sizes or 40 per-component heights; it redefines `--fdp-control-h-*`, `--fdp-field-h`, `--fdp-tab-h`, `--fdp-chip-h`. Controls use `min-height: var(--fdp-control-h-md, 36px)` instead of a fixed `height`, so larger text grows them.
- `DesignSystem`: the text selection colour follows the theme (`--fdp-color-selection`); the focus border of text fields applies in Unity 6000.3; literal transition durations in `Controls.uss` use the duration tokens.
- `DsFonts.InstallFallbacks` adds to the panel's list instead of clearing it and no longer writes into a shared `PanelTextSettings` asset (it gives the panel a runtime copy).
- `ThemeRuntime.Apply` accepts runtime themes (`ThemeData.CreateRuntime`); a `ThemeData` without a baked stylesheet that is not a runtime theme still returns false.
- **Collision-proof names** (`Tools~/rename/rename.py`, mapping in `Tools~/rename/rename-map.md`). Assemblies and namespaces are
  `FutureDigitalPlus.UIKit.<Module>` (were `DesignSystem.Runtime`, `DesignSystem.Editor`, `UIComponents[.X]`,
  `id.instance.elementanimationtoolkit`, `Game.UI`). Resources live under `FutureDigitalPlus/UIKit/<Module>/...` (were `UI/Styles/DesignSystem`,
  `UI/Themes`, `Textures/Icons`, `Fx/Shaders`, `Style`, `GameUI/Styles`, the test and sample folders and `DsFonts`). USS custom properties
  are `--fdp-*` (were `--color-*`, `--space-*`, ...; `--ds-font-*` and `--unity-*` unchanged), Extras classes `fdp-*`, `.back-btn*` is
  `.ds-back-btn*`, `is-spinning` is `ds-is-spinning`, the touch-size class `mobile` is `fdp-input-touch`. The shader is
  `Hidden/FutureDigitalPlus/UIKit/DsFx/Blueprint`, menus are under `Future Digital Plus UI Kit`, the temp folder is
  `Assets/_FutureDigitalPlusUIKitTemp`, test assemblies are `<Module>.Tests[.Editor]`.
- Components: the four Roslyn DLLs are rebuilt from upstream's source (namespace literals patched, new assembly names, Roslyn 4.0.1),
  because upstream's DLLs generate nothing in a renamed namespace. Hashes, recipe and patch: `THIRD_PARTY_NOTICES.md`.
- DesignSystem: attaching the behaviours to every `UIDocument`/`PanelRenderer` at each scene load is now opt-in (scripting define
  `FDP_UIKIT_AUTO_ATTACH`). `UIKitInstaller` still runs `EnsureAll` once per panel. The pre-release opt-out define
  `DS_DISABLE_AUTO_ATTACH` is gone.
- `package.json`: `unity` is `6000.3` (the only verified editor version).
- Animation: `Easy` (easing curves) no longer carries decompiled code of Unity's own `Easing` class (licence review, 2026-10-06): the standard curves
  call `UnityEngine.UIElements.Experimental.Easing`, `OutExpo` and `EaseInOutQuint` stay as upstream wrote them. Same members, same values.
- Licence records: the `SOURCE.md` of DesignSystem, Animation, Components and Extras now list every change since the first import (previously only the rename and the first fixes); `THIRD_PARTY_NOTICES.md`
  lists the files we wrote inside the upstream modules, the six themes, the optional and development-only references, and the Castle.Core copyright year; changed upstream-derived files that lacked the
  `FutureDigitalPlus:` marker got one (four Editor C# files, `DesignTokens.uss`, the Extras style sheets).

### Fixed
- Components generator: a file with `using FutureDigitalPlus.UIKit;` that declares a `UIComponent` class outside that namespace compiles (the generator wrote `Components.IAssetResolver`, CS0246). Only `Generation.dll` was rebuilt; hashes, patch and recipe in `THIRD_PARTY_NOTICES.md`, `Runtime/Components/SOURCE.md` and `Tools~/rename/README.md`. Test: `Tests/Runtime/UIKit/ComponentWithFacadeUsingTests.cs`.
- UIKit facade: the default safe area is the whole screen for a panel that renders into a `RenderTexture`, a world-space panel or a panel on another display (an in-game monitor was padded by the phone's notch); with a shell under the root the host also stops writing and removing `fdp-input-touch` (the input profile owns it). A `SafeAreaSource` you assign is applied to every panel.
- `Tools~/rename/rename.py --apply` is idempotent again: on the package it rewrote 13 files of the modules written after the rename (a class suffix such as `UssClassName + "--empty"` or an example token such as `--mygame-color-health` read as an old custom property). The script now leaves those modules, their tests, samples and guides alone (`VERBATIM_GLOBS`) and never rewrites a `+ "--x"` literal.
- `Tools~/rename/rename.py --verify-only` reports 0 problems on the package again (it flagged development tools, lint test data and the deliberately undeclared `--fdp-color-focus-ring`; those are now excluded by name in the script).
- Foundation: the loading overlay's invisible card and spinner took pointer input in the middle of the screen while no loading was going on (a `pickingMode` on the backdrop does not reach its children). Only the backdrop takes input now, and only while a blocking token is alive. The banner's action and dismiss buttons are at least 44 units in the touch tier.
- Controls: `PopupMenu` never added its rows to the menu panel (an empty menu). `PopupMenuBinding` listens in the trickle-down phase, so a touch long press and a right click work on a `Button`.
- Hud: `WorldAnchorLayer` projects with the viewport fraction of the camera's target scaled to the panel's size; `RuntimePanelUtils.CameraTransformWorldToPanel` assumes the panel covers the screen and put anchors of a render-texture panel in the wrong place.
- Interaction: the focus ring on items without a border (`ds-nav-item`, `ds-sheet__row`, `ds-bottom-nav__item`, `ds-rail-item`) reserves its width with a transparent border in navigation mode, so a focus move no longer pushes the next item aside.
- Extras (upstream-derived, see `Runtime/Extras/SOURCE.md`): the tooltip's hover is for the mouse only and its long press now works on a button; `PopupPanel.Show` takes focus after a first call that could not.
- DesignSystem: the six built-in theme assets were generated and baked (only `Dark` and `Light` existed, from before the contrast fixes).
- Documentation: `CompatibilityMatrix.md` (what was run and what was not), `Verification.md`, `Platforms.md`, `Samples.md`, `ChooseModules.md` and the sample READMEs state the real results; `GettingStarted.md` says a `PanelSettings` needs a theme style sheet (without it the document root has no height).
- Tools: `Tools~/Matrix/run-matrix.sh` starts Unity directly (no Unity CLI), `--samples` runs the sample smoke tests, `PlayerCompileCheck` also tries Windows, Linux and WebGL (reported as "no module" when not installed), the unsupported HDRP Gamma variant is gone. `Tools~/SamplesSmoke` covers all eleven samples.
- Lint: three rules on the assembly definitions (`asmdef-foreign-reference`: the kit depends on Unity and itself only, no game code; `asmdef-optional-unconstrained`: an assembly that references an optional package has a define constraint, so the package compiles without it; `asmdef-core-engine`: the `.Core` layers have no engine reference). They run in Unity EditMode and under `dotnet test Tools~/CompileCheck/LintRunner`.
- Tests: the PlayMode panels of Foundation, Hud, Controls and Gameplay use Unity's default runtime theme like a game does (the document root gets its size, text its font); other test fixes: a `SlotGridTests` panel was reused after it was destroyed, a bottom-sheet "slow drag" test waited frames that take a millisecond, pointer events are sent to elements that have a size and are not at the origin, a Hud countdown is checked to the minute.
- Enter and the gamepad south button now activate design-system items and Extras colour fields (they raised no `ClickEvent`).
- Lint: the 10 allow-listed undefined `--fdp-*` variables of the Extras sheets are gone; the allow-list for `uss-undefined-variable` is empty.
- `Animation`: `Wait.Until` and `Wait.While` run their condition on the main thread, frame paced, with a real-time timeout (upstream used `Task.Run` and `Task.Delay`: UnityEngine calls were illegal in the condition and the wait never completed on WebGL). Signatures unchanged; `frequency` is now the minimum gap between checks.
- `Animation`: `UIParser` parses numbers with the invariant culture (`"0.5"` was 5 in de-DE); documented as editor-time tooling.
- UIKit facade: with a `UiShell` under the document root the host no longer pads the root by the safe area (the shell's top bars, dialogs and toasts pad themselves; the notch was applied twice and every scrim stopped short of the screen edge).
- UIKit facade: the design system behaviours are re-scanned every 250 ms (`EnsureBehaviours`), so a `ds-toggle` in a screen built after the install gets its knob.
- Interaction: the text size steps and the reduced-motion tokens beat the theme sheet whatever the sheet order (they repeat their class in the selector); before, a 125 percent step changed nothing while any stock theme was worn.
- Hud: `MeterBar` has a minimum width of 160 units (in a `HudZone` it shrank to its label, or to 2 units without one).
- Docs: GettingStarted (Source Asset with a shell, `ds-btn` needs a variant, host and shell on one document, saved navigation, the in-game screen as router root, HUD zones, testing a menu without a device), Hud README, UIKit README, `UIKitOptions.EnsureBehaviours` tooltip (was wrong: it promised the opt-in auto-attach).
- Tests: `HostIntegrationTests` (UIKit) and `HudZoneLayoutTests` (Hud) guard the four fixes above.
- Foundation: `DialogService` is `IDisposable` and `UiShell.Dispose` calls it: a dialog callback that shows a follow-up while the shell is disposed can no longer leave a modal open (`DialogQueue.Shutdown`, `IsShutDown`). `ScreenRouter` navigation after `Dispose` does nothing (it built screens and held an input block).
  `LoadingOverlay` ignores a `LoadingToken` of another overlay (`LoadingToken.IsIssuedBy`); ids repeat between overlays. `PanelScalePolicy` ignores a DPI of 0 (a profile nobody filled in gave a scale of 0) and caps the user scale at `MaxUserScale` (10).
  `FoundationRuntime` also resets `UiLog.Sink`, and `SafeAreaProvider.Default` drops its `Changed` subscribers, when Play Mode starts without a domain reload.
- Facade: `UIKitInstaller.Install` on a root that already has an installation disposes the old one first (two installations restored each other's padding wrongly); `SetTheme` after `Dispose` no longer dresses the root; the 500 ms safe-area poll no longer allocates two strings.
  `UIKitMotion.FadeIn` / `FadeOut`: a new fade replaces a running one, so FadeOut then FadeIn ends visible (the late completion of the first one hid the element).
- DesignSystem: `ThemeRuntime.Clear` removes the scope class even when a runtime theme was destroyed while worn; high contrast keeps its own record: `IsHighContrast` is false once the root was cleared or dressed in another theme, turning it off no longer
  replaces (or destroys) a theme the game applied in the meantime. With the domain reload off, `DsGoogleFonts` (in-flight and failed downloads, loaded fonts), `DsFonts` and `DesignSystemEvents` no longer keep the state or the subscribers of the last play session; the same for `DiContext` in Components.
- Animation: the delayed animations (`*Delayed`, `Animate` with a value store) and `ExecuteIn` schedule on the element itself; on an element without a parent they threw a `NullReferenceException`.
- Interaction: a backend or a subscriber of `InputFeed` that throws is logged and no longer stops the other backends from starting, polling or stopping, nor keeps the poll driver from being created.
- Hud, Controls, Gameplay: NaN and infinities no longer poison the models (`MeterModel`, `CooldownModel` stuck "on cooldown" for ever, `CountUpModel`, `RewardSequence`, `FloatingTextModel` and `TimedFeedModel` entries that never expire, `RadialProgress` that repainted for ever,
  `CountdownModel.Format`, `WorldAnchorMath`, `PopupPlacement`, `GridLayoutMath.VisibleRows` returning rows past the end). A `WorldAnchor` whose `Transform` was destroyed is hidden instead of jumping to the world origin. `NumberFormatter.Compact` with an empty suffix list no longer throws.
  `InventoryModel.FirstSlotOf(null)` returned the first empty slot, `Remove(null, n)` raised a change event for every empty slot; a null or empty id now matches nothing.
- Extras: `Tooltip` shows nothing for a target without tooltip text (it showed an empty bubble) and closes when its target leaves the panel under the pointer (`PointerOut` never came).

## [0.1.0] - 2026-10-05

### Added
- **DesignSystem** module from sinanata/unity-ui-toolkit-design-system (MIT, commit 65f6211): tokens, 15 stylesheets,
  Dark and Light themes, theme runtime, behaviours, typography tooling, 120 icons, editor tooling, guides, a showcase
  sample. Unity 6000.5-only features are guarded.
- **Components** module from jonisavo/uicomponents (MIT, commit dc64355, 1.0.0-beta.13): framework, `TestBed`, optional
  Addressables source, editor tools, prebuilt Roslyn generators and analyzers (SHA-256 recorded), six samples, upstream tests.
- **Animation** module from instance-id/ElementAnimationToolkit (MIT, commit a5d5184, 0.1.93), made runtime-safe and Unity 6
  clean; the editor window and fonts are not included.
- **Extras** module from plyoung/UIElements (Unlicense, commit 8fac813): aspect ratio padding, colour field and picker,
  popups, tooltip, blur panel UI.
- **UIKit facade** (new): `UIKitHost`, `UIKitInstaller`, `UIKitInstallation`, `UIKitOptions`, `SafeAreaInsets`, `UIKitMotion`.
- Samples: UI Kit Quick Start, Design System Showcase, Components Samples, Animation Toolkit Demo, Extras Stylesheets.
- Guides: GettingStarted, Upgrading, UpstreamFeatures, UIKit, and one folder per module; `THIRD_PARTY_NOTICES.md` and a
  `SOURCE.md` per module.
- Tests: upstream Components tests plus new tests for every module and a cross-module test (counts: see `Documentation~/Verification.md`).

- DesignSystem: scripting define `DS_DISABLE_AUTO_ATTACH` stops the behaviour being added to every UIDocument/PanelRenderer.
- Fixed: `BlurPanel` no longer produces a non-finite `sourceRect` while the document root has no size (marked change, see Extras `SOURCE.md`).
- Fixed: `UIKitInstallation.Dispose` restores the root's previous inline padding instead of clearing it.
- Tests: landscape/portrait safe-area test; `CustomThemeTests` (a game-owned theme bakes and applies).

### Notes
- Verified on Unity 6000.3.25f1: 0 compiler warnings, all tests pass, iOS/Android/macOS player scripts compile.
- Package licence: TODO (owner to decide).
