# Samples

Twelve samples ship with the kit: five from the first release (one per upstream module), six for global use, one per kind of game, and Scroll View Basics, which came with the ScrollView module. Import one from
Window > Package Manager > FDP UI Kit > Samples (UPM install), or copy its folder from `Samples~/<Name>` to `Assets/Samples/<Name>` (classic Assets install, where `Samples~` stays hidden). Each is a single `MonoBehaviour` you put on an empty GameObject in an empty scene; it builds its own camera, document and
panel settings in code, so it runs in any render pipeline, with the legacy Input Manager, the Input System or both, and with none of the optional packages. Each has a README of its own.

| Sample | Kind of game | Shows | Modules |
|---|---|---|---|
| [Shell Quick Start](../Samples~/ShellQuickStart/README.md) | card, puzzle, casual, any game with menus | `UiShell`: router, dialogs, toasts, loading overlay, back handling, tab bar with a badge, localized texts that switch language while open | Foundation, Controls, Language |
| [RPG Inventory](../Samples~/RpgInventory/README.md) | RPG, strategy, roguelike | two linked slot grids, drag and drop, keyboard and gamepad pick-up, context menu, tooltips, locked slots, refused moves | Gameplay, Controls, Extras |
| [Action HUD](../Samples~/ActionHud/README.md) | action, FPS, racing | meters with a damage trail, cooldowns, floating damage numbers, a name plate that follows a 3D object and clamps to the screen edge, a kill feed, a pause menu on the router, a HUD that ignores the D-pad | Hud, Foundation, Interaction |
| [Gamepad Menu](../Samples~/GamepadMenu/README.md) | console, handheld, TV, desktop | the focus ring only for keyboard and gamepad, focus on the first control, focus trap in dialogs, one back signal, `Activate` on a plain element, title-safe margin, an input-device override to test without a gamepad | Interaction, Foundation, Controls |
| [Responsive](../Samples~/Responsive/README.md) | phone, tablet, foldable, desktop window, TV | panel scale from the screen and the player's UI size, size-class and orientation classes driving plain USS, a safe area with a fake notch, touch targets, the scale policy for six devices | Foundation |
| [Theming and Language](../Samples~/ThemingLanguage/README.md) | any | dark, light, run-time brand themes, high contrast, text size, reduced motion, English / German / Arabic (right-to-left), a contrast audit of the current theme | DesignSystem, Language, Interaction, Controls |
| UI Kit Quick Start | any | the facade: `UIKitHost`, theme switch, tooltip, fade-in | UIKit, DesignSystem, Extras |
| Design System Showcase | any | the design system components and a Light/Dark switch | DesignSystem |
| Components Samples | tools, editor-style UI | the six UIComponents samples | Components |
| Animation Toolkit Demo | any | fades, hover, toast, pulse, foldout | Animation |
| Extras Stylesheets | any | theme variables and stylesheets for the Extras controls | Extras |
| [Scroll View Basics](../Samples~/ScrollViewBasics/README.md) | any game with a level select, a shop shelf, a leaderboard or a collection | a looping, snapping carousel with click-to-focus, a 1,000-row list with a scrollbar and jump buttons, a 400-tile grid; six smoke tests click, drag, scroll and press its buttons (`Tools~/SamplesSmoke/ScrollViewBasicsSmokeTests.cs`); not yet run through the matrix | ScrollView |

## How the new samples are written

- **Everything in code, one file.** The component creates a `PanelSettings` (with the one-line runtime theme every panel needs, in the sample's own `Resources` folder), a `UIDocument` configured while inactive, and the kit objects it demonstrates.
  The point is to be read and copied, so each file starts with a summary of what to try.
- **No `UnityEngine.Input`.** Input comes through UI Toolkit events and the Interaction module, so nothing in a sample depends on the input backend.
- **The kit's modules are joined in the sample, in a few lines.** The new modules do not reference each other. Where two of them meet, the sample shows the join, for example `InputProfileSource` (Interaction's `InputProfile` as the shell's `IUiInputModeSource`)
  and `BackKeySource.Pressed` calling `UiShell.HandleBackPress()`. `UIShellHost` ([Bootstrap/README.md](Bootstrap/README.md)) does those joins once for a game that does not need to read them.
- **A host component per sample instead of `UIShellHost`.** The samples that use a shell create it themselves and call `Tick()` from `Update`, as the Foundation guide shows, so each sample stays one readable file that shows every join. A game that does not need to read the joins uses `UIShellHost` ([Bootstrap/README.md](Bootstrap/README.md)).
  [Recipes.md](Recipes.md), recipe 0, is the same joins as one reusable component for your own game.
- **Namespaced Resources.** Every sample's Resources subfolder is under `FutureDigitalPlus/UIKit/Samples/<Name>/`, so it cannot collide with a game's paths.
- **The only content outside the kit** is what a sample draws: design system icons, procedural primitives in the HUD scene, and the text the samples define themselves.

## Verification state

| What | State |
|---|---|
| Compiles | yes: in Unity in every variant of [CompatibilityMatrix.md](CompatibilityMatrix.md) (all eleven samples imported next to the package, Built-in, URP, HDRP, with and without the optional packages), and with Roslyn (`Tools~/CompileCheck/run-compile-check.sh --samples`, also with the WebGL and Windows symbols) |
| Runs in Unity | yes: `Tools~/SamplesSmoke` (ten PlayMode tests for the eleven samples: each builds without a logged error, shows the widgets its README names, survives 30 frames of its own `Update`, leaves no document behind) passes in the Built-in pipeline, URP 3D, URP 2D and HDRP (Linear), with the Input System package only, with both input backends, with all optional packages and with Addressables |
| Not run | the Extras sample (style sheets only: it imports, with the warnings its README lists) and the Addressables sample (it needs an Addressables group and a player content build: it compiles when Addressables is installed) |
| Input (click, key, gamepad) | not covered by any test |
| Devices (phone, tablet, foldable, console, TV, gamepad) | none available; the Responsive and Gamepad Menu samples are written so a desktop can fake what they show |

To run them: `Tools~/Matrix/run-matrix.sh <variant> --samples --base-project <clean project> --work <dir>` (it copies the samples and the tests into a scratch project, runs only them and deletes nothing of yours),
or `Tools~/SamplesSmoke/run-samples-smoke.sh <scratch project>` and then the PlayMode test run described in `Tools~/SamplesSmoke/README.md`.
Delete the copy afterwards; never copy samples into a game by script.
