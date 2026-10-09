# Responsive (sample)

One UI for a phone, a tablet, a foldable, a desktop window and a TV. Modules: Foundation (and the design system it reads).

1. Window > Package Manager > FDP UI Kit > Samples > Responsive > Import.
2. Create an empty scene, add an empty GameObject, add the component `FDP UI Kit > Responsive Sample`.
3. Press Play, then resize the Game view or switch to the Device Simulator.

## What you see

- A status card: the panel size in units, the size class (`Compact`, `Regular`, `Expanded`), the orientation, the input mode, the applied scale and the safe-area insets.
- Four switches: a fake notch (so safe-area handling can be tested on a desktop), touch targets (the `fdp-input-touch` class), the player's UI size (80 to 160 percent), and the scale profile (`CardPortrait`, `DesktopHud`, `Console`, `Web`).
- Six tiles that reflow by size class with plain USS (`Responsive.uss`): three per row when expanded, two when regular, one when compact.
- The scale policy evaluated for six devices that are not plugged in (phone, tablet, foldable, desktop windows, TV).

## What to read

| Topic | Code |
|---|---|
| Scale from screen size and DPI, plus the player's UI size | `PanelScaleDriver(settings, profile)`, `Tick()` from `Update`, `UserScale`, `Settings` assigned to the `UIDocument` |
| Layout classes (USS has no media queries) | the shell writes `fdp-compact/regular/expanded`, `fdp-portrait/landscape`, `fdp-platform-*` on its root; `Responsive.uss` selects on them |
| Safe area | `SafeAreaPanel` around the content; `SafeAreaProvider.SafeAreaSource` replaced by a function that returns a made-up notch |
| Touch tier | `fdp-input-touch` on the document root enlarges design system controls |

## Notes

- The sample does not fake the screen size: resize the window to see the classes change. The device table shows what the same policy gives elsewhere.
- Run in Unity 6000.3.25f1 by `Tools~/SamplesSmoke` (PlayMode: it builds without a logged error, shows the widgets named above, survives 30 frames of its own `Update` and leaves no document behind) in the Built-in pipeline, URP 3D, URP 2D and HDRP, with the Input System package only and with all optional packages; see `Documentation~/CompatibilityMatrix.md`. The test does not click or press anything, and nothing was run on a phone, a tablet, a foldable or a TV.
