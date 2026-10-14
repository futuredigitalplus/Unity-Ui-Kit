using System;
using NUnit.Framework;

namespace FutureDigitalPlus.UIKit.ScrollView.Tests
{
    public class EaseAndMathTests
    {
        [Test]
        public void EveryEase_StartsAtZero_AndEndsAtOne()
        {
            foreach (Ease ease in Enum.GetValues(typeof(Ease)))
            {
                var f = EaseFunctions.Get(ease);
                Assert.AreEqual(0f, f(0f), 1e-4f, ease + " at 0");
                Assert.AreEqual(1f, f(1f), 1e-4f, ease + " at 1");
            }
        }

        [Test]
        public void Ease_KnownValues()
        {
            Assert.AreEqual(0.875f, EaseFunctions.Get(Ease.OutCubic)(0.5f), 1e-5f);
            Assert.AreEqual(0.5f, EaseFunctions.Get(Ease.InOutCubic)(0.5f), 1e-5f);
            Assert.AreEqual(0.25f, EaseFunctions.Get(Ease.InQuad)(0.5f), 1e-5f);
            Assert.AreEqual(0.3f, EaseFunctions.Get(Ease.Linear)(0.3f), 1e-6f);
        }

        [Test]
        public void Ease_UndefinedValue_IsLinear_AndFunctionsAreCached()
        {
            Assert.AreEqual(0.3f, EaseFunctions.Get((Ease)999)(0.3f), 1e-6f);
            Assert.AreSame(EaseFunctions.Get(Ease.OutBack), EaseFunctions.Get(Ease.OutBack));
        }

        [Test]
        public void CircularIndex_WrapsBothWays()
        {
            Assert.AreEqual(0, ScrollMath.CircularIndex(5, 5));
            Assert.AreEqual(4, ScrollMath.CircularIndex(-1, 5));
            Assert.AreEqual(3, ScrollMath.CircularIndex(-7, 5));
            Assert.AreEqual(2, ScrollMath.CircularIndex(12, 5));
            Assert.AreEqual(0, ScrollMath.CircularIndex(3, 0), "no items");
        }

        [Test]
        public void CircularPosition_WrapsBothWays()
        {
            Assert.AreEqual(9.5f, ScrollMath.CircularPosition(-0.5f, 10), 1e-5f);
            Assert.AreEqual(0.25f, ScrollMath.CircularPosition(10.25f, 10), 1e-5f);
            Assert.AreEqual(0f, ScrollMath.CircularPosition(3f, 0));
        }

        [Test]
        public void RoundToInt_RoundsHalvesToEven_LikeMathf()
        {
            Assert.AreEqual(2, ScrollMath.RoundToInt(2.5f));
            Assert.AreEqual(4, ScrollMath.RoundToInt(3.5f));
            Assert.AreEqual(-2, ScrollMath.RoundToInt(-2.5f));
        }

        [Test]
        public void Clamp_WithMinAboveMax_BehavesLikeMathf()
        {
            Assert.AreEqual(0, ScrollMath.Clamp(-3, 0, -1));
            Assert.AreEqual(-1, ScrollMath.Clamp(0, 0, -1));
        }

        [Test]
        public void SmoothDamp_ReachesTheTarget_WithoutOvershoot()
        {
            var v = 0f;
            var x = 0f;
            for (var i = 0; i < 120; i++)
            {
                x = ScrollMath.SmoothDamp(x, 1f, ref v, 0.1f, float.PositiveInfinity, 1f / 60f);
                Assert.LessOrEqual(x, 1f + 1e-6f);
            }

            Assert.AreEqual(1f, x, 1e-3f);
        }

        [Test]
        public void RubberDelta_GrowsSlowerThanTheStretch()
        {
            var d = ScrollMath.RubberDelta(2f, 10f);
            Assert.Greater(d, 0f);
            Assert.Less(d, 2f);
            Assert.AreEqual(-d, ScrollMath.RubberDelta(-2f, 10f), 1e-6f);
            Assert.AreEqual(0f, ScrollMath.RubberDelta(0f, 10f));
        }
    }
}
