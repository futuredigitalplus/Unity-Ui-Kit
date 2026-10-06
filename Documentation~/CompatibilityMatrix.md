# Compatibility matrix

What was run, on what, and what was **not**. Everything below was run on **2026-10-06** with **Unity 6000.3.25f1 on macOS arm64 (Metal)**, in scratch copies of a clean project
(never a game), by `Tools~/Matrix/run-matrix.sh`. The kit has no camera, render pass or pipeline-specific code of its own (UI Toolkit draws the panels), so one code base serves every pipeline;
the rows below are the proof for the combinations that were run. Notes per platform and the things that ship in a build are in [Platforms.md](Platforms.md); the first-version checks are in [Verification.md](Verification.md).

Legend: **run** = executed in Unity; **compile** = compiled by Unity for that target, not run; **approximation** = compiled by Roslyn with the target's symbols, not by Unity; **not verified** = nobody tried.

## How a row is made

`run-matrix.sh <variant> --base-project <clean project> --work <dir> --player-compile` does, for one variant:

1. copies a clean base project (only `Packages/manifest.json`, `ProjectSettings` and an empty `Assets`) and a copy of this package, rewrites the manifest (the package by path, `testables`, the variant's optional packages),
   `Active Input Handling` and the colour space, and creates the render pipeline asset for URP and HDRP variants with an editor script;
2. deletes `Library/Bee` and `Library/ScriptAssemblies`, so every compiler warning is printed again;
3. runs the whole EditMode suite and the whole PlayMode suite with the package in `testables` (EditMode with a graphics device, PlayMode with panels that really render);
4. compiles the runtime assemblies for every build target whose module is installed (`PlayerBuildInterface.CompilePlayerScripts`);
5. counts `error CS`, `warning CS`, `error UIC` and `warning UIC` lines in both editor logs and prints one row.

The `--samples` form copies the eleven samples and `Tools~/SamplesSmoke` into the project and runs only the sample smoke tests (the Components samples have no convention layouts, which the package's own EditMode tests would report).

## Results by variant

| Variant | Pipeline | Colour | Input handling | EditMode | PlayMode | Paint | Compile errors / warnings | Player script compile |
|---|---|---|---|---|---|---|---|---|
| base | Built-in | Gamma | Input Manager | 918/918 | 675/683 (8 skipped) | 4/4 | 0 / 0 | iOS, Android, macOS: ok |
| base-linear | Built-in | Linear | Input Manager | 918/918 | 675/683 (8 skipped) | 4/4 | 0 / 0 | iOS, Android, macOS: ok |
| legacy | Built-in | Gamma | Input Manager (Input System installed, inactive) | 918/918 | 675/683 (8 skipped) | 4/4 | 0 / 0 | iOS, Android, macOS: ok |
| input-system | Built-in | Gamma | Input System only | 918/918 | 678/686 (8 skipped) | 4/4 | 0 / 0 | iOS, Android, macOS: ok |
| both | Built-in | Gamma | both | 918/918 | 678/686 (8 skipped) | 4/4 | 0 / 0 | iOS, Android, macOS: ok |
| ugui | Built-in | Gamma | Input Manager | 918/918 | 675/683 (8 skipped) | 4/4 | 0 / 0 | iOS, Android, macOS: ok |
| localization | Built-in | Gamma | Input Manager | 923/923 | 675/683 (8 skipped) | 4/4 | 0 / 0 | iOS, Android, macOS: ok |
| addressables | Built-in | Gamma | Input Manager | 919/919 | 675/683 (8 skipped) | 4/4 | 0 / 0 | iOS, Android, macOS: ok |
| all-optional | Built-in | Gamma | Input System only | 923/923 | 678/686 (8 skipped) | 4/4 | 0 / 0 | iOS, Android, macOS: ok |
| urp3d | URP, UniversalRenderer | Gamma | Input Manager | 918/918 | 675/683 (8 skipped) | 4/4 | 0 / 0 | iOS, Android, macOS: ok |
| urp3d-linear | URP, UniversalRenderer | Linear | Input Manager | 918/918 | 675/683 (8 skipped) | 4/4 | 0 / 0 | iOS, Android, macOS: ok |
| urp2d | URP, Renderer2D | Gamma | Input Manager | 918/918 | 675/683 (8 skipped) | 4/4 | 0 / 0 | iOS, Android, macOS: ok |
| hdrp-linear | HDRP | Linear | Input Manager | 918/918 | 675/683 (8 skipped) | 4/4 | 0 / 0 | iOS, Android, macOS: ok |

Reading the table: **EditMode** includes the pure logic tests that Unity also runs (`*.Core.Tests`) and the lint tests; **PlayMode** includes `RenderPipelineSmokeTests`, which paints a themed panel into a `RenderTexture` with and without a camera and compares pixels with the theme
(the `paint` column, four cases per variant). Skipped PlayMode tests are the seven allocation probes and one Arabic measurement (see "What is NOT verified"). Compile errors and warnings are **0 / 0** in every variant: no `error CS`, `warning CS`, `error UIC`, `warning UIC` from this package, and no asmdef cycle, missing reference or duplicate GUID.

| Variant | What it proves |
|---|---|
| `base` | the package compiles and works with **none** of the optional packages: no Input System, no Localization, no Addressables, no uGUI or TextMeshPro, Built-in pipeline, Active Input Handling = Input Manager |
| `base-linear` | the same in Linear colour space |
| `legacy` | the Input System package is installed but inactive (Input Manager only): its adapter assembly compiles, the three Input System tests do not run |
| `input-system` | Active Input Handling = Input System Package only. The Input Manager is disabled, so any `UnityEngine.Input` call in a code path the tests reach would throw: none did. The adapter and its three tests run |
| `both` | Active Input Handling = Both |
| `ugui` | uGUI (which brings TextMeshPro) installed: nothing in the kit references it |
| `localization` | Unity Localization installed: the adapter assembly compiles and its five tests run |
| `addressables` | Addressables installed: `FutureDigitalPlus.UIKit.Components.Addressables` compiles (and a test that ships inside the Addressables package runs) |
| `all-optional` | Input System (only), uGUI, Localization and Addressables together |
| `urp3d`, `urp3d-linear`, `urp2d` | URP 17.3 with the Universal renderer (Gamma and Linear) and with the 2D renderer |
| `hdrp-linear` | HDRP 17.3. HDRP supports Linear only: in Gamma it logs an error for every camera that renders, which failed the tests that render with a camera, so that variant was dropped |

Optional packages and their versions as run: Input System 1.20.0, uGUI 2.0.0, Localization 1.5.13, Addressables 2.10.3, URP and HDRP 17.3.0.

### Classic Assets layout (0.2.2)

The same variants with the kit installed as `Assets/FutureDigitalPlus/UIKit` (`run-matrix.sh <variant> --classic`), next to the same source run as a UPM package. Totals are the scratch project's (the kit's 933 EditMode tests plus its one smoke test).

| Variant | Layout | EditMode | PlayMode | Paint | Compile errors / warnings | Player script compile |
|---|---|---|---|---|---|---|
| base | UPM | 934/934 | 678/686 (8 skipped) | 4/4 | 0 / 0 | iOS, Android, macOS: ok |
| base | classic | 934/934 | 678/686 (8 skipped) | 4/4 | 0 / 0 | iOS, Android, macOS: ok |
| urp2d | UPM | 934/934 | 678/686 (8 skipped) | 4/4 | 0 / 0 | iOS, Android, macOS: ok |
| urp2d | classic | 934/934 | 678/686 (8 skipped) | 4/4 | 0 / 0 | iOS, Android, macOS: ok |
| all-optional | UPM | 939/939 | 681/689 (8 skipped) | 4/4 | 0 / 0 | iOS, Android, macOS: ok |
| all-optional | classic | 939/939 | 681/689 (8 skipped) | 4/4 | 0 / 0 | iOS, Android, macOS: ok |

The other variants were not run in the classic layout (see `Verification.md`, "Release check for 0.2.2"). The numbers of the table above are those of 0.2.0 and 0.2.1 (918 EditMode, 675 PlayMode in the base variant); 0.2.1 and 0.2.2 added tests.

## Samples

| Variant | Samples smoke tests (PlayMode) | Compile errors / warnings |
|---|---|---|
| base | 10/10 | 0 / 0 |
| input-system | 10/10 | 0 / 0 |
| both | 10/10 | 0 / 0 |
| all-optional | 10/10 | 0 / 0 |
| addressables | 10/10 | 0 / 0 |
| urp3d | 10/10 | 0 / 0 |
| urp2d | 10/10 | 0 / 0 |
| hdrp-linear | 10/10 | 0 / 0 |

The Extras sample is style sheets only: it imports with about 16 warnings that its README lists (upstream sheets that point at images of the upstream demo project). The Addressables sample needs an Addressables group and a player content build, so it is compiled, not run
(its README has the steps). Not covered by any test: input (click, key, gamepad), the look of a sample, devices.

## Host games

Three throwaway host games followed `GettingStarted.md` step by step in Unity 6000.3.25f1 (each uses the package by relative `file:` path, a themed main menu, a HUD, a settings screen and a dialog, and has PlayMode tests); all of them were run again for 0.2.0 against the final package:

| Host | Setup | Result |
|---|---|---|
| 2D URP, mobile | Input System only, Gamma, portrait, asmdef, a faked notch | 4 of 4 |
| 3D Built-in, desktop | legacy Input Manager, no asmdef (`Assembly-CSharp`), mouse and keyboard, resizable window | 3 of 3 |
| HDRP, gamepad only | Input System and legacy (Both), Linear, a 5 percent title-safe margin, a virtual gamepad | 2 of 2 |

What they do not cover: Unity's own bridge from a real gamepad to UI Toolkit navigation (the tests drive a virtual gamepad), a real notch, a device.

## Build targets

`PlayerBuildInterface.CompilePlayerScripts` ran in every variant above; the "Player script compile" column shows the result for the targets whose module is installed (the editor here has the iOS and Android modules only; macOS standalone is the host).

| Target | State |
|---|---|
| iOS, Android, macOS standalone | **compile** in every variant above: `PlayerBuildInterface.CompilePlayerScripts` succeeds for the 18 runtime assemblies of the package |
| iOS and Android IL2CPP export (Managed Stripping **High**, no Xcode or Gradle build) | **compile** (base variant, `run-matrix.sh base --export ios --export android`): both `Succeeded`, 0 errors, 0 warnings; the scene is one `UIDocument` whose UXML names `BlurPanel`, `Tooltip`, `AnimatedFoldout` and `MaskedInputField`, so those kit elements are reachable only through UXML, as in a game. Nothing was run |
| Windows standalone (x64), Linux standalone | **approximation**: the platform module is not installed, so Unity cannot compile for them; Roslyn compiled every runtime, editor and sample assembly with `UNITY_STANDALONE` and `UNITY_STANDALONE_WIN` defined, and again with `UNITY_STANDALONE_LINUX`: 0 errors each time. The package has no platform symbol in its runtime code except the two places listed under WebGL |
| WebGL | **approximation**: module not installed; Roslyn compile with `UNITY_WEBGL` defined: 0 errors. `UNITY_WEBGL` appears in two places: `DsFxManager` (compiled in this check) and the Unity Localization adapter (a block that is left out; this check skips the adapter assembly, which needs the Localization assemblies, so that block was not compiled) |
| tvOS, visionOS, Switch, PlayStation, Xbox, other consoles | **not verified**: no module; the kit adds no console specific code and does not touch `UnityEngine.Input`, but nobody built it |
| XR (headsets) | **not verified**: a `RenderTexture` panel on a quad paints on every pipeline above; world-space UI Toolkit panels exist only in Unity 6000.5, which was not run |

Commands for the approximation (no Unity process needed):

```
Tools~/CompileCheck/run-compile-check.sh --samples --define UNITY_WEBGL
Tools~/CompileCheck/run-compile-check.sh --samples --define UNITY_STANDALONE --define UNITY_STANDALONE_WIN
Tools~/CompileCheck/run-compile-check.sh --samples --define UNITY_STANDALONE --define UNITY_STANDALONE_LINUX
```

## Test counts (base variant, Built-in)

| EditMode assembly | Tests | PlayMode assembly | Tests |
|---|---|---|---|
| Components.Tests.Editor | 25 | Animation.Tests | 52 |
| Controls.Core.Tests | 96 | Bootstrap.Tests | 7 |
| DesignSystem.Tests.Editor | 75 | Components.Tests | 89 |
| Extras.Tests.Editor | 13 | Components.Tests.Roslyn.IntegrationTests | 6 |
| Foundation.Core.Tests | 231 | Components.Tests.Smoke | 1 |
| Foundation.Tests.Editor | 114 | Controls.Tests | 75 |
| Gameplay.Core.Tests | 42 | DesignSystem.Tests | 56 |
| Hud.Core.Tests | 110 | Extras.Tests | 10 |
| Interaction.Core.Tests | 65 | Foundation.Tests | 91 |
| Interaction.Tests.Editor | 4 | Gameplay.Tests | 35 |
| Language.Tests.Editor | 94 | Hud.Tests | 63 |
| Lint.Tests.Editor | 46 | Interaction.Tests | 75 |
| Tests.Editor | 3 | Language.Tests | 40 |
|  |  | PlatformSmoke.Tests | 46 |
|  |  | Tests (UIKit facade and cross-module) | 37 |
| **total** | **918** | | **683** |

## What the first run in Unity found

The new modules had been written and checked without Unity (Roslyn compile and dotnet tests). Their first run found these, all fixed:

- **EditMode, 9 failures.** Only `Dark` and `Light` existed as theme assets, and those were baked before the contrast fixes. `ThemePresets.GenerateBatch` wrote and baked all six built-in themes; the assets and their metas are in the package now.
- **PlayMode, 69 failures at first.** The causes, by kind:
  - *Real defects in the modules:* the Foundation loading overlay's invisible card and spinner took pointer input in the middle of the screen when no loading was going on (`pickingMode` on the backdrop does not reach its children); `PopupMenu` never added its rows to the menu panel;
    the `WorldAnchorLayer` put anchors in the wrong place on a panel that renders into a `RenderTexture` (`RuntimePanelUtils.CameraTransformWorldToPanel` assumes the panel covers the screen); the focus ring on items without a border (`ds-nav-item`...) pushed the next item aside
    by 4 px; the banner's buttons were 36 high in the touch tier (the kit promises 44).
  - *Real defects in upstream-derived Extras code:* a `Button`'s own `Clickable` stops the propagation of a `PointerDown` at the target, so the tooltip's long press (and the menu binding's touch long press) never saw a press on a button; UI Toolkit reports the compatibility
    mouse events for a finger, so a tap showed the tooltip at once and hid it when the finger lifted; `PopupPanel.Show()` on a popup that was hidden a moment ago dropped focus altogether (the popup could not take it until its inherited visibility was resolved).
  - *Test setups that were not like a game:* the PlayMode panels of Foundation, Hud, Controls and Gameplay had no theme style sheet, so the document root had no height and text no font (a game assigns Unity's default runtime theme; the tests do now); a `SlotGridTests` panel was reused after it was destroyed;
    pointer events were sent to elements of zero size or at the origin, where the panel believes the mouse is; a bottom-sheet "slow drag" waited frames that take a millisecond when nothing is drawn; a Hud test camera rendered into a render texture without a depth buffer, which URP's render graph rejects.
- **The allocation probes** were the first thing to run on the editor's Mono runtime, which cannot count allocations per thread. A try with the Mono heap counter as a substitute found that the slot grid allocates about 3 KB when a drag enters another slot (UI Toolkit allocates when a USS class changes) and that the counter is useless as proof (it stays flat while the collector frees garbage, and it moves with other threads), so the test now asserts what the code promises (moves inside a slot) and still skips on Mono.
- **HDRP in Gamma** is not a configuration HDRP supports (it logs an error for every camera that renders); the variant was dropped, HDRP is run in Linear.
- **Tooling.** `run-matrix.sh` used the Unity CLI, whose output had no editor log; it starts Unity directly now. The player compile covers Windows, Linux and WebGL where the module exists.


## Documentation

`Tools~/DocsCheck` (see its README) checks the guides themselves: a link, anchor and path check over every Markdown file (0 problems), and a compile of every C# block of `Recipes.md` and of the module guides, plus a PlayMode run of recipes 0 to 9 (recipe 10, the ready-made host, is compiled there and run by the Bootstrap tests), of GettingStarted step 2 and of the UXML written in the Hud, Controls, Language and Extras guides (12 tests).
Run on 2026-10-06 again for 0.2.0 in the `base` project and in a project with Input System (only), Localization, Addressables and uGUI: 12 of 12 passed in both, 0 compiler warnings.

## What is NOT verified

- **Devices.** Nothing ran on a phone, a tablet, a foldable, a console, a TV or a headset: touch, hover stickiness, real notches and cutouts, rotation, OS font scale, the soft keyboard, screen readers, a real gamepad. The safe area, the input mode classes and the long press are tested with fake sources and injected events.
- **Interactive input.** Pointer, navigation and key events are injected into the panel; no real mouse, touch screen or gamepad drove a test. Input System only is run, but with no device.
- **Other Unity versions.** `package.json` asks for 6000.3 and only 6000.3.25f1 was run. Code behind `UNITY_6000_5_OR_NEWER` (world-space panels, the design system's GPU material surfaces) never compiled here.
- **Other platforms' players.** See Build targets. No player was built and run on any platform; IL2CPP was not run (the allocation probes below are the main loss).
- **Allocation probes.** Seven PlayMode tests skip themselves on the Mono editor (the table's "skipped" column): five assert that a steady state allocates nothing (meter bar, radial progress, floating text, world anchors, slot drag within a slot) and two check that the probe sees an allocation. They need a runtime that counts allocations per thread (IL2CPP, .NET). The Mono editor cannot; its heap counter is no substitute (it stays flat while the collector frees what a step allocated). macOS IL2CPP is not installed here, so **nobody has run these seven**; the code they guard is written to be allocation free (pooled elements, work in the element scheduler), which is a design intent, not a measurement.
- **Arabic shaping.** `OsFontsTests.Real_ShapedArabicIsNarrowerThanUnjoinedArabic` expects joined letters to be narrower than isolated forms (175 against 213 px in the card game). On this editor and system (Geeza Pro) both text generators measure the same width, so the test skips itself; whether the advanced generator shapes Arabic here was not established by a measurement.
- **Screen overlay panels** cannot be pixel checked in batch mode (screen capture returns nothing); the paint tests render into a `RenderTexture`.
- **Live blur** `BlurSource.FromCamera` runs on Built-in, URP and HDRP in the test (a red scene stays red; HDRP's exposure lowers the value to 0.57).
- **The facade** (`Runtime/UIKit`): the safe area is faked in every test (the editor's `Screen.safeArea` has no notch), so "a RenderTexture panel gets no notch padding" and "with a shell under the root the host steps aside" are tested with a faked device safe area, never with a real cutout.

## Repeat it

```
M=<package>/Tools~/Matrix
$M/run-matrix.sh base --base-project <clean Unity 6000.3 project> --work <scratch dir> --player-compile
$M/run-matrix.sh urp2d --base-project ... --work ... --player-compile
$M/run-matrix.sh base --samples --base-project ... --work ...
$M/run-matrix.sh --table --work <scratch dir>
```

A variant takes 2 to 10 minutes (HDRP the longest); up to three can run at the same time. See `Tools~/Matrix/README.md`.
