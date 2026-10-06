using System;

namespace FutureDigitalPlus.UIKit.Controls
{
    /// <summary>A direction of keyboard or gamepad movement in a grid.</summary>
    public enum GridDirection
    {
        /// <summary>One cell to the left.</summary>
        Left,

        /// <summary>One cell to the right.</summary>
        Right,

        /// <summary>One row up.</summary>
        Up,

        /// <summary>One row down.</summary>
        Down,
    }

    /// <summary>
    /// Pure math for a grid of equal cells: how many columns fit a width, how wide a cell gets, which rows are visible
    /// in a scrolled window, and where a direction key moves.
    /// </summary>
    public static class GridLayoutMath
    {
        /// <summary>The number of columns of at least <paramref name="minCellWidth"/> that fit in <paramref name="width"/> with <paramref name="spacing"/> between them.</summary>
        public static int ColumnCount(float width, float minCellWidth, float spacing, int minColumns = 1, int maxColumns = int.MaxValue)
        {
            minColumns = Math.Max(1, minColumns);
            maxColumns = Math.Max(minColumns, maxColumns);
            if (width <= 0f || minCellWidth <= 0f) return minColumns;
            var columns = (int)Math.Floor((width + spacing) / (minCellWidth + spacing));
            return Math.Max(minColumns, Math.Min(maxColumns, columns));
        }

        /// <summary>The width of one cell when <paramref name="columns"/> cells share <paramref name="width"/> with <paramref name="spacing"/> between them.</summary>
        public static float CellWidth(float width, int columns, float spacing)
        {
            columns = Math.Max(1, columns);
            return Math.Max(0f, (width - spacing * (columns - 1)) / columns);
        }

        /// <summary>The number of rows needed for <paramref name="itemCount"/> items in <paramref name="columns"/> columns.</summary>
        public static int RowCount(int itemCount, int columns)
        {
            columns = Math.Max(1, columns);
            return itemCount <= 0 ? 0 : (itemCount + columns - 1) / columns;
        }

        /// <summary>The height of <paramref name="rowCount"/> rows of <paramref name="rowHeight"/> with <paramref name="rowSpacing"/> between them.</summary>
        public static float ContentHeight(int rowCount, float rowHeight, float rowSpacing)
        {
            return rowCount <= 0 ? 0f : rowCount * rowHeight + (rowCount - 1) * rowSpacing;
        }

        /// <summary>
        /// The rows that intersect a scrolled window, widened by <paramref name="overscanRows"/> on both sides.
        /// <paramref name="lastExclusive"/> is one past the last visible row.
        /// </summary>
        public static void VisibleRows(float scrollOffset, float viewportHeight, float rowHeight, float rowSpacing, int rowCount,
            int overscanRows, out int first, out int lastExclusive)
        {
            first = 0;
            lastExclusive = 0;
            if (rowCount <= 0 || rowHeight <= 0f) return;
            var stride = rowHeight + Math.Max(0f, rowSpacing);
            // NaN (a viewport that has not been measured) counts as 0; rows beyond the last are clamped before the cast to int, which is not defined for huge doubles.
            var top = scrollOffset > 0f ? scrollOffset : 0f;
            var bottom = top + (viewportHeight > 0f ? viewportHeight : 0f);
            var overscan = Math.Max(0, overscanRows);
            first = Math.Max(0, (int)Math.Min(rowCount, Math.Floor(top / stride)) - overscan);
            lastExclusive = (int)Math.Min(rowCount, Math.Ceiling(bottom / stride) + overscan);
            if (lastExclusive < first) lastExclusive = first;
        }

        /// <summary>
        /// Where a direction key moves from <paramref name="index"/>. Left and right wrap to the neighbouring row only when
        /// <paramref name="wrapRows"/> is set; up and down stop at the first and last row, and moving down from a short last
        /// row goes to the last item. Returns the new index (the same index when the move is not possible).
        /// </summary>
        public static int Move(int index, GridDirection direction, int columns, int itemCount, bool wrapRows = false)
        {
            if (itemCount <= 0) return -1;
            columns = Math.Max(1, columns);
            index = Math.Max(0, Math.Min(itemCount - 1, index));
            switch (direction)
            {
                case GridDirection.Left:
                    if (index % columns > 0) return index - 1;
                    return wrapRows && index > 0 ? index - 1 : index;
                case GridDirection.Right:
                    if (index % columns < columns - 1 && index + 1 < itemCount) return index + 1;
                    return wrapRows && index + 1 < itemCount ? index + 1 : index;
                case GridDirection.Up:
                    return index - columns >= 0 ? index - columns : index;
                default:
                    if (index + columns < itemCount) return index + columns;
                    // on the second to last row and the last row is short: go to the last item
                    return RowOf(index, columns) < RowOf(itemCount - 1, columns) ? itemCount - 1 : index;
            }
        }

        private static int RowOf(int index, int columns) => index / columns;
    }
}
