using System;

namespace FutureDigitalPlus.UIKit.Gameplay
{
    /// <summary>
    /// The content of one inventory slot: an item id and how many of it. An empty stack has no id and a count of 0.
    /// Two stacks hold the same kind of item when their ids are equal (ordinal comparison). Items that must never
    /// stack (a unique sword with its own durability) are given unique ids and a stack limit of 1 by the game.
    /// </summary>
    public readonly struct ItemStack : IEquatable<ItemStack>
    {
        /// <summary>Creates a stack. A null or empty id or a count of 0 or less gives <see cref="Empty"/>.</summary>
        /// <param name="itemId">The id of the item kind.</param>
        /// <param name="count">How many items.</param>
        public ItemStack(string itemId, int count)
        {
            if (string.IsNullOrEmpty(itemId) || count <= 0)
            {
                ItemId = null;
                Count = 0;
                return;
            }

            ItemId = itemId;
            Count = count;
        }

        /// <summary>The empty stack.</summary>
        public static ItemStack Empty => default;

        /// <summary>The id of the item kind, or null when the stack is empty.</summary>
        public string ItemId { get; }

        /// <summary>How many items the stack holds (0 when empty).</summary>
        public int Count { get; }

        /// <summary>True when the stack holds nothing.</summary>
        public bool IsEmpty => Count <= 0;

        /// <summary>The same kind of item with another count (empty when <paramref name="count"/> is 0 or less).</summary>
        public ItemStack WithCount(int count) => new ItemStack(ItemId, count);

        /// <summary>True when <paramref name="other"/> is the same kind of item (two empty stacks count as the same).</summary>
        public bool SameItem(ItemStack other) => string.Equals(ItemId, other.ItemId, StringComparison.Ordinal);

        /// <inheritdoc />
        public bool Equals(ItemStack other) => Count == other.Count && SameItem(other);

        /// <inheritdoc />
        public override bool Equals(object obj) => obj is ItemStack other && Equals(other);

        /// <inheritdoc />
        public override int GetHashCode() => (ItemId == null ? 0 : StringComparer.Ordinal.GetHashCode(ItemId)) * 397 ^ Count;

        /// <inheritdoc />
        public override string ToString() => IsEmpty ? "(empty)" : ItemId + " x" + Count;

        /// <summary>Equality operator.</summary>
        public static bool operator ==(ItemStack left, ItemStack right) => left.Equals(right);

        /// <summary>Inequality operator.</summary>
        public static bool operator !=(ItemStack left, ItemStack right) => !left.Equals(right);
    }
}
