using FutureDigitalPlus.UIKit.Components.InterfaceModifiers;
using UnityEngine.UIElements;

namespace FutureDigitalPlus.UIKit.Components
{
    /// <summary>
    /// When implemented by a <see cref="UIComponent"/>,
    /// a callback for <see cref="NavigationSubmitEvent"/> is
    /// automatically registered in the UIComponent constructor.
    /// Enter on a keyboard and the south button of a gamepad raise this event, not a ClickEvent: a component that must be reachable without a pointer implements this next to <see cref="IOnClick"/>. The element must be focusable to receive it.
    /// </summary>
    [RegistersEventCallback(typeof(NavigationSubmitEvent))]
    public interface IOnNavigationSubmit
    {
        /// <summary>Called when the event reaches the component.</summary>
        void OnNavigationSubmit(NavigationSubmitEvent evt);
    }
}
