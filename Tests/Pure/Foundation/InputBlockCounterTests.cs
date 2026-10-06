using System.Collections.Generic;
using NUnit.Framework;

namespace FutureDigitalPlus.UIKit.Foundation.Tests
{
    [TestFixture]
    public sealed class InputBlockCounterTests
    {
        [Test]
        public void IsBlockedWhileAnyReasonIsActive()
        {
            InputBlockCounter counter = new InputBlockCounter();
            Assert.That(counter.IsBlocked, Is.False);
            int dialog = counter.Acquire("dialog");
            int loading = counter.Acquire("loading");
            Assert.That(counter.IsBlocked, Is.True);
            Assert.That(counter.Count, Is.EqualTo(2));
            counter.Release(dialog);
            Assert.That(counter.IsBlocked, Is.True);
            counter.Release(loading);
            Assert.That(counter.IsBlocked, Is.False);
        }

        [Test]
        public void Changed_FiresOnlyWhenTheStateFlips()
        {
            InputBlockCounter counter = new InputBlockCounter();
            List<bool> states = new List<bool>();
            counter.Changed += states.Add;
            int a = counter.Acquire("a");
            int b = counter.Acquire("b");
            counter.Release(a);
            counter.Release(b);
            Assert.That(states, Is.EqualTo(new[] { true, false }));
        }

        [Test]
        public void Release_Twice_IsHarmless()
        {
            InputBlockCounter counter = new InputBlockCounter();
            int token = counter.Acquire("x");
            Assert.That(counter.Release(token), Is.True);
            Assert.That(counter.Release(token), Is.False);
        }

        [Test]
        public void Reasons_ListTheActiveNames()
        {
            InputBlockCounter counter = new InputBlockCounter();
            counter.Acquire("dialog");
            counter.Acquire("screen transition");
            Assert.That(counter.Reasons, Is.EquivalentTo(new[] { "dialog", "screen transition" }));
        }

        [Test]
        public void Clear_ReleasesEverything()
        {
            InputBlockCounter counter = new InputBlockCounter();
            counter.Acquire("a");
            counter.Acquire("b");
            counter.Clear();
            Assert.That(counter.IsBlocked, Is.False);
        }
    }
}
