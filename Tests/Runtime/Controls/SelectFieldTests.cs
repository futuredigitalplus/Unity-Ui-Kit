using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UIElements;

namespace FutureDigitalPlus.UIKit.Controls.Tests
{
    public class SelectFieldTests
    {
        private ControlsPanel _panel;

        [SetUp]
        public void SetUp() => LogAssert.ignoreFailingMessages = true;

        [TearDown]
        public void TearDown()
        {
            LogAssert.ignoreFailingMessages = false;
            _panel?.Dispose();
        }

        private SelectField AddField()
        {
            _panel = ControlsPanel.Create();
            var field = new SelectField { Placeholder = "Pick one", style = { width = 200 } };
            field.Choices = new[] { "Low", "Medium", "High" };
            _panel.Root.Add(field);
            return field;
        }

        private static string LabelText(SelectField field) => field.Q<Label>("label").text;

        [UnityTest]
        public IEnumerator NoValue_ShowsThePlaceholder()
        {
            var field = AddField();
            yield return ControlsPanel.Frames();
            Assert.AreEqual(-1, field.value);
            Assert.AreEqual("Pick one", LabelText(field));
            Assert.IsTrue(field.Q<Label>("label").ClassListContains("fdp-select-field__label--placeholder"));
            Assert.IsNull(field.SelectedText);
        }

        [UnityTest]
        public IEnumerator ArrowsChangeTheValueWithoutOpeningTheList()
        {
            var field = AddField();
            yield return ControlsPanel.Frames();
            var changes = new System.Collections.Generic.List<int>();
            field.RegisterValueChangedCallback(evt => changes.Add(evt.newValue));

            Send.Move(field, NavigationMoveEvent.Direction.Right);
            Assert.AreEqual(0, field.value);
            Send.Move(field, NavigationMoveEvent.Direction.Right);
            Assert.AreEqual("Medium", LabelText(field));
            Send.Move(field, NavigationMoveEvent.Direction.Right);
            Send.Move(field, NavigationMoveEvent.Direction.Right);
            Assert.AreEqual(2, field.value, "clamped at the last entry");
            Send.Move(field, NavigationMoveEvent.Direction.Left);
            Assert.AreEqual(1, field.value);
            CollectionAssert.AreEqual(new[] { 0, 1, 2, 1 }, changes);
            Assert.IsFalse(field.IsOpen);
        }

        [UnityTest]
        public IEnumerator Submit_OpensTheList_WithTheCurrentChoiceFocused_AndChoosing_ChangesTheValue()
        {
            var field = AddField();
            yield return ControlsPanel.Frames();
            field.value = 1;
            Send.Submit(field);
            Assert.IsTrue(field.IsOpen);
            var menu = field.OpenList();
            yield return ControlsPanel.Frames();
            Assert.AreEqual("1", menu.Model.Focused.Id, "the current choice has focus");

            Send.Move(menu.Layer, NavigationMoveEvent.Direction.Down);
            Send.Submit(menu.Layer);
            Assert.IsFalse(field.IsOpen);
            Assert.AreEqual(2, field.value);
            Assert.AreEqual("High", LabelText(field));
        }

        [UnityTest]
        public IEnumerator Cancel_ClosesTheList_WithoutChangingTheValue()
        {
            var field = AddField();
            yield return ControlsPanel.Frames();
            field.value = 0;
            var menu = field.OpenList();
            yield return ControlsPanel.Frames();
            Send.Cancel(menu.Layer);
            Assert.IsFalse(field.IsOpen);
            Assert.AreEqual(0, field.value);
        }

        [UnityTest]
        public IEnumerator EntriesAttribute_ParsesACommaList()
        {
            _panel = ControlsPanel.Create();
            var field = new SelectField { Entries = "One, Two,Three" };
            _panel.Root.Add(field);
            yield return ControlsPanel.Frames();
            Assert.AreEqual(3, field.Choices.Count);
            Assert.AreEqual("Two", field.Choices[1]);
            Assert.AreEqual("One,Two,Three", field.Entries);
        }

        [UnityTest]
        public IEnumerator DisabledField_DoesNotOpen()
        {
            var field = AddField();
            yield return ControlsPanel.Frames();
            field.SetEnabled(false);
            Assert.IsNull(field.OpenList());
            Assert.IsFalse(field.IsOpen);
        }

        [UnityTest]
        public IEnumerator SetValueWithoutNotify_RaisesNothing()
        {
            var field = AddField();
            yield return ControlsPanel.Frames();
            var raised = false;
            field.RegisterValueChangedCallback(_ => raised = true);
            field.SetValueWithoutNotify(2);
            Assert.IsFalse(raised);
            Assert.AreEqual("High", LabelText(field));
        }
    }
}
