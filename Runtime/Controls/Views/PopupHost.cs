using FutureDigitalPlus.UIKit.Foundation;
using UnityEngine;
using UnityEngine.UIElements;

namespace FutureDigitalPlus.UIKit.Controls
{
    /// <summary>
    /// Where popups live and how they keep focus. A popup is added as the last child of the top element of the panel
    /// (the document root), above everything else, inside the cascade of the design system tokens and theme.
    /// </summary>
    internal static class PopupHost
    {
        public const string LayerClass = "fdp-popup-layer";

        /// <summary>The top element below the panel root that contains <paramref name="anchor"/> (the anchor itself when it is that element).</summary>
        public static VisualElement TopLevel(VisualElement anchor)
        {
            var panel = anchor?.panel;
            if (panel == null) return null;
            var e = anchor;
            while (e.hierarchy.parent != null && e.hierarchy.parent != panel.visualTree) e = e.hierarchy.parent;
            return e;
        }

        /// <summary>Creates a full-size layer that blocks pointer events below it and adds it on top of <paramref name="host"/>.</summary>
        public static VisualElement CreateLayer(VisualElement host, string name)
        {
            var layer = new VisualElement { name = name, pickingMode = PickingMode.Position };
            layer.AddToClassList(LayerClass);
            host.Add(layer);
            return layer;
        }

        /// <summary>True when the element is in a right-to-left context.</summary>
        public static bool IsRightToLeft(VisualElement element) => element.languageDirection == LanguageDirection.RTL;

        /// <summary>The seconds used by type-ahead and long-press timing.</summary>
        public static double Now => Time.realtimeSinceStartupAsDouble;
    }

    /// <summary>
    /// Keeps keyboard and gamepad focus inside a popup while it is open and gives it back when it closes. It is a
    /// <see cref="FocusTrap"/> of the Foundation module, so a popup opened from a dialog (a select list in a settings
    /// dialog) and the dialog do not fight over focus: only the newest trap of a panel is active, and the dialog's trap
    /// takes focus back when the popup closes.
    /// </summary>
    internal sealed class PopupFocus
    {
        private readonly VisualElement _scope;
        private FocusTrap _trap;

        public PopupFocus(VisualElement scope)
        {
            _scope = scope;
        }

        /// <summary>Starts containing focus. The element that has focus now is focused again by <see cref="End"/>.</summary>
        public void Begin()
        {
            if (_trap == null) _trap = new FocusTrap(_scope);
        }

        /// <summary>Focuses <paramref name="element"/>, or the first focusable element of the popup when null.</summary>
        public void Focus(VisualElement element)
        {
            var target = element ?? UiFocus.FirstFocusable(_scope);
            target?.Focus();
        }

        /// <summary>Stops containing focus and gives it back to the element that had it before.</summary>
        public void End()
        {
            _trap?.Dispose();
            _trap = null;
        }
    }
}
