using System;

namespace FutureDigitalPlus.UIKit.Gameplay
{
    /// <summary>
    /// The "pick up, carry, put down" state shared by pointer dragging and keyboard or gamepad use. Nothing leaves its
    /// slot while an item is carried: picking up only remembers where the items are and how many, and
    /// <see cref="Place"/> performs the move. So cancelling, or dropping on something that refuses the items, leaves
    /// everything as it was. One instance can serve several grids, which is what allows carrying an item from one
    /// inventory to another.
    /// </summary>
    public sealed class PickupMode
    {
        /// <summary>True while a stack is picked up.</summary>
        public bool IsHolding { get; private set; }

        /// <summary>The inventory the carried items come from (null when not holding).</summary>
        public InventoryModel Source { get; private set; }

        /// <summary>The slot the carried items come from (-1 when not holding).</summary>
        public int SourceSlot { get; private set; } = -1;

        /// <summary>How many items were asked for when picking up; 0 means the whole stack.</summary>
        public int RequestedCount { get; private set; }

        /// <summary>The stack being carried as it is now (empty when not holding).</summary>
        public ItemStack Held
        {
            get
            {
                if (!IsHolding) return ItemStack.Empty;
                var stack = Source[SourceSlot];
                return RequestedCount > 0 && RequestedCount < stack.Count ? stack.WithCount(RequestedCount) : stack;
            }
        }

        /// <summary>Raised when the holding state changes (picked up, put down, cancelled).</summary>
        public event Action Changed;

        /// <summary>Picks up the stack in a slot, or only <paramref name="count"/> items of it.</summary>
        /// <returns>False when already holding, the slot is empty or locked, or outside the inventory.</returns>
        public bool Pick(InventoryModel model, int slot, int count = 0)
        {
            if (IsHolding || model == null || !model.IsValidSlot(slot) || model[slot].IsEmpty || model.IsLocked(slot)) return false;
            Source = model;
            SourceSlot = slot;
            RequestedCount = count;
            IsHolding = true;
            Changed?.Invoke();
            return true;
        }

        /// <summary>Picks up the larger half of the stack in a slot.</summary>
        public bool PickHalf(InventoryModel model, int slot)
        {
            if (model == null || !model.IsValidSlot(slot)) return false;
            return Pick(model, slot, (model[slot].Count + 1) / 2);
        }

        /// <summary>
        /// Puts the carried items into a slot. On success the mode is idle again. Placing on the slot the items came from
        /// cancels. A refused placement keeps carrying (so the player can try another slot) and returns the reason.
        /// </summary>
        public MoveResult Place(InventoryModel target, int slot)
        {
            if (!IsHolding) return new MoveResult(MoveReason.NotHolding, 0);
            if (ReferenceEquals(target, Source) && slot == SourceSlot)
            {
                Cancel();
                return new MoveResult(MoveReason.SameSlot, 0);
            }

            var result = InventoryModel.Transfer(Source, SourceSlot, target, slot, RequestedCount);
            if (result.Success || result.Reason == MoveReason.SourceEmpty) Reset();
            return result;
        }

        /// <summary>Puts the carried items back (nothing moved, so this only ends the mode).</summary>
        public void Cancel()
        {
            if (!IsHolding) return;
            Reset();
        }

        private void Reset()
        {
            Source = null;
            SourceSlot = -1;
            RequestedCount = 0;
            IsHolding = false;
            Changed?.Invoke();
        }
    }
}
