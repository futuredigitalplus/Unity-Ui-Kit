using System.Collections.Generic;
using NUnit.Framework;

namespace FutureDigitalPlus.UIKit.Foundation.Tests
{
    [TestFixture]
    public sealed class ScreenStackTests
    {
        [Test]
        public void Push_OnEmptyStack_MakesTheRootCurrent()
        {
            ScreenStack stack = new ScreenStack();
            ScreenChange change = stack.Push("Home");
            Assert.That(change.Kind, Is.EqualTo(ScreenChangeKind.Push));
            Assert.That(change.From, Is.Null);
            Assert.That(change.To.Id, Is.EqualTo("Home"));
            Assert.That(change.FromStaysAlive, Is.False);
            Assert.That(stack.Depth, Is.EqualTo(1));
            Assert.That(stack.CanPop, Is.False);
        }

        [Test]
        public void Push_KeepsTheCurrentScreenAliveUnderneath()
        {
            ScreenStack stack = new ScreenStack();
            stack.Push("Home");
            ScreenChange change = stack.Push("Settings", 42);
            Assert.That(change.From.Id, Is.EqualTo("Home"));
            Assert.That(change.To.Id, Is.EqualTo("Settings"));
            Assert.That(change.To.Args, Is.EqualTo(42));
            Assert.That(change.FromStaysAlive, Is.True);
            Assert.That(change.Disposed, Is.Empty);
            Assert.That(stack.CanPop, Is.True);
        }

        [Test]
        public void Push_OfTheScreenOnTop_IsIgnored()
        {
            ScreenStack stack = new ScreenStack();
            stack.Push("Home");
            stack.Push("Settings");
            ScreenChange again = stack.Push("Settings");
            Assert.That(again.IsNone, Is.True);
            Assert.That(stack.Depth, Is.EqualTo(2));
        }

        [Test]
        public void Replace_SwapsTheTopAndDisposesIt()
        {
            ScreenStack stack = new ScreenStack();
            stack.Push("Home");
            stack.Push("Choose");
            ScreenChange change = stack.Replace("Table");
            Assert.That(change.Kind, Is.EqualTo(ScreenChangeKind.Replace));
            Assert.That(change.From.Id, Is.EqualTo("Choose"));
            Assert.That(change.Disposed.Count, Is.EqualTo(1));
            Assert.That(change.Disposed[0].Id, Is.EqualTo("Choose"));
            Assert.That(change.FromStaysAlive, Is.False);
            Assert.That(stack.Depth, Is.EqualTo(2));
            Assert.That(stack[0].Id, Is.EqualTo("Home"));
            Assert.That(stack.CurrentId, Is.EqualTo("Table"));
        }

        [Test]
        public void Replace_OnEmptyStack_IsAPush()
        {
            ScreenStack stack = new ScreenStack();
            ScreenChange change = stack.Replace("Home");
            Assert.That(change.Kind, Is.EqualTo(ScreenChangeKind.Push));
            Assert.That(stack.CurrentId, Is.EqualTo("Home"));
        }

        [Test]
        public void Pop_ReturnsToTheScreenBelowAndMarksItExisting()
        {
            ScreenStack stack = new ScreenStack();
            stack.Push("Home");
            ScreenEntry home = stack.Current;
            stack.Push("Settings");
            ScreenChange change = stack.Pop();
            Assert.That(change.Kind, Is.EqualTo(ScreenChangeKind.Pop));
            Assert.That(change.From.Id, Is.EqualTo("Settings"));
            Assert.That(change.To, Is.SameAs(home));
            Assert.That(change.ToIsExisting, Is.True);
            Assert.That(change.Disposed.Count, Is.EqualTo(1));
        }

        [Test]
        public void Pop_AtTheRoot_DoesNothing()
        {
            ScreenStack stack = new ScreenStack();
            stack.Push("Home");
            Assert.That(stack.Pop().IsNone, Is.True);
            Assert.That(stack.CurrentId, Is.EqualTo("Home"));
            Assert.That(new ScreenStack().Pop().IsNone, Is.True);
        }

        [Test]
        public void PopTo_RemovesEverythingAboveTheTarget()
        {
            ScreenStack stack = new ScreenStack();
            stack.Push("Home");
            stack.Push("Stats");
            stack.Push("Levels");
            stack.Push("Table");
            ScreenChange change = stack.PopTo("Home");
            Assert.That(stack.Depth, Is.EqualTo(1));
            Assert.That(change.Disposed.Count, Is.EqualTo(3));
            Assert.That(change.From.Id, Is.EqualTo("Table"));
            Assert.That(change.To.Id, Is.EqualTo("Home"));
        }

        [Test]
        public void PopTo_UnknownOrCurrentId_DoesNothing()
        {
            ScreenStack stack = new ScreenStack();
            stack.Push("Home");
            stack.Push("Stats");
            Assert.That(stack.PopTo("Nowhere").IsNone, Is.True);
            Assert.That(stack.PopTo("Stats").IsNone, Is.True);
            Assert.That(stack.Depth, Is.EqualTo(2));
        }

        [Test]
        public void ResetTo_ClearsTheStackAndDisposesEverything()
        {
            ScreenStack stack = new ScreenStack();
            stack.Push("Home");
            stack.Push("Settings");
            stack.Push("Language");
            ScreenChange change = stack.ResetTo("Home");
            Assert.That(change.Kind, Is.EqualTo(ScreenChangeKind.Reset));
            Assert.That(change.Disposed.Count, Is.EqualTo(3));
            Assert.That(stack.Depth, Is.EqualTo(1));
            Assert.That(stack.CurrentId, Is.EqualTo("Home"));
            Assert.That(change.To, Is.Not.SameAs(change.Disposed[0]));
        }

        [Test]
        public void Push_BeyondMaxDepth_DisposesTheEntryAboveTheRoot()
        {
            ScreenStack stack = new ScreenStack(3);
            stack.Push("A");
            stack.Push("B");
            stack.Push("C");
            ScreenChange change = stack.Push("D");
            Assert.That(stack.Depth, Is.EqualTo(3));
            Assert.That(stack[0].Id, Is.EqualTo("A"));
            Assert.That(stack[1].Id, Is.EqualTo("C"));
            Assert.That(stack.CurrentId, Is.EqualTo("D"));
            Assert.That(change.Disposed.Count, Is.EqualTo(1));
            Assert.That(change.Disposed[0].Id, Is.EqualTo("B"));
        }

        [TestCase(null)]
        [TestCase("")]
        [TestCase("a\tb")]
        [TestCase("a\nb")]
        public void Push_WithAnInvalidId_Throws(string id)
        {
            Assert.Throws<System.ArgumentException>(() => new ScreenStack().Push(id));
        }

        [Test]
        public void Capture_ThenParse_RoundTripsIdsAndStates()
        {
            ScreenStack stack = new ScreenStack();
            stack.Push("Home");
            stack.Push("Stats");
            stack[0].State = null;
            stack[1].State = "tab=2\nsearch=a\tb\\c";
            string captured = stack.Capture();

            List<ScreenEntry> entries;
            Assert.That(ScreenStack.TryParse(captured, null, out entries), Is.True);
            Assert.That(entries.Count, Is.EqualTo(2));
            Assert.That(entries[0].Id, Is.EqualTo("Home"));
            Assert.That(entries[0].State, Is.Empty.Or.Null);
            Assert.That(entries[1].Id, Is.EqualTo("Stats"));
            Assert.That(entries[1].State, Is.EqualTo("tab=2\nsearch=a\tb\\c"));
            Assert.That(entries[1].Args, Is.Null);
        }

        [Test]
        public void TryParse_DropsUnknownScreensButKeepsTheRest()
        {
            string captured = "fdp-nav-v1\nHome\t\nGone\t\nStats\tx";
            List<ScreenEntry> entries;
            Assert.That(ScreenStack.TryParse(captured, id => id != "Gone", out entries), Is.True);
            Assert.That(entries.Count, Is.EqualTo(2));
            Assert.That(entries[1].Id, Is.EqualTo("Stats"));
        }

        [Test]
        public void TryParse_FailsWhenTheRootIsUnknown()
        {
            List<ScreenEntry> entries;
            Assert.That(ScreenStack.TryParse("fdp-nav-v1\nGone\t\nStats\t", id => id == "Stats", out entries), Is.False);
            Assert.That(entries, Is.Empty);
        }

        [TestCase(null)]
        [TestCase("")]
        [TestCase("garbage")]
        [TestCase("fdp-nav-v1")]
        [TestCase("fdp-nav-v2\nHome\t")]
        public void TryParse_RejectsAnythingElse(string text)
        {
            List<ScreenEntry> entries;
            Assert.That(ScreenStack.TryParse(text, null, out entries), Is.False);
        }

        [Test]
        public void Load_ReplacesTheContent()
        {
            ScreenStack stack = new ScreenStack();
            stack.Push("Old");
            List<ScreenEntry> entries = new List<ScreenEntry> { new ScreenEntry("Home", null, null), new ScreenEntry("Stats", null, "s") };
            stack.Load(entries);
            Assert.That(stack.Depth, Is.EqualTo(2));
            Assert.That(stack.CurrentId, Is.EqualTo("Stats"));
            Assert.That(stack.IndexOf("Home"), Is.EqualTo(0));
            Assert.That(stack.Contains("Old"), Is.False);
        }
    }
}
