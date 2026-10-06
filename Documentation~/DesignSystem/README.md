# DesignSystem module: guides

Five of the six guides in this folder are the upstream documentation of
[sinanata/unity-ui-toolkit-design-system](https://github.com/sinanata/unity-ui-toolkit-design-system)
(MIT, (c) 2026 Sinan Ata / Leap of Legends), copied with one relative link changed and the mechanical rename applied (`Tools~/rename/rename.py`: Resources paths, USS
variables `--fdp-*`, the touch class `fdp-input-touch`, menus, namespaces). They are written for the upstream project layout,
so read the folder paths in them through the table below.

| Guide | About |
|---|---|
| [ARCHITECTURE.md](ARCHITECTURE.md) | Tokens, import order, theming, runtime behaviours, folder layout |
| [COMPONENTS.md](COMPONENTS.md) | One line per component class |
| [MOBILE.md](MOBILE.md) | The `.fdp-input-touch` class, touch targets, breakpoints |
| [FONTS.md](FONTS.md) | Google Fonts importer and runtime fetch, weights, scripts |
| [ICONS.md](ICONS.md) | The 120 SVG icons and how to add one |
| [MATERIALS.md](MATERIALS.md) | The GPU material pipeline (Unity 6000.5 and newer) |
| [THEMING.md](THEMING.md) | New: runtime and brand themes, tokens, contrast, high contrast, `ThemeManager`, the touch tier (Future Digital Plus) |

## Path map: upstream to this package

| In the guides | In this package |
|---|---|
| `Assets/DesignSystem/Runtime/...` | `Runtime/DesignSystem/...` (assembly `FutureDigitalPlus.UIKit.DesignSystem`) |
| `Assets/DesignSystem/Editor/...` | `Editor/DesignSystem/...` (assembly `FutureDigitalPlus.UIKit.DesignSystem.Editor`) |
| `Assets/DesignSystem/Resources/...` | `Runtime/DesignSystem/Resources/FutureDigitalPlus/UIKit/DesignSystem/...` (the guides here already use the renamed paths: `resource("FutureDigitalPlus/UIKit/DesignSystem/Icons/check")`, `Resources.Load("FutureDigitalPlus/UIKit/DesignSystem/Themes/Dark")`) |
| `project://database/Assets/DesignSystem/Resources/UI/Styles/DesignSystem/DesignSystem.uss` in a `<Style>` tag | `project://database/Packages/com.futuredigitalplus.uikit/Runtime/DesignSystem/Resources/FutureDigitalPlus/UIKit/DesignSystem/Styles/DesignSystem.uss` (classic Assets install: `project://database/Assets/FutureDigitalPlus/UIKit/Runtime/...`; or keep the upstream form with `guid=2fe121bab235f2a4f8cbc07737f87fe2`, or the form without a path `project://database/?fileID=7433441132597879392&guid=2fe121bab235f2a4f8cbc07737f87fe2&type=3`; Unity resolves by GUID, and only the last form is free of a "was moved" message in both layouts) |
| `Assets/Showcase/...` | Not shipped. See `Samples~/DesignSystemShowcase` |
| `Tools/Build/...` | Not shipped |
| `Include "Packages/com.sinanata.designsystem/..."` in shaders | `Packages/com.futuredigitalplus.uikit/Runtime/DesignSystem/Resources/FutureDigitalPlus/UIKit/DesignSystem/Shaders/DsFx.cginc` (classic Assets install: `Assets/FutureDigitalPlus/UIKit/Runtime/...`) |
| Menu `Tools > Future Digital Plus UI Kit > Design System > ...` | Same menus |

## The five things you use first

```csharp
using FutureDigitalPlus.UIKit.DesignSystem.Theme.Applier;   // ThemeRuntime
using FutureDigitalPlus.UIKit.DesignSystem.Theme.Data;      // ThemeData
using FutureDigitalPlus.UIKit.DesignSystem.Behaviour;       // DesignSystemBehaviourBase<T>
using UnityEngine;
using UnityEngine.UIElements;

// 1. The base stylesheet (tokens + every ds-* component sheet). On the PANEL root (see "Where the theme goes" below).
var baseSheet = Resources.Load<StyleSheet>("FutureDigitalPlus/UIKit/DesignSystem/Styles/DesignSystem");
root.parent.styleSheets.Add(baseSheet);

// 2. Classes in UXML or code: ds-root (top element), ds-card, ds-h2, ds-body-2, ds-btn ds-btn--primary, ds-icon--trophy ...
var button = new Button { text = "Play" };
button.AddToClassList("ds-btn");
button.AddToClassList("ds-btn--primary");

// 3. A theme (a ThemeData asset, baked into a stylesheet). Dark is the base; Light and Dark ship in Resources.
var light = Resources.Load<ThemeData>("FutureDigitalPlus/UIKit/DesignSystem/Themes/Light");
ThemeRuntime.Apply(root, light);          // apply or swap
ThemeRuntime.Apply(root, null);           // back to the base tokens (same as ThemeRuntime.Clear(root))
var worn = ThemeRuntime.WornBy(root);     // which theme this root wears, or null

// 4. Runtime behaviours (toggle knob, tabs, drag and drop, dropdown placement) over a tree built in code.
DesignSystemBehaviourBase<UIDocument>.EnsureAll(root);   // idempotent; the UIDocument behaviour does it for you too

// 5. The touch-size tier: add the class "fdp-input-touch" to the root (see MOBILE.md). The UIKit facade decides it automatically.
root.EnableInClassList("fdp-input-touch", Application.isMobilePlatform);
```

The `UIKitHost` component or `UIKitInstaller.Install` does 1, 3, 4 and 5 in one call (see the UIKit guide).
Classes and tokens are listed in COMPONENTS.md and ARCHITECTURE.md; icons are `ds-icon--<name>` (ICONS.md).

## Things that differ from the upstream project

- The package is read-only when installed from a registry or git URL. Create your own themes by duplicating
  `Dark.asset` into `Assets/` (the Theme Configurator and Rebake All Themes work on those). Regenerating the
  two built-in themes (Tools > Future Digital Plus UI Kit > Design System > Generate Built-in Themes) only works when the package is embedded or
  installed from a local path.
- **Icon import type.** ICONS.md says the SVGs import as `Texture` (`svgType: 3`). On Unity 6000.3.25f1 the importer
  setting `svgType: 3` produces `VectorImage` assets, and `Resources.Load<Texture2D>("FutureDigitalPlus/UIKit/DesignSystem/Icons/...")` returns null
  (`Resources.Load<VectorImage>` works). USS `background-image: resource("FutureDigitalPlus/UIKit/DesignSystem/Icons/<name>")` works either way. The
  `.svg.meta` files are the upstream ones and were not changed (the files moved, their GUIDs did not).
- No fonts are bundled. Use Tools > Future Digital Plus UI Kit > Design System > Google Fonts to import the families you want into your project.
- The material pipeline in MATERIALS.md needs Unity 6000.5 or newer and compiles out on older versions.
- **Where the theme goes (checked on Unity 6000.3.25f1).** The guides say a theme added last to the same element as the base
  tokens wins ties by sheet order. In practice the base sheet's `:root` tokens beat a class-scoped theme (`.theme-light`) on the
  same element or on an ancestor of it. Put the base sheet on the panel root (`root.parent.styleSheets.Add(...)`) and apply the
  theme to the document root (`ThemeRuntime.Apply(root, theme)`), or use the UIKit facade, which does this. Do not add the
  design system sheet with a `<Style>` tag in the UXML of a screen you want to theme this way. Details:
  `Runtime/DesignSystem/SOURCE.md`, section "Behaviour on Unity 6000.3".
- The showcase scene, the world-space gallery and the Codigrate theme provider mentioned in the guides are not
  part of this package.

Everything about what was copied, left out and changed is in `Runtime/DesignSystem/SOURCE.md`.
