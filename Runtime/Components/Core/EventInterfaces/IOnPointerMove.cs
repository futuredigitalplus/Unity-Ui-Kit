using FutureDigitalPlus.UIKit.Components.InterfaceModifiers;
using UnityEngine.UIElements;

namespace FutureDigitalPlus.UIKit.Components
{
    /// <summary>
    /// When implemented by a <see cref="UIComponent"/>,
    /// a callback for <see cref="PointerMoveEvent"/> is
    /// automatically registered in the UIComponent constructor.
    /// Mouse, pen and touch raise pointer events; prefer them to the mouse events, which touch input may not raise.
    /// </summary>
    [RegistersEventCallback(typeof(PointerMoveEvent))]
    public interface IOnPointerMove
    {
        /// <summary>Called when the event reaches the component.</summary>
        void OnPointerMove(PointerMoveEvent evt);
    }
}
