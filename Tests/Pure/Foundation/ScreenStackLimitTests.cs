using System.Collections.Generic;
using NUnit.Framework;

namespace FutureDigitalPlus.UIKit.Foundation.Tests
{
    /// <summary>Edge cases of the depth limit and of the saved text that the first test pass did not cover.</summary>
    [TestFixture]
    public sealed class ScreenStackLimitTests
    {
        [Test]
        public void Push_ThatTrimsTheScreenThatWasOnTop_DoesNotKeepItAlive()
        {
            // MaxDepth 2: [A, B] + C trims the entry above the root, which is B, the screen that was on top.
            ScreenStack stack = new ScreenStack(2);
            stack.Push("A");
            stack.Push("B");
            ScreenChange change = stack.Push("C");
            Assert.That(change.From.Id, Is.EqualTo("B"));
            Assert.That(change.Disposed.Count, Is.EqualTo(1));
            Assert.That(change.Disposed[0], Is.SameAs(change.From));
            Assert.That(change.FromStaysAlive, Is.False, "a screen that is disposed cannot also stay alive underneath");
            Assert.That(stack.Depth, Is.EqualTo(2));
        }

        [Test]
        public void Push_WithinTheLimit_KeepsTheScreenThatWasOnTopAlive()
        {
            ScreenStack stack = new ScreenStack(3);
            stack.Push("A");
            stack.Push("B");
            Assert.That(stack.Push("C").FromStaysAlive, Is.True);
            ScreenChange trimming = stack.Push("D");
            Assert.That(trimming.Disposed[0].Id, Is.EqualTo("B"));
            Assert.That(trimming.FromStaysAlive, Is.True, "C was not trimmed");
        }

        [Test]
        public void TheMaxDepthIsNeverBelowTwo()
        {
            Assert.That(new ScreenStack(0).MaxDepth, Is.EqualTo(2));
            Assert.That(new ScreenStack(-5).MaxDepth, Is.EqualTo(2));
        }

        [TestCase("a\\nb")]
        [TestCase("\\")]
        [TestCase("ends with backslash\\")]
        [TestCase("\\t\\r\\n")]
        [TestCase("line1\r\nline2")]
        [TestCase("tab\there")]
        [TestCase("")]
        public void Capture_ThenParse_KeepsTrickyStatesExactly(string state)
        {
            ScreenStack stack = new ScreenStack();
            stack.Push("Home");
            stack[0].State = state;
            stack.Push("Stats");
            stack[1].State = "second";

            List<ScreenEntry> entries;
            Assert.That(ScreenStack.TryParse(stack.Capture(), null, out entries), Is.True);
            Assert.That(entries.Count, Is.EqualTo(2));
            Assert.That(entries[0].State ?? string.Empty, Is.EqualTo(state));
            Assert.That(entries[1].State, Is.EqualTo("second"));
        }

        [Test]
        public void TryParse_OfAnEmptyStack_Fails()
        {
            List<ScreenEntry> entries;
            Assert.That(ScreenStack.TryParse(new ScreenStack().Capture(), null, out entries), Is.False);
            Assert.That(entries, Is.Empty);
        }

        [Test]
        public void TryParse_WithOnlyUnknownScreens_FailsAndLeavesNothingBehind()
        {
            ScreenStack stack = new ScreenStack();
            stack.Push("Gone");
            stack.Push("AlsoGone");
            List<ScreenEntry> entries;
            Assert.That(ScreenStack.TryParse(stack.Capture(), id => false, out entries), Is.False);
            Assert.That(entries, Is.Empty);
        }

        [Test]
        public void Replace_OfTheTopWithTheSameId_StillMakesANewEntry()
        {
            ScreenStack stack = new ScreenStack();
            stack.Push("Home");
            stack.Push("Game");
            ScreenEntry before = stack.Current;
            ScreenChange change = stack.Replace("Game");
            Assert.That(change.Kind, Is.EqualTo(ScreenChangeKind.Replace));
            Assert.That(stack.Current, Is.Not.SameAs(before), "a replace restarts the screen (a new game of the same kind)");
            Assert.That(change.Disposed[0], Is.SameAs(before));
        }

        [Test]
        public void PopTo_ReturnsAllEntriesAboveInTheDisposedList()
        {
            ScreenStack stack = new ScreenStack();
            stack.Push("Home");
            stack.Push("A");
            stack.Push("B");
            stack.Push("C");
            ScreenChange change = stack.PopTo("A");
            Assert.That(change.Kind, Is.EqualTo(ScreenChangeKind.Pop));
            Assert.That(change.ToIsExisting, Is.True);
            Assert.That(change.FromStaysAlive, Is.False);
            Assert.That(change.Disposed.Count, Is.EqualTo(2));
            Assert.That(change.Disposed[0].Id, Is.EqualTo("B"));
            Assert.That(change.Disposed[1].Id, Is.EqualTo("C"));
            Assert.That(change.From.Id, Is.EqualTo("C"));
        }
    }
}
