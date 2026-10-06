using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UIElements;

namespace FutureDigitalPlus.UIKit.Controls.Tests
{
    public class BottomSheetTests
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

        private IEnumerator Prepare(bool reduceMotion = false)
        {
            _panel = ControlsPanel.Create(reduceMotion);
            _anchor = new Button { text = "Open", focusable = true };
            _panel.Root.Add(_anchor);
            yield return ControlsPanel.Frames();
            _anchor.Focus();
        }

        private static BottomSheet MakeSheet()
        {
            var sheet = new BottomSheet();
            var button = new Button { text = "Inside", focusable = true, name = "inside", style = { height = 120 } };
            sheet.Add(button);
            return sheet;
        }

        private static VisualElement Grab(BottomSheet sheet) => sheet.Q<VisualElement>("grab");

        [UnityTest]
        public IEnumerator Open_SlidesUp_ToTheLastSnapPoint_AndFocusesTheContent()
        {
            yield return Prepare();
            var sheet = MakeSheet();
            Assert.IsTrue(sheet.Open(_anchor));
            Assert.IsTrue(sheet.IsOpen);
            Assert.IsFalse(sheet.Open(_anchor), "already open");
            yield return ControlsPanel.WaitSeconds(0.6f);
            Assert.AreEqual(1f, sheet.VisibleFraction, 0.001f);
            Assert.AreSame(sheet.Q<Button>("inside"), sheet.panel.focusController.focusedElement);
            Assert.IsTrue(sheet.ClassListContains("ds-sheet"), "carries the design system sheet skin");
        }

        [UnityTest]
        public IEnumerator Open_AtAnEarlierSnapPoint_RestsThere_AndArrowsMoveBetweenSnapPoints()
        {
            yield return Prepare();
            var sheet = MakeSheet();
            sheet.SetSnapPoints(0.5f, 1f);
            var snaps = new System.Collections.Generic.List<int>();
            sheet.SnapChanged += snaps.Add;
            sheet.Open(_anchor, 0);
            yield return ControlsPanel.WaitSeconds(0.6f);
            Assert.AreEqual(0.5f, sheet.VisibleFraction, 0.001f);
            Assert.AreEqual(0, sheet.SnapIndex);

            Send.Move(Grab(sheet), NavigationMoveEvent.Direction.Up);
            Assert.AreEqual(1, sheet.SnapIndex);
            Assert.AreEqual(1f, sheet.VisibleFraction, 0.001f);
            Send.Move(Grab(sheet), NavigationMoveEvent.Direction.Down);
            Assert.AreEqual(0, sheet.SnapIndex);
            CollectionAssert.AreEqual(new[] { 1, 0 }, snaps);

            Send.Move(Grab(sheet), NavigationMoveEvent.Direction.Down); // below the lowest snap point: closes
            Assert.IsFalse(sheet.IsOpen);
        }

        [UnityTest]
        public IEnumerator Cancel_Closes_RaisesClosed_AndRestoresFocus()
        {
            yield return Prepare();
            var sheet = MakeSheet();
            var closed = 0;
            sheet.Closed += () => closed++;
            sheet.Open(_anchor);
            yield return ControlsPanel.WaitSeconds(0.5f);
            Send.Cancel(sheet);
            Assert.IsFalse(sheet.IsOpen);
            yield return ControlsPanel.WaitSeconds(0.5f);
            Assert.AreEqual(1, closed);
            Assert.IsNull(sheet.parent, "removed from the panel");
            Assert.AreSame(_anchor, _anchor.panel.focusController.focusedElement);
        }

        [UnityTest]
        public IEnumerator NotDismissable_IgnoresCancelAndTheScrim_ButCloseWorks()
        {
            yield return Prepare();
            var sheet = MakeSheet();
            sheet.Dismissable = false;
            sheet.Open(_anchor);
            yield return ControlsPanel.WaitSeconds(0.5f);
            Send.Cancel(sheet);
            Assert.IsTrue(sheet.IsOpen);
            var scrim = sheet.parent.Q<VisualElement>("scrim");
            Send.PointerDown(scrim, new Vector2(10f, 10f));
            Assert.IsTrue(sheet.IsOpen);
            Assert.IsTrue(sheet.HandleBack(), "back is consumed so nothing below reacts");
            sheet.Close();
            Assert.IsFalse(sheet.IsOpen);
        }

        [UnityTest]
        public IEnumerator ScrimTap_Closes()
        {
            yield return Prepare();
            var sheet = MakeSheet();
            sheet.Open(_anchor);
            yield return ControlsPanel.WaitSeconds(0.5f);
            Send.PointerDown(sheet.parent.Q<VisualElement>("scrim"), new Vector2(10f, 10f));
            Assert.IsFalse(sheet.IsOpen);
        }

        [UnityTest]
        public IEnumerator DraggingTheHandleDown_Dismisses()
        {
            yield return Prepare();
            var sheet = MakeSheet();
            sheet.Open(_anchor);
            yield return ControlsPanel.WaitSeconds(0.5f);
            var grab = Grab(sheet);
            var start = grab.worldBound.center;
            Send.PointerDown(grab, start);
            Send.PointerMove(grab, start + new Vector2(0f, sheet.resolvedStyle.height * 0.9f));
            Assert.Less(sheet.VisibleFraction, 0.2f, "follows the pointer");
            Send.PointerUp(grab, start + new Vector2(0f, sheet.resolvedStyle.height * 0.9f));
            Assert.IsFalse(sheet.IsOpen);
        }

        [UnityTest]
        public IEnumerator ShortDrag_SnapsBack()
        {
            yield return Prepare();
            var sheet = MakeSheet();
            sheet.Open(_anchor);
            yield return ControlsPanel.WaitSeconds(0.5f);
            var grab = Grab(sheet);
            var start = grab.worldBound.center;
            Send.PointerDown(grab, start);
            yield return ControlsPanel.WaitSeconds(0.3f);
            Send.PointerMove(grab, start + new Vector2(0f, 8f));
            yield return ControlsPanel.WaitSeconds(0.3f); // slow drag: no flick velocity (a frame takes a millisecond when nothing is drawn)
            Send.PointerUp(grab, start + new Vector2(0f, 8f));
            Assert.IsTrue(sheet.IsOpen);
            Assert.AreEqual(1f, sheet.VisibleFraction, 0.001f);
        }

        [UnityTest]
        public IEnumerator ReducedMotion_OpensAndClosesAtOnce()
        {
            yield return Prepare(reduceMotion: true);
            var sheet = MakeSheet();
            sheet.Open(_anchor);
            yield return ControlsPanel.WaitSeconds(0.4f);
            Assert.IsTrue(sheet.ClassListContains("fdp-bottom-sheet--instant"));
            Assert.AreEqual(1f, sheet.VisibleFraction, 0.001f);
            sheet.Close();
            Assert.IsNull(sheet.parent, "no slide: gone in the same call");
        }

        [Test]
        public void SetSnapPoints_ValidatesItsInput()
        {
            var sheet = new BottomSheet();
            Assert.Throws<System.ArgumentException>(() => sheet.SetSnapPoints());
            Assert.Throws<System.ArgumentException>(() => sheet.SetSnapPoints(0f, 1f));
            Assert.Throws<System.ArgumentException>(() => sheet.SetSnapPoints(0.5f, 1.5f));
            Assert.Throws<System.ArgumentException>(() => sheet.SetSnapPoints(0.8f, 0.4f));
            sheet.SetSnapPoints(0.4f, 0.8f);
            Assert.AreEqual(2, sheet.SnapPoints.Count);
        }

        [Test]
        public void Open_WithAnElementOutsideAPanel_ReturnsFalse()
        {
            var sheet = new BottomSheet();
            Assert.IsFalse(sheet.Open(new VisualElement()));
            Assert.IsFalse(sheet.IsOpen);
        }
    }
}
