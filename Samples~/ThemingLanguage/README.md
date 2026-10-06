# Theming and language (sample)

Everything a player can change about how a UI looks and reads, in one small page. Modules: DesignSystem, Language, Interaction (and Controls for the segmented picker).

1. Window > Package Manager > Future Digital Plus UI Kit > Samples > Theming and Language > Import.
2. Create an empty scene, add an empty GameObject, add the component `Future Digital Plus UI Kit > Theming and Language Sample`.
3. Press Play.

## What you can change while it runs

| Control | What happens | Code |
|---|---|---|
| Language: English, Deutsch, Arabic | every label changes; Arabic mirrors the layout (`fdp-rtl`, text direction) | `DictionaryTextSource.SetLocale`, `LocaleClassApplier.Bind`, `LanguageStyles.Install` |
| Theme: Dark, Light | the two baked themes of the design system | `ThemeManager.Register/Attach/Set` |
| Brand buttons (Ocean, Forest, Crimson) | a theme made at run time from one colour and the page background | `ThemeData.FromBrand(colour, background, dark)` |
| High contrast | the high-contrast version of whatever theme is worn | `ThemeManager.HighContrast` |
| Reduce motion, A- / A+ | classes on the root (`fdp-reduce-motion`, `fdp-text-scale-150`) | `AccessibilityApplier` with `AccessibilityOptions` |
| Status line | how many colour pairs of the current theme fall below WCAG AA (AAA in high contrast) | `ContrastAudit.Failures` |

## Unity Localization

The sample uses the dictionary backend, which needs no package. With `com.unity.localization` installed the assembly `FutureDigitalPlus.UIKit.Language.UnityLocalization` exists, and the
backend swap is one line: `UiLocalization.Default.Backend = new UnityLocalizationBackend();`. The labels do not change.

## Notes

- Arabic needs a font with Arabic glyphs. The kit bundles no font; a device whose OS fonts cover Arabic and a runtime that falls back to them show it, otherwise the glyphs are boxes. The mirroring itself does not depend on the font.
- The mirrored layout does not cover Slider, range sliders and the side of a ScrollView scrollbar (limits are listed in the Language module guide).
- Brand themes are runtime objects: the sample destroys the previous one when a new one is applied and the last one when it is destroyed itself.
- Run in Unity 6000.3.25f1 by `Tools~/SamplesSmoke` (PlayMode: it builds without a logged error, shows the widgets named above, survives 30 frames of its own `Update` and leaves no document behind) in the Built-in pipeline, URP 3D, URP 2D and HDRP, with the Input System package only and with all optional packages; see `Documentation~/CompatibilityMatrix.md`. The test does not click or press anything, and nothing was run on a device.
