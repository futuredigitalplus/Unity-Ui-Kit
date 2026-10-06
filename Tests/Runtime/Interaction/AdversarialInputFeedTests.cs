using System;
using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace FutureDigitalPlus.UIKit.Interaction.Tests
{
    /// <summary>The shared input feed with a backend or a subscriber that misbehaves, and with acquire and release out of balance.</summary>
    public class AdversarialInputFeedTests
    {
        private sealed class ThrowingBackend : IInputBackend
        {
            public void Start() => throw new InvalidOperationException("start");
            public void Stop() => throw new InvalidOperationException("stop");
            public void Poll() => throw new InvalidOperationException("poll");
        }

        private sealed class CountingBackend : IInputBackend
        {
            public int Started, Stopped, Polled;
            public void Start() => Started++;
            public void Stop() => Stopped++;
            public void Poll() => Polled++;
        }

        private ThrowingBackend _throwing;
        private CountingBackend _counting;

        [SetUp]
        public void SetUp()
        {
            LogAssert.ignoreFailingMessages = true;
            InputFeed.Reset();
            _throwing = new ThrowingBackend();
            _counting = new CountingBackend();
        }

        [TearDown]
        public void TearDown()
        {
            LogAssert.ignoreFailingMessages = true;
            InputFeed.UnregisterBackend(_throwing);
            InputFeed.UnregisterBackend(_counting);
            InputFeed.Reset();
            LogAssert.ignoreFailingMessages = false;
        }

        [UnityTest]
        public IEnumerator ABackendThatThrows_DoesNotStopTheOthersFromStartingPollingOrStopping()
        {
            if (!Application.isPlaying) Assert.Ignore("Needs Play Mode (the poll driver).");
            LogAssert.ignoreFailingMessages = true;
            InputFeed.RegisterBackend(_throwing);
            InputFeed.RegisterBackend(_counting);

            InputFeed.Acquire();
            Assert.AreEqual(1, _counting.Started, "the backend after the faulty one started");
            yield return null;
            yield return null;
            Assert.Greater(_counting.Polled, 0, "the driver exists and polls the healthy backend");

            InputFeed.Release();
            Assert.AreEqual(1, _counting.Stopped);
            var polled = _counting.Polled;
            yield return null;
            Assert.AreEqual(polled, _counting.Polled, "polling stopped with the last release");
        }

        [Test]
        public void ASubscriberThatThrows_DoesNotBreakTheReporter()
        {
            LogAssert.ignoreFailingMessages = true;
            InputFeed.DeviceUsed += kind => throw new InvalidOperationException("device");
            InputFeed.BackPressed += () => throw new InvalidOperationException("back");

            Assert.DoesNotThrow(() => InputFeed.ReportDevice(InputDeviceKind.Touch));
            Assert.DoesNotThrow(() => InputFeed.ReportBack());
        }

        [Test]
        public void ReleaseWithoutAcquireAndDoubleRelease_NeverStopsAnotherOwnersFeed()
        {
            InputFeed.RegisterBackend(_counting);
            InputFeed.Release();
            InputFeed.Release();

            InputFeed.Acquire();
            InputFeed.Acquire();
            InputFeed.Release();
            Assert.AreEqual(0, _counting.Stopped, "one owner is left");
            InputFeed.Release();
            Assert.AreEqual(1, _counting.Stopped);
            InputFeed.Release();
            Assert.AreEqual(1, _counting.Stopped);
        }

        [Test]
        public void RegisterAndUnregisterNull_AreRefusedWithoutDamage()
        {
            Assert.Throws<ArgumentNullException>(() => InputFeed.RegisterBackend(null));
            Assert.IsFalse(InputFeed.UnregisterBackend(null));
            Assert.IsFalse(InputFeed.UnregisterBackend(_counting));
            InputFeed.RegisterBackend(_counting);
            InputFeed.RegisterBackend(new CountingBackend());   // same concrete type: ignored
            InputFeed.Acquire();
            Assert.AreEqual(1, _counting.Started);
            InputFeed.Release();
        }

        [Test]
        public void ABackendRegisteredWhileRunning_IsStartedAtOnce_AndStoppedWhenUnregistered()
        {
            InputFeed.Acquire();
            InputFeed.RegisterBackend(_counting);
            Assert.AreEqual(1, _counting.Started);
            Assert.IsTrue(InputFeed.UnregisterBackend(_counting));
            Assert.AreEqual(1, _counting.Stopped);
            InputFeed.Release();
        }
    }
}
