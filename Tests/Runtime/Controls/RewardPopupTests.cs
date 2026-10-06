using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UIElements;

namespace FutureDigitalPlus.UIKit.Controls.Tests
{
    public class RewardPopupTests
    {
        private ControlsPanel _panel;
        private VisualElement _anchor;

        [SetUp]
        public void SetUp() => LogAssert.ignoreFailingMessages = true;

        [TearDown]
        public void TearDown()
        {
            LogAssert.ignoreFailingMessages = false;
            _panel?.Dispose();
        }

        private IEnumerator Prepare(bool reduceMotion = false)
        {
            _panel = ControlsPanel.Create(reduceMotion);
            _anchor = new Button { text = "Chest", focusable = true };
            _panel.Root.Add(_anchor);
            yield return ControlsPanel.Frames();
            _anchor.Focus();
        }

        private static List<RewardItem> ThreeRewards() => new List<RewardItem>
        {
            new RewardItem("coins", "Coins", "x250"),
            new RewardItem("gem", "Gem", "x3", rarity: "epic"),
            new RewardItem("skin", "Skin", null, null, "Legendary"),
        };

        private static Button ActionOf(RewardPopup popup) => popup.Card.Q<Button>("action");

        private static List<VisualElement> Tiles(RewardPopup popup) => popup.Card.Query<VisualElement>(className: "fdp-reward__item").ToList();

        [UnityTest]
        public IEnumerator Show_BuildsTheCard_WithHiddenRewards_AndASkipButton()
        {
            yield return Prepare();
            var popup = RewardPopup.Show(_anchor, "You got", ThreeRewards(), initialDelaySeconds: 5f);
            Assert.IsNotNull(popup);
            Assert.IsTrue(popup.IsOpen);
            Assert.AreEqual(3, Tiles(popup).Count);
            Assert.AreEqual(0, popup.Sequence.RevealedCount);
            foreach (var tile in Tiles(popup)) Assert.IsTrue(tile.ClassListContains("fdp-reward__item--hidden"));
            Assert.AreEqual("Skip", ActionOf(popup).text);
            Assert.IsTrue(popup.Card.ClassListContains("ds-modal"));
            Assert.AreEqual("You got", popup.Card.Q<Label>(null, "ds-modal__title").text);
            Assert.IsTrue(Tiles(popup)[1].ClassListContains("fdp-reward__item--epic"));
            Assert.IsTrue(Tiles(popup)[2].ClassListContains("fdp-reward__item--legendary"), "rarity is case-insensitive");
            yield return ControlsPanel.Frames();
            Assert.AreSame(ActionOf(popup), _anchor.panel.focusController.focusedElement);
            popup.Close();
        }

        [UnityTest]
        public IEnumerator Rewards_AppearOneAfterAnother_ThenTheButtonSaysClaim()
        {
            yield return Prepare();
            var popup = RewardPopup.Show(_anchor, "Rewards", ThreeRewards(), "Claim", "Skip", 0.05f, 0.1f);
            yield return ControlsPanel.WaitSeconds(0.12f);
            Assert.GreaterOrEqual(popup.Sequence.RevealedCount, 1);
            Assert.Less(popup.Sequence.RevealedCount, 3);
            Assert.AreEqual("Skip", ActionOf(popup).text);
            yield return ControlsPanel.WaitSeconds(0.5f);
            Assert.IsTrue(popup.Sequence.IsComplete);
            Assert.AreEqual("Claim", ActionOf(popup).text);
            foreach (var tile in Tiles(popup)) Assert.IsTrue(tile.ClassListContains("fdp-reward__item--shown"));
            popup.Close();
        }

        [UnityTest]
        public IEnumerator Button_SkipsFirst_ThenClaims()
        {
            yield return Prepare();
            var popup = RewardPopup.Show(_anchor, "Rewards", ThreeRewards(), initialDelaySeconds: 5f);
            var claimed = 0;
            var closed = 0;
            popup.Claimed += () => claimed++;
            popup.Closed += () => closed++;

            Send.Submit(ActionOf(popup));
            Assert.AreEqual(3, popup.Sequence.RevealedCount);
            Assert.IsTrue(popup.Sequence.WasSkipped);
            Assert.IsTrue(popup.IsOpen, "the first press only skips");
            Assert.AreEqual("Claim", ActionOf(popup).text);
            Assert.AreEqual(0, claimed);

            Send.Submit(ActionOf(popup));
            Assert.AreEqual(1, claimed);
            Assert.AreEqual(1, closed);
            Assert.IsFalse(popup.IsOpen);
            Assert.IsNull(popup.Card.panel, "removed from the panel");
            Assert.AreSame(_anchor, _anchor.panel.focusController.focusedElement, "focus returns");
        }

        [UnityTest]
        public IEnumerator TapOnTheCard_Skips_ButDoesNotClaim()
        {
            yield return Prepare();
            var popup = RewardPopup.Show(_anchor, "Rewards", ThreeRewards(), initialDelaySeconds: 5f);
            using (var e = ClickEvent.GetPooled())
            {
                e.target = popup.Card;
                popup.Card.SendEvent(e);
            }

            Assert.IsTrue(popup.Sequence.IsComplete);
            Assert.IsTrue(popup.IsOpen);
            Assert.IsTrue(popup.Claim(), "claim works now");
        }

        [UnityTest]
        public IEnumerator Claim_BeforeEverythingIsShown_Fails()
        {
            yield return Prepare();
            var popup = RewardPopup.Show(_anchor, "Rewards", ThreeRewards(), initialDelaySeconds: 5f);
            Assert.IsFalse(popup.Claim());
            Assert.IsTrue(popup.IsOpen);
            popup.Close();
            Assert.IsFalse(popup.IsOpen);
            popup.Close(); // harmless
        }

        [UnityTest]
        public IEnumerator Back_BehavesLikeTheButton()
        {
            yield return Prepare();
            var popup = RewardPopup.Show(_anchor, "Rewards", ThreeRewards(), initialDelaySeconds: 5f);
            Assert.IsTrue(popup.HandleBack());
            Assert.IsTrue(popup.Sequence.IsComplete);
            Assert.IsTrue(popup.IsOpen);
            var claimed = false;
            popup.Claimed += () => claimed = true;
            Assert.IsTrue(popup.HandleBack());
            Assert.IsTrue(claimed);
            Assert.IsFalse(popup.HandleBack(), "closed: nothing to consume");
        }

        [UnityTest]
        public IEnumerator Cancel_SkipsThenClaims()
        {
            yield return Prepare();
            var popup = RewardPopup.Show(_anchor, "Rewards", ThreeRewards(), initialDelaySeconds: 5f);
            Send.Cancel(popup.Card);
            Assert.IsTrue(popup.Sequence.IsComplete);
            yield return ControlsPanel.Frames(2);
            Send.Cancel(popup.Card);
            Assert.IsFalse(popup.IsOpen);
        }

        [UnityTest]
        public IEnumerator ReducedMotion_ShowsEverythingAtOnce()
        {
            yield return Prepare(reduceMotion: true);
            var popup = RewardPopup.Show(_anchor, "Rewards", ThreeRewards());
            Assert.IsTrue(popup.Sequence.IsComplete);
            Assert.IsFalse(popup.Sequence.WasSkipped);
            Assert.AreEqual("Claim", ActionOf(popup).text);
            popup.Close();
        }

        [UnityTest]
        public IEnumerator NoRewards_OffersClaimAtOnce()
        {
            yield return Prepare();
            var popup = RewardPopup.Show(_anchor, "Nothing", new List<RewardItem>());
            Assert.AreEqual("Claim", ActionOf(popup).text);
            Assert.IsTrue(popup.Claim());
        }

        [UnityTest]
        public IEnumerator Show_WithoutAPanelOrRewards_ReturnsNull()
        {
            yield return Prepare();
            Assert.IsNull(RewardPopup.Show(new VisualElement(), "x", ThreeRewards()));
            Assert.IsNull(RewardPopup.Show(_anchor, "x", null));
        }

        [UnityTest]
        public IEnumerator Queue_ShowsSetsOneAtATime_InOrder()
        {
            yield return Prepare(reduceMotion: true);
            var queue = new RewardPopupQueue();
            var order = new List<string>();
            var drained = 0;
            queue.Drained += () => drained++;

            queue.Enqueue(_anchor, "First", ThreeRewards(), () => order.Add("first"));
            queue.Enqueue(_anchor, "Second", ThreeRewards(), () => order.Add("second"));
            Assert.AreEqual(1, queue.WaitingCount);
            Assert.AreEqual("First", queue.Current.Card.Q<Label>(null, "ds-modal__title").text);
            Assert.AreEqual(1, _panel.Root.panel.visualTree.Query<VisualElement>(className: "fdp-reward").ToList().Count, "only one card at a time");

            Assert.IsTrue(queue.Current.Claim());
            Assert.AreEqual("Second", queue.Current.Card.Q<Label>(null, "ds-modal__title").text);
            Assert.AreEqual(0, queue.WaitingCount);
            Assert.AreEqual(0, drained);

            Assert.IsTrue(queue.Current.Claim());
            Assert.IsNull(queue.Current);
            Assert.AreEqual(1, drained);
            CollectionAssert.AreEqual(new[] { "first", "second" }, order);
        }

        [UnityTest]
        public IEnumerator Queue_Clear_ClosesTheCurrentCardAndForgetsTheRest()
        {
            yield return Prepare(reduceMotion: true);
            var queue = new RewardPopupQueue();
            queue.Enqueue(_anchor, "First", ThreeRewards());
            queue.Enqueue(_anchor, "Second", ThreeRewards());
            var first = queue.Current;
            queue.Clear();
            Assert.IsFalse(first.IsOpen);
            Assert.IsNull(queue.Current);
            Assert.AreEqual(0, queue.WaitingCount);
            Assert.AreEqual(0, _panel.Root.panel.visualTree.Query<VisualElement>(className: "fdp-reward").ToList().Count);
        }
    }
}
