using System.Reflection;
using FutureDigitalPlus.UIKit.DesignSystem.Typography;
using NUnit.Framework;
using UnityEngine.UIElements;

namespace FutureDigitalPlus.UIKit.Tests.DesignSystemModule
{
    /// <summary>
    /// Static state must be reset when Unity enters Play Mode without reloading the domain. The reset methods are private and run by Unity, so the tests call them
    /// the way Unity does.
    /// </summary>
    public class AdversarialStaticsTests
    {
        private static void RunReset(System.Type type)
        {
            var method = type.GetMethod("ResetStatics", BindingFlags.NonPublic | BindingFlags.Static);
            Assert.IsNotNull(method, type.Name + " has a ResetStatics method");
            method.Invoke(null, null);
        }

        [Test]
        public void DsFonts_ResetDropsTheSubscribersOfTheLastPlaySession()
        {
            var calls = 0;
            DsFonts.FamilyChanged += (root, family) => calls++;

            RunReset(typeof(DsFonts));
            DsFonts.Apply(new VisualElement(), null);

            Assert.AreEqual(0, calls, "a subscriber of the last session (a destroyed component) must not be called");
            Assert.AreEqual(0, DsFonts.GlobalFallbacks.Count);
        }

        [Test]
        public void DsGoogleFonts_ResetForgetsFontsThatAreInFlightFailedOrLoaded()
        {
            var inFlight = typeof(DsGoogleFonts).GetField("InFlight", BindingFlags.NonPublic | BindingFlags.Static);
            var failed = typeof(DsGoogleFonts).GetField("Failed", BindingFlags.NonPublic | BindingFlags.Static);
            ((System.Collections.Generic.HashSet<string>)inFlight.GetValue(null)).Add("ofl/test");
            ((System.Collections.Generic.Dictionary<string, string>)failed.GetValue(null))["ofl/test"] = "offline";
            Assert.IsTrue(DsGoogleFonts.FailedFor("ofl/test", out _));

            RunReset(typeof(DsGoogleFonts));

            Assert.IsFalse(DsGoogleFonts.FailedFor("ofl/test", out _));
            Assert.AreEqual(0, ((System.Collections.Generic.HashSet<string>)inFlight.GetValue(null)).Count);
            Assert.IsFalse(DsGoogleFonts.Has("ofl/test"));
        }
    }
}
