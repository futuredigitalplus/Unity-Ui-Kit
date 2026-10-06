# Platforms, render pipelines and Unity versions

What is verified, what is only reasoned, and what nobody has run. Read this before you promise a platform to a producer.
The matrix was run on **Unity 6000.3.25f1, macOS arm64 (Metal)**, in scratch copies of a clean project (never a game). The
kit has no camera, pass, render feature or pipeline-specific shader of its own: UI Toolkit draws the panel, so the same
code runs in every pipeline. The tests below prove that for the pipelines that were run.

Legend: **run** = executed in Unity; **compile** = compiled or exported, not run; **static** = reasoned from the code;
**not verified** = nobody has tried.

## Verified matrix

The full table (tests per variant, compile errors and warnings, player compile per target, the samples, test counts, what the first run found) is
[CompatibilityMatrix.md](CompatibilityMatrix.md). In short, all of these ran the whole EditMode and PlayMode suites with 0 compiler errors and 0 warnings:

| Variant | Run |
|---|---|
| Built-in, Gamma and Linear, Input Manager, no optional package | run |
| Built-in + Input System (only) / Input System (installed, inactive) / both active | run |
| Built-in + uGUI, + Localization, + Addressables, + all of them (Input System only) | run |
| URP 17.3, 3D `UniversalRenderer`, Gamma and Linear | run |
| URP 17.3, 2D `Renderer2D`, Gamma | run |
| HDRP 17.3, Linear (HDRP supports Linear only; in Gamma it logs an error for every camera that renders) | run |

The paint test renders a themed panel into a `RenderTexture`, with and without a camera in the scene (a camera-less menu
scene is a common setup), and compares the pixels with the theme's colours. The screen-overlay path (no target texture) cannot
be pixel-checked in batch mode (screen capture returns null); it was checked on `RenderTexture` panels only.

Player checks (`PlayerBuildInterface.CompilePlayerScripts`, and an IL2CPP project export with Managed Stripping **High**):

| Check | Result |
|---|---|
| Script compile for iOS, Android, macOS standalone (every variant: Built-in, URP, HDRP, all optional packages) | compile, success (18 runtime assemblies each) |
| iOS IL2CPP export, Metal, stripping High | compile (export only, no Xcode build), 0 errors, 0 warnings |
| Android IL2CPP Gradle export, ARM64, GLES3 + Vulkan, stripping High | compile (export only), 0 errors, 0 warnings |
| Kit elements used only from UXML under High stripping | checked in the generated IL2CPP output (types and code kept), not by running a player |

Numbers of tests change with every module that is added; the current counts are in `Verification.md`. Reproduce any row with
`Tools~/Matrix/run-matrix.sh <variant>` (see [Reproducing](#reproducing-and-checking)).

## Not verified here

No module was installed or no editor was available. Treat these as unproven, not as unsupported.

| Target | State |
|---|---|
| WebGL | approximation only (Roslyn with `UNITY_WEBGL`, 0 errors; the module is not installed); see notes below |
| tvOS, visionOS | not built; compile status unknown |
| Windows, Linux standalone | approximation only (Roslyn with the target's symbols, 0 errors; the module is not installed); macOS standalone compiles in Unity |
| Consoles | static only; the kit adds no console-specific code |
| XR (headsets) | a `RenderTexture` panel on a quad paints on every pipeline above; world-space panels exist only in Unity 6000.5 |
| Unity 6000.0 to 6000.2, 6000.4, 6000.5 | never run. `package.json` asks for `6000.3`. Code behind `UNITY_6000_5_OR_NEWER` has never compiled |
| Device behaviour | touch, hover stickiness, screen readers, OS font scale, soft keyboard, real notches and foldables: never run on a device |
| `Screen.safeArea` on a screen overlay | logic tests with a fake source only |
| Live blur source `BlurSource.FromCamera` | run in the test on Built-in, URP and HDRP (a uniform red scene stays red; HDRP's exposure lowers the value) |

## Render pipelines

- **Neutral by construction.** Nothing in the kit uses `Camera`, `CommandBuffer`, `ScriptableRendererFeature`, `OnRenderImage` or
  `GraphicsSettings` for rendering. Do not add a URP or HDRP package reference to a kit assembly.
- **The one shader of the design system** (`Hidden/FutureDigitalPlus/UIKit/DsFx/Blueprint`, GPU material skins) is drawn by UI
  Toolkit's own renderer through `style.unityMaterial`, so it never meets the SRP Batcher or the render graph. It declares its uniforms outside a
  `CBUFFER`; that is intended, do not "fix" it. The C# side exists only on Unity 6000.5 (`UNITY_6000_5_OR_NEWER`), but the shader file ships in every
  player (26 KB Metal, 128 KB GLES3 and Vulkan), because it sits in a `Resources` folder.
- **Blur.** `BlurPanel` shows a texture you supply. `BlurSource` (Extras) makes one with a plain CG shader and `Graphics.Blit`, which
  works in the Built-in pipeline, URP and HDRP (run on a step image; see [Extras/BlurOrGlassEffect.md](Extras/BlurOrGlassEffect.md)). The
  legacy URP renderer feature in `Samples~/Extras/BlurUrpLegacy~` uses the pre Render Graph API and does **not** run on URP 17.
- **Colour space.** Gamma and Linear both paint the same theme colours (run on Built-in, URP 3D and HDRP).
- **Fx materials (6000.5 only)**: not verified on any pipeline. Do not enable them by default in a game.

## Platform notes

**iOS and Android.** Script compile and IL2CPP export verified (above). The device safe area is applied by the shell's views (`SafeAreaPanel`, which leaves render-texture and world-space panels alone) or by the facade
(`Documentation~/UIKit/README.md`). Neither pads a panel that renders into a `RenderTexture`, a world-space panel or a panel on another display (an in-game monitor does not get the phone's notch).
Do not use both on one document (they would pad twice). Nothing was run on a device.

**WebGL** (static analysis, nothing built):
- No threads and no timers: `Wait.Until` and `Wait.While` (Animation) now run on the main thread, frame paced, so they complete;
  the upstream versions used `Task.Run` and `Task.Delay` and never would have.
- No OS fonts and no system font fallback: bundle the fonts your languages need. `DsGoogleFonts` caches in the browser's IDBFS through `Application.persistentDataPath` (upstream's claim; it
  needs a CORS enabled font host and has not been tested here).
- The screen has no notch: `Screen.safeArea` is the full canvas.
- A phone browser may report `Application.isMobilePlatform == false`, so a platform based touch detection can miss it; the game has to tell the kit (and `UnityEngine.Input.touchSupported` throws when only the Input System is active, so the kit never calls it).
- World-space panels on WebGL are off by default (a 6000.5 geometry buffer overrun).

**Consoles and TV (gamepad only).** `Screen.safeArea` is the full screen on consoles and tvOS, so the platform gives no title-safe margin. The Foundation shell adds one: 5 percent of the panel on every edge on consoles and tvOS by default
(`UiShellOptions.MinimumMarginPercent`, any value from 0 up overrides it); the facade (`UIKitHost`) pads only by `Screen.safeArea`, so a TV game uses the shell (see [Recipes.md](Recipes.md), recipe 0) or pads the root itself.
The runtime font download refuses to run there unless you set `DsGoogleFonts.CacheDirectory` (see below). There are no OS fonts. Keyboard and gamepad navigation needs focusable elements: the Interaction module draws the focus ring, the Extras `Tooltip` opens on focus under keyboard and gamepad navigation,
and `Activate.Register` makes a plain element reachable. The stock `DropdownField` cannot be changed with the D-pad in Unity 6000.3; use the Controls module's `SelectField`. None of this was run on a console or with a real gamepad.

**Windows, Linux, macOS.** Standard UI Toolkit with mouse and keyboard. `Screen.dpi` can be 0 or unreliable (the Editor reports 72), so any scaling by dpi
needs a fallback and a clamp. Touch screens on Windows are not special-cased. A PanelSettings reference resolution of 1080x1920 (the card game's
portrait default) is wrong for landscape desktop games: pass your own or use a constant pixel size.

**XR.** In 6000.3 there is no world-space UI Toolkit panel: target a `RenderTexture` and put it on a quad (paints on every pipeline above). Do not apply the screen safe area to it.
Unity 6000.5 has `PanelRenderMode.WorldSpace` (unverified here).

## Threads, time and culture

- `Wait.Until(condition, frequency, timeout)` and `Wait.While`: the condition always runs on the main thread, at most once per frame and no more
  often than `frequency` milliseconds; the timeout is real time (`Time.realtimeSinceStartupAsDouble`, so a paused game still times out) and throws
  `TimeoutException`. Call them from the main thread. In the Editor outside Play Mode they advance with editor updates.
- `UIParser` (Animation) parses numbers with the **invariant culture**: `"0.5"` is 0.5 on a German device. Upstream used the current culture. The rest of the kit
  already did (`ThemeData` and the facade). `UIParser` is editor-time style tooling carried over from upstream: it finds elements by reflection on a non-public Unity method and
  returns null where Unity hides it, and `url(...)` backgrounds cannot be resolved at runtime. Use `UxmlElement` types and USS sheets in game code.

## Runtime font download (`DsGoogleFonts`)

Opt-in and never automatic. FreeType needs a file, so a download is written to `DsGoogleFonts.CacheDirectory`: by default
`Application.persistentDataPath/FutureDigitalPlus/UIKit/Fonts`, or the same sub-folder of `Application.temporaryCachePath` when the persistent path is empty or throws.
`DsGoogleFonts.IsSupported` is false on consoles and tvOS unless you set `CacheDirectory` yourself (to a folder the platform lets you write to); `EnsureFont` and `Load` then report
an error up front instead of failing after the download. Set `CacheDirectory` before the first call; set it to `null` to restore the default.

## What ships in a build

Unity packs every `Resources` folder of an installed package into every player, whether or not the C# that uses it is compiled. Measured in an Android export of the
kit (no `testables`): about **1.25 MB**, of which the 120 SVG icons are 1,043 KB (always included), the design system stylesheets 135 KB, the Extras stylesheets
18 KB, the Fx shader 26 to 128 KB depending on the graphics API, plus the blur shader (a few KB) and about 44 KB of test layouts under
`FutureDigitalPlus/UIKit/Tests/` (and a one-line `DefaultRuntimeTheme.tss` there that the PlayMode test panels use). The test layouts ship because the Components tests load them through `Resources`; they cannot move into code. The paths are prefixed so they cannot collide;
do not use them. `[assembly: AlwaysLinkAssembly]` keeps the whole design system assembly through stripping (needed by the Fx registry).

## Reproducing and checking

| What | How | Needs Unity |
|---|---|---|
| Tests in one pipeline / colour space / input variant, one table row | `Tools~/Matrix/run-matrix.sh <variant> --base-project <clean project>`; `--list` shows the variants, `--table` prints all rows. Variants: base, base-linear, legacy, input-system, both, ugui, localization, addressables, all-optional, urp3d, urp2d, urp3d-linear, hdrp-linear | yes |
| Player script compile and IL2CPP export | `run-matrix.sh <variant> --player-compile --export ios --export android` | yes (platform modules) |
| Compile every assembly with Roslyn, XML docs of the new modules, name hazards | `Tools~/CompileCheck/run-compile-check.sh [--tests --test-project <project>]` | no |
| Lint rules (USS features, `--fdp-*` variables, class prefixes, resource paths, `.meta` files, name hazards) | EditMode tests in `Tests/Editor/Lint`; without Unity: `dotnet test Tools~/CompileCheck/LintRunner` | optional |
| Pipeline smoke, `Wait`, culture, font cache and blur tests | PlayMode tests in `Tests/Runtime/Platform` (assembly `FutureDigitalPlus.UIKit.PlatformSmoke.Tests`); a game that adds the package to `testables` (or installs the classic Assets copy) re-proves its own pipeline | yes |

Each test logs one `FDPMATRIX ...` line (pipeline, renderer, graphics device, colour space, paint result) that `run-matrix.sh` collects into the table.
See `Tools~/Matrix/README.md` and `Tools~/CompileCheck/README.md`.
