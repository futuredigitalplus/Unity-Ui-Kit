using FutureDigitalPlus.UIKit;
using FutureDigitalPlus.UIKit.Components;
using NUnit.Framework;
using UnityEngine.UIElements;

// A game's own namespace: the generated code is shortened by the usings of this file, and `using FutureDigitalPlus.UIKit;` (the facade) is the one that used to turn
// the framework's type names into `Components.X`, which do not resolve here (CS0246 in the generated *.Dependencies.g.cs). That this file compiles is the test.
namespace GameLikeNamespace.Tests
{
    public partial class ComponentWithFacadeUsingTests
    {
        private partial class FacadeUsingScreen : UIComponent
        {
            [Query("title")] public Label Title;
        }

        [Test]
        public void AComponentInAFileThatUsesTheFacadeNamespace_Compiles_AndCanUseTheFacade()
        {
            var screen = new FacadeUsingScreen();
            Assert.IsNotNull(screen);
            Assert.AreEqual(UIKitInstaller.MobileClass, "fdp-input-touch");
        }
    }
}
