# Theming

Everything a game does to change how the design system looks, from "use the dark theme" to "build a brand theme from two colours at runtime".
This guide is new (Future Digital Plus); the upstream guides next to it describe the original layout.

## What a theme is

A theme is a `ThemeData` asset: 63 token values (colours, font sizes, radii, spacing, border widths, durations) plus a scope. Its **baked stylesheet** is a USS block
(`:root { --fdp-color-primary: ...; }`) that `ThemeRuntime.Apply(root, theme)` adds to a root element. Every `ds-*` rule reads `var(--fdp-...)`, so adding another
theme's sheet repaints the tree. The scope decides when the sheet paints: `:root` paints at once (the default theme), a class such as `.theme-light` paints only on a root that carries it.

| Built-in theme | Scope | Made for |
|---|---|---|
| `Dark` | `:root` | the default look |
| `Light` | `.theme-light` | day mode |
| `NeutralDark`, `NeutralLight` | `:root`, `.theme-neutral-light` | a grey, brand-free start for a new game |
| `HighContrastDark`, `HighContrastLight` | `.theme-hc-dark`, `.theme-hc-light` | players who need 7:1 text and 4.5:1 borders |

All six are pure data in `ThemePalettes` (no UnityEngine) and are checked by `ContrastAudit` in the EditMode tests: a palette that drops below WCAG AA fails the build of the package.
The assets are written and baked by the menu **Tools > FDP UI Kit > Design System > Generate Built-in Themes** (batch: `ThemePresets.GenerateBatch`).
Run it after changing a palette; the tests named `BuiltIn...` fail until you have.

## Where the theme goes

A theme has to land on the element where the base sheet resolves its tokens, or on a descendant of it, otherwise the base `:root` block wins ties by order. The rule that works on
Unity 6000.3: base sheet on the panel root (`root.parent`), theme on the document root below it. `UIKitInstaller` does this; see `Runtime/DesignSystem/SOURCE.md` for the measurements.

```csharp
root.parent.styleSheets.Add(Resources.Load<StyleSheet>("FutureDigitalPlus/UIKit/DesignSystem/Styles/DesignSystem"));
ThemeRuntime.Apply(root, Resources.Load<ThemeData>(ThemeData.DarkResource));
```

`ThemeRuntime.Apply` remembers what each root wears, so swapping, re-applying and `Apply(root, null)` (back to the base tokens) are all the same call. Applying the same theme again is a no-op.
Swapping a theme on a live panel puts `fdp-no-transition` on the root for 120 ms (`ThemeRuntime.NoTransitionMillis`), so colours change at once instead of fading through the whole tree.

## A brand theme at runtime (no Editor, no bake)

```csharp
var brand = ThemeData.FromBrand(primary: new Color(0.91f, 0.27f, 0.45f), background: new Color(0.06f, 0.07f, 0.10f), dark: true);
ThemeRuntime.Apply(root, brand);
brand.SetToken("--fdp-color-primary", Color.cyan);   // change anything...
ThemeRuntime.Apply(root, brand);                      // ...and apply again: a new stylesheet is generated, the old one destroyed
Object.Destroy(brand);                                // when done; its stylesheet goes with the last root that wears it
```

`ThemeData.CreateRuntime(template)` copies any baked theme; `FromBrand` also derives the primary family (hover, press, soft), surfaces, borders, text, focus ring and selection from two
colours (`ThemeDerivation.FromBrand`: the brand colour is never altered, text and borders are corrected until they pass AA). How it works: Unity has no API that creates a stylesheet
from text at runtime, but a `StyleSheet` serialises through `JsonUtility`, so `ThemeSheetPatcher` clones the baked template and rewrites its colour and dimension tables. Slots are
found by token name (never by position), a clone is never changed after it is attached (Unity caches resolved styles per sheet), and clones are reference counted by the roots that wear them
(`ThemeRuntime.LiveRuntimeSheets` for leak checks).

Limits: a runtime theme keeps its template's scope (it cannot change from `:root` to a class); it can only change tokens its template sheet contains (extra tokens only if the template was baked with them); if a
Unity version lays the stylesheet out differently, `ThemeSheetPatcher.TryCreate` fails with one warning and `ThemeRuntime.Apply` returns false (a canary test, `PatcherCanary_...`, tells you on upgrade).

## Tokens

The 56 original tokens are `--fdp-color-*`, `--fdp-font-size-*`, `--fdp-radius-*`, `--fdp-space-*`, `--fdp-border-*`, `--fdp-transition-*`. Added for global use:

| Token | Default (Dark) | Use |
|---|---|---|
| `--fdp-color-success`, `--fdp-color-info` | green, blue | feedback colours (toasts, hud) |
| `--fdp-color-scrim` | black 60% | behind modal dialogs and sheets |
| `--fdp-color-focus-ring` | not declared: falls back to `--fdp-color-text-primary` | focus outline under keyboard and gamepad navigation (Interaction module) |
| `--fdp-color-selection` | primary at 35% | text selection in input fields (follows the theme) |
| `--fdp-font-size-display` | 34px | titles and big numbers |
| `--fdp-transition-slow` | 320ms | screen-level transitions |
| `--fdp-touch-min`, `--fdp-control-h-sm/md/lg`, `--fdp-field-h`, `--fdp-tab-h`, `--fdp-chip-h` | 44, 28/36/44, 40, 32, 24 px | control metrics, redefined by the touch tier |
| `--fdp-icon-s/m/l`, `--fdp-row-h`, `--fdp-dialog-w`, `--fdp-page-max-w`, `--fdp-gutter`, `--fdp-focus-ring-width` | 16/20/24, 44, 320, 960, 16, 2 px | layout metrics for other modules |

The first block (colours, font size display, slow transition) is part of `ThemeData` and baked; the metrics are not: the touch tier redefines them on a class, which a theme sheet on the same element
would otherwise outrank. A game that wants other values writes them as **extra tokens**: `theme.extraTokens` (name, kind Color, Px or Ms, value) are emitted into the same block, baked and switched with the
theme, and patched at runtime when the base theme was baked with them. Write `var(--mygame-color-health, #E5484D)` with a fallback so a theme without the token still renders.

Consumers of the kit's tokens always write a fallback: `var(--fdp-color-focus-ring, var(--fdp-color-text-primary))`. A theme baked before a token existed then still works.

## Units, the touch tier and font sizes

1 USS pixel is 1 dp (1/160 inch) on handhelds when the panel scale policy of the Foundation module is used (`ScaleWithScreenSize` or a dp-based constant scale); the base sizes (36 px buttons, 14 px body text) are
sized for that. `Mobile.uss` therefore no longer contains a single font size: a brand theme's `--fdp-font-size-*` applies everywhere. The `fdp-input-touch` class (added by the Interaction module or the facade
when the player uses touch) only redefines the control-height tokens (`--fdp-control-h-sm/md/lg` 36/48/56, `--fdp-field-h` 48, `--fdp-tab-h` 40, `--fdp-chip-h` 26) and tightens a few layout rules.
Controls use `min-height: var(--fdp-control-h-md, 36px)` instead of a fixed `height`, so larger text (OS font scale, `ThemeVariants.WithTextScale`) grows a button instead of clipping it. Hit areas below 44 px
are padded, not enlarged.

## Contrast

| Rule | Where |
|---|---|
| Text on a filled accent uses `--fdp-color-text-on-accent` (secondary, tertiary and danger buttons used the page text colour before: 3.3:1 on Dark) | `Buttons.uss` |
| Hover and press fills lighten in Dark so the dark ink keeps 4.5:1 in every state; Light darkens them under white ink | `ThemePalettes` |
| A control boundary (input, toggle, check, radio, card) uses `--fdp-color-border-strong` at 3:1 or more; the decorative `--fdp-color-border` stays quiet | `Inputs.uss`, `Controls.uss` |
| The text field's focus border follows the field, not the inner input (Unity 6000.3 puts `:focus` on the field) | `Inputs.uss` |

`ContrastAudit.Run(palette, level)` returns every checked pair with its ratio; `Failures` returns only the failing ones. Levels: `Standard` (4.5:1 text, 3:1 boundaries), `High` (7:1 text, 4.5:1 boundaries).
Disabled text is exempt. Use it on your own themes (it is plain C#, no editor needed):

```csharp
var failures = ContrastAudit.Failures(myTheme.ToColorMap(), ContrastLevel.Standard);
```

**The two built-in themes look slightly different from 0.1.0** (the contrast fixes): Dark hover/press fills are lighter, Dark `tertiary` is `#B26CF9` and `border-strong` is `#5E6E88`; Light `primary`
is `#15803D`, `warning` is `#B45309`, `border-strong` is `#78879F`. Rarity colours are unchanged.

## High contrast

```csharp
ThemeRuntime.SetHighContrast(root, true);    // the built-in HighContrast* counterpart, or a derived runtime variant for brand themes
ThemeRuntime.SetHighContrast(root, false);   // back to the theme the root wore before
```

`ThemeVariants.HighContrast(source, level)` derives a variant from any theme (hues survive, neutrals become black and white at `High`, borders grow by 1 px); `ThemeVariants.HighContrastFor` returns the baked
counterpart when one exists. `ThemeVariants.WithTextScale(source, 1.25f)` multiplies the font sizes. `ThemeManager.HighContrast` calls `SetHighContrast` for every attached root, and `UIShellHost` ([../Bootstrap/README.md](../Bootstrap/README.md)) sets it from the player's and the OS accessibility settings; without that host call it from your settings screen. Text size steps need no variant: `AccessibilityApplier` writes the `fdp-text-scale-*` classes that the Interaction sheet turns into font sizes, so `WithTextScale` is for code that builds a theme itself.

## Choosing a theme by name, following the OS

`ThemeManager` holds named themes and the roots that wear the current one (roots are weak references):

```csharp
var themes = ThemeManager.Default;
themes.Register("dark", dark); themes.Register("light", light);
themes.Attach(root);
themes.PersistKey = "ui.theme";           // optional PlayerPrefs persistence
themes.Restore() ;                        // select the stored one, if still registered
themes.Set("light");                      // applies to every attached root, raises Changed
themes.Follow(myModeSource, "dark", "light");   // IThemeModeSource: Unity has no system dark-mode API; plug a native query or a settings screen in
themes.HighContrast = true;
```

## Checks in the Editor

- `ThemeBuildCheck` (an `IPreprocessBuildWithReport`) fails a player build when a theme asset in a `Resources` folder or used by an enabled scene has no baked stylesheet or was edited after its bake;
  a stylesheet baked by an older version of the package is only a warning. `ThemeBuildCheck.Evaluate(theme)` gives the state; the Theme Configurator shows it.
- `ThemeBaker` records a hash of the generated USS at each bake (`ThemeData.BakedHash`, compared with `ComputeBakeHash`).
- `Lists.uss` styles Unity's `ListView`, `MultiColumnListView`, `TreeView`, `Foldout`, `TabView`, `RadioButtonGroup` and `GroupBox` with the same tokens (additive; nothing else changed).

## Not verified here

The tests that prove the points above (in `Tests/Runtime/DesignSystem` and `Tests/Editor/DesignSystem`) ran in Unity 6000.3.25f1 in every variant of [../CompatibilityMatrix.md](../CompatibilityMatrix.md) (Built-in, URP, HDRP), after the six built-in themes were generated and baked. IL2CPP
(the patcher uses `JsonUtility` only, no reflection on the stylesheet), WebGL and consoles are untested for runtime themes.
