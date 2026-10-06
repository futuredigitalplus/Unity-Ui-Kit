using System;
using System.Collections.Generic;
using NUnit.Framework;

namespace FutureDigitalPlus.UIKit.Foundation.Tests
{
    /// <summary>A view that throws must never leave the queue stuck behind a dialog nobody can see.</summary>
    [TestFixture]
    public sealed class DialogQueueFaultTests
    {
        List<string> _log;

        [SetUp]
        public void SetUp()
        {
            _log = new List<string>();
            UiLog.Sink = (level, text, exception) => _log.Add(text);
        }

        [TearDown]
        public void TearDown()
        {
            UiLog.Sink = null;
        }

        DialogRequest Request(string id, List<DialogOutcome> results)
        {
            DialogRequest request = new DialogRequest { Id = id, Callback = result => results.Add(result) };
            request.AddButton(new DialogButton(UiText.FromLiteral("Cancel"), DialogButtonRole.Cancel));
            request.AddButton(new DialogButton(UiText.FromLiteral("OK")));
            return request;
        }

        [Test]
        public void AShowingHandlerThatThrows_ClosesThatDialogAndShowsTheNext()
        {
            DialogQueue queue = new DialogQueue();
            List<DialogOutcome> results = new List<DialogOutcome>();
            List<string> shown = new List<string>();
            queue.Showing += handle =>
            {
                shown.Add(handle.Request.Id);
                if (handle.Request.Id == "broken")
                    throw new InvalidOperationException("cannot build the view");
            };
            queue.Enqueue(Request("broken", results));
            DialogHandle next = queue.Enqueue(Request("fine", results));

            Assert.That(shown, Is.EqualTo(new[] { "broken", "fine" }));
            Assert.That(queue.Current, Is.SameAs(next));
            Assert.That(results.Count, Is.EqualTo(1), "the broken dialog's callback ran exactly once");
            Assert.That(results[0].Reason, Is.EqualTo(DialogCloseReason.Programmatic));
            Assert.That(_log.Exists(line => line.Contains("cannot build the view")), Is.True);
        }

        [Test]
        public void AShowingHandlerThatThrowsOnTheOnlyDialog_LeavesNoModalOpen()
        {
            DialogQueue queue = new DialogQueue();
            List<bool> states = new List<bool>();
            queue.ModalStateChanged += states.Add;
            queue.Showing += handle => throw new InvalidOperationException("boom");
            List<DialogOutcome> results = new List<DialogOutcome>();
            DialogHandle handle2 = queue.Enqueue(Request("broken", results));
            Assert.That(queue.IsModalOpen, Is.False);
            Assert.That(queue.Current, Is.Null);
            Assert.That(handle2.State, Is.EqualTo(DialogHandleState.Closed));
            Assert.That(states, Is.EqualTo(new[] { true, false }), "the table timer is resumed again");
        }

        [Test]
        public void AShowingHandlerThatAnswersTheDialogAndThenThrows_DoesNotRunTheCallbackTwice()
        {
            DialogQueue queue = new DialogQueue();
            List<DialogOutcome> results = new List<DialogOutcome>();
            queue.Showing += handle =>
            {
                queue.Complete(handle, 1);
                throw new InvalidOperationException("late failure");
            };
            queue.Enqueue(Request("a", results));
            Assert.That(results.Count, Is.EqualTo(1));
            Assert.That(results[0].Reason, Is.EqualTo(DialogCloseReason.Button));
            Assert.That(queue.Current, Is.Null);
        }

        [Test]
        public void AClosedHandlerThatThrows_DoesNotStallTheQueue()
        {
            DialogQueue queue = new DialogQueue();
            List<DialogOutcome> results = new List<DialogOutcome>();
            queue.Closed += handle => throw new InvalidOperationException("teardown failed");
            DialogHandle first = queue.Enqueue(Request("a", results));
            DialogHandle second = queue.Enqueue(Request("b", results));
            Assert.DoesNotThrow(() => queue.Complete(first, 1));
            Assert.That(queue.Current, Is.SameAs(second));
            Assert.That(_log.Exists(line => line.Contains("teardown failed")), Is.True);
        }

        [Test]
        public void ACallbackThatClosesEverything_RunsEachRemainingCallbackOnce()
        {
            DialogQueue queue = new DialogQueue();
            List<DialogOutcome> results = new List<DialogOutcome>();
            DialogRequest first = Request("a", results);
            first.Callback = result =>
            {
                results.Add(result);
                queue.CloseAll();
            };
            DialogHandle a = queue.Enqueue(first);
            queue.Enqueue(Request("b", results));
            queue.Enqueue(Request("c", results));
            queue.Complete(a, 1);
            Assert.That(queue.Current, Is.Null);
            Assert.That(queue.WaitingCount, Is.EqualTo(0));
            Assert.That(results.Count, Is.EqualTo(3));
            Assert.That(results[1].Reason, Is.EqualTo(DialogCloseReason.Cleared));
            Assert.That(results[2].Reason, Is.EqualTo(DialogCloseReason.Cleared));
        }

        [Test]
        public void ACallbackThatCancelsAnotherWaitingDialog_RunsThatCallbackOnce()
        {
            DialogQueue queue = new DialogQueue();
            List<DialogOutcome> results = new List<DialogOutcome>();
            DialogHandle victim = null;
            DialogRequest first = Request("a", results);
            first.Callback = result =>
            {
                results.Add(result);
                queue.Cancel(victim);
            };
            DialogHandle a = queue.Enqueue(first);
            victim = queue.Enqueue(Request("b", results));
            DialogHandle c = queue.Enqueue(Request("c", results));
            queue.Complete(a, 1);
            Assert.That(results.Count, Is.EqualTo(2), "a and b, once each");
            Assert.That(results[1].Reason, Is.EqualTo(DialogCloseReason.Programmatic));
            Assert.That(queue.Current, Is.SameAs(c));
        }

        [Test]
        public void AFollowUpWithTheSameDedupeKeyAsTheClosingDialog_IsAccepted()
        {
            DialogQueue queue = new DialogQueue();
            List<DialogOutcome> results = new List<DialogOutcome>();
            DialogHandle second = null;
            DialogRequest first = Request("a", results);
            first.DedupeKey = "same";
            first.Callback = result => second = queue.Enqueue(new DialogRequest { Id = "again", DedupeKey = "same" });
            DialogHandle a = queue.Enqueue(first);
            queue.Complete(a, 1);
            Assert.That(second, Is.Not.Null.And.Not.SameAs(a));
            Assert.That(queue.Current, Is.SameAs(second));
        }
    }
}
