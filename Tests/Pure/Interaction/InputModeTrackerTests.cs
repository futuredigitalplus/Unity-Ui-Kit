using NUnit.Framework;

namespace FutureDigitalPlus.UIKit.Interaction.Tests
{
    public class InputModeTrackerTests
    {
        private double _now;

        [SetUp]
        public void SetUp() => _now = 100d;

        private InputModeTracker Create(InputDeviceKind initial = InputDeviceKind.Pointer) => new InputModeTracker(initial, () => _now);

        [Test]
        public void StartsInTheGivenMode()
        {
            var tracker = Create(InputDeviceKind.Gamepad);
            Assert.AreEqual(InputDeviceKind.Gamepad, tracker.Mode);
            Assert.IsTrue(tracker.IsNavigation);
            Assert.IsNull(tracker.Forced);
        }

        [Test]
        public void Observe_ReportsOnlyRealChanges()
        {
            var tracker = Create();
            Assert.IsFalse(tracker.Observe(InputDeviceKind.Pointer), "same mode");
            Assert.IsTrue(tracker.Observe(InputDeviceKind.Keyboard));
            Assert.IsFalse(tracker.Observe(InputDeviceKind.Keyboard));
            Assert.IsTrue(tracker.Observe(InputDeviceKind.Gamepad));
            Assert.AreEqual(InputDeviceKind.Gamepad, tracker.Mode);
        }

        [TestCase(InputDeviceKind.Pointer, false)]
        [TestCase(InputDeviceKind.Touch, false)]
        [TestCase(InputDeviceKind.Keyboard, true)]
        [TestCase(InputDeviceKind.Gamepad, true)]
        public void IsNavigation_IsTrueOnlyForKeyboardAndGamepad(InputDeviceKind kind, bool expected)
        {
            Assert.AreEqual(expected, Create(kind).IsNavigation);
            Assert.AreEqual(expected, InputModeTracker.IsNavigationKind(kind));
        }

        [Test]
        public void Force_OverridesObservedInput_AndNullReturnsToIt()
        {
            var tracker = Create();
            tracker.Observe(InputDeviceKind.Touch);

            Assert.IsTrue(tracker.Force(InputDeviceKind.Keyboard));
            Assert.AreEqual(InputDeviceKind.Keyboard, tracker.Mode);
            Assert.IsTrue(tracker.IsNavigation);

            _now += 1d;
            Assert.IsFalse(tracker.Observe(InputDeviceKind.Pointer), "a forced mode does not change while input arrives");
            Assert.AreEqual(InputDeviceKind.Keyboard, tracker.Mode);

            Assert.IsTrue(tracker.Force(null));
            Assert.AreEqual(InputDeviceKind.Pointer, tracker.Mode, "the last observed device is back in charge");
        }

        [Test]
        public void Force_ToTheCurrentMode_IsNotAChange()
        {
            var tracker = Create(InputDeviceKind.Gamepad);
            Assert.IsFalse(tracker.Force(InputDeviceKind.Gamepad));
        }

        [TestCase(false, false, InputDeviceKind.Pointer)]
        [TestCase(true, false, InputDeviceKind.Touch)]
        [TestCase(false, true, InputDeviceKind.Gamepad)]
        [TestCase(true, true, InputDeviceKind.Gamepad)]
        public void PlatformDefault_FollowsTheDeviceClass(bool mobile, bool console, InputDeviceKind expected)
        {
            Assert.AreEqual(expected, InputModeTracker.PlatformDefault(mobile, console));
        }

        [TestCase(0f, false)]
        [TestCase(0.1f, false)]
        [TestCase(0.29f, false)]
        [TestCase(0.3f, true)]
        [TestCase(0.9f, true)]
        [TestCase(1f, true)]
        public void IsSignificantStick_IgnoresDrift(float magnitude, bool expected)
        {
            Assert.AreEqual(expected, InputModeTracker.IsSignificantStick(magnitude));
        }

        [TestCase("touch", InputDeviceKind.Touch)]
        [TestCase("mouse", InputDeviceKind.Pointer)]
        [TestCase("pen", InputDeviceKind.Pointer)]
        [TestCase("tracked", InputDeviceKind.Pointer)]
        [TestCase("", InputDeviceKind.Pointer)]
        [TestCase(null, InputDeviceKind.Pointer)]
        public void FromPointerType_OnlyTouchIsTouch(string pointerType, InputDeviceKind expected)
        {
            Assert.AreEqual(expected, InputModeTracker.FromPointerType(pointerType));
        }

        [Test]
        public void MouseRightAfterTouch_IsEmulationAndIgnored()
        {
            var tracker = Create();
            tracker.Observe(InputDeviceKind.Touch);

            _now += InputModeTracker.TouchMouseGuardSeconds * 0.5;
            Assert.IsFalse(tracker.Observe(InputDeviceKind.Pointer));
            Assert.AreEqual(InputDeviceKind.Touch, tracker.Mode);

            _now += InputModeTracker.TouchMouseGuardSeconds;
            Assert.IsTrue(tracker.Observe(InputDeviceKind.Pointer), "a real mouse is picked up once the guard has passed");
            Assert.AreEqual(InputDeviceKind.Pointer, tracker.Mode);
        }

        [Test]
        public void ANewTouchRestartsTheGuard()
        {
            var tracker = Create();
            tracker.Observe(InputDeviceKind.Touch);
            _now += 0.6;
            tracker.Observe(InputDeviceKind.Touch);
            _now += 0.6;
            Assert.IsFalse(tracker.Observe(InputDeviceKind.Pointer), "1.2 s after the first touch but only 0.6 s after the last one");
        }

        [Test]
        public void TheGuardOnlyCoversTouchToMouse()
        {
            var tracker = Create();
            tracker.Observe(InputDeviceKind.Touch);

            Assert.IsTrue(tracker.Observe(InputDeviceKind.Keyboard), "keyboard right after touch is real");
            Assert.IsTrue(tracker.Observe(InputDeviceKind.Pointer), "and the mouse after the keyboard is real too");
        }
    }
}
