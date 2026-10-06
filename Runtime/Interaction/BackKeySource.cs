using System;
using UnityEngine.UIElements;

namespace FutureDigitalPlus.UIKit.Interaction
{
    /// <summary>A source of "back" presses: Escape, the Android back button, gamepad east (B). A screen router or dialog stack subscribes and decides what back means.</summary>
    public interface IBackPressSource
    {
        /// <summary>Raised on the main thread when back was pressed, at most once per frame.</summary>
        event Action Pressed;
    }

    /// <summary>
    /// Raises <see cref="Pressed"/> for every back press from every source: the device backends (legacy Input Manager, Input System) and the
    /// <see cref="NavigationCancelEvent"/> of a bound element. Both kinds are needed: UI Toolkit delivers Cancel only to the focused element, and on a
    /// touch device nothing is ever focused, so the Android back button never reaches the panel by itself. One physical press counts once.
    /// <code>
    /// using var back = new BackKeySource();
    /// back.Bind(uiDocument.rootVisualElement);
    /// back.Pressed += () => router.HandleBack();
    /// </code>
    /// Known limit: a control that consumes Cancel itself (a dropdown popup) does not stop the press from reaching <see cref="Pressed"/>. Let such a
    /// control register a back handler with the dispatcher instead.
    /// </summary>
    public sealed class BackKeySource : IBackPressSource, IDisposable
    {
        private VisualElement _root;
        private bool _disposed;

        /// <summary>Starts the device backends and begins to listen. Dispose to stop.</summary>
        public BackKeySource()
        {
            InputFeed.Acquire();
            InputFeed.BackPressed += OnBack;
        }

        /// <inheritdoc />
        public event Action Pressed;

        /// <summary>Also treats a <see cref="NavigationCancelEvent"/> that reaches <paramref name="root"/> as a back press. Replaces an earlier binding.</summary>
        public void Bind(VisualElement root)
        {
            Unbind();
            if (root == null || _disposed) return;
            _root = root;
            _root.RegisterCallback<NavigationCancelEvent>(OnCancel, TrickleDown.TrickleDown);
        }

        /// <summary>Stops listening and releases the device backends.</summary>
        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;
            Unbind();
            InputFeed.BackPressed -= OnBack;
            InputFeed.Release();
        }

        private void Unbind()
        {
            _root?.UnregisterCallback<NavigationCancelEvent>(OnCancel, TrickleDown.TrickleDown);
            _root = null;
        }

        private static void OnCancel(NavigationCancelEvent e) => InputFeed.ReportBack();

        private void OnBack() => Pressed?.Invoke();
    }
}
