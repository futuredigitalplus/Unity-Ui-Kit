using System;
using UnityEngine;

namespace FutureDigitalPlus.UIKit.Foundation
{
    /// <summary>
    /// Answers "should the game world ignore this touch or click?". True while a modal dialog, a blocking loading token or a screen transition is active, and
    /// for pointer positions over an interactive element of the shell. A game's input code can depend on this small interface only.
    /// </summary>
    public interface IUiInputGate
    {
        /// <summary>True while any blocking reason is active (dialog, loading, transition).</summary>
        bool IsBlocked { get; }

        /// <summary>Raised with the new state when <see cref="IsBlocked"/> flips.</summary>
        event Action<bool> BlockedChanged;

        /// <summary>True when the screen position (pixels, origin bottom left like a mouse position) is over a pickable element of the shell.</summary>
        bool IsPointerOverUi(Vector2 screenPosition);
    }
}
