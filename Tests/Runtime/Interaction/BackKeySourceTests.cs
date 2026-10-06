using System.Collections;
using NUnit.Framework;
using UnityEngine.TestTools;
using UnityEngine.UIElements;

namespace FutureDigitalPlus.UIKit.Interaction.Tests
{
    public class BackKeySourceTests
    {
        private InteractionPanel _panel;
        private CountingBackend _backend;

        [SetUp]
        public void SetUp()
        {
            LogAssert.ignoreFailingMessages = true;
            InputFeed.Reset();
        }

        [TearDown]
        public void TearDown()
        {
            if (_backend != null) InputFeed.UnregisterBackend(_backend);
            _backend = null;
            InputFeed.Reset();
            LogAssert.ignoreFailingMessages = false;
            _panel?.Dispose();
            _panel = null;
        }

        [UnityTest]
        public IEnumerator Pressed_IsRaisedOncePerFrame()
        {
            using (var source = new BackKeySource())
            {
                var presses = 0;
                source.Pressed += () => presses++;

                InputFeed.ReportBack();
                InputFeed.ReportBack();
                Assert.AreEqual(1, presses, "two reports in one frame are one physical press");

                yield return null;
                InputFeed.ReportBack();
                Assert.AreEqual(2, presses);
            }
        }

        [UnityTest]
        public IEnumerator CancelEvent_OnABoundRoot_IsABackPress()
        {
            _panel = InteractionPanel.Create();
            var button = new Button { text = "x" };
            _panel.Screen.Add(button);
            yield return InteractionPanel.Frames();

            using (var source = new BackKeySource())
            {
                source.Bind(_panel.Root);
                var presses = 0;
                source.Pressed += () => presses++;

                Send.Cancel(button);
                Assert.AreEqual(1, presses);

                InputFeed.ReportBack();
                Assert.AreEqual(1, presses, "the device poll of the same frame is the same press");
            }
        }

        [UnityTest]
        public IEnumerator CancelEvent_IsNotConsumed()
        {
            _panel = InteractionPanel.Create();
            var button = new Button { text = "x" };
            _panel.Screen.Add(button);
            yield return InteractionPanel.Frames();

            var reachedTarget = false;
            button.RegisterCallback<NavigationCancelEvent>(_ => reachedTarget = true);
            using (var source = new BackKeySource())
            {
                source.Bind(_panel.Root);
                Send.Cancel(button);
            }

            Assert.IsTrue(reachedTarget, "a dropdown or popup still gets its own Cancel");
        }

        [UnityTest]
        public IEnumerator Bind_Replaces_AndDisposeStopsEverything()
        {
            _panel = InteractionPanel.Create();
            var button = new Button { text = "x" };
            _panel.Screen.Add(button);
            yield return InteractionPanel.Frames();

            var source = new BackKeySource();
            var presses = 0;
            source.Pressed += () => presses++;
            source.Bind(_panel.Root);
            source.Bind(null);                 // unbinds
            Send.Cancel(button);
            Assert.AreEqual(0, presses, "no longer bound to the element");

            source.Bind(_panel.Root);
            source.Dispose();
            Send.Cancel(button);
            InputFeed.ReportBack();
            Assert.AreEqual(0, presses, "disposed");
        }

        [UnityTest]
        public IEnumerator Backends_RunOnlyWhileSomethingListens()
        {
            _backend = new CountingBackend();
            InputFeed.RegisterBackend(_backend);

            Assert.AreEqual(0, _backend.Started);
            var first = new BackKeySource();
            var second = new BackKeySource();
            Assert.AreEqual(1, _backend.Started, "started once, for the first listener");

            second.Dispose();
            Assert.AreEqual(0, _backend.Stopped, "still one listener");
            first.Dispose();
            Assert.AreEqual(1, _backend.Stopped, "stopped with the last one");
            yield return null;
        }

        [UnityTest]
        public IEnumerator Backend_Poll_Runs_EveryFrame_WhileStarted()
        {
            _backend = new CountingBackend();
            InputFeed.RegisterBackend(_backend);
            using (new BackKeySource())
            {
                yield return InteractionPanel.Frames(3);
                Assert.GreaterOrEqual(_backend.Polled, 2);
            }
        }

        [Test]
        public void RegisterBackend_IgnoresASecondBackendOfTheSameType_AndNull()
        {
            _backend = new CountingBackend();
            InputFeed.RegisterBackend(_backend);
            InputFeed.RegisterBackend(new CountingBackend());
            Assert.Throws<System.ArgumentNullException>(() => InputFeed.RegisterBackend(null));
            Assert.IsFalse(InputFeed.UnregisterBackend(null));

            using (new BackKeySource())
                Assert.AreEqual(1, _backend.Started, "the second backend of the same type was not added");
        }

        private sealed class CountingBackend : IInputBackend
        {
            public int Started, Stopped, Polled;
            public void Start() => Started++;
            public void Stop() => Stopped++;
            public void Poll() => Polled++;
        }
    }
}
