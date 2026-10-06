#if ENABLE_INPUT_SYSTEM
using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.TestTools;
using UnityEngine.UIElements;

namespace FutureDigitalPlus.UIKit.Interaction.Tests
{
    /// <summary>
    /// The Input System backend with virtual devices. Compiled and run only when the Input System package is installed and the project uses it
    /// (Active Input Handling "Input System Package (New)" or "Both").
    /// </summary>
    public class InputSystemBackendTests
    {
        private InputSettings.BackgroundBehavior _background;
        private InputSettings.EditorInputBehaviorInPlayMode _editorBehavior;
        private InputDevice[] _devices;

        [SetUp]
        public void SetUp()
        {
            InputFeed.Reset();
            // In the Editor, input is dropped while the Game view is not focused (always, in batch mode).
            _background = InputSystem.settings.backgroundBehavior;
            _editorBehavior = InputSystem.settings.editorInputBehaviorInPlayMode;
            InputSystem.settings.backgroundBehavior = InputSettings.BackgroundBehavior.IgnoreFocus;
            InputSystem.settings.editorInputBehaviorInPlayMode = InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;
        }

        [TearDown]
        public void TearDown()
        {
            if (_devices != null)
                foreach (var d in _devices) InputSystem.RemoveDevice(d);
            _devices = null;
            InputSystem.settings.backgroundBehavior = _background;
            InputSystem.settings.editorInputBehaviorInPlayMode = _editorBehavior;
            InputFeed.Reset();
        }

        private static IEnumerator Settle()
        {
            yield return null;
            yield return null;
        }

        [UnityTest]
        public IEnumerator Devices_DriveTheProfile_InTheOrderTheyAreUsed()
        {
            var pad = InputSystem.AddDevice<Gamepad>();
            var keyboard = InputSystem.AddDevice<Keyboard>();
            var mouse = InputSystem.AddDevice<Mouse>();
            var touch = InputSystem.AddDevice<Touchscreen>();
            _devices = new InputDevice[] { pad, keyboard, mouse, touch };

            using (var profile = InputProfile.Bind(new VisualElement()))
            {
                InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.A));
                yield return Settle();
                Assert.AreEqual(InputDeviceKind.Keyboard, profile.Mode);
                InputSystem.QueueStateEvent(keyboard, new KeyboardState());
                yield return Settle();

                InputSystem.QueueStateEvent(pad, new GamepadState { buttons = 1u << (int)GamepadButton.South });
                yield return Settle();
                Assert.AreEqual(InputDeviceKind.Gamepad, profile.Mode);
                InputSystem.QueueStateEvent(pad, new GamepadState());
                yield return Settle();

                InputSystem.QueueStateEvent(mouse, new MouseState { position = new Vector2(100, 100) });
                yield return Settle();
                Assert.AreEqual(InputDeviceKind.Pointer, profile.Mode);

                InputSystem.QueueStateEvent(touch, new TouchState { touchId = 1, phase = UnityEngine.InputSystem.TouchPhase.Began, position = new Vector2(50, 50), pressure = 1f });
                yield return Settle();
                Assert.AreEqual(InputDeviceKind.Touch, profile.Mode);
                InputSystem.QueueStateEvent(touch, new TouchState { touchId = 1, phase = UnityEngine.InputSystem.TouchPhase.Ended, position = new Vector2(50, 50) });
                yield return Settle();

                // Mouse events right after a touch are emulation (see InputModeTracker); wait the guard out before a real mouse move.
                yield return new WaitForSecondsRealtime((float)InputModeTracker.TouchMouseGuardSeconds + 0.1f);
                InputSystem.QueueStateEvent(mouse, new MouseState { position = new Vector2(300, 300) });
                yield return Settle();
                Assert.AreEqual(InputDeviceKind.Pointer, profile.Mode);
            }
        }

        [UnityTest]
        public IEnumerator StickDriftIsIgnored_AStrongStickIsNot()
        {
            var pad = InputSystem.AddDevice<Gamepad>();
            _devices = new InputDevice[] { pad };

            using (var profile = InputProfile.Bind(new VisualElement()))
            {
                profile.Report(InputDeviceKind.Pointer);

                InputSystem.QueueStateEvent(pad, new GamepadState { leftStick = new Vector2(0.1f, 0f) });
                yield return Settle();
                Assert.AreEqual(InputDeviceKind.Pointer, profile.Mode, "a resting stick is not input");

                InputSystem.QueueStateEvent(pad, new GamepadState { leftStick = new Vector2(0f, 0.9f) });
                yield return Settle();
                Assert.AreEqual(InputDeviceKind.Gamepad, profile.Mode);
            }
        }

        [UnityTest]
        public IEnumerator EscapeAndGamepadEast_AreBackPresses()
        {
            var pad = InputSystem.AddDevice<Gamepad>();
            var keyboard = InputSystem.AddDevice<Keyboard>();
            _devices = new InputDevice[] { pad, keyboard };

            using (var source = new BackKeySource())
            {
                var presses = 0;
                source.Pressed += () => presses++;

                InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.Escape));
                yield return Settle();
                Assert.AreEqual(1, presses, "Escape");
                InputSystem.QueueStateEvent(keyboard, new KeyboardState());
                yield return Settle();
                Assert.AreEqual(1, presses, "releasing is not a press");

                InputSystem.QueueStateEvent(pad, new GamepadState { buttons = 1u << (int)GamepadButton.East });
                yield return Settle();
                Assert.AreEqual(2, presses, "gamepad east (B)");
            }
        }
    }
}
#endif
