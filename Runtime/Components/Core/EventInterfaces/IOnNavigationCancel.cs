using FutureDigitalPlus.UIKit.Components.InterfaceModifiers;
using UnityEngine.UIElements;

namespace FutureDigitalPlus.UIKit.Components
{
    /// <summary>
    /// When implemented by a <see cref="UIComponent"/>,
    /// a callback for <see cref="NavigationCancelEvent"/> is
    /// automatically registered in the UIComponent constructor.
    /// Escape and the east button of a gamepad raise this event, but only on the focused element.
    /// </summary>
    [RegistersEventCallback(typeof(NavigationCancelEvent))]
    public interface IOnNavigationCancel
    {
        /// <summary>Called when the event reaches the component.</summary>
        void OnNavigationCancel(NavigationCancelEvent evt);
    }
}
