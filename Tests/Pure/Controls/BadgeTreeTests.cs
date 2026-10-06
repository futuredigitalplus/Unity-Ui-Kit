using System.Collections.Generic;
using NUnit.Framework;

namespace FutureDigitalPlus.UIKit.Controls.Tests
{
    public class BadgeTreeTests
    {
        [Test]
        public void ParentShowsTheSumOfItsChildren()
        {
            var t = new BadgeTree();
            t.SetCount("shop/offers", 2);
            t.SetCount("shop/gems", 1);
            Assert.AreEqual(3, t.Get("shop").Count);
            Assert.AreEqual(2, t.Get("shop/offers").Count);
            Assert.AreEqual(3, t.Get("").Count, "the root is the grand total");
            Assert.AreEqual(0, t.Get("home").Count);
            Assert.IsFalse(t.Get("home").IsVisible);
        }

        [Test]
        public void OwnCountAddsToTheChildren()
        {
            var t = new BadgeTree();
            t.SetCount("shop", 5);
            t.SetCount("shop/offers", 2);
            Assert.AreEqual(7, t.Get("shop").Count);
            Assert.AreEqual(5, t.OwnCount("shop"));
        }

        [Test]
        public void PathsDoNotMatchSiblingsWithTheSamePrefix()
        {
            var t = new BadgeTree();
            t.SetCount("shop2", 9);
            Assert.AreEqual(0, t.Get("shop").Count);
        }

        [Test]
        public void Dots_BubbleUp_AndShowWithoutACount()
        {
            var t = new BadgeTree();
            t.SetDot("settings/privacy", true);
            var parent = t.Get("settings");
            Assert.IsTrue(parent.Dot);
            Assert.AreEqual(0, parent.Count);
            Assert.IsTrue(parent.IsVisible);
        }

        [Test]
        public void Add_AccumulatesAndNeverGoesBelowZero()
        {
            var t = new BadgeTree();
            t.Add("mail");
            t.Add("mail", 4);
            Assert.AreEqual(5, t.OwnCount("mail"));
            t.Add("mail", -100);
            Assert.AreEqual(0, t.OwnCount("mail"));
            t.SetCount("mail", -3);
            Assert.AreEqual(0, t.OwnCount("mail"));
        }

        [Test]
        public void Visit_ClearsThePlaceAndEverythingBelow()
        {
            var t = new BadgeTree();
            t.SetCount("shop/offers", 2);
            t.SetCount("shop/offers/daily", 1);
            t.SetDot("shop/gems", true);
            t.SetCount("home", 4);

            t.Visit("shop");
            Assert.IsFalse(t.Get("shop").IsVisible);
            Assert.IsFalse(t.Get("shop/offers/daily").IsVisible);
            Assert.AreEqual(4, t.Get("home").Count, "other places keep their badge");
        }

        [Test]
        public void Visit_OnALeaf_LeavesTheSiblings()
        {
            var t = new BadgeTree();
            t.SetCount("shop/offers", 2);
            t.SetCount("shop/gems", 1);
            t.Visit("shop/offers");
            Assert.AreEqual(1, t.Get("shop").Count);
        }

        [Test]
        public void Changed_ReportsThePlaceAndItsAncestors_OnlyWhenTheirValueChanged()
        {
            var t = new BadgeTree();
            var changed = new List<string>();
            t.Changed += changed.Add;

            t.SetCount("shop/offers/daily", 2);
            CollectionAssert.AreEquivalent(new[] { "shop/offers/daily", "shop/offers", "shop", "" }, changed);

            changed.Clear();
            t.SetCount("shop/offers/daily", 2);
            Assert.AreEqual(0, changed.Count, "same value, no event");

            t.SetCount("shop/gems", 0);
            Assert.AreEqual(0, changed.Count, "a zero badge that was already zero");
        }

        [Test]
        public void Changed_OnVisit_ReportsIntermediatePlaces()
        {
            var t = new BadgeTree();
            t.SetCount("a/b/c", 3);
            var changed = new List<string>();
            t.Changed += changed.Add;
            t.Visit("a");
            CollectionAssert.Contains(changed, "a/b/c");
            CollectionAssert.Contains(changed, "a/b");
            CollectionAssert.Contains(changed, "a");
        }

        [Test]
        public void Visit_WithNothingToClear_RaisesNothing()
        {
            var t = new BadgeTree();
            var raised = 0;
            t.Changed += _ => raised++;
            t.Visit("anything");
            Assert.AreEqual(0, raised);
        }

        [Test]
        public void Clear_RaisesForEveryAffectedPlace()
        {
            var t = new BadgeTree();
            t.SetCount("shop/offers", 2);
            var changed = new List<string>();
            t.Changed += changed.Add;
            t.Clear();
            CollectionAssert.AreEquivalent(new[] { "shop/offers", "shop", "" }, changed);
            Assert.IsFalse(t.Get("").IsVisible);
        }

        [Test]
        public void Paths_AreTrimmed_AndNullIsTheRoot()
        {
            var t = new BadgeTree();
            t.SetCount("/shop/", 1);
            Assert.AreEqual(1, t.Get("shop").Count);
            Assert.AreEqual(1, t.Get(null).Count);
        }

        [Test]
        public void BadgeValue_Equality()
        {
            Assert.AreEqual(new BadgeValue(2, false), new BadgeValue(2, false));
            Assert.AreNotEqual(new BadgeValue(2, false), new BadgeValue(2, true));
            Assert.IsTrue(new BadgeValue(0, true).IsVisible);
            Assert.IsFalse(default(BadgeValue).IsVisible);
        }
    }
}
