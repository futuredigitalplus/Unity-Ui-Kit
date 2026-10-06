#if ENABLE_LEGACY_INPUT_MANAGER
using System;
using UnityEngine;

namespace FutureDigitalPlus.UIKit.Interaction
{
    /// <summary>
    /// Classifies the player's device and reads the back key with the legacy Input Manager (<c>UnityEngine.Input</c>). It is compiled
    /// only when the project's Active Input Handling includes the legacy manager: with "Input System Package (New)" the <c>Input</c> class
    /// throws, so <c>ENABLE_LEGACY_INPUT_MANAGER</c> is the guard, not a version define.
    /// Limits: joystick sticks cannot be told apart from keyboard arrows (they share the Horizontal and Vertical axes), so a gamepad is
    /// recognised by its buttons; navigation events from a stick still switch the profile to the keyboard class, which shows the same focus ring.
    /// </summary>
    internal sealed class LegacyInputBackend : IInputBackend
    {
        private const int Mouse0 = (int)KeyCode.Mouse0, Mouse6 = (int)KeyCode.Mouse6;
        private const int Joy0 = (int)KeyCode.JoystickButton0, Joy19 = (int)KeyCode.Joystick8Button19;
        private static readonly string[] RequiredAxes = { "Horizontal", "Vertical", "Submit", "Cancel" };
        private static readonly Array KeyCodes = Enum.GetValues(typeof(KeyCode));

        private bool _cancelMissing;

        public void Start()
        {
            _cancelMissing = false;
            WarnAboutMissingAxes();
        }

        public void Stop() { }

        public void Poll()
        {
            if (Input.GetKeyDown(KeyCode.Escape) || CancelPressed())
                InputFeed.ReportBack();

            if (Input.touchCount > 0)
            {
                InputFeed.ReportDevice(InputDeviceKind.Touch);
                return;
            }

            if (!Input.anyKeyDown)
            {
                if (Input.mousePositionDelta.sqrMagnitude > 1f) InputFeed.ReportDevice(InputDeviceKind.Pointer);
                return;
            }

            // anyKeyDown is true for keys, mouse buttons and joystick buttons alike; the key codes tell them apart.
            foreach (var code in KeyCodes)
            {
                var keyCode = (KeyCode)code;
                if (!Input.GetKeyDown(keyCode)) continue;
                var value = (int)keyCode;
                if (value >= Mouse0 && value <= Mouse6) InputFeed.ReportDevice(InputDeviceKind.Pointer);
                else if (value >= Joy0 && value <= Joy19) InputFeed.ReportDevice(InputDeviceKind.Gamepad);
                // Escape is the back button on Android: not evidence of a keyboard on a handheld.
                else if (keyCode != KeyCode.Escape || !Application.isMobilePlatform) InputFeed.ReportDevice(InputDeviceKind.Keyboard);
                return;
            }
        }

        private bool CancelPressed()
        {
            if (_cancelMissing) return false;
            try
            {
                return Input.GetButtonDown("Cancel");
            }
            catch (ArgumentException)
            {
                _cancelMissing = true;
                return false;
            }
        }

        private static void WarnAboutMissingAxes()
        {
            foreach (var axis in RequiredAxes)
            {
                try
                {
                    Input.GetAxisRaw(axis);
                }
                catch (ArgumentException)
                {
                    Debug.LogWarning($"[UIKit] The Input Manager has no '{axis}' axis. UI Toolkit navigation and the Cancel back key need the default Horizontal, Vertical, Submit and Cancel entries (Edit > Project Settings > Input Manager).");
                }
            }
        }
    }
}
#endif
