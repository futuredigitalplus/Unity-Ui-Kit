using System.Collections.Generic;
using NUnit.Framework;

namespace FutureDigitalPlus.UIKit.Foundation.Tests
{
    /// <summary>The queue of a UI that is going away: everything closes once, and nothing a callback asks for afterwards can open.</summary>
    public class DialogQueueShutdownTests
    {
        [Test]
        public void Shutdown_ClosesEverythingOnce_AndDropsFollowUpsFromCallbacks()
        {
            DialogQueue queue = new DialogQueue();
            List<string> log = new List<string>();
            DialogHandle followUp = null;
            DialogRequest first = new DialogRequest
            {
                Id = "first",
                Callback = result =>
                {
                    log.Add("first:" + result.Reason);
                    followUp = queue.Enqueue(new DialogRequest { Id = "follow", Callback = r => log.Add("follow:" + r.Reason) });
                }
            };
            queue.Enqueue(first);
            queue.Enqueue(new DialogRequest { Id = "second", Callback = result => log.Add("second:" + result.Reason) });

            queue.Shutdown();

            Assert.That(log, Is.EqualTo(new[] { "second:Cleared", "first:Cleared" }));
            Assert.That(queue.IsModalOpen, Is.False);
            Assert.That(queue.WaitingCount, Is.EqualTo(0));
            Assert.That(followUp.State, Is.EqualTo(DialogHandleState.Closed));
            Assert.That(followUp.Result.Reason, Is.EqualTo(DialogCloseReason.Cleared));
            Assert.That(queue.IsShutDown, Is.True);
        }

        [Test]
        public void AfterShutdown_EnqueueReturnsAClosedHandle_WithoutShowingOrCallingBack()
        {
            DialogQueue queue = new DialogQueue();
            int showing = 0;
            int called = 0;
            queue.Showing += handle => showing++;
            queue.Shutdown();

            DialogHandle handle = queue.Enqueue(new DialogRequest { Callback = r => called++ });

            Assert.That(handle.State, Is.EqualTo(DialogHandleState.Closed));
            Assert.That(showing, Is.EqualTo(0));
            Assert.That(called, Is.EqualTo(0));
            Assert.That(queue.IsModalOpen, Is.False);
            Assert.That(queue.Cancel(handle), Is.False);
        }

        [Test]
        public void ACallbackThatAlwaysAsksForAnotherDialog_CannotLoopForeverDuringShutdown()
        {
            DialogQueue queue = new DialogQueue();
            int asked = 0;
            System.Action<DialogOutcome> again = null;
            again = result =>
            {
                asked++;
                queue.Enqueue(new DialogRequest { Callback = again });
            };
            queue.Enqueue(new DialogRequest { Callback = again });

            queue.Shutdown();

            Assert.That(asked, Is.EqualTo(1));
            Assert.That(queue.IsModalOpen, Is.False);
        }
    }
}
