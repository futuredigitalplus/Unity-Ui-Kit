using System;

namespace FutureDigitalPlus.UIKit.Gameplay
{
    /// <summary>Why a move succeeded or failed. Every value up to <see cref="Swapped"/> is a success.</summary>
    public enum MoveReason
    {
        /// <summary>Items went into an empty slot.</summary>
        Moved,

        /// <summary>Items were added to a stack of the same kind.</summary>
        Merged,

        /// <summary>A whole stack and the stack in the target slot traded places.</summary>
        Swapped,

        /// <summary>A slot number is outside the inventory.</summary>
        InvalidSlot,

        /// <summary>The source and the target are the same slot.</summary>
        SameSlot,

        /// <summary>The source slot holds nothing.</summary>
        SourceEmpty,

        /// <summary>The source or the target slot is locked.</summary>
        Locked,

        /// <summary>The rule callback of an inventory refused the stack.</summary>
        Rejected,

        /// <summary>The target stack is at its limit, nothing could be merged.</summary>
        TargetFull,

        /// <summary>A swap would put more items into a slot than that kind of item may stack.</summary>
        StackLimit,

        /// <summary>Part of a stack cannot be dropped onto a stack of another kind (a swap needs the whole stack).</summary>
        CannotSplitIntoOccupied,

        /// <summary>A <see cref="PickupMode"/> was asked to place while it was holding nothing.</summary>
        NotHolding,
    }

    /// <summary>The outcome of a move between slots.</summary>
    public readonly struct MoveResult
    {
        /// <summary>Creates a result.</summary>
        public MoveResult(MoveReason reason, int movedCount)
        {
            Reason = reason;
            MovedCount = movedCount;
        }

        /// <summary>What happened.</summary>
        public MoveReason Reason { get; }

        /// <summary>How many items left the source slot (for a swap, the size of the stack that moved).</summary>
        public int MovedCount { get; }

        /// <summary>True when something moved.</summary>
        public bool Success => Reason <= MoveReason.Swapped;

        /// <inheritdoc />
        public override string ToString() => Reason + " (" + MovedCount + ")";
    }

    /// <summary>
    /// A fixed number of slots holding <see cref="ItemStack"/>s, with the rules of moving things between them: merging
    /// into stacks up to a limit per kind of item, swapping, splitting, locked slots, a drop rule callback, and moves
    /// between two inventories. The model has no idea what an item is; it only needs the stack limit of an id and,
    /// optionally, a callback that vetoes a stack in a slot (an equipment slot that takes only helmets).
    /// </summary>
    public sealed class InventoryModel
    {
        private readonly ItemStack[] _slots;
        private readonly bool[] _locked;
        private readonly Func<string, int> _maxStackOf;
        private readonly Func<int, ItemStack, bool> _canPlace;

        /// <summary>Creates an empty inventory.</summary>
        /// <param name="capacity">Number of slots (at least 1).</param>
        /// <param name="maxStackOf">The stack limit of an item id (1 or more); null means unlimited.</param>
        /// <param name="canPlace">Called with a slot and the stack that would be in it; return false to veto. Null accepts everything.</param>
        public InventoryModel(int capacity, Func<string, int> maxStackOf = null, Func<int, ItemStack, bool> canPlace = null)
        {
            if (capacity < 1) throw new ArgumentOutOfRangeException(nameof(capacity), "An inventory needs at least one slot.");
            _slots = new ItemStack[capacity];
            _locked = new bool[capacity];
            _maxStackOf = maxStackOf;
            _canPlace = canPlace;
        }

        /// <summary>Raised with the slot number after the content or the locked state of a slot changed.</summary>
        public event Action<int> SlotChanged;

        /// <summary>Number of slots.</summary>
        public int Capacity => _slots.Length;

        /// <summary>The stack in a slot.</summary>
        public ItemStack this[int slot] => _slots[slot];

        /// <summary>True when the slot number is inside the inventory.</summary>
        public bool IsValidSlot(int slot) => slot >= 0 && slot < _slots.Length;

        /// <summary>True when the slot is locked: nothing can be moved into it or out of it.</summary>
        public bool IsLocked(int slot) => _locked[slot];

        /// <summary>Locks or unlocks a slot (the content stays).</summary>
        public void SetLocked(int slot, bool locked)
        {
            if (_locked[slot] == locked) return;
            _locked[slot] = locked;
            SlotChanged?.Invoke(slot);
        }

        /// <summary>The stack limit of an item kind (never below 1).</summary>
        public int MaxStackOf(string itemId)
        {
            if (_maxStackOf == null) return int.MaxValue;
            return Math.Max(1, _maxStackOf(itemId));
        }

        /// <summary>Puts a stack into a slot directly, ignoring locks, limits and the rule callback (for loading a save or granting an item).</summary>
        public void Set(int slot, ItemStack stack)
        {
            if (_slots[slot] == stack) return;
            _slots[slot] = stack;
            SlotChanged?.Invoke(slot);
        }

        /// <summary>The number of items of a kind in the whole inventory.</summary>
        public int CountOf(string itemId)
        {
            if (string.IsNullOrEmpty(itemId)) return 0;
            var total = 0;
            for (var i = 0; i < _slots.Length; i++)
                if (string.Equals(_slots[i].ItemId, itemId, StringComparison.Ordinal)) total += _slots[i].Count;
            return total;
        }

        /// <summary>The first slot holding the kind of item, or -1.</summary>
        public int FirstSlotOf(string itemId)
        {
            if (string.IsNullOrEmpty(itemId)) return -1;
            for (var i = 0; i < _slots.Length; i++)
                if (string.Equals(_slots[i].ItemId, itemId, StringComparison.Ordinal)) return i;
            return -1;
        }

        /// <summary>
        /// Adds items: first to stacks of the same kind that have room, then to empty slots, skipping locked slots and
        /// slots the rule callback refuses.
        /// </summary>
        /// <returns>How many items did not fit (0 when everything was added).</returns>
        public int Add(ItemStack stack)
        {
            if (stack.IsEmpty) return 0;
            var remaining = stack.Count;
            var max = MaxStackOf(stack.ItemId);

            for (var i = 0; i < _slots.Length && remaining > 0; i++)
            {
                var existing = _slots[i];
                if (_locked[i] || existing.IsEmpty || !existing.SameItem(stack) || existing.Count >= max) continue;
                var take = Math.Min(remaining, max - existing.Count);
                var merged = existing.WithCount(existing.Count + take);
                if (!AcceptsByRule(i, merged)) continue;
                _slots[i] = merged;
                remaining -= take;
                SlotChanged?.Invoke(i);
            }

            for (var i = 0; i < _slots.Length && remaining > 0; i++)
            {
                if (_locked[i] || !_slots[i].IsEmpty) continue;
                var take = Math.Min(remaining, max);
                var placed = stack.WithCount(take);
                if (!AcceptsByRule(i, placed)) continue;
                _slots[i] = placed;
                remaining -= take;
                SlotChanged?.Invoke(i);
            }

            return remaining;
        }

        /// <summary>Removes up to <paramref name="count"/> items of a kind, from the first slots that hold it (locked slots are skipped).</summary>
        /// <returns>How many items were removed.</returns>
        public int Remove(string itemId, int count)
        {
            if (string.IsNullOrEmpty(itemId) || count <= 0) return 0;
            var removed = 0;
            for (var i = 0; i < _slots.Length && removed < count; i++)
            {
                var existing = _slots[i];
                if (_locked[i] || !string.Equals(existing.ItemId, itemId, StringComparison.Ordinal)) continue;
                var take = Math.Min(count - removed, existing.Count);
                _slots[i] = existing.WithCount(existing.Count - take);
                removed += take;
                SlotChanged?.Invoke(i);
            }

            return removed;
        }

        /// <summary>Moves items from one slot of this inventory to another: see <see cref="Transfer"/>.</summary>
        public MoveResult Move(int from, int to, int count = 0) => Transfer(this, from, this, to, count);

        /// <summary>Moves the larger half of a stack (a stack of one moves whole): see <see cref="Transfer"/>.</summary>
        public MoveResult MoveHalf(int from, int to)
        {
            if (!IsValidSlot(from)) return new MoveResult(MoveReason.InvalidSlot, 0);
            return Transfer(this, from, this, to, (_slots[from].Count + 1) / 2);
        }

        /// <summary>
        /// Moves items from a slot of one inventory to a slot of another (or the same) inventory.
        /// An empty target takes the items (up to the stack limit; the rest stays). A target of the same kind takes as
        /// many as fit. A target of another kind swaps places with the whole source stack. Locked slots, slots the
        /// rule callback refuses and stacks that would exceed a limit fail with a reason and change nothing.
        /// </summary>
        /// <param name="source">The inventory to take from.</param>
        /// <param name="from">The source slot.</param>
        /// <param name="target">The inventory to put into.</param>
        /// <param name="to">The target slot.</param>
        /// <param name="count">How many to move; 0 or more than the stack moves the whole stack.</param>
        public static MoveResult Transfer(InventoryModel source, int from, InventoryModel target, int to, int count = 0)
        {
            if (source == null) throw new ArgumentNullException(nameof(source));
            if (target == null) throw new ArgumentNullException(nameof(target));
            if (!source.IsValidSlot(from) || !target.IsValidSlot(to)) return new MoveResult(MoveReason.InvalidSlot, 0);
            if (ReferenceEquals(source, target) && from == to) return new MoveResult(MoveReason.SameSlot, 0);

            var moving = source._slots[from];
            if (moving.IsEmpty) return new MoveResult(MoveReason.SourceEmpty, 0);
            if (source._locked[from] || target._locked[to]) return new MoveResult(MoveReason.Locked, 0);

            var wanted = count <= 0 || count >= moving.Count ? moving.Count : count;
            var whole = wanted == moving.Count;
            var occupant = target._slots[to];
            var max = target.MaxStackOf(moving.ItemId);

            if (occupant.IsEmpty)
            {
                var placed = moving.WithCount(Math.Min(wanted, max));
                if (!target.AcceptsByRule(to, placed)) return new MoveResult(MoveReason.Rejected, 0);
                source._slots[from] = moving.WithCount(moving.Count - placed.Count);
                target._slots[to] = placed;
                source.SlotChanged?.Invoke(from);
                target.SlotChanged?.Invoke(to);
                return new MoveResult(MoveReason.Moved, placed.Count);
            }

            if (occupant.SameItem(moving))
            {
                var room = max - occupant.Count;
                if (room <= 0) return new MoveResult(MoveReason.TargetFull, 0);
                var take = Math.Min(wanted, room);
                var merged = occupant.WithCount(occupant.Count + take);
                if (!target.AcceptsByRule(to, merged)) return new MoveResult(MoveReason.Rejected, 0);
                source._slots[from] = moving.WithCount(moving.Count - take);
                target._slots[to] = merged;
                source.SlotChanged?.Invoke(from);
                target.SlotChanged?.Invoke(to);
                return new MoveResult(MoveReason.Merged, take);
            }

            if (!whole) return new MoveResult(MoveReason.CannotSplitIntoOccupied, 0);
            if (moving.Count > max || occupant.Count > source.MaxStackOf(occupant.ItemId)) return new MoveResult(MoveReason.StackLimit, 0);
            if (!target.AcceptsByRule(to, moving) || !source.AcceptsByRule(from, occupant)) return new MoveResult(MoveReason.Rejected, 0);
            source._slots[from] = occupant;
            target._slots[to] = moving;
            source.SlotChanged?.Invoke(from);
            target.SlotChanged?.Invoke(to);
            return new MoveResult(MoveReason.Swapped, moving.Count);
        }

        private bool AcceptsByRule(int slot, ItemStack stack) => _canPlace == null || _canPlace(slot, stack);
    }
}
