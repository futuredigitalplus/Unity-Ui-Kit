using System;

namespace FutureDigitalPlus.UIKit.Interaction
{
    /// <summary>
    /// The state machine behind "which device is the player using": the last device that produced real input wins, a forced
    /// mode overrides it, and two guards keep the answer stable (stick drift is ignored, and mouse events that arrive right
    /// after a touch are treated as emulation of that touch, not as a new device).
    /// Pure logic with an injected clock, so it runs and is tested without Unity.
    /// </summary>
    public sealed class InputModeTracker
    {
        /// <summary>Stick and trigger deflection below this value is drift, not input.</summary>
        public const float StickThreshold = 0.3f;

        /// <summary>
        /// A pointer (mouse) report within this many seconds after a touch is ignored. Several platforms synthesise mouse events
        /// from touches (the legacy Input Manager does with <c>simulateMouseWithTouches</c>); a real mouse is picked up by its next move.
        /// </summary>
        public const double TouchMouseGuardSeconds = 0.75;

        private readonly Func<double> _clock;
        private InputDeviceKind _observed;
        private InputDeviceKind? _forced;
        private double _lastTouchTime = double.NegativeInfinity;

        /// <summary>Creates a tracker that starts in <paramref name="initial"/>.</summary>
        /// <param name="initial">The mode before any input was seen, usually <see cref="PlatformDefault"/>.</param>
        /// <param name="clock">Seconds, monotonic. Null means the guard never expires within one call chain (tests that do not care).</param>
        public InputModeTracker(InputDeviceKind initial, Func<double> clock = null)
        {
            _observed = initial;
            _clock = clock ?? (() => 0d);
        }

        /// <summary>The mode in effect: the forced one when set, otherwise the last observed one.</summary>
        public InputDeviceKind Mode => _forced ?? _observed;

        /// <summary>The forced mode, or null when the mode follows the input.</summary>
        public InputDeviceKind? Forced => _forced;

        /// <summary>True when <see cref="Mode"/> is keyboard or gamepad: focus rings are shown and a screen should hold focus.</summary>
        public bool IsNavigation => IsNavigationKind(Mode);

        /// <summary>Records that <paramref name="kind"/> produced input. Returns true when <see cref="Mode"/> changed.</summary>
        public bool Observe(InputDeviceKind kind)
        {
            var now = _clock();
            if (kind == InputDeviceKind.Touch)
            {
                _lastTouchTime = now;
            }
            else if (kind == InputDeviceKind.Pointer && _observed == InputDeviceKind.Touch && now - _lastTouchTime < TouchMouseGuardSeconds)
            {
                return false;
            }

            var before = Mode;
            _observed = kind;
            return Mode != before;
        }

        /// <summary>Pins the mode (a settings option such as "always show focus"); null returns to following the input. Returns true when <see cref="Mode"/> changed.</summary>
        public bool Force(InputDeviceKind? kind)
        {
            var before = Mode;
            _forced = kind;
            return Mode != before;
        }

        /// <summary>True for <see cref="InputDeviceKind.Keyboard"/> and <see cref="InputDeviceKind.Gamepad"/>.</summary>
        public static bool IsNavigationKind(InputDeviceKind kind) =>
            kind == InputDeviceKind.Keyboard || kind == InputDeviceKind.Gamepad;

        /// <summary>The mode to assume before the first input: gamepad on consoles, touch on handhelds, pointer elsewhere.</summary>
        public static InputDeviceKind PlatformDefault(bool isMobilePlatform, bool isConsolePlatform)
        {
            if (isConsolePlatform) return InputDeviceKind.Gamepad;
            if (isMobilePlatform) return InputDeviceKind.Touch;
            return InputDeviceKind.Pointer;
        }

        /// <summary>True when a stick or trigger value is real input rather than drift (the magnitude is compared with <see cref="StickThreshold"/>).</summary>
        public static bool IsSignificantStick(float magnitude) => magnitude >= StickThreshold;

        /// <summary>Maps a UI Toolkit <c>pointerType</c> string (<c>touch</c>, <c>mouse</c>, <c>pen</c>, <c>tracked</c>) to a device kind. Anything but touch can hover, so it is a pointer.</summary>
        public static InputDeviceKind FromPointerType(string pointerType) =>
            pointerType == "touch" ? InputDeviceKind.Touch : InputDeviceKind.Pointer;
    }
}
