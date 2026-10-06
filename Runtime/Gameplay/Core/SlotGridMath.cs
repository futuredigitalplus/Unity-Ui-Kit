using System;

namespace FutureDigitalPlus.UIKit.Gameplay
{
    /// <summary>A direction of keyboard or gamepad movement in a slot grid.</summary>
    public enum SlotDirection
    {
        /// <summary>One slot to the left.</summary>
        Left,

        /// <summary>One slot to the right.</summary>
        Right,

        /// <summary>One row up.</summary>
        Up,

        /// <summary>One row down.</summary>
        Down,
    }

    /// <summary>Pure math for keyboard and gamepad movement in a grid of slots read row by row.</summary>
    public static class SlotGridMath
    {
        /// <summary>
        /// Where a direction key moves from <paramref name="index"/> in a grid read row by row. The index stays the same at
        /// an edge (so the caller can let navigation leave the grid). Moving down from the second to last row onto a
        /// short last row goes to the last slot.
        /// </summary>
        public static int Move(int index, SlotDirection direction, int columns, int slotCount)
        {
            if (slotCount <= 0) return -1;
            columns = Math.Max(1, columns);
            index = Math.Max(0, Math.Min(slotCount - 1, index));
            switch (direction)
            {
                case SlotDirection.Left:
                    return index % columns > 0 ? index - 1 : index;
                case SlotDirection.Right:
                    return index % columns < columns - 1 && index + 1 < slotCount ? index + 1 : index;
                case SlotDirection.Up:
                    return index - columns >= 0 ? index - columns : index;
                default:
                    if (index + columns < slotCount) return index + columns;
                    return index / columns < (slotCount - 1) / columns ? slotCount - 1 : index;
            }
        }
    }
}
