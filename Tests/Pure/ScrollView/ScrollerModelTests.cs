using System;
using System.Collections.Generic;
using NUnit.Framework;

namespace FutureDigitalPlus.UIKit.ScrollView.Tests
{
    public class ScrollerModelTests
    {
        private const float Frame = 1f / 60f;

        private static ScrollerModel Make(int count = 10, MovementType movement = MovementType.Elastic, bool snap = true)
        {
            var m = new ScrollerModel { ViewportSize = 500f, MovementType = movement, SnapEnabled = snap };
            m.SetTotalCount(count);
            return m;
        }

        private static void Run(ScrollerModel m, float seconds)
        {
            for (var t = 0f; t < seconds; t += Frame) m.Tick(Frame);
        }

        [Test]
        public void Position_Set_RaisesValueChanged_AndStopsMotion()
        {
            var m = Make();
            var values = new List<float>();
            m.ValueChanged += values.Add;
            m.ScrollTo(5f, 1f);
            m.Position = 2f;
            Assert.AreEqual(new[] { 2f }, values.ToArray());
            Assert.IsFalse(m.IsAutoScrolling);
            Run(m, 0.5f);
            Assert.AreEqual(2f, m.Position, "nothing moves an item-aligned resting position");
        }

        [Test]
        public void ScrollTo_ReachesTheTarget_InTheDuration_AndCallsBack()
        {
            var m = Make();
            var selections = new List<int>();
            m.SelectionChanged += selections.Add;
            var done = 0;
            m.ScrollTo(4f, 0.5f, Ease.Linear, () => done++);
            Assert.AreEqual(new[] { 4 }, selections.ToArray(), "selection is announced at the start");

            m.Tick(0.25f);
            Assert.AreEqual(2f, m.Position, 1e-4f, "half way with a linear ease");
            Assert.AreEqual(0, done);

            m.Tick(0.25f);
            Assert.AreEqual(4f, m.Position, 1e-4f);
            Assert.AreEqual(1, done);
            Assert.IsFalse(m.IsAutoScrolling);
        }

        [Test]
        public void ScrollTo_ZeroDuration_Jumps()
        {
            var m = Make();
            var done = false;
            m.ScrollTo(3f, 0f, () => done = true);
            Assert.AreEqual(3f, m.Position);
            Assert.IsTrue(done);
        }

        [Test]
        public void ScrollTo_ClampsToTheEnds_WhenNotUnrestricted()
        {
            var m = Make(5);
            m.ScrollTo(99f, 0.1f, Ease.Linear);
            Run(m, 0.2f);
            Assert.AreEqual(4f, m.Position, 1e-4f);
        }

        [Test]
        public void ScrollTo_Unrestricted_TakesTheShorterWayAround()
        {
            var m = Make(10, MovementType.Unrestricted);
            m.ScrollTo(9f, 0.1f, Ease.Linear);
            Run(m, 0.2f);
            Assert.AreEqual(-1f, m.Position, 1e-4f, "from 0 back one step to item 9");
        }

        [Test]
        public void JumpTo_SetsPositionAndSelection_AndRejectsOutOfRange()
        {
            var m = Make(5);
            var selected = -1;
            m.SelectionChanged += i => selected = i;
            m.JumpTo(3);
            Assert.AreEqual(3f, m.Position);
            Assert.AreEqual(3, selected);
            Assert.Throws<ArgumentOutOfRangeException>(() => m.JumpTo(5));
            Assert.Throws<ArgumentOutOfRangeException>(() => m.JumpTo(-1));
        }

        [Test]
        public void Drag_AcrossTheViewport_MovesBySensitivity()
        {
            var m = Make();
            m.ScrollSensitivity = 2f;
            m.PointerDown();
            m.BeginDrag();
            m.Drag(250f);
            Assert.AreEqual(1f, m.Position, 1e-5f, "half the viewport at sensitivity 2 is one item");
            m.Drag(-250f);
            Assert.Less(m.Position, -0f, "pulled before the start");
            Assert.Greater(m.Position, -1f, "the rubber band gives less than the pull");
        }

        [Test]
        public void Elastic_ReturnsToTheStart_AfterTheDrag()
        {
            var m = Make();
            var selections = new List<int>();
            m.SelectionChanged += selections.Add;
            m.PointerDown();
            m.BeginDrag();
            m.Drag(-200f);
            m.PointerUp();
            m.EndDrag();
            Run(m, 2f);
            Assert.AreEqual(0f, m.Position, 1e-4f);
            Assert.Contains(0, selections);
        }

        [Test]
        public void Clamped_Wheel_StopsAtTheEnd()
        {
            var m = Make(3, MovementType.Clamped);
            m.Scroll(10000f);
            Assert.AreEqual(2f, m.Position, 1e-5f);
            m.Scroll(-10000f);
            Assert.AreEqual(0f, m.Position, 1e-5f);
        }

        [Test]
        public void Tap_WithSnap_MovesToTheNearestItem()
        {
            var m = Make();
            m.Position = 2.4f;
            m.PointerDown();
            m.PointerUp();
            Assert.IsTrue(m.IsAutoScrolling);
            Run(m, 1f);
            Assert.AreEqual(2f, m.Position, 1e-4f);
        }

        [Test]
        public void Fling_WithInertiaAndSnap_ComesToRestOnAnItem()
        {
            var m = Make(50);
            m.PointerDown();
            m.BeginDrag();
            for (var i = 1; i <= 10; i++)
            {
                m.Drag(i * 40f);
                m.Tick(Frame);
            }

            m.PointerUp();
            m.EndDrag();
            var released = m.Position;
            Assert.Greater(m.Velocity, 0f, "the drag left a velocity");
            Run(m, 5f);
            Assert.Greater(m.Position, released, "inertia carried it on");
            Assert.AreEqual(Math.Round(m.Position), m.Position, 1e-4f, "rests on an item");
            Assert.IsFalse(m.IsAutoScrolling);
        }

        [Test]
        public void Drag_ReleasedWithoutVelocity_StillSnaps()
        {
            var m = Make();
            m.PointerDown();
            m.BeginDrag();
            m.Drag(130f); // 0.26 items, all within one frame: no tick measured a velocity
            m.PointerUp();
            m.EndDrag();
            Run(m, 1f);
            Assert.AreEqual(0f, m.Position, 1e-4f, "back to the nearest item, not left between two");

            m.Inertia = false;
            m.PointerDown();
            m.BeginDrag();
            for (var i = 1; i <= 5; i++)
            {
                m.Drag(i * 80f);
                m.Tick(Frame);
            }

            m.PointerUp();
            m.EndDrag();
            Run(m, 1f);
            Assert.AreEqual(1f, m.Position, 1e-4f, "without inertia the release snaps too (0.8 items rounds to 1)");
        }

        [Test]
        public void NotDraggable_IgnoresPointerAndWheel()
        {
            var m = Make();
            m.Draggable = false;
            m.PointerDown();
            m.BeginDrag();
            m.Drag(400f);
            m.Scroll(400f);
            Assert.AreEqual(0f, m.Position);
            Assert.IsFalse(m.IsDragging);
        }

        [Test]
        public void NoViewportSize_IgnoresDragAndWheel()
        {
            var m = Make();
            m.ViewportSize = 0f;
            m.BeginDrag();
            m.Drag(100f);
            m.Scroll(100f);
            Assert.AreEqual(0f, m.Position);
        }

        [Test]
        public void Tick_AtRest_RaisesNothing()
        {
            var m = Make();
            m.Position = 3f;
            var events = 0;
            m.ValueChanged += _ => events++;
            m.SelectionChanged += _ => events++;
            Run(m, 1f);
            Assert.AreEqual(0, events, "an idle scroller is silent (Approximately(0, 0) must hold)");
            Assert.IsTrue(ScrollMath.Approximately(0f, 0f));
        }

        [Test]
        public void CancelPress_KeepsAnAnimationStartedDuringThePress()
        {
            var m = Make();
            m.Position = 0.2f;
            m.PointerDown();
            m.ScrollTo(4f, 0.2f, Ease.Linear);
            m.CancelPress();
            Run(m, 0.5f);
            Assert.AreEqual(4f, m.Position, 1e-4f, "no snap back to item 0");
        }

        [Test]
        public void Tick_WithoutTime_DoesNothing()
        {
            var m = Make();
            m.ScrollTo(3f, 0.5f);
            m.Tick(0f);
            m.Tick(-1f);
            m.Tick(float.NaN);
            Assert.AreEqual(0f, m.Position);
        }

        [Test]
        public void NormalizedPosition_RoundTrips_ThroughTheScrollbar()
        {
            var m = Make(11);
            m.SetNormalizedPosition(0.5f);
            Assert.AreEqual(5f, m.Position, 1e-5f);
            Assert.AreEqual(0.5f, m.NormalizedPosition, 1e-5f);
            m.Position = -3f;
            Assert.AreEqual(0f, m.NormalizedPosition);
        }

        [Test]
        public void GetMovementDirection_FollowsTheAxis()
        {
            var m = Make();
            Assert.AreEqual(MovementDirection.Up, m.GetMovementDirection(0, 3));
            Assert.AreEqual(MovementDirection.Down, m.GetMovementDirection(3, 0));
            m.ScrollDirection = ScrollDirection.Horizontal;
            Assert.AreEqual(MovementDirection.Left, m.GetMovementDirection(0, 3));
            Assert.AreEqual(MovementDirection.Right, m.GetMovementDirection(3, 0));
        }
    }
}
