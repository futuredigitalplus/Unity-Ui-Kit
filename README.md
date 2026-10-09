# FDP UI Kit

`com.futuredigitalplus.uikit` is a Unity 6 (6000.3+) package for UI Toolkit. It bundles four open-source
projects as independent modules, adds seven modules of our own (screens and dialogs, input and accessibility, localization, HUD, menu controls, inventory, and a ready-made shell host) and a small facade,
so any game, of any genre, on any platform and render pipeline, can take the whole kit or one module.

> **Status:** 0.2.0. The four upstream modules, the seven modules written for global use and the facade compile without warnings on Unity
> 6000.3.25f1 and pass their tests there in 13 variants (render pipelines, colour spaces, input backends, optional packages; see "Verification").
> Player-script compile checked for iOS, Android and macOS. Not yet used in a shipping game, not run on a device, a console or a headset.
>
> **Names:** the display name is `FDP UI Kit` (Unity menus under Tools, Assets, Window, Add Component, and Create). The package id and the `FutureDigitalPlus.UIKit` prefix are still placeholders the owner may
> rename (checklist in `Documentation~/Upgrading.md`, "Renaming the package"). Every name the kit puts into a game's
> global namespaces is collision-proof: assemblies and namespaces `FutureDigitalPlus.UIKit.<Module>`, Resources under
> `FutureDigitalPlus/UIKit/<Module>/`, USS custom properties `--fdp-*`, Extras classes `fdp-*`. The generic names of the four
> upstream projects (`DesignSystem.Runtime`, `UIComponents`, `id.instance.elementanimationtoolkit`, `Game.UI`) survive only in
> the "Rename record" of each module's `SOURCE.md` and in `Tools~/rename/rename-map.md`.

## Open items

1. **Package licence (owner decision, not made).** The code and guides written by Future Digital Plus carry no licence yet and the repository has no `LICENSE` file on purpose; see "Licence" at the end of this file and [Documentation~/RELEASING.md](Documentation~/RELEASING.md). Bundled upstream components keep their own licences.

## Which modules do I need?

Start with the first row and add what your game has. Every module is its own assembly set; leave out what you do not use. The full table by game type and the assembly names are in [Documentation~/ChooseModules.md](Documentation~/ChooseModules.md).

| Your game has | Add | Start from |
|---|---|---|
| any UI Toolkit screen | DesignSystem + UIKit facade | [Quick Start](Samples~/UIKitQuickStart/README.md) |
| more than one screen, dialogs, toasts, a loading state | Foundation (+ Bootstrap: `UIShellHost` joins the modules for you) | [Shell Quick Start](Samples~/ShellQuickStart/README.md), [Bootstrap](Documentation~/Bootstrap/README.md) |
| a HUD over the game (bars, cooldowns, damage numbers, name plates) | Hud | [Action HUD](Samples~/ActionHud/README.md) |
| an inventory, a loadout, a collection | Gameplay + Controls | [RPG Inventory](Samples~/RpgInventory/README.md) |
| a gamepad, a keyboard, a console or TV, accessibility options | Interaction (+ Controls for tabs, menus, select fields) | [Gamepad Menu](Samples~/GamepadMenu/README.md) |
| more than one language, or a right-to-left one | Language | [Theming and Language](Samples~/ThemingLanguage/README.md) |
| phones, tablets, foldables and resizable windows | Foundation (`PanelScaleDriver`, size classes, safe area) | [Responsive](Samples~/Responsive/README.md) |
| your own brand colours | DesignSystem (`ThemeData.FromBrand`) | [Recipes, brand theme](Documentation~/Recipes.md#8-a-brand-theme) |

Copy-paste solutions for the common tasks (one bootstrap component for the whole kit, a themed screen, dialogs, toasts, gamepad navigation, a HUD bar, an inventory grid, localization, a brand theme, one UI on every screen size) are in [Documentation~/Recipes.md](Documentation~/Recipes.md).

## Modules

| Module | Folder | Upstream | Licence | Depends on |
|---|---|---|---|---|
| DesignSystem | `Runtime/DesignSystem`, `Editor/DesignSystem` | [sinanata/unity-ui-toolkit-design-system](https://github.com/sinanata/unity-ui-toolkit-design-system) | MIT | nothing |
| Components | `Runtime/Components`, `Editor/Components` | [jonisavo/uicomponents](https://github.com/jonisavo/uicomponents) | MIT | nothing (Addressables part is optional) |
| Animation | `Runtime/Animation` (no editor code) | [instance-id/ElementAnimationToolkit](https://github.com/instance-id/ElementAnimationToolkit) | MIT | nothing |
| Extras | `Runtime/Extras` (no editor code) | [plyoung/UIElements](https://github.com/plyoung/UIElements) | Unlicense | nothing |
| Foundation | `Runtime/Foundation` (`Core`, `Views`, `Resources`) | new code, Future Digital Plus | see below | nothing (design system sheets are used when present) |
| Interaction | `Runtime/Interaction` (`Core`, `InputSystem`) | new code, Future Digital Plus | see below | nothing (the Input System part needs `com.unity.inputsystem`) |
| Language | `Runtime/Language` (`Language.UnityLocalization` optional), font tooling in `Runtime/DesignSystem/Typography` | new code, Future Digital Plus | see below | `Foundation.Core` (the pure text types); Unity Localization is optional |
| Hud | `Runtime/Hud` (`Core`, `Views`, `Resources`) | new code, Future Digital Plus | see below | nothing (design system tokens are used when present) |
| Controls | `Runtime/Controls` (`Core`, `Views`, `Resources`) | new code, Future Digital Plus | see below | Foundation (the `FocusTrap` class only); design system classes are used when present |
| Gameplay | `Runtime/Gameplay` (`Core`, `Views`, `Resources`) | new code, Future Digital Plus | see below | nothing (design system tokens are used when present) |
| Bootstrap | `Runtime/Bootstrap` | new code, Future Digital Plus | see below | DesignSystem, Animation, Foundation, Interaction, Language |
| UIKit (facade) | `Runtime/UIKit` | new code, Future Digital Plus | see below | DesignSystem and Animation |
| Checks and tooling (no runtime code) | `Tools~/Matrix`, `Tools~/CompileCheck`, `Tools~/SamplesSmoke`, `Tools~/DocsCheck`, `Tests/Editor/Lint`, `Tests/Runtime/Platform` | new code, Future Digital Plus | see below | a Unity editor (matrix, tests) or the dotnet SDK (compile check, lint runner) |

No module depends on any game code. Each module has its own assembly definitions, so a game can reference only
the ones it needs.

### DesignSystem
Design tokens, USS themes (dark and light built in), typography and font tooling (no font is bundled), 15 stylesheets
of `ds-*` components, 120 SVG icons, runtime behaviours (toggle knob, tabs, drag and drop, dropdown placement),
effects (`Fx`, GPU materials need Unity 6000.5 and compile out before it) and editor tooling (Theme Configurator).
Gives every screen one look that can be re-themed by swapping a theme asset, or by building a brand theme from two colours at runtime (`ThemeData.FromBrand`, no Editor bake), with six built-in themes
(Dark, Light, two neutral, two high-contrast) that pass a WCAG contrast audit, named themes (`ThemeManager`), a touch tier that only changes control sizes, and operating-system font fallbacks for CJK and shaping
scripts (`DsOsFonts`). See `Documentation~/DesignSystem/THEMING.md`.

### Components
The UIComponents framework: attribute-driven `UIComponent` base class (layout and stylesheet loading, queries,
dependency injection, event registration), a `TestBed<T>` testing helper, an optional Addressables asset source
that only compiles when the Addressables package is installed, and the prebuilt Roslyn source generators and
analyzers that make the attributes work. Reference `FutureDigitalPlus.UIKit.Components` by name from your asmdef (not auto-referenced).

### Animation
ElementAnimationToolkit: chainable animations for `VisualElement` (opacity, size, colours, display, sequences, hover,
pulse, easing) and three animated elements. Runtime code, works in player builds. Its editor window and font files
are not included.

### Extras
Small single-purpose controls from plyoung/UIElements: tooltip, popup panels, aspect-ratio padding, colour field
and picker, and the UI half of a blur/glass panel.

### Foundation
The UI plumbing every game needs, in plain C# with no singleton: `UiShell` (ordered layers, one per panel), a screen router with a saved stack, dialogs with a priority queue, toasts, a loading overlay, a safe-area provider that
knows render-texture and world-space panels, a panel scale policy for phones, tablets, desktop, console and web, profile classes on the root (orientation, size class, platform, input mode), focus trapping and a few views
(top bar, list row, banner, pause menu). The pure logic is in `Foundation.Core` (no `UnityEngine`), tested by dotnet. See `Documentation~/Foundation/README.md`, which also maps the card game's `ColorSolitaire.UI` names to the kit.

### Interaction
Which device the player uses and what the UI must do about it: classes on the root for mouse, touch, keyboard and gamepad (`fdp-input-nav` shows the focus ring only for keyboard and gamepad), `Activate`
(Submit works like a click), one back signal for Escape, the Android back button and gamepad B, and accessibility settings (reduced motion, text size steps, bold text, high contrast) from the player
and the OS. Legacy Input Manager, Input System or both; the pure logic is in `Interaction.Core`. Also adds keyboard and gamepad paths to the design system, the tooltip, the popup and the colour picker.
See `Documentation~/Interaction/README.md`.

### Language
Text that follows the player's language: `UiLocalization` (works with no localization package: labels show their fallback or `[KEY]` and nothing throws), `LocalizedLabel`/`LocalizedButton` and `BindText` that
update on a language change and unsubscribe on detach, an in-memory text source for code, CSV and JSON, `LocaleClassApplier` (`fdp-lang-<code>`, `fdp-rtl`), `Rtl.uss` (the design system mirrored for Arabic, Hebrew and
other right-to-left languages), and an optional adapter for Unity Localization. System-font fallbacks for scripts a game did not bundle a font for are in the DesignSystem module (`DsOsFonts`). See `Documentation~/Language/README.md`.

### Hud
Widgets for the in-game overlay of any game: `MeterBar` (health, mana, XP with a damage trail and warning levels), `RadialProgress`, `CooldownIcon`, rolling counters (`CountUpLabel`, `CurrencyLabel`),
`CountdownLabel` with an injectable clock, `FloatingTextLayer` (pooled damage numbers), `WorldAnchorLayer` (name plates and markers pinned to the 3D world through any `Camera`), `TimedFeed` and `HudZones`.
No `Update` loops (element scheduler only), no allocations in steady state, unscaled time, reduced motion respected. The pure logic is in `Hud.Core`. See `Documentation~/Hud/README.md`.

### Controls
Pickers and containers for menus that work with a pointer, touch, keyboard and gamepad alike: `TabBar` and `SegmentedControl` with badges, `PopupMenu` with submenus, `SelectField` (the gamepad path the stock dropdown lacks),
`BottomSheet` with snap points, `ListViewBinder` and `VirtualGrid` that keep their scroll position and have empty and loading states, and `RewardPopup` with a queue. The pure logic is in `Controls.Core`.
See `Documentation~/Controls/README.md`.

### Gameplay
Inventory building blocks: `InventoryModel` (stack limits, merge, split, swap, locked slots, a drop rule, moves between inventories, each refusal with a reason), `PickupMode`, `ItemSlot` and `SlotGrid`
(pointer drag with a ghost, keyboard and gamepad pick-up, several linked grids, events for tooltips and context menus). The pure logic is in `Gameplay.Core`. See `Documentation~/Gameplay/README.md`.

### Bootstrap
`UIShellHost`: put it next to a `UIDocument` and the kit runs: design system, Interaction and Language sheets, the Dark and Light themes, the input profile, accessibility settings, language classes, a `UiShell` and one back signal for Escape, gamepad B and the Android back button. Register your screens when `Ready` is raised. It is its own assembly because it joins modules that otherwise do not reference each other. See `Documentation~/Bootstrap/README.md`.

### UIKit (facade)
New, deliberately thin code: `UIKitHost` (a component next to a `UIDocument`) or `UIKitInstaller.Install(root)` installs
the design system base styles, a theme, the `fdp-input-touch` class and the safe-area padding; `UIKitMotion` wraps the two common
animations. It references DesignSystem and Animation only; it steps aside from the safe area and the touch class when a shell is present, and leaves panels that render into a `RenderTexture` alone. See `Documentation~/UIKit/README.md`.

## Documentation

Start with [Documentation~/GettingStarted.md](Documentation~/GettingStarted.md) (install, first screen, module
matrix), then [Recipes.md](Documentation~/Recipes.md) (copy-paste solutions: bootstrap, screen, dialog, toast, gamepad, HUD, inventory, localization, brand theme, responsive). Also: [Samples.md](Documentation~/Samples.md) (the eleven samples: what each shows and what was verified), [ChooseModules.md](Documentation~/ChooseModules.md) (which modules a game needs, by game type, platform
and optional package), [Migration.md](Documentation~/Migration.md) (card game foundation to the kit),
[Upgrading.md](Documentation~/Upgrading.md) (re-sync from upstream, release), [CONTRIBUTING.md](Documentation~/CONTRIBUTING.md) (change the kit, run its checks, bump the version), [RELEASING.md](Documentation~/RELEASING.md) (tags, install lines, the classic copy, how a game updates),
[UpstreamFeatures.md](Documentation~/UpstreamFeatures.md) (what is included, excluded and modified),
[CompatibilityMatrix.md](Documentation~/CompatibilityMatrix.md) (what was run: pipelines, input backends, optional packages, build targets, the samples, and what is not verified),
[Platforms.md](Documentation~/Platforms.md) (render pipelines, platforms, per platform notes, what ships in a build),
[UIKit](Documentation~/UIKit/README.md), [Bootstrap](Documentation~/Bootstrap/README.md), [Foundation](Documentation~/Foundation/README.md), [Interaction](Documentation~/Interaction/README.md), [Language](Documentation~/Language/README.md), [Hud](Documentation~/Hud/README.md), [Controls](Documentation~/Controls/README.md), [Gameplay](Documentation~/Gameplay/README.md), [DesignSystem theming](Documentation~/DesignSystem/THEMING.md), and one folder per module. Each module has a `Runtime/<Module>/SOURCE.md`
with upstream, commit, copied, excluded and modified files.

## Samples (Package Manager > Samples)

| Sample | Shows |
|---|---|
| UI Kit Quick Start | The facade: `UIKitHost`, theme switch, tooltip, fade-in, about 100 lines of C# |
| Design System Showcase | Buttons, inputs, tabs, meters, badges, icons, toasts, Light/Dark switch |
| Components Samples | The six upstream UIComponents samples (Counter, Query, Resources, EventInterfaces, UxmlTraits, Addressables) |
| Animation Toolkit Demo | Fade, width, hover, toast, pulse, `AnimatedFoldout` |
| Extras Stylesheets | Theme variables for the Extras controls, a bridge to the design system tokens, upstream reference files |
| Shell Quick Start | `UiShell`: router, dialogs, toasts, loading overlay, back handling, tab bar, localized texts (card, puzzle, casual) |
| RPG Inventory | Two linked slot grids with drag, keyboard and gamepad pick-up, context menu, tooltips (RPG, strategy) |
| Action HUD | Meters, cooldowns, floating numbers, a name plate pinned to a 3D object, a feed, a pause menu (action, FPS) |
| Gamepad Menu | Focus ring, focus trap, back handling, `Activate`, title-safe margin (console, handheld, TV, desktop) |
| Responsive | Panel scale, size-class classes, a safe area with a fake notch, touch targets (phone, tablet, foldable, desktop) |
| Theming and Language | Dark, light and run-time brand themes, high contrast, text size, reduced motion, a right-to-left language |

The last six rows are new for global use. They compile in Unity in every variant of the matrix and have a PlayMode smoke test (`Tools~/SamplesSmoke`, ten tests, run in eight variants); no click, key or gamepad press has driven them and none has run on a device. Details in [Documentation~/Samples.md](Documentation~/Samples.md).

## Installation

The kit is one source tree with two install forms. **UPM package** (sections 1 to 3 below) keeps the kit outside the game's `Assets` and references it
from `Packages/manifest.json`. **Classic Assets package** (section 4) is a plain folder, `Assets/FutureDigitalPlus/UIKit`, with no `manifest.json` entry; it is
generated from the UPM source by `Tools~/make_classic_kit.py`. The code, the `.meta` files and the GUIDs are the same in both, so assets, `Resources` paths,
USS and UXML references resolve identically and the same tests pass in both (numbers in [Documentation~/Verification.md](Documentation~/Verification.md)).

| | UPM package (1 to 3) | Classic Assets package (4) |
|---|---|---|
| Location | `Packages/com.futuredigitalplus.uikit` (path, git or embedded) | `Assets/FutureDigitalPlus/UIKit` |
| The game repository holds | one line in `manifest.json` (and the kit repository next to it) | a copy of the kit (about 2,000 files, 12 MB; `Samples~`, `Documentation~` and `Tools~`, about 2 MB, are ignored by Unity) |
| The kit's own tests | only with `testables` | always in the Test Runner (Assets are always testable) |
| Update | change the tag, pull, or edit the source in place | run `Tools~/make_classic_kit.py` again from the UPM source, commit the result |
| Edit the kit from the game | embedded or `file:` packages only | possible, but the folder is generated: edit the UPM source instead, or the next update asks for `--force` |
| Needs a sibling repository or a git host at build time | yes (path) or a reachable git URL | no, the game repository is self-contained |

Choose UPM when several games share the kit or you want one tag to roll out; choose classic when a game must build from its own repository alone (a CI
checkout without the kit repository, a store submission, a vendor that receives only the game). Both can be used in turn: never both in the same project
(the GUIDs would exist twice).

**UPM package.** Pick one.

**1. Local path (development, same machine).** `Unity-Ui-Kit` is a clone of the kit repository (its root is the package root). In your game's `Packages/manifest.json`:

```json
{
  "dependencies": {
    "com.futuredigitalplus.uikit": "file:../../path/to/Unity-Ui-Kit"
  }
}
```

A relative path is resolved from the game's `Packages` folder. An absolute `file:/…` path also works but is not
portable between machines; do not commit it.

**2. Git URL.** The repository root is the package root, so no `?path=` is needed. HTTPS or SSH, pinned to a tag (or `#main` to follow the branch):

```json
{
  "dependencies": {
    "com.futuredigitalplus.uikit": "https://github.com/futuredigitalplus/Unity-Ui-Kit.git#v0.2.2"
  }
}
```

The SSH form is `git@github.com:futuredigitalplus/Unity-Ui-Kit.git#v0.2.2`. Always pin a tag or commit so builds are reproducible. Tags, the classic copy and how a game updates: [Documentation~/RELEASING.md](Documentation~/RELEASING.md).

**3. Embedded.** Copy the `com.futuredigitalplus.uikit` folder into your game's `Packages/` folder. Unity treats it
as an editable embedded package. Use this when a game needs local patches, then port them back.

To run this package's own tests from a game or test project, add it to `testables` in `Packages/manifest.json`:

```json
"testables": [ "com.futuredigitalplus.uikit" ]
```

**4. Classic Assets package.** Generated copy of the UPM source below `Assets`. No `manifest.json` entry, no `testables`.

```sh
python3 <kit source>/Tools~/make_classic_kit.py --src <kit source> --dst <game>/Assets/FutureDigitalPlus/UIKit
```

`<kit source>` is the UPM folder (the one with `package.json`): the root of a clone of the kit repository.
The script (Python 3.9+, standard library only) copies the kit without `package.json`, keeps every `.meta` file byte for byte, keeps `Samples~`, `Documentation~` and `Tools~`
hidden from Unity (their names end with `~`), writes `VERSION` (kit version and the package metadata) and a manifest of file hashes, rewrites the one constant that must name the
install folder, and adds `Assets/FutureDigitalPlus.meta` and `Assets/FutureDigitalPlus/UIKit.meta` when they are missing. Run it again after every kit change: it is deterministic and idempotent
(a second run touches nothing), and it refuses to overwrite a copy with local changes unless you pass `--force`. `--check` writes nothing and exits 3 when the copy differs (for CI),
`--exclude Tests` (repeatable) leaves a top level folder out. Details: [Documentation~/GettingStarted.md](Documentation~/GettingStarted.md) section 1 and [Documentation~/Upgrading.md](Documentation~/Upgrading.md) "Updating a classic copy".
The game needs the two built-in modules the kit uses in its own `Packages/manifest.json` (`com.unity.modules.unitywebrequest`, `com.unity.modules.vectorgraphics`; both are in new projects).
Samples: copy one folder of `Samples~` into `Assets/Samples/` (or rename `Samples~` to `Samples`, which needs `--force` on the next update).

## Requirements

- Unity 6000.3 or newer. 6000.3.25f1 is the only editor version verified; 6000.0 to 6000.2 and 6000.5 are untested.
- No other packages are required. In the classic layout the two built-in modules `com.unity.modules.unitywebrequest` and `com.unity.modules.vectorgraphics` must be in the game's `manifest.json` (the UPM package brings them itself). Optional: `com.unity.addressables` enables the Addressables asset resolver in
  the Components module; `com.unity.localization` (1.4 or newer) enables the Unity Localization adapter of the Language module.
- Render pipelines: the kit has no pipeline-specific code. Built-in, URP 3D and 2D, and HDRP (Linear; HDRP supports no other colour space) are verified; the matrix is in
  [Documentation~/CompatibilityMatrix.md](Documentation~/CompatibilityMatrix.md), the platforms nobody
  could run (WebGL, tvOS, visionOS, consoles, Windows, Linux, devices, other Unity versions) and the per platform notes are in
  [Documentation~/Platforms.md](Documentation~/Platforms.md). Input: the kit uses UI Toolkit events only and never reads `UnityEngine.Input`, so legacy,
  Input System only and both work (Input System only was run).
- Features needing a newer Unity (for example the design system's GPU material surfaces, 6000.5+) are guarded
  with `#if UNITY_6000_5_OR_NEWER` and are simply absent on older editors (`versionDefines` is used for the optional
  Addressables part of Components and for `DS_WEBREQUEST`).

## Folder layout

```
Runtime/<Module>/        runtime code, one asmdef set per module, SOURCE.md, upstream licence file
Editor/<Module>/         editor-only code (only DesignSystem and Components have any; the other folders hold a .gitkeep)
Tests/Runtime|Editor/<Module>/
Samples~/                importable samples (hidden from Unity, no .meta files)
Documentation~/          guides (hidden from Unity)
Tools~/rename/           rename.py (collision-proofing rename of the upstream names), rename-map.md, README.md (hidden from Unity)
Tools~/PureTests/        dotnet test: the pure (no Unity) tests of Foundation.Core and the other Core assemblies, in seconds
Tools~/Matrix/           run-matrix.sh: the package's tests per pipeline / colour space / input / optional-package variant, one table row each
Tools~/CompileCheck/     Roslyn compile of every assembly, XML-doc check, name-hazard check, lint runner (no Unity needed)
Tools~/SamplesSmoke/     PlayMode smoke tests of the samples and a script that copies them into a scratch project
Tools~/DocsCheck/        link and path check of the guides; compiles the code blocks of Recipes.md and the module guides and runs the recipes in a scratch project
Tests/Editor/Lint/       rule tests on the package itself: USS features, --fdp-* variables, class prefixes, resource paths, .meta files, name hazards
Tests/Runtime/Platform/  pipeline smoke tests, Wait, culture, font cache and blur tests
Editor/Location/         KitLocation: where the kit is (UPM package or Assets folder); the only editor code that knows both locations
.gitignore, .gitattributes  repository files (the repository root is the package root); hidden from Unity (dot files), copied by the classic converter like everything else
Tools~/make_classic_kit.py  UPM source to classic Assets copy (section "Installation", 4); Tools~/classic/ holds the Unity-generated .meta files it needs
VERSION                  classic copy only: the kit version and package metadata (the UPM source has package.json)
THIRD_PARTY_NOTICES.md   upstream licences, commits, checksums
```

## Verification

Run in Unity 6000.3.25f1 (macOS arm64, Metal) on 2026-10-06 by `Tools~/Matrix/run-matrix.sh`, in clean scratch projects that reference a copy of the package with `testables`. Every number, the build targets, the samples and what is **not**
verified are in [Documentation~/CompatibilityMatrix.md](Documentation~/CompatibilityMatrix.md); the commands are in [Documentation~/Verification.md](Documentation~/Verification.md).

| Check | Result |
|---|---|
| Full recompile (clean `Library`), 13 variants | 0 errors, 0 warnings |
| Variants | Built-in (Gamma, Linear), Input System only / installed but inactive / both, uGUI, Localization, Addressables, all of them, none of them, URP 3D (Gamma, Linear), URP 2D, HDRP (Linear) |
| EditMode tests (all modules, pure logic, lint) | 918 of 918 in the base variant (919 in the scratch project, which has one more test); every variant all green (up to 923 with the optional packages' own tests) |
| PlayMode tests | 683 in the base variant: 675 passed, 8 skipped (seven allocation probes need IL2CPP, one Arabic width test finds nothing to compare), 0 failed in every variant |
| Pure logic without Unity (`dotnet test`), lint rules, Roslyn compile of every assembly and sample (also with the WebGL, Windows, Linux, iOS and Android symbols) | 544 of 544, 45 of 45, 0 errors and 0 documentation findings |
| Runtime assemblies compiled for iOS, Android and macOS standalone | success in every variant (18 assemblies); IL2CPP export for iOS and Android (stripping High) succeeded; Windows, Linux and WebGL only compiled with Roslyn (no module) |
| The eleven samples | import and compile in every variant; the ten sample smoke tests pass in eight variants (Built-in, Input System only, both, all optional packages, Addressables, URP 3D, URP 2D, HDRP) |
| Three host games that follow the guide (2D URP + Input System, 3D Built-in + Input Manager, HDRP + gamepad) | 9 of 9 tests |
| The guides (`Tools~/DocsCheck`) | links and snippets: 0 problems; the recipes run in Unity: 12 of 12 |
| The rename (`Tools~/rename/`) | `rename.py --verify-only` reports 0 problems on the whole package; a second `--apply` changes nothing; baked themes equal a rebake of the unrenamed package; see `Documentation~/Verification.md` |
| `.meta` files | none in `Samples~` / `Documentation~` / `Tools~`, none missing, no orphans, no duplicate GUIDs, every upstream `.meta` unchanged, Roslyn DLL labels intact |

Not verified: devices and real input devices (touch, notch, gamepad, hover), Windows / Linux / WebGL / tvOS / visionOS / console / XR players, an IL2CPP run, Unity versions other than 6000.3.25f1 (`package.json` depends on the built-in
module `com.unity.modules.vectorgraphics`, confirmed to exist in 6000.3.25f1 only; the `6000.5` guarded code never compiled here).

Classic Assets layout (0.2.2), same source: base, URP 2D and all-optional variants run in both layouts with identical numbers (EditMode 934/934 and 934/934 and 939/939 in the scratch project, PlayMode 678 + 8 skipped, 678 + 8 skipped, 681 + 8 skipped), 0 compiler errors and warnings, player scripts for iOS, Android and macOS compile; see `Documentation~/Verification.md` ("Release check for 0.2.2").

## Known issues and notes

- On Unity 6000.3 a class-scoped theme only wins over the base tokens from a descendant of the element holding the base
  sheet; use `UIKitHost`/`UIKitInstaller` or put the base sheet on the panel root. See `Documentation~/GettingStarted.md`.
- Components: the upstream generator emits obsolete `UxmlFactory` code for `[UxmlName]`/`[UxmlTrait]` components, which
  gives `CS0618` warnings in an assembly that uses them on Unity 6 (our own test assemblies silence it with `csc.rsp`).
- The tests' `Resources` assets (about 44 KB, under `FutureDigitalPlus/UIKit/Tests/Components/` and `FutureDigitalPlus/UIKit/Tests/`) end up in player
  builds of any game that installs the package, because Unity packs every `Resources` folder. Harmless; do not use those paths.
- Run the EditMode tests without `-nographics`: eight Components editor tests open an editor window.
- Platform fixes in upstream-derived code (each marked `FutureDigitalPlus:` and recorded in the module's `SOURCE.md`): `Wait.Until/While` run their condition on the main thread
  (upstream: a thread-pool thread, which never completed on WebGL), `UIParser` parses numbers with the invariant culture, `DsGoogleFonts` has a settable `CacheDirectory`
  and `IsSupported`. `BlurSource` (new) makes a blurred texture for `BlurPanel` on any render pipeline. Details: `Documentation~/Platforms.md`.
- Everything the kit puts into a global namespace is under `FutureDigitalPlus.UIKit` (C# namespaces and assembly names) or
  `FutureDigitalPlus/UIKit/` (Resources, shader names `Hidden/FutureDigitalPlus/UIKit/...`, font folders). Keep those
  prefixes free in a game. Type names such as `Tooltip` or `Easing` live inside those namespaces; a file that imports a
  game type of the same name next to ours has to alias one of them.
- What the kit does inside a host game (reuse notes):
  - Editor: no project settings, scripting defines or importers are changed. Two `[InitializeOnLoadMethod]` hooks exist
    (`ThemeBaker`: deletes a stranded `Assets/_FutureDigitalPlusUIKitTemp` scratch folder after a crash; `DsFxBlueprintFamily`: registers
    a Fx family in memory, 6000.5+ only). Windows write into `Assets/` only when you press a button (Google Fonts import,
    Theme Configurator, Generate Built-in Themes).
  - Runtime: the DesignSystem behaviours (toggle knob, tabs, drag and drop, dropdown placement) are attached by
    `UIKitInstaller` (one `EnsureAll` pass per panel) or by your own call to `DesignSystemBehaviourBase<UIDocument>.EnsureAll(root)`.
    Upstream instead adds a behaviour to every `UIDocument` and `PanelRenderer` at each scene load and re-scans each tree
    every 250 ms; that is still available behind the scripting define `FDP_UIKIT_AUTO_ATTACH` and is off by default.
  - Network: nothing is fetched unless you call `DsGoogleFonts` (runtime) or use the Google Fonts window (editor).
  - Assembly and namespace names carry the `FutureDigitalPlus.UIKit` prefix, so they cannot clash with a game's own assemblies
    or with the upstream packages themselves. The few names that stay generic are listed in `Tools~/rename/README.md`
    (the internal generated `UIComponentAssetRegistry`, the `_DsFxTime` shader global, three assembly-local defines).
  - A theme asset needs a baked stylesheet (duplicate `Light.asset` into `Assets/`, edit, Save; `ThemeBuildCheck` fails a player build with an unbaked or stale one). A theme made at runtime with
    `ThemeData.CreateRuntime` or `ThemeData.FromBrand` needs no bake: `ThemeRuntime.Apply` patches a copy of a baked template's stylesheet (`Documentation~/DesignSystem/THEMING.md`).
  - Remove the package from `Packages/manifest.json` (or delete `Assets/FutureDigitalPlus/UIKit` in the classic layout) and nothing is left behind except what you created yourself (themes,
    imported fonts, `Assets/Resources/FutureDigitalPlus/UIKit/Fonts`, the font cache in `persistentDataPath/FutureDigitalPlus/UIKit/Fonts`).
  - Low-end devices: the GPU material effects (Fx) exist only on Unity 6000.5+ and only on elements with a `ds-fx-*` marker;
    elsewhere elements render as plain stylesheet UI. `BlurPanel` shows a texture you supply and costs nothing without one.
  - The text-selection colour in input fields follows the theme (`--fdp-color-selection`).
- The package licence is undecided (below).

## Version policy

- Semantic Versioning. `0.x` means the API may change between minor versions. From `1.0.0`, breaking changes
  only in major versions.
- Upstream code is kept close to the original (namespaces, type names, file structure) so future upstream updates
  can be diffed. The exact upstream commit of each module is in `THIRD_PARTY_NOTICES.md` and in the module's
  `SOURCE.md`. Updating an upstream is a minor version bump and a CHANGELOG entry.
- Every local change to upstream files is marked in the source with a `FutureDigitalPlus:` comment and listed in
  that module's `SOURCE.md`.
- `.meta` files are committed, so GUIDs stay stable for consumers.

## Licence

**TODO (owner):** choose a licence for the code and documentation written by Future Digital Plus (the UIKit
facade, tests, samples and guides). Until then this package carries no licence of its own.
Bundled upstream components keep their own licences: see [THIRD_PARTY_NOTICES.md](THIRD_PARTY_NOTICES.md).
