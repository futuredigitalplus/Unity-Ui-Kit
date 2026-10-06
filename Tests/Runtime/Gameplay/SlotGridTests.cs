using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UIElements;

namespace FutureDigitalPlus.UIKit.Gameplay.Tests
{
    public class SlotGridTests
    {
        private GameplayPanel _panel;

        [SetUp]
        public void SetUp() => LogAssert.ignoreFailingMessages = true;

        [TearDown]
        public void TearDown()
        {
            LogAssert.ignoreFailingMessages = false;
            _panel?.Dispose();
            _panel = null;
        }

        private static int Max(string id) => id == "potion" ? 10 : id == "sword" ? 1 : 99;

        private static ItemVisual Look(string id) => new ItemVisual(default, id == "sword" ? "Epic" : id == "gem" ? "rare" : null);

        private SlotGrid AddGrid(InventoryModel model, float left = 0f, float top = 0f, bool navigationMode = false)
        {
            if (_panel == null) _panel = GameplayPanel.Create(navigationMode);
            var grid = new SlotGrid { Columns = 4, style = { position = Position.Absolute, left = left, top = top } };
            grid.Bind(model, Look);
            _panel.Root.Add(grid);
            return grid;
        }

        private VisualElement Ghost() => _panel.Root.panel.visualTree.Q<VisualElement>("ghost");

        private static Vector2 Center(VisualElement element) => element.worldBound.center;

        // A press, a move past the threshold to a point, and nothing else yet.
        private static void StartDrag(SlotGrid grid, int slot, Vector2 to, EventModifiers modifiers = EventModifiers.None)
        {
            var element = grid.Slots[slot];
            Send.PointerDown(element, Center(element), 0, modifiers);
            Send.PointerMove(element, Center(element) + new Vector2(10f, 0f));
            Send.PointerMove(element, to);
        }

        private static void Drop(SlotGrid grid, int slot, Vector2 at)
        {
            Send.PointerUp(grid.Slots[slot], at);
        }

        private static InventoryModel Filled()
        {
            var inv = new InventoryModel(8, Max);
            inv.Set(0, new ItemStack("potion", 6));
            inv.Set(1, new ItemStack("sword", 1));
            inv.Set(2, new ItemStack("gem", 3));
            inv.SetLocked(7, true);
            return inv;
        }

        // ---- showing the inventory ----

        [UnityTest]
        public IEnumerator Bind_CreatesASlotPerInventorySlot_AndShowsTheContent()
        {
            var inv = Filled();
            var grid = AddGrid(inv);
            yield return GameplayPanel.Frames();
            Assert.AreEqual(8, grid.Slots.Count);
            Assert.IsTrue(grid.Slots[0].HasItem);
            Assert.AreEqual(6, grid.Slots[0].Count);
            Assert.IsFalse(grid.Slots[1].Q<Label>("count").resolvedStyle.display == DisplayStyle.Flex, "a stack of one shows no number");
            Assert.IsTrue(grid.Slots[1].ClassListContains("fdp-item-slot--epic"), "rarity names are case-insensitive");
            Assert.IsTrue(grid.Slots[2].ClassListContains("fdp-item-slot--rare"));
            Assert.IsTrue(grid.Slots[3].ClassListContains("fdp-item-slot--empty"));
            Assert.IsTrue(grid.Slots[7].IsLocked);
            Assert.AreEqual(5, grid.Slots[5].Index);
            Assert.AreSame(grid, grid.Slots[5].Grid);
        }

        [UnityTest]
        public IEnumerator ModelChanges_UpdateTheSlots()
        {
            var inv = Filled();
            var grid = AddGrid(inv);
            yield return GameplayPanel.Frames();
            inv.Set(3, new ItemStack("potion", 4));
            Assert.IsTrue(grid.Slots[3].HasItem);
            Assert.AreEqual(4, grid.Slots[3].Count);
            Assert.IsFalse(grid.Slots[3].ClassListContains("fdp-item-slot--empty"));
            inv.Set(3, ItemStack.Empty);
            Assert.IsFalse(grid.Slots[3].HasItem);
            inv.SetLocked(3, true);
            Assert.IsTrue(grid.Slots[3].IsLocked);
        }

        [UnityTest]
        public IEnumerator Columns_FixedOrMeasuredFromTheWrap()
        {
            var inv = new InventoryModel(10, Max);
            _panel = GameplayPanel.Create();
            var fixedGrid = AddGrid(inv);
            yield return GameplayPanel.Frames();
            Assert.AreEqual(4, fixedGrid.ColumnCount);
            Assert.AreEqual(fixedGrid.Slots[0].layout.y, fixedGrid.Slots[3].layout.y, 0.5f);
            Assert.Greater(fixedGrid.Slots[4].layout.y, fixedGrid.Slots[0].layout.y);

            var wrapped = new SlotGrid { style = { position = Position.Absolute, left = 0, top = 300, width = 190 } };
            wrapped.Bind(new InventoryModel(10, Max), Look);
            _panel.Root.Add(wrapped);
            yield return GameplayPanel.Frames();
            Assert.AreEqual(3, wrapped.ColumnCount, "190 / (56 + 4)");
            wrapped.SlotSize = 40f;
            yield return GameplayPanel.Frames();
            Assert.AreEqual(4, wrapped.ColumnCount, "190 / (40 + 4)");
        }

        [UnityTest]
        public IEnumerator Unbind_RemovesTheSlotsAndStopsListening()
        {
            var inv = Filled();
            var grid = AddGrid(inv);
            yield return GameplayPanel.Frames();
            var first = grid.Slots[0];
            grid.Unbind();
            Assert.AreEqual(0, grid.Slots.Count);
            Assert.AreEqual(0, grid.childCount);
            Assert.IsNull(first.Grid);
            inv.Set(0, new ItemStack("gem", 1)); // must not throw
        }

        [UnityTest]
        public IEnumerator Detaching_StopsListening_AndAttachingRefreshes()
        {
            var inv = Filled();
            var grid = AddGrid(inv);
            yield return GameplayPanel.Frames();
            grid.RemoveFromHierarchy();
            inv.Set(3, new ItemStack("gem", 2));
            Assert.IsFalse(grid.Slots[3].HasItem, "not updated while detached");
            _panel.Root.Add(grid);
            Assert.IsTrue(grid.Slots[3].HasItem, "refreshed when it comes back");
        }

        // ---- pointer ----

        [UnityTest]
        public IEnumerator Drag_ShowsAGhostAndMarksTheSourceAndTheTarget_ThenMoves()
        {
            var inv = Filled();
            var grid = AddGrid(inv);
            yield return GameplayPanel.Frames();
            var moves = new List<SlotMove>();
            grid.Moved += moves.Add;

            var target = Center(grid.Slots[4]);
            StartDrag(grid, 0, target);
            Assert.IsTrue(grid.Pickup.IsHolding);
            Assert.IsTrue(grid.Slots[0].ClassListContains("fdp-item-slot--held"));
            Assert.IsTrue(grid.Slots[4].ClassListContains("fdp-item-slot--drop"));
            var ghost = Ghost();
            Assert.IsNotNull(ghost, "the ghost is on the top element");
            Assert.IsTrue(ghost.ClassListContains("ds-drag-ghost"));
            Assert.AreEqual(target.x, ghost.worldBound.center.x, 1.5f);
            Assert.AreEqual(target.y, ghost.worldBound.center.y, 1.5f);

            Drop(grid, 0, target);
            Assert.IsFalse(grid.Pickup.IsHolding);
            Assert.IsTrue(inv[0].IsEmpty);
            Assert.AreEqual(new ItemStack("potion", 6), inv[4]);
            Assert.AreEqual(1, moves.Count);
            Assert.AreEqual(MoveReason.Moved, moves[0].Result.Reason);
            Assert.AreEqual(0, moves[0].SourceSlot);
            Assert.AreEqual(4, moves[0].TargetSlot);
            Assert.IsNull(Ghost(), "the ghost is gone");
            Assert.IsFalse(grid.Slots[4].ClassListContains("fdp-item-slot--drop"));
            Assert.IsFalse(grid.Slots[0].ClassListContains("fdp-item-slot--held"));
        }

        [UnityTest]
        public IEnumerator Drag_OntoAnotherKind_Swaps()
        {
            var inv = Filled();
            var grid = AddGrid(inv);
            yield return GameplayPanel.Frames();
            var target = Center(grid.Slots[1]);
            StartDrag(grid, 0, target);
            Drop(grid, 0, target);
            Assert.AreEqual(new ItemStack("sword", 1), inv[0]);
            Assert.AreEqual(new ItemStack("potion", 6), inv[1]);
        }

        [UnityTest]
        public IEnumerator Drag_OntoARefusingSlot_ChangesNothing_AndReportsTheReason()
        {
            var inv = new InventoryModel(8, Max, (slot, stack) => slot != 5);
            inv.Set(0, new ItemStack("potion", 6));
            var grid = AddGrid(inv);
            yield return GameplayPanel.Frames();
            var rejected = new List<SlotMove>();
            grid.MoveRejected += rejected.Add;
            var target = Center(grid.Slots[5]);
            StartDrag(grid, 0, target);
            Drop(grid, 0, target);
            Assert.AreEqual(new ItemStack("potion", 6), inv[0]);
            Assert.IsTrue(inv[5].IsEmpty);
            Assert.IsFalse(grid.Pickup.IsHolding, "a drag never keeps carrying");
            Assert.AreEqual(1, rejected.Count);
            Assert.AreEqual(MoveReason.Rejected, rejected[0].Result.Reason);
            Assert.IsNull(Ghost());
        }

        [UnityTest]
        public IEnumerator Drag_OntoALockedSlot_IsRejected()
        {
            var inv = Filled();
            var grid = AddGrid(inv);
            yield return GameplayPanel.Frames();
            var rejected = new List<SlotMove>();
            grid.MoveRejected += rejected.Add;
            var target = Center(grid.Slots[7]);
            StartDrag(grid, 0, target);
            Drop(grid, 0, target);
            Assert.AreEqual(MoveReason.Locked, rejected[0].Result.Reason);
            Assert.AreEqual(6, inv[0].Count);
        }

        [UnityTest]
        public IEnumerator Drag_DroppedOutsideAnySlot_RestoresEverything()
        {
            var inv = Filled();
            var grid = AddGrid(inv);
            yield return GameplayPanel.Frames();
            var raised = 0;
            grid.Moved += _ => raised++;
            grid.MoveRejected += _ => raised++;
            var outside = new Vector2(700f, 500f);
            StartDrag(grid, 0, outside);
            Assert.IsTrue(grid.Pickup.IsHolding);
            Drop(grid, 0, outside);
            Assert.IsFalse(grid.Pickup.IsHolding);
            Assert.AreEqual(new ItemStack("potion", 6), inv[0]);
            Assert.AreEqual(0, raised);
            Assert.IsNull(Ghost());
        }

        [UnityTest]
        public IEnumerator Drag_WithShift_TakesHalf_AndWithControlTakesOne()
        {
            var inv = Filled();
            var grid = AddGrid(inv);
            yield return GameplayPanel.Frames();
            var target = Center(grid.Slots[4]);
            StartDrag(grid, 0, target, EventModifiers.Shift);
            Drop(grid, 0, target);
            Assert.AreEqual(3, inv[0].Count);
            Assert.AreEqual(3, inv[4].Count);

            target = Center(grid.Slots[5]);
            StartDrag(grid, 0, target, EventModifiers.Control);
            Drop(grid, 0, target);
            Assert.AreEqual(2, inv[0].Count);
            Assert.AreEqual(1, inv[5].Count);
        }

        [UnityTest]
        public IEnumerator Drag_BelowTheThreshold_IsAClick_AndRaisesSlotActivated()
        {
            var inv = Filled();
            var grid = AddGrid(inv);
            yield return GameplayPanel.Frames();
            ItemSlot activated = null;
            grid.SlotActivated += slot => activated = slot;
            var element = grid.Slots[0];
            Send.PointerDown(element, Center(element));
            Send.PointerMove(element, Center(element) + new Vector2(2f, 0f));
            Send.PointerUp(element, Center(element));
            Assert.AreSame(element, activated);
            Assert.IsFalse(grid.Pickup.IsHolding);
            Assert.AreEqual(new ItemStack("potion", 6), inv[0]);
        }

        [UnityTest]
        public IEnumerator DraggingAnEmptyOrLockedSlot_DoesNothing()
        {
            var inv = Filled();
            var grid = AddGrid(inv);
            yield return GameplayPanel.Frames();
            StartDrag(grid, 3, Center(grid.Slots[4])); // empty
            Assert.IsFalse(grid.Pickup.IsHolding);
            Drop(grid, 3, Center(grid.Slots[4]));
            inv.Set(7, new ItemStack("potion", 1));
            StartDrag(grid, 7, Center(grid.Slots[4])); // locked
            Assert.IsFalse(grid.Pickup.IsHolding);
            Drop(grid, 7, Center(grid.Slots[4]));
        }

        [UnityTest]
        public IEnumerator RightClick_RaisesSlotContext_WithThePointerPosition()
        {
            var grid = AddGrid(Filled());
            yield return GameplayPanel.Frames();
            ItemSlot slot = null;
            var position = Vector2.zero;
            grid.SlotContext += (s, p) =>
            {
                slot = s;
                position = p;
            };
            var element = grid.Slots[2];
            Send.PointerDown(element, Center(element) + new Vector2(3f, 4f), 1);
            Assert.AreSame(element, slot);
            Assert.AreEqual(Center(element).x + 3f, position.x, 0.01f);
            Assert.IsFalse(grid.Pickup.IsHolding);
        }

        [UnityTest]
        public IEnumerator DragFromOneGridToAnother_TransfersAndRaisesMovedOnTheTarget()
        {
            var a = Filled();
            var b = new InventoryModel(4, Max);
            var gridA = AddGrid(a);
            var gridB = AddGrid(b, left: 400f);
            yield return GameplayPanel.Frames();
            var movedA = 0;
            var movedB = new List<SlotMove>();
            gridA.Moved += _ => movedA++;
            gridB.Moved += movedB.Add;

            var target = Center(gridB.Slots[1]);
            StartDrag(gridA, 0, target);
            Assert.IsTrue(gridB.Slots[1].ClassListContains("fdp-item-slot--drop"), "a slot of another grid lights up");
            Drop(gridA, 0, target);
            Assert.IsTrue(a[0].IsEmpty);
            Assert.AreEqual(new ItemStack("potion", 6), b[1]);
            Assert.AreEqual(0, movedA);
            Assert.AreEqual(1, movedB.Count);
            Assert.AreSame(a, movedB[0].Source);
            Assert.AreSame(b, movedB[0].Target);
            Assert.AreEqual(6, gridB.Slots[1].Count, "the target grid redrew");
        }

        [UnityTest]
        public IEnumerator Drag_StepsWithinASlotAllocateNothing()
        {
            var inv = Filled();
            var grid = AddGrid(inv);
            yield return GameplayPanel.Frames();
            var target = Center(grid.Slots[4]);
            StartDrag(grid, 0, target);
            if (!AllocProbe.IsAvailable()) Assert.Ignore("The runtime does not report per-thread allocations.");
            // Two points inside one slot: the ghost follows the pointer and nothing else changes. (A step that enters another slot changes a USS class, and
            // UI Toolkit allocates when it does that; that is once per slot, not once per frame.)
            var a = Center(grid.Slots[2]) + new Vector2(-4f, 0f);
            var b = Center(grid.Slots[2]) + new Vector2(4f, 0f);
            grid.UpdateDrag(a);
            var flip = false;
            var bytes = AllocProbe.Measure(() =>
            {
                flip = !flip;
                grid.UpdateDrag(flip ? a : b);
            });
            Drop(grid, 0, target);
            Assert.AreEqual(0L, bytes);
        }

        // ---- keyboard and gamepad ----

        [UnityTest]
        public IEnumerator Arrows_MoveFocusBetweenSlots_ByRowsAndColumns()
        {
            var grid = AddGrid(Filled());
            yield return GameplayPanel.Frames();
            Assert.IsTrue(grid.FocusSlot(0));
            Assert.AreSame(grid.Slots[0], grid.panel.focusController.focusedElement);
            Send.Move(grid.Slots[0], NavigationMoveEvent.Direction.Right);
            Assert.AreSame(grid.Slots[1], grid.panel.focusController.focusedElement);
            Send.Move(grid.Slots[1], NavigationMoveEvent.Direction.Down);
            Assert.AreSame(grid.Slots[5], grid.panel.focusController.focusedElement);
            Send.Move(grid.Slots[5], NavigationMoveEvent.Direction.Left);
            Assert.AreSame(grid.Slots[4], grid.panel.focusController.focusedElement);
            Send.Move(grid.Slots[4], NavigationMoveEvent.Direction.Up);
            Assert.AreSame(grid.Slots[0], grid.panel.focusController.focusedElement);
            Assert.IsFalse(grid.FocusSlot(99));
        }

        [UnityTest]
        public IEnumerator SubmitPicksUp_MoveFocus_SubmitPlaces()
        {
            var inv = Filled();
            var grid = AddGrid(inv);
            yield return GameplayPanel.Frames();
            var moves = new List<SlotMove>();
            grid.Moved += moves.Add;

            Send.Submit(grid.Slots[0]);
            Assert.IsTrue(grid.Pickup.IsHolding);
            Assert.IsTrue(grid.Slots[0].ClassListContains("fdp-item-slot--held"));
            Assert.AreEqual(new ItemStack("potion", 6), inv[0], "nothing moved yet");

            Send.Submit(grid.Slots[5]);
            Assert.IsFalse(grid.Pickup.IsHolding);
            Assert.AreEqual(new ItemStack("potion", 6), inv[5]);
            Assert.IsTrue(inv[0].IsEmpty);
            Assert.AreEqual(1, moves.Count);
            Assert.IsFalse(grid.Slots[0].ClassListContains("fdp-item-slot--held"));
        }

        [UnityTest]
        public IEnumerator SubmitWithShift_PicksHalf()
        {
            var inv = Filled();
            var grid = AddGrid(inv);
            yield return GameplayPanel.Frames();
            Send.Submit(grid.Slots[0], EventModifiers.Shift);
            Assert.AreEqual(3, grid.Pickup.Held.Count);
            Send.Submit(grid.Slots[4]);
            Assert.AreEqual(3, inv[0].Count);
            Assert.AreEqual(3, inv[4].Count);
        }

        [UnityTest]
        public IEnumerator Cancel_PutsTheItemBack_AndOnlyConsumesTheEventWhileCarrying()
        {
            var inv = Filled();
            var grid = AddGrid(inv);
            yield return GameplayPanel.Frames();
            Send.Submit(grid.Slots[0]);
            Send.Cancel(grid.Slots[3]);
            Assert.IsFalse(grid.Pickup.IsHolding);
            Assert.AreEqual(new ItemStack("potion", 6), inv[0]);

            Send.Submit(grid.Slots[0]);
            Send.Key(grid.Slots[0], KeyCode.Escape);
            Assert.IsFalse(grid.Pickup.IsHolding);

            // Not carrying: the cancel is not ours, so it reaches whatever is above the grid.
            var reached = false;
            _panel.Root.RegisterCallback<NavigationCancelEvent>(_ => reached = true);
            Send.Cancel(grid.Slots[0]);
            Assert.IsTrue(reached);
        }

        [UnityTest]
        public IEnumerator SubmitOnTheSourceSlot_CancelsThePickUp()
        {
            var inv = Filled();
            var grid = AddGrid(inv);
            yield return GameplayPanel.Frames();
            Send.Submit(grid.Slots[0]);
            Send.Submit(grid.Slots[0]);
            Assert.IsFalse(grid.Pickup.IsHolding);
            Assert.AreEqual(6, inv[0].Count);
        }

        [UnityTest]
        public IEnumerator PlacingOnARefusingSlot_KeepsCarrying_UntilAValidSlotIsChosen()
        {
            var inv = new InventoryModel(8, Max, (slot, stack) => slot != 5);
            inv.Set(0, new ItemStack("potion", 6));
            var grid = AddGrid(inv);
            yield return GameplayPanel.Frames();
            var rejected = 0;
            grid.MoveRejected += _ => rejected++;
            Send.Submit(grid.Slots[0]);
            Send.Submit(grid.Slots[5]);
            Assert.IsTrue(grid.Pickup.IsHolding);
            Assert.AreEqual(1, rejected);
            Send.Submit(grid.Slots[6]);
            Assert.IsFalse(grid.Pickup.IsHolding);
            Assert.AreEqual(6, inv[6].Count);
        }

        [UnityTest]
        public IEnumerator SubmitPicksUpFalse_RaisesSlotActivatedInstead()
        {
            var inv = Filled();
            var grid = AddGrid(inv);
            grid.SubmitPicksUp = false;
            yield return GameplayPanel.Frames();
            ItemSlot activated = null;
            grid.SlotActivated += slot => activated = slot;
            Send.Submit(grid.Slots[0]);
            Assert.AreSame(grid.Slots[0], activated);
            Assert.IsFalse(grid.Pickup.IsHolding);
        }

        [UnityTest]
        public IEnumerator SubmitOnAnEmptyOrLockedSlot_RaisesSlotActivated()
        {
            var inv = Filled();
            inv.Set(6, new ItemStack("gem", 1));
            inv.SetLocked(6, true);
            var grid = AddGrid(inv);
            yield return GameplayPanel.Frames();
            var activated = new List<int>();
            grid.SlotActivated += slot => activated.Add(slot.Index);
            Send.Submit(grid.Slots[3]);
            Send.Submit(grid.Slots[6]);
            CollectionAssert.AreEqual(new[] { 3, 6 }, activated);
            Assert.IsFalse(grid.Pickup.IsHolding);
        }

        [UnityTest]
        public IEnumerator LinkedGrids_CarryAnItemFromOneToTheOther_WithTheKeyboard()
        {
            var a = Filled();
            var b = new InventoryModel(4, Max);
            var gridA = AddGrid(a);
            var gridB = AddGrid(b, left: 400f);
            SlotGrid.Link(gridA, gridB);
            yield return GameplayPanel.Frames();
            Assert.AreSame(gridA.Pickup, gridB.Pickup);

            Send.Submit(gridA.Slots[0]);
            Assert.IsTrue(gridB.Pickup.IsHolding);
            Send.Submit(gridB.Slots[2]);
            Assert.AreEqual(new ItemStack("potion", 6), b[2]);
            Assert.IsTrue(a[0].IsEmpty);
            Assert.IsFalse(gridA.Pickup.IsHolding);
        }

        [UnityTest]
        public IEnumerator Cancel_WhileCarryingInALinkedGrid_PutsItBack()
        {
            var a = Filled();
            var gridA = AddGrid(a);
            var gridB = AddGrid(new InventoryModel(4, Max), left: 400f);
            SlotGrid.Link(gridA, gridB);
            yield return GameplayPanel.Frames();
            Send.Submit(gridA.Slots[0]);
            Send.Cancel(gridB.Slots[0]);
            Assert.IsFalse(gridA.Pickup.IsHolding);
            Assert.AreEqual(6, a[0].Count);
        }

        [UnityTest]
        public IEnumerator HoveringAndFocusingASlot_RaiseEvents()
        {
            var grid = AddGrid(Filled());
            yield return GameplayPanel.Frames();
            var focused = new List<int>();
            grid.SlotFocused += slot => focused.Add(slot.Index);
            grid.FocusSlot(2);
            grid.FocusSlot(3);
            CollectionAssert.AreEqual(new[] { 2, 3 }, focused);

            ItemSlot hovered = null;
            grid.SlotHovered += slot => hovered = slot;
            var element = grid.Slots[1];
            using (var e = PointerEnterEvent.GetPooled(new Event { type = EventType.MouseMove, mousePosition = Center(element) }))
            {
                e.target = element;
                element.SendEvent(e);
            }

            Assert.AreSame(element, hovered);
        }

        [UnityTest]
        public IEnumerator ContextKey_RaisesSlotContext_AtTheSlot()
        {
            var grid = AddGrid(Filled());
            yield return GameplayPanel.Frames();
            ItemSlot slot = null;
            var position = Vector2.zero;
            grid.SlotContext += (s, p) =>
            {
                slot = s;
                position = p;
            };
            Send.Key(grid.Slots[2], KeyCode.JoystickButton3);
            Assert.AreSame(grid.Slots[2], slot);
            Assert.AreEqual(Center(grid.Slots[2]).x, position.x, 0.5f);
            slot = null;
            Send.Key(grid.Slots[1], KeyCode.Menu);
            Assert.AreSame(grid.Slots[1], slot);
        }

        [UnityTest]
        public IEnumerator FocusRing_ColoursTheBorderOnlyInNavigationMode_WithoutShiftingTheLayout()
        {
            var inv = Filled();
            var grid = AddGrid(inv, navigationMode: true);
            yield return GameplayPanel.Frames();
            var slot = grid.Slots[3];
            var rest = slot.resolvedStyle.borderTopColor;
            var restWidth = slot.resolvedStyle.borderTopWidth;
            var restY = grid.Slots[4].layout.y;
            grid.FocusSlot(3);
            yield return new WaitForSeconds(0.3f); // the border colour transitions over 120 ms
            Assert.AreNotEqual(rest, slot.resolvedStyle.borderTopColor, "the border becomes the ring colour");
            Assert.AreEqual(restWidth, slot.resolvedStyle.borderTopWidth, 0.01f, "the ring does not change the border width");
            Assert.AreEqual(restY, grid.Slots[4].layout.y, 0.01f, "nothing moved");

            _panel.Root.RemoveFromClassList("fdp-input-nav");
            yield return new WaitForSeconds(0.3f);
            Assert.AreEqual(rest, slot.resolvedStyle.borderTopColor, "no ring after pointer input");
        }
    }
}
