using System.Collections.Generic;
using NUnit.Framework;

namespace FutureDigitalPlus.UIKit.Controls.Tests
{
    public class MenuModelTests
    {
        private static List<MenuEntry> Sample()
        {
            return new List<MenuEntry>
            {
                new MenuEntry("open", "Open"),
                new MenuEntry("rename", "Rename"),
                MenuEntry.Separator(),
                new MenuEntry("delete", "Delete") { Enabled = false },
                new MenuEntry("share", "Share")
                    .Add(new MenuEntry("mail", "Mail"))
                    .Add(new MenuEntry("link", "Copy link"))
                    .Add(new MenuEntry("nearby", "Nearby") { Enabled = false }),
                new MenuEntry("print", "Print"),
            };
        }

        [Test]
        public void New_FocusesTheFirstFocusableItem()
        {
            var m = new MenuModel(new List<MenuEntry> { MenuEntry.Separator(), new MenuEntry("a", "A") });
            Assert.AreEqual(1, m.FocusIndex);
            Assert.AreEqual("a", m.Focused.Id);
        }

        [Test]
        public void New_NoFocusableItem_HasNoFocus()
        {
            var m = new MenuModel(new List<MenuEntry> { MenuEntry.Separator(), new MenuEntry("a", "A") { Enabled = false } });
            Assert.AreEqual(-1, m.FocusIndex);
            Assert.IsNull(m.Focused);
            Assert.IsFalse(m.MoveNext());
            Assert.AreEqual(MenuAction.None, m.Activate().Action);
        }

        [Test]
        public void MoveNext_SkipsSeparatorsAndDisabledItems()
        {
            var m = new MenuModel(Sample());
            Assert.AreEqual("open", m.Focused.Id);
            Assert.IsTrue(m.MoveNext());
            Assert.AreEqual("rename", m.Focused.Id);
            Assert.IsTrue(m.MoveNext());
            Assert.AreEqual("share", m.Focused.Id, "separator and disabled item skipped");
            Assert.IsTrue(m.MoveNext());
            Assert.AreEqual("print", m.Focused.Id);
        }

        [Test]
        public void Move_WrapsByDefault_AndStopsWhenWrapIsOff()
        {
            var m = new MenuModel(Sample());
            Assert.IsTrue(m.MovePrevious());
            Assert.AreEqual("print", m.Focused.Id, "wrapped from the first to the last");
            Assert.IsTrue(m.MoveNext());
            Assert.AreEqual("open", m.Focused.Id);

            m.Wrap = false;
            Assert.IsFalse(m.MovePrevious());
            Assert.AreEqual("open", m.Focused.Id);
            m.MoveLast();
            Assert.IsFalse(m.MoveNext());
            Assert.AreEqual("print", m.Focused.Id);
        }

        [Test]
        public void MoveFirstAndLast()
        {
            var m = new MenuModel(Sample());
            Assert.IsTrue(m.MoveLast());
            Assert.AreEqual("print", m.Focused.Id);
            Assert.IsFalse(m.MoveLast());
            Assert.IsTrue(m.MoveFirst());
            Assert.AreEqual("open", m.Focused.Id);
        }

        [Test]
        public void FocusItem_OnlyFocusableItems()
        {
            var m = new MenuModel(Sample());
            Assert.IsFalse(m.FocusItem(2), "separator");
            Assert.IsFalse(m.FocusItem(3), "disabled");
            Assert.IsFalse(m.FocusItem(99));
            Assert.IsTrue(m.FocusItem(5));
            Assert.AreEqual("print", m.Focused.Id);
            Assert.IsFalse(m.FocusItem(5), "already focused");
        }

        [Test]
        public void Activate_InvokesALeaf()
        {
            var m = new MenuModel(Sample());
            m.MoveNext();
            var step = m.Activate();
            Assert.AreEqual(MenuAction.Invoked, step.Action);
            Assert.AreEqual("rename", step.Item.Id);
            Assert.AreEqual(0, m.Depth);
        }

        [Test]
        public void Activate_OnASubmenuItem_OpensIt_AndFocusesItsFirstFocusableChild()
        {
            var m = new MenuModel(Sample());
            m.FocusItem(4);
            var step = m.Activate();
            Assert.AreEqual(MenuAction.OpenedSubmenu, step.Action);
            Assert.AreEqual("share", step.Item.Id);
            Assert.AreEqual(1, m.Depth);
            Assert.AreEqual("mail", m.Focused.Id);
            Assert.AreEqual(3, m.CurrentItems.Count);
            Assert.AreEqual("share", m.CurrentParent.Id);

            m.MoveNext();
            Assert.AreEqual("link", m.Focused.Id);
            m.MoveNext();
            Assert.AreEqual("mail", m.Focused.Id, "disabled child skipped, wrapped inside the submenu");
        }

        [Test]
        public void Back_ClosesTheSubmenu_AndKeepsTheParentFocus()
        {
            var m = new MenuModel(Sample());
            m.FocusItem(4);
            m.OpenSubmenu();
            var step = m.Back();
            Assert.AreEqual(MenuAction.ClosedSubmenu, step.Action);
            Assert.AreEqual("share", step.Item.Id);
            Assert.AreEqual(0, m.Depth);
            Assert.AreEqual("share", m.Focused.Id);
            Assert.AreEqual(MenuAction.None, m.Back().Action, "nothing to close at the root");
        }

        [Test]
        public void Cancel_ClosesSubmenusFirst_ThenTheMenu()
        {
            var m = new MenuModel(Sample());
            m.FocusItem(4);
            m.OpenSubmenu();
            Assert.AreEqual(MenuAction.ClosedSubmenu, m.Cancel().Action);
            Assert.AreEqual(MenuAction.Closed, m.Cancel().Action);
        }

        [Test]
        public void OpenSubmenu_OnALeaf_DoesNothing()
        {
            var m = new MenuModel(Sample());
            Assert.AreEqual(MenuAction.None, m.OpenSubmenu().Action);
            Assert.AreEqual(0, m.Depth);
        }

        [Test]
        public void ItemsAtAndFocusIndexAt_DescribeEveryOpenLevel()
        {
            var m = new MenuModel(Sample());
            m.FocusItem(4);
            m.OpenSubmenu();
            Assert.AreEqual(6, m.ItemsAt(0).Count);
            Assert.AreEqual(4, m.FocusIndexAt(0));
            Assert.AreEqual(3, m.ItemsAt(1).Count);
            Assert.AreEqual(0, m.FocusIndexAt(1));
        }

        [Test]
        public void TypeAhead_FocusesTheNextMatchingItem()
        {
            var m = new MenuModel(Sample());
            Assert.IsTrue(m.TypeAhead('s', 0d));
            Assert.AreEqual("share", m.Focused.Id);
            Assert.IsTrue(m.TypeAhead('p', 5d), "new word after the timeout");
            Assert.AreEqual("print", m.Focused.Id);
        }

        [Test]
        public void TypeAhead_MultipleLettersNarrowTheSearch()
        {
            var m = new MenuModel(new List<MenuEntry>
            {
                new MenuEntry("a", "Resume"), new MenuEntry("b", "Restart"), new MenuEntry("c", "Resolution"),
            });
            m.TypeAhead('r', 0d);
            m.TypeAhead('e', 0.2d);
            m.TypeAhead('s', 0.4d);
            m.TypeAhead('t', 0.6d);
            Assert.AreEqual("b", m.Focused.Id);
        }

        [Test]
        public void TypeAhead_RepeatedLetter_CyclesThroughMatches()
        {
            var m = new MenuModel(new List<MenuEntry>
            {
                new MenuEntry("a", "Apple"), new MenuEntry("b", "Avocado"), new MenuEntry("c", "Banana"),
            });
            m.TypeAhead('a', 0d);
            Assert.AreEqual("b", m.Focused.Id, "starts after the focused item");
            m.TypeAhead('a', 0.2d);
            Assert.AreEqual("a", m.Focused.Id);
        }

        [Test]
        public void TypeAhead_IgnoresCaseAndWhiteSpace_AndSkipsDisabledItems()
        {
            var m = new MenuModel(Sample());
            Assert.IsFalse(m.TypeAhead(' ', 0d));
            Assert.IsFalse(m.TypeAhead('\n', 0d));
            Assert.IsFalse(m.TypeAhead('d', 10d), "Delete is disabled");
            Assert.IsTrue(m.TypeAhead('P', 20d));
            Assert.AreEqual("print", m.Focused.Id);
        }

        [Test]
        public void TypeAhead_NoMatch_ReturnsFalse()
        {
            var m = new MenuModel(Sample());
            Assert.IsFalse(m.TypeAhead('z', 0d));
            Assert.AreEqual("open", m.Focused.Id);
        }

        [Test]
        public void Separator_CannotHaveChildren_AndNullItemsAreRejected()
        {
            Assert.Throws<System.InvalidOperationException>(() => MenuEntry.Separator().Add(new MenuEntry("x", "x")));
            Assert.Throws<System.ArgumentNullException>(() => new MenuEntry("x", "x").Add(null));
            Assert.Throws<System.ArgumentNullException>(() => new MenuModel(null));
        }
    }
}
