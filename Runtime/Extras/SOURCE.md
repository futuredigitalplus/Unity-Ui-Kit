# Extras: source record

- Upstream: plyoung/UIElements
- URL: https://github.com/plyoung/UIElements
- Commit: `8fac81337c5929010c84d8038160fe3406b7137c`
- Licence: Unlicense (public domain dedication). File kept in this folder as `UNLICENSE.md` (upstream file `UNLICENSE`,
  byte for byte); full text also in `../../THIRD_PARTY_NOTICES.md`. The upstream source files carry no copyright
  headers, so there were none to preserve.
- Guides: `../../Documentation~/Extras/` (one file per feature, with the upstream README excerpt for it).

## Layout

Upstream keeps everything under `Scripts/<Feature>/`. Here the `Scripts/` level is dropped because the module folder
already says what it is; each feature keeps its upstream folder name below `Runtime/Extras/`:

| Upstream path | Package path |
|---|---|
| `Scripts/<Feature>/*.cs` | `Runtime/Extras/<Feature>/*.cs` |
| `Scripts/<Feature>/Resources/GameUI/Styles/*.uss` | `Runtime/Extras/<Feature>/Resources/FutureDigitalPlus/UIKit/Extras/Styles/*.uss` |
| `Scripts/_misc/UIElementsExtensions.cs` | `Runtime/Extras/_misc/UIElementsExtensions.cs` |

The folder `PupupPanel` (sic) keeps upstream's spelling for the same reason. Upstream has no `.meta` files for the
scripts and style sheets, so Unity generated them on import (they are in the package, to keep GUIDs stable). There is
no Editor code upstream, so `Editor/Extras/` holds only a `.gitkeep`.

## Assemblies (all new, so they carry the `FutureDigitalPlus.UIKit.` prefix; upstream has none)

| Assembly | Folder | Notes |
|---|---|---|
| `FutureDigitalPlus.UIKit.Extras` | `Runtime/Extras` | root namespace `FutureDigitalPlus.UIKit.Extras` (upstream: `Game.UI`); no references to other assemblies; all platforms; auto referenced |
| `FutureDigitalPlus.UIKit.Extras.Tests` | `Tests/Runtime/Extras` | PlayMode tests (new) |
| `FutureDigitalPlus.UIKit.Extras.Tests.Editor` | `Tests/Editor/Extras` | EditMode tests (new) |

## Copied (all of it compiles on Unity 6000.3.25f1)

| Feature | Files |
|---|---|
| AspectRatioPadding | `AspectRatioPadding.cs` |
| BlurOrGlassEffect | `BlurPanel.cs` (only the UI half; see Excluded) |
| ColorFieldAndPicker | `ColorField.cs`, `ColorPopup.cs`, `Slider2D.cs`, `DragManipulator.cs`, 3 style sheets |
| PupupPanel | `PopupPanel.cs`, `PopupMessage.cs`, `PopupTextField.cs`, 3 style sheets |
| Tooltip | `Tooltip.cs`, `TooltipManipulator.cs`, 1 style sheet |
| _misc | `UIElementsExtensions.cs` (`GetDocumentRoot`, used by `BlurPanel`) |

Copied without changes beyond the rename: `PopupMessage.cs` and `PopupTextField.cs` (apart from the pragma lines in the table
below) and `UIElementsExtensions.cs`. Every other C# file and all style sheets were changed later; see the table (the style sheets
read `var(--fdp-color-...)` tokens now, and carry a `FutureDigitalPlus:` comment on their first line). Upstream's `Sample.uxml` and `_misc/stylesheets` and the
blur shaders are in `Samples~/Extras` (see below).

## Excluded from the compiled module

| Upstream | Where it went | Reason |
|---|---|---|
| `BlurOrGlassEffect/Shaders/BlurRenderFeature.cs`, `.shader`, `BlurRender.mat`, `UnlitGlassBlur.shadergraph` (and their `.meta`) | verbatim (without `.meta`) in `Samples~/Extras/BlurUrpLegacy~/` | Pre Unity 6 URP API (does not run under Render Graph), built in pipeline blit shader, would make the module depend on URP and Shader Graph, cannot be compile checked here (no URP in the test project). A live per-frame blur still needs a Render Graph pass written in the game; a blur captured once is `BlurSource`. Details and the mobile cost judgement: `Documentation~/Extras/BlurOrGlassEffect.md` |
| `_misc/stylesheets/*.uss` (10 files) | in `Samples~/Extras/UpstreamMiscStylesheets/` (verbatim apart from the `--fdp-` variable rename) | Example theme of the upstream demo game; references fonts (`resource('Text/Inter-...')`) and cursor images (`/Assets/GUI/...`) that do not exist in this package, and would compete with the DesignSystem module as a theme. Only the colour variables the Extras style sheets use were extracted (`ExtrasThemeVariables.uss`) |
| `AspectRatioPadding/Sample.uxml` | `Samples~/Extras/AspectRatioPadding.Sample.uxml` | Demo UXML, not part of the runtime |
| `Images/*` | `Documentation~/Extras/Images/` | Used by the guides only |
| `README.md` | split into the per feature guides in `Documentation~/Extras/` | |

Added (new, by Future Digital Plus, not upstream): `BlurOrGlassEffect/BlurSource.cs` and
`BlurOrGlassEffect/Resources/FutureDigitalPlus/UIKit/Extras/Shaders/Kawase.shader` (`Hidden/FutureDigitalPlus/UIKit/Blur/Kawase`): a render
pipeline independent blur (Kawase: four diagonal bilinear taps per pass, `Graphics.Blit`) that makes the texture `BlurPanel.SetImage` shows.
The shader is written for this package: its vertex stage is Unity's stock `appdata_img` pass-through and the Kawase scheme is a published
technique (no code is taken from upstream or from another project; checked in the licence review of 2026-10-06 by comparing it with every
file of the four upstream clones). `BlurPanel.cs` itself is unchanged by this. The shader sits in a `Resources` folder, so it ships in
every player. Also added: the two asmdefs and the tests listed above,
`Samples~/Extras/ExtrasThemeVariables.uss` (upstream palette values reduced to the used variables),
`Samples~/Extras/ExtrasDesignSystemBridge.uss`, `Samples~/Extras/README.md`, and the guides.
The `package.json` `samples` array has the entry "Extras Stylesheets" (`Samples~/Extras`); added in the integration step.
Integration run (Unity 6000.3.25f1): 0 warnings, 13 EditMode and 6 PlayMode tests in the Extras assemblies pass, the cross-module
test in `Tests/Runtime/UIKit` creates a `Tooltip` and `TooltipManipulator` on a themed panel, and the runtime assembly compiles for
iOS, Android and macOS standalone players.

## Local modifications

Every change is marked in the source with a `FutureDigitalPlus:` comment. The code was compiled against Unity
6000.3.25f1's own assemblies outside the editor (see the report) with 0 errors and 0 warnings after these changes
(before them: 28 warnings).

| File | Change | Reason |
|---|---|---|
| `AspectRatioPadding/AspectRatioPadding.cs`, `BlurOrGlassEffect/BlurPanel.cs`, `ColorFieldAndPicker/ColorField.cs`, `ColorPopup.cs`, `Slider2D.cs`, `PupupPanel/PopupPanel.cs`, `PopupMessage.cs`, `PopupTextField.cs`, `Tooltip/Tooltip.cs` | `#pragma warning disable/restore CS0618` around the `UxmlFactory` / `UxmlTraits` declarations | `UxmlFactory`, `UxmlTraits`, `BaseFieldTraits` are deprecated in Unity 6 in favour of `[UxmlElement]` but still work. Suppressing keeps the upstream code diffable. Later step: migrate to `[UxmlElement]` partial classes (then UXML attributes become `[UxmlAttribute]` properties) |
| `BlurOrGlassEffect/BlurPanel.cs` | `UpdateImageRect` returns early while the document root's `worldBound` width or height is not positive | Division by zero produced an infinite/NaN `sourceRect`. Retries on the next `GeometryChangedEvent`. Not verified visually |
| `ColorFieldAndPicker/DragManipulator.cs` | Removed upstream's own copies of the `Clickable` pointer handlers (`RegisterCallbacksOnTarget`, `UnregisterCallbacksFromTarget`, `OnPointerDown/Move/Up/Cancel`, `IsNotMouseEvent`) | Unity 6's `Clickable` has identical protected handlers, so the copies hid them (warning CS0108) and ran twice for touch input. The `ProcessDownEvent` / `ProcessMoveEvent` overrides are kept and are called by the base handlers. Not verified by interactive dragging, see Known limits |
| `ColorFieldAndPicker/DragManipulator.cs` | `#pragma warning disable/restore CS0618` around `evt.PreventDefault()` | `PreventDefault` is obsolete in Unity 6; kept to preserve the upstream behaviour |
| `ColorFieldAndPicker/Slider2D.cs` | `dragElement.transform.position` / `dragBorderElement.transform.position` (read and write) replaced by `style.translate` | `VisualElement.transform` is obsolete in Unity 6 (warning CS0618); `style.translate` is what it maps to. Same pixel offsets |

Later changes (input, platform fixes and the integration run), all marked `FutureDigitalPlus:` in the code; the style sheets carry one
comment on their first line:

| File | Change | Why |
|---|---|---|
| `Tooltip/TooltipManipulator.cs` | Also shows the tooltip on `FocusInEvent` when an ancestor carries `fdp-input-nav` (written by the Interaction module; absent: never), and after a long press with a finger (`LongPressDelay` 500 ms, `LongPressLinger` 1500 ms, 10 px slop); closes on `FocusOutEvent`, pointer up (after the linger) and cancel. New public members: `NavigationClass`, `LongPressDelay`, `LongPressLinger`. The hover path listens to `PointerEnterEvent` and `PointerOutEvent` and acts only for `PointerType.mouse` (it was `MouseEnterEvent` and `MouseOutEvent`); the four pointer callbacks of the long press are registered for the trickle-down phase. | Mouse hover only meant no tooltip for keyboard, gamepad or touch. UI Toolkit also reports the compatibility mouse events for a finger, so a tap showed the tooltip at once and hid it before the long press could act; and a `Button`'s own `Clickable` stops a bubbling `PointerDown`. |
| `Tooltip/Tooltip.cs` | Placement is computed in the local space of the tooltip's parent (`WorldToLocal`, layout sizes) instead of `worldBound`. | `worldBound` is in world units in a world-space panel; the result is the same in a screen panel without scale. |
| `Tooltip/Tooltip.cs`, `Tooltip/TooltipManipulator.cs` (adversarial API review) | `Tooltip.Show` closes the tooltip instead of showing an empty bubble when the target is null or its `tooltip` text is empty. `TooltipManipulator` closes the tooltip when its target leaves the panel while the manipulator had it showing (new private `DetachFromPanelEvent` handler). | A hover on an element whose tooltip text was cleared showed an empty bubble; a target removed under the pointer (a screen change) never received its `PointerOut`, so the tooltip stayed on screen. |
| `PupupPanel/PopupPanel.cs` | While open: focus cannot leave the popup (refocus on `FocusOutEvent`), `Show` remembers the focused element and `Hide` restores it, `NavigationCancelEvent` closes it. New `static Func<Action, IDisposable> BackRegistrar` (default null, reset on subsystem registration) registers the close action with a back dispatcher while open. `Show` retries `mainPanel.Focus()` every 16 ms (at most 20 times) when the first call left the focus outside (new private `HasFocusInside()`). | The overlay blocked the pointer but not keyboard or gamepad navigation, and a touch device has no focused element to receive Cancel (Android back). A popup hidden a moment ago has not resolved its inherited visibility, so the first `Focus()` fails. The module references no dispatcher, so the registrar is a delegate that the facade or the game assigns. |
| `ColorFieldAndPicker/ColorField.cs` | The colour swatch is focusable and opens the picker on `NavigationSubmitEvent`. | Submit raises no `ClickEvent`. |
| `ColorFieldAndPicker/Slider2D.cs` | Focusable. Submit toggles an "editing" state (class `fdp-slider-2d--editing`); while editing, `NavigationMoveEvent` moves the value by `KeyStepFraction` (new property, 0.05; Shift x5); Submit, Cancel and focus loss end editing. Other directions still move focus. | The picker needs all four directions, so it must not consume them all the time or focus could never leave it. |
| `Tooltip`, `PopupPanel`, `ColorField`, `Slider2D` style sheets (`Resources/.../Extras/Styles/*.uss`, 7 files) | Every colour is `var(--fdp-color-back-drk, var(--fdp-color-surface, rgb(...)))`: the upstream demo variable stays as an optional override, the DesignSystem token is next, the upstream demo colour is the last fallback. `-unity-font-definition: var(--fdp-font-bold)` became `-unity-font-style: bold`. A focus and editing rule for the `Slider2D` knob was added. | The sheets read ten variables that nothing in the package declared, so a control had no colours without a sample. |


## Known limits (not changed, listed so nobody is surprised)

- Interactive behaviour (dragging in `Slider2D`, hover in `Tooltip`) has not been run in a player. The EditMode and
  PlayMode tests cover construction, style sheet loading, layout dependent results and button callbacks only.
- `Tooltip` shows on mouse hover, on keyboard or gamepad focus (navigation mode) and after a long press with a finger (see the table above); the long press is covered by tests but has not been tried on a device. See `Documentation~/Extras/Tooltip.md`.
- `BlurPanel` caches the document root on first use and assumes the blur texture covers the full panel. While the root
  has no size it now leaves `sourceRect` unchanged (see the modification table) and retries on the next geometry change.
- `Slider2D.UpdateDraggerPosition` compares the stored translate (which already includes the half knob offset) with the
  value without it, as upstream did, so it rewrites the knob position on most calls. Harmless.
- Upstream's generic `GameUI/Styles/...` Resources paths and `Game.UI` namespace were renamed to `FutureDigitalPlus/UIKit/Extras/Styles/...` and
  `FutureDigitalPlus.UIKit.Extras`, and the 40 bare class names (`tooltip`, `popup-panel`, `color-field`, ...) to `fdp-*`. Type names such as
  `Tooltip` are upstream's; a game that has its own `Tooltip` aliases one of them.
- Style sheets use `var(--fdp-color-...)` custom properties that the host must define; see `Samples~/Extras`.

<!-- rename-record:begin -->
## Rename record

Generated by `Tools~/rename/rename.py` (collision-proofing). Module: Extras. The upstream names survive only in this record.

| Namespace (old) | Namespace (new) |
|---|---|
| `Game.UI` | `FutureDigitalPlus.UIKit.Extras` |

| Resources path (old) | Resources path (new) |
|---|---|
| `GameUI/Styles/` | `FutureDigitalPlus/UIKit/Extras/Styles/` |

Extras classes `tooltip, slider-2d, color-field, color-popup, popup-panel, popup-message, popup-textfield` got the prefix `fdp-`.
<!-- rename-record:end -->
