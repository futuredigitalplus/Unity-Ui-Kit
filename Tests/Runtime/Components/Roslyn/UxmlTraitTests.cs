// FutureDigitalPlus: renamed by Tools~/rename/rename.py (namespaces, resource paths); see SOURCE.md "Rename record".
using NUnit.Framework;
using UnityEngine;
using UnityEngine.UIElements;

namespace FutureDigitalPlus.UIKit.Components.Tests.Roslyn
{
    [TestFixture]
    public class UxmlTraitTests
    {
        [Test]
        public void Generates_Traits_For_Non_UIComponent_Class()
        {
            // FutureDigitalPlus: test UXML moved to Resources/FutureDigitalPlus/UIKit/Tests/Components/ so the Resources path cannot collide with game assets.
            var layout = Resources.Load<VisualTreeAsset>("FutureDigitalPlus/UIKit/Tests/Components/RoslynTestVisualElement");

            var container = new VisualElement();
            layout.CloneTree(container);

            var valuesSetElement = container.Q<RoslynTestVisualElement>("values-set");
            var valuesNotSetElement = container.Q<RoslynTestVisualElement>("values-not-set");

            Assert.That(valuesSetElement.Description, Is.EqualTo("None set"));
            Assert.That(valuesSetElement.Age, Is.EqualTo(20));
            Assert.That(valuesSetElement.MusicVolume, Is.EqualTo(0.5f));
            Assert.That(valuesNotSetElement.MusicVolume, Is.EqualTo(1.0f));
            Assert.That(valuesSetElement.UnixTimestamp, Is.EqualTo(1234567890));
        }

        [Test]
        public void Generates_Traits_For_UIComponent_Class()
        {
            // FutureDigitalPlus: test UXML moved to Resources/FutureDigitalPlus/UIKit/Tests/Components/ so the Resources path cannot collide with game assets.
            var layout = Resources.Load<VisualTreeAsset>("FutureDigitalPlus/UIKit/Tests/Components/RoslynTestComponent");
            var container = new VisualElement();
            layout.CloneTree(container);

            var valuesSetComponent = container.Q<RoslynTestComponent>("values-set");
            var valuesNotSetComponent = container.Q<RoslynTestComponent>("values-not-set");
            
            Assert.That(valuesSetComponent.TextColor, Is.EqualTo(Color.red));
            Assert.That(valuesNotSetComponent.TextColor, Is.EqualTo(Color.cyan));
            Assert.That(valuesSetComponent.Greeting, Is.EqualTo(RoslynTestComponent.Greetings.Morning));
            Assert.That(valuesSetComponent.CurrentTime, Is.EqualTo(123.456));
            Assert.That(valuesSetComponent.Enabled, Is.EqualTo(false));
        }

        [Test]
        public void Generates_Traits_For_Nested_Class()
        {
            // FutureDigitalPlus: test UXML moved to Resources/FutureDigitalPlus/UIKit/Tests/Components/ so the Resources path cannot collide with game assets.
            var layout = Resources.Load<VisualTreeAsset>("FutureDigitalPlus/UIKit/Tests/Components/NestedRoslynTestComponent");
            var container = new VisualElement();
            layout.CloneTree(container);

            var component = container.Q<NestParentClass.NestedRoslynTestComponent>();
            
            Assert.That(component.Trait, Is.EqualTo("Hello world"));
        }
    }
}
