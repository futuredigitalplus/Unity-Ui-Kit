# Action HUD (sample)

A heads-up display over a 3D scene, the way an action game or shooter builds it: bars, counters, cooldowns, floating damage numbers, a feed, and a name plate that follows an
enemy in the world. Genre: action, FPS, racing, survival. Modules: Hud, Foundation, Interaction.

1. Window > Package Manager > FDP UI Kit > Samples > Action HUD > Import.
2. Create an empty scene, add an empty GameObject, add the component `FDP UI Kit > Action HUD Sample`.
3. Press Play. Use the buttons at the bottom right ("Hit", "Heal") and the three skill slots at the bottom centre (click, Enter or gamepad A). Escape, gamepad B or the pause button opens the pause menu.

The scene (camera, light, ground, an enemy walking a wide ellipse) is built in code, so the sample works in an empty scene in any render pipeline.

## What to read in `ActionHudSample.cs`

| Widget | Code |
|---|---|
| Health, stamina | `MeterBar` (damage trail, segments, warning and danger colours) |
| Coins | `CurrencyLabel` (counts up, shows the delta), `SetIcon` with a design system icon |
| Wave timer | `CountdownLabel.StartIn` |
| Skills | `CooldownIcon` + `Activate.Register` so a click, Enter and gamepad A all cast |
| Charge ring | `RadialProgress` |
| Kill and loot feed | `TimedFeed.Push(text, key, style)` (identical messages merge) |
| Damage numbers | `FloatingTextLayer.Spawn(panelPosition, text, kind)`; the position comes from `RuntimePanelUtils.CameraTransformWorldToPanel` |
| Name plate on the enemy | `WorldAnchorLayer.Pin(content, transform, offset, OffScreenMode.Clamp, AnchorPivot.Above)`: an edge arrow when the enemy is off screen |
| Layout | `HudZones` (nine zones) inside a `SafeAreaPanel`, in the shell's HUD layer |
| Pause | `PauseMenu.Register` on the shell catalog; `Router.Push("Pause")` |
| Input | `InputProfile.NavigationEnabled = false` while the game runs, true while the pause menu is open; `BackKeySource` for Escape and gamepad B |

## Notes

- The HUD document must not react to WASD and the D-pad while the game reads them. That is the whole point of `NavigationEnabled = false`.
- The router needs a start screen. The sample registers an empty screen that lets every touch through (`RootPicking = PickingMode.Ignore`).
- `InputProfileSource` (a few lines at the end of the file) is the seam that tells the shell which device the `InputProfile` sees.
- The keys and buttons stand in for your game's own bindings. The sample never reads `UnityEngine.Input`, so it compiles with the legacy Input Manager, the Input System, or both.
- Run in Unity 6000.3.25f1 by `Tools~/SamplesSmoke` (PlayMode: it builds without a logged error, shows the widgets named above, survives 30 frames of its own `Update` and leaves no document behind) in the Built-in pipeline, URP 3D, URP 2D and HDRP, with the Input System package only and with all optional packages; see `Documentation~/CompatibilityMatrix.md`. The test does not click or press anything, and nothing was run on a device.
