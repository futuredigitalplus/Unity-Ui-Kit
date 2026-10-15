# Controls

The pickers and containers a menu is made of, for any game and any input: tabs and segmented controls with badges, a context menu with submenus, a select field that works with a gamepad, a bottom sheet, list and grid helpers that keep their scroll position, and a reward popup. Everything works with a pointer, a touch screen, a keyboard and a gamepad alone.

| | |
|---|---|
| Assemblies | `FutureDigitalPlus.UIKit.Controls.Core` (pure C#, no `UnityEngine`, tested by dotnet) and `FutureDigitalPlus.UIKit.Controls` (elements) |
| Namespace | `FutureDigitalPlus.UIKit.Controls` |
| Depends on | `FutureDigitalPlus.UIKit.Foundation` for one thing, `FocusTrap` (the popups hold focus with it, so a popup opened from a dialog and the dialog never fight over focus). Uses design system classes (`ds-tabs`, `ds-sheet`, `ds-modal`, `ds-empty`, `ds-skeleton`, `ds-btn`) when the design system sheet is on the panel, with a literal fallback for every token |
| Does not need | `UiShell` or any other Foundation service (only the `FocusTrap` class is used), Interaction, Components, TextMeshPro, uGUI, Input System, Localization |
| Styles | `Resources/FutureDigitalPlus/UIKit/Controls/Styles/Controls.uss`, installed with `ControlsStyles.Install(root)` |

## Set up

```csharp
using FutureDigitalPlus.UIKit.Controls;

var root = uiDocument.rootVisualElement;
ControlsStyles.Install(root);     // once per panel, on the document root or an ancestor of the controls
```

Popups (context menu, select list, bottom sheet, reward popup) are added to the top element of the panel the control lives in, as a full-size layer above everything else, so a scroll view or a clipped parent never cuts them off. The sheet must therefore be installed on that top element or above it, which is what the document root is.

In UXML the elements that are `[UxmlElement]` (`TabBar`, `SegmentedControl`, `SelectField`) are written `xmlns:c="FutureDigitalPlus.UIKit.Controls"`, `<c:TabBar items="home:Home,shop:Shop" value="home"/>`, `<c:SelectField entries="Low,Medium,High" placeholder="Quality"/>`.

## Elements

| Element | What it does |
|---|---|
| `TabBar` | One selection controller, two skins (`TabBarSkin.Tabs` = the design system tab strip, `TabBarSkin.Segmented` = a rounded segmented control). Items have an id and a label, can be disabled, and can carry a badge (dot or count). `value` is the selected id and raises a `ChangeEvent<string>`. Pointer, Submit, left and right arrows (toward the item on that side on screen), Home and End select, focus follows the selection. Mirrors itself in a right-to-left context (see "Right to left" below). `BindBadges(BadgeTree, prefix)` shows aggregated badges and visiting an item clears its badge |
| `SegmentedControl` | A `TabBar` already in the segmented skin; the drop-in name for "Easy / Normal / Hard" switches |
| `PopupMenu` | Menu at a pointer position (`Show`) or below an element (`ShowBelow`, optionally as wide as it). Submenus, separators, disabled and checked items, hints, type-ahead, Home and End, arrows, Submit, Cancel, outside click. Flips at the screen edge. Focus moves into it and returns on close. `PopupMenu.Attach(element, items, onInvoke)` opens it from a right click, a long press on touch, the menu key (or Shift+F10) and the gamepad Y button |
| `SelectField` | A select that works without a pointer. Submit opens the list (a `PopupMenu` with the current choice focused and marked), left and right change the choice without opening. Value is the index; `ChangeEvent<int>`. This is the console and gamepad path: the stock `DropdownField` cannot select an item with the d-pad in Unity 6000.3 |
| `BottomSheet` | `ds-sheet` skin over a scrim. Slides up to a snap point (fractions of its own height), dragged by its handle or moved with the up and down arrows, dismissed by a downward flick, Cancel or back, the scrim, `Close()`. Keeps focus inside and gives it back. `HandleBack()` is the hook for a back dispatcher (Android back, gamepad B) |
| `ListViewBinder<T>` | A `ListView` fed with whole lists by stable key: keeps the scroll offset and the selection (by key), skips unchanged rows, shows an empty state and skeleton rows while loading, fixed or dynamic row height |
| `VirtualGrid<T>` | Scrolling grid of equal cells that builds only the visible ones (recycled pool). Columns follow the width, keyboard and gamepad arrows move focus by cell and scroll the target into view even when it is not built yet. Same data, empty and loading handling as the list |
| `EmptyStates.Create(title, message)` | A ready `ds-empty` content for the two collections |
| `RewardPopup` / `RewardPopupQueue` | A modal "you got these": rewards appear one after another, the one button skips and then claims, a tap on the card skips, Cancel or back behaves like the button. Takes view models (`RewardItem`: id, label, amount text, icon, rarity) and knows nothing about stores, prices or analytics. The queue shows several sets in a row |

The pure part (`Controls.Core`) holds the logic: `MenuModel` (focus, submenus, type-ahead), `TabNavigation` (the arrow, Home and End rules of `TabBar`), `BadgeTree`, `ListDiff`, `GridLayoutMath`, `PopupPlacement` (edge flip), `SheetSnap`, `RewardSequence`, `PresentationQueue<T>`.

```csharp
// Tabs with badges
var tabs = new TabBar();
tabs.AddTab("home", "Home"); tabs.AddTab("shop", "Shop");
tabs.RegisterValueChangedCallback(e => ShowPage(e.newValue));
var badges = new BadgeTree();
tabs.BindBadges(badges);
badges.SetCount("shop/offers", 2);          // the Shop tab shows 2; visiting the tab clears it

// Context menu on a row
PopupMenu.Attach(row, () => new[] { new MenuEntry("rename", "Rename"), MenuEntry.Separator(), new MenuEntry("delete", "Delete") },
                   item => Handle(item.Id));

// Reward popup
RewardPopup.Show(root, "Level complete", new[] { new RewardItem("coins", "Coins", "x250"), new RewardItem("gem", "Gem", "x3", gemSprite, "epic") });
```

## How it behaves

- **Keyboard and gamepad, with no extra setup.** Every control reacts to the UI Toolkit navigation events (`NavigationMoveEvent`, `NavigationSubmitEvent`, `NavigationCancelEvent`) and to `KeyDownEvent`, which is what the panel raises for a keyboard, a gamepad, or the Input System UI module alike. The kit never reads `UnityEngine.Input`.
- **Focus ring.** The sheet draws the same ring as the Interaction module's `Focus.uss` for the controls that only carry `fdp-*` classes; it shows only while the root has `fdp-input-nav`. Where the design system styles the control (tabs) the Interaction sheet applies. Rest borders are transparent and as wide as the ring, so focus never moves a layout.
- **Reduced motion.** With `fdp-reduce-motion` on an ancestor, the sheet and reward popup transitions are 0 ms, the sheet opens and closes at once, and all rewards show at once.
- **Back.** Cancel (and Escape) closes the topmost popup one level at a time; Escape raises a key and a cancel event in one frame, which is counted once. Popups consume the event so nothing below reacts. Focus is held with Foundation's `FocusTrap`: traps nest (only the newest of a panel is active), so a select list opened inside a dialog gets focus and the dialog gets it back when the list closes. Connect your own back source with `HandleBack()` on `BottomSheet`, `PopupMenu` and `RewardPopup`.
- **Right to left.** `TabBar` and `SegmentedControl` mirror themselves, with nothing to add in the game:
  - *When.* The bar is right to left when the nearest element from the bar up to the panel root that sets `languageDirection` says RTL, or when none sets it and one carries `fdp-rtl` (`UiDirection.IsRightToLeft`, rule in `ReadingDirection`). `LocaleClassApplier` writes both on the root, so a game that uses it, or the shell, needs nothing else. UI Toolkit's own `languageDirection` getter is not enough: it returns only the value set on that element (`Inherit` below the root). `TabBar.IsRightToLeft` tells the result.
  - *Layout.* The bar puts `fdp-tab-bar--rtl` on itself; `Controls.uss` lays the items out from the right (`.fdp-tab-bar.fdp-tab-bar--rtl { flex-direction: row-reverse; }`), so the first item is at the right edge, and moves the badges to the top-left corner. The selected segment is the selected item, so its highlight moves with it. Two classes, so a plain `.my-bar { flex-direction: row; }` does not undo it; a stylesheet that stacks the bar as a column needs three classes (or a rule on `fdp-tab-bar--rtl`).
  - *Keys.* Left and Right go to the item on that side on screen: the bar reads its resolved flex direction, so the keys match the picture whoever reversed the row (the kit, `Rtl.uss` for the tabs skin, a game sheet). In a stacked (column) bar Right goes back and Left forward in a right-to-left language. Home selects the first item and End the last, in item order: the reading start (right edge) and end (left edge).
  - *Focus and reading order.* The items stay in the order they were added, in the tree too, so Tab and Shift+Tab and assistive technology meet them in reading order (the first item first, at the right).
  - *Live change.* The bar reads the direction again when it is attached, whenever UI Toolkit resolves its style again (the class change of a language switch on the root; `Controls.uss` gives the bar the custom property `--fdp-tab-bar-watch` so that UI Toolkit sends it `CustomStyleResolvedEvent`), when it gets focus and before it handles an arrow. Code that changes only `languageDirection` on an ancestor while the bar is shown calls `RefreshDirection()`.
  - `SelectField` and `PopupMenu` still swap Left and Right only when `languageDirection` is RTL on the element itself, not when it is inherited (`PopupMenu` opens its submenus to the right in every language, so the wider rule would point its keys away from the submenu; a known limit).
- **Touch.** Hit areas follow the design system touch tier (`fdp-input-touch` raises the control heights through the `--fdp-control-h-*` tokens). A long press opens a context menu from `Attach`.

## Not done, and why

- **`RewardPopup` is not a `DialogRequest`.** The plan had it go through the Foundation dialog queue with high priority. The module uses only the `FocusTrap` class of Foundation, so the popup owns its layer instead of going through a service. A game that wants priority and de-duplication shows it from its dialog service and calls `Show` from there; the plan's variant belongs in the facade (`Documentation~/UIKit`).
- **Settings rows** (toggle, slider, stepper, link, reset to defaults, the stretch item) are not included. They are built from design system controls and a dialog; the kit's samples show the pattern.
- **Tree and multi-column lists:** `ListViewBinder` wraps `ListView`. `MultiColumnListView` and `TreeView` are styled by the design system and can be fed by hand.

## Tests

Current test counts: [../CompatibilityMatrix.md](../CompatibilityMatrix.md), "Test counts".

- `Tests/Pure/Controls` (EditMode asmdef `...Controls.Core.Tests`, also run by `Tools~/PureTests` under dotnet): the six models, the placement and the queue.
- `Tests/Runtime/Controls` (PlayMode `...Controls.Tests`, UI Toolkit panel on a `RenderTexture`, events sent directly): tab selection by pointer, Submit, arrows, Home and End, disabled items, badge aggregation, right to left (the order on screen, arrows, Home and End in both skins, `languageDirection` alone, a live language switch, a left-to-right island, the badge corner, a stacked bar); context menu at the pointer, edge flip, submenus, Cancel and outside click, focus return, `Attach`; select field with arrows and Submit; bottom sheet open, snap, drag, flick, scrim, Cancel, reduced motion; list and grid scroll and selection keeping, empty and loading states, grid focus movement; reward popup reveal, skip, claim, back, reduced motion and the queue.
