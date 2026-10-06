using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UIElements;

namespace FutureDigitalPlus.UIKit.Foundation.Tests
{
    /// <summary>PlayMode tests of the dialog view: pointer input, focus, the equal-width button rule and the fade-in.</summary>
    public class DialogPlayModeTests : PlayModeBase
    {
        List<Button> Buttons()
        {
            return Layer(UiLayer.Dialogs).Query<Button>().ToList();
        }

        [UnityTest]
        public IEnumerator Confirm_PressingTheConfirmButton_ReportsTrue_AndTheCancelButtonReportsFalse()
        {
            yield return CreatePanel();
            UiShell shell = CreateShell();
            shell.Boot("Home");
            List<bool> answers = new List<bool>();

            shell.Context.Dialogs.Confirm(UiText.FromLiteral("One"), UiText.Empty, answers.Add);
            yield return FoundationPanel.Frames(3);
            Panel.Click(Buttons()[1]);
            yield return FoundationPanel.Frames();

            shell.Context.Dialogs.Confirm(UiText.FromLiteral("Two"), UiText.Empty, answers.Add);
            yield return FoundationPanel.Frames(3);
            Panel.Click(Buttons()[0]);
            yield return FoundationPanel.Frames();

            CollectionAssert.AreEqual(new[] { true, false }, answers);
        }

        [UnityTest]
        public IEnumerator Scrim_ATapOutsideTheCardClosesOnlyADialogThatAllowsIt()
        {
            yield return CreatePanel();
            UiShell shell = CreateShell();
            shell.Boot("Home");
            List<DialogCloseReason> reasons = new List<DialogCloseReason>();

            DialogRequest sticky = new DialogRequest { Title = UiText.FromLiteral("Sticky"), Callback = result => reasons.Add(result.Reason) };
            sticky.AddButton(new DialogButton(UiText.FromLiteral("Close"), DialogButtonRole.Cancel));
            shell.Context.Dialogs.Show(sticky);
            yield return FoundationPanel.Frames(3);
            VisualElement scrim = Layer(UiLayer.Dialogs).Q<VisualElement>(UiNames.DialogScrim);
            Panel.PointerDown(scrim);
            Assert.IsTrue(shell.Context.Dialogs.IsModalOpen, "this dialog does not dismiss on the scrim");

            shell.Context.Dialogs.CloseAll();
            DialogRequest dismissible = new DialogRequest { Title = UiText.FromLiteral("Loose"), DismissOnScrim = true, Callback = result => reasons.Add(result.Reason) };
            dismissible.AddButton(new DialogButton(UiText.FromLiteral("Close"), DialogButtonRole.Cancel));
            shell.Context.Dialogs.Show(dismissible);
            yield return FoundationPanel.Frames(3);
            scrim = Layer(UiLayer.Dialogs).Q<VisualElement>(UiNames.DialogScrim);
            Panel.PointerDown(Layer(UiLayer.Dialogs).Q<VisualElement>(UiNames.DialogPanel));
            Assert.IsTrue(shell.Context.Dialogs.IsModalOpen, "a press inside the card is not a scrim tap");
            Panel.PointerDown(scrim);
            Assert.IsFalse(shell.Context.Dialogs.IsModalOpen);
            CollectionAssert.AreEqual(new[] { DialogCloseReason.Cleared, DialogCloseReason.Scrim }, reasons);
        }

        [UnityTest]
        public IEnumerator Focus_MovesIntoTheDialog_AndReturnsToTheOpenerWhenItCloses()
        {
            yield return CreatePanel();
            UiShell shell = CreateShell();
            shell.Boot("Home");
            yield return FoundationPanel.Frames(3);
            Button opener = ((ProbeScreen)shell.Router.CurrentScreen).Button;
            opener.Focus();
            yield return null;
            Assert.AreSame(opener, Panel.Panel.focusController.focusedElement);

            shell.Context.Dialogs.Confirm(UiText.FromLiteral("Sure?"), UiText.Empty, null);
            yield return FoundationPanel.Frames(3);
            VisualElement focused = Panel.Panel.focusController.focusedElement as VisualElement;
            Assert.IsNotNull(focused);
            Assert.IsTrue(Layer(UiLayer.Dialogs).Contains(focused), "keyboard and gamepad focus is inside the dialog");

            shell.Context.Dialogs.HandleBack();
            yield return FoundationPanel.Frames(3);
            Assert.AreSame(opener, Panel.Panel.focusController.focusedElement, "focus goes back to where it was");
        }

        [UnityTest]
        public IEnumerator FadeIn_TheScrimBecomesVisibleOnTheNextTick()
        {
            yield return CreatePanel();
            UiShell shell = CreateShell();
            shell.Boot("Home");
            shell.Context.Dialogs.Alert(UiText.FromLiteral("Hi"), UiText.Empty);
            VisualElement scrim = Layer(UiLayer.Dialogs).Q<VisualElement>(UiNames.DialogScrim);
            Assert.IsFalse(scrim.ClassListContains(UiNames.ClassVisible), "starts transparent so the fade runs");
            yield return Real(0.1f);
            Assert.IsTrue(scrim.ClassListContains(UiNames.ClassVisible));
        }

        [UnityTest]
        public IEnumerator Layout_TheCardStaysInsideAPortraitPhoneWithAWideTranslation()
        {
            yield return CreatePanel(390, 780);
            UiShell shell = CreateShell();
            shell.Boot("Home");
            string longText = "A very long sentence that a translator produced and that must wrap inside the card instead of running off the screen, even on a narrow phone.";
            shell.Context.Dialogs.Confirm(UiText.FromLiteral("Title"), UiText.FromLiteral(longText), null, UiText.FromLiteral("Yes, please do continue"), UiText.FromLiteral("No, go back"));
            yield return Real(0.4f);
            VisualElement card = Layer(UiLayer.Dialogs).Q<VisualElement>(UiNames.DialogPanel);
            Rect bounds = card.worldBound;
            Assert.GreaterOrEqual(bounds.xMin, 0f);
            Assert.LessOrEqual(bounds.xMax, 390f + 0.5f);
            Assert.GreaterOrEqual(bounds.yMin, 0f);
            Assert.LessOrEqual(bounds.yMax, 780f + 0.5f);
            foreach (Button button in Buttons())
            {
                Rect b = button.worldBound;
                Assert.GreaterOrEqual(b.xMin, bounds.xMin - 0.5f, "a button never leaves the card");
                Assert.LessOrEqual(b.xMax, bounds.xMax + 0.5f);
            }
        }

        [UnityTest]
        public IEnumerator Layout_InLandscapeTheButtonsSitSideBySide_AndInPortraitTheyStack()
        {
            yield return CreatePanel(800, 400);
            UiShell shell = CreateShell();
            shell.Boot("Home");
            shell.Context.Dialogs.Confirm(UiText.FromLiteral("T"), UiText.Empty, null);
            yield return Real(0.4f);
            List<Button> landscape = Buttons();
            Assert.AreEqual(FlexDirection.Row, landscape[0].parent.resolvedStyle.flexDirection);
            Assert.AreEqual(landscape[0].worldBound.center.y, landscape[1].worldBound.center.y, 1f, "same line");

            shell.Dispose();
            Shell = null;
            Panel.Dispose();
            Panel = null;
            yield return CreatePanel(390, 780);
            shell = CreateShell();
            shell.Boot("Home");
            shell.Context.Dialogs.Confirm(UiText.FromLiteral("T"), UiText.Empty, null);
            yield return Real(0.4f);
            List<Button> portrait = Buttons();
            Assert.AreEqual(FlexDirection.Column, portrait[0].parent.resolvedStyle.flexDirection);
            Assert.Greater(portrait[1].worldBound.center.y, portrait[0].worldBound.center.y + 10f, "stacked");
        }

        [UnityTest]
        public IEnumerator Input_TheValueOfThePromptIsReturnedWhenItIsConfirmed()
        {
            yield return CreatePanel();
            UiShell shell = CreateShell();
            shell.Boot("Home");
            string result = "unset";
            shell.Context.Dialogs.Input(UiText.FromLiteral("Name"), value => result = value, "Ada");
            yield return FoundationPanel.Frames(3);
            TextField field = Layer(UiLayer.Dialogs).Q<TextField>();
            field.value = "Grace";
            List<Button> buttons = Buttons();
            Panel.Click(buttons[1]);
            yield return FoundationPanel.Frames();
            Assert.AreEqual("Grace", result);
        }
    }
}
