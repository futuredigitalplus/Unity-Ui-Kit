using System.Collections.Generic;
using NUnit.Framework;

namespace FutureDigitalPlus.UIKit.Foundation.Tests
{
    [TestFixture]
    public sealed class BackDispatcherTests
    {
        [Test]
        public void Dispatch_WithNoHandlers_ReturnsFalse()
        {
            Assert.That(new BackDispatcher().Dispatch(), Is.False);
        }

        [Test]
        public void Dispatch_RunsHighestPriorityFirstAndStopsAtTheFirstThatConsumes()
        {
            BackDispatcher dispatcher = new BackDispatcher();
            List<string> calls = new List<string>();
            dispatcher.Register(BackPriority.Router, () => { calls.Add("router"); return true; });
            dispatcher.Register(BackPriority.Dialog, () => { calls.Add("dialog"); return false; });
            dispatcher.Register(BackPriority.Screen, () => { calls.Add("screen"); return true; });
            dispatcher.Register(BackPriority.Loading, () => { calls.Add("loading"); return false; });

            Assert.That(dispatcher.Dispatch(), Is.True);
            Assert.That(calls, Is.EqualTo(new[] { "loading", "dialog", "screen" }));
        }

        [Test]
        public void Dispatch_EqualPriorities_RunNewestFirst()
        {
            BackDispatcher dispatcher = new BackDispatcher();
            List<string> calls = new List<string>();
            dispatcher.Register(BackPriority.Overlay, () => { calls.Add("first"); return false; });
            dispatcher.Register(BackPriority.Overlay, () => { calls.Add("second"); return false; });
            dispatcher.Dispatch();
            Assert.That(calls, Is.EqualTo(new[] { "second", "first" }));
        }

        [Test]
        public void Unregister_RemovesTheHandler()
        {
            BackDispatcher dispatcher = new BackDispatcher();
            int token = dispatcher.Register(BackPriority.Screen, () => true);
            Assert.That(dispatcher.Unregister(token), Is.True);
            Assert.That(dispatcher.Unregister(token), Is.False);
            Assert.That(dispatcher.Dispatch(), Is.False);
            Assert.That(dispatcher.Count, Is.EqualTo(0));
        }

        [Test]
        public void Dispatch_SkipsAHandlerThatWasUnregisteredByAnEarlierOne()
        {
            BackDispatcher dispatcher = new BackDispatcher();
            int victim = 0;
            bool victimRan = false;
            dispatcher.Register(BackPriority.Dialog, () => { dispatcher.Unregister(victim); return false; });
            victim = dispatcher.Register(BackPriority.Screen, () => { victimRan = true; return true; });
            Assert.That(dispatcher.Dispatch(), Is.False);
            Assert.That(victimRan, Is.False);
        }

        [Test]
        public void Register_NullHandler_Throws()
        {
            Assert.Throws<System.ArgumentNullException>(() => new BackDispatcher().Register(0, null));
        }
    }
}
