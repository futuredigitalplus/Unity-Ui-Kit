// FutureDigitalPlus: renamed by Tools~/rename/rename.py (namespaces, resource paths); see SOURCE.md "Rename record".
using System.Collections;
using NUnit.Framework;
using FutureDigitalPlus.UIKit.Components;
using FutureDigitalPlus.UIKit.Components.Testing;
using UnityEngine.TestTools;
using UnityEngine.UIElements;

namespace FutureDigitalPlus.UIKit.Tests.ComponentsModule
{
    /// <summary>
    /// New smoke test for the Components module (upstream's own tests live in Tests/Runtime/Components).
    /// It proves the bundled Roslyn generators run on a component in a consuming assembly: [Layout] loads the
    /// UXML, [Query] fills the fields, and TestBed can instantiate it without a panel.
    /// </summary>
    public partial class ComponentsSmokeTests
    {
        [Layout("FutureDigitalPlus/UIKit/Tests/ComponentsSmoke")]
        private partial class Smoke : UIComponent
        {
            [Query("title")] public Label Title;
            [Query("ok")] public Button Ok;
            [Query(Name = "row")] public VisualElement Row;
            [Query(Class = "headline")] public VisualElement[] Headlines;
        }

        // FutureDigitalPlus.UIKit.Components' own Task-to-IEnumerator helper is internal to the package, so polling is done here.
        private static IEnumerator WaitFor(System.Threading.Tasks.Task task)
        {
            while (!task.IsCompleted) yield return null;
            if (task.IsFaulted) throw task.Exception;
        }

        [UnityTest]
        public IEnumerator LayoutAndQuery_AreGenerated_AndPopulateFields()
        {
            var component = new TestBed<Smoke>().Instantiate();
            yield return WaitFor(component.Initialize());

            Assert.IsTrue(component.Initialized);
            Assert.AreEqual("Hello", component.Title.text);
            Assert.AreEqual("OK", component.Ok.text);
            Assert.IsNotNull(component.Row);
            Assert.AreEqual(2, component.Headlines.Length, "the label and the OK button carry the class");
        }
    }
}
