/*
 * FancyScrollView (https://github.com/setchi/FancyScrollView)
 * Copyright (c) 2020 setchi
 * Licensed under MIT (https://github.com/setchi/FancyScrollView/blob/master/LICENSE)
 */

// FutureDigitalPlus: new file. The arithmetic of FancyScrollView.UpdatePosition, ResizePool and UpdateCells (Sources/Runtime/Core/
// FancyScrollView.cs), lifted out unchanged so it is testable without the engine; the view calls it.

namespace FutureDigitalPlus.UIKit.ScrollView
{
    /// <summary>
    /// Where the cells of a fancy scroll view go. The viewport is the range 0..1; cells sit <c>cellInterval</c> apart, and the item at the
    /// scroll position sits at <c>scrollOffset</c>. A pool of cells is reused in a ring: slot <c>i</c> shows item <c>firstIndex + i</c>.
    /// </summary>
    public static class CellLayout
    {
        /// <summary>The first slot: the item index it shows (may be negative or past the end) and its position in 0..1.</summary>
        public static void First(float position, float scrollOffset, float cellInterval, out int firstIndex, out float firstPosition)
        {
            var p = position - scrollOffset / cellInterval;
            firstIndex = ScrollMath.CeilToInt(p);
            firstPosition = ((float)System.Math.Ceiling(p) - p) * cellInterval;
        }

        /// <summary>True when <paramref name="poolCount"/> cells do not reach the end of the viewport.</summary>
        public static bool NeedsMoreCells(float firstPosition, int poolCount, float cellInterval) =>
            firstPosition + poolCount * cellInterval < 1f;

        /// <summary>The number of cells that cover the viewport from <paramref name="firstPosition"/>.</summary>
        public static int PoolSize(float firstPosition, float cellInterval) =>
            ScrollMath.CeilToInt((1f - firstPosition) / cellInterval);

        /// <summary>The pool cell that shows slot <paramref name="slot"/>, so a cell keeps its item while the view scrolls.</summary>
        public static int PoolIndex(int firstIndex, int slot, int poolCount) => ScrollMath.CircularIndex(firstIndex + slot, poolCount);

        /// <summary>The item a slot shows: wrapped around the item count when <paramref name="loop"/> is on.</summary>
        public static int ItemIndex(int index, int itemCount, bool loop) => loop ? ScrollMath.CircularIndex(index, itemCount) : index;

        /// <summary>False for a slot past the viewport or without an item; that cell is hidden.</summary>
        public static bool IsShown(int itemIndex, float slotPosition, int itemCount) =>
            !(itemIndex < 0 || itemIndex >= itemCount || slotPosition > 1f);
    }
}
