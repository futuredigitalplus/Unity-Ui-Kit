using UnityEngine.UIElements;

namespace FutureDigitalPlus.UIKit.Foundation
{
    /// <summary>Focus helpers for dialogs, sheets and menus: find and focus the first control, and make an overlay absorb pointer input.</summary>
    public static class UiFocus
    {
        /// <summary>The first focusable, enabled, displayed descendant in tree order, or null. Elements with a negative tab index are skipped.</summary>
        public static VisualElement FirstFocusable(VisualElement root)
        {
            if (root == null)
                return null;
            for (int i = 0; i < root.hierarchy.childCount; i++)
            {
                VisualElement child = root.hierarchy[i];
                // The inline value is checked too: before the first style pass the resolved display is still the default.
                if (child.style.display.value == DisplayStyle.None || child.resolvedStyle.display == DisplayStyle.None || !child.enabledSelf)
                    continue;
                if (child.focusable && child.tabIndex >= 0)
                    return child;
                VisualElement nested = FirstFocusable(child);
                if (nested != null)
                    return nested;
            }
            return null;
        }

        /// <summary>Focuses the first focusable descendant. Returns true when something took focus.</summary>
        public static bool FocusFirst(VisualElement root)
        {
            VisualElement target = FirstFocusable(root);
            if (target == null)
                return false;
            target.Focus();
            return true;
        }

        /// <summary>
        /// Makes <paramref name="container"/> absorb pointer input (it takes picking and nothing reaches the elements below it) and stops the events from reaching
        /// its ancestors. Use it for scrims and blocking overlays.
        /// </summary>
        public static void AbsorbPointer(VisualElement container)
        {
            if (container == null)
                return;
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
