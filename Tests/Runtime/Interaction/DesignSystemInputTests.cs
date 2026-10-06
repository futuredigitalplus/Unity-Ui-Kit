using System.Collections;
using System.Collections.Generic;
using FutureDigitalPlus.UIKit.DesignSystem.Behaviour;
using FutureDigitalPlus.UIKit.DesignSystem.Behaviour.UIDocument;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UIElements;

namespace FutureDigitalPlus.UIKit.Interaction.Tests
{
    /// <summary>The input and accessibility changes in the design system's behaviours: Submit as click, focusable items, Cancel on drawers, keyboard drag, reduced motion.</summary>
    public class DesignSystemInputTests
    {
        private InteractionPanel _panel;

        [SetUp]
        public void SetUp() => LogAssert.ignoreFailingMessages = true;

        [TearDown]
        public void TearDown()
        {
            LogAssert.ignoreFailingMessages = false;
            _panel?.Dispose();
            _panel = null;
        }

        [UnityTest]
        public IEnumerator MakeActivatable_FocusesAPlainElement_AndTurnsSubmitIntoOneClick()
        {
            _panel = InteractionPanel.Create();
            var item = InteractionPanel.WithClasses(new VisualElement { name = "row" }, "ds-sheet__row");
            _panel.Screen.Add(item);
            yield return InteractionPanel.Frames();

            var clicks = 0;
            item.RegisterCallback<ClickEvent>(_ => clicks++);
            DesignSystemBehaviourBase<UIDocument>.MakeActivatable(item);
            DesignSystemBehaviourBase<UIDocument>.MakeActivatable(item);   // idempotent

            Assert.IsTrue(item.focusable);
            Send.Submit(item);
            Assert.AreEqual(1, clicks, "Submit raises exactly one ClickEvent, even after two calls");
        }

        [UnityTest]
        public IEnumerator MakeActivatable_LeavesButtonsAndFieldsToTheirOwnSubmitHandling()
        {
            _panel = InteractionPanel.Create();
            var button = new Button();
            var toggle = new Toggle();
            _panel.Screen.Add(button);
            _panel.Screen.Add(toggle);
            yield return InteractionPanel.Frames();

            DesignSystemBehaviourBase<UIDocument>.MakeActivatable(button);
            DesignSystemBehaviourBase<UIDocument>.MakeActivatable(toggle);
            Assert.IsFalse(button.ClassListContains("ds-activate--wired"));
            Assert.IsFalse(toggle.ClassListContains("ds-activate--wired"));

            var clicks = 0;
            button.RegisterCallback<ClickEvent>(_ => clicks++);
            Send.Submit(button);
            Assert.AreEqual(0, clicks, "a Button reacts to Submit through its own clicked event, not through a synthetic ClickEvent");
        }

        [UnityTest]
        public IEnumerator EnsureFocusable_CoversTheClickableItemFamilies()
        {
            _panel = InteractionPanel.Create();
            var items = new List<VisualElement>();
            foreach (var cls in new[] { "ds-nav-item", "ds-rail-item", "ds-sheet__row", "ds-bottom-nav__item" })
            {
                var e = InteractionPanel.WithClasses(new VisualElement(), cls);
                items.Add(e);
                _panel.Screen.Add(e);
            }
            var plain = new VisualElement();
            _panel.Screen.Add(plain);
            yield return InteractionPanel.Frames();

            DesignSystemBehaviourBase<UIDocument>.EnsureAll(_panel.Screen);

            foreach (var e in items) Assert.IsTrue(e.focusable, string.Join(" ", e.GetClasses()));
            Assert.IsFalse(plain.focusable, "unrelated elements are not touched");
        }

        [UnityTest]
        public IEnumerator SideNav_ItemsAreFocusable_AndSubmitSwitchesThePanel()
        {
            _panel = InteractionPanel.Create();
            var host = new VisualElement();
            var nav = InteractionPanel.WithClasses(new VisualElement(), "ds-side-nav");
            var itemA = InteractionPanel.WithClasses(new VisualElement { name = "a" }, "ds-nav-item");
            var itemB = InteractionPanel.WithClasses(new VisualElement { name = "b" }, "ds-nav-item");
            nav.Add(itemA);
            nav.Add(itemB);
            var panels = InteractionPanel.WithClasses(new VisualElement(), "ds-tabpanels");
            var panelA = InteractionPanel.WithClasses(new VisualElement(), "ds-tabpanel");
            var panelB = InteractionPanel.WithClasses(new VisualElement(), "ds-tabpanel");
            panels.Add(panelA);
            panels.Add(panelB);
            host.Add(nav);
            host.Add(panels);
            _panel.Screen.Add(host);
            yield return InteractionPanel.Frames();

            Assert.IsTrue(DesignSystemBehaviourBase<UIDocument>.WireSideNav(nav));
            Assert.IsTrue(itemA.focusable);
            Assert.IsTrue(itemB.focusable);
            Assert.IsTrue(panelA.ClassListContains("is-active"));

            Send.Submit(itemB);
            Assert.IsTrue(itemB.ClassListContains("is-active"));
            Assert.IsTrue(panelB.ClassListContains("is-active"));
            Assert.IsFalse(panelA.ClassListContains("is-active"));
        }

        [UnityTest]
        public IEnumerator Drawer_ClosesOnCancel_FromTheDrawerAndFromTheOpener()
        {
            _panel = InteractionPanel.Create();
            var wrapper = InteractionPanel.WithClasses(new VisualElement(), "ds-drawer-wrap");
            var drawer = InteractionPanel.WithClasses(new VisualElement(), "ds-drawer");
            var inside = new Button { text = "inside" };
            drawer.Add(inside);
            wrapper.Add(drawer);
            var opener = new Button { text = "open" };
            _panel.Screen.Add(opener);
            _panel.Screen.Add(wrapper);
            yield return InteractionPanel.Frames();

            DesignSystemBehaviourBase<UIDocument>.WireDrawer(opener, wrapper);

            Send.Submit(opener);
            Assert.IsTrue(wrapper.ClassListContains("is-open"), "the opener opens it");
            Send.Cancel(inside);
            Assert.IsFalse(wrapper.ClassListContains("is-open"), "Cancel inside the drawer closes it");

            Send.Submit(opener);
            Assert.IsTrue(wrapper.ClassListContains("is-open"));
            Send.Cancel(opener);
            Assert.IsFalse(wrapper.ClassListContains("is-open"), "Cancel on the opener closes it");
        }

        [UnityTest]
        public IEnumerator Drawer_Cancel_WhileClosed_IsNotConsumed()
        {
            _panel = InteractionPanel.Create();
            var wrapper = InteractionPanel.WithClasses(new VisualElement(), "ds-drawer-wrap");
            var opener = new Button { text = "open" };
            _panel.Screen.Add(opener);
            _panel.Screen.Add(wrapper);
            yield return InteractionPanel.Frames();
            DesignSystemBehaviourBase<UIDocument>.WireDrawer(opener, wrapper);

            var reachedRoot = false;
            _panel.Screen.RegisterCallback<NavigationCancelEvent>(_ => reachedRoot = true);
            Send.Cancel(opener);
            Assert.IsTrue(reachedRoot, "a closed drawer lets Cancel through to whoever handles back");
        }

        [UnityTest]
        public IEnumerator ScrollAutoHide_FlashesWhenFocusMovesInside()
        {
            _panel = InteractionPanel.Create();
            var scroll = InteractionPanel.WithClasses(new ScrollView(), "ds-scroll--auto-hide");
            var button = new Button { text = "inside" };
            scroll.Add(button);
            _panel.Screen.Add(scroll);
            yield return InteractionPanel.Frames();

            DesignSystemBehaviourBase<UIDocument>.EnsureScrollAutoHide(_panel.Screen);
            Assert.IsFalse(scroll.ClassListContains("is-scrolling"));
            button.Focus();
            yield return null;
            Assert.IsTrue(scroll.ClassListContains("is-scrolling"));
        }

        private struct DragRig
        {
            public VisualElement ZoneA, ZoneB, Item;
        }

        private DragRig BuildDragRig()
        {
            var rig = new DragRig
            {
                ZoneA = InteractionPanel.WithClasses(new VisualElement { name = "zoneA" }, "ds-drop-zone"),
                ZoneB = InteractionPanel.WithClasses(new VisualElement { name = "zoneB" }, "ds-drop-zone"),
                Item = InteractionPanel.WithClasses(new VisualElement { name = "item" }, "ds-draggable"),
            };
            rig.ZoneA.Add(rig.Item);
            _panel.Screen.Add(rig.ZoneA);
            _panel.Screen.Add(rig.ZoneB);
            return rig;
        }

        [UnityTest]
        public IEnumerator KeyboardDrag_PickUp_Cycle_Drop_MovesTheItem()
        {
            _panel = InteractionPanel.Create();
            var rig = BuildDragRig();
            yield return InteractionPanel.Frames();
            DesignSystemBehaviourBase<UIDocument>.WireDraggable(rig.Item);
            Assert.IsTrue(rig.Item.focusable, "a draggable can take focus");
            rig.Item.Focus();

            Send.Submit(rig.Item);
            Assert.IsTrue(rig.Item.ClassListContains("is-dragging"), "picked up");
            Assert.IsTrue(rig.ZoneA.ClassListContains("is-drag-over"), "the zone it came from is the current one");

            Send.Move(rig.Item, NavigationMoveEvent.Direction.Right);
            Assert.IsTrue(rig.ZoneB.ClassListContains("is-drag-over"));
            Assert.IsFalse(rig.ZoneA.ClassListContains("is-drag-over"));

            Send.Move(rig.Item, NavigationMoveEvent.Direction.Right);
            Assert.IsTrue(rig.ZoneA.ClassListContains("is-drag-over"), "the cycle wraps around");
            Send.Move(rig.Item, NavigationMoveEvent.Direction.Left);
            Assert.IsTrue(rig.ZoneB.ClassListContains("is-drag-over"), "and runs backwards");

            Send.Submit(rig.Item);
            Assert.AreSame(rig.ZoneB, rig.Item.parent, "dropped into the chosen zone");
            Assert.IsFalse(rig.Item.ClassListContains("is-dragging"));
            Assert.IsFalse(rig.ZoneB.ClassListContains("is-drag-over"));
            yield return InteractionPanel.Frames();
            Assert.AreEqual("item", _panel.FocusedName, "focus returns to the item after the move");
        }

        [UnityTest]
        public IEnumerator KeyboardDrag_Cancel_PutsTheItemBack()
        {
            _panel = InteractionPanel.Create();
            var rig = BuildDragRig();
            yield return InteractionPanel.Frames();
            DesignSystemBehaviourBase<UIDocument>.WireDraggable(rig.Item);
            rig.Item.Focus();

            Send.Submit(rig.Item);
            Send.Move(rig.Item, NavigationMoveEvent.Direction.Down);
            Assert.IsTrue(rig.ZoneB.ClassListContains("is-drag-over"));

            var reachedParent = false;
            _panel.Screen.RegisterCallback<NavigationCancelEvent>(_ => reachedParent = true);
            Send.Cancel(rig.Item);

            Assert.AreSame(rig.ZoneA, rig.Item.parent);
            Assert.IsFalse(rig.Item.ClassListContains("is-dragging"));
            Assert.IsFalse(rig.ZoneB.ClassListContains("is-drag-over"));
            Assert.IsFalse(reachedParent, "Cancel that ends a pick-up is consumed");
        }

        [UnityTest]
        public IEnumerator KeyboardDrag_NavigationIsNotConsumedWhileNothingIsPickedUp()
        {
            _panel = InteractionPanel.Create();
            var rig = BuildDragRig();
            yield return InteractionPanel.Frames();
            DesignSystemBehaviourBase<UIDocument>.WireDraggable(rig.Item);

            var reachedParent = false;
            _panel.Screen.RegisterCallback<NavigationMoveEvent>(_ => reachedParent = true);
            Send.Move(rig.Item, NavigationMoveEvent.Direction.Down);
            Assert.IsTrue(reachedParent, "focus can still move away from a draggable");
        }

        [UnityTest]
        public IEnumerator KeyboardDrag_LosingFocusEndsThePickUp()
        {
            _panel = InteractionPanel.Create();
            var rig = BuildDragRig();
            var other = new Button { text = "other" };
            _panel.Screen.Add(other);
            yield return InteractionPanel.Frames();
            DesignSystemBehaviourBase<UIDocument>.WireDraggable(rig.Item);
            rig.Item.Focus();
            Send.Submit(rig.Item);
            Assert.IsTrue(rig.Item.ClassListContains("is-dragging"));

            other.Focus();
            yield return null;
            Assert.IsFalse(rig.Item.ClassListContains("is-dragging"));
            Assert.IsFalse(rig.ZoneA.ClassListContains("is-drag-over"));
        }

        private static float AngleOf(VisualElement e) => e.resolvedStyle.rotate.angle.ToDegrees();

        [UnityTest]
        public IEnumerator Spinner_Rotates_AndUnderReducedMotionPulsesInsteadOfRotating()
        {
            _panel = InteractionPanel.Create(configure: host => host.AddComponent<DesignSystemBehaviour>());
            var spinner = InteractionPanel.WithClasses(new VisualElement(), "ds-spinner ds-is-spinning");
            _panel.Screen.Add(spinner);
            yield return new WaitForSecondsRealtime(0.4f);

            var maxAngle = 0f;
            for (var i = 0; i < 20; i++)
            {
                maxAngle = Mathf.Max(maxAngle, Mathf.Abs(AngleOf(spinner)));
                yield return null;
            }
            Assert.Greater(maxAngle, 1f, "a spinner turns");
            Assert.AreEqual(1f, spinner.resolvedStyle.opacity, 0.001f, "and keeps full opacity");

            _panel.Root.AddToClassList("fdp-reduce-motion");
            yield return new WaitForSecondsRealtime(0.4f);

            var minOpacity = 1f;
            var turned = 0f;
            for (var i = 0; i < 40; i++)
            {
                minOpacity = Mathf.Min(minOpacity, spinner.resolvedStyle.opacity);
                turned = Mathf.Max(turned, Mathf.Abs(AngleOf(spinner)));
                yield return new WaitForSecondsRealtime(0.05f);
            }
            Assert.AreEqual(0f, turned, 0.01f, "no rotation under reduced motion");
            Assert.Less(minOpacity, 0.9f, "a slow opacity pulse instead");

            _panel.Root.RemoveFromClassList("fdp-reduce-motion");
            yield return new WaitForSecondsRealtime(0.4f);
            Assert.AreEqual(1f, spinner.resolvedStyle.opacity, 0.001f, "full opacity is back once motion is allowed");
        }

        [UnityTest]
        public IEnumerator Spinner_StopsPulsing_WhenItStopsSpinning()
        {
            _panel = InteractionPanel.Create(configure: host => host.AddComponent<DesignSystemBehaviour>());
            _panel.Root.AddToClassList("fdp-reduce-motion");
            var spinner = InteractionPanel.WithClasses(new VisualElement(), "ds-spinner ds-is-spinning");
            _panel.Screen.Add(spinner);
            yield return new WaitForSecondsRealtime(0.5f);

            DesignSystemBehaviourBase<UIDocument>.SetSpinning(spinner, false);
            yield return new WaitForSecondsRealtime(0.5f);
            Assert.AreEqual(1f, spinner.resolvedStyle.opacity, 0.001f);
        }

        [UnityTest]
        public IEnumerator SkeletonShimmer_IsHiddenUnderReducedMotion_AndBackWithoutIt()
        {
            _panel = InteractionPanel.Create();
            var skeleton = InteractionPanel.WithClasses(new VisualElement(), "ds-skeleton");
            skeleton.style.width = 100;
            skeleton.style.height = 20;
            _panel.Screen.Add(skeleton);
            yield return InteractionPanel.Frames();

            DesignSystemBehaviourBase<UIDocument>.EnsureSkeletonShimmers(_panel.Screen);
            var shimmer = skeleton.Q(className: "ds-skeleton__shimmer");
            Assert.IsNotNull(shimmer);
            yield return new WaitForSecondsRealtime(0.1f);
            Assert.AreNotEqual(DisplayStyle.None, shimmer.resolvedStyle.display);

            _panel.Root.AddToClassList("fdp-reduce-motion");
            yield return new WaitForSecondsRealtime(0.1f);
            Assert.AreEqual(DisplayStyle.None, shimmer.resolvedStyle.display);

            _panel.Root.RemoveFromClassList("fdp-reduce-motion");
            yield return new WaitForSecondsRealtime(0.1f);
            Assert.AreNotEqual(DisplayStyle.None, shimmer.resolvedStyle.display);
        }
    }
}
