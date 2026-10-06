using System;
using System.Collections.Generic;
using NUnit.Framework;

namespace FutureDigitalPlus.UIKit.Foundation.Tests
{
    [TestFixture]
    public sealed class DialogQueueTests
    {
        List<DialogOutcome> _results;
        List<string> _log;

        [SetUp]
        public void SetUp()
        {
            _results = new List<DialogOutcome>();
            _log = new List<string>();
            UiLog.Sink = (level, text, exception) => _log.Add(text);
        }

        [TearDown]
        public void TearDown()
        {
            UiLog.Sink = null;
        }

        DialogRequest Request(string id, DialogPriority priority = DialogPriority.Normal, bool cancel = true)
        {
            DialogRequest request = new DialogRequest { Id = id, Priority = priority, Callback = result => { _results.Add(result); _log.Add(id + ":" + result.Reason); } };
            if (cancel)
                request.AddButton(new DialogButton(UiText.FromLiteral("Cancel"), DialogButtonRole.Cancel));
            request.AddButton(new DialogButton(UiText.FromLiteral("OK")));
            return request;
        }

        [Test]
        public void Enqueue_OnAnEmptyQueue_ShowsAtOnce()
        {
            DialogQueue queue = new DialogQueue();
            List<DialogHandle> shown = new List<DialogHandle>();
            queue.Showing += shown.Add;
            DialogHandle handle = queue.Enqueue(Request("a"));
            Assert.That(shown, Is.EqualTo(new[] { handle }));
            Assert.That(queue.Current, Is.SameAs(handle));
            Assert.That(handle.State, Is.EqualTo(DialogHandleState.Showing));
            Assert.That(queue.IsModalOpen, Is.True);
        }

        [Test]
        public void OnlyOneDialogShowsAtATime_TheRestWaitInArrivalOrder()
        {
            DialogQueue queue = new DialogQueue();
            DialogHandle a = queue.Enqueue(Request("a"));
            DialogHandle b = queue.Enqueue(Request("b"));
            DialogHandle c = queue.Enqueue(Request("c"));
            Assert.That(queue.Current, Is.SameAs(a));
            Assert.That(queue.WaitingCount, Is.EqualTo(2));

            queue.Complete(a, 1);
            Assert.That(queue.Current, Is.SameAs(b));
            queue.Complete(b, 1);
            Assert.That(queue.Current, Is.SameAs(c));
            queue.Complete(c, 0);
            Assert.That(queue.Current, Is.Null);
            Assert.That(queue.IsModalOpen, Is.False);
        }

        [Test]
        public void HigherPriorityWaitsAheadOfLowerButNeverPreemptsTheShowingDialog()
        {
            DialogQueue queue = new DialogQueue();
            DialogHandle first = queue.Enqueue(Request("first"));
            DialogHandle normal = queue.Enqueue(Request("normal"));
            DialogHandle high = queue.Enqueue(Request("high", DialogPriority.High));
            DialogHandle system = queue.Enqueue(Request("system", DialogPriority.System));
            Assert.That(queue.Current, Is.SameAs(first));

            queue.Complete(first, 1);
            Assert.That(queue.Current, Is.SameAs(system));
            queue.Complete(system, 1);
            Assert.That(queue.Current, Is.SameAs(high));
            queue.Complete(high, 1);
            Assert.That(queue.Current, Is.SameAs(normal));
        }

        [Test]
        public void Complete_InvokesTheCallbackOnceWithTheButtonIndex()
        {
            DialogQueue queue = new DialogQueue();
            DialogHandle handle = queue.Enqueue(Request("a"));
            Assert.That(queue.Complete(handle, 1), Is.True);
            Assert.That(queue.Complete(handle, 1), Is.False);
            Assert.That(_results.Count, Is.EqualTo(1));
            Assert.That(_results[0].ButtonIndex, Is.EqualTo(1));
            Assert.That(_results[0].Reason, Is.EqualTo(DialogCloseReason.Button));
            Assert.That(_results[0].IsButton, Is.True);
            Assert.That(handle.State, Is.EqualTo(DialogHandleState.Closed));
        }

        [Test]
        public void Complete_WithAnOutOfRangeButton_IsRejected()
        {
            DialogQueue queue = new DialogQueue();
            DialogHandle handle = queue.Enqueue(Request("a"));
            Assert.That(queue.Complete(handle, 2), Is.False);
            Assert.That(queue.Complete(handle, -1), Is.False);
            Assert.That(queue.Current, Is.SameAs(handle));
        }

        [Test]
        public void Complete_OfAWaitingDialog_IsRejected()
        {
            DialogQueue queue = new DialogQueue();
            queue.Enqueue(Request("a"));
            DialogHandle waiting = queue.Enqueue(Request("b"));
            Assert.That(queue.Complete(waiting, 1), Is.False);
        }

        [Test]
        public void RequestBack_ClosesADismissibleDialogWithTheCancelIndex()
        {
            DialogQueue queue = new DialogQueue();
            queue.Enqueue(Request("a"));
            Assert.That(queue.RequestBack(), Is.True);
            Assert.That(_results[0].ButtonIndex, Is.EqualTo(0));
            Assert.That(_results[0].Reason, Is.EqualTo(DialogCloseReason.Back));
            Assert.That(queue.IsModalOpen, Is.False);
        }

        [Test]
        public void RequestBack_WithoutACancelButton_ReportsMinusOne()
        {
            DialogQueue queue = new DialogQueue();
            queue.Enqueue(Request("a", cancel: false));
            queue.RequestBack();
            Assert.That(_results[0].ButtonIndex, Is.EqualTo(-1));
        }

        [Test]
        public void RequestBack_OnADialogThatMustBeAnswered_IsSwallowedButKeepsItOpen()
        {
            DialogQueue queue = new DialogQueue();
            DialogRequest request = Request("win");
            request.DismissOnBack = false;
            queue.Enqueue(request);
            Assert.That(queue.RequestBack(), Is.True);
            Assert.That(queue.IsModalOpen, Is.True);
            Assert.That(_results, Is.Empty);
        }

        [Test]
        public void RequestBack_WithNoDialog_IsNotConsumed()
        {
            Assert.That(new DialogQueue().RequestBack(), Is.False);
        }

        [Test]
        public void RequestScrimTap_ClosesOnlyWhenTheRequestAllowsIt()
        {
            DialogQueue queue = new DialogQueue();
            queue.Enqueue(Request("a"));
            Assert.That(queue.RequestScrimTap(), Is.False);
            Assert.That(queue.IsModalOpen, Is.True);
            queue.CloseAll();

            DialogRequest request = Request("b");
            request.DismissOnScrim = true;
            queue.Enqueue(request);
            Assert.That(queue.RequestScrimTap(), Is.True);
            Assert.That(_results[_results.Count - 1].Reason, Is.EqualTo(DialogCloseReason.Scrim));
        }

        [Test]
        public void DedupeKey_IgnoresASecondRequestWhileTheFirstIsQueuedOrShowing()
        {
            DialogQueue queue = new DialogQueue();
            DialogRequest one = Request("restart");
            one.DedupeKey = "D1";
            DialogRequest two = Request("restart-again");
            two.DedupeKey = "D1";
            DialogHandle first = queue.Enqueue(one);
            DialogHandle second = queue.Enqueue(two);
            Assert.That(second, Is.SameAs(first));
            Assert.That(queue.WaitingCount, Is.EqualTo(0));

            queue.Complete(first, 0);
            DialogRequest three = Request("restart-3");
            three.DedupeKey = "D1";
            Assert.That(queue.Enqueue(three), Is.Not.SameAs(first));
        }

        [Test]
        public void Cancel_ClosesAWaitingDialogWithoutShowingIt()
        {
            DialogQueue queue = new DialogQueue();
            queue.Enqueue(Request("a"));
            DialogHandle waiting = queue.Enqueue(Request("b"));
            Assert.That(queue.Cancel(waiting), Is.True);
            Assert.That(queue.WaitingCount, Is.EqualTo(0));
            Assert.That(_results[0].Reason, Is.EqualTo(DialogCloseReason.Programmatic));
            Assert.That(queue.Cancel(waiting), Is.False);
        }

        [Test]
        public void Cancel_OfTheShowingDialog_AdvancesTheQueue()
        {
            DialogQueue queue = new DialogQueue();
            DialogHandle a = queue.Enqueue(Request("a"));
            DialogHandle b = queue.Enqueue(Request("b"));
            Assert.That(queue.Cancel(a), Is.True);
            Assert.That(queue.Current, Is.SameAs(b));
        }

        [Test]
        public void CloseAll_RunsEveryCallbackOnceWithCleared()
        {
            DialogQueue queue = new DialogQueue();
            queue.Enqueue(Request("a"));
            queue.Enqueue(Request("b"));
            queue.Enqueue(Request("c"));
            queue.CloseAll();
            Assert.That(_results.Count, Is.EqualTo(3));
            Assert.That(_results.TrueForAll(r => r.Reason == DialogCloseReason.Cleared), Is.True);
            Assert.That(queue.IsModalOpen, Is.False);
            Assert.That(queue.WaitingCount, Is.EqualTo(0));
        }

        [Test]
        public void DismissOnResume_ClosesOnlyTheDialogsThatAskedForIt()
        {
            DialogQueue queue = new DialogQueue();
            DialogRequest custom = Request("custom");
            custom.DismissOnAppResume = true;
            queue.Enqueue(custom);
            DialogHandle keep = queue.Enqueue(Request("confirm"));
            DialogRequest custom2 = Request("custom2");
            custom2.DismissOnAppResume = true;
            queue.Enqueue(custom2);

            queue.DismissOnResume();
            Assert.That(queue.Current, Is.SameAs(keep));
            Assert.That(_results.Count, Is.EqualTo(2));
            Assert.That(_results.TrueForAll(r => r.Reason == DialogCloseReason.AppResumed), Is.True);
            Assert.That(queue.WaitingCount, Is.EqualTo(0));
        }

        [Test]
        public void ACallbackThatEnqueuesAFollowUp_IsOrderedByPriorityBehindWaitingDialogs()
        {
            DialogQueue queue = new DialogQueue();
            DialogRequest step1 = Request("step1");
            DialogHandle chained = null;
            step1.Callback = result => chained = queue.Enqueue(Request("step2", DialogPriority.High));
            DialogHandle first = queue.Enqueue(step1);
            DialogHandle other = queue.Enqueue(Request("other"));
            queue.Complete(first, 1);
            Assert.That(queue.Current, Is.SameAs(chained), "a High follow-up comes before the waiting Normal dialog");
            queue.Complete(chained, 1);
            Assert.That(queue.Current, Is.SameAs(other));
        }

        [Test]
        public void ACallbackThatEnqueuesANormalFollowUp_WaitsBehindOlderDialogs()
        {
            DialogQueue queue = new DialogQueue();
            DialogRequest step1 = Request("step1");
            DialogHandle chained = null;
            step1.Callback = result => chained = queue.Enqueue(Request("step2"));
            DialogHandle first = queue.Enqueue(step1);
            DialogHandle other = queue.Enqueue(Request("other"));
            queue.Complete(first, 1);
            Assert.That(queue.Current, Is.SameAs(other));
            queue.Complete(other, 1);
            Assert.That(queue.Current, Is.SameAs(chained));
        }

        [Test]
        public void AThrowingCallback_IsLoggedAndDoesNotStopTheQueue()
        {
            DialogQueue queue = new DialogQueue();
            DialogRequest bad = Request("bad");
            bad.Callback = result => throw new InvalidOperationException("boom");
            DialogHandle first = queue.Enqueue(bad);
            DialogHandle second = queue.Enqueue(Request("next"));
            Assert.DoesNotThrow(() => queue.Complete(first, 1));
            Assert.That(queue.Current, Is.SameAs(second));
            Assert.That(_log.Exists(line => line.Contains("boom")), Is.True);
        }

        [Test]
        public void ModalStateChanged_FiresOnlyAtTheBoundaries()
        {
            DialogQueue queue = new DialogQueue();
            List<bool> states = new List<bool>();
            queue.ModalStateChanged += states.Add;
            DialogHandle a = queue.Enqueue(Request("a"));
            DialogHandle b = queue.Enqueue(Request("b"));
            queue.Complete(a, 1);
            queue.Complete(b, 1);
            Assert.That(states, Is.EqualTo(new[] { true, false }));
        }

        [Test]
        public void Closed_FiresAfterTheCallbackAndBeforeTheNextShowing()
        {
            DialogQueue queue = new DialogQueue();
            List<string> order = new List<string>();
            queue.Closed += h => order.Add("closed:" + h.Request.Id);
            queue.Showing += h => order.Add("showing:" + h.Request.Id);
            DialogRequest a = Request("a");
            a.Callback = r => order.Add("callback:a");
            DialogHandle first = queue.Enqueue(a);
            queue.Enqueue(Request("b"));
            queue.Complete(first, 1);
            Assert.That(order, Is.EqualTo(new[] { "showing:a", "callback:a", "closed:a", "showing:b" }));
        }

        [Test]
        public void Enqueue_Null_Throws()
        {
            Assert.Throws<ArgumentNullException>(() => new DialogQueue().Enqueue(null));
        }

        [Test]
        public void ARequestExposesItsCancelIndex()
        {
            Assert.That(Request("a").CancelIndex, Is.EqualTo(0));
            Assert.That(Request("a", cancel: false).CancelIndex, Is.EqualTo(-1));
        }
    }
}
