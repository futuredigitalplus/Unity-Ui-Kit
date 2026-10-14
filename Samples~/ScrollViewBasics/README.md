# Scroll View Basics (sample of the ScrollView module)

The three kinds of view on one screen, built in code:

- **Carousel** (`Carousel.cs`): a `FancyScrollView` with a `FancyScroller`. Twelve cards loop forever, snap to the middle, and shrink and
  fade away from it. A click on a card scrolls it to the middle; the label above shows the selection. Cards place themselves in percent
  of the viewport, so the carousel needs no relayout when the window is resized. Shows the upstream Basic, FocusOn and InfiniteScroll
  examples in one view, including a context shared by view and cells (`CarouselContext`).
- **List** (`RowList.cs`): a `FancyScrollRect` of 1,000 rows with spacing, padding and a UI Toolkit `Scroller` as scrollbar. The buttons
  call `ScrollTo` with alignment 0 (top) and 1 (bottom), and `JumpTo`. About 15 row cells exist at any time.
- **Grid** (`ColorGrid.cs`): a `FancyGridView` of 400 colour tiles, four per row.

## Run it

1. Window > Package Manager > FDP UI Kit > Samples > Scroll View Basics > Import.
2. In an empty scene, add the component **FDP UI Kit > Scroll View Basics Sample** to an empty GameObject.
3. Press Play. Drag, flick, use the mouse wheel, or click.

`ScrollViewBasicsSample.cs` creates its own PanelSettings (scale with screen size, 1280 x 720) and UIDocument. Its runtime theme is
`Resources/FutureDigitalPlus/UIKit/Samples/ScrollViewBasics/RuntimeTheme.tss`, under the kit's own prefix like every sample's
resources (Unity merges all `Resources` folders into one namespace).

## Automated check

`Tools~/SamplesSmoke/ScrollViewBasicsSmokeTests.cs` in the kit runs this sample and clicks, drags, scrolls and presses its buttons;
see `Tools~/SamplesSmoke/README.md`. The module guide is `Documentation~/ScrollView/README.md`.
