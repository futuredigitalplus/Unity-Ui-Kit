# ScrollView: source record

- Upstream: setchi/FancyScrollView
- URL: https://github.com/setchi/FancyScrollView
- Commit: `8fce45dd69f36813ba5d4894f9adb0905e4ce465` (2022-07-08, upstream project on Unity 2019.4)
- Licence: MIT. File kept in this folder as `LICENSE.md` (upstream `LICENSE`, byte for byte); full text also in `../../THIRD_PARTY_NOTICES.md` (section 5).
- Guide: `../../Documentation~/ScrollView/README.md`.
- What was taken: the 17 C# files of `Assets/FancyScrollView/Sources/Runtime/` (the asmdefs are new). Not taken: `Sources/Editor/ScrollerEditor.cs` (an inspector for the
  uGUI `Scroller` component, which no longer exists), `Examples/**` (uGUI scenes, prefabs, animator controllers, shaders and textures;
  `Samples~/ScrollViewBasics` shows in UI Toolkit code what the Basic, FocusOn, InfiniteScroll, ScrollRect and GridView examples show), `ProjectSettings/**`.

## Why it is a port, not a copy

Upstream is built on uGUI: cells are `MonoBehaviour` prefabs moved by `RectTransform`, input comes from the `EventSystem`, time from
`Update`. This module targets UI Toolkit, like the rest of the kit, so the classes are `VisualElement`s and the logic that did
not depend on uGUI was moved, unchanged, into an engine-free `Core` assembly that is unit tested outside Unity.

## File map

| Upstream (`Sources/Runtime/`) | Package (`Runtime/ScrollView/`) | Notes |
|---|---|---|
| `Core/FancyCell.cs` | `Views/FancyCell.cs` | `VisualElement`; visibility is `style.display` |
| `Core/FancyScrollView.cs` | `Views/FancyScrollView.cs`, `Core/CellLayout.cs` | `CellPrefab` became `CreateCell()`; fields became properties |
| `Scroller/Scroller.cs` | `Core/ScrollerModel.cs`, `Views/FancyScroller.cs` | logic in the model, UI Toolkit input in the view |
| `Scroller/EasingCore.cs` | `Core/Ease.cs` | `Easing` renamed `EaseFunctions`, `EasingFunction` renamed `EaseFunction` |
| `Scroller/ScrollDirection.cs`, `MovementType.cs`, `MovementDirection.cs` | `Core/ScrollEnums.cs` | `MovementType` no longer aliases uGUI |
| `ScrollRect/FancyScrollRect.cs` | `Views/FancyScrollRect.cs`, `Core/ScrollRectLayout.cs` | owns its `FancyScroller` |
| `ScrollRect/FancyScrollRectCell.cs` | `Views/FancyScrollRectCell.cs` | moves by `style.translate` |
| `ScrollRect/FancyScrollRectContext.cs`, `IFancyScrollRectContext.cs` | `Views/Contexts.cs` | two new members (below) |
| `GridView/FancyGridView.cs` | `Views/FancyGridView.cs` | `Setup` takes a cell factory |
| `GridView/FancyCellGroup.cs` | `Views/FancyCellGroup.cs` | creates cells with the factory |
| `GridView/FancyGridViewCell.cs` | `Views/FancyGridViewCell.cs` | moves by `style.translate` |
| `GridView/FancyGridViewContext.cs`, `IFancyGridViewContext.cs`, `IFancyCellGroupContext.cs` | `Views/Contexts.cs` | `CellTemplate` became `CreateCell` |

## Changes, by file

Each file also says this in its `FutureDigitalPlus:` header comment.

- **All files.** Namespace `FutureDigitalPlus.UIKit.ScrollView` (was `FancyScrollView` and `EasingCore`); doc comments translated from Japanese to English.
- **ScrollerModel / FancyScroller.** Input is fed in by the view: `PointerDown`, `PointerUp`, `BeginDrag`, `Drag(distance)`, `EndDrag`,
  `Scroll(delta)`, `SetNormalizedPosition`; time by `Tick(deltaTime)` (was `Update` with `Time.unscaledTime`). `OnValueChanged(callback)`
  and `OnSelectionChanged(callback)` became the events `ValueChanged` and `SelectionChanged`. The snap settings, a private serialized
  class upstream, are properties (`SnapEnabled`, `SnapVelocityThreshold`, `SnapDuration`, `SnapEase`). New in the view: a drag starts after
  `DragThreshold` (8 panel units) and then captures the pointer, so a tap still clicks a button in a cell; while a child holds the pointer
  the scroller listens on that child (UI Toolkit sends captured events to the capturing element only); a press released outside the view
  is noticed on the panel root; one wheel unit scrolls `WheelStep` panel units (18, like UI Toolkit's `ScrollView`; upstream moved one
  screen point per wheel unit). The scrollbar is a UI Toolkit `Scroller`. Guards: `Tick` ignores a zero or negative step (upstream
  divided by it); `Drag` and `Scroll` ignore input while the viewport has no size (before the first layout); the first scheduled tick
  after the view is attached is skipped (it measures the time spent detached). Also new in the view: a release snaps only when nothing
  started an animation during the press (`CancelPress` otherwise), so a click on a cell that calls `ScrollTo` wins, as it did upstream
  where uGUI sent the click after the scroller's pointer-up; a move with no button pressed ends a press whose release was lost; only the
  primary button's release ends a press; if a relayout sets the position during a drag, the drag continues from the pointer (upstream's
  drag stopped until the next press); the viewport is the element's padding box, where the cells are (its padding does not inset them);
  `ScrollDirection` is fixed at construction, as upstream; `Dispose` ends a press in progress. `ScrollMath.Approximately` uses the
  smallest normal float as epsilon, which is `Mathf.Epsilon` on CPUs that flush denormals to zero (ARM phones); `float.Epsilon` would
  read as 0 there and make an idle scroller raise events every frame. `EndDrag` snaps when the drag left no velocity at all (released in
  the frame it moved, after holding still for seconds, or with `Inertia` off); upstream snapped only from inertia and left such a
  release between two items.
- **FancyScrollView.** `cellContainer` is created by the view (`CellContainer`, fills the view, ignores the pointer); the view clips
  (`overflow: hidden`). Setting `CellInterval`, `ScrollOffset` or `Loop` relayouts at once (upstream checked in an editor-only `LateUpdate`).
  `CreateCell()` returning null throws `InvalidOperationException` (upstream: `MissingComponentException` for a prefab without the cell).
- **FancyScrollRect.** Relayouts when its length along the scroll axis changes (`GeometryChangedEvent`), since UI Toolkit lays out after
  construction; a change across the axis only moves the cells, so a fling or animation keeps running. The checks
  of `OnValidate` (no loop, no snap, no unrestricted movement) run when the scroller is refreshed and log the same errors; `Loop` is
  always false. `Scroller` is public. `ScrollRectLayout` guards against a cell size plus spacing of 0 or less and returns 0 from
  `ToScrollerPosition` when the content fits the viewport (upstream divided by zero there; the value was not used).
- **FancyScrollRectCell, FancyGridViewCell.** Cells move with `style.translate` from the viewport's top-left corner. Upstream moved
  `transform.localPosition` from the viewport's centre with y up; `localPosition` passed to `UpdatePosition(normalized, local)` keeps the
  upstream meaning. A ScrollRect cell starts at the viewport's edge across the axis (upstream: centred); give it `width: 100%`.
- **Contexts.** `IFancyScrollRectContext` adds `GetViewportSize` and `GetScrollAxisCellSize` (a cell needs them to place itself from the
  corner). `IFancyCellGroupContext.CellTemplate` (a `GameObject`) became `CreateCell` (a `Func<VisualElement>`).
- **FancyGridView.** `Setup<TGroup>(cellPrefab)` became `Setup<TGroup>(Func<FancyCell> createCell)`, plus `Setup(createCell)` with the
  default group; `DefaultCellGroup` is no longer abstract. The fields became `StartAxisSpacing`, `StartAxisCellCount` (fixed once cells
  exist, upstream broke silently) and `GridCellSize` (upstream `cellSize`, renamed because `CellSize` is the scroll-axis length).
- **FancyCellGroup.** Fills the view and ignores the pointer; a factory that returns the wrong element type throws with the type names.

Behaviour that is deliberately unchanged: the position arithmetic (`CellLayout`, `ScrollRectLayout`), every branch of the scroller's
update, drag, wheel and snap logic, the selection events, and upstream quirks such as the scrollbar shrink formula and the
`Mathf.Clamp` semantics when the item count is 0.

## Rename record

Done by hand when the module moved into the kit (it was the stand-alone package `com.futuredigitalplus.scrollview` 0.1.0 before); `Tools~/rename/rename.py` does not cover it.
The namespace `FutureDigitalPlus.UIKit.ScrollView` is an accepted name hazard (`Tests/Editor/Lint/LintAllowList.cs`). Where it is visible, code in a
`FutureDigitalPlus.UIKit.*` namespace that means UI Toolkit's class must write `UnityEngine.UIElements.ScrollView`: the namespace is found before the
file's `using UnityEngine.UIElements;`, and a bare `ScrollView` is error CS0118 (checked). In Unity it is visible only in assemblies that reference this
module, but a tool that compiles the kit's `Runtime` as one unit (Color Solitaire's `UikitCompile`, and `UikitEditorCompile` for `Editor` on top of it)
sees it in every module. So all `Runtime` and `Editor` code writes the full name (`VirtualGrid`, `DialogService`, `DesignSystemBehaviourBase`,
`ThemeConfiguratorWindow`, `ResponsiveLayoutAudit`); tests and samples need it only when their assembly references the module. A component in such an
assembly cannot have a `[Query]` field of UI Toolkit's `ScrollView`, because the bundled generator writes the short name into the generated partial
class; query that element by hand. Game code in its own namespaces is not affected. The `.meta` GUIDs are those of the stand-alone package.

| Namespace (old) | Namespace (new) |
|---|---|
| `FancyScrollView`, `EasingCore` (upstream), `FutureDigitalPlus.FancyScroll` (stand-alone package) | `FutureDigitalPlus.UIKit.ScrollView` |
| `FutureDigitalPlus.FancyScroll.Tests` | `FutureDigitalPlus.UIKit.ScrollView.Tests` |
| `FutureDigitalPlus.FancyScroll.Samples` | `FutureDigitalPlus.UIKit.Samples.ScrollViewBasics` |

| Assembly (stand-alone package) | Assembly (kit) |
|---|---|
| `FutureDigitalPlus.FancyScroll.Core` | `FutureDigitalPlus.UIKit.ScrollView.Core` |
| `FutureDigitalPlus.FancyScroll` | `FutureDigitalPlus.UIKit.ScrollView` |
| `FutureDigitalPlus.FancyScroll.Core.Tests`, `FutureDigitalPlus.FancyScroll.Tests` | `FutureDigitalPlus.UIKit.ScrollView.Core.Tests`, `FutureDigitalPlus.UIKit.ScrollView.Tests` |
| `FutureDigitalPlus.FancyScroll.Samples.Basics` | `FutureDigitalPlus.UIKit.Samples.ScrollViewBasics` |

The sample lost its scene and its `.meta` files (no kit sample ships them); it is a component for an empty GameObject.
The PlayMode tests load the kit's shared `FutureDigitalPlus/UIKit/Tests/DefaultRuntimeTheme` (their own copy, `FutureDigitalPlus/FancyScroll/Tests/DefaultRuntimeTheme`, was dropped).
USS classes keep the prefix `fdp-fancy-`.
