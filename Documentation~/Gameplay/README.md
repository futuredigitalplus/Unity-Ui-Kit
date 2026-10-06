# Gameplay

Inventory building blocks: a model of slots with the rules of moving, merging, splitting and swapping stacks, and a grid of slots that a player rearranges with a pointer, touch, keyboard or gamepad. For RPGs, survival and crafting games, shops, deck and collection screens, equipment and loadout screens, and any "stuff you put in boxes" interface.

| | |
|---|---|
| Assemblies | `FutureDigitalPlus.UIKit.Gameplay.Core` (pure C#, no `UnityEngine`, tested by dotnet) and `FutureDigitalPlus.UIKit.Gameplay` (elements) |
| Namespace | `FutureDigitalPlus.UIKit.Gameplay` |
| Depends on | nothing else in the kit; uses design system tokens and the `ds-drag-ghost` and `ds-badge` classes when the design system sheet is on the panel, with a literal fallback for every token |
| Styles | `Resources/FutureDigitalPlus/UIKit/Gameplay/Styles/Gameplay.uss`, installed with `GameplayStyles.Install(root)` |

## Set up

```csharp
using FutureDigitalPlus.UIKit.Gameplay;

GameplayStyles.Install(uiDocument.rootVisualElement);     // once per panel

var bag = new InventoryModel(24, id => id == "potion" ? 10 : 1);   // 24 slots, stack limit per item id
bag.Add(new ItemStack("potion", 14));                              // fills stacks first, returns what did not fit

var grid = new SlotGrid { Columns = 6, SlotSize = 56, Spacing = 4 };
grid.Bind(bag, id => new ItemVisual(IconFor(id), RarityFor(id)));   // icon and rarity per item id
root.Add(grid);

grid.Moved += move => Audio.Play("drop");
grid.MoveRejected += move => Audio.Play("nope");                   // move.Result.Reason says why
grid.SlotActivated += slot => Use(bag[slot.Index]);
grid.SlotContext += (slot, position) => PopupMenu.Show(slot, items, position, onInvoke);  // Controls module, optional
```

The grid does not know what an item is, what a tooltip is or what the context menu does: it reports hover, focus, activation and context requests as events, and a game connects whatever it has (the Extras tooltip, the Controls `PopupMenu`). Because nothing is referenced, there is no dependency on those modules.

## The model (`Gameplay.Core`)

- **`ItemStack`**: an item id and a count. An empty stack has no id. Items that must not stack get unique ids and a stack limit of 1.
- **`InventoryModel`**: fixed slots. `Add`, `Remove`, `CountOf`, `FirstSlotOf` (a null or empty item id matches nothing: 0, -1, 0), `Set` (direct, ignores rules, for loading a save), `SetLocked`, and the moves:
  - `Move(from, to, count)`: an empty target takes the items (up to the stack limit), the same kind merges up to the limit, another kind swaps with the whole stack. `count` 0 means the whole stack.
  - `MoveHalf(from, to)`: the larger half. `Transfer(source, from, target, to, count)`: the same between two inventories, using the target's limits and rules.
  - A refusal changes nothing and carries a reason: `InvalidSlot`, `SameSlot`, `SourceEmpty`, `Locked`, `Rejected` (your callback said no), `TargetFull`, `StackLimit`, `CannotSplitIntoOccupied`.
  - The rule callback `canPlace(slot, stack)` vetoes a stack in a slot, so an equipment slot can accept only helmets. It sees the stack that would result (the merged stack), and for a swap it is asked for both slots.
  - `SlotChanged(slot)` is raised after a slot changed. `MoveResult.Success` and `MovedCount` say what happened.
- **`PickupMode`**: pick up, carry, put down. Nothing leaves its slot while carried, so cancelling or a refused drop leaves everything as it was. One instance can serve several grids.
- **`SlotGridMath`**: where an arrow key moves in a grid.

The tests move items at random between two inventories for thousands of steps and check that no item is created or lost and no stack limit is exceeded.

## The views

- **`ItemSlot`** (`[UxmlElement]`): icon, rarity border (the design system rarity colours: `common`, `rare`, `epic`, `legendary`), stack count from 2 up, cooldown shade (`Cooldown` 0..1 from the top), lock mark, selected state, equipped chip. Focusable. Also usable alone for a hotbar or an equipment slot with `SetItem` and `SetEmpty`.
- **`SlotGrid`**: shows an `InventoryModel` and keeps up to date while on a panel (it stops listening when detached and redraws when attached again).
  - Pointer: press and move past `DragThreshold` (6) to drag; a ghost (`ds-drag-ghost`) follows the pointer, the slot under it lights up (`fdp-item-slot--drop`), letting go puts the items there. A drop that is refused or lands outside any slot changes nothing. Shift while pressing takes half, Ctrl or Command takes one. A press without movement raises `SlotActivated`; right click raises `SlotContext`; a long press on touch raises `SlotContext` (`LongPressSeconds`).
  - Keyboard and gamepad: the arrows move focus by slot; Submit picks up the stack (Shift: half, Ctrl: one), Submit on another slot puts it down, Submit on the source slot or Cancel puts it back. A refused placement keeps carrying so the player can try another slot. Set `SubmitPicksUp = false` when Submit should only raise `SlotActivated` (use the item) and picking up is done by dragging. The gamepad Y button (`ContextKey`) and the menu key raise `SlotContext`.
  - Several grids: `SlotGrid.Link(a, b)` makes them share one `PickupMode`, so an item picked up (or dragged) in one is put down in the other. `Moved` and `MoveRejected` are raised on the grid that owns the target slot.
  - Layout: slots wrap to the width, or set `Columns`. `ColumnCount` tells the number used right now.

## How it behaves

- **No allocation per drag move** inside a slot after the ghost exists: the move updates the ghost position and the highlighted slot only. A PlayMode test asserts 0 bytes. Entering another slot changes one USS class, and UI Toolkit allocates when a class changes (about 3 KB measured), once per slot entered, not once per frame.
- **Reduced motion:** the only motion is the 120 ms colour transition of the slot border, which becomes 0 ms under `fdp-reduce-motion`.
- **Focus ring:** shown only while the root has `fdp-input-nav`; the slot keeps a constant border (its colour is the state), so nothing moves.
- **Touch:** a slot inside a scrolling parent competes with the parent's touch scrolling; give the inventory its own area, or start drags with a long press in your own code.
- **Right to left:** left and right arrows are not mirrored; set `Columns` and read order explicitly for RTL layouts if it matters to you.

## Tests

Current test counts: [../CompatibilityMatrix.md](../CompatibilityMatrix.md), "Test counts".

- `Tests/Pure/Gameplay` (EditMode asmdef `...Gameplay.Core.Tests`, also run by `Tools~/PureTests` under dotnet): the model rules, pickup mode, grid math and a random-operation invariant test.
- `Tests/Runtime/Gameplay` (PlayMode `...Gameplay.Tests`): drag by pointer events with ghost and highlight, refused and outside drops, split by modifier, transfer between two grids, pick-up mode by navigation events, linked grids, events, focus ring without layout shift, slot visuals, and the drag allocation test.
