using System;
using UnityEngine;
using UnityEngine.UIElements;

namespace FutureDigitalPlus.UIKit.Interaction
{
    /// <summary>
    /// Tracks which device the player uses and mirrors it as classes on a root element, so USS can react: the focus ring only shows
    /// for keyboard and gamepad (<c>fdp-input-nav</c>), hit sizes can follow touch (<c>fdp-input-touch</c>), tooltips can wait for focus.
    /// <para>
    /// UI Toolkit raises the same navigation events for keyboard and gamepad and keeps <c>:focus</c> and <c>:hover</c> state whatever the
    /// device, so USS alone cannot tell them apart. Pointer devices are read from the pointer events of the bound element (this works with
    /// every input backend and every panel kind); keyboard and gamepad come from the backends of <see cref="InputFeed"/> and from the
    /// navigation events that reach the element.
    /// </para>
    /// <code>
    /// var profile = InputProfile.Bind(uiDocument.rootVisualElement);
    /// profile.ModeChanged += mode => ShowPrompts(mode);
    /// profile.NavigationEnabled = false;   // a HUD document that must not react to WASD / D-pad
    /// </code>
    /// Classes written: <c>fdp-input-mouse</c>, <c>fdp-input-touch</c>, <c>fdp-input-keyboard</c>, <c>fdp-input-gamepad</c>, and <c>fdp-input-nav</c>
    /// for the last two.
    /// </summary>
    public sealed class InputProfile : IDisposable
    {
        /// <summary>Class set while the last device was a mouse or pen.</summary>
        public const string PointerClass = "fdp-input-mouse";

        /// <summary>Class set while the last device was a finger. Also the hit-size tier class of the design system.</summary>
        public const string TouchClass = "fdp-input-touch";

        /// <summary>Class set while the last device was a keyboard.</summary>
        public const string KeyboardClass = "fdp-input-keyboard";

        /// <summary>Class set while the last device was a gamepad.</summary>
        public const string GamepadClass = "fdp-input-gamepad";

        /// <summary>Class set for keyboard and gamepad: the focus ring is shown.</summary>
        public const string NavigationClass = "fdp-input-nav";

        private readonly VisualElement _root;
        private readonly InputModeTracker _tracker;
        private bool _navigationEnabled = true;
        private bool _manageTouchClass = true;
        private bool _disposed;

        private InputProfile(VisualElement root)
        {
            _root = root;
            _tracker = new InputModeTracker(
                InputModeTracker.PlatformDefault(Application.isMobilePlatform, Application.isConsolePlatform),
                () => Time.realtimeSinceStartupAsDouble);

            InputFeed.Acquire();
            InputFeed.DeviceUsed += Report;
            root.RegisterCallback<PointerDownEvent>(OnPointerDown, TrickleDown.TrickleDown);
            root.RegisterCallback<PointerMoveEvent>(OnPointerMove, TrickleDown.TrickleDown);
            root.RegisterCallback<NavigationMoveEvent>(OnNavigation, TrickleDown.TrickleDown);
            root.RegisterCallback<NavigationSubmitEvent>(OnNavigation, TrickleDown.TrickleDown);
            Apply();
        }

        /// <summary>Starts tracking for <paramref name="root"/> and writes the input classes on it. Returns null when <paramref name="root"/> is null. Dispose to stop and remove the classes.</summary>
        public static InputProfile Bind(VisualElement root) => root == null ? null : new InputProfile(root);

        /// <summary>The element that carries the classes.</summary>
        public VisualElement Root => _root;

        /// <summary>The device kind in effect: the forced one when set, otherwise the last one used.</summary>
        public InputDeviceKind Mode => _tracker.Mode;

        /// <summary>The forced device kind, or null when the mode follows the input.</summary>
        public InputDeviceKind? Forced => _tracker.Forced;

        /// <summary>True for keyboard and gamepad: focus rings are shown and the first control of a screen should hold focus.</summary>
        public bool IsNavigationMode => _tracker.IsNavigation;

        /// <summary>Raised on the main thread after <see cref="Mode"/> changed and the classes were updated.</summary>
        public event Action<InputDeviceKind> ModeChanged;

        /// <summary>
        /// When false, navigation events (D-pad, arrows, WASD, Submit) are stopped at <see cref="Root"/> and never move focus or click a control.
        /// Use it for HUD documents that sit on top of a game which reads the same keys. Default true.
        /// </summary>
        public bool NavigationEnabled
        {
            get => _navigationEnabled;
            set => _navigationEnabled = value;
        }

        /// <summary>
        /// Whether this profile writes <c>fdp-input-touch</c>. Default true. Set it to false when another component (the facade's touch-tier
        /// logic, a width breakpoint) owns that class; the class is removed once when this is switched off.
        /// </summary>
        public bool ManageTouchClass
        {
            get => _manageTouchClass;
            set
            {
                if (_manageTouchClass == value) return;
                _manageTouchClass = value;
                if (value) Apply();
                else _root.RemoveFromClassList(TouchClass);
            }
        }

        /// <summary>Records that <paramref name="kind"/> produced input. Backends and pointer events call this; so can a game's own source and tests.</summary>
        public void Report(InputDeviceKind kind)
        {
            if (_disposed) return;
            if (_tracker.Observe(kind)) Changed();
        }

        /// <summary>Pins the mode (for example a settings option "always show focus"); null returns to following the input.</summary>
        public void Force(InputDeviceKind? kind)
        {
            if (_disposed) return;
            if (_tracker.Force(kind)) Changed();
        }

        /// <summary>Stops tracking, unregisters every callback and removes the classes from <see cref="Root"/>.</summary>
        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;
            InputFeed.DeviceUsed -= Report;
            _root.UnregisterCallback<PointerDownEvent>(OnPointerDown, TrickleDown.TrickleDown);
            _root.UnregisterCallback<PointerMoveEvent>(OnPointerMove, TrickleDown.TrickleDown);
            _root.UnregisterCallback<NavigationMoveEvent>(OnNavigation, TrickleDown.TrickleDown);
            _root.UnregisterCallback<NavigationSubmitEvent>(OnNavigation, TrickleDown.TrickleDown);
            _root.RemoveFromClassList(PointerClass);
            _root.RemoveFromClassList(KeyboardClass);
            _root.RemoveFromClassList(GamepadClass);
            _root.RemoveFromClassList(NavigationClass);
            if (_manageTouchClass) _root.RemoveFromClassList(TouchClass);
            InputFeed.Release();
        }

        private void Changed()
        {
            Apply();
            ModeChanged?.Invoke(_tracker.Mode);
        }

        private void Apply()
        {
            var mode = _tracker.Mode;
            _root.EnableInClassList(PointerClass, mode == InputDeviceKind.Pointer);
            if (_manageTouchClass) _root.EnableInClassList(TouchClass, mode == InputDeviceKind.Touch);
            _root.EnableInClassList(KeyboardClass, mode == InputDeviceKind.Keyboard);
            _root.EnableInClassList(GamepadClass, mode == InputDeviceKind.Gamepad);
            _root.EnableInClassList(NavigationClass, InputModeTracker.IsNavigationKind(mode));
        }

        // Pointer events are shared with every other profile: a touch on one panel is a touch for all.
        private void OnPointerDown(PointerDownEvent e) => InputFeed.ReportDevice(InputModeTracker.FromPointerType(e.pointerType));

        private void OnPointerMove(PointerMoveEvent e) => InputFeed.ReportDevice(InputModeTracker.FromPointerType(e.pointerType));

        private void OnNavigation(EventBase e)
        {
            if (!_navigationEnabled)
            {
                Suppress(e);
                return;
            }

            // Navigation events carry no device. If one arrives while a pointer or touch mode is active, the player is on a
            // keyboard or gamepad; the backends refine the guess on their next poll.
            if (!_tracker.IsNavigation) InputFeed.ReportDevice(InputDeviceKind.Keyboard);
        }

        private static void Suppress(EventBase e)
        {
            e.StopPropagation();
#pragma warning disable CS0618 // PreventDefault is obsolete in Unity 6 but is what stops the default focus move of a navigation event (verified on 6000.3)
            e.PreventDefault();
#pragma warning restore CS0618
        }
    }
}
