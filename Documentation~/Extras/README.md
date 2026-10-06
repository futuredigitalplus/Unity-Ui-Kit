# Extras module

Small UI Toolkit (UIElements) controls from plyoung/UIElements (Unlicense, public domain), made to compile and run on
Unity 6. Upstream: https://github.com/plyoung/UIElements at commit `8fac81337c5929010c84d8038160fe3406b7137c`.
What was copied, what was left out and every local change is listed in `Runtime/Extras/SOURCE.md`.

The upstream author describes these as "not all plug-and-play but ... examples you can adopt for your own needs".
They are kept close to upstream so they can be diffed against it; expect to restyle them for your game.

| Feature | Guide | Types | Needs |
|---|---|---|---|
| Aspect ratio padding | [AspectRatioPadding.md](AspectRatioPadding.md) | `AspectRatioPadding` | nothing |
| Blur / glass panel | [BlurOrGlassEffect.md](BlurOrGlassEffect.md) | `BlurPanel`, `BlurSource` | a blurred `Texture`: you supply one, or make it with `BlurSource` (any render pipeline) |
| Colour field and picker | [ColorFieldAndPicker.md](ColorFieldAndPicker.md) | `ColorField`, `ColorPopup`, `Slider2D`, `DragManipulator` | popup panel |
| Popup panels | [PopupPanel.md](PopupPanel.md) | `PopupPanel`, `PopupMessage`, `PopupTextField` | nothing |
| Tooltip | [Tooltip.md](Tooltip.md) | `Tooltip`, `TooltipManipulator` | nothing (hover, keyboard or gamepad focus, or a long press) |

## Using the module

- Assembly: `FutureDigitalPlus.UIKit.Extras` (Runtime/Extras). It is auto referenced and has no dependency on any other
  module or on the game. It needs only the built in UI Toolkit modules.
- Namespace: upstream's generic `Game.UI` became `FutureDigitalPlus.UIKit.Extras` (see `Tools~/rename/rename-map.md`). If a game
  already has its own `FutureDigitalPlus.UIKit.Extras` types named `Tooltip`, `PopupPanel`, `ColorField`, `BlurPanel`, `Slider2D` and so on,
  reference these with an alias: `using ExtrasTooltip = FutureDigitalPlus.UIKit.Extras.Tooltip;`. In UXML the elements are written with the
  full name, for example `<FutureDigitalPlus.UIKit.Extras.Tooltip />`.
- Style sheets: every control loads its own style sheet in its constructor with `Resources.Load`. The files live in
  `Runtime/Extras/<Feature>/Resources/FutureDigitalPlus/UIKit/Extras/Styles/` and are addressed as `FutureDigitalPlus/UIKit/Extras/Styles/<Name>StyleSheet`
  (`TooltipStyleSheet`, `PopupPanelStyleSheet`, `PopupMessageStyleSheet`, `PopupTextFieldStyleSheet`,
  `ColorFieldStyleSheet`, `ColorPopupStyleSheet`, `Slider2DStyleSheet`). Do not create assets with the same
  `FutureDigitalPlus/UIKit/Extras/Styles/...` path in your game: Resources keeps only one of two files with the same path. The path is a
  string in the code (moved from upstream's `GameUI/Styles` by the rename).
- Colours: the style sheets take their colours from the DesignSystem tokens when the design system's base sheet is on the panel
  (`--fdp-color-surface`, `-surface-elev`, `-surface-hover`, `-primary`, `-primary-press`, `-text-primary`, `-text-secondary`, `-overlay`), and fall back to the upstream demo palette
  when it is not, so the controls always have colours and follow the active theme. Each declaration is
  `var(--fdp-color-back-drk, var(--fdp-color-surface, rgb(40, 35, 35)))`: the first name is an optional override that you may define on a parent `:root`
  (`--fdp-color-back-drk`, `-back-mid`, `-back-lit`, `-primary-drk`, `-primary-mid`, `-primary-lit`, `-txt-drk`, `-txt-mid`, `--fdp-color-screenfill-translucent`).
  `Samples~/Extras` has two ready made override sheets (`ExtrasThemeVariables.uss`, the upstream palette; `ExtrasDesignSystemBridge.uss`, the token mapping).
  Headings use `-unity-font-style: bold` (upstream pointed `--fdp-font-bold` at a font asset of its demo; the variable is no longer read).
- Input: the tooltip, the popups and the colour picker work with keyboard and gamepad (see the three guides). The root classes that tell them which device is in use
  (`fdp-input-nav`) are written by the Interaction module's `InputProfile`; without it nothing changes for mouse users and focus never opens a tooltip.
- UXML: all controls register `UxmlFactory` and `UxmlTraits`, which Unity 6 marks as deprecated (still working). The
  compiler warning is suppressed at those lines. Moving to `[UxmlElement]` is a possible later step, see SOURCE.md.

## The five things you use first

```csharp
using FutureDigitalPlus.UIKit.Extras;
using UnityEngine;
using UnityEngine.UIElements;

// root = the document root. Add popups and the tooltip LAST so they paint over everything else.

// 1. Tooltip: one element per document, one manipulator per element that has a tooltip text ("B:" below, "T:" above, "L:", "R:").
var tooltip = new Tooltip();
root.Add(tooltip);
saveButton.tooltip = "T:Save the game";
saveButton.AddManipulator(new TooltipManipulator(tooltip));

// 2. Message and text popups (modal, full screen overlay).
var message = new PopupMessage();
root.Add(message);
message.Show("Heading", "Are you sure?", "Yes", "No",
    () => { message.Hide(); /* accepted */ },
    () => message.Hide());                      // PopupMessage does not hide itself

// 3. Colour field with its picker popup.
var popup = new ColorPopup();
root.Add(popup);
var field = new ColorField { label = "Fill" };
field.ColorPopup = popup;
field.RegisterValueChangedCallback(evt => Debug.Log(evt.newValue));
panel.Add(field);

// 4. Aspect ratio padding: wrap the part of the screen that must keep a 4:3 design on wider displays (UXML: width="4" height="3").
//    <FutureDigitalPlus.UIKit.Extras.AspectRatioPadding width="4" height="3"> ...your screen... </FutureDigitalPlus.UIKit.Extras.AspectRatioPadding>

// 5. Blur panel: an Image that shows the part of a texture that lies behind it. You supply the blurred texture, or make one with BlurSource.Blur(texture, downsample, iterations).
var glass = new BlurPanel();
glass.SetImage(blurredTexture);
```

None of these needs a variable of its own: with the DesignSystem's base sheet on the panel they follow the active theme, and without it they use the upstream demo palette (see "Colours" above).
The names in the `var(--fdp-color-back-drk, ...)` chains are optional overrides. To change a colour for one control, define the name on that control, for example
`.fdp-tooltip { --fdp-color-back-drk: #101820; }` (the Quick Start sample's `QuickStartExtras.uss` does this). `Samples~/Extras/ExtrasThemeVariables.uss` (the upstream palette) and
`ExtrasDesignSystemBridge.uss` (the same names mapped onto the design system tokens) define all of them at once on `:root`; use one of the two, not both. Whether names mapped on `:root` follow a runtime Light/Dark switch (the theme sits on a descendant of the root, see `GettingStarted.md`, "Why no `<Style>`") has not been tested; scoped to the control, as the Quick Start sample does, they follow the theme.

## Not included

The upstream `_misc` folder holds one extension method (`GetDocumentRoot`, which is included) and a set of example
style sheets, which are in `Samples~/Extras/UpstreamMiscStylesheets` for reference. The URP blur render feature is in
`Samples~/Extras/BlurUrpLegacy~` for reference, with the reasons in the blur guide.

## Tests

- EditMode: `Tests/Editor/Extras` (construction, Resources style sheets, clamping, popup content).
- PlayMode: `Tests/Runtime/Extras` (layout dependent behaviour: aspect ratio margins, tooltip placement, popup and
  colour picker callbacks). The panel renders into a 1600x900 RenderTexture so results do not depend on the Game view.
