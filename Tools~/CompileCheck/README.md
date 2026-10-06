# Tools~/CompileCheck: checks without Unity

Compile every assembly of the package with Roslyn against Unity's own reference assemblies (no Unity process, about 20 seconds), and check things that Unity's compile does not.

| Check | How | Fails when |
|---|---|---|
| Compile | `plan.py` turns the asmdef graph into a plan, `Program.cs` compiles each assembly in dependency order, runs the Components source generators, reports errors | any `error CS` / `error UIC` |
| XML documentation | files and folders listed in `doc-scope.txt` (the new modules, the facade, files this package rewrote) | CS1591 (missing comment) or a malformed comment (CS1570 to CS1587) in those files |
| Name hazards (`--hazards`, always on in the script) | every namespace segment and public type of the kit against the public top-level type names of all `UnityEngine*` and `UnityEditor*` assemblies, with `Tests/Editor/Lint/LintAllowList.cs` (the 9 upstream names) | a new name equals a Unity type name |
| Lint rules (`LintRunner`) | the EditMode lint tests, run by dotnet | see `Tests/Editor/Lint` |

```
Tools~/CompileCheck/run-compile-check.sh                                    # runtime and editor assemblies
Tools~/CompileCheck/run-compile-check.sh --tests --test-project <project>   # also the test assemblies (needs a project whose Library has the TestRunner and NUnit DLLs)
Tools~/CompileCheck/run-compile-check.sh --samples                          # also Samples~
Tools~/CompileCheck/run-compile-check.sh --define UNITY_WEBGL               # compile with a platform's scripting symbols (repeat --define for more)
Tools~/CompileCheck/run-compile-check.sh --dump-generated <dir>             # write the generated source of the Components generators
dotnet test Tools~/CompileCheck/LintRunner --artifacts-path <temp dir>      # lint rules without Unity
```

Build output goes to a temp folder (`--work`), never into the package. Requirements: .NET SDK 10, Python 3.9+, Unity 6000.3 installed (macOS layout; pass `--unity <Unity.app>` otherwise).
Roslyn 4.0.1 is the version in the offline NuGet cache and the one Unity 6000.3 uses for source generators.

Limits: it approximates Unity's compile (no player settings, no `csc.rsp`, defines only from the asmdefs and the version defines of packages in `--installed`). Assemblies whose
`defineConstraints` are not met, such as the optional-package adapters, are skipped and listed. A pass here does not replace a Unity compile; it finds most mistakes in seconds.
Other agents' half-finished files make the compile fail in their own assemblies; read the assembly name before you blame your change.

## Lint rules (`Tests/Editor/Lint`)

Pure rule classes (`Tests/Editor/Lint/Pure`, C# 9, no UnityEngine) with unit tests on small inputs, and `LintPackageTests`, which applies them to the package's own files. The same
sources run in Unity EditMode (assembly `FutureDigitalPlus.UIKit.Lint.Tests.Editor`) and in the dotnet `LintRunner`.

| Rule id | Rule | Scope |
|---|---|---|
| `uss-forbidden-property`, `-calc`, `-at-rule`, `-selector` | no `gap`, `row-gap`, `column-gap`, `box-shadow`, `z-index`, `calc()`, at-rules other than `@import` (`@media`, `@keyframes`), pseudo-elements, sibling combinators, attribute selectors, `:not` `:nth-*` `:has` `:is` `:where` | `Runtime/**` and `Editor/**` `.uss` |
| `uss-undefined-variable` | every `var(--fdp-x)` names a custom property declared in a package sheet or a baked theme asset, or has a fallback `var(--fdp-x, 8px)` | same |
| `uss-bare-class` | class selectors start with `ds-`, `fdp-` or `unity-`; `is-*` only chained to such a class; the Animation sheets are exempt (camelCase upstream names, attached to their own elements only) | `Runtime/**` `.uss` |
| `resource-unresolved` | every `Resources.Load`, `LoadAsync`, `LoadAll` literal (or a `const string` passed to them), every `resource("...")` and relative `@import url("...")` finds a file in a `Resources` folder of the package | `Runtime/**`, `Editor/**` |
| `meta-missing`, `meta-orphan`, `meta-in-hidden-folder`, `meta-duplicate-guid`, `meta-no-guid` | every visible file and folder has a `.meta`, every `.meta` has its file, hidden folders (`~`) and dot files have none, GUIDs are unique | whole package |
| `asmdef-foreign-reference` | an assembly definition references only `FutureDigitalPlus.UIKit*`, `Unity.*`, `UnityEngine.*`, `UnityEditor.*` or a GUID: no game code | every `.asmdef` of the package, its tests and its samples (not `Tools~`) |
| `asmdef-optional-unconstrained` | an assembly that references an optional package (Addressables, Input System, Localization, TextMeshPro, uGUI, Cinemachine, URP, HDRP) has a `defineConstraints` entry, so the package compiles without it | same |
| `asmdef-core-engine` | a `*.Core` assembly sets `noEngineReferences` and references only other `*.Core` assemblies | same |
| `name-hazard-namespace`, `name-hazard-type` | no namespace segment or public type of the kit equals a loaded Unity type name (Unity only: `NameHazardScanTests`; without Unity: this tool's `--hazards`) | assemblies of the kit, not tests or samples |

Accepted exceptions are in `Tests/Editor/Lint/LintAllowList.cs`, each with a reason; an entry that matches nothing fails the test, so the list cannot rot. The name-hazard list holds the nine upstream names
(`Behaviour`, `UIDocument`, `Editor`, `Animation`, `EditorHelpers`, `Scope`, `ILogger`, `Easing`, `ColorField`); new names do not go there.
`meta-missing` fails for files that Unity has not imported yet: let Unity generate the metas, never write them by hand.
