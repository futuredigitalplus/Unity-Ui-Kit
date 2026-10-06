using FutureDigitalPlus.UIKit.Components.InterfaceModifiers;
using UnityEngine.UIElements;

namespace FutureDigitalPlus.UIKit.Components
{
    /// <summary>
    /// When implemented by a <see cref="UIComponent"/>,
    /// a callback for <see cref="FocusOutEvent"/> is
    /// automatically registered in the UIComponent constructor.
    /// Raised after the component or one of its descendants lost focus.
    /// </summary>
    [RegistersEventCallback(typeof(FocusOutEvent))]
    public interface IOnFocusOut
    {
        /// <summary>Called when the event reaches the component.</summary>
        void OnFocusOut(FocusOutEvent evt);
    }
}
