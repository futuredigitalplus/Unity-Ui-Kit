using FutureDigitalPlus.UIKit.Components.InterfaceModifiers;
using UnityEngine.UIElements;

namespace FutureDigitalPlus.UIKit.Components
{
    /// <summary>
    /// When implemented by a <see cref="UIComponent"/>,
    /// a callback for <see cref="FocusInEvent"/> is
    /// automatically registered in the UIComponent constructor.
    /// Raised after the component or one of its descendants received focus.
    /// </summary>
    [RegistersEventCallback(typeof(FocusInEvent))]
    public interface IOnFocusIn
    {
        /// <summary>Called when the event reaches the component.</summary>
        void OnFocusIn(FocusInEvent evt);
    }
}
