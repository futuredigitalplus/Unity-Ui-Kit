# UI Kit quick start (sample)

The smallest screen that uses the facade: a themed card, a Light/Dark switch, a tooltip and a fade-in.

1. Window > Package Manager > FDP UI Kit > Samples > UI Kit Quick Start > Import.
2. Create an empty scene, add an empty GameObject, add the component
   `FDP UI Kit > UI Kit Quick Start Sample`.
3. Press Play.

`UIKitQuickStart.cs` is about 100 lines and meant to be read and copied. It shows the three things most games need:

- `UIKitInstaller.CreatePanelSettings(...)` makes a scaling PanelSettings (give it your runtime theme `.tss`).
- A `UIKitHost` next to the `UIDocument` installs the design system base styles, the theme, the `fdp-input-touch` class and the
  safe area every time the document builds its tree. Assign `host.Theme` at any time to re-theme the screen.
- The Extras tooltip follows the design system tokens by itself; `QuickStartExtras.uss` (loaded in `Build`) shows how to override its optional colour names
  for one control. Not verified by running the sample on a screen: only the tooltip's construction is tested.
- `FadeIn` / `FadeOut` (from `UIKitMotion`) wrap the Animation module for the two most common motions.

The sample's Resources folder is namespaced (`FutureDigitalPlus/UIKit/Samples/UIKitQuickStart`) so it cannot collide with a
game's own Resources paths.
