# Verification record

What was checked, with the commands used. The last full section, **"Release check for 0.2.0"**, is the current state of the whole package (0.2.2 added a layout, see its section and the results by layout); the sections before it are the history it replaces (the 0.1.0 baseline of the first four modules, the rename, the modules added for global use, the adversarial API review) and are kept because their commands and findings still apply.
All of it ran on Unity 6000.3.25f1 (macOS, Apple silicon) on 2026-10-05 and 2026-10-06. Nothing was run on a device or in an interactive player: interactive behaviour
(dragging in `Slider2D`, hover in `Tooltip`, the blur panel's look, the safe area on a real notch, a real gamepad) is covered by logic
tests and injected events only. See the known limits in each module's `SOURCE.md` and the "Not verified" section.

## Method

The package was imported by path into a clean scratch project that contains only the package, the built-in UI Toolkit
modules, `com.unity.test-framework` 1.6.0 and a one-test smoke assembly. The project's `Library` and `Logs` folders were
deleted first, so every assembly compiled from scratch. Unity ran only on that scratch project, never on a game.

```
S=<scratch directory>
T=$S/uikit-testproject                      # clean test project, package added as "file:<path to this package>"
U=/Applications/Unity/Hub/Editor/6000.3.25f1/Unity.app/Contents/MacOS/Unity

rm -rf $T/Library $T/Logs                   # clean import

$U -batchmode -projectPath $T -runTests -testPlatform EditMode -testResults A-EditMode.xml -logFile A-EditMode.log
$U -batchmode -projectPath $T -runTests -testPlatform PlayMode -testResults A-PlayMode.xml -logFile A-PlayMode.log
$U -batchmode -nographics -projectPath $T -executeMethod PlayerCompileCheck.Run -logFile A-player.log
```

`PlayerCompileCheck` is a scratch-project script (not part of the package) that calls
`PlayerBuildInterface.CompilePlayerScripts` for iOS, Android and StandaloneOSX and exits non-zero if any target fails.

The Addressables variant added `"com.unity.addressables": "2.10.3"` to the scratch manifest and ran the same three
commands (logs `B-*`). The manifest and lock file were then restored and EditMode was run again (`C-EditMode`).

## Results

| Check | Result |
|---|---|
| Compile from a clean `Library`, no Addressables | 0 errors, 0 warnings from `error CS`, `warning CS`, `error UIC`, `warning UIC` in the EditMode and PlayMode logs |
| EditMode tests | 47 of 47 passed (includes the scratch smoke test) |
| PlayMode tests | 196 of 196 passed |
| Player script compile (iOS, Android, StandaloneOSX) | 6 runtime assemblies each, success |
| Addressables variant: compile | 0 errors, 0 warnings; the optional Addressables assembly compiles (11 player assemblies per target) |
| Addressables variant: EditMode | 48 of 48 passed (47 plus one test that ships inside the Addressables package) |
| Addressables variant: PlayMode | 196 of 196 passed |
| Back to the no-Addressables manifest | EditMode 47 of 47 passed |
| New `.meta` files or changed files in the package after the runs | none |

Test counts by assembly (EditMode, no Addressables): Components editor 25, Extras editor 13, DesignSystem editor 5,
UIKit editor 3, scratch smoke 1. PlayMode: Components 88 (upstream) + 6 (Roslyn integration) + 1 (smoke), DesignSystem
26, Animation 52, UIKit 17, Extras 6.

Log lines that are expected and are not compiler warnings:

- `AssetDatabase.FindAssets: Folder not found: 'Assets/UIKitTests.AmbiguousConventionTestAssets'` (16 times in the
  EditMode log): an upstream Components test deliberately looks for a folder that does not exist.
- `No Theme Style Sheet set to PanelSettings ... UI will not render properly` in the PlayMode log: the tests create
  headless panels without a runtime theme and set `LogAssert.ignoreFailingMessages`.

## Static scan of the package folder

Run by a small Python script (kept in the scratch directory, not shipped) over every file in the package:

| Check | Result |
|---|---|
| Relative Markdown links and `#anchors` (40 `.md` files, 72 relative links) | 0 broken |
| Duplicate GUIDs across the 501 `.meta` files | 0 |
| `.meta` without an asset, asset without a `.meta`, `.meta` without a `guid:` | 0 / 0 / 0 |
| `.meta` files inside `Samples~` or `Documentation~` | 0 |
| `package.json` parses; has `name`, `version`, `displayName`, `unity`, `description`; name and semver well formed; every `samples[].path` exists | pass |
| Stray files (`Library`, `Temp`, `Logs`, `obj`, `*.csproj`, `*.sln`, `.DS_Store`, `*.orig`, `*.swp`) | 0 |
| Absolute local user or temp paths (macOS home and `/private` temp prefixes) in `.cs`, `.asmdef`, `.json`, `.md`, `.uss`, `.uxml` | 0 |
| References to the Solitaire game (`ColorSolitaire.`, `FCSolitaire`) in `.cs` and `.asmdef` | 0 |
| `THIRD_PARTY_NOTICES.md`: all four repo URLs, commit hashes, copyright holders, MIT and Unlicense texts present | pass |
| The 12 binaries (4 Roslyn DLLs, 4 PDBs, 4 test DLLs) hashed with SHA-256: each hash appears in the notices | 12 of 12 |
| Upstream licence file in each module folder (`LICENSE.md` x3, `UNLICENSE.md`) and a `SOURCE.md` per module (5 including UIKit) | present |
| Files that the repo-level `.gitignore` would drop (`git ls-files --others --exclude-standard` against files on disk) | 1009 of 1009 listed (the package `.gitignore` re-includes `*~/` folders and `*.dll`) |

## Size and layout

1009 files on disk: 508 content files (188 C# files) and 501 `.meta` files. About 4.16 MB of content (4,155,938 bytes;
`du` reports 7.1 MB because of filesystem block size). Per-module file lists are in each module's `SOURCE.md`.

## After the rename (names made collision-proof)

`Tools~/rename/rename.py` was run on a copy first, the copy was verified in Unity, then the script, the hand edits
(`Documentation~/Upgrading.md`, "Re-syncing after the rename") and the generated `.meta` files were applied to the package; the package
then equalled the verified copy file for file (`diff -r`) and was verified again in the original scratch project from a deleted `Library`.

| Check | Result |
|---|---|
| `rename.py --verify-only` on the package | 0 problems (asmdef references resolve, no old name left, every `--fdp-*` used is declared, every baked theme token is defined) |
| Second `rename.py --apply` on the renamed package | 0 files moved, 0 rewritten (idempotent) |
| Compile from a clean `Library` | 0 errors, 0 warnings (`error CS`, `warning CS`, `error UIC`, `warning UIC`, `AD0001`) |
| EditMode tests | 48 of 48 passed (47 package, 1 scratch smoke); two new tests guard the rename |
| PlayMode tests | 197 of 197 passed |
| Addressables variant | EditMode 49 of 49, PlayMode 197 of 197, `...Components.Addressables` and the Addressables sample compile |
| Samples copied into the scratch project (with Addressables) | all compile; the three sample smoke tests pass (200 of 200 PlayMode with them) |
| Player script compile (iOS, Android, StandaloneOSX) | 6 assemblies each, success |
| GUIDs | every upstream asset keeps its GUID (the 26 renamed asmdefs and the moved Resources files carried their `.meta`); the only new GUIDs are 31 folder metas and the 8 Roslyn DLL/PDB metas; 0 duplicates, 0 orphans |
| Roslyn analyzers | each `-analyzer:` list (generated `.rsp`) holds the four DLLs for the Components assemblies and their dependents only; the generator produces code (a `[Query]` component compiles and the Roslyn integration tests pass); `UIC003` is reported for a `readonly` `[Query]` field; a non-partial component gives `CS0260` exactly as with upstream's DLLs |
| Baked themes | `ThemeBaker.RebakeAll` on the renamed copy gives the same asset as `RebakeAll` on the unrenamed package once names are mapped (only `m_ContentHash` differs); the shipped assets keep their original serialisation |
| Resources at run time | base sheets, tokens, icons, themes, Extras and Animation sheets load by their new paths; the old paths load nothing; `Shader.Find("Hidden/FutureDigitalPlus/UIKit/DsFx/Blueprint")` finds the shader |
| Menus (listed from the editor with `TypeCache`) | `Tools/FDP UI Kit/Design System/` Theme Configurator, Google Fonts, Generate Built-in Themes, Rebake All Themes; `Assets/FDP UI Kit/Design System/Add Stylesheet To Selected Layout Asset`; `Window/FDP UI Kit/Components/Validate Registry Asset Paths`; `[AddComponentMenu]` and `CreateAssetMenu` entries under `FDP UI Kit`. The Components sample menus (`Tools/FDP UI Kit/Components Examples/...`) compile but were not opened |
| Licences | the licence files, the licence texts in `THIRD_PARTY_NOTICES.md` and the upstream README copy are byte-identical to before |

Findings worth knowing: a rebake on Unity 6000.3.25f1 writes the baked sheet with `m_Specificity: 11` and a `resourcePaths` field where the shipped
Dark/Light assets (made on another Unity version) have `m_Specificity: {m_Score: 256}`; this is true of upstream too and was not changed here. In
PlayMode, `ds-btn` animates `background-color` (a transition), so a test that reads `resolvedStyle.backgroundColor` right after a theme swap must wait about
half a second.

## Modules and samples added for global use (run in Unity on 2026-10-06)

Foundation, Interaction, Language, Hud, Controls and Gameplay, the new theming code, the new `Tools~/Matrix` and the eleven samples were first run in Unity by the integration step. The whole matrix (every
variant, every number, the samples, the build targets, what the first run found) is in [CompatibilityMatrix.md](CompatibilityMatrix.md); this section records the checks on the package itself and the commands.

```
S=<scratch directory>
T=$S/uikit-testproject      # the scratch project that references the live package by path (Unity writes the .meta files of new files into the package here; they are kept)
U=/Applications/Unity/Hub/Editor/6000.3.25f1/Unity.app/Contents/MacOS/Unity

rm -rf $T/Library/Bee $T/Library/ScriptAssemblies          # every compiler warning is printed again
$U -batchmode -projectPath $T -runTests -testPlatform EditMode -testResults edit.xml -logFile edit.log
$U -batchmode -projectPath $T -runTests -testPlatform PlayMode -testResults play.xml -logFile play.log
$U -batchmode -nographics -projectPath $T -logFile gen.log -quit \
   -executeMethod FutureDigitalPlus.UIKit.DesignSystem.Editor.Theming.ThemePresets.GenerateBatch   # writes and bakes the six built-in themes

Tools~/Matrix/run-matrix.sh <variant> --base-project <clean project> --work <dir> --player-compile    # 13 variants
Tools~/Matrix/run-matrix.sh <variant> --samples --base-project <clean project> --work <dir>           # 8 variants

Tools~/CompileCheck/run-compile-check.sh --samples [--define UNITY_WEBGL]      # Roslyn, no Unity
dotnet test Tools~/PureTests --artifacts-path <temp>                           # pure logic of the Core assemblies
dotnet test Tools~/CompileCheck/LintRunner --artifacts-path <temp>             # lint rules
python3 -m unittest test_matrix                                                # in Tools~/Matrix
```

| Check | Result |
|---|---|
| Full recompile (`Library/Bee` deleted), 13 variants | 0 `error CS`, `warning CS`, `error UIC`, `warning UIC` in every EditMode and PlayMode log; no asmdef cycle, no missing reference |
| EditMode, scratch project with the live package | 896 of 896 passed (895 package, 1 scratch smoke test), from a deleted `Library/Bee` |
| PlayMode, scratch project with the live package | 591 tests: 583 passed, 8 skipped (the seven allocation probes and the Arabic width test), 0 failed |
| Matrix (`Documentation~/CompatibilityMatrix.md`) | 13 variants, all green: EditMode 895 to 900 of the same count passed, PlayMode 583 to 586 passed with the same 8 skipped, `paint` 4 of 4, compile errors and warnings 0 / 0, player script compile ok for iOS, Android and macOS; IL2CPP export for iOS and Android succeeded (stripping High) |
| Package compiles with none of the optional packages | `base` variant: no Input System, Localization, Addressables, uGUI or TextMeshPro, 0 errors, 0 warnings, all tests pass |
| Samples | all eleven import and compile in every variant; the ten sample smoke tests pass in 8 variants (Built-in, Input System only, both, all optional packages, Addressables, URP 3D, URP 2D, HDRP Linear) |
| `.meta` files | 0 duplicate GUIDs, 0 files without a `.meta`, 0 orphans, none in `Samples~`, `Documentation~` or `Tools~`; the metas of the new files were written by Unity and kept (never by hand) |
| Pure tests (`dotnet test Tools~/PureTests`) | 521 of 521 |
| Lint rules (`dotnet test Tools~/CompileCheck/LintRunner`) | 45 of 45 (includes three new rules: assemblies depend on Unity and the kit only, optional integrations sit behind a define constraint, pure layers have no engine reference) |
| Roslyn compile with `UNITY_WEBGL`, `UNITY_STANDALONE_WIN`, `UNITY_STANDALONE_LINUX` defined (runtime, editor and sample assemblies) | 0 errors each; XML documentation: 19 CS1591 findings, all in `Runtime/UIKit` (the facade), filled afterwards by the documentation review: 0 findings |
| `Tools~/Matrix` tests (`python3 -m unittest test_matrix`) | 8 of 8 |
| Documentation (`Tools~/DocsCheck/run-docs-check.sh <project> --run`) | links, anchors, quoted paths: 0 problems in 61 Markdown files; the ten recipes, GettingStarted step 2 and the guides' UXML run in Unity: 12 of 12, in the base project and with Input System only + Localization + Addressables + uGUI |

The integration run changed code, not only tests; every fix, with its cause, is listed in [CompatibilityMatrix.md](CompatibilityMatrix.md) ("What the first run in Unity found") and under "Fixed" in `CHANGELOG.md`; the changes to upstream-derived files are in each module's `SOURCE.md`.

## Adversarial API review (hostile usage; run in Unity 6000.3.25f1, base variant, macOS arm64)

Tests that use the public API the way a careless or hostile caller would: null and empty arguments, NaN and infinities, repeated install and dispose, services used after they were disposed, dialogs shown from dialog callbacks, rapid navigation with transitions, a panel destroyed or rotated mid-transition, a theme that changes or dies while worn, overlapping fades, elements outside a panel, two shells on one host, a throwing backend or subscriber, and the static state a play session without a domain reload leaves behind.
They live next to the module tests: `Tests/Runtime/Foundation/AdversarialPlayModeTests.cs`, `Tests/Runtime/UIKit/AdversarialUIKitTests.cs`, `Tests/Runtime/DesignSystem/AdversarialThemeTests.cs` and `AdversarialStaticsTests.cs`, `Tests/Runtime/Hud/AdversarialHudTests.cs`, `Tests/Runtime/Controls/AdversarialControlsTests.cs`,
`Tests/Runtime/Extras/AdversarialExtrasTests.cs`, `Tests/Runtime/Interaction/AdversarialInputFeedTests.cs`, `Tests/Runtime/Language/AdversarialLanguageTests.cs`, `Tests/Runtime/Components/AdversarialStaticsTests.cs`, and in the pure suites `HostileInputTests.cs` (Hud, Controls, Gameplay), `PanelScaleHostileTests.cs` and `DialogQueueShutdownTests.cs` (Foundation).
Every defect they found is fixed and listed under "Fixed" in `CHANGELOG.md`; the change to each upstream-derived file is in its module's `SOURCE.md`.

| Check | Result |
|---|---|
| EditMode | 919 of 919 |
| PlayMode | 661 passed, 8 skipped (the allocation probes that need IL2CPP), 0 failed |
| `dotnet test Tools~/PureTests` | 544 of 544 |
| `dotnet test Tools~/CompileCheck/LintRunner` | 45 of 45 |
| `Tools~/CompileCheck/run-compile-check.sh` | 0 errors, 0 documentation findings, 0 name hazards |

## Release check for 0.2.0 (clean `Library`, the whole package)

Run last, on the final files, after every review finding was fixed. The package under test is the live folder (so Unity wrote nothing new into it: 1966 files before and after); the matrix runs used copies.

```
S=<scratch directory>
T=$S/uikit-testproject     # the scratch project that references the live package by path, with "testables"
U=/Applications/Unity/Hub/Editor/6000.3.25f1/Unity.app/Contents/MacOS/Unity

rm -rf $T/Library $T/Logs $T/Temp                                   # clean import: every assembly compiles, every warning is printed
$U -batchmode -projectPath $T -runTests -testPlatform EditMode -testResults A-EditMode.xml -logFile A-EditMode.log
$U -batchmode -projectPath $T -runTests -testPlatform PlayMode -testResults A-PlayMode.xml -logFile A-PlayMode.log
$U -batchmode -nographics -projectPath $T -executeMethod PlayerCompileCheck.Run -logFile A-player.log
grep -E 'error CS|warning CS|error UIC|warning UIC|AD0001' A-EditMode.log A-PlayMode.log      # prints nothing

Tools~/Matrix/run-matrix.sh <variant> --base-project <clean project> --work <dir> --player-compile       # 13 variants, one at a time or three lanes
Tools~/Matrix/run-matrix.sh <variant> --samples --base-project <clean project> --work <dir>              # 8 variants
Tools~/Matrix/run-matrix.sh base --filter FutureDigitalPlus.UIKit.Bootstrap --player-compile --export ios --export android ...   # IL2CPP export
Tools~/DocsCheck/run-docs-check.sh <project> --run                                                       # base project and the all-optional project

dotnet test Tools~/PureTests --artifacts-path <temp>
dotnet test Tools~/CompileCheck/LintRunner --artifacts-path <temp>
Tools~/CompileCheck/run-compile-check.sh --samples [--define UNITY_WEBGL | UNITY_STANDALONE_WIN | UNITY_STANDALONE_LINUX | UNITY_IOS | UNITY_ANDROID]
python3 -m unittest test_matrix                                      # in Tools~/Matrix
python3 Tools~/rename/rename.py --root <package> --verify-only       # and --apply on a copy: 0 files rewritten
```

| Check | Result |
|---|---|
| Compile from a deleted `Library` (the scratch project, live package) | 0 `error CS`, `warning CS`, `error UIC`, `warning UIC`, `AD0001` in the EditMode and PlayMode logs |
| EditMode | 919 of 919 (918 package, 1 scratch smoke test) |
| PlayMode | 683 tests: 675 passed, 8 skipped (seven allocation probes that need IL2CPP, one Arabic width test), 0 failed |
| Player script compile (iOS, Android, macOS standalone) | success, 18 runtime assemblies each; Windows, Linux, WebGL: no module installed (see below) |
| Matrix, 13 variants (none of the optional packages, each one alone, all of them, Input System only / both / installed but inactive, Gamma and Linear, Built-in, URP 3D, URP 2D, HDRP) | all green: EditMode 918 of 918 (919 with Addressables, 923 with Localization or all optional packages), PlayMode 675 passed and 8 skipped (678 with the Input System), paint 4 of 4, compile errors and warnings 0 / 0, player compile ok for iOS, Android and macOS in every variant; see [CompatibilityMatrix.md](CompatibilityMatrix.md) |
| Optional packages: none (`base`) and all (`all-optional`) | both green (the kit compiles and runs with no Input System, Localization, Addressables, uGUI or TextMeshPro, and with all of them) |
| Samples (all eleven), smoke tests | 10 of 10 in 8 variants (Built-in, Input System only, both, all optional packages, Addressables, URP 3D, URP 2D, HDRP Linear), 0 compiler warnings |
| IL2CPP export, Managed Stripping High (`base`) | iOS and Android `Succeeded`, 0 errors, 0 warnings (no Xcode or Gradle build) |
| Three host games that follow GettingStarted (2D URP + Input System, 3D Built-in + Input Manager, HDRP + gamepad) | 4 of 4, 3 of 3, 2 of 2 against the final package |
| Documentation (`Tools~/DocsCheck`) | links, anchors, quoted paths: 0 problems in 62 Markdown files; the C# blocks compile and recipes 0 to 9, GettingStarted step 2 and the guides' UXML run: 12 of 12 in the base project and 12 of 12 in the all-optional project |
| Pure tests | 544 of 544 |
| Lint rules | 45 of 45 |
| Roslyn compile (`run-compile-check.sh --samples`), plain and with `UNITY_WEBGL`, `UNITY_STANDALONE_WIN`, `UNITY_STANDALONE_LINUX`, `UNITY_IOS`, `UNITY_ANDROID` | 0 errors, 0 documentation findings (every public API has an XML comment), 0 name hazards, each |
| `Tools~/Matrix` tests | `python3 -m unittest test_matrix` passes |
| Components generator fix (`using FutureDigitalPlus.UIKit;` in a component file) | `ComponentWithFacadeUsingTests` compiles with the rebuilt `Generation.dll`; the same test project with the previous DLL fails with CS0246 in `FacadeUsingScreen.Dependencies.g.cs` (mutation check, scratch copy) |
| `rename.py --verify-only` | 0 problems on the whole package; `--apply` on a copy rewrote 0 files and moved 0 (idempotent) |
| `.meta` files | 993 metas for 1966 files; 0 duplicate GUIDs, 0 files without a meta, 0 orphans, none in `Samples~`, `Documentation~` or `Tools~`; every meta of a new file was written by Unity and kept |
| `package.json` | parses, version 0.2.0, name and semver well formed, every `samples[].path` exists |
| `THIRD_PARTY_NOTICES.md` | all four repositories, commits, copyright holders and licence texts present; the 12 binaries (4 Roslyn DLLs, 4 PDBs, 4 test DLLs) hash to the values in the notices (the `Generation` DLL and PDB were rebuilt for 0.2.0, their new hashes are recorded) |
| Stray files | none (no `Library`, `Temp`, `Logs`, `obj`, `bin`, `.DS_Store`, `*.sln`; the three `*.csproj` files in `Tools~` are the tools' projects), no absolute local path, no reference to the card game in runtime code, samples or guides |
| Size | 1966 files on disk: 973 content files (541 C# files) and 993 `.meta` files; 6.6 MB of content (6,599,623 bytes; 13 MB by `du` because of filesystem blocks) |

Reproduce a number: delete `Library`, run the three Unity commands, read the totals from the XML (`testcasecount`, `passed`, `failed`, `skipped`).

## Release check for 0.2.1 (partial, first host game)

0.2.1 changes three things (see `CHANGELOG.md`): `fdp-regular-up` and `UiProfile.IsRegularOrLarger`, the right-to-left rules of `ListRow` and `TopBar` in `Foundation.uss`, and `DsOsFontOrder` in the pure run. Run on 2026-10-06 in the scratch project that references the live package:

| Check | Result |
|---|---|
| `dotnet test Tools~/PureTests` | 596 of 596 (544 + 9 `UiProfileTests` + 43 `DsOsFontOrderTests`) |
| PlayMode, filter `FutureDigitalPlus.UIKit.Foundation.Tests` | 94 of 94, including `RtlPlayModeTests` (3, new) and `ProfilePlayModeTests` (the three `fdp-regular-up` checks) |

Not rerun for 0.2.1: the full EditMode and PlayMode suites, `run-compile-check.sh`, the player compile check and the matrix. The 0.2.0 results above still describe every file that 0.2.1 did not touch (one USS rule block, four small C# edits, one csproj, three test files).

## Release check for 0.2.2 (classic Assets layout and the same source in both layouts)

0.2.2 adds the classic layout (`Tools~/make_classic_kit.py`) and makes every place that assumed a package location-independent (`KitLocation`, see `CHANGELOG.md`). Run on 2026-10-06 on Unity 6000.3.25f1 (macOS arm64), in
scratch projects only. "Scratch projects" have one extra smoke test of their own, so the Unity EditMode totals below are the kit's 933 plus 1.

How the classic projects were made: `python3 Tools~/make_classic_kit.py --src <kit> --dst <project>/Assets/FutureDigitalPlus/UIKit`, a manifest with only `com.unity.test-framework` and the built-in modules (no kit entry, no `testables`), `Library` deleted, then `unity test` (EditMode, PlayMode) and the player compile check; the variants with `Tools~/Matrix/run-matrix.sh <variant> --classic --player-compile`. The UPM rows are the same source run with `run-matrix.sh <variant> --player-compile` (a `file:` reference with `testables`).

| Check | UPM layout | Classic layout |
|---|---|---|
| Compile, clean `Library` | 0 errors, 0 warnings | 0 errors, 0 warnings |
| EditMode, base (scratch project) | 934 of 934 (928 before the 0.2.2 changes) | 934 of 934 |
| PlayMode, base | 678 passed, 8 skipped, 0 failed | 678 passed, 8 skipped, 0 failed |
| Variant URP 2D (Renderer2D), Gamma | 934 of 934; 678 of 686 (8 skipped); paint 4/4 | 934 of 934; 678 of 686 (8 skipped); paint 4/4 |
| Variant all optional packages (Input System only, uGUI, Localization, Addressables) | 939 of 939; 681 of 689 (8 skipped); paint 4/4 | 939 of 939; 681 of 689 (8 skipped); paint 4/4 |
| Player scripts compiled for iOS, Android, macOS | ok (18 assemblies) | ok (18 assemblies) |
| `dotnet test Tools~/PureTests` | 596 of 596 | 596 of 596 (run from the classic copy's `Tools~`) |
| `dotnet test Tools~/CompileCheck/LintRunner` | 45 of 45 | 45 of 45 |
| `Tools~/CompileCheck/run-compile-check.sh` | 0 errors, 0 documentation findings, 0 name hazards | same |
| `python3 -m unittest discover -s Tools~ -p test_make_classic_kit.py` | 12 of 12 | |

Findings: (1) Unity did not change a single copied `.meta` file when it imported the classic copy (all 1,983 files stayed byte-identical to the generator's output), so the next converter run never sees "local changes" caused by Unity. (2) Every `Runtime/` and `Editor/` source is free of both install paths (a test enforces it). (3) A UXML or USS URL with a path of the other layout resolves by GUID but logs a "was moved" message on import; `ThemePreview.uxml` therefore carries a GUID-only URL. (4) The Roslyn generator DLLs keep working in `Assets` with their `.meta` labels unchanged (the Components tests, which need the generated code, pass). (5) A classic copy runs the kit's tests in the host game's Test Runner without `testables`. In the Solitaire game: EditMode, game and kit together, 8,403 tests with 0 failures (the kit adds 937: 933 passed, 4 skipped); PlayMode, the kit alone in the game project 689 tests (682 passed, 7 skipped, 0 failed); the game alone 213 (160 passed, 53 skipped, 0 failed, equal to the same game without the kit in `Assets`). (6) One PlayMode run of both sets together fails two game tests and about 40 kit tests (twice, reproducibly; a game test that takes 148 s alone exceeds its 180 s timeout, UI-heavy game tests run 3 to 5 times slower), with "The LinkedList node does not belong to current LinkedList" in the kit tests: UI Toolkit state left behind by the game's tests. Run the two PlayMode sets as separate runs. Not isolated further.

Not verified in the classic layout: the other ten matrix variants (Gamma and Linear Built-in, Input System variants, uGUI, Localization alone, Addressables alone, URP 3D, HDRP), the samples (`--samples`), IL2CPP export, and a host game other than the Solitaire game. They differ from the verified ones only in packages and pipeline, which the layout does not touch.

## Not verified

The list for the whole package, after the integration run, is in [CompatibilityMatrix.md](CompatibilityMatrix.md) ("What is NOT verified"). In short:

- No run on a device or with a real input device (touch, notch, rotation, a gamepad, hover): events are injected into the panel.
- Windows, Linux and WebGL players, tvOS, visionOS, consoles and XR: no module installed here (Windows, Linux and WebGL were compiled with Roslyn and the target's symbols only).
- No IL2CPP run (the export builds the C++ project only): the seven allocation probes skip themselves on the Mono editor.
- Unity versions other than 6000.3.25f1; features that need 6000.5 or later are guarded by version defines and were never exercised.
- Arabic shaping through a system font (the width test skips itself here).
- Unity's own bridge from a real gamepad to UI Toolkit navigation (the host game with a gamepad drives a virtual gamepad), and a real notch (the facade and the shell are tested with a faked safe area).
- `UxmlFactory`/`UxmlTraits` types are deprecated in Unity 6 and are kept as upstream wrote them (warnings suppressed
  locally and listed in each `SOURCE.md`).

## Repeat it

1. Delete `Library/Bee` and `Library/ScriptAssemblies` (or the whole `Library`) of a scratch project that references this package.
2. Run the commands in "Release check for 0.2.0" (the whole package; Method is the 0.1.0 baseline); for every variant use `Tools~/Matrix/run-matrix.sh`.
3. Grep the logs: `grep -E 'error CS|warning CS|error UIC|warning UIC' A-EditMode.log A-PlayMode.log` should print nothing.
4. Check the test XML totals (`testcasecount`, `passed`).
