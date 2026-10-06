using FutureDigitalPlus.UIKit.Components.InterfaceModifiers;
using UnityEngine.UIElements;

namespace FutureDigitalPlus.UIKit.Components
{
    /// <summary>
    /// When implemented by a <see cref="UIComponent"/>,
    /// a callback for <see cref="KeyDownEvent"/> is
    /// automatically registered in the UIComponent constructor.
    /// Raised for a key press while the component or a descendant has keyboard focus.
    /// </summary>
    [RegistersEventCallback(typeof(KeyDownEvent))]
    public interface IOnKeyDown
    {
        /// <summary>Called when the event reaches the component.</summary>
        void OnKeyDown(KeyDownEvent evt);
    }
}
