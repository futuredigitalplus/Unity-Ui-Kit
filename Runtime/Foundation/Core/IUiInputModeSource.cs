using System;

namespace FutureDigitalPlus.UIKit.Foundation
{
    /// <summary>Reports which input device the player used last. The Interaction module provides one; a game can write its own.</summary>
    public interface IUiInputModeSource
    {
        /// <summary>The current mode.</summary>
        UiInputMode Mode { get; }

        /// <summary>Raised after <see cref="Mode"/> changed.</summary>
        event Action<UiInputMode> ModeChanged;
    }
}
