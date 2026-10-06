# Gamepad menu (sample)

A menu that works with a gamepad, a keyboard, a mouse and a finger alike. Genre: console, handheld, TV, desktop. Modules: Interaction, Foundation, Controls.

1. Window > Package Manager > Future Digital Plus UI Kit > Samples > Gamepad Menu > Import.
2. Create an empty scene, add an empty GameObject, add the component `Future Digital Plus UI Kit > Gamepad Menu Sample`.
3. Press Play. Use the arrow keys (or a gamepad D-pad), Enter (or A) to select, Escape (or B) to go back.

No gamepad? The "Input device" row pins the input mode to Keyboard, Gamepad or Mouse, so the focus ring and the prompts can be checked with a mouse.

## What it shows

| Topic | Where |
|---|---|
| The focus ring appears only for keyboard and gamepad (`fdp-input-nav`), never for mouse or touch | `InputProfile.Bind(root)` + `InteractionStyles.Install(root)` |
| Focus starts on the first control of every page | `UiFocus.FocusFirst(Root)` in `OnShow` |
| Focus stays inside a dialog and returns to the button that opened it | the quit confirmation (the dialog service uses `FocusTrap` itself) |
| One back signal for Escape, gamepad B and the Android back button | `BackKeySource` -> `UiShell.HandleBackPress()`; at the main menu the game decides (a quit question) |
| A plain element that works like a button | `Activate.Register(card, action)` |
| Controls that follow the D-pad | `TabBar` (segmented, left/right), `SelectField`, `Slider`, `ListRow` |
| Title-safe margin for television overscan | `UiShellOptions.MinimumMarginPercent = 5`, switchable at run time with `SafeAreaProvider.MinimumMarginPercent` |

## Notes

- Nothing in this sample was run on a console, a handheld or a real gamepad (none was available). The input-mode, focus-ring and back logic it uses is exercised by the Interaction module's own tests with synthetic events.
- `InputProfileSource` (at the end of the file) tells the shell which device the `InputProfile` sees, so the shell's layout classes and the profile's agree.
- The sample never reads `UnityEngine.Input`. With the Input System package installed the `FutureDigitalPlus.UIKit.Interaction.InputSystem` backend registers itself; without it the legacy Input Manager is used and its axes `Horizontal`, `Vertical`, `Submit` and `Cancel` must exist (they do in a new project).
- Run in Unity 6000.3.25f1 by `Tools~/SamplesSmoke` (PlayMode: it builds without a logged error, shows the widgets named above, survives 30 frames of its own `Update` and leaves no document behind) in the Built-in pipeline, URP 3D, URP 2D and HDRP, with the Input System package only and with all optional packages; see `Documentation~/CompatibilityMatrix.md`. The test does not click or press anything, and nothing was run on a device.
