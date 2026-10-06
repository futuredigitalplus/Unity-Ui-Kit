using System;
using System.Collections.Generic;
using FutureDigitalPlus.UIKit.Animation;
using NUnit.Framework;

namespace FutureDigitalPlus.UIKit.Tests.AnimationModule
{
    /// <summary>New smoke tests for the easing curves (pure math, no panel needed).</summary>
    public class EasyTests
    {
        private static IEnumerable<TestCaseData> AllCurves()
        {
            foreach (Easing e in Enum.GetValues(typeof(Easing)))
            {
                // InPower / OutPower / InOutPower need an extra 'power' argument and are not reachable through GetEasing.
                if (e == Easing.InPower || e == Easing.OutPower || e == Easing.InOutPower) continue;
                yield return new TestCaseData(e).SetName($"Curve_{e}_StartsAtZeroAndEndsAtOne");
            }
        }

        [TestCaseSource(nameof(AllCurves))]
        public void Curve_StartsAtZeroAndEndsAtOne(Easing easing)
        {
            var curve = GetEasing.Get(easing);
            Assert.AreEqual(0f, curve(0f), 1e-3f, "f(0)");
            Assert.AreEqual(1f, curve(1f), 1e-3f, "f(1)");
        }

        [Test]
        public void Linear_IsIdentity() => Assert.AreEqual(0.37f, Easy.Linear(0.37f), 1e-6f);

        [Test]
        public void Quad_KnownMidpoints()
        {
            Assert.AreEqual(0.25f, Easy.InQuad(0.5f), 1e-6f);
            Assert.AreEqual(0.75f, Easy.OutQuad(0.5f), 1e-6f);
            Assert.AreEqual(0.5f, Easy.InOutQuad(0.5f), 1e-6f);
        }

        [Test]
        public void Step_SwitchesAtHalf()
        {
            Assert.AreEqual(0f, Easy.Step(0.49f));
            Assert.AreEqual(1f, Easy.Step(0.5f));
        }

        [Test]
        public void PowerCurves_MatchCubicHelpers()
        {
            Assert.AreEqual(Easy.InCubic(0.3f), Easy.InPower(0.3f, 3), 1e-6f);
            Assert.AreEqual(Easy.OutCubic(0.3f), Easy.OutPower(0.3f, 3), 1e-6f);
            Assert.AreEqual(Easy.InOutCubic(0.3f), Easy.InOutPower(0.3f, 3), 1e-6f);
        }
    }
}
