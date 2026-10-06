using System;
using UnityEngine.UIElements;

namespace FutureDigitalPlus.UIKit.Interaction
{
    /// <summary>
    /// "Activate" for any element, from every device. A pointer click raises <see cref="ClickEvent"/>, but Enter on a keyboard and the south
    /// button of a gamepad raise <see cref="NavigationSubmitEvent"/> and no ClickEvent (verified on Unity 6000.3: a <see cref="Button"/> with a
    /// ClickEvent callback does not fire on Submit; only its <c>clicked</c> event does). Anything that listens for ClickEvent alone is
    /// unreachable without a pointer, and a plain <see cref="VisualElement"/> is not even focusable.
    /// <code>
    /// using var registration = Activate.Register(rowElement, () => OpenDetails());
    /// </code>
    /// </summary>
    public static class Activate
    {
        /// <summary>
        /// Calls <paramref name="onActivate"/> once per click and once per Submit on <paramref name="element"/>. With <paramref name="focusable"/> the
        /// element can take keyboard and gamepad focus. Dispose the result to unregister and to restore the previous <c>focusable</c> value.
        /// </summary>
        /// <param name="element">The element to make activatable.</param>
        /// <param name="onActivate">The action to run.</param>
        /// <param name="focusable">True to set <c>element.focusable</c> (needed for plain elements; buttons already are).</param>
        public static IDisposable Register(VisualElement element, Action onActivate, bool focusable = true)
        {
            if (element == null) throw new ArgumentNullException(nameof(element));
            if (onActivate == null) throw new ArgumentNullException(nameof(onActivate));
            return new Registration(element, onActivate, focusable);
        }

        private sealed class Registration : IDisposable
        {
            private readonly VisualElement _element;
            private readonly Action _action;
            private readonly bool _wasFocusable;
            private readonly bool _changedFocusable;
            private bool _disposed;

            public Registration(VisualElement element, Action action, bool focusable)
            {
                _element = element;
                _action = action;
                _wasFocusable = element.focusable;
                if (focusable && !_wasFocusable)
                {
                    element.focusable = true;
                    _changedFocusable = true;
                }

                element.RegisterCallback<ClickEvent>(OnClick);
                element.RegisterCallback<NavigationSubmitEvent>(OnSubmit);
            }

            private void OnClick(ClickEvent e)
            {
                _action();
                e.StopPropagation();
            }

            // StopImmediatePropagation: when another component on the same element turns Submit into a ClickEvent (the design system does for its
            // nav items and chips), the action must run once, either through this callback or through that click, never both.
            private void OnSubmit(NavigationSubmitEvent e)
            {
                _action();
                e.StopImmediatePropagation();
            }

            public void Dispose()
            {
                if (_disposed) return;
                _disposed = true;
                _element.UnregisterCallback<ClickEvent>(OnClick);
                _element.UnregisterCallback<NavigationSubmitEvent>(OnSubmit);
                if (_changedFocusable) _element.focusable = _wasFocusable;
            }
        }
    }
}
