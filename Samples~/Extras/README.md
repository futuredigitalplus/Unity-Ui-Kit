# Extras sample files

Reference material for the Extras module. Nothing here is compiled; copy what you need into your project.

| File | What it is |
|---|---|
| `ExtrasThemeVariables.uss` | The USS custom properties the Extras style sheets need, with the upstream palette. |
| `ExtrasDesignSystemBridge.uss` | The same properties mapped onto the DesignSystem tokens (new file, by Future Digital Plus). |
| `AspectRatioPadding.Sample.uxml` | Upstream UXML sample for `FutureDigitalPlus.UIKit.Extras.AspectRatioPadding` (4:3 design on a wider display). |
| `UpstreamMiscStylesheets/` | The upstream `_misc/stylesheets` set (default theme, buttons, sliders, scroll view, toggles...). Reference only: it points at fonts and cursor images of the upstream demo project that are not part of this package, so importing this sample logs about 20 USS warnings (`Invalid asset path 'Assets/GUI/...'`, one `justify-items`). They are expected and harmless; delete the folder if you do not need it. |
| `BlurUrpLegacy~/` | The upstream URP blur render feature, shader, material and Shader Graph. Reference only and not usable as is on Unity 6, see `Documentation~/Extras/BlurOrGlassEffect.md`. The trailing `~` hides the folder from the Unity importer. |

See `Documentation~/Extras/README.md` for the module guide.
