using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.UIElements;

namespace FutureDigitalPlus.UIKit.Foundation.Tests
{
    /// <summary>EditMode tests of the dialog, toast and loading services and of the text source, on a detached element (no panel, no timers).</summary>
    public class ServicesLogicTests
    {
        UiShell _shell;

        [SetUp]
        public void SetUp()
        {
            _shell = new UiShell(new VisualElement(), new UiShellOptions { TransitionsEnabled = false });
        }

        [TearDown]
        public void TearDown()
        {
            _shell.Dispose();
            TestEnvironment.ResetStatics();
        }

        VisualElement DialogsLayer
        {
            get { return _shell.Context.GetLayer(UiLayer.Dialogs); }
        }

        // ---- dialogs ----

        [Test]
        public void Confirm_BuildsTheFrameOnTheDesignSystemClasses_AndBlocksInput()
        {
            _shell.Context.Dialogs.Confirm(UiText.FromLiteral("Restart?"), UiText.FromLiteral("Progress is lost."), null);

            Assert.IsTrue(_shell.Context.Dialogs.IsModalOpen);
            Assert.IsTrue(_shell.Context.Input.IsBlocked);
            VisualElement scrim = DialogsLayer.Q<VisualElement>(UiNames.DialogScrim);
            Assert.IsNotNull(scrim);
            Assert.IsTrue(scrim.ClassListContains("ds-backdrop"));
            VisualElement card = scrim.Q<VisualElement>(UiNames.DialogPanel);
            Assert.IsTrue(card.ClassListContains("ds-dialog"));
            Assert.IsTrue(card.ClassListContains(UiNames.ClassDialog));
            Assert.AreEqual("Restart?", scrim.Q<Label>(UiNames.DialogTitle).text);
            Assert.AreEqual("Progress is lost.", scrim.Q<Label>(UiNames.DialogMessage).text);
            Assert.AreEqual(PickingMode.Position, scrim.pickingMode, "the scrim absorbs input");
            Assert.IsNotNull(scrim.Q<SafeAreaPanel>(UiNames.DialogSafe));
        }

        [Test]
        public void Confirm_HasACancelAndAConfirmButton_WithTheRightRoles()
        {
            _shell.Context.Dialogs.Confirm(UiText.FromLiteral("Delete?"), UiText.Empty, null, destructive: true);

            List<Button> buttons = DialogsLayer.Query<Button>().ToList();
            Assert.AreEqual(2, buttons.Count);
            Assert.AreEqual("Cancel", buttons[0].text);
            Assert.IsTrue(buttons[0].ClassListContains("ds-btn--secondary"));
            Assert.AreEqual("OK", buttons[1].text);
            Assert.IsTrue(buttons[1].ClassListContains("ds-btn--danger"));
        }

        [Test]
        public void Confirm_ReportsFalseOnBack_AndClosesTheDialog()
        {
            bool? answer = null;
            _shell.Context.Dialogs.Confirm(UiText.FromLiteral("Sure?"), UiText.Empty, result => answer = result);
            Assert.IsTrue(_shell.Context.Dialogs.HandleBack());
            Assert.AreEqual(false, answer);
            Assert.IsFalse(_shell.Context.Dialogs.IsModalOpen);
            Assert.IsFalse(_shell.Context.Input.IsBlocked);
            Assert.IsNull(DialogsLayer.Q<VisualElement>(UiNames.DialogScrim), "the frame is removed when the dialog closes");
        }

        [Test]
        public void Alert_RunsItsCallbackHoweverItCloses()
        {
            int closed = 0;
            _shell.Context.Dialogs.Alert(UiText.FromLiteral("Saved"), UiText.Empty, () => closed++);
            Assert.AreEqual(1, DialogsLayer.Query<Button>().ToList().Count);
            _shell.Context.Dialogs.HandleBack();
            Assert.AreEqual(1, closed);
        }

        [Test]
        public void Choice_ReportsTheChosenIndex_AndMinusOneWhenCancelled()
        {
            List<int> answers = new List<int>();
            UiText[] choices = { UiText.FromLiteral("Easy"), UiText.FromLiteral("Hard") };
            _shell.Context.Dialogs.Choice(UiText.FromLiteral("Level"), UiText.Empty, choices, answers.Add);
            Assert.AreEqual(3, DialogsLayer.Query<Button>().ToList().Count, "two choices and a cancel button");
            _shell.Context.Dialogs.HandleBack();
            CollectionAssert.AreEqual(new[] { -1 }, answers);
        }

        [Test]
        public void Choice_WithoutCancel_HasOnlyTheChoices_AndBackIsSwallowed()
        {
            List<int> answers = new List<int>();
            UiText[] choices = { UiText.FromLiteral("A"), UiText.FromLiteral("B") };
            _shell.Context.Dialogs.Choice(UiText.FromLiteral("Pick"), UiText.Empty, choices, answers.Add, allowCancel: false);
            Assert.AreEqual(2, DialogsLayer.Query<Button>().ToList().Count);
            Assert.IsTrue(_shell.Context.Dialogs.HandleBack(), "back is consumed while the dialog is up");
            Assert.IsTrue(_shell.Context.Dialogs.IsModalOpen, "but it does not close a dialog that forbids it");
            Assert.AreEqual(0, answers.Count);
        }

        [Test]
        public void Input_ShowsATextField_AndReportsNullWhenCancelled()
        {
            List<string> answers = new List<string>();
            _shell.Context.Dialogs.Input(UiText.FromLiteral("Name"), answers.Add, "Ada");
            TextField field = DialogsLayer.Q<TextField>();
            Assert.IsNotNull(field);
            Assert.AreEqual("Ada", field.value);
            Assert.IsTrue(DialogsLayer.Q<VisualElement>(UiNames.DialogPanel).ClassListContains("ds-modal"), "custom content uses the modal skin");
            _shell.Context.Dialogs.HandleBack();
            Assert.AreEqual(1, answers.Count);
            Assert.IsNull(answers[0]);
        }

        [Test]
        public void DedupeKey_ReturnsTheExistingHandle_SoADoubleTapDoesNotStackTwoDialogs()
        {
            DialogHandle first = _shell.Context.Dialogs.Confirm(UiText.FromLiteral("Restart?"), UiText.Empty, null, dedupeKey: "restart");
            DialogHandle second = _shell.Context.Dialogs.Confirm(UiText.FromLiteral("Restart?"), UiText.Empty, null, dedupeKey: "restart");
            Assert.AreSame(first, second);
            Assert.AreEqual(1, DialogsLayer.Query<VisualElement>(UiNames.DialogScrim).ToList().Count);
        }

        [Test]
        public void ADialogWaiting_OpensWhenTheOneOnScreenCloses_InPriorityOrder()
        {
            List<string> closed = new List<string>();
            _shell.Context.Dialogs.Alert(UiText.FromLiteral("first"), UiText.Empty, () => closed.Add("first"));
            DialogRequest normal = new DialogRequest { Title = UiText.FromLiteral("normal"), Priority = DialogPriority.Normal, Callback = r => closed.Add("normal") };
            normal.AddButton(new DialogButton(UiText.FromLiteral("x"), DialogButtonRole.Cancel));
            DialogRequest system = new DialogRequest { Title = UiText.FromLiteral("system"), Priority = DialogPriority.System, Callback = r => closed.Add("system") };
            system.AddButton(new DialogButton(UiText.FromLiteral("x"), DialogButtonRole.Cancel));
            _shell.Context.Dialogs.Show(normal);
            _shell.Context.Dialogs.Show(system);

            Assert.AreEqual("first", DialogsLayer.Q<Label>(UiNames.DialogTitle).text);
            _shell.Context.Dialogs.HandleBack();
            Assert.AreEqual("system", DialogsLayer.Q<Label>(UiNames.DialogTitle).text, "the higher priority goes first");
            Assert.IsTrue(_shell.Context.Dialogs.IsModalOpen, "no gap between two queued dialogs");
            _shell.Context.Dialogs.HandleBack();
            _shell.Context.Dialogs.HandleBack();
            CollectionAssert.AreEqual(new[] { "first", "system", "normal" }, closed);
            Assert.IsFalse(_shell.Context.Dialogs.IsModalOpen);
        }

        [Test]
        public void ADialogTemplateInTheAssetSource_ReplacesTheCodeBuiltFrame()
        {
            // An empty template has no dialog-scrim element, so the service falls back to the built-in frame instead of showing nothing.
            VisualTreeAsset empty = ScriptableObject.CreateInstance<VisualTreeAsset>();
            ScreenDefinition definition = ScreenDefinition.Create(UiNames.TemplateDialogFrame, empty);
            UiShell shell = new UiShell(new VisualElement(), new UiShellOptions { Assets = new ScreenDefinitionAssetSource(new[] { definition }) });
            try
            {
                shell.Context.Dialogs.Alert(UiText.FromLiteral("x"), UiText.Empty);
                Assert.IsNotNull(shell.Context.GetLayer(UiLayer.Dialogs).Q<VisualElement>(UiNames.DialogScrim));
                Assert.IsNotNull(shell.Context.GetLayer(UiLayer.Dialogs).Q<Button>());
            }
            finally
            {
                shell.Dispose();
                Object.DestroyImmediate(definition);
                Object.DestroyImmediate(empty);
            }
        }

        // ---- toasts ----

        [Test]
        public void Toast_ShowsTheTextWithTheKindClass()
        {
            _shell.Context.Toasts.ShowRaw("Saved", ToastKind.Success);
            VisualElement toasts = _shell.Context.GetLayer(UiLayer.Toasts);
            VisualElement toast = toasts.Q<VisualElement>(UiNames.ToastRoot);
            Assert.IsNotNull(toast);
            Assert.IsTrue(toast.ClassListContains("ds-toast"));
            Assert.IsTrue(toast.ClassListContains("ds-toast--success"));
            Assert.AreEqual("Saved", toast.Q<Label>(UiNames.ToastLabel).text);
            Assert.AreEqual(PickingMode.Ignore, toast.pickingMode, "a toast never takes pointer input");
            Assert.IsFalse(toast.focusable, "and never takes focus");
        }

        [TestCase(ToastKind.Info, "ds-toast--info")]
        [TestCase(ToastKind.Success, "ds-toast--success")]
        [TestCase(ToastKind.Warning, "ds-toast--warning")]
        [TestCase(ToastKind.Danger, "ds-toast--danger")]
        public void KindClass_MapsEveryKindToTheDesignSystemClass(ToastKind kind, string expected)
        {
            Assert.AreEqual(expected, ToastService.KindClass(kind));
        }

        [Test]
        public void Toast_ClearRemovesTheVisibleToast()
        {
            _shell.Context.Toasts.ShowRaw("Hello");
            _shell.Context.Toasts.Clear();
            Assert.IsNull(_shell.Context.GetLayer(UiLayer.Toasts).Q<VisualElement>(UiNames.ToastRoot));
        }

        [Test]
        public void Toast_AnIdenticalRepeatIsCoalescedIntoTheVisibleOne()
        {
            _shell.Context.Toasts.ShowRaw("Same");
            _shell.Context.Toasts.ShowRaw("Same");
            Assert.AreEqual(1, _shell.Context.GetLayer(UiLayer.Toasts).Query<VisualElement>(UiNames.ToastRoot).ToList().Count);
        }

        [Test]
        public void ToastPlacementTop_AddsTheClassToTheToastLayerPanel()
        {
            UiShell top = new UiShell(new VisualElement(), new UiShellOptions { ToastPlacement = ToastPlacement.Top });
            try
            {
                Assert.IsNotNull(top.Context.GetLayer(UiLayer.Toasts).Q<VisualElement>(className: UiNames.ClassToastsTop));
                Assert.IsNull(_shell.Context.GetLayer(UiLayer.Toasts).Q<VisualElement>(className: UiNames.ClassToastsTop));
            }
            finally
            {
                top.Dispose();
            }
        }

        // ---- loading ----

        [Test]
        public void Loading_BlocksInputFromTheMomentItBegins_AndReleasesWhenTheLastTokenEnds()
        {
            ILoadingOverlay loading = _shell.Context.Loading;
            LoadingToken first = loading.Begin(UiText.FromLiteral("A"));
            LoadingToken second = loading.Begin(UiText.FromLiteral("B"));
            Assert.IsTrue(loading.BlocksInput);
            Assert.IsTrue(_shell.Context.Input.IsBlocked, "before the overlay is even visible");
            first.Dispose();
            Assert.IsTrue(_shell.Context.Input.IsBlocked, "one token is still alive");
            second.Dispose();
            second.Dispose();
            Assert.IsFalse(_shell.Context.Input.IsBlocked);
        }

        [Test]
        public void Loading_WithBlockInputOff_NeverBlocks()
        {
            using (_shell.Context.Loading.Begin(UiText.FromLiteral("quiet"), false))
            {
                Assert.IsFalse(_shell.Context.Input.IsBlocked);
                Assert.IsFalse(_shell.Context.Loading.BlocksInput);
            }
        }

        [Test]
        public void Loading_TheBackdropOnlyAbsorbsPointerInputWhileItBlocks()
        {
            VisualElement backdrop = _shell.Context.GetLayer(UiLayer.Loading).Q<VisualElement>(UiNames.LoadingBackdrop);
            Assert.AreEqual(PickingMode.Ignore, backdrop.pickingMode);
            LoadingToken token = _shell.Context.Loading.Begin(UiText.Empty);
            Assert.AreEqual(PickingMode.Position, backdrop.pickingMode);
            token.Dispose();
            Assert.AreEqual(PickingMode.Ignore, backdrop.pickingMode);
        }

        [Test]
        public void Loading_ProgressAndMessageReachTheViews()
        {
            VisualElement backdrop = _shell.Context.GetLayer(UiLayer.Loading).Q<VisualElement>(UiNames.LoadingBackdrop);
            ILoadingOverlay loading = _shell.Context.Loading;
            LoadingToken token = loading.Begin(UiText.FromLiteral("Loading"));
            Assert.AreEqual("Loading", backdrop.Q<Label>(UiNames.LoadingMessage).text);
            Assert.AreEqual(DisplayStyle.None, backdrop.Q<VisualElement>(UiNames.LoadingProgress).style.display.value, "no determinate progress yet");
            loading.SetProgress(token, 0.25f);
            Assert.AreEqual(DisplayStyle.Flex, backdrop.Q<VisualElement>(UiNames.LoadingProgress).style.display.value);
            Assert.AreEqual(25f, backdrop.Q<VisualElement>(UiNames.LoadingProgressFill).style.width.value.value, 0.01f);
            loading.Update(token, UiText.FromLiteral("Almost"), false);
            Assert.AreEqual("Almost", backdrop.Q<Label>(UiNames.LoadingMessage).text);
            Assert.AreEqual(DisplayStyle.None, backdrop.Q<VisualElement>(UiNames.LoadingSpinner).style.display.value);
            token.Dispose();
        }

        // ---- text source ----

        [Test]
        public void TextSource_WithoutAResolver_ShowsLiteralsFallbacksAndPlaceholders_AndNeverThrows()
        {
            UiTextSource source = new UiTextSource();
            Assert.AreEqual("Hello", source.Resolve(UiText.FromLiteral("Hello")));
            Assert.AreEqual("Play", source.Resolve(UiText.FromKey("HOME-PLAY").WithFallback("Play")));
            Assert.AreEqual("[HOME-PLAY]", source.Resolve(UiText.FromKey("HOME-PLAY")));
            Assert.AreEqual(string.Empty, source.Resolve(UiText.Empty));
            Assert.AreEqual(string.Empty, source.Resolve(null));
        }

        [Test]
        public void TextSource_UsesTheGivenResolver_AndSurvivesOneThatThrows()
        {
            UiTextSource source = new UiTextSource(text => "<" + text.Key + ">");
            Assert.AreEqual("<K>", source.Resolve(UiText.FromKey("K")));
            UiTextSource broken = new UiTextSource(text => { throw new System.InvalidOperationException("nope"); });
            UnityEngine.TestTools.LogAssert.Expect(LogType.Warning, new System.Text.RegularExpressions.Regex("text resolver threw"));
            Assert.AreEqual("fb", broken.Resolve(UiText.FromKey("K").WithFallback("fb")));
        }

        [Test]
        public void TextSource_BindSetsTheTextNow_AndASecondBindReplacesTheFirst()
        {
            UiTextSource source = new UiTextSource();
            Label label = new Label();
            source.Bind(label, UiText.FromLiteral("one"));
            Assert.AreEqual("one", label.text);
            source.Bind(label, UiText.FromLiteral("two"));
            Assert.AreEqual("two", label.text);
            source.NotifyChanged();
            Assert.AreEqual("two", label.text);
        }

        [Test]
        public void Context_SetTextAndResolve_GoThroughTheTextSource()
        {
            UiShell shell = new UiShell(new VisualElement(), new UiShellOptions { Text = new UiTextSource(text => text.Literal.ToUpperInvariant()) });
            try
            {
                Label label = new Label();
                shell.Context.SetText(label, UiText.FromLiteral("abc"));
                Assert.AreEqual("ABC", label.text);
                Assert.AreEqual("XYZ", shell.Context.Resolve(UiText.FromLiteral("xyz")));
            }
            finally
            {
                shell.Dispose();
            }
        }
    }
}
