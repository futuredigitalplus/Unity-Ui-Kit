# Components module (FutureDigitalPlus.UIKit.Components)

The Components module is the [UIComponents](https://github.com/jonisavo/uicomponents) framework by Joni Savolainen
(MIT), bundled unchanged except for the small modifications listed in `Runtime/Components/SOURCE.md`. It is a
front-end framework for UI Toolkit: a `UIComponent` base class that loads its layout (UXML) and stylesheets
(USS), queries elements into fields, registers event callbacks from interfaces, and gets dependencies injected.
C# source generators (Roslyn) write the boilerplate, and Roslyn analyzers check your component classes.

This page explains how the module sits inside the Future Digital Plus UI Kit. The framework's own documentation
is kept next to this file:

- `UIComponents-upstream-README.md`: the upstream README (overview, example, testing, validation, requirements).
  Its installation sections do not apply to this package: install the UI Kit instead.
- `UIComponents-upstream-CHANGELOG.md`: the upstream changelog (version `1.0.0-beta.13` is bundled).
- The upstream wiki (UxmlFactory and UxmlTraits generation, layouts and stylesheets, asset loading, dependency
  injection, event interfaces, logging) is a separate repository and is not bundled:
  https://github.com/jonisavo/uicomponents/wiki

## Assemblies

Namespaces and assembly names are not upstream's (`UIComponents` is `FutureDigitalPlus.UIKit.Components`): code written against
upstream UIComponents needs its `using` lines and asmdef references changed, nothing else. Type names are unchanged.

| Assembly | Folder | Platforms | Depends on | Notes |
|---|---|---|---|---|
| `FutureDigitalPlus.UIKit.Components` | `Runtime/Components/Core` | all | nothing | Namespaces `FutureDigitalPlus.UIKit.Components`, `FutureDigitalPlus.UIKit.Components.DependencyInjection`, `FutureDigitalPlus.UIKit.Components.InterfaceModifiers`. Not auto-referenced: reference it from your asmdef. |
| `FutureDigitalPlus.UIKit.Components.Testing` | `Runtime/Components/Testing` | all | `FutureDigitalPlus.UIKit.Components` | `TestBed<T>` helper for testing components. |
| `FutureDigitalPlus.UIKit.Components.Addressables` | `Runtime/Components/Addressables` | all | `FutureDigitalPlus.UIKit.Components`, `Unity.Addressables`, `Unity.ResourceManager` | Compiles only when `com.unity.addressables` 1.18.19 or newer is installed (define `UICOMPONENTS_ADDRESSABLES_INSTALLED`). Without the package it compiles to nothing. |
| `FutureDigitalPlus.UIKit.Components.Editor` | `Editor/Components` | Editor | `FutureDigitalPlus.UIKit.Components` | `AssetDatabaseAssetSource` and `ConventionValidator`. It reaches Addressables by reflection, so it has no hard Addressables dependency. |

The module does not depend on the other UI Kit modules, and none of them depend on it.

## Using it in a game

Add `FutureDigitalPlus.UIKit.Components` to the `references` of your asmdef (by name). With the default asmdef-less project (`Assembly-CSharp`)
nothing is auto-referenced: create an asmdef for your UI code.

```csharp
using FutureDigitalPlus.UIKit.Components;
using UnityEngine.UIElements;

[Layout("UI/Counter")]             // Resources/UI/Counter.uxml (a bare [Layout] resolves by convention, see upstream wiki)
[Stylesheet("UI/Counter.style")]   // Resources/UI/Counter.style.uss
public partial class CounterComponent : UIComponent
{
    [Query("count-label")] public Label CountLabel;
    public override void OnInit() => CountLabel.text = "0";
}

// in a MonoBehaviour that owns a UIDocument:
//     root.Add(new CounterComponent());   // loads its assets when it is attached to a panel
```

With the default `ResourcesAssetSource`, an explicit path is the surest form: `[Layout("UI/Counter")]` loads
`Resources/UI/Counter.uxml` and `[Stylesheet("UI/Counter.style")]` loads `Resources/UI/Counter.style.uss` (no extension
on the layout path; see the Resources sample). Classes that derive from `UIComponent` must be `partial` (analyzer error UIC001), because
the generators add the other half of the class.

The five attributes you need first:

| Attribute | Does |
|---|---|
| `[Layout("path")]` / `[Layout]` | Loads the UXML from the path (a bare `[Layout]` uses the class-name convention) |
| `[Stylesheet("path")]` / `[SharedStylesheet("path")]` | Loads a USS for this component, or one shared by several components |
| `[Query("name")]` / `[Query(Class = "x")]` | Fills a field (`VisualElement` type or an array of them) after the assets have loaded |
| `[Provide]` + `[Dependency(typeof(IFoo), provide: typeof(Foo))]` | Dependency injection into a component field; `TestBed<T>.WithSingleton<IFoo>(...)` swaps it in tests |
| `[UxmlName("Tag")]` / `[UxmlTrait]` | Generates `UxmlFactory`/`UxmlTraits` so the component can be used in UXML. Marks obsolete code on Unity 6: expect `CS0618` warnings in your assembly (see "Known issues" in the package README) |

The framework's own guides (wiki) are not bundled; `UIComponents-upstream-README.md` has a longer example, the test helper
and the validation command, and `Samples~/Components` has runnable ones.

### Resources paths

`ResourcesAssetSource` is the default asset source. It uses `Resources.Load`, so layouts and stylesheets of your
components must sit in a `Resources` folder of your game. This package ships no `Resources` assets of its own
for the framework. The tests do have some (about 44 KB in total, under `FutureDigitalPlus/UIKit/Tests/Components/` and
`FutureDigitalPlus/UIKit/Tests/`); because Unity packs every `Resources` folder it finds, they are also in a player build
of a game that has this package installed. Do not use those two folder names. If your game has other `Resources` content,
keep component assets under a folder of your own (for example `Resources/UI/...`) and use `[AssetRoot("UI/")]`
to avoid collisions.

## Roslyn source generators and analyzers

The four prebuilt DLLs live in `Runtime/Components/Core/Roslyn/`. Their `.meta` files carry the label
`RoslynAnalyzer`, which is what makes Unity hand them to the C# compiler. Never delete or regenerate those `.meta`
files: without the label code generation stops silently. The DLLs are rebuilt from upstream's source with the namespace literals
patched (upstream's DLLs look types up by the literal name `UIComponents.X` and would silently generate nothing in the renamed
namespace); see `Tools~/rename/README.md`.

The DLLs sit below the folder of `FutureDigitalPlus.UIKit.Components.asmdef`, so Unity applies them to the `FutureDigitalPlus.UIKit.Components` assembly and
to every assembly that references it (directly or transitively), and to no other assembly. A game's other
assemblies are not touched and pay no compile-time cost. (Upstream puts the DLLs one folder higher, which applies
them to every assembly of the project; this was verified on Unity 6000.3.25f1 and the folder move is recorded in
`Runtime/Components/SOURCE.md`.)

Diagnostics: UIC001 and UIC002 (class must be `partial`; no internal methods in components), UIC003 (no `readonly`
members with `[Provide]` or `[Query]`), UIC101 to UIC105 (empty or conflicting asset paths and names).

The DLLs run inside the Unity compiler. They are bundled exactly as upstream built them and their SHA-256 values
are recorded in `THIRD_PARTY_NOTICES.md`.

## Using the facade's namespace in a component file

The generator writes type names relative to the `using` directives of your file. Until 0.2.0 a file with `using FutureDigitalPlus.UIKit;` (the facade's namespace) that declared a `UIComponent` class outside that namespace got generated code with `Components.IAssetResolver`, which did not resolve (CS0246 in a `<Class>.Dependencies.g.cs`). The bundled generator no longer shortens the framework's own types; `Tests/Runtime/UIKit/ComponentWithFacadeUsingTests.cs` keeps it that way. The change to the generator is recorded in `Runtime/Components/SOURCE.md`.

## Addressables

Install `com.unity.addressables` in your game and `FutureDigitalPlus.UIKit.Components.Addressables` starts compiling; then use
`[Dependency(typeof(IAssetSource), provide: typeof(AddressableAssetSource))]` on a component or on an assembly.
The Addressables sample (`Samples~/Components/Addressables`) shows the setup and its README lists the manual steps.

## Validation

`FutureDigitalPlus.UIKit.Components.Editor.ConventionValidator` checks that the asset paths of all components resolve. From the editor
use the menu `Window > Future Digital Plus UI Kit > Components > Validate Registry Asset Paths`, or in CI:

```shell
Unity -batchmode -projectPath . -quit -executeMethod FutureDigitalPlus.UIKit.Components.Editor.ConventionValidator.ValidateForBatchMode
```

In batch mode it skips assemblies whose names start with `FutureDigitalPlus.UIKit.Components.Tests` or `FutureDigitalPlus.UIKit.Components.Benchmarks`.

## Samples

Six samples are available from the Package Manager (package "Future Digital Plus UI Kit", tab Samples; classic Assets install: copy `Samples~/Components` to `Assets/Samples/Components`):
Counter (dependency injection), Resources, Addressables, Query, Event interfaces and UXML code generation.
They open as editor windows under the menu `Tools > Future Digital Plus UI Kit > Components Examples`. The sample asmdefs reference `FutureDigitalPlus.UIKit.Components`
by name. The Resources sample puts files in `Resources/Components/` and `Resources/Margins.uss` of your project
when imported; delete the imported sample when you no longer need it.

## Tests

The tests are ported from upstream (see `Runtime/Components/SOURCE.md` for what was changed and what was left out).
They only compile when the package is listed under `testables` in your `Packages/manifest.json` (UPM install; in the classic Assets install they are part of the project and always compile). Assemblies:
`FutureDigitalPlus.UIKit.Components.Tests` (runtime, EditMode and PlayMode), `FutureDigitalPlus.UIKit.Components.Tests.Roslyn.IntegrationTests` (checks that
the generators produced factories and traits) and `FutureDigitalPlus.UIKit.Components.Tests.Editor` (EditMode only). The test
assemblies use NSubstitute, bundled under `Tests/Runtime/Components/NSubstitute/`, which is only active when
tests are included (`UNITY_INCLUDE_TESTS`).

The editor tests read test assets from the kit's own `Tests/Editor/Components/` folder. Attribute arguments must be constants, so
`TestAssetPaths.KitAssetRoot` (`Tests/Editor/Components/AssetDatabaseAssetResolverTests.cs`) names the kit folder once:
`Packages/com.futuredigitalplus.uikit` in the UPM source, rewritten to `Assets/FutureDigitalPlus/UIKit` by `Tools~/make_classic_kit.py` for the classic Assets install.
The test `Kit_Asset_Root_Constant_Matches_The_Install` fails with a clear message when the kit sits somewhere else (a renamed package or a different `--dst`).
