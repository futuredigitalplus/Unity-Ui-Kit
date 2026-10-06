using UnityEngine.UIElements;

namespace FutureDigitalPlus.UIKit.Foundation
{
    /// <summary>Small layout helpers the foundation views share.</summary>
    internal static class FoundationLayout
    {
        /// <summary>
        /// Makes <paramref name="element"/> cover its parent (absolute, all insets 0) without taking pointer input itself. A template's outer
        /// <c>TemplateContainer</c> has no size of its own, so absolutely positioned children (scrims, overlays) need it to fill the layer.
        /// </summary>
        public static void Fill(VisualElement element)
        {
            element.style.position = Position.Absolute;
            element.style.left = 0;
            element.style.right = 0;
            element.style.top = 0;
            element.style.bottom = 0;
            element.pickingMode = PickingMode.Ignore;
        }

        /// <summary>Makes <paramref name="container"/> absorb pointer input (it sits on top of everything below it) and keeps the events from reaching its ancestors. Use it for scrims and blocking overlays.</summary>
        public static void AbsorbPointer(VisualElement container)
        {
            container.pickingMode = PickingMode.Position;
            container.RegisterCallback<PointerDownEvent>(Stop);
            container.RegisterCallback<PointerUpEvent>(Stop);
            container.RegisterCallback<PointerMoveEvent>(Stop);
            container.RegisterCallback<WheelEvent>(Stop);
        }

        static void Stop(EventBase evt)
        {
            evt.StopPropagation();
        }
    }
}
