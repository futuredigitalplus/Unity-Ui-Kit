# Components: source record

- Upstream: jonisavo/uicomponents (UIComponents, version `1.0.0-beta.13`, upstream package id `io.savolainen.uicomponents`)
- URL: https://github.com/jonisavo/uicomponents
- Commit: `dc64355910f52fd42924b9af44dca9f20340dfd9`
- Licence: MIT, (c) 2022 Joni Savolainen (file kept in this folder: `LICENSE.md`; full text also in `../../THIRD_PARTY_NOTICES.md`)
- Status: integrated and verified on Unity 6000.3.25f1: compiles with no warnings from this package, the upstream tests pass (94 PlayMode and 25 EditMode tests in the Components assemblies, plus the new smoke test), and `FutureDigitalPlus.UIKit.Components.Addressables` compiles when `com.unity.addressables` 2.10.3 is added to the project (checked, then removed).

Type names and file names are the upstream ones. Namespaces (`UIComponents[.X]` is `FutureDigitalPlus.UIKit.Components[.X]`) and assembly
names (`UIComponents[.X]` is `FutureDigitalPlus.UIKit.Components[.X]`) were renamed to avoid collisions; see "Rename record" and "Rebuilt Roslyn DLLs". All upstream `.meta` files were
copied with their files and travelled with the renames, so their GUIDs are unchanged; only the eight Roslyn DLL and PDB metas are new. Upstream copyright headers: the
upstream sources carry none (the licence file is the notice); nothing was removed.

## Copied

Upstream paths are relative to the repository root. Everything below was byte-identical to upstream when copied; the rename script then changed namespaces, assembly names and paths as
described in "Rename record" (each changed C# file starts with a `// FutureDigitalPlus: renamed by ...` line), and the DLLs were rebuilt.

| Upstream | Package | Notes |
|---|---|---|
| `Assets/UIComponents/Core/` (assembly `UIComponents`) | `Runtime/Components/Core/` | whole folder with metas |
| `Assets/UIComponents/Testing/` (assembly `UIComponents.Testing`) | `Runtime/Components/Testing/` | `TestBed<T>` |
| `Assets/UIComponents/Addressables/` (assembly `UIComponents.Addressables`) | `Runtime/Components/Addressables/` | optional, see below |
| `Assets/UIComponents/Roslyn/` (4 DLLs, 4 PDBs, metas) | `Runtime/Components/Core/Roslyn/` | layout change, see below |
| `Assets/UIComponents/Editor/` (assembly `UIComponents.Editor`) | `Editor/Components/` | `AssetDatabaseAssetSource`, `ConventionValidator` |
| `Assets/UIComponents.Tests/` root `*.cs`, `Resources/`, `Utilities/`, `NSubstitute/`, `UIComponents.Tests.asmdef` | `Tests/Runtime/Components/` | |
| `Assets/UIComponents.Tests/Roslyn/` (assembly `UIComponents.Tests.Roslyn.IntegrationTests`) | `Tests/Runtime/Components/Roslyn/` | |
| `Assets/UIComponents.Tests/Editor/` (assembly `UIComponents.Tests.Editor`) | `Tests/Editor/Components/` | |
| `Assets/UIComponents.Tests/Addressables/Assets/Component.uxml`, `Component.uss` | `Tests/Editor/Components/ExplicitTestAssets/` | only the two asset files, used by the editor tests |
| `Assets/Samples/{Counter,Resources,Addressables,Query,EventInterfaces,UxmlTraits}/` | `Samples~/Components/<same name>/` | no `.meta` files (hidden folder) |
| `README.md`, `CHANGELOG.md` | `Documentation~/Components/UIComponents-upstream-README.md`, `UIComponents-upstream-CHANGELOG.md` | verbatim; the upstream wiki is a separate repository and is not in the clone |
| `LICENSE.md` | `Runtime/Components/LICENSE.md` | placed by the scaffold step |
| `Assets/UIComponents/package.json` | not copied; facts recorded here | upstream version `1.0.0-beta.13`, Unity `2021.3`, license `MIT`; its `samples` list is reproduced in the package's `package.json` by the integration step (paths `Samples~/Components/<name>`) |

Test counts: 18 test source files at `Tests/Runtime/Components/` plus the Roslyn integration tests and the editor
tests (29 `.cs` files in total under the two Components test folders, including helpers and sample test components).

### Layout of the analyzers

Upstream keeps the DLLs in `Assets/UIComponents/Roslyn/`, a sibling of the folder that holds `UIComponents.asmdef`,
so Unity applies them to every assembly of the project. Here they are in `Runtime/Components/Core/Roslyn/`,
below the asmdef folder, so Unity applies them to `FutureDigitalPlus.UIKit.Components` and to every assembly that references it, and to
no other assembly (experiment on Unity 6000.3.25f1; re-checked after the rename: the generated `.rsp` files of exactly the Components
assemblies and their dependents list the four `-analyzer:` DLLs). The four DLLs must stay together in
one folder, with their `.meta` labels (`RoslynAnalyzer`, `SourceGenerator`) intact. SHA-256 of the DLLs and PDBs
are in `../../THIRD_PARTY_NOTICES.md`.

### Rebuilt Roslyn DLLs (the namespace rename needs it)

Every generator and analyzer finds its types by the literal metadata name `UIComponents.<X>` (about 55 string literals in 21 source files).
With the namespace renamed and upstream's DLLs, the generators silently produce nothing (measured: 0 generated files in every consumer
assembly, no diagnostic, a clean compile, and `[Query]` fields would stay null at run time). The four DLLs are therefore rebuilt from the upstream
source at the pinned commit with exactly three kinds of change: string literals that start with `"UIComponents` and are not `UIComponents.Roslyn`
now start with `"FutureDigitalPlus.UIKit.Components`, and the four assemblies are named `FutureDigitalPlus.UIKit.Components.Roslyn.{Common,Generation,Analyzers,Analyzers.CodeFixes}`
(so a project that also has upstream's package never gets two plugins with one name), and, since 0.2.0, one fix in `UIComponents.Roslyn.Generation/Generators/AugmentGenerationContext.cs`: `GetTypeName` returns a framework type (`FutureDigitalPlus.UIKit.Components.*`) in full and never shortens it by the usings of the component's file. Before, a file with `using FutureDigitalPlus.UIKit;` (the facade) turned `FutureDigitalPlus.UIKit.Components.IAssetResolver` into `Components.IAssetResolver`, which does not resolve outside that namespace (CS0246 in `<Class>.Dependencies.g.cs`); upstream's own check only skipped a using that starts with the framework namespace. The patch is marked `FutureDigitalPlus:` in the rebuilt source and applied by `rename.py --generator-src`; only `Generation.dll` and its PDB changed (the other three DLLs are the 2026-10-05 build). Internal C# namespaces `UIComponents.Roslyn.*`, the
generated global class `UIComponentAssetRegistry` and the `UIC_*` generated method names are unchanged. Built with `rename.py --generator-src` (recipe, Roslyn
version and hashes in `Tools~/rename/README.md` and `../../THIRD_PARTY_NOTICES.md`). The `.meta` of each DLL and PDB is derived from upstream's (same labels and importer
settings) with a new, deterministic GUID; Unity kept the labels on import.

### Addressables (optional)

`FutureDigitalPlus.UIKit.Components.Addressables` keeps its upstream asmdef (apart from the rename): `defineConstraints: UICOMPONENTS_ADDRESSABLES_INSTALLED`
with a `versionDefines` entry that sets the define when `com.unity.addressables` 1.18.19 or newer is installed, and
references to `Unity.Addressables` and `Unity.ResourceManager` by name. Without the Addressables package the
assembly is not compiled. `FutureDigitalPlus.UIKit.Components.Editor` does not reference it (it reaches Addressables by reflection), so
the editor module compiles either way.

## Excluded

| Upstream path | Reason |
|---|---|
| `Assets/UIComponents.Benchmarks/` | needs `com.unity.test-framework.performance` (not in this package) and Addressables; benchmarks only. `InternalsVisibleTo("UIComponents.Benchmarks")` in `Core/AssemblyInfo.cs` is left as is (harmless) |
| `Assets/UIComponents.Tests/Addressables/` (`AddressableAssetResolverTests.cs`, its asmdef) | the tests need an Addressables group configured in the project (upstream keeps one in `Assets/AddressableAssetsData`, with the test assets addressable by path). Not portable into a package. Only the two plain asset files were kept (see above) |
| `Assets/AddressableAssetsData/`, `Assets/Scenes/`, `ProjectSettings/`, `Packages/` | upstream project settings, not part of the framework |
| `UIComponents.Roslyn/` (C# source of the analyzers and generators), `UIElementsSchema/`, `Scripts/` | not needed at runtime. The four DLLs are rebuilt from that source (see "Rebuilt Roslyn DLLs"; same licence) |
| `AGENTS.md`, `CLAUDE.md`, `.github/`, `.release-it.yml`, `.gitignore`, `logo.png` | upstream repository tooling. `AGENTS.md`/`CLAUDE.md` were read as data only |
| `Assets/*.meta` of the upstream project folders (`UIComponents.meta`, `Samples.meta`, ...) | folder metas of the upstream project; the folders of this package have their own metas |

## Bundled test-only third-party libraries

Upstream bundles these under `Assets/UIComponents.Tests/NSubstitute/`; they are kept (with their upstream metas) in
`Tests/Runtime/Components/NSubstitute/`. Their `.meta` files make them active only when `UNITY_INCLUDE_TESTS` is
defined and the importer is "explicitly referenced", so they are never part of a game build and never visible to
game code. Licences and SHA-256 are in `../../THIRD_PARTY_NOTICES.md`.

| Library | Licence | Licence file in the package |
|---|---|---|
| NSubstitute 4.3.0 | BSD-3-Clause | `LICENSE.txt` (added by us: upstream ships no licence file for this DLL; verify the text before a public release) |
| Castle.Core 4.4.1 | Apache-2.0 | upstream `ASL - Apache Software Foundation License.txt` and `LICENSE` |
| System.Runtime.CompilerServices.Unsafe 6.0.0 | MIT (.NET Foundation) | upstream `LICENSE.TXT`, `THIRD-PARTY-NOTICES.TXT` |
| System.Threading.Tasks.Extensions 4.5.4 | MIT (.NET Foundation) | upstream `LICENSE.TXT`, `THIRD-PARTY-NOTICES.TXT` |

## Local modifications

JSON files (asmdef) cannot hold comments: their changes are listed here only. Changes in C# files carry a comment
starting with `FutureDigitalPlus:`.

| File | Change | Reason |
|---|---|---|
| `Editor/Components/FutureDigitalPlus.UIKit.Components.Editor.asmdef` | reference `GUID:d593635333b4cae48bac5b5f0b596e90` replaced by the name `FutureDigitalPlus.UIKit.Components` | package rule: assembly references by name. (The GUID would also still resolve, because the upstream `.meta` was kept.) |
| `Tests/Runtime/Components/FutureDigitalPlus.UIKit.Components.Tests.asmdef` | removed the reference `UnityEditor.TestRunner` | the runtime tests do not use editor APIs; the asmdef has no platform restriction, so an editor-only reference would leak Editor code into player test builds |
| `Tests/Runtime/Components/Roslyn/FutureDigitalPlus.UIKit.Components.Tests.Roslyn.IntegrationTests.asmdef` | three `GUID:` references replaced by the names `FutureDigitalPlus.UIKit.Components` and `UnityEngine.TestRunner`; reference to `UnityEditor.TestRunner` (the third GUID) dropped | names instead of GUIDs; no editor APIs used |
| `Tests/Editor/Components/FutureDigitalPlus.UIKit.Components.Tests.Editor.asmdef` | removed the reference `FutureDigitalPlus.UIKit.Components.Addressables` | the editor tests that used Addressables types were removed (below); the reference would also point at an assembly that may not exist |
| `Tests/Runtime/Components/Roslyn/Resources/*.uxml` (4 files, plus their metas) | moved into `Resources/FutureDigitalPlus/UIKit/Tests/Components/` | upstream had them at the root of a `Resources` folder under generic names; the subfolder avoids collisions with a game's own `Resources` (the other test assets use the same prefix) |
| `Tests/Runtime/Components/Roslyn/UxmlNameTests.cs`, `UxmlTraitTests.cs` (4 `Resources.Load` calls) | load path prefixed with `FutureDigitalPlus/UIKit/Tests/Components/` | follows the move above |
| `Tests/Editor/Components/AssetDatabaseAssetResolverTests.cs` | all `Assets/UIComponents.Tests/...` and `Assets/Samples/...` paths replaced by constants in a new `TestAssetPaths` class (at the top of the file); test assets now resolve to `<kit>/Tests/Editor/Components/...` (`Packages/com.futuredigitalplus.uikit/...` in the UPM source, rewritten to `Assets/FutureDigitalPlus/UIKit/...` in the classic copy by `Tools~/make_classic_kit.py`); the temporary "ambiguous" assets are created in `Assets/UIKitTests.AmbiguousConventionTestAssets` (packages can be read-only); the sample asset used by one test and the fallback test is replaced by `ExplicitTestAssets/Component` and the Resources test asset `FutureDigitalPlus/UIKit/Tests/Components/LayoutAttributeTests` | the upstream project laid its test assets out in `Assets/`; they live in the package here. The kit folder appears once, in `TestAssetPaths.KitAssetRoot` (a constant, because attribute arguments must be; the test `Kit_Asset_Root_Constant_Matches_The_Install` checks it) |
| same file | removed `AddressableValidatedComponent`, `NonAddressableProjectAssetComponent`, the tests `ConventionValidator_Resolves_Addressable_Keys` and `ConventionValidator_Does_Not_Treat_NonAddressable_Project_Assets_As_Valid`, and the `using UIComponents.Addressables` line and two assertions that mentioned them | they need the Addressables package and a configured Addressables group |
| `Tests/Editor/Components/ExplicitTestAssets/` | new folder with copies of `Component.uxml`, `Component.uss` (+ metas) taken from upstream `UIComponents.Tests/Addressables/Assets/` | assets for the editor tests; no longer in a folder named after the Addressables tests |
| `Tests/Runtime/Components/csc.rsp`, `Tests/Runtime/Components/Roslyn/csc.rsp` (new, content `-nowarn:0618`) | added | The upstream generator emits the obsolete `UxmlFactory`/`UxmlTraits` for test components that use `[UxmlName]`/`[UxmlTrait]`; on Unity 6 that is warning CS0618 in generated code, which cannot be edited. The response files apply to these two test assemblies only. |
| `Samples~/Components/EventInterfaces/csc.rsp`, `Samples~/Components/UxmlTraits/csc.rsp` (new, content `-nowarn:0618`) | added | Same reason as above: these two samples use `UxmlFactory`/`UxmlTraits` (directly or in generated code), which is warning CS0618 on Unity 6 when the sample is imported. Found in the clean-room review. |
| `Core/DependencyInjection/DiContext.cs` | New private method `ResetStatics` with `[RuntimeInitializeOnLoadMethod(SubsystemRegistration)]`: `Current = Static; Static.Clear();`. | With the domain reload switched off (Enter Play Mode options) the singletons of the previous play session and a context a game had switched to stayed alive. Found by the adversarial API review. |

New source files in the runtime assembly (no upstream equivalent, no upstream header): `Core/EventInterfaces/IOnNavigationSubmit.cs`, `IOnNavigationCancel.cs`, `IOnFocusIn.cs`, `IOnFocusOut.cs`, `IOnPointerDown.cs`, `IOnPointerUp.cs`, `IOnPointerMove.cs`, `IOnKeyDown.cs`: eight event interfaces in the pattern of upstream's `IOnClick` (`[RegistersEventCallback(typeof(XEvent))]`, XML docs on every member). Written from that pattern, not copied from another project. The bundled source generator registers them with no change, because it finds an interface by the `[RegistersEventCallback]` attribute, not by name (checked: the compile check without Unity runs the bundled generator over a test component implementing all eight, and the generated constructor registers all eight callbacks). No upstream file was edited for them and the Roslyn DLLs are unchanged (SHA-256 values in `../../THIRD_PARTY_NOTICES.md` still match the files, re-computed in the licence review of 2026-10-06).

New files (no upstream equivalent, written for this package; `Tests/Runtime/Components/FutureDigitalPlusSmoke/` holds a new smoke test with its own asmdef `FutureDigitalPlus.UIKit.Components.Tests.Smoke`, a UXML under `Resources/FutureDigitalPlus/UIKit/Tests/` and a `[Layout]`/`[Query]` component, which the upstream `UIComponents.Tests` assembly does not include): `Runtime/Components/SOURCE.md` (this file),
`Tests/Runtime/Components/NSubstitute/nsubstitute.4.3.0/LICENSE.txt`, `Samples~/Components/Addressables/README.md`,
`Documentation~/Components/README.md`.

Not modified, but worth knowing:

- Sample `Samples~/Components/Addressables/AddressablesExampleComponent.cs` hard-codes
  `[AssetRoot("Assets/Samples/Addressables/Data/")]` as upstream does. After a Package Manager import the sample
  lives elsewhere, so the sample README tells the user to change it. The code is left byte-identical.
- The Resources sample puts files under `Resources/Components/` of the importing project. This only happens when a
  user imports that sample on purpose.
- Roslyn-generated code is produced by the rebuilt DLLs (same generators) and cannot be edited here. It does produce `CS0618` warnings on
  Unity 6 (the obsolete `UxmlFactory`/`UxmlTraits`) in any assembly that uses `[UxmlName]`/`[UxmlTrait]`. Our test
  assemblies silence them with `csc.rsp`; a game that uses those attributes can add the same file next to its asmdef.
- `FutureDigitalPlus.UIKit.Components.Tests`'s `InternalsVisibleTo` (in `Core/AssemblyInfo.cs`) gives the upstream tests access to internals such as
  `TaskExtensions.AsEnumerator`; other assemblies (our smoke test) poll the `Task` themselves.
- `FutureDigitalPlus.UIKit.Components.Tests.Roslyn.IntegrationTests` ships UXML test assets under a `Resources` folder that name types of
  the test assembly (`FutureDigitalPlus.UIKit.Components.Tests.Roslyn.*`). In a project that has not made this package `testables`, those
  types do not exist and Unity may log an import warning for the four UXML files (not observed in the scratch project, which
  does list the package in `testables`; not checked without it).
- The upstream editor tests cannot run with `-nographics`: eight of them open an editor window.

<!-- rename-record:begin -->
## Rename record

Generated by `Tools~/rename/rename.py` (collision-proofing). Module: Components. The upstream names survive only in this record.

| Assembly (old) | Assembly (new) |
|---|---|
| `FutureDigitalPlus.UIKit.Tests.Components` | `FutureDigitalPlus.UIKit.Components.Tests.Smoke` |
| `UIComponents` | `FutureDigitalPlus.UIKit.Components` |
| `UIComponents.Addressables` | `FutureDigitalPlus.UIKit.Components.Addressables` |
| `UIComponents.Editor` | `FutureDigitalPlus.UIKit.Components.Editor` |
| `UIComponents.Samples.Addressables` | `FutureDigitalPlus.UIKit.Components.Samples.Addressables` |
| `UIComponents.Samples.Addressables.Editor` | `FutureDigitalPlus.UIKit.Components.Samples.Addressables.Editor` |
| `UIComponents.Samples.Counter` | `FutureDigitalPlus.UIKit.Components.Samples.Counter` |
| `UIComponents.Samples.Counter.Editor` | `FutureDigitalPlus.UIKit.Components.Samples.Counter.Editor` |
| `UIComponents.Samples.EventInterfaces` | `FutureDigitalPlus.UIKit.Components.Samples.EventInterfaces` |
| `UIComponents.Samples.EventInterfaces.Editor` | `FutureDigitalPlus.UIKit.Components.Samples.EventInterfaces.Editor` |
| `UIComponents.Samples.Query` | `FutureDigitalPlus.UIKit.Components.Samples.Query` |
| `UIComponents.Samples.Query.Editor` | `FutureDigitalPlus.UIKit.Components.Samples.Query.Editor` |
| `UIComponents.Samples.Resources` | `FutureDigitalPlus.UIKit.Components.Samples.Resources` |
| `UIComponents.Samples.Resources.Editor` | `FutureDigitalPlus.UIKit.Components.Samples.Resources.Editor` |
| `UIComponents.Samples.UxmlTraits` | `FutureDigitalPlus.UIKit.Components.Samples.UxmlTraits` |
| `UIComponents.Samples.UxmlTraits.Editor` | `FutureDigitalPlus.UIKit.Components.Samples.UxmlTraits.Editor` |
| `UIComponents.Testing` | `FutureDigitalPlus.UIKit.Components.Testing` |
| `UIComponents.Tests` | `FutureDigitalPlus.UIKit.Components.Tests` |
| `UIComponents.Tests.Editor` | `FutureDigitalPlus.UIKit.Components.Tests.Editor` |
| `UIComponents.Tests.Roslyn.IntegrationTests` | `FutureDigitalPlus.UIKit.Components.Tests.Roslyn.IntegrationTests` |

| Namespace (old) | Namespace (new) |
|---|---|
| `UIComponents` | `FutureDigitalPlus.UIKit.Components` |

| Resources path (old) | Resources path (new) |
|---|---|
| `UIComponentTests/` | `FutureDigitalPlus/UIKit/Tests/Components/` |
| `FutureDigitalPlusUIKitTests/` | `FutureDigitalPlus/UIKit/Tests/` |

Menus moved under `FDP UI Kit` (`Tools/`, `Assets/`, `Window/`, `AddComponentMenu`, and `Design System >` sentences in code and stylesheets).

The four Roslyn DLLs were rebuilt from the upstream source with the namespace literals patched (see `Tools~/rename/README.md` and `THIRD_PARTY_NOTICES.md`).
<!-- rename-record:end -->
