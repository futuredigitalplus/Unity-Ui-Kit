using System.Collections.Generic;
using NUnit.Framework;

namespace FutureDigitalPlus.UIKit.Foundation.Tests
{
    [TestFixture]
    public sealed class ToastQueueTests
    {
        static ToastItem Toast(string text, ToastKind kind = ToastKind.Info)
        {
            return new ToastItem(UiText.FromLiteral(text), kind);
        }

        [Test]
        public void Show_OnAnEmptyQueue_PresentsAtOnce()
        {
            ToastQueue queue = new ToastQueue();
            List<ToastItem> shown = new List<ToastItem>();
            queue.Showing += shown.Add;
            Assert.That(queue.Show(Toast("hello")), Is.True);
            Assert.That(shown.Count, Is.EqualTo(1));
            Assert.That(queue.Current, Is.SameAs(shown[0]));
        }

        [Test]
        public void OneToastIsVisibleAtATime_TheNextAppearsWhenTheFirstIsHidden()
        {
            ToastQueue queue = new ToastQueue();
            List<string> shown = new List<string>();
            queue.Showing += item => shown.Add(item.Text.Literal);
            queue.Show(Toast("one"));
            queue.Show(Toast("two"));
            Assert.That(shown, Is.EqualTo(new[] { "one" }));
            Assert.That(queue.WaitingCount, Is.EqualTo(1));
            queue.Hidden();
            Assert.That(shown, Is.EqualTo(new[] { "one", "two" }));
            queue.Hidden();
            Assert.That(queue.Current, Is.Null);
        }

        [Test]
        public void IdenticalToastAsTheVisibleOne_RefreshesInsteadOfStacking()
        {
            ToastQueue queue = new ToastQueue();
            int refreshed = 0;
            queue.Refreshed += item => refreshed++;
            queue.Show(Toast("saved"));
            Assert.That(queue.Show(Toast("saved")), Is.False);
            Assert.That(refreshed, Is.EqualTo(1));
            Assert.That(queue.WaitingCount, Is.EqualTo(0));
        }

        [Test]
        public void IdenticalToastAsTheLastWaiting_IsDropped()
        {
            ToastQueue queue = new ToastQueue();
            queue.Show(Toast("a"));
            queue.Show(Toast("b"));
            Assert.That(queue.Show(Toast("b")), Is.False);
            Assert.That(queue.WaitingCount, Is.EqualTo(1));
        }

        [Test]
        public void ADifferentKindIsNotAnIdenticalToast()
        {
            ToastQueue queue = new ToastQueue();
            queue.Show(Toast("x", ToastKind.Info));
            Assert.That(queue.Show(Toast("x", ToastKind.Danger)), Is.True);
            Assert.That(queue.WaitingCount, Is.EqualTo(1));
        }

        [Test]
        public void WhenTheWaitingListIsFull_TheOldestWaitingToastIsDropped()
        {
            ToastQueue queue = new ToastQueue();
            List<string> shown = new List<string>();
            queue.Showing += item => shown.Add(item.Text.Literal);
            queue.Show(Toast("visible"));
            queue.Show(Toast("w1"));
            queue.Show(Toast("w2"));
            queue.Show(Toast("w3"));
            queue.Show(Toast("w4"));
            Assert.That(queue.WaitingCount, Is.EqualTo(ToastQueue.MaxWaiting));
            queue.Hidden();
            queue.Hidden();
            queue.Hidden();
            queue.Hidden();
            Assert.That(shown, Is.EqualTo(new[] { "visible", "w2", "w3", "w4" }));
        }

        [Test]
        public void EmptyOrNullToasts_AreIgnored()
        {
            ToastQueue queue = new ToastQueue();
            Assert.That(queue.Show(null), Is.False);
            Assert.That(queue.Show(new ToastItem(UiText.Empty)), Is.False);
            Assert.That(queue.Current, Is.Null);
        }

        [Test]
        public void Clear_DropsEverythingSilently()
        {
            ToastQueue queue = new ToastQueue();
            queue.Show(Toast("a"));
            queue.Show(Toast("b"));
            queue.Clear();
            Assert.That(queue.Current, Is.Null);
            Assert.That(queue.WaitingCount, Is.EqualTo(0));
        }

        [Test]
        public void ADurationBelowTheMinimumIsRaised()
        {
            Assert.That(new ToastItem(UiText.FromLiteral("x"), ToastKind.Info, 0f).Seconds, Is.GreaterThan(0f));
            Assert.That(new ToastItem(UiText.FromLiteral("x")).Seconds, Is.EqualTo(ToastItem.DefaultSeconds));
        }
    }
}
