using System.Collections.Generic;
using NUnit.Framework;

namespace FutureDigitalPlus.UIKit.Hud.Tests
{
    public class TimedFeedModelTests
    {
        [Test]
        public void Add_ShowsOldestFirstByDefault()
        {
            var f = new TimedFeedModel();
            f.Add("a");
            f.Add("b");
            Assert.AreEqual(2, f.Count);
            Assert.AreEqual("a", f[0].Text);
            Assert.AreEqual("b", f[1].Text);
        }

        [Test]
        public void NewestOnTop_ReversesDisplayOrder()
        {
            var f = new TimedFeedModel { NewestOnTop = true };
            var a = f.Add("a");
            var b = f.Add("b");
            Assert.AreEqual("b", f[0].Text);
            Assert.AreEqual(0, f.DisplayIndexOf(b));
            Assert.AreEqual(1, f.DisplayIndexOf(a));
            Assert.AreEqual(-1, f.DisplayIndexOf(new TimedFeedModel().Add("x")));
        }

        [Test]
        public void Max_EvictsTheOldest()
        {
            var f = new TimedFeedModel { MaxEntries = 2 };
            var removed = new List<(string, FeedRemoveReason)>();
            f.EntryRemoved += (e, r) => removed.Add((e.Text, r));
            f.Add("a");
            f.Add("b");
            f.Add("c");
            Assert.AreEqual(2, f.Count);
            Assert.AreEqual("b", f[0].Text);
            Assert.AreEqual(("a", FeedRemoveReason.Evicted), removed[0]);
        }

        [Test]
        public void Merge_SameKey_IncrementsAndRefreshesAge()
        {
            var f = new TimedFeedModel { TimeToLive = 4f };
            var updated = 0;
            f.EntryUpdated += _ => updated++;
            var first = f.Add("+10 gold", "gold");
            f.Tick(3f);
            var second = f.Add("+10 gold", "gold");
            Assert.AreSame(first, second);
            Assert.AreEqual(2, first.Count);
            Assert.AreEqual(0f, first.Age);
            Assert.AreEqual(1, f.Count);
            Assert.AreEqual(1, updated);
        }

        [Test]
        public void Merge_Disabled_Or_NullKey_AddsSeparateEntries()
        {
            var f = new TimedFeedModel();
            f.Add("x");
            f.Add("x");
            Assert.AreEqual(2, f.Count);

            var g = new TimedFeedModel { MergeIdentical = false };
            g.Add("x", "k");
            g.Add("x", "k");
            Assert.AreEqual(2, g.Count);
        }

        [Test]
        public void Tick_FadesThenExpires()
        {
            var f = new TimedFeedModel { TimeToLive = 2f, FadeSeconds = 0.5f };
            var e = f.Add("a");
            var removed = 0;
            var updates = 0;
            f.EntryRemoved += (x, r) => { removed++; Assert.AreEqual(FeedRemoveReason.Expired, r); };
            f.EntryUpdated += _ => updates++;

            f.Tick(1.4f);
            Assert.IsFalse(e.IsFading);
            f.Tick(0.2f);
            Assert.IsTrue(e.IsFading);
            Assert.AreEqual(1, updates);
            f.Tick(0.1f);
            Assert.AreEqual(1, updates, "fade start is reported once");
            f.Tick(0.5f);
            Assert.AreEqual(0, f.Count);
            Assert.AreEqual(1, removed);
        }

        [Test]
        public void Merge_ClearsTheFade()
        {
            var f = new TimedFeedModel { TimeToLive = 2f, FadeSeconds = 0.5f };
            var e = f.Add("a", "k");
            f.Tick(1.7f);
            Assert.IsTrue(e.IsFading);
            f.Add("a", "k");
            Assert.IsFalse(e.IsFading);
        }

        [Test]
        public void Clear_RemovesAll_WithReason()
        {
            var f = new TimedFeedModel();
            var reasons = new List<FeedRemoveReason>();
            f.EntryRemoved += (e, r) => reasons.Add(r);
            f.Add("a");
            f.Add("b");
            f.Clear();
            Assert.AreEqual(0, f.Count);
            CollectionAssert.AreEqual(new[] { FeedRemoveReason.Cleared, FeedRemoveReason.Cleared }, reasons);
        }

        [Test]
        public void Ids_AreUnique()
        {
            var f = new TimedFeedModel();
            Assert.AreNotEqual(f.Add("a").Id, f.Add("b").Id);
        }
    }
}
