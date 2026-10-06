using System;
using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UIElements;

namespace FutureDigitalPlus.UIKit.Controls.Tests
{
    /// <summary>Controls used in the wrong order, twice, with nothing, and on elements that are not on a panel.</summary>
    public class AdversarialControlsTests
    {
        private ControlsPanel _panel;
        private Button _anchor;

        [SetUp]
        public void SetUp() => LogAssert.ignoreFailingMessages = true;

        [TearDown]
        public void TearDown()
        {
            LogAssert.ignoreFailingMessages = false;
            _panel?.Dispose();
            _panel = null;
        }

        private IEnumerator Prepare()
        {
            if (!Application.isPlaying) Assert.Ignore("Needs a runtime panel (Play Mode).");
            _panel = ControlsPanel.Create();
            _anchor = new Button { text = "Anchor", focusable = true, style = { width = 120, height = 40 } };
            _panel.Root.Add(_anchor);
            yield return ControlsPanel.Frames();
            _anchor.Focus();
        }

        [Test]
        public void TabBar_ValueAssignedBeforeItems_SelectsThatItem()
        {
            var bar = new TabBar { value = "b", Items = "a:Alpha,b:Beta,c:Gamma" };

            Assert.AreEqual("b", bar.value, "an object initializer may assign value first; the id is remembered until the item exists");
        }

        [Test]
        public void TabBar_DuplicateAndEmptyIds_AreRefusedAndLeaveTheBarIntact()
        {
            var bar = new TabBar();
            bar.AddTab("a", "A");
            Assert.Throws<ArgumentException>(() => bar.AddTab("a", "Again"));
            Assert.Throws<ArgumentException>(() => bar.AddTab("", "Empty"));
            Assert.Throws<ArgumentException>(() => bar.AddTab(null, "Null"));
            Assert.AreEqual(1, bar.Count);
            Assert.IsFalse(bar.RemoveTab("missing"));
            Assert.IsFalse(bar.RemoveTab(null));
            bar.SetTabEnabled("missing", false);
            bar.SetTabLabel(null, "x");
            bar.SetBadge("missing", new BadgeValue(3, false));
            bar.ClearTabs();
            bar.ClearTabs();
            Assert.AreEqual(0, bar.Count);
            Assert.IsTrue(string.IsNullOrEmpty(bar.value));
        }

        [Test]
        public void TabBar_RemovingTheSelectedTabWhileOthersAreDisabled_ClearsTheSelection()
        {
            var bar = new TabBar();
            bar.AddTab("a", "A");
            bar.AddTab("b", "B", enabled: false);
            bar.RemoveTab("a");

            Assert.IsTrue(string.IsNullOrEmpty(bar.value), "no enabled tab is left to select");
            Assert.AreEqual(1, bar.Count);
        }

        [Test]
        public void SelectField_HostileValuesAndChoices_StayInRange()
        {
            var field = new SelectField();
            field.Entries = null;
            field.Choices = null;
            field.value = int.MaxValue;
            field.value = int.MinValue;
            Assert.AreEqual(-1, field.value);
            Assert.IsNull(field.OpenList());

            field.Choices = new[] { "a", null, "c" };
            field.value = 2;
            field.Choices = new[] { "only" };
            Assert.AreEqual(0, field.value, "the index follows the shrunken list");
            Assert.AreEqual("only", field.SelectedText);
            field.Choices = new string[0];
            Assert.AreEqual(-1, field.value);
            Assert.IsNull(field.SelectedText);
            field.Entries = " , ,a,,b , ";
            Assert.AreEqual(2, field.Choices.Count);
        }

        [UnityTest]
        public IEnumerator Popups_OnAnchorsThatAreNotOnAPanel_ReturnNullAndDoNotThrow()
        {
            yield return Prepare();
            var floating = new Button();
            var items = new List<MenuEntry> { new MenuEntry("a", "A") };

            Assert.IsNull(PopupMenu.Show(floating, items, Vector2.zero));
            Assert.IsNull(PopupMenu.ShowBelow(floating, items));
            Assert.IsNull(PopupMenu.Show(null, items, Vector2.zero));
            Assert.Throws<ArgumentNullException>(() => PopupMenu.ShowBelow(_anchor, (IReadOnlyList<MenuEntry>)null));
            Assert.IsFalse(new BottomSheet().Open(floating));
            Assert.IsFalse(new BottomSheet().Open(null));
            Assert.IsNull(RewardPopup.Show(floating, "Reward", new[] { new RewardItem("gold", "Gold", "10") }));
        }

        [UnityTest]
        public IEnumerator BottomSheet_OpenedClosedAndReopenedRepeatedly_EndsClosedWithNoLayerLeft()
        {
            yield return Prepare();
            var sheet = new BottomSheet();
            sheet.Add(new Button { text = "Inside", focusable = true });
            var closed = 0;
            sheet.Closed += () => closed++;

            for (var i = 0; i < 5; i++)
            {
                Assert.IsTrue(sheet.Open(_anchor));
                Assert.IsFalse(sheet.Open(_anchor));
                sheet.Close();
                sheet.Close();
                yield return ControlsPanel.WaitSeconds(0.35f);
            }
            sheet.Close();

            Assert.IsFalse(sheet.IsOpen);
            Assert.AreEqual(5, closed, "Closed is raised once per opening");
            Assert.AreEqual(0, _panel.Root.Query<VisualElement>(className: "fdp-popup-layer").ToList().Count);
        }

        [UnityTest]
        public IEnumerator PopupMenu_ClosedTwiceOrBackWhenClosed_IsHarmless()
        {
            yield return Prepare();
            var menu = PopupMenu.Show(_anchor, new List<MenuEntry> { new MenuEntry("a", "A"), new MenuEntry("b", "B") }, new Vector2(10f, 10f));
            Assert.IsNotNull(menu);
            var closed = 0;
            menu.Closed += () => closed++;

            menu.Close();
            menu.Close();
            Assert.IsFalse(menu.HandleBack());
            yield return ControlsPanel.Frames();

            Assert.AreEqual(1, closed);
            Assert.AreEqual(0, _panel.Root.Query<VisualElement>(className: "fdp-menu-layer").ToList().Count);
        }

        [UnityTest]
        public IEnumerator RewardPopupQueue_ClearedWhileShowingAndEnqueuedFromCallbacks_NeverLeavesAPopup()
        {
            yield return Prepare();
            var queue = new RewardPopupQueue();
            var claimed = 0;
            Action again = null;
            again = () =>
            {
                claimed++;
                if (claimed < 3) queue.Enqueue(_anchor, "Again " + claimed, new[] { new RewardItem("gold", "Gold", "1") }, again);
            };
            queue.Enqueue(_anchor, "First", new[] { new RewardItem("gold", "Gold", "1") }, again);
            queue.Enqueue(_anchor, "Second", new[] { new RewardItem("gem", "Gem", "2") });
            yield return ControlsPanel.Frames();

            queue.Current.Skip();
            queue.Current.Claim();
            yield return ControlsPanel.Frames();
            queue.Clear();
            yield return ControlsPanel.Frames();

            Assert.IsNull(queue.Current);
            Assert.AreEqual(0, queue.WaitingCount);
            Assert.AreEqual(0, _panel.Root.Query<VisualElement>(className: "fdp-popup-layer").ToList().Count);
        }
    }
}
