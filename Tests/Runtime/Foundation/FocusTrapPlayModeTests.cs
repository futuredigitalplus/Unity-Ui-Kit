using System;
using System.Collections;
using NUnit.Framework;
using UnityEngine.TestTools;
using UnityEngine.UIElements;

namespace FutureDigitalPlus.UIKit.Foundation.Tests
{
    /// <summary>PlayMode tests of <see cref="FocusTrap"/> and <see cref="UiFocus"/>: trap, restore, nesting across layers, a removed opener and two panels.</summary>
    public class FocusTrapPlayModeTests : PlayModeBase
    {
        static VisualElement Focused(VisualElement any)
        {
            return any.panel.focusController.focusedElement as VisualElement;
        }

        static VisualElement Container(VisualElement parent, string name, int buttons = 2)
        {
            VisualElement container = new VisualElement { name = name };
            for (int i = 0; i < buttons; i++)
                container.Add(new Button { name = name + "-" + i, text = name + i });
            parent.Add(container);
            return container;
        }

        [UnityTest]
        public IEnumerator Trap_FocusesTheFirstControl_AndDisposeGivesFocusBackToTheOpener()
        {
            yield return CreatePanel();
            Button opener = new Button { name = "opener" };
            Panel.Root.Add(opener);
            yield return FoundationPanel.Frames(2);
            opener.Focus();
            Assert.AreSame(opener, Focused(opener));

            VisualElement dialog = Container(Panel.Root, "dialog");
            FocusTrap trap = new FocusTrap(dialog);
            Assert.AreSame(dialog[0], Focused(opener), "the first control took focus");
            Assert.IsTrue(trap.IsActive);

            trap.Dispose();
            Assert.AreSame(opener, Focused(opener), "focus returned to the element that had it before");
            Assert.IsFalse(trap.IsActive);
            trap.Dispose();
        }

        [UnityTest]
        public IEnumerator Trap_FocusThatLeavesTheContainerComesBack()
        {
            yield return CreatePanel();
            Button outside = new Button { name = "outside" };
            Panel.Root.Add(outside);
            VisualElement dialog = Container(Panel.Root, "dialog");
            yield return FoundationPanel.Frames(2);

            using (new FocusTrap(dialog))
            {
                outside.Focus();
                yield return FoundationPanel.Frames(3);
                Assert.IsTrue(dialog.Contains(Focused(outside)), "focus went back into the container");

                dialog[1].Focus();
                yield return FoundationPanel.Frames(2);
                Assert.AreSame(dialog[1], Focused(outside), "focus moving inside the container is left alone");
            }
        }

        [UnityTest]
        public IEnumerator Trap_WhenTheFocusedControlIsRemoved_FocusStaysInTheContainer()
        {
            yield return CreatePanel();
            VisualElement dialog = Container(Panel.Root, "dialog", 3);
            yield return FoundationPanel.Frames(2);
            using (new FocusTrap(dialog))
            {
                Button first = (Button)dialog[0];
                first.RemoveFromHierarchy();
                yield return Real(0.5f);
                Assert.IsTrue(dialog.Contains(Focused(dialog)), "focus moved to another control of the container");
            }
        }

        [UnityTest]
        public IEnumerator Trap_NestedTrapsInDifferentLayers_OnlyTheNewestHoldsFocus()
        {
            yield return CreatePanel();
            VisualElement screen = Container(Panel.Root, "screen");
            VisualElement sheet = Container(Panel.Root, "sheet");
            VisualElement dialog = Container(Panel.Root, "dialog");
            yield return FoundationPanel.Frames(2);

            FocusTrap screenTrap = new FocusTrap(screen);
            sheet[1].Focus();
            FocusTrap sheetTrap = new FocusTrap(sheet);
            Assert.IsFalse(screenTrap.IsActive);
            Assert.IsTrue(sheetTrap.IsActive);
            FocusTrap dialogTrap = new FocusTrap(dialog);
            Assert.AreSame(dialog[0], Focused(dialog));
            Assert.IsFalse(sheetTrap.IsActive);

            screen[1].Focus();
            yield return FoundationPanel.Frames(3);
            Assert.IsTrue(dialog.Contains(Focused(dialog)), "only the newest trap pulls focus back, the older ones do not fight it");

            dialogTrap.Dispose();
            Assert.AreSame(sheet[0], Focused(dialog), "focus returned to the control the sheet's trap had focused when the dialog opened");
            Assert.IsTrue(sheetTrap.IsActive);

            sheetTrap.Dispose();
            Assert.IsTrue(screenTrap.IsActive);
            screenTrap.Dispose();
        }

        [UnityTest]
        public IEnumerator Trap_WhenTheOpenerIsGone_FocusGoesToTheTrapBelow()
        {
            yield return CreatePanel();
            VisualElement screen = Container(Panel.Root, "screen");
            VisualElement dialog = Container(Panel.Root, "dialog");
            yield return FoundationPanel.Frames(2);

            FocusTrap screenTrap = new FocusTrap(screen);
            Button opener = (Button)screen[1];
            opener.Focus();
            FocusTrap dialogTrap = new FocusTrap(dialog);
            opener.RemoveFromHierarchy();
            dialogTrap.Dispose();
            Assert.IsTrue(screen.Contains(Focused(screen)), "the trap below took the focus back");
            screenTrap.Dispose();
        }

        [UnityTest]
        public IEnumerator Trap_TwoPanelsHaveTheirOwnNewestTrap()
        {
            yield return CreatePanel();
            FoundationPanel other = FoundationPanel.Create();
            try
            {
                yield return FoundationPanel.Frames(2);
                VisualElement a = Container(Panel.Root, "a");
                VisualElement b = Container(other.Root, "b");
                yield return FoundationPanel.Frames(2);
                using (FocusTrap trapA = new FocusTrap(a))
                using (FocusTrap trapB = new FocusTrap(b))
                {
                    Assert.IsTrue(trapA.IsActive, "a trap on another panel does not deactivate this one");
                    Assert.IsTrue(trapB.IsActive);
                }
            }
            finally
            {
                other.Dispose();
            }
        }

        [UnityTest]
        public IEnumerator Trap_AContainerWithoutFocusableControls_DoesNotThrow()
        {
            yield return CreatePanel();
            VisualElement empty = new VisualElement();
            empty.Add(new Label("text"));
            Panel.Root.Add(empty);
            yield return FoundationPanel.Frames(2);
            Assert.DoesNotThrow(() => new FocusTrap(empty).Dispose());
            Assert.Throws<ArgumentNullException>(() => new FocusTrap(null));
        }

        [UnityTest]
        public IEnumerator FirstFocusable_SkipsHiddenDisabledAndUnfocusableElements()
        {
            yield return CreatePanel();
            VisualElement container = new VisualElement();
            Button hidden = new Button { name = "hidden" };
            hidden.style.display = DisplayStyle.None;
            Button disabled = new Button { name = "disabled" };
            disabled.SetEnabled(false);
            Button skipped = new Button { name = "skipped", tabIndex = -1 };
            Label label = new Label("label");
            VisualElement group = new VisualElement();
            Button inner = new Button { name = "inner" };
            group.Add(inner);
            container.Add(hidden);
            container.Add(disabled);
            container.Add(skipped);
            container.Add(label);
            container.Add(group);
            Panel.Root.Add(container);
            yield return FoundationPanel.Frames(2);
            Assert.AreSame(inner, UiFocus.FirstFocusable(container));
            Assert.IsNull(UiFocus.FirstFocusable(null));
            Assert.IsTrue(UiFocus.FocusFirst(container));
            Assert.IsFalse(UiFocus.FocusFirst(new VisualElement()));
        }

        [UnityTest]
        public IEnumerator AbsorbPointer_StopsPointerEventsFromReachingTheAncestors()
        {
            yield return CreatePanel();
            VisualElement parent = new VisualElement();
            VisualElement scrim = new VisualElement();
            scrim.style.width = 100;
            scrim.style.height = 100;
            parent.Add(scrim);
            Panel.Root.Add(parent);
            UiFocus.AbsorbPointer(scrim);
            int reached = 0;
            parent.RegisterCallback<PointerDownEvent>(e => reached++);
            yield return FoundationPanel.Frames(2);
            Panel.PointerDown(scrim);
            Assert.AreEqual(0, reached);
            Assert.AreEqual(PickingMode.Position, scrim.pickingMode);
        }
    }
}
