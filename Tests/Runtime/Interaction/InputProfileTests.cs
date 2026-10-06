using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UIElements;

namespace FutureDigitalPlus.UIKit.Interaction.Tests
{
    public class InputProfileTests
    {
        private InteractionPanel _panel;

        [SetUp]
        public void SetUp()
        {
            LogAssert.ignoreFailingMessages = true;   // PanelSettings without a runtime theme log a warning
            InputFeed.Reset();
        }

        [TearDown]
        public void TearDown()
        {
            InputFeed.Reset();
            LogAssert.ignoreFailingMessages = false;
            _panel?.Dispose();
            _panel = null;
        }

        private static string[] InputClasses(VisualElement root)
        {
            var list = new System.Collections.Generic.List<string>();
            foreach (var c in root.GetClasses())
                if (c.StartsWith("fdp-input-")) list.Add(c);
            list.Sort();
            return list.ToArray();
        }

        [UnityTest]
        public IEnumerator Bind_StartsInThePlatformDefaultMode_AndWritesItsClasses()
        {
            _panel = InteractionPanel.Create();
            yield return InteractionPanel.Frames();

            using (var profile = InputProfile.Bind(_panel.Root))
            {
                var expected = InputModeTracker.PlatformDefault(Application.isMobilePlatform, Application.isConsolePlatform);
                Assert.AreEqual(expected, profile.Mode);
                Assert.AreEqual(profile.IsNavigationMode, _panel.Root.ClassListContains(InputProfile.NavigationClass));
            }
        }

        [UnityTest]
        public IEnumerator Report_SwitchesTheClasses_AndRaisesModeChanged()
        {
            _panel = InteractionPanel.Create();
            yield return InteractionPanel.Frames();
            using (var profile = InputProfile.Bind(_panel.Root))
            {
                var seen = new System.Collections.Generic.List<InputDeviceKind>();
                profile.ModeChanged += seen.Add;

                profile.Report(InputDeviceKind.Gamepad);
                CollectionAssert.AreEqual(new[] { "fdp-input-gamepad", "fdp-input-nav" }, InputClasses(_panel.Root));
                Assert.IsTrue(profile.IsNavigationMode);

                profile.Report(InputDeviceKind.Keyboard);
                CollectionAssert.AreEqual(new[] { "fdp-input-keyboard", "fdp-input-nav" }, InputClasses(_panel.Root));

                profile.Report(InputDeviceKind.Pointer);
                CollectionAssert.AreEqual(new[] { "fdp-input-mouse" }, InputClasses(_panel.Root));
                Assert.IsFalse(profile.IsNavigationMode);

                profile.Report(InputDeviceKind.Pointer);   // no change: no event
                CollectionAssert.AreEqual(new[] { InputDeviceKind.Gamepad, InputDeviceKind.Keyboard, InputDeviceKind.Pointer }, seen);
            }
        }

        [UnityTest]
        public IEnumerator Force_PinsTheMode_UntilItIsCleared()
        {
            _panel = InteractionPanel.Create();
            yield return InteractionPanel.Frames();
            using (var profile = InputProfile.Bind(_panel.Root))
            {
                profile.Report(InputDeviceKind.Pointer);
                profile.Force(InputDeviceKind.Keyboard);
                Assert.AreEqual(InputDeviceKind.Keyboard, profile.Mode);
                Assert.IsTrue(_panel.Root.ClassListContains(InputProfile.NavigationClass));

                InputFeed.ReportDevice(InputDeviceKind.Touch);
                Assert.AreEqual(InputDeviceKind.Keyboard, profile.Mode, "input does not change a forced mode");

                profile.Force(null);
                Assert.AreEqual(InputDeviceKind.Touch, profile.Mode);
                Assert.IsFalse(_panel.Root.ClassListContains(InputProfile.NavigationClass));
            }
        }

        [UnityTest]
        public IEnumerator PointerEvents_SwitchBetweenTouchAndMouse()
        {
            _panel = InteractionPanel.Create();
            var button = InteractionPanel.WithClasses(new Button { text = "Play" }, "ds-btn");
            _panel.Screen.Add(button);
            yield return InteractionPanel.Frames();
            using (var profile = InputProfile.Bind(_panel.Root))
            {
                Send.PointerDown(button, touch: true);
                Assert.AreEqual(InputDeviceKind.Touch, profile.Mode);
                Assert.IsTrue(_panel.Root.ClassListContains(InputProfile.TouchClass));

                // Right after a touch a mouse report is treated as emulation of that touch (see InputModeTracker); wait the guard out.
                yield return new WaitForSecondsRealtime((float)InputModeTracker.TouchMouseGuardSeconds + 0.1f);
                Send.PointerDown(button, touch: false);
                Assert.AreEqual(InputDeviceKind.Pointer, profile.Mode);
                Assert.IsTrue(_panel.Root.ClassListContains(InputProfile.PointerClass));
                Assert.IsFalse(_panel.Root.ClassListContains(InputProfile.TouchClass));
            }
        }

        [UnityTest]
        public IEnumerator PointerMove_CountsAsPointerInput()
        {
            _panel = InteractionPanel.Create();
            var button = InteractionPanel.WithClasses(new Button { text = "Play" }, "ds-btn");
            _panel.Screen.Add(button);
            yield return InteractionPanel.Frames();
            using (var profile = InputProfile.Bind(_panel.Root))
            {
                profile.Report(InputDeviceKind.Gamepad);
                Send.PointerMove(button, touch: false);
                Assert.AreEqual(InputDeviceKind.Pointer, profile.Mode, "moving the mouse ends gamepad mode");
            }
        }

        [UnityTest]
        public IEnumerator ProfilesShareDeviceFacts()
        {
            _panel = InteractionPanel.Create();
            var other = new VisualElement();
            _panel.Root.Add(other);
            var button = InteractionPanel.WithClasses(new Button { text = "Play" }, "ds-btn");
            _panel.Screen.Add(button);
            yield return InteractionPanel.Frames();
            using (var first = InputProfile.Bind(_panel.Root))
            using (var second = InputProfile.Bind(other))
            {
                Send.PointerDown(button, touch: true);
                Assert.AreEqual(InputDeviceKind.Touch, first.Mode);
                Assert.AreEqual(InputDeviceKind.Touch, second.Mode, "a touch on one panel is a touch for every profile");
            }
        }

        [UnityTest]
        public IEnumerator ManageTouchClass_False_LeavesTheTouchClassToSomeoneElse()
        {
            _panel = InteractionPanel.Create();
            yield return InteractionPanel.Frames();
            using (var profile = InputProfile.Bind(_panel.Root))
            {
                profile.Report(InputDeviceKind.Touch);
                Assert.IsTrue(_panel.Root.ClassListContains(InputProfile.TouchClass));

                profile.ManageTouchClass = false;
                Assert.IsFalse(_panel.Root.ClassListContains(InputProfile.TouchClass), "removed once when switched off");

                _panel.Root.AddToClassList(InputProfile.TouchClass);   // the other owner of the class
                profile.Report(InputDeviceKind.Keyboard);
                Assert.IsTrue(_panel.Root.ClassListContains(InputProfile.TouchClass), "no longer touched by the profile");

                profile.ManageTouchClass = true;
                Assert.IsFalse(_panel.Root.ClassListContains(InputProfile.TouchClass), "the profile owns it again and the mode is keyboard");
            }
        }

        [UnityTest]
        public IEnumerator Dispose_RemovesTheClasses_AndStopsListening()
        {
            _panel = InteractionPanel.Create();
            var button = InteractionPanel.WithClasses(new Button { text = "Play" }, "ds-btn");
            _panel.Screen.Add(button);
            yield return InteractionPanel.Frames();

            var profile = InputProfile.Bind(_panel.Root);
            profile.Report(InputDeviceKind.Gamepad);
            profile.Dispose();
            CollectionAssert.IsEmpty(InputClasses(_panel.Root));

            Send.PointerDown(button, touch: true);
            InputFeed.ReportDevice(InputDeviceKind.Keyboard);
            CollectionAssert.IsEmpty(InputClasses(_panel.Root), "a disposed profile writes nothing");
            Assert.AreEqual(InputDeviceKind.Gamepad, profile.Mode, "and does not track any more");
        }

        [UnityTest]
        public IEnumerator Bind_Null_ReturnsNull()
        {
            Assert.IsNull(InputProfile.Bind(null));
            yield return null;
        }

        [UnityTest]
        public IEnumerator NavigationEvent_InPointerMode_MeansAKeyboardOrGamepadIsInUse()
        {
            _panel = InteractionPanel.Create();
            var button = InteractionPanel.WithClasses(new Button { text = "Play" }, "ds-btn");
            _panel.Screen.Add(button);
            yield return InteractionPanel.Frames();
            using (var profile = InputProfile.Bind(_panel.Root))
            {
                profile.Report(InputDeviceKind.Pointer);
                Send.Move(button, NavigationMoveEvent.Direction.Down);
                Assert.AreEqual(InputDeviceKind.Keyboard, profile.Mode);
                Assert.IsTrue(profile.IsNavigationMode);
            }
        }

        [UnityTest]
        public IEnumerator NavigationEnabled_False_StopsSubmitAndMove_AndDoesNotChangeTheMode()
        {
            _panel = InteractionPanel.Create();
            var first = InteractionPanel.WithClasses(new Button { text = "One", name = "b1" }, "ds-btn");
            var second = InteractionPanel.WithClasses(new Button { text = "Two", name = "b2" }, "ds-btn");
            var clicks = 0;
            first.clicked += () => clicks++;
            _panel.Screen.Add(first);
            _panel.Screen.Add(second);
            yield return InteractionPanel.Frames();

            using (var profile = InputProfile.Bind(_panel.Root))
            {
                profile.Report(InputDeviceKind.Pointer);
                first.Focus();
                yield return null;

                profile.NavigationEnabled = false;
                Send.Submit(first);
                Send.Move(first, NavigationMoveEvent.Direction.Down);
                yield return null;
                Assert.AreEqual(0, clicks, "Submit is swallowed");
                Assert.AreEqual("b1", _panel.FocusedName, "focus does not move");
                Assert.AreEqual(InputDeviceKind.Pointer, profile.Mode, "a gated panel does not count as keyboard use");

                profile.NavigationEnabled = true;
                Send.Submit(first);
                Assert.AreEqual(1, clicks, "Submit reaches the button again");
            }
        }
    }
}
