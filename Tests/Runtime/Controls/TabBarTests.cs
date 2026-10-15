using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UIElements;

namespace FutureDigitalPlus.UIKit.Controls.Tests
{
    public class TabBarTests
    {
        private ControlsPanel _panel;

        [SetUp]
        public void SetUp() => LogAssert.ignoreFailingMessages = true;

        [TearDown]
        public void TearDown()
        {
            LogAssert.ignoreFailingMessages = false;
            _panel?.Dispose();
        }

        private TabBar AddBar(TabBarSkin skin = TabBarSkin.Tabs)
        {
            _panel = ControlsPanel.Create();
            var bar = new TabBar { Skin = skin };
            bar.AddTab("home", "Home");
            bar.AddTab("shop", "Shop");
            bar.AddTab("bag", "Bag");
            _panel.Root.Add(bar);
            return bar;
        }

        private static Button ButtonOf(TabBar bar, string id) => bar.Q<Button>("tab-" + id);

        [UnityTest]
        public IEnumerator FirstItemIsSelected_AndClickSelectsAnother()
        {
            var bar = AddBar();
            yield return ControlsPanel.Frames();
            Assert.AreEqual("home", bar.value);

            var changes = 0;
            string last = null;
            bar.RegisterValueChangedCallback(evt =>
            {
                changes++;
                last = evt.newValue;
            });
            Send.Tap(ButtonOf(bar, "shop"));
            Assert.AreEqual("shop", bar.value);
            Assert.AreEqual(1, changes);
            Assert.AreEqual("shop", last);
            Assert.IsTrue(ButtonOf(bar, "shop").ClassListContains("is-active"));
            Assert.IsFalse(ButtonOf(bar, "home").ClassListContains("is-active"));
        }

        [UnityTest]
        public IEnumerator SubmitSelects_AndFocusFollowsTheSelection()
        {
            var bar = AddBar();
            yield return ControlsPanel.Frames();
            Send.Submit(ButtonOf(bar, "bag"));
            Assert.AreEqual("bag", bar.value);
            Assert.AreSame(ButtonOf(bar, "bag"), bar.panel.focusController.focusedElement);
        }

        [UnityTest]
        public IEnumerator ArrowsMoveTheSelection_SkipDisabledItems_AndStopAtTheEnds()
        {
            var bar = AddBar();
            yield return ControlsPanel.Frames();
            bar.SetTabEnabled("shop", false);
            Send.Move(ButtonOf(bar, "home"), NavigationMoveEvent.Direction.Right);
            Assert.AreEqual("bag", bar.value, "the disabled item is skipped");
            Send.Move(ButtonOf(bar, "bag"), NavigationMoveEvent.Direction.Right);
            Assert.AreEqual("bag", bar.value, "no wrap at the end");
            Send.Move(ButtonOf(bar, "bag"), NavigationMoveEvent.Direction.Left);
            Assert.AreEqual("home", bar.value);
        }

        [UnityTest]
        public IEnumerator HomeAndEndJumpToTheEnds()
        {
            var bar = AddBar();
            yield return ControlsPanel.Frames();
            Send.Key(ButtonOf(bar, "home"), KeyCode.End);
            Assert.AreEqual("bag", bar.value);
            Send.Key(ButtonOf(bar, "bag"), KeyCode.Home);
            Assert.AreEqual("home", bar.value);
        }

        [UnityTest]
        public IEnumerator DisabledItem_CannotBeSelected()
        {
            var bar = AddBar();
            yield return ControlsPanel.Frames();
            bar.SetTabEnabled("shop", false);
            Send.Tap(ButtonOf(bar, "shop"));
            Assert.AreEqual("home", bar.value);
            Assert.IsFalse(ButtonOf(bar, "shop").enabledSelf);
        }

        [UnityTest]
        public IEnumerator RemovingTheSelectedItem_SelectsTheFirstEnabledOne()
        {
            var bar = AddBar();
            yield return ControlsPanel.Frames();
            bar.value = "shop";
            Assert.IsTrue(bar.RemoveTab("shop"));
            Assert.AreEqual("home", bar.value);
            Assert.AreEqual(2, bar.Count);
            Assert.IsFalse(bar.RemoveTab("nothing"));
        }

        [UnityTest]
        public IEnumerator SegmentedSkin_SwitchesTheClasses()
        {
            var bar = AddBar(TabBarSkin.Segmented);
            yield return ControlsPanel.Frames();
            Assert.IsTrue(bar.ClassListContains("fdp-tab-bar--segmented"));
            Assert.IsFalse(bar.ClassListContains("ds-tabs"));
            Assert.IsTrue(ButtonOf(bar, "home").ClassListContains("fdp-tab-bar__segment"));
            Assert.IsTrue(ButtonOf(bar, "home").ClassListContains("fdp-tab-bar__item--selected"));
            bar.Skin = TabBarSkin.Tabs;
            Assert.IsTrue(bar.ClassListContains("ds-tabs"));
            Assert.IsTrue(ButtonOf(bar, "home").ClassListContains("ds-tab"));
        }

        [UnityTest]
        public IEnumerator SegmentedControl_IsATabBarInTheSegmentedSkin()
        {
            _panel = ControlsPanel.Create();
            var control = new SegmentedControl { Items = "easy:Easy,normal:Normal,hard:Hard", value = "normal" };
            _panel.Root.Add(control);
            yield return ControlsPanel.Frames();
            Assert.AreEqual(3, control.Count);
            Assert.AreEqual("normal", control.value);
            Assert.AreEqual(TabBarSkin.Segmented, control.Skin);
            Assert.AreEqual("Hard", control.Q<Button>("tab-hard").text);
        }

        [UnityTest]
        public IEnumerator Badges_FollowTheTree_AndAParentShowsItsChildren()
        {
            var bar = AddBar();
            yield return ControlsPanel.Frames();
            var tree = new BadgeTree();
            bar.BindBadges(tree, "main");
            var shopBadge = ButtonOf(bar, "shop").Q<Label>(null, "fdp-tab-bar__badge");

            Assert.AreEqual(DisplayStyle.None, shopBadge.style.display.value);
            tree.SetCount("main/shop/offers", 2);
            tree.SetCount("main/shop/gems", 1);
            Assert.AreEqual(DisplayStyle.Flex, shopBadge.style.display.value);
            Assert.AreEqual("3", shopBadge.text);

            tree.SetDot("main/bag", true);
            var bagBadge = ButtonOf(bar, "bag").Q<Label>(null, "fdp-tab-bar__badge");
            Assert.AreEqual(DisplayStyle.Flex, bagBadge.style.display.value);
            Assert.IsTrue(bagBadge.ClassListContains("fdp-tab-bar__badge--dot"));
            Assert.AreEqual(string.Empty, bagBadge.text);

            tree.SetCount("main/shop/offers", 200);
            Assert.AreEqual("99+", shopBadge.text);
        }

        [UnityTest]
        public IEnumerator SelectingAnItem_VisitsItsBadge()
        {
            var bar = AddBar();
            yield return ControlsPanel.Frames();
            var tree = new BadgeTree();
            bar.BindBadges(tree);
            tree.SetCount("shop", 4);
            var badge = ButtonOf(bar, "shop").Q<Label>(null, "fdp-tab-bar__badge");
            Assert.AreEqual("4", badge.text);
            Send.Submit(ButtonOf(bar, "shop"));
            Assert.AreEqual(DisplayStyle.None, badge.style.display.value, "visiting clears the badge");
        }

        [UnityTest]
        public IEnumerator DirectBadge_ShowsCountAndHides()
        {
            var bar = AddBar();
            yield return ControlsPanel.Frames();
            bar.SetBadge("home", new BadgeValue(5, false));
            var badge = ButtonOf(bar, "home").Q<Label>(null, "fdp-tab-bar__badge");
            Assert.AreEqual("5", badge.text);
            bar.SetBadge("home", default);
            Assert.AreEqual(DisplayStyle.None, badge.style.display.value);
        }

        [UnityTest]
        public IEnumerator AValueSetBeforeTheItemExists_IsSelectedWhenItIsAdded()
        {
            _panel = ControlsPanel.Create();
            var bar = new TabBar { value = "b" };
            _panel.Root.Add(bar);
            bar.AddTab("a", "A");
            bar.AddTab("b", "B");
            yield return ControlsPanel.Frames();
            Assert.AreEqual("b", bar.value);
        }

        // ---- right to left ----

        private static float X(VisualElement element) => element.worldBound.center.x;

        // home, shop, bag from the left (left to right) or from the right (right to left).
        private static void AssertOrder(TabBar bar, bool rightToLeft, string message)
        {
            float home = X(ButtonOf(bar, "home")), shop = X(ButtonOf(bar, "shop")), bag = X(ButtonOf(bar, "bag"));
            if (rightToLeft)
            {
                Assert.Greater(home, shop, message + ": the first item is at the right");
                Assert.Greater(shop, bag, message);
            }
            else
            {
                Assert.Less(home, shop, message + ": the first item is at the left");
                Assert.Less(shop, bag, message);
            }
        }

        [UnityTest]
        public IEnumerator LeftToRight_TheBarIsNotMirrored([Values(TabBarSkin.Tabs, TabBarSkin.Segmented)] TabBarSkin skin)
        {
            var bar = AddBar(skin);
            yield return ControlsPanel.Frames();
            Assert.IsFalse(bar.IsRightToLeft);
            Assert.IsFalse(bar.ClassListContains(TabBar.RightToLeftUssClassName));
            AssertOrder(bar, false, "left to right");
            bar.value = "shop";
            Send.Move(ButtonOf(bar, "shop"), NavigationMoveEvent.Direction.Right);
            Assert.AreEqual("bag", bar.value, "Right goes to the item on the right");
        }

        [UnityTest]
        public IEnumerator TheRtlClassOnTheRoot_MirrorsTheItems_AndTheArrowsFollowTheScreen([Values(TabBarSkin.Tabs, TabBarSkin.Segmented)] TabBarSkin skin)
        {
            var bar = AddBar(skin);
            _panel.Root.AddToClassList("fdp-rtl");
            yield return ControlsPanel.Frames();
            Assert.IsTrue(bar.IsRightToLeft);
            Assert.IsTrue(bar.ClassListContains(TabBar.RightToLeftUssClassName));
            Assert.AreEqual(FlexDirection.RowReverse, bar.resolvedStyle.flexDirection);
            AssertOrder(bar, true, "right to left");
            Assert.AreEqual(0, bar.hierarchy.IndexOf(ButtonOf(bar, "home")), "the tree (focus and reading) order stays the item order");

            bar.value = "shop";
            Send.Move(ButtonOf(bar, "shop"), NavigationMoveEvent.Direction.Right);
            Assert.AreEqual("home", bar.value, "Right goes to the item on the right: the first one");
            Send.Move(ButtonOf(bar, "home"), NavigationMoveEvent.Direction.Right);
            Assert.AreEqual("home", bar.value, "no wrap at the right edge");
            Send.Move(ButtonOf(bar, "home"), NavigationMoveEvent.Direction.Left);
            Assert.AreEqual("shop", bar.value, "Left goes to the item on the left");
            Send.Key(ButtonOf(bar, "shop"), KeyCode.End);
            Assert.AreEqual("bag", bar.value, "End: the last item, at the left edge");
            Send.Key(ButtonOf(bar, "bag"), KeyCode.Home);
            Assert.AreEqual("home", bar.value, "Home: the first item, at the right edge");
        }

        [UnityTest]
        public IEnumerator LanguageDirectionOnTheRoot_WithoutTheClass_MirrorsTheBar()
        {
            var bar = AddBar(TabBarSkin.Segmented);
            _panel.Root.languageDirection = LanguageDirection.RTL;
            bar.RefreshDirection(); // a languageDirection change alone is not announced by UI Toolkit
            yield return ControlsPanel.Frames();
            Assert.IsTrue(bar.IsRightToLeft, "the bar inherits the direction of the root (its own languageDirection is Inherit)");
            AssertOrder(bar, true, "languageDirection only");
            Send.Move(ButtonOf(bar, "home"), NavigationMoveEvent.Direction.Left);
            Assert.AreEqual("shop", bar.value);
        }

        [UnityTest]
        public IEnumerator ALanguageSwitchWhileShown_MirrorsAndUnmirrorsTheBar()
        {
            var bar = AddBar(TabBarSkin.Segmented);
            yield return ControlsPanel.Frames();
            AssertOrder(bar, false, "before");

            _panel.Root.AddToClassList("fdp-rtl");
            _panel.Root.languageDirection = LanguageDirection.RTL;
            yield return ControlsPanel.Frames();
            Assert.IsTrue(bar.IsRightToLeft, "the style pass after the class change told the bar");
            AssertOrder(bar, true, "after switching to a right-to-left language");

            _panel.Root.RemoveFromClassList("fdp-rtl");
            _panel.Root.languageDirection = LanguageDirection.LTR;
            yield return ControlsPanel.Frames();
            Assert.IsFalse(bar.IsRightToLeft);
            Assert.IsFalse(bar.ClassListContains(TabBar.RightToLeftUssClassName));
            AssertOrder(bar, false, "back to left to right");
        }

        [UnityTest]
        public IEnumerator ALeftToRightIslandInsideARightToLeftScreen_StaysLeftToRight()
        {
            _panel = ControlsPanel.Create();
            _panel.Root.AddToClassList("fdp-rtl");
            _panel.Root.languageDirection = LanguageDirection.RTL;
            var island = new VisualElement { languageDirection = LanguageDirection.LTR };
            _panel.Root.Add(island);
            var bar = new SegmentedControl { Items = "home:Home,shop:Shop,bag:Bag" };
            island.Add(bar);
            yield return ControlsPanel.Frames();
            Assert.IsFalse(bar.IsRightToLeft);
            AssertOrder(bar, false, "inside the island");
            Send.Move(ButtonOf(bar, "home"), NavigationMoveEvent.Direction.Right);
            Assert.AreEqual("shop", bar.value);
        }

        [UnityTest]
        public IEnumerator RightToLeft_TheBadgeMovesToTheTopLeftCorner()
        {
            var bar = AddBar(TabBarSkin.Segmented);
            _panel.Root.AddToClassList("fdp-rtl");
            bar.SetBadge("shop", new BadgeValue(3, false));
            yield return ControlsPanel.Frames();
            var button = ButtonOf(bar, "shop");
            var badge = button.Q<Label>(null, "fdp-tab-bar__badge");
            Assert.Less(badge.worldBound.center.x, button.worldBound.center.x, "the badge sits at the end corner, the left one");
        }

        [UnityTest]
        public IEnumerator AStackedBar_KeepsItsColumn_AndRightGoesBackInRightToLeft()
        {
            var bar = AddBar(TabBarSkin.Segmented);
            bar.style.flexDirection = FlexDirection.Column; // a game that stacks long labels (an inline style, the strongest rule)
            _panel.Root.AddToClassList("fdp-rtl");
            yield return ControlsPanel.Frames();
            Assert.AreEqual(FlexDirection.Column, bar.resolvedStyle.flexDirection);
            bar.value = "shop";
            Send.Move(ButtonOf(bar, "shop"), NavigationMoveEvent.Direction.Right);
            Assert.AreEqual("home", bar.value, "a column has no sides: Right goes back, as in a right-to-left row");
        }
    }
}
