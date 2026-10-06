# RPG inventory (sample)

A backpack and a chest with drag and drop, keyboard and gamepad pick-up, a context menu, tooltips and a tab bar with badges. Genre: RPG, strategy, roguelike, survival. Modules: Gameplay, Controls (and the Extras tooltip).

1. Window > Package Manager > Future Digital Plus UI Kit > Samples > RPG Inventory > Import.
2. Create an empty scene, add an empty GameObject, add the component `Future Digital Plus UI Kit > RPG Inventory Sample`.
3. Press Play.

## How to play with it

| Device | Do |
|---|---|
| Mouse | drag an item to another slot or to the other grid; right click for the context menu; click a potion to drink it |
| Finger | the same; a long press opens the context menu |
| Keyboard | arrow keys move focus; Enter picks the stack up and puts it down on another slot; Escape puts it back; Shift while picking up takes half; the menu key opens the context menu |
| Gamepad | D-pad moves focus; A picks up and puts down; B puts back; Y opens the context menu |

The last row of the backpack is locked, and the chest refuses the cellar key. Both show up as a refusal in the status line.

## What to read in `RpgInventorySample.cs`

| Topic | Code |
|---|---|
| The data is plain C#, no UI | `InventoryModel(capacity, maxStackOf, canPlace)`, `ItemStack`, `Add`, `Set`, `MoveHalf`, `SetLocked` |
| Showing it | `SlotGrid.Bind(model, visualOf)` with an `ItemVisual` (icon and rarity) per item id; `Columns`, `SlotSize`, `Spacing` |
| Carrying items between grids | `SlotGrid.Link(a, b)` shares one `PickupMode` |
| What happened | `Moved`, `MoveRejected` (with a `MoveReason`), `SlotActivated`, `SlotContext` |
| Context menu | `PopupMenu.Show(slot, items, panelPosition, onInvoke)` with `MenuEntry` items, from Controls |
| Tooltips | one `Tooltip` element at the end of the root, a `TooltipManipulator` per slot, the text in `slot.tooltip` (`T:` puts it above) |
| Tab bar with badges | `TabBar.AddTab`, `SetBadge(id, new BadgeValue(count, dot))`, `EmptyStates.Create` for the empty tabs |

## Notes

- The grids report and the game decides: what "use" means, what a tooltip says and which items may go where are all in the sample's code, not in the module.
- The sample never reads `UnityEngine.Input`.
- Run in Unity 6000.3.25f1 by `Tools~/SamplesSmoke` (PlayMode: it builds without a logged error, shows the widgets named above, survives 30 frames of its own `Update` and leaves no document behind) in the Built-in pipeline, URP 3D, URP 2D and HDRP, with the Input System package only and with all optional packages; see `Documentation~/CompatibilityMatrix.md`. The test does not click or press anything, and nothing was run on a device.
