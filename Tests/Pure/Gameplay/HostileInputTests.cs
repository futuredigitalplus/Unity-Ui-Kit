using NUnit.Framework;

namespace FutureDigitalPlus.UIKit.Gameplay.Tests
{
    /// <summary>The inventory model asked about items that do not exist, with ids a game forgot to fill in, and with numbers at the edge of int.</summary>
    public class HostileInputTests
    {
        [Test]
        public void NullOrEmptyItemId_MatchesNothing_AndRaisesNoEvents()
        {
            var inventory = new InventoryModel(4);
            inventory.Add(new ItemStack("sword", 1));
            var events = 0;
            inventory.SlotChanged += slot => events++;

            foreach (var id in new string[] { null, "" })
            {
                Assert.AreEqual(0, inventory.CountOf(id));
                Assert.AreEqual(-1, inventory.FirstSlotOf(id), "an empty slot is not the slot of 'no item'");
                Assert.AreEqual(0, inventory.Remove(id, 5));
            }

            Assert.AreEqual(0, events, "removing nothing changes nothing");
            Assert.AreEqual(0, inventory.Remove("sword", -3));
            Assert.AreEqual(1, inventory.CountOf("sword"));
        }

        [Test]
        public void HugeStacksAndCounts_NeverOverflowIntoNegativeStacks()
        {
            var inventory = new InventoryModel(2);
            Assert.AreEqual(0, inventory.Add(new ItemStack("gold", int.MaxValue)));
            var left = inventory.Add(new ItemStack("gold", int.MaxValue));
            Assert.AreEqual(0, left);
            for (var i = 0; i < inventory.Capacity; i++) Assert.GreaterOrEqual(inventory[i].Count, 0);

            var small = new InventoryModel(1, id => 10);
            Assert.AreEqual(int.MaxValue - 10, small.Add(new ItemStack("ore", int.MaxValue)));
            Assert.AreEqual(10, small[0].Count);
            Assert.AreEqual(10, small.Remove("ore", int.MaxValue));
            Assert.IsTrue(small[0].IsEmpty);
        }

        [Test]
        public void RuleCallbacksAndCapacity_AreCheckedOnTheEdges()
        {
            Assert.Throws<System.ArgumentOutOfRangeException>(() => new InventoryModel(0));
            Assert.Throws<System.ArgumentOutOfRangeException>(() => new InventoryModel(-5));
            var inventory = new InventoryModel(2, id => -7, (slot, stack) => false);
            Assert.AreEqual(1, inventory.MaxStackOf("x"), "a rule that says 0 or less still allows one");
            Assert.AreEqual(3, inventory.Add(new ItemStack("x", 3)), "nothing is accepted: everything is left over");
            Assert.AreEqual(MoveReason.InvalidSlot, inventory.Move(-1, 0).Reason);
            Assert.AreEqual(MoveReason.InvalidSlot, inventory.Move(0, 99).Reason);
            Assert.AreEqual(MoveReason.InvalidSlot, inventory.MoveHalf(int.MinValue, 0).Reason);
            Assert.Throws<System.ArgumentNullException>(() => InventoryModel.Transfer(null, 0, inventory, 0));
        }

        [Test]
        public void ChangeHandlerThatEditsTheInventory_DoesNotCorruptTheOperation()
        {
            var inventory = new InventoryModel(3);
            inventory.Add(new ItemStack("a", 5));
            var reentered = false;
            inventory.SlotChanged += slot =>
            {
                if (reentered) return;
                reentered = true;
                inventory.Add(new ItemStack("b", 1));
            };

            var result = inventory.Move(0, 2);

            Assert.IsTrue(result.Success);
            Assert.AreEqual(5, inventory.CountOf("a"));
            Assert.AreEqual(1, inventory.CountOf("b"));
        }
    }
}
