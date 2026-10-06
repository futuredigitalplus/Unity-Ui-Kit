#if ENABLE_INPUT_SYSTEM
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;

namespace FutureDigitalPlus.UIKit.Interaction
{
    /// <summary>
    /// The Input System backend of <see cref="InputFeed"/>. Compiled only when the Input System package is installed (<c>FDP_UIKIT_INPUTSYSTEM</c>, set by the
    /// assembly's version define) and the project uses it (<c>ENABLE_INPUT_SYSTEM</c>, set by Active Input Handling).
    /// Device kinds come from state events: stick and trigger drift below <see cref="InputModeTracker.StickThreshold"/> is ignored, so a resting gamepad does not
    /// flip the mode. Back is Escape (which is the Android back button) and gamepad east, read once per frame.
    /// </summary>
    internal sealed class InputSystemBackend : IInputBackend
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Register() => InputFeed.RegisterBackend(new InputSystemBackend());

        public void Start() => InputSystem.onEvent += OnEvent;

        public void Stop() => InputSystem.onEvent -= OnEvent;

        public void Poll()
        {
            var keyboard = Keyboard.current;
            var gamepad = Gamepad.current;
            if ((keyboard != null && keyboard.escapeKey.wasPressedThisFrame) || (gamepad != null && gamepad.buttonEast.wasPressedThisFrame))
                InputFeed.ReportBack();
        }

        private static void OnEvent(InputEventPtr ptr, InputDevice device)
        {
            if (device == null || (!ptr.IsA<StateEvent>() && !ptr.IsA<DeltaStateEvent>())) return;
            foreach (var control in ptr.EnumerateChangedControls(device, InputModeTracker.StickThreshold))
            {
                // Escape is the back button on Android: not evidence of a keyboard on a handheld.
                if (Application.isMobilePlatform && device is Keyboard keyboard && control == keyboard.escapeKey) return;

                InputDeviceKind? kind = device switch
                {
                    Touchscreen => InputDeviceKind.Touch,
                    Gamepad => InputDeviceKind.Gamepad,
                    Joystick => InputDeviceKind.Gamepad,
                    Keyboard => InputDeviceKind.Keyboard,
                    Mouse => InputDeviceKind.Pointer,
                    Pen => InputDeviceKind.Pointer,
                    _ => null,
                };
                if (kind.HasValue) InputFeed.ReportDevice(kind.Value);
                return;
            }
        }
    }
}
#endif
