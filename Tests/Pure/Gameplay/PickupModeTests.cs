using NUnit.Framework;

namespace FutureDigitalPlus.UIKit.Gameplay.Tests
{
    public class PickupModeTests
    {
        private static InventoryModel Make() => new InventoryModel(4, id => 10);

        [Test]
        public void PickThenPlace_MovesTheStackAndEndsTheMode()
        {
            var inv = Make();
            inv.Set(0, new ItemStack("potion", 4));
            var mode = new PickupMode();
            var changes = 0;
            mode.Changed += () => changes++;

            Assert.IsTrue(mode.Pick(inv, 0));
            Assert.IsTrue(mode.IsHolding);
            Assert.AreEqual(new ItemStack("potion", 4), mode.Held);
            Assert.AreEqual(new ItemStack("potion", 4), inv[0], "nothing leaves the slot while carried");

            var r = mode.Place(inv, 2);
            Assert.AreEqual(MoveReason.Moved, r.Reason);
            Assert.IsFalse(mode.IsHolding);
            Assert.AreEqual(new ItemStack("potion", 4), inv[2]);
            Assert.AreEqual(2, changes);
        }

        [Test]
        public void PickHalf_CarriesTheLargerHalf()
        {
            var inv = Make();
            inv.Set(0, new ItemStack("potion", 5));
            var mode = new PickupMode();
            Assert.IsTrue(mode.PickHalf(inv, 0));
            Assert.AreEqual(3, mode.Held.Count);
            mode.Place(inv, 1);
            Assert.AreEqual(2, inv[0].Count);
            Assert.AreEqual(3, inv[1].Count);
        }

        [Test]
        public void Pick_Fails_ForEmptyLockedInvalidOrWhenAlreadyHolding()
        {
            var inv = Make();
            inv.Set(0, new ItemStack("potion", 1));
            inv.Set(1, new ItemStack("potion", 1));
            inv.SetLocked(1, true);
            var mode = new PickupMode();
            Assert.IsFalse(mode.Pick(inv, 2), "empty");
            Assert.IsFalse(mode.Pick(inv, 1), "locked");
            Assert.IsFalse(mode.Pick(inv, 9), "invalid");
            Assert.IsFalse(mode.Pick(null, 0), "no model");
            Assert.IsFalse(mode.PickHalf(inv, 9));
            Assert.IsTrue(mode.Pick(inv, 0));
            Assert.IsFalse(mode.Pick(inv, 0), "already holding");
        }

        [Test]
        public void PlaceOnTheSourceSlot_Cancels()
        {
            var inv = Make();
            inv.Set(0, new ItemStack("potion", 3));
            var mode = new PickupMode();
            mode.Pick(inv, 0);
            var r = mode.Place(inv, 0);
            Assert.AreEqual(MoveReason.SameSlot, r.Reason);
            Assert.IsFalse(mode.IsHolding);
            Assert.AreEqual(3, inv[0].Count);
        }

        [Test]
        public void RefusedPlacement_KeepsCarrying_AndCancelRestoresNothingToDo()
        {
            var inv = new InventoryModel(3, id => 10, (slot, stack) => slot != 2);
            inv.Set(0, new ItemStack("potion", 3));
            var mode = new PickupMode();
            mode.Pick(inv, 0);
            var r = mode.Place(inv, 2);
            Assert.AreEqual(MoveReason.Rejected, r.Reason);
            Assert.IsTrue(mode.IsHolding, "the player may try another slot");
            Assert.AreEqual(MoveReason.Moved, mode.Place(inv, 1).Reason);

            inv.Set(0, new ItemStack("potion", 2));
            mode.Pick(inv, 0);
            mode.Cancel();
            Assert.IsFalse(mode.IsHolding);
            Assert.AreEqual(2, inv[0].Count);
        }

        [Test]
        public void Place_WhenNotHolding_ReportsIt()
        {
            var mode = new PickupMode();
            Assert.AreEqual(MoveReason.NotHolding, mode.Place(Make(), 0).Reason);
            mode.Cancel(); // harmless
            Assert.IsTrue(mode.Held.IsEmpty);
        }

        [Test]
        public void Place_IntoAnotherInventory_Transfers()
        {
            var a = Make();
            var b = Make();
            a.Set(1, new ItemStack("gem", 2));
            var mode = new PickupMode();
            mode.Pick(a, 1);
            Assert.AreEqual(MoveReason.Moved, mode.Place(b, 0).Reason);
            Assert.IsTrue(a[1].IsEmpty);
            Assert.AreEqual(2, b[0].Count);
        }

        [Test]
        public void SourceThatEmptiedWhileCarrying_EndsTheModeOnPlace()
        {
            var inv = Make();
            inv.Set(0, new ItemStack("potion", 3));
            var mode = new PickupMode();
            mode.Pick(inv, 0);
            inv.Set(0, ItemStack.Empty);
            Assert.IsTrue(mode.Held.IsEmpty);
            Assert.AreEqual(MoveReason.SourceEmpty, mode.Place(inv, 1).Reason);
            Assert.IsFalse(mode.IsHolding);
        }

        [Test]
        public void Held_FollowsTheStackThatGrowsOrShrinks()
        {
            var inv = Make();
            inv.Set(0, new ItemStack("potion", 8));
            var mode = new PickupMode();
            mode.Pick(inv, 0, 3);
            Assert.AreEqual(3, mode.Held.Count);
            inv.Set(0, new ItemStack("potion", 2));
            Assert.AreEqual(2, mode.Held.Count, "never more than the slot holds");
        }
    }
}
