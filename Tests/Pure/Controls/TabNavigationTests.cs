using System;
using NUnit.Framework;

namespace FutureDigitalPlus.UIKit.Controls.Tests
{
    /// <summary>The arrow, Home and End rules of TabBar and SegmentedControl, left to right and right to left.</summary>
    public class TabNavigationTests
    {
        private static readonly ItemFlow[] Flows = { ItemFlow.Row, ItemFlow.RowReverse, ItemFlow.Column, ItemFlow.ColumnReverse };

        private static Func<int, bool> Enabled(params bool[] enabled) => i => enabled[i];

        [Test]
        public void LeftToRightRow_RightIsNext_LeftIsPrevious()
        {
            // The behaviour every left-to-right bar had before right to left was mirrored.
            Assert.AreEqual(1, TabNavigation.StepFor(true, ItemFlow.Row, false));
            Assert.AreEqual(-1, TabNavigation.StepFor(false, ItemFlow.Row, false));
        }

        [Test]
        public void MirroredRow_RightIsPrevious_LeftIsNext()
        {
            Assert.AreEqual(-1, TabNavigation.StepFor(true, ItemFlow.RowReverse, true));
            Assert.AreEqual(1, TabNavigation.StepFor(false, ItemFlow.RowReverse, true));
        }

        [Test]
        public void InARow_TheLayoutDecides_NotTheLanguage()
        {
            // A right-to-left bar that is not mirrored on screen (no kit stylesheet) keeps keys that match the picture, and a reversed
            // row in a left-to-right screen too.
            Assert.AreEqual(1, TabNavigation.StepFor(true, ItemFlow.Row, true));
            Assert.AreEqual(-1, TabNavigation.StepFor(true, ItemFlow.RowReverse, false));
        }

        [Test]
        public void InAColumn_TheReadingDirectionDecides()
        {
            foreach (var flow in new[] { ItemFlow.Column, ItemFlow.ColumnReverse })
            {
                Assert.AreEqual(1, TabNavigation.StepFor(true, flow, false), flow.ToString());
                Assert.AreEqual(-1, TabNavigation.StepFor(false, flow, false), flow.ToString());
                Assert.AreEqual(-1, TabNavigation.StepFor(true, flow, true), flow.ToString());
                Assert.AreEqual(1, TabNavigation.StepFor(false, flow, true), flow.ToString());
            }
        }

        [Test]
        public void InEveryRow_RightMovesOneSlotToTheRight_AndLeftOneSlotToTheLeft()
        {
            const int count = 5;
            foreach (var flow in new[] { ItemFlow.Row, ItemFlow.RowReverse })
            foreach (var rtl in new[] { false, true })
            for (var index = 0; index < count; index++)
            {
                var slot = TabNavigation.VisualSlot(index, count, flow);
                var right = index + TabNavigation.StepFor(true, flow, rtl);
                var left = index + TabNavigation.StepFor(false, flow, rtl);
                if (right >= 0 && right < count) Assert.AreEqual(slot + 1, TabNavigation.VisualSlot(right, count, flow), $"{flow} rtl={rtl} index={index} Right");
                if (left >= 0 && left < count) Assert.AreEqual(slot - 1, TabNavigation.VisualSlot(left, count, flow), $"{flow} rtl={rtl} index={index} Left");
            }
        }

        [Test]
        public void VisualSlot_MirroredRowPutsTheFirstItemAtTheRightEdge()
        {
            Assert.AreEqual(0, TabNavigation.VisualSlot(0, 2, ItemFlow.Row), "Classic | Moods: Classic at the left");
            Assert.AreEqual(1, TabNavigation.VisualSlot(0, 2, ItemFlow.RowReverse), "Moods | Classic: Classic at the right");
            Assert.AreEqual(0, TabNavigation.VisualSlot(1, 2, ItemFlow.RowReverse));
            Assert.AreEqual(2, TabNavigation.VisualSlot(2, 5, ItemFlow.RowReverse), "the middle stays");
            Assert.AreEqual(3, TabNavigation.VisualSlot(3, 5, ItemFlow.Column));
        }

        [Test]
        public void StepFor_IsAlwaysOneStep()
        {
            foreach (var flow in Flows)
            foreach (var right in new[] { false, true })
            foreach (var rtl in new[] { false, true })
            {
                var step = TabNavigation.StepFor(right, flow, rtl);
                Assert.That(step == 1 || step == -1, $"{flow} {right} {rtl}");
                Assert.AreEqual(-step, TabNavigation.StepFor(!right, flow, rtl), "Left undoes Right");
            }
        }

        [Test]
        public void Neighbour_SkipsDisabledItems_AndStopsAtTheEnds()
        {
            var enabled = Enabled(true, false, true, true);
            Assert.AreEqual(2, TabNavigation.Neighbour(4, enabled, 0, 1), "the disabled item is skipped");
            Assert.AreEqual(0, TabNavigation.Neighbour(4, enabled, 2, -1));
            Assert.AreEqual(-1, TabNavigation.Neighbour(4, enabled, 3, 1), "no wrap at the end");
            Assert.AreEqual(-1, TabNavigation.Neighbour(4, enabled, 0, -1), "no wrap at the start");
            Assert.AreEqual(3, TabNavigation.Neighbour(4, enabled, 2, 7), "only the sign of the step counts");
        }

        [Test]
        public void Neighbour_OddInput_FindsNothingInsteadOfLooping()
        {
            Assert.AreEqual(-1, TabNavigation.Neighbour(3, Enabled(true, true, true), 1, 0), "step 0");
            Assert.AreEqual(-1, TabNavigation.Neighbour(0, i => true, -1, 1), "no items");
            Assert.AreEqual(-1, TabNavigation.Neighbour(-2, i => true, -1, 1), "a negative count");
            Assert.AreEqual(0, TabNavigation.Neighbour(3, Enabled(true, true, true), -9, 1), "a start before the items is the start");
            Assert.AreEqual(2, TabNavigation.Neighbour(3, Enabled(true, true, true), 99, -1), "a start after the items is the end");
            Assert.Throws<ArgumentNullException>(() => TabNavigation.Neighbour(3, null, 0, 1));
        }

        [Test]
        public void HomeAndEnd_AreTheFirstAndLastEnabledItems_InEveryDirection()
        {
            // Home and End follow the item order (the reading order): in a right-to-left bar Home goes to the right edge, End to the left.
            var enabled = Enabled(false, true, true, false);
            Assert.AreEqual(1, TabNavigation.First(4, enabled));
            Assert.AreEqual(2, TabNavigation.Last(4, enabled));
            Assert.AreEqual(3 - 1, TabNavigation.VisualSlot(TabNavigation.First(4, enabled), 4, ItemFlow.RowReverse), "Home, mirrored: the right-most enabled slot");
            Assert.AreEqual(-1, TabNavigation.First(2, Enabled(false, false)));
            Assert.AreEqual(-1, TabNavigation.Last(2, Enabled(false, false)));
        }
    }
}
