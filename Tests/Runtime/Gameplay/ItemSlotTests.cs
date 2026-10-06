using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UIElements;

namespace FutureDigitalPlus.UIKit.Gameplay.Tests
{
    public class ItemSlotTests
    {
        private GameplayPanel _panel;

        [SetUp]
        public void SetUp() => LogAssert.ignoreFailingMessages = true;

        [TearDown]
        public void TearDown()
        {
            LogAssert.ignoreFailingMessages = false;
            _panel?.Dispose();
        }

        private ItemSlot AddSlot()
        {
            _panel = GameplayPanel.Create();
            var slot = new ItemSlot { style = { width = 56, height = 56 } };
            _panel.Root.Add(slot);
            return slot;
        }

        [UnityTest]
        public IEnumerator NewSlot_IsEmptyAndFocusable()
        {
            var slot = AddSlot();
            yield return GameplayPanel.Frames();
            Assert.IsFalse(slot.HasItem);
            Assert.IsTrue(slot.focusable);
            Assert.IsTrue(slot.ClassListContains("fdp-item-slot--empty"));
            Assert.AreEqual(-1, slot.Index);
            Assert.IsNull(slot.Grid);
        }

        [UnityTest]
        public IEnumerator SetItem_ShowsTheCountFromTwo_AndTheRarityColour()
        {
            var slot = AddSlot();
            yield return GameplayPanel.Frames();
            slot.SetItem(new ItemVisual(default, "legendary"), 5);
            var count = slot.Q<Label>("count");
            Assert.AreEqual(DisplayStyle.Flex, count.style.display.value);
            Assert.AreEqual("5", count.text);
            Assert.IsTrue(slot.ClassListContains("fdp-item-slot--legendary"));
            Assert.IsFalse(slot.ClassListContains("fdp-item-slot--empty"));
            yield return GameplayPanel.Frames(2);
            Assert.AreNotEqual(Color.clear, slot.resolvedStyle.borderTopColor);

            slot.SetItem(new ItemVisual(default, "rare"), 1);
            Assert.AreEqual(DisplayStyle.None, count.style.display.value);
            Assert.IsFalse(slot.ClassListContains("fdp-item-slot--legendary"), "the old rarity class is removed");
            Assert.IsTrue(slot.ClassListContains("fdp-item-slot--rare"));

            slot.SetEmpty();
            Assert.IsFalse(slot.HasItem);
            Assert.IsFalse(slot.ClassListContains("fdp-item-slot--rare"));
            Assert.IsTrue(slot.ClassListContains("fdp-item-slot--empty"));
        }

        [UnityTest]
        public IEnumerator CooldownShade_CoversTheGivenShareFromTheTop()
        {
            var slot = AddSlot();
            yield return GameplayPanel.Frames();
            var shade = slot.Q<VisualElement>("cooldown");
            Assert.AreEqual(DisplayStyle.None, shade.style.display.value);
            slot.Cooldown = 0.5f;
            yield return GameplayPanel.Frames(2);
            Assert.AreEqual(DisplayStyle.Flex, shade.style.display.value);
            var inner = slot.contentRect.height;
            Assert.AreEqual(inner * 0.5f, shade.resolvedStyle.height, 1.5f);
            slot.Cooldown = 2f;
            Assert.AreEqual(1f, slot.Cooldown, 0.0001f, "clamped");
            slot.Cooldown = 0f;
            Assert.AreEqual(DisplayStyle.None, shade.style.display.value);
        }

        [UnityTest]
        public IEnumerator LockedSelectedAndEquipped_AreShown()
        {
            var slot = AddSlot();
            yield return GameplayPanel.Frames();
            slot.IsLocked = true;
            slot.IsSelected = true;
            slot.IsEquipped = true;
            slot.EquippedText = "On";
            Assert.IsTrue(slot.ClassListContains("fdp-item-slot--locked"));
            Assert.IsTrue(slot.ClassListContains("fdp-item-slot--selected"));
            Assert.AreEqual(DisplayStyle.Flex, slot.Q<VisualElement>("lock").style.display.value);
            var chip = slot.Q<Label>("equipped");
            Assert.AreEqual(DisplayStyle.Flex, chip.style.display.value);
            Assert.AreEqual("On", chip.text);
            Assert.IsTrue(chip.ClassListContains("ds-badge"));
            slot.IsLocked = false;
            slot.IsEquipped = false;
            Assert.AreEqual(DisplayStyle.None, slot.Q<VisualElement>("lock").style.display.value);
            Assert.AreEqual(DisplayStyle.None, chip.style.display.value);
        }

        [UnityTest]
        public IEnumerator Count_IsClampedAtZero()
        {
            var slot = AddSlot();
            yield return GameplayPanel.Frames();
            slot.Count = -4;
            Assert.AreEqual(0, slot.Count);
        }
    }
}
