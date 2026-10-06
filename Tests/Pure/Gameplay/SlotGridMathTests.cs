using NUnit.Framework;

namespace FutureDigitalPlus.UIKit.Gameplay.Tests
{
    public class SlotGridMathTests
    {
        [Test]
        public void Move_StaysInsideTheGrid_AndReportsEdges()
        {
            // 3 columns, 7 slots: rows 0 1 2 / 3 4 5 / 6
            Assert.AreEqual(1, SlotGridMath.Move(0, SlotDirection.Right, 3, 7));
            Assert.AreEqual(2, SlotGridMath.Move(2, SlotDirection.Right, 3, 7), "right edge");
            Assert.AreEqual(0, SlotGridMath.Move(0, SlotDirection.Left, 3, 7), "left edge");
            Assert.AreEqual(3, SlotGridMath.Move(0, SlotDirection.Down, 3, 7));
            Assert.AreEqual(0, SlotGridMath.Move(0, SlotDirection.Up, 3, 7), "top edge");
            Assert.AreEqual(6, SlotGridMath.Move(6, SlotDirection.Down, 3, 7), "bottom edge");
            Assert.AreEqual(6, SlotGridMath.Move(6, SlotDirection.Right, 3, 7), "short last row");
        }

        [Test]
        public void Move_DownOntoAShortLastRow_GoesToTheLastSlot()
        {
            // 3 columns, 8 slots: rows 0 1 2 / 3 4 5 / 6 7
            Assert.AreEqual(7, SlotGridMath.Move(5, SlotDirection.Down, 3, 8));
            Assert.AreEqual(7, SlotGridMath.Move(4, SlotDirection.Down, 3, 8));
            Assert.AreEqual(6, SlotGridMath.Move(5, SlotDirection.Down, 3, 7));
        }

        [Test]
        public void Move_InAnEmptyGrid_ReturnsMinusOne()
        {
            Assert.AreEqual(-1, SlotGridMath.Move(0, SlotDirection.Down, 3, 0));
        }
    }
}
