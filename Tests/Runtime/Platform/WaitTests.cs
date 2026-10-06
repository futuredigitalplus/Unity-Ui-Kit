using System;
using System.Collections;
using System.Threading;
using FutureDigitalPlus.UIKit.Animation;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace FutureDigitalPlus.UIKit.PlatformSmoke.Tests
{
    /// <summary>
    /// <c>Wait.Until</c> and <c>Wait.While</c> run their condition on the main thread, frame paced, with a real-time timeout. Upstream ran the condition
    /// on a thread-pool thread, which is illegal for UnityEngine calls and never completes on WebGL.
    /// </summary>
    public class WaitTests
    {
        [UnityTest]
        public IEnumerator Until_RunsTheConditionOnTheMainThread_AndCompletes()
        {
            int main = Thread.CurrentThread.ManagedThreadId;
            int conditionThread = -1;
            int polls = 0;

            var task = Wait.Until(() => { conditionThread = Thread.CurrentThread.ManagedThreadId; return ++polls > 3; }, 0, 5000);
            yield return PlatformTestUtil.Await(task);

            Assert.IsTrue(task.IsCompletedSuccessfully, "the wait completes");
            Assert.AreEqual(main, conditionThread, "the condition runs on the main thread");
            Assert.AreEqual(4, polls, "one check per frame until it is true");
        }

        [UnityTest]
        public IEnumerator Until_TheConditionMayCallUnityEngineApis()
        {
            // Would throw "can only be called from the main thread" on a thread-pool thread.
            var go = new GameObject("WaitTests");
            try
            {
                var task = Wait.Until(() => go.transform.position == Vector3.zero && Time.frameCount > 0, 0, 5000);
                yield return PlatformTestUtil.Await(task);
                Assert.IsTrue(task.IsCompletedSuccessfully);
            }
            finally
            {
                UnityEngine.Object.Destroy(go);
            }
        }

        [Test]
        public void Until_AlreadyTrue_CompletesWithoutWaitingForAFrame()
        {
            var task = Wait.Until(() => true);
            Assert.IsTrue(task.IsCompletedSuccessfully);
        }

        [UnityTest]
        public IEnumerator While_CompletesWhenTheConditionTurnsFalse()
        {
            bool busy = true;
            var task = Wait.While(() => busy, 0, 5000);

            yield return null;
            yield return null;
            Assert.IsFalse(task.IsCompleted, "still waiting while the condition is true");

            busy = false;
            yield return PlatformTestUtil.Await(task);
            Assert.IsTrue(task.IsCompletedSuccessfully);
        }

        [UnityTest]
        public IEnumerator Until_ThrowsTimeoutException_AfterTheTimeout()
        {
            double start = Time.realtimeSinceStartupAsDouble;
            var task = Wait.Until(() => false, 10, 150);
            yield return PlatformTestUtil.Await(task);
            double elapsed = Time.realtimeSinceStartupAsDouble - start;

            Assert.IsTrue(task.IsFaulted, "the wait timed out");
            Assert.IsInstanceOf<TimeoutException>(task.Exception.InnerException);
            Assert.GreaterOrEqual(elapsed, 0.14, "not before the timeout");
            Assert.Less(elapsed, 5.0, "and not much later");
        }

        [UnityTest]
        public IEnumerator While_ThrowsTimeoutException_WhenTheConditionStaysTrue()
        {
            var task = Wait.While(() => true, 0, 100);
            yield return PlatformTestUtil.Await(task);

            Assert.IsTrue(task.IsFaulted);
            Assert.IsInstanceOf<TimeoutException>(task.Exception.InnerException);
        }

        [UnityTest]
        public IEnumerator Until_FrequencyIsTheMinimumGapBetweenChecks()
        {
            int polls = 0;
            double start = Time.realtimeSinceStartupAsDouble;
            var task = Wait.Until(() => { polls++; return Time.realtimeSinceStartupAsDouble - start > 0.6; }, 200, 5000);
            yield return PlatformTestUtil.Await(task);

            Assert.IsTrue(task.IsCompletedSuccessfully);
            Assert.LessOrEqual(polls, 5, "about one check per 200 ms, not one per frame (polls: " + polls + ")");
        }

        [Test]
        public void NullCondition_Throws()
        {
            Assert.Throws<ArgumentNullException>(() => Wait.Until(null));
            Assert.Throws<ArgumentNullException>(() => Wait.While(null));
        }
    }
}
