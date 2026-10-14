using System;
using NUnit.Framework;

namespace FutureDigitalPlus.UIKit.Foundation.Tests
{
    /// <summary>The rule that decides the reading direction of an element from its own setting, its classes and its ancestors.</summary>
    [TestFixture]
    public sealed class ReadingDirectionTests
    {
        // A stand-in for a visual element: its own languageDirection (null = Inherit), whether it carries fdp-rtl, and its parent.
        private sealed class Node
        {
            public Node Parent;
            public bool? Own;
            public bool RtlClass;
        }

        private static bool Rtl(Node node) => ReadingDirection.IsRightToLeft(node, n => n.Parent, n => n.Own, n => n.RtlClass);

        private static Node Chain(out Node root, int depth = 3)
        {
            root = new Node();
            var node = root;
            for (var i = 0; i < depth; i++) node = new Node { Parent = node };
            return node;
        }

        [Test]
        public void TheClassIsTheOneLocaleClassApplierWrites()
        {
            Assert.AreEqual("fdp-rtl", ReadingDirection.RtlClass);
        }

        [Test]
        public void OfElement_OwnDirectionFirst_ThenTheClass_ThenTheParent()
        {
            Assert.AreEqual(true, ReadingDirection.OfElement(true, false));
            Assert.AreEqual(false, ReadingDirection.OfElement(false, false));
            Assert.AreEqual(false, ReadingDirection.OfElement(false, true), "an element that sets left to right wins over its own fdp-rtl");
            Assert.AreEqual(true, ReadingDirection.OfElement(null, true));
            Assert.IsNull(ReadingDirection.OfElement(null, false), "inherits: the parent decides");
        }

        [Test]
        public void NothingOnTheWay_IsLeftToRight()
        {
            var leaf = Chain(out _);
            Assert.IsFalse(Rtl(leaf));
            Assert.IsFalse(ReadingDirection.IsRightToLeft<Node>(null, n => n.Parent, n => n.Own, n => n.RtlClass), "null reads left to right");
        }

        [Test]
        public void ADirectionSetOnTheRoot_IsInheritedByADeepElement()
        {
            // The case UI Toolkit's public languageDirection getter misses: it returns Inherit on the deep element.
            var leaf = Chain(out var root, 6);
            root.Own = true;
            Assert.IsTrue(Rtl(leaf));
            root.Own = false;
            Assert.IsFalse(Rtl(leaf));
        }

        [Test]
        public void TheRtlClassOnTheRoot_MakesItRightToLeft_WithoutADirection()
        {
            var leaf = Chain(out var root);
            root.RtlClass = true;
            Assert.IsTrue(Rtl(leaf));
            Assert.IsTrue(Rtl(root), "the element itself counts");
        }

        [Test]
        public void TheNearestSetting_Wins()
        {
            var leaf = Chain(out var root, 4);
            root.RtlClass = true;
            root.Own = true;
            leaf.Parent.Own = false; // a left-to-right island (a number pad) inside an Arabic screen
            Assert.IsFalse(Rtl(leaf));
            leaf.Own = true;
            Assert.IsTrue(Rtl(leaf), "and an element inside the island can set right to left again");
        }

        [Test]
        public void TheRtlClassBelowALeftToRightRoot_MarksItsSubtree()
        {
            var leaf = Chain(out var root, 3);
            root.Own = false;
            leaf.Parent.RtlClass = true;
            Assert.IsTrue(Rtl(leaf));
        }

        [Test]
        public void MissingDelegates_Throw()
        {
            var node = new Node();
            Assert.Throws<ArgumentNullException>(() => ReadingDirection.IsRightToLeft(node, null, n => n.Own, n => n.RtlClass));
            Assert.Throws<ArgumentNullException>(() => ReadingDirection.IsRightToLeft(node, n => n.Parent, null, n => n.RtlClass));
            Assert.Throws<ArgumentNullException>(() => ReadingDirection.IsRightToLeft(node, n => n.Parent, n => n.Own, null));
        }
    }
}
