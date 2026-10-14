# Tools~/SamplesSmoke: does every sample run?

`SamplesSmokeTests.cs` is a PlayMode test per sample (ten tests for the first eleven samples): it adds the sample component to an empty GameObject, waits a few frames, checks that the widgets the sample's README names exist,
lets 30 more frames of the sample's own `Update` run, destroys it and checks that no `UIDocument` is left. The five Components samples that have a layout are built as elements of one document
(`ComponentsSamples_EachComponentBuilds`); the Extras sample is style sheets only and the Addressables sample needs an Addressables group and a player content build (see its README), so both are covered by
compiling and importing them, not by a run. The test framework fails a test on any logged error or exception, so a sample that throws
while it builds or updates fails here. It does not click or press anything.

`ScrollViewBasicsSmokeTests.cs` (six tests) uses the Scroll View Basics sample (ScrollView module) like a player, by sending UI Toolkit pointer and wheel events.
Each test saves a 1280 x 720 screenshot when the environment variable `FDP_SMOKE_OUT` names a folder.

| Test | Checks |
|---|---|
| `Builds_ThreeViews_WithFewCells` | 1,000 rows and 400 tiles use few cells; card 1 starts in the middle |
| `Carousel_ClickACard_ScrollsItToTheMiddle` | a click on card 3 brings it to the middle and selects it |
| `Carousel_Drag_SnapsToAnotherCard` | a flick (one move per frame) ends with another card snapped to the middle, and the label agrees |
| `List_Buttons_JumpAndScroll` | Jump to 500, Bottom (scrollbar at 1), Top (scrollbar at 0) |
| `List_Drag_ScrollsAndMovesTheScrollbar` | a drag on a row scrolls the list and moves the scrollbar |
| `Grid_Wheel_Scrolls` | ten wheel notches scroll past the first four rows |

Samples are not compiled inside the package (`Samples~` is hidden from Unity), so the tests live here and run in a project that has the samples imported:

```
Tools~/SamplesSmoke/run-samples-smoke.sh <unity project>      # copies the samples and these tests into <project>/Assets/UIKitSamplesSmoke
unity test <project> --mode PlayMode --filter "FutureDigitalPlus.UIKit.Samples.Smoke" ...   # see Documentation~/Verification.md for the full command
rm -rf <project>/Assets/UIKitSamplesSmoke <project>/Assets/UIKitSamplesSmoke.meta           # afterwards
```

Use a scratch project (never a game). Package Manager imports a sample into `Assets/Samples/...`; the script does the same copy without the editor. The project needs the package with the modules the
samples use (all of them), and `com.unity.test-framework`. Run it in each variant of `Tools~/Matrix` that applies (Built-in, URP, HDRP, Input System only).

Run it for every variant with `Tools~/Matrix/run-matrix.sh <variant> --samples` (it copies the samples and these tests into the variant project and runs only them).

Not covered: input (keys, gamepad; clicks, drags and the wheel only for Scroll View Basics), the look of a sample (no screenshot comparison), devices.
