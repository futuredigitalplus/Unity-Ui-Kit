using FutureDigitalPlus.UIKit.Components.InterfaceModifiers;
using UnityEngine.UIElements;

namespace FutureDigitalPlus.UIKit.Components
{
    /// <summary>
    /// When implemented by a <see cref="UIComponent"/>,
    /// a callback for <see cref="PointerDownEvent"/> is
    /// automatically registered in the UIComponent constructor.
    /// Mouse, pen and touch raise pointer events; prefer them to the mouse events, which touch input may not raise.
    /// </summary>
    [RegistersEventCallback(typeof(PointerDownEvent))]
    public interface IOnPointerDown
    {
        /// <summary>Called when the event reaches the component.</summary>
        void OnPointerDown(PointerDownEvent evt);
    }
}
