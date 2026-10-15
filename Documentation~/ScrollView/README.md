# ScrollView

[FancyScrollView](https://github.com/setchi/FancyScrollView) for **UI Toolkit**: scroll views whose cells place, scale and fade themselves
by their position in the viewport (carousels, wheels, cover flows), with infinite looping, snapping, inertia and a rubber band, plus a
ScrollRect-style list and a grid. All three build only the cells in view and reuse them while scrolling: a list of 1,000 rows keeps about
15 row cells.

| | |
|---|---|
| Assemblies | `FutureDigitalPlus.UIKit.ScrollView.Core` (pure C#, no `UnityEngine`, tested by dotnet: `ScrollerModel`, `CellLayout`, `ScrollRectLayout`, `Ease`, the enums) and `FutureDigitalPlus.UIKit.ScrollView` (the views, cells, contexts and `FancyScroller`) |
| Namespace | `FutureDigitalPlus.UIKit.ScrollView` |
| Depends on | nothing else in the kit |
| Does not need | uGUI, the design system, Foundation, Interaction, Components |
| Styles | none: cells style themselves; USS classes start with `fdp-fancy-` |
| Upstream | setchi/FancyScrollView, MIT; file map and every change in [Runtime/ScrollView/SOURCE.md](../../Runtime/ScrollView/SOURCE.md) |
| Sample | [Scroll View Basics](../../Samples~/ScrollViewBasics/README.md) |

It is a port, not a wrapper: upstream is built on uGUI (`MonoBehaviour` prefabs, `RectTransform`, `EventSystem`), and this module
contains no uGUI. The API and the behaviour are kept, so the upstream examples and documentation still apply; the differences are listed below.

Add `FutureDigitalPlus.UIKit.ScrollView` (and, for `Ease` and the enums, `FutureDigitalPlus.UIKit.ScrollView.Core`) to your assembly
definition. To try it: **Window > Package Manager > FDP UI Kit > Samples > Scroll View Basics > Import**, add the component
**FDP UI Kit > Scroll View Basics Sample** to an empty GameObject and press Play.

> **Status.** Moved into the kit from the stand-alone package `com.futuredigitalplus.scrollview` 0.1.0 (names in the "Rename record" of
> `SOURCE.md`). 39 EditMode tests of Core (`Tests/Pure/ScrollView`, also `dotnet test Tools~/PureTests -p:Module=ScrollView`), 18 PlayMode
> tests on real panels (`Tests/Runtime/ScrollView`: pooling, positions, jump, drag, tap on a button in a cell, click-to-scroll in a snapping
> view, wheel, scrollbar, padding, resize, release outside the view, loop, grid) and 6 sample smoke tests that click, drag, scroll and press
> the sample's buttons (`Tools~/SamplesSmoke/ScrollViewBasicsSmokeTests.cs`). Not yet run on a device.
## Which view?

| You want | Derive from | Cells derive from |
|---|---|---|
| a carousel, a wheel, a cover flow; cells that animate by position; looping; snapping | `FancyScrollView<TItem>` + a `FancyScroller` | `FancyCell<TItem>` |
| a long list of rows of one height (chat, leaderboard, shop) | `FancyScrollRect<TItem>` | `FancyScrollRectCell<TItem>` |
| a grid of tiles of one size (inventory, level select) | `FancyGridView<TItem>` | `FancyGridViewCell<TItem>` |

Each has a `<TItem, TContext>` form when cells and view need to share state (a selected index, a click callback).

## A carousel

```csharp
using FutureDigitalPlus.UIKit.ScrollView;
using UnityEngine;
using UnityEngine.UIElements;

public sealed class LevelCarousel : FancyScrollView<string>
{
    readonly FancyScroller scroller;

    public LevelCarousel()
    {
        style.flexGrow = 1;
        CellInterval = 0.2f;   // five cells across
        Loop = true;
        scroller = new FancyScroller(this, ScrollDirection.Horizontal)
        {
            MovementType = MovementType.Unrestricted,  // no ends when looping
            ScrollSensitivity = 5f,                    // one viewport of drag moves five items
        };
        scroller.ValueChanged += UpdatePosition;
    }

    public void Show(string[] levels)
    {
        UpdateContents(levels);
        scroller.SetTotalCount(levels.Length);
    }

    protected override FancyCell<string, NullContext> CreateCell() => new Card();

    sealed class Card : FancyCell<string>
    {
        readonly Label label = new Label();
        public Card() { style.width = 100; style.height = 130; style.top = Length.Percent(50); Add(label); }

        public override void UpdateContent(string level) => label.text = level;

        // position: 0 at the left edge of the viewport, 0.5 in the middle, 1 at the right edge.
        public override void UpdatePosition(float position)
        {
            style.left = Length.Percent(position * 100f);
            style.translate = new Translate(Length.Percent(-50), Length.Percent(-50));
            var s = Mathf.Lerp(1f, 0.6f, Mathf.Abs(position - 0.5f) * 2f);
            style.scale = new Scale(new Vector2(s, s));
        }
    }
}
```

`scroller.SelectionChanged` tells you which item comes to rest in the middle; `scroller.ScrollTo(index, 0.35f, Ease.OutCubic)` moves to one.

## A list

`Entry` stands for your row data; the usings are the same as above plus `System.Collections.Generic`.

```csharp
public sealed class Leaderboard : FancyScrollRect<Entry>
{
    public Leaderboard() { style.flexGrow = 1; Spacing = 4; PaddingHead = 8; }
    protected override float CellSize => 48;   // every row is 48 high
    protected override FancyCell<Entry, FancyScrollRectContext> CreateCell() => new Row();
    public void Show(IList<Entry> entries) => UpdateContents(entries);
    public void Focus(int index) => ScrollTo(index, 0.4f, Ease.OutCubic, alignment: 0.5f);
}

sealed class Row : FancyScrollRectCell<Entry>
{
    public Row() { style.width = Length.Percent(100); style.height = 48; }
    public override void UpdateContent(Entry e) { /* fill labels */ }
}
```

A scrollbar: create a UI Toolkit `Scroller`, add it to the view, and assign it to `view.Scroller.Scrollbar`. A grid works the same way with
`FancyGridView<T>`: set `StartAxisCellCount`, `GridCellSize`, `StartAxisSpacing`, and call `Setup(() => new Tile())` in `SetupCellTemplate()`.

## Things to know

- **Cells place themselves.** The view creates cells, fills them (`UpdateContent`) and tells them where they are (`UpdatePosition`); it
  never moves them. ScrollRect and grid cells already move themselves with `style.translate`; make them `CellSize` long along the scroll
  axis (grid cells `GridCellSize`). For a `FancyScrollView` cell, prefer percentages (`style.left = Length.Percent(...)`), so a resized
  view needs no relayout.
- **Drag versus tap.** A press becomes a drag after 8 panel units (`FancyScroller.DragThreshold`). Until then, buttons in cells get their
  click; once it is a drag, the scroller takes the pointer and the button does not click. A click handler that calls `ScrollTo` is not
  undone by the snap of the release.
- **Hidden views do not tick.** `FancyScroller` ticks every frame while its viewport is in a panel and shown. When the viewport or an
  ancestor gets `display: none` (a screen under another one), the layout empties it and the scroller pauses its tick; it resumes when the
  viewport is laid out again (shown), takes input, or is told to `ScrollTo`. An animation or inertia in progress waits meanwhile and goes on
  from where it was, as upstream's `Update` stops on an inactive GameObject. `FancyScroller.IsTicking` tells. `visibility: hidden` keeps the
  layout and keeps ticking.
- **Viewport.** The viewport is the view's padding box: give the view a border if you like, but use `PaddingHead`/`PaddingTail` (or a
  wrapper element) for space around the cells; the view's own padding does not inset them. The scroll direction is set in the constructor.
- **Positions.** Scroller positions are in items: 0 is the first, `count - 1` the last. On a `FancyScrollRect`, `Scroller.Position` is in
  scroller units; convert with `ToScrollerPosition`, or use `JumpTo` / `ScrollTo` with an alignment (0 top, 0.5 centre, 1 bottom).
- **Not supported**, as upstream: loop and snapping on `FancyScrollRect` and `FancyGridView` (they log an error); cells of different sizes.
- **Protected API, as upstream.** `UpdateContents`, `JumpTo`, `ScrollTo`, `Refresh` and `Relayout` are protected: your subclass decides
  what it makes public. `FancyGridView.UpdateContents(IList<T>)` and `FancyScrollRect.Scroller` are public.
- **UXML.** The views are generic, so UI Builder cannot create them directly. Give your concrete subclass `[UxmlElement]` and make it `partial`.
- **Names.** Game code in its own namespaces is not affected: `using FutureDigitalPlus.UIKit.ScrollView;` next to
  `using UnityEngine.UIElements;` is fine. Inside the kit, `Runtime` and `Editor` code (and tests and samples whose
  assembly references this module) must write UI Toolkit's class as `UnityEngine.UIElements.ScrollView`; a bare `ScrollView` is CS0118
  (details in `Runtime/ScrollView/SOURCE.md`, "Rename record"). USS classes start with `fdp-fancy-`.

## Coming from upstream FancyScrollView

| Upstream (uGUI) | Here (UI Toolkit) |
|---|---|
| `MonoBehaviour` view and cells, cell prefab | `VisualElement` view and cells; override `CreateCell()` |
| `Scroller` component in the Inspector | `new FancyScroller(viewElement, direction)`; `FancyScrollRect` creates its own (`Scroller` property) |
| `scroller.OnValueChanged(UpdatePosition)` | `scroller.ValueChanged += UpdatePosition` |
| `scroller.OnSelectionChanged(cb)` | `scroller.SelectionChanged += cb` |
| `EasingCore.Ease`, `Easing.Get` | `Ease`, `EaseFunctions.Get` |
| serialized `cellInterval`, `scrollOffset`, `loop` | properties `CellInterval`, `ScrollOffset`, `Loop` (or the protected fields in a constructor) |
| `transform.localPosition` in a cell | `style.translate` (ScrollRect and grid cells do it for you) |
| Animator driven by the position | set `style.left`/`translate`/`scale`/`opacity` from the position in `UpdatePosition` |
| `Setup<CellGroup>(cellPrefab)` in a grid | `Setup(() => new MyCell())` |
| uGUI `Scrollbar` | UI Toolkit `Scroller` in `FancyScroller.Scrollbar` |

## Layout in the kit

| Folder | Assembly | What |
|---|---|---|
| `Runtime/ScrollView/Core` | `FutureDigitalPlus.UIKit.ScrollView.Core` | engine-free: `ScrollerModel`, `CellLayout`, `ScrollRectLayout`, `Ease`, enums |
| `Runtime/ScrollView/Views` | `FutureDigitalPlus.UIKit.ScrollView` | UI Toolkit: the views, cells, contexts and `FancyScroller` |
| `Tests/Pure/ScrollView` | `FutureDigitalPlus.UIKit.ScrollView.Core.Tests` | EditMode tests of Core |
| `Tests/Runtime/ScrollView` | `FutureDigitalPlus.UIKit.ScrollView.Tests` | PlayMode tests on a real panel |
| `Samples~/ScrollViewBasics` | `FutureDigitalPlus.UIKit.Samples.ScrollViewBasics` | carousel, list and grid on one component |

## Licence

The ported FancyScrollView code is MIT (c) 2020 setchi; see section 5 of [THIRD_PARTY_NOTICES.md](../../THIRD_PARTY_NOTICES.md).
