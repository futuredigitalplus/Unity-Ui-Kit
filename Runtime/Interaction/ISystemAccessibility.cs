using System;

namespace FutureDigitalPlus.UIKit.Interaction
{
    /// <summary>
    /// Where the operating system's accessibility settings come from. The default (<see cref="UnitySystemAccessibility"/>) reads the two values Unity
    /// exposes, font scale and bold text. A game that ships a native plugin for iOS Reduce Motion, Android animation scale or Windows high contrast
    /// implements this interface and passes it to <see cref="AccessibilityApplier.Bind"/>; the kit itself contains no native code.
    /// </summary>
    public interface ISystemAccessibility
    {
        /// <summary>The current values.</summary>
        SystemAccessibility Current { get; }

        /// <summary>Raised on the main thread when any value changed.</summary>
        event Action Changed;
    }
}
