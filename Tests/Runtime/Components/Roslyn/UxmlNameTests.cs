// FutureDigitalPlus: renamed by Tools~/rename/rename.py (namespaces, resource paths); see SOURCE.md "Rename record".
using NUnit.Framework;
using UnityEngine;
using UnityEngine.UIElements;

namespace FutureDigitalPlus.UIKit.Components.Tests.Roslyn
{
    [TestFixture]
    public class UxmlNameTests
    {
        [Test]
        public void Generates_Uxml_Factory_For_Name()
        {
            // FutureDigitalPlus: test UXML moved to Resources/FutureDigitalPlus/UIKit/Tests/Components/ so the Resources path cannot collide with game assets.
            var layout = Resources.Load<VisualTreeAsset>("FutureDigitalPlus/UIKit/Tests/Components/UxmlNameTestComponent");

            var container = new VisualElement();
            layout.CloneTree(container);

            var testElement = container.Q<UxmlNameTestComponent>();

            Assert.That(testElement, Is.Not.Null);
        }
    }
}
