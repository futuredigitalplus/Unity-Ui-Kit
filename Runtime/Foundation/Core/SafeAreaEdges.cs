using System;

namespace FutureDigitalPlus.UIKit.Foundation
{
    /// <summary>Which edges of a <c>SafeAreaPanel</c> take the safe-area inset.</summary>
    [Flags]
    public enum SafeAreaEdges
    {
        /// <summary>No edge.</summary>
        None = 0,
        /// <summary>The left edge.</summary>
        Left = 1,
        /// <summary>The top edge.</summary>
        Top = 2,
        /// <summary>The right edge.</summary>
        Right = 4,
        /// <summary>The bottom edge.</summary>
        Bottom = 8,
        /// <summary>All four edges.</summary>
        All = Left | Top | Right | Bottom
    }
}
