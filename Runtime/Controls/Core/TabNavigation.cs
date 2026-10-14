using System;

namespace FutureDigitalPlus.UIKit.Controls
{
    /// <summary>How the items of a bar are laid out: the resolved flex direction of the bar (an engine-free copy of UI Toolkit's <c>FlexDirection</c>).</summary>
    public enum ItemFlow
    {
        /// <summary>Items from left to right in item order.</summary>
        Row,

        /// <summary>Items from right to left in item order (a bar mirrored for a right-to-left language).</summary>
        RowReverse,

        /// <summary>Items from top to bottom.</summary>
        Column,

        /// <summary>Items from bottom to top.</summary>
        ColumnReverse,
    }

    /// <summary>
    /// Keyboard and gamepad rules of a <c>TabBar</c> (and <c>SegmentedControl</c>), engine-free. Items are counted in item order (the order
    /// they were added, which is also the tree, focus and reading order); the arrows move toward the item that is on that side on screen.
    /// </summary>
    public static class TabNavigation
    {
        /// <summary>
        /// The step through the items, +1 or -1, of the Right arrow (<paramref name="towardRight"/> true) or the Left arrow. In a row the
        /// arrow goes to the item that is visually on its side: Right is +1 in <see cref="ItemFlow.Row"/> and -1 in
        /// <see cref="ItemFlow.RowReverse"/>. A column has no left and right, so the reading direction decides: Right is +1 left to right
        /// and -1 right to left (Right goes back, as in a right-to-left row).
        /// </summary>
        /// <param name="towardRight">True for the Right arrow, false for Left.</param>
        /// <param name="flow">How the bar lays its items out now.</param>
        /// <param name="rightToLeft">True when the bar is in a right-to-left context (used for columns).</param>
        public static int StepFor(bool towardRight, ItemFlow flow, bool rightToLeft)
        {
            bool forward;
            switch (flow)
            {
                case ItemFlow.Row: forward = towardRight; break;
                case ItemFlow.RowReverse: forward = !towardRight; break;
                default: forward = towardRight != rightToLeft; break;
            }

            return forward ? 1 : -1;
        }

        /// <summary>
        /// The index of the next enabled item after <paramref name="from"/> in the direction of <paramref name="step"/> (only its sign
        /// counts), or -1 when there is none (no wrap at the ends). <paramref name="from"/> may be -1 or <paramref name="count"/> to start
        /// outside the items.
        /// </summary>
        /// <param name="count">Number of items.</param>
        /// <param name="isEnabled">Whether the item at an index can be selected.</param>
        /// <param name="from">The index to start from (not itself a candidate).</param>
        /// <param name="step">The direction: positive forward, negative back, 0 finds nothing.</param>
        public static int Neighbour(int count, Func<int, bool> isEnabled, int from, int step)
        {
            if (isEnabled == null) throw new ArgumentNullException(nameof(isEnabled));
            step = Math.Sign(step);
            if (step == 0 || count <= 0) return -1;
            for (var i = Math.Max(-1, Math.Min(count, from)) + step; i >= 0 && i < count; i += step)
                if (isEnabled(i)) return i;
            return -1;
        }

        /// <summary>The first enabled item in item order (Home: the reading start, at the right in a right-to-left bar), or -1.</summary>
        /// <param name="count">Number of items.</param>
        /// <param name="isEnabled">Whether the item at an index can be selected.</param>
        public static int First(int count, Func<int, bool> isEnabled) => Neighbour(count, isEnabled, -1, 1);

        /// <summary>The last enabled item in item order (End: the reading end, at the left in a right-to-left bar), or -1.</summary>
        /// <param name="count">Number of items.</param>
        /// <param name="isEnabled">Whether the item at an index can be selected.</param>
        public static int Last(int count, Func<int, bool> isEnabled) => Neighbour(count, isEnabled, count, -1);

        /// <summary>
        /// Where the item at <paramref name="index"/> shows in a row, counted from the left edge (0 = leftmost):
        /// <paramref name="index"/> in <see cref="ItemFlow.Row"/>, <c>count - 1 - index</c> in <see cref="ItemFlow.RowReverse"/>.
        /// For a column it is the position from the top (bottom for <see cref="ItemFlow.ColumnReverse"/>), which is <paramref name="index"/>.
        /// </summary>
        /// <param name="index">The item index.</param>
        /// <param name="count">Number of items.</param>
        /// <param name="flow">How the bar lays its items out.</param>
        public static int VisualSlot(int index, int count, ItemFlow flow) => flow == ItemFlow.RowReverse ? count - 1 - index : index;
    }
}
