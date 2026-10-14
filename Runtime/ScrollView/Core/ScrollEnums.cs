/*
 * FancyScrollView (https://github.com/setchi/FancyScrollView)
 * Copyright (c) 2020 setchi
 * Licensed under MIT (https://github.com/setchi/FancyScrollView/blob/master/LICENSE)
 */

// FutureDigitalPlus: the three enums of Sources/Runtime/Scroller (ScrollDirection.cs, MovementType.cs, MovementDirection.cs) in one file.
// Namespace FutureDigitalPlus.UIKit.ScrollView (was FancyScrollView); MovementType no longer aliases uGUI's ScrollRect.MovementType (same values).

namespace FutureDigitalPlus.UIKit.ScrollView
{
    /// <summary>The axis a scroller moves along.</summary>
    public enum ScrollDirection
    {
        /// <summary>Top to bottom.</summary>
        Vertical,
        /// <summary>Left to right.</summary>
        Horizontal,
    }

    /// <summary>What happens when the content is moved past either end.</summary>
    public enum MovementType
    {
        /// <summary>No ends: the position runs on forever (use it with a looping view).</summary>
        Unrestricted = 0,
        /// <summary>The content can be pulled past an end and springs back.</summary>
        Elastic = 1,
        /// <summary>The content stops at the ends.</summary>
        Clamped = 2,
    }

    /// <summary>The way the content moves on screen to go from one index to another.</summary>
    public enum MovementDirection
    {
        /// <summary>Content moves left.</summary>
        Left,
        /// <summary>Content moves right.</summary>
        Right,
        /// <summary>Content moves up.</summary>
        Up,
        /// <summary>Content moves down.</summary>
        Down,
    }
}
