# Upstream features: included, excluded, modified

A one-page summary. The authoritative, file-level lists are the `SOURCE.md` files next to each module
(`Runtime/<Module>/SOURCE.md`); licence texts, commits and checksums are in `THIRD_PARTY_NOTICES.md`.
"Modified" means a file differs from upstream; every such change carries a `FutureDigitalPlus:` comment in C#
(asmdef and Markdown changes are only listed in `SOURCE.md`).

## DesignSystem (sinanata/unity-ui-toolkit-design-system, MIT, commit 65f6211, version 1.5.3)

| | |
|---|---|
| **Included** | Everything under upstream `Assets/DesignSystem/`: the 15 USS sheets (tokens, typography, icons, buttons, inputs, tabs, cards, navigation, badges, controls, overlays, feedback, mobile, dropdown popup), the Dark and Light baked themes, `ThemeData`, `ThemeRuntime` and the UIDocument appliers, runtime behaviours (toggle knob, spinners, tabs, drag and drop, dropdown placement), typography and the Google Fonts runtime fetcher, the 120 SVG icons, the Theme Configurator, theme baker and presets, the Google Fonts importer window, the font asset factory, and the six upstream guides. Same layout; assemblies and namespaces renamed (`DesignSystem.Runtime` is `FutureDigitalPlus.UIKit.DesignSystem`, `DesignSystem.Editor` is `FutureDigitalPlus.UIKit.DesignSystem.Editor`), as are the Resources paths, the USS variables (`--fdp-*`) and the shader name. |
| **Guarded** | The GPU material pipeline (`Runtime/DesignSystem/Fx`, `DsFxCompileCheck`) and the PanelRenderer appliers need Unity 6000.5 and are compiled out on 6000.0 to 6000.4 by the upstream `#if UNITY_6000_5_OR_NEWER`; nothing was added. The shader `DsFxBlueprint.shader` imports without messages on 6000.3. |
| **Excluded** | The upstream showcase project (`Assets/Showcase`: scene, world-space corridor, doc overlay, input and uGUI code), the third-party Codigrate theme palettes (no licence file), the font binaries and font assets (SIL OFL, game imports its own), upstream build tooling and WebGL pipeline, project settings, screenshots, upstream README/CHANGELOG/AGENTS/llms.txt. |
| **Modified** | `ThemePresets.cs` (theme folder resolved from the package instead of `Assets/DesignSystem/...`; generate returns 0 with a warning when the package is read-only), `ThemeBaker.cs` (Rebake All skips read-only package themes), `ThemeConfiguratorWindow.cs` (obsolete-API warning suppressed on 6000.3), `ThemePreview.uxml` (URL path hint), `DsFx.cginc` (two comment lines), `ARCHITECTURE.md` (one link). |
| **Added** | Showcase sample (excerpt plus a new 120-line bootstrap), tests, `Documentation~/DesignSystem/README.md` path map. |
| **Behaviour note** | On Unity 6000.3.25f1 a class-scoped theme does not win over the base `:root` tokens when it sits on the same element as the base sheet or on an ancestor of it; it must be on a descendant. Upstream's `ThemeRuntime` comments say sheet order decides. Install the base sheet on the panel root (the facade does). |

## Components (jonisavo/uicomponents, MIT, commit dc64355, version 1.0.0-beta.13)

| | |
|---|---|
| **Included** | The framework (`UIComponents` core: `UIComponent`, `[Layout]`, `[Stylesheet]`, `[SharedStylesheet]`, `[Query]`, `[Provide]` and `[Dependency]` injection with scopes, event interfaces such as `IOnClick`, `[UxmlName]`, `[UxmlTrait]`, `[UIComponentEffect]`, asset sources and `[AssetRoot]`/`[AssetPrefix]`, `DebugLogger`), `TestBed<T>` (assembly `...Components.Testing`), the optional Addressables asset source (`...Components.Addressables`), the editor tools (`AssetDatabaseAssetSource`, `ConventionValidator`), the four Roslyn DLLs (generators and analyzers, rebuilt from upstream's source with the namespace literals patched) with PDBs and their `.meta` labels, the six upstream samples, and the upstream tests (runtime, Roslyn integration, editor) with bundled NSubstitute 4.3.0 / Castle.Core / two System.* DLLs (test-only, never in a build). Namespaces and assembly names renamed to `FutureDigitalPlus.UIKit.Components[...]`. |
| **Excluded** | Benchmarks (need the performance test package), the Addressables tests (need a configured Addressables group), the Roslyn C# sources, schema generator, upstream project settings and repository tooling. The upstream wiki is a separate repository and is not bundled. |
| **Modified** | Layout only: the DLLs sit in `Core/Roslyn` instead of next to the `Core` folder, so the generators and analyzers apply to `FutureDigitalPlus.UIKit.Components` and the assemblies that reference it, not to the whole project. File contents are byte-identical (SHA-256 recorded). `FutureDigitalPlus.UIKit.Components.Editor.asmdef` references `FutureDigitalPlus.UIKit.Components` by name instead of GUID. Test asmdef and path changes so the tests run from inside a package (listed in `SOURCE.md`), and `csc.rsp` files (new) that silence `CS0618` in the two test assemblies that contain generated `UxmlFactory` code. |
| **Behaviour note** | The upstream generator emits the obsolete `UxmlFactory`/`UxmlTraits` for components with `[UxmlName]`/`[UxmlTrait]`; on Unity 6 an assembly that uses them gets `CS0618` warnings from generated code. Our own tests silence them with `csc.rsp`; a game can do the same or not use the attributes. |

## Animation (instance-id/ElementAnimationToolkit, MIT, commit a5d5184, version 0.1.93)

| | |
|---|---|
| **Included** | The runtime animation library: `AnimateOpacity/Width/Height/BackgroundColor/BorderColor/Color/ImageTintColor/Display/Value/Slider`, sequences (`AnimFadeInSequence`, `ShowForDuration`, character sequences), continuous animations (border pulse), hover animations, event helpers, `Easy` easing functions, value stores, task helpers, `AnimatedFoldout`, `AnimatedLabel`, `MaskedInputField`, the helper extensions (the code of the unavailable `id.instance.extensions` package is already bundled in the upstream repository; nothing had to be vendored). |
| **Excluded** | The upstream editor window and its generator (about 2,600 lines), the Source Code Pro font files (SIL OFL, no licence text upstream), the colour-name list from an unlicensed gist, editor-only data types and asset helpers, the Sass sources, the sample scene. |
| **Modified** | Whole-file `#if UNITY_EDITOR` guards removed so it works in player builds; `UxmlFactory`/`UxmlTraits` replaced by `[UxmlElement]` on the three elements; `AssetDatabase`/`EditorApplication` calls replaced by `Resources.Load` and an immediate call; some editor-only members removed from `VisualElementExtension` and `ParseElements`; asmdef references and `versionDefines` removed; one stray `Debug.Log` removed. **Behaviour difference:** `<AnimatedFoldout>` without a `value` attribute starts collapsed. |

## Extras (plyoung/UIElements, Unlicense, commit 8fac813)

| | |
|---|---|
| **Included** | Aspect ratio padding, colour field, colour popup and 2D slider with drag manipulator, popup panel / message / text-field popups, tooltip and tooltip manipulator, blur panel (the UI half), the `UIElementsExtensions` helper, and the 7 style sheets. The folder name `PupupPanel` (sic) is upstream's; the namespace `Game.UI` became `FutureDigitalPlus.UIKit.Extras`. |
| **Excluded from the compiled module** | The URP blur render feature, its shader, material and Shader Graph (pre-Unity-6 URP API, would add URP as a dependency; kept verbatim in `Samples~/Extras/BlurUrpLegacy~`), the demo game's theme style sheets (reference sample), the sample UXML (sample). |
| **Modified** | `#pragma warning disable CS0618` around the obsolete `UxmlFactory`/`UxmlTraits`; `DragManipulator` lost its copies of `Clickable`'s pointer handlers (they hid the base ones in Unity 6); `Slider2D` uses `style.translate` instead of the obsolete `transform.position`. New assembly `FutureDigitalPlus.UIKit.Extras` (upstream has no asmdef). |
| **Behaviour note** | `Tooltip` is hover only. Interactive drag/hover behaviour is covered by tests only as far as events can be simulated; check on a device. |

## UIKit facade (new code, Future Digital Plus)

`UIKitHost`, `UIKitInstaller`, `UIKitInstallation`, `UIKitOptions`, `SafeAreaInsets`, `UIKitMotion`, the Quick Start
sample, tests and guides. The other samples (Design System Showcase, Animation, Extras stylesheets) are new glue around
upstream modules. Nothing from upstream.
