using System;
using System.Collections.Generic;
using NUnit.Framework;

namespace FutureDigitalPlus.UIKit.Gameplay.Tests
{
    public class InventoryModelTests
    {
        private static int Max(string id) => id == "potion" ? 10 : id == "sword" ? 1 : 99;

        private static InventoryModel Make(int capacity = 6) => new InventoryModel(capacity, Max);

        [Test]
        public void ItemStack_IsEmptyWithoutIdOrCount()
        {
            Assert.IsTrue(new ItemStack(null, 5).IsEmpty);
            Assert.IsTrue(new ItemStack("", 5).IsEmpty);
            Assert.IsTrue(new ItemStack("a", 0).IsEmpty);
            Assert.IsTrue(new ItemStack("a", -2).IsEmpty);
            Assert.IsTrue(ItemStack.Empty.IsEmpty);
            Assert.AreEqual(ItemStack.Empty, new ItemStack("a", 0));
            Assert.AreEqual(new ItemStack("a", 2), new ItemStack("a", 2));
            Assert.AreNotEqual(new ItemStack("a", 2), new ItemStack("a", 3));
            Assert.IsTrue(new ItemStack("a", 2).SameItem(new ItemStack("a", 9)));
            Assert.IsTrue(new ItemStack("a", 2).WithCount(0).IsEmpty);
        }

        [Test]
        public void Move_ToAnEmptySlot_MovesTheWholeStack()
        {
            var inv = Make();
            inv.Set(0, new ItemStack("potion", 4));
            var r = inv.Move(0, 3);
            Assert.AreEqual(MoveReason.Moved, r.Reason);
            Assert.AreEqual(4, r.MovedCount);
            Assert.IsTrue(inv[0].IsEmpty);
            Assert.AreEqual(new ItemStack("potion", 4), inv[3]);
        }

        [Test]
        public void Move_WithACount_Splits()
        {
            var inv = Make();
            inv.Set(0, new ItemStack("potion", 7));
            var r = inv.Move(0, 1, 3);
            Assert.AreEqual(MoveReason.Moved, r.Reason);
            Assert.AreEqual(new ItemStack("potion", 4), inv[0]);
            Assert.AreEqual(new ItemStack("potion", 3), inv[1]);
        }

        [Test]
        public void Move_CountAboveTheStack_MovesTheWholeStack()
        {
            var inv = Make();
            inv.Set(0, new ItemStack("potion", 2));
            Assert.AreEqual(2, inv.Move(0, 1, 50).MovedCount);
            Assert.IsTrue(inv[0].IsEmpty);
        }

        [Test]
        public void Move_ToAnEmptySlot_IsCappedByTheStackLimit()
        {
            var inv = Make();
            inv.Set(0, new ItemStack("potion", 25)); // set bypasses the limit (a save from an older version)
            var r = inv.Move(0, 1);
            Assert.AreEqual(MoveReason.Moved, r.Reason);
            Assert.AreEqual(10, r.MovedCount);
            Assert.AreEqual(new ItemStack("potion", 15), inv[0]);
            Assert.AreEqual(new ItemStack("potion", 10), inv[1]);
        }

        [Test]
        public void Move_OntoTheSameKind_Merges()
        {
            var inv = Make();
            inv.Set(0, new ItemStack("potion", 3));
            inv.Set(1, new ItemStack("potion", 4));
            var r = inv.Move(0, 1);
            Assert.AreEqual(MoveReason.Merged, r.Reason);
            Assert.AreEqual(3, r.MovedCount);
            Assert.IsTrue(inv[0].IsEmpty);
            Assert.AreEqual(7, inv[1].Count);
        }

        [Test]
        public void Move_OntoTheSameKind_PartialMergeLeavesTheRest()
        {
            var inv = Make();
            inv.Set(0, new ItemStack("potion", 8));
            inv.Set(1, new ItemStack("potion", 7));
            var r = inv.Move(0, 1);
            Assert.AreEqual(MoveReason.Merged, r.Reason);
            Assert.AreEqual(3, r.MovedCount);
            Assert.AreEqual(5, inv[0].Count);
            Assert.AreEqual(10, inv[1].Count);
        }

        [Test]
        public void Move_OntoAFullStackOfTheSameKind_Fails()
        {
            var inv = Make();
            inv.Set(0, new ItemStack("potion", 2));
            inv.Set(1, new ItemStack("potion", 10));
            var r = inv.Move(0, 1);
            Assert.AreEqual(MoveReason.TargetFull, r.Reason);
            Assert.IsFalse(r.Success);
            Assert.AreEqual(2, inv[0].Count);
        }

        [Test]
        public void Move_OntoAnotherKind_Swaps()
        {
            var inv = Make();
            inv.Set(0, new ItemStack("potion", 2));
            inv.Set(1, new ItemStack("sword", 1));
            var r = inv.Move(0, 1);
            Assert.AreEqual(MoveReason.Swapped, r.Reason);
            Assert.AreEqual(new ItemStack("sword", 1), inv[0]);
            Assert.AreEqual(new ItemStack("potion", 2), inv[1]);
        }

        [Test]
        public void Move_PartOfAStackOntoAnotherKind_Fails()
        {
            var inv = Make();
            inv.Set(0, new ItemStack("potion", 5));
            inv.Set(1, new ItemStack("sword", 1));
            var r = inv.Move(0, 1, 2);
            Assert.AreEqual(MoveReason.CannotSplitIntoOccupied, r.Reason);
            Assert.AreEqual(5, inv[0].Count);
            Assert.AreEqual(new ItemStack("sword", 1), inv[1]);
        }

        [Test]
        public void Swap_ThatWouldBreakAStackLimit_Fails()
        {
            var inv = new InventoryModel(2, id => id == "big" ? 5 : 1);
            inv.Set(0, new ItemStack("big", 4));
            inv.Set(1, new ItemStack("small", 1));
            Assert.AreEqual(MoveReason.Swapped, inv.Move(0, 1).Reason, "both stacks fit the other slot");

            var over = new InventoryModel(2, id => 1);
            over.Set(0, new ItemStack("small", 1));
            over.Set(1, new ItemStack("huge", 3)); // already above its limit of 1: set bypasses limits
            Assert.AreEqual(MoveReason.StackLimit, over.Move(0, 1).Reason);
            Assert.AreEqual(new ItemStack("huge", 3), over[1]);
            Assert.AreEqual(new ItemStack("small", 1), over[0]);
        }

        [Test]
        public void InvalidMoves_FailWithAReasonAndChangeNothing()
        {
            var inv = Make(3);
            inv.Set(0, new ItemStack("potion", 2));
            Assert.AreEqual(MoveReason.InvalidSlot, inv.Move(0, 3).Reason);
            Assert.AreEqual(MoveReason.InvalidSlot, inv.Move(-1, 1).Reason);
            Assert.AreEqual(MoveReason.SameSlot, inv.Move(0, 0).Reason);
            Assert.AreEqual(MoveReason.SourceEmpty, inv.Move(1, 2).Reason);
            Assert.AreEqual(new ItemStack("potion", 2), inv[0]);
        }

        [Test]
        public void LockedSlots_RefuseMovesInAndOut()
        {
            var inv = Make();
            inv.Set(0, new ItemStack("potion", 2));
            inv.SetLocked(1, true);
            Assert.AreEqual(MoveReason.Locked, inv.Move(0, 1).Reason);
            inv.Set(1, new ItemStack("potion", 1));
            Assert.AreEqual(MoveReason.Locked, inv.Move(1, 2).Reason);
            inv.SetLocked(1, false);
            Assert.AreEqual(MoveReason.Merged, inv.Move(0, 1).Reason);
        }

        [Test]
        public void RuleCallback_CanVetoAPlacement()
        {
            var inv = new InventoryModel(4, Max, (slot, stack) => slot != 2 || stack.ItemId == "sword");
            inv.Set(0, new ItemStack("potion", 2));
            inv.Set(1, new ItemStack("sword", 1));
            Assert.AreEqual(MoveReason.Rejected, inv.Move(0, 2).Reason);
            Assert.AreEqual(2, inv[0].Count);
            Assert.AreEqual(MoveReason.Moved, inv.Move(1, 2).Reason);
            // swap out of the restricted slot: the potion would have to go into slot 2 -> refused
            Assert.AreEqual(MoveReason.Rejected, inv.Move(0, 2).Reason);
            Assert.AreEqual(new ItemStack("sword", 1), inv[2]);
        }

        [Test]
        public void RuleCallback_SeesTheMergedStack()
        {
            var seen = new List<ItemStack>();
            var inv = new InventoryModel(2, Max, (slot, stack) =>
            {
                seen.Add(stack);
                return true;
            });
            inv.Set(0, new ItemStack("potion", 3));
            inv.Set(1, new ItemStack("potion", 4));
            inv.Move(0, 1);
            Assert.AreEqual(new ItemStack("potion", 7), seen[seen.Count - 1]);
        }

        [Test]
        public void Transfer_BetweenInventories_UsesTheTargetsRulesAndLimits()
        {
            var a = Make();
            var b = new InventoryModel(3, id => 5);
            a.Set(0, new ItemStack("potion", 8));
            var r = InventoryModel.Transfer(a, 0, b, 1);
            Assert.AreEqual(MoveReason.Moved, r.Reason);
            Assert.AreEqual(5, r.MovedCount);
            Assert.AreEqual(3, a[0].Count);
            Assert.AreEqual(5, b[1].Count);
        }

        [Test]
        public void Transfer_SwapBetweenInventories_ChecksBothSides()
        {
            var a = new InventoryModel(2, Max, (slot, stack) => stack.ItemId == "potion");
            var b = new InventoryModel(2, Max);
            a.Set(0, new ItemStack("potion", 1));
            b.Set(0, new ItemStack("sword", 1));
            Assert.AreEqual(MoveReason.Rejected, InventoryModel.Transfer(a, 0, b, 0).Reason);
            Assert.AreEqual(MoveReason.Rejected, InventoryModel.Transfer(b, 0, a, 0).Reason);
        }

        [Test]
        public void MoveHalf_TakesTheLargerHalf()
        {
            var inv = Make();
            inv.Set(0, new ItemStack("potion", 7));
            Assert.AreEqual(MoveReason.Moved, inv.MoveHalf(0, 1).Reason);
            Assert.AreEqual(3, inv[0].Count);
            Assert.AreEqual(4, inv[1].Count);
            inv.Set(2, new ItemStack("potion", 1));
            Assert.AreEqual(MoveReason.Moved, inv.MoveHalf(2, 3).Reason);
            Assert.IsTrue(inv[2].IsEmpty);
            Assert.AreEqual(MoveReason.InvalidSlot, inv.MoveHalf(9, 0).Reason);
        }

        [Test]
        public void Add_FillsStacksFirstThenEmptySlots_AndReportsTheLeftover()
        {
            var inv = new InventoryModel(3, Max);
            inv.Set(1, new ItemStack("potion", 8));
            var leftover = inv.Add(new ItemStack("potion", 15));
            Assert.AreEqual(0, leftover);
            Assert.AreEqual(10, inv[1].Count);
            Assert.AreEqual(10, inv[0].Count);
            Assert.AreEqual(3, inv[2].Count);

            Assert.AreEqual(7, inv.Add(new ItemStack("potion", 14)), "7 fit into slot 2, 7 do not");
            Assert.AreEqual(0, inv.Add(ItemStack.Empty));
        }

        [Test]
        public void Add_SkipsLockedSlotsAndVetoedSlots()
        {
            var inv = new InventoryModel(3, Max, (slot, stack) => slot != 1);
            inv.SetLocked(0, true);
            Assert.AreEqual(0, inv.Add(new ItemStack("sword", 1)));
            Assert.IsTrue(inv[0].IsEmpty);
            Assert.IsTrue(inv[1].IsEmpty);
            Assert.AreEqual(new ItemStack("sword", 1), inv[2]);
            Assert.AreEqual(1, inv.Add(new ItemStack("sword", 1)), "no slot left");
        }

        [Test]
        public void Remove_TakesFromTheFirstSlotsAndSkipsLockedOnes()
        {
            var inv = Make();
            inv.Set(0, new ItemStack("potion", 3));
            inv.Set(1, new ItemStack("potion", 4));
            inv.SetLocked(0, true);
            Assert.AreEqual(4, inv.Remove("potion", 10));
            Assert.AreEqual(3, inv.CountOf("potion"));
            Assert.AreEqual(0, inv.Remove("nothing", 3));
        }

        [Test]
        public void CountOfAndFirstSlotOf()
        {
            var inv = Make();
            inv.Set(2, new ItemStack("potion", 3));
            inv.Set(4, new ItemStack("potion", 4));
            Assert.AreEqual(7, inv.CountOf("potion"));
            Assert.AreEqual(2, inv.FirstSlotOf("potion"));
            Assert.AreEqual(-1, inv.FirstSlotOf("sword"));
        }

        [Test]
        public void SlotChanged_FiresOncePerChangedSlot()
        {
            var inv = Make();
            inv.Set(0, new ItemStack("potion", 3));
            var changed = new List<int>();
            inv.SlotChanged += changed.Add;
            inv.Move(0, 2);
            CollectionAssert.AreEquivalent(new[] { 0, 2 }, changed);
            changed.Clear();
            inv.Move(5, 1); // fails: no event
            Assert.IsEmpty(changed);
            inv.Set(2, inv[2]); // same content: no event
            Assert.IsEmpty(changed);
            inv.SetLocked(2, true);
            CollectionAssert.AreEqual(new[] { 2 }, changed);
        }

        [Test]
        public void Constructor_RejectsNoSlots()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => new InventoryModel(0));
        }

        [Test]
        public void Transfer_RejectsNullInventories()
        {
            Assert.Throws<ArgumentNullException>(() => InventoryModel.Transfer(null, 0, Make(), 0));
            Assert.Throws<ArgumentNullException>(() => InventoryModel.Transfer(Make(), 0, null, 0));
        }

        // ---- property style: random operations never create or destroy items and never break a limit ----

        [Test]
        public void RandomMoves_ConserveItemsAndRespectStackLimits()
        {
            var rng = new Random(1234);
            var kinds = new[] { "potion", "sword", "gem" };
            for (var round = 0; round < 40; round++)
            {
                var a = new InventoryModel(8, Max);
                var b = new InventoryModel(5, Max);
                var total = new Dictionary<string, int>();
                for (var i = 0; i < 8; i++)
                {
                    var id = kinds[rng.Next(kinds.Length)];
                    var count = 1 + rng.Next(Max(id));
                    if (!a[i].IsEmpty || rng.Next(3) == 0) continue;
                    a.Set(i, new ItemStack(id, count));
                    total[id] = (total.TryGetValue(id, out var t) ? t : 0) + count;
                }

                for (var step = 0; step < 200; step++)
                {
                    var src = rng.Next(2) == 0 ? a : b;
                    var dst = rng.Next(2) == 0 ? a : b;
                    var from = rng.Next(src.Capacity);
                    var to = rng.Next(dst.Capacity);
                    var count = rng.Next(4) == 0 ? 1 + rng.Next(6) : 0;
                    var before = src[from];
                    var result = InventoryModel.Transfer(src, from, dst, to, count);
                    if (!result.Success)
                    {
                        Assert.AreEqual(before, src[from], "a failed move changes nothing");
                        Assert.AreEqual(0, result.MovedCount);
                    }

                    foreach (var model in new[] { a, b })
                        for (var s = 0; s < model.Capacity; s++)
                            Assert.LessOrEqual(model[s].Count, Max(model[s].ItemId ?? "x"), "stack limit of " + model[s]);

                    foreach (var pair in total)
                        Assert.AreEqual(pair.Value, a.CountOf(pair.Key) + b.CountOf(pair.Key), "items of " + pair.Key + " are conserved");
                }
            }
        }
    }
}
