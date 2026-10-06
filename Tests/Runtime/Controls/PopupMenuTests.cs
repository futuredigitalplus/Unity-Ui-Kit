using System.Collections;
using System.Collections.Generic;
using FutureDigitalPlus.UIKit.Foundation;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UIElements;

namespace FutureDigitalPlus.UIKit.Controls.Tests
{
    public class PopupMenuTests
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
        }

        private IEnumerator Prepare()
        {
            _panel = ControlsPanel.Create();
            _anchor = new Button { text = "Anchor", focusable = true, style = { width = 120, height = 40 } };
            _panel.Root.Add(_anchor);
            yield return ControlsPanel.Frames();
            _anchor.Focus();
        }

        private static List<MenuEntry> Items() => new List<MenuEntry>
        {
            new MenuEntry("open", "Open"),
            new MenuEntry("disabled", "Disabled") { Enabled = false },
            new MenuEntry("delete", "Delete") { Hint = "Del" },
        };

        private static List<VisualElement> Panels(PopupMenu menu) => menu.Layer.Query<VisualElement>(className: "fdp-menu").ToList();

        private static List<VisualElement> Rows(VisualElement panel) => panel.Query<VisualElement>(className: "fdp-menu__item").ToList();

        [UnityTest]
        public IEnumerator Show_PlacesTheMenuAtThePointer_AndFocusesTheFirstRow()
        {
            yield return Prepare();
            var menu = PopupMenu.Show(_anchor, Items(), new Vector2(100f, 120f));
            yield return ControlsPanel.Frames();
            Assert.IsTrue(menu.IsOpen);
            var panel = Panels(menu)[0];
            Assert.AreEqual(100f, panel.worldBound.x, 1.5f);
            Assert.AreEqual(120f, panel.worldBound.y, 1.5f);
            Assert.AreEqual(Visibility.Visible, panel.resolvedStyle.visibility);
            var rows = Rows(panel);
            Assert.AreEqual(3, rows.Count);
            Assert.IsTrue(rows[0].ClassListContains("fdp-menu__item--active"));
            Assert.AreSame(rows[0], _anchor.panel.focusController.focusedElement);
            Assert.IsFalse(rows[1].focusable, "a disabled row cannot take focus");
        }

        [UnityTest]
        public IEnumerator Show_NearTheCornerFlipsToStayOnScreen()
        {
            yield return Prepare();
            var menu = PopupMenu.Show(_anchor, Items(), new Vector2(ControlsPanel.Width - 5f, ControlsPanel.Height - 5f));
            yield return ControlsPanel.Frames();
            var rect = Panels(menu)[0].worldBound;
            Assert.LessOrEqual(rect.xMax, ControlsPanel.Width + 0.5f);
            Assert.LessOrEqual(rect.yMax, ControlsPanel.Height + 0.5f);
            Assert.Less(rect.x, ControlsPanel.Width - 5f, "flipped to the left of the pointer");
            Assert.Less(rect.y, ControlsPanel.Height - 5f, "flipped above the pointer");
        }

        [UnityTest]
        public IEnumerator ArrowsMoveFocus_SkippingDisabledRows_AndSubmitInvokes()
        {
            yield return Prepare();
            MenuEntry invoked = null;
            var closedCount = 0;
            var menu = PopupMenu.Show(_anchor, Items(), new Vector2(50f, 50f), item => invoked = item);
            menu.Closed += () => closedCount++;
            yield return ControlsPanel.Frames();

            Send.Move(menu.Layer, NavigationMoveEvent.Direction.Down);
            Assert.AreEqual("delete", menu.Model.Focused.Id);
            var rows = Rows(Panels(menu)[0]);
            Assert.IsTrue(rows[2].ClassListContains("fdp-menu__item--active"));
            Assert.IsFalse(rows[0].ClassListContains("fdp-menu__item--active"));

            Send.Submit(menu.Layer);
            Assert.IsNotNull(invoked);
            Assert.AreEqual("delete", invoked.Id);
            Assert.IsFalse(menu.IsOpen);
            Assert.AreEqual(1, closedCount);
            Assert.IsNull(menu.Layer);
        }

        [UnityTest]
        public IEnumerator Cancel_Closes_AndFocusReturnsToTheAnchor()
        {
            yield return Prepare();
            var menu = PopupMenu.Show(_anchor, Items(), new Vector2(50f, 50f));
            yield return ControlsPanel.Frames();
            Assert.AreNotSame(_anchor, _anchor.panel.focusController.focusedElement);
            Send.Cancel(menu.Layer);
            Assert.IsFalse(menu.IsOpen);
            Assert.AreSame(_anchor, _anchor.panel.focusController.focusedElement);
        }

        [UnityTest]
        public IEnumerator Escape_AndCancelInTheSameFrame_CloseOnlyOneLevel()
        {
            yield return Prepare();
            var parent = new MenuEntry("more", "More");
            parent.Add(new MenuEntry("a", "A"));
            var menu = PopupMenu.Show(_anchor, new List<MenuEntry> { parent, new MenuEntry("b", "B") }, new Vector2(50f, 50f));
            yield return ControlsPanel.Frames();
            Send.Move(menu.Layer, NavigationMoveEvent.Direction.Right); // open the submenu
            yield return ControlsPanel.Frames();
            Assert.AreEqual(2, Panels(menu).Count);

            Send.Key(menu.Layer, KeyCode.Escape);
            Send.Cancel(menu.Layer); // the same physical key press raises both
            yield return ControlsPanel.Frames();
            Assert.IsTrue(menu.IsOpen, "only the submenu closed");
            Assert.AreEqual(1, Panels(menu).Count);

            Send.Cancel(menu.Layer);
            Assert.IsFalse(menu.IsOpen);
        }

        [UnityTest]
        public IEnumerator PointerDownOutsideTheMenu_Closes()
        {
            yield return Prepare();
            var menu = PopupMenu.Show(_anchor, Items(), new Vector2(50f, 50f));
            yield return ControlsPanel.Frames();
            var layer = menu.Layer;
            Send.PointerDown(layer, new Vector2(700f, 500f));
            Assert.IsFalse(menu.IsOpen);
        }

        [UnityTest]
        public IEnumerator ClickingARow_Invokes()
        {
            yield return Prepare();
            MenuEntry invoked = null;
            var menu = PopupMenu.Show(_anchor, Items(), new Vector2(50f, 50f), item => invoked = item);
            yield return ControlsPanel.Frames();
            var row = Rows(Panels(menu)[0])[0];
            using (var e = ClickEvent.GetPooled())
            {
                e.target = row;
                row.SendEvent(e);
            }

            Assert.AreEqual("open", invoked?.Id);
            Assert.IsFalse(menu.IsOpen);
        }

        [UnityTest]
        public IEnumerator Submenu_OpensToTheRight_AndLeftClosesIt()
        {
            yield return Prepare();
            var parent = new MenuEntry("more", "More");
            parent.Add(new MenuEntry("a", "A"));
            parent.Add(new MenuEntry("b", "B"));
            var menu = PopupMenu.Show(_anchor, new List<MenuEntry> { parent }, new Vector2(100f, 100f));
            yield return ControlsPanel.Frames();
            Send.Move(menu.Layer, NavigationMoveEvent.Direction.Right);
            yield return ControlsPanel.Frames();
            var panels = Panels(menu);
            Assert.AreEqual(2, panels.Count);
            Assert.GreaterOrEqual(panels[1].worldBound.x, panels[0].worldBound.xMax - 6f, "the submenu sits beside its parent (it starts at the parent row, inside the padding and border of the parent)");
            Assert.AreEqual("a", menu.Model.Focused.Id);

            Send.Move(menu.Layer, NavigationMoveEvent.Direction.Left);
            yield return ControlsPanel.Frames();
            Assert.AreEqual(1, Panels(menu).Count);
            Assert.IsTrue(menu.IsOpen);
        }

        [UnityTest]
        public IEnumerator CheckedItems_ShowTheCheckMark()
        {
            yield return Prepare();
            var items = new List<MenuEntry> { new MenuEntry("a", "A") { Checked = true }, new MenuEntry("b", "B") };
            var menu = PopupMenu.Show(_anchor, items, new Vector2(50f, 50f));
            yield return ControlsPanel.Frames();
            var rows = Rows(Panels(menu)[0]);
            Assert.IsTrue(rows[0].Q<VisualElement>(null, "fdp-menu__check").ClassListContains("fdp-menu__check--on"));
            Assert.IsFalse(rows[1].Q<VisualElement>(null, "fdp-menu__check").ClassListContains("fdp-menu__check--on"));
        }

        [UnityTest]
        public IEnumerator ShowBelow_PlacesTheMenuUnderTheAnchor_AsWideAsTheAnchor()
        {
            yield return Prepare();
            _anchor.style.marginLeft = 60f;   // the placement keeps 8 px from the edge of the panel, so the anchor must not sit at the edge
            yield return ControlsPanel.Frames(2);
            var menu = PopupMenu.ShowBelow(_anchor, Items(), null, matchAnchorWidth: true);
            yield return ControlsPanel.Frames(4);
            var rect = Panels(menu)[0].worldBound;
            Assert.AreEqual(_anchor.worldBound.x, rect.x, 1.5f);
            Assert.GreaterOrEqual(rect.y, _anchor.worldBound.yMax - 1.5f);
            Assert.GreaterOrEqual(rect.width, _anchor.worldBound.width - 1.5f);
        }

        [UnityTest]
        public IEnumerator Attach_LongPressWithAFingerOnAButton_Opens()
        {
            yield return Prepare();
            _anchor.style.marginLeft = 200f;   // away from the origin, where the panel believes the mouse is
            _anchor.style.marginTop = 200f;
            yield return ControlsPanel.Frames(2);
            var binding = PopupMenu.Attach(_anchor, Items, null);
            binding.LongPressSeconds = 0.05f;
            Send.TouchDown(_anchor, _anchor.worldBound.center);
            Assert.IsNull(binding.Current, "not yet");
            yield return ControlsPanel.WaitSeconds(0.3f);
            Assert.IsNotNull(binding.Current, "a Button's own click handling does not swallow the press");
            Assert.IsTrue(binding.Current.IsOpen);
            Send.TouchUp(_anchor, _anchor.worldBound.center);
            binding.Dispose();
        }

        [UnityTest]
        public IEnumerator Attach_OpensOnRightClick_AndDisposeStops()
        {
            yield return Prepare();
            var binding = PopupMenu.Attach(_anchor, Items, null);
            Send.PointerDown(_anchor, _anchor.worldBound.center, button: 1);
            Assert.IsNotNull(binding.Current);
            Assert.IsTrue(binding.Current.IsOpen);
            var menu = binding.Current;
            binding.Dispose();
            Assert.IsFalse(menu.IsOpen, "disposing closes the menu");
            Send.PointerDown(_anchor, _anchor.worldBound.center, button: 1);
            Assert.IsNull(binding.Current);
        }

        [UnityTest]
        public IEnumerator Attach_GamepadButtonOpensBelow()
        {
            yield return Prepare();
            var binding = PopupMenu.Attach(_anchor, Items, null);
            Send.Key(_anchor, KeyCode.JoystickButton3);
            Assert.IsNotNull(binding.Current);
            Assert.IsTrue(binding.Current.IsOpen);
            binding.Dispose();
        }

        [UnityTest]
        public IEnumerator Show_WithoutAPanel_ReturnsNull()
        {
            yield return null;
            Assert.IsNull(PopupMenu.Show(new VisualElement(), Items(), Vector2.zero));
        }

        [UnityTest]
        public IEnumerator OpenedFromADialogWithAFocusTrap_KeepsFocus_AndHandsItBackOnClose()
        {
            yield return Prepare();
            var dialog = new VisualElement { name = "dialog" };
            var inDialog = new Button { text = "Choose", focusable = true };
            dialog.Add(inDialog);
            _panel.Root.Add(dialog);
            var dialogTrap = new FocusTrap(dialog);
            yield return ControlsPanel.Frames();
            Assert.AreSame(inDialog, inDialog.panel.focusController.focusedElement);

            var menu = PopupMenu.ShowBelow(inDialog, Items());
            yield return ControlsPanel.WaitSeconds(0.6f); // longer than the trap's recovery interval
            var focused = menu.Layer.panel.focusController.focusedElement as VisualElement;
            Assert.IsNotNull(focused);
            Assert.IsTrue(menu.Layer.Contains(focused), "the dialog's trap does not take focus back while the menu is open");

            menu.Close();
            Assert.AreSame(inDialog, inDialog.panel.focusController.focusedElement, "focus returns into the dialog");
            dialogTrap.Dispose();
        }
    }
}
