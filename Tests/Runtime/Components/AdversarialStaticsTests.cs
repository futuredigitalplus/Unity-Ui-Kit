using System.Reflection;
using FutureDigitalPlus.UIKit.Components.DependencyInjection;
using NUnit.Framework;

namespace FutureDigitalPlus.UIKit.Components.Tests
{
    /// <summary>The dependency injection context is static: a play session that does not reload the domain must not inherit the last one's singletons.</summary>
    public class AdversarialStaticsTests
    {
        [Test]
        public void ResetStatics_PutsBackTheStaticContextAndForgetsItsSingletons()
        {
            var custom = new DiContext();
            DiContext.ChangeCurrent(custom);
            DiContext.Static.SingletonInstanceDictionary[typeof(string)] = "left over";

            typeof(DiContext).GetMethod("ResetStatics", BindingFlags.NonPublic | BindingFlags.Static).Invoke(null, null);

            Assert.AreSame(DiContext.Static, DiContext.Current);
            Assert.AreEqual(0, DiContext.Static.GetSingletonInstances().Count);
        }
    }
}
