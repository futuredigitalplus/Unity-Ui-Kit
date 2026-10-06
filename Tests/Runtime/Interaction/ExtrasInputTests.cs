using System;
using System.Collections;
using System.Collections.Generic;
using FutureDigitalPlus.UIKit.Extras;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UIElements;

namespace FutureDigitalPlus.UIKit.Interaction.Tests
{
    /// <summary>The input changes in the Extras module: tooltip triggers, popup focus and back, colour picker and 2D slider access from keyboard and gamepad.</summary>
    public class ExtrasInputTests
    {
        private InteractionPanel _panel;

        [SetUp]
        public void SetUp()
        {
            LogAssert.ignoreFailingMessages = true;
            PopupPanel.BackRegistrar = null;
        }

        [TearDown]
        public void TearDown()
        {
            PopupPanel.BackRegistrar = null;
            LogAssert.ignoreFailingMessages = false;
            _panel?.Dispose();
            _panel = null;
        }

#pragma warning disable CS0618 // UxmlFactory / IUxmlAttributes are deprecated in Unity 6 but still the upstream API
        private sealed class Bag : IUxmlAttributes
        {
            private readonly Dictionary<string, string> _values = new Dictionary<string, string>();
            public Bag(params string[] keyValues)
            {
                for (var i = 0; i + 1 < keyValues.Length; i += 2) _values[keyValues[i]] = keyValues[i + 1];
            }
            public bool TryGetAttributeValue(string attributeName, out string value) => _values.TryGetValue(attributeName, out value);
        }

        private static Tooltip NewTooltip() => (Tooltip)new Tooltip.UxmlFactory().Create(new Bag("fade-time", "0"), CreationContext.Default);

        private static PopupPanel NewPopup() => (PopupPanel)new PopupPanel.UxmlFactory().Create(new Bag("fade-time", "0"), CreationContext.Default);
#pragma warning restore CS0618

        private IEnumerator SetUpTooltip(Action<VisualElement, Tooltip, TooltipManipulator> body)
        {
            _panel = InteractionPanel.Create();
            var target = new Button { text = "Target", tooltip = "T:Settings", name = "target", style = { width = 120, height = 40, marginLeft = 200, marginTop = 200 } };   // away from the origin, where the panel believes the mouse is
            _panel.Screen.Add(target);
            var tooltip = NewTooltip();
            _panel.Screen.Add(tooltip);
            var manipulator = new TooltipManipulator(tooltip);
            target.AddManipulator(manipulator);
            yield return InteractionPanel.Frames();
            body(target, tooltip, manipulator);
        }

        [UnityTest]
        public IEnumerator Tooltip_ShowsOnFocus_OnlyInNavigationMode()
        {
            yield return SetUpTooltip((target, tooltip, _) =>
            {
                target.Focus();
                Assert.AreEqual(Visibility.Hidden, tooltip.style.visibility.value, "no navigation class on the root: focus alone shows nothing");
                target.Blur();

                _panel.Root.AddToClassList("fdp-input-nav");
                target.Focus();
                Assert.AreEqual(Visibility.Visible, tooltip.style.visibility.value, "navigation mode: focus shows the tooltip");
                Assert.AreEqual("Settings", tooltip.Q<Label>(className: "fdp-tooltip__label").text);

                target.Blur();
                Assert.AreEqual(Visibility.Hidden, tooltip.style.visibility.value, "and losing focus hides it");
            });
        }

        [UnityTest]
        public IEnumerator Tooltip_FocusWithoutTooltipText_ShowsNothing()
        {
            yield return SetUpTooltip((target, tooltip, _) =>
            {
                target.tooltip = "";
                _panel.Root.AddToClassList("fdp-input-nav");
                target.Focus();
                Assert.AreEqual(Visibility.Hidden, tooltip.style.visibility.value);
            });
        }

        [UnityTest]
        public IEnumerator Tooltip_LongPressWithAFinger_ShowsIt_AndHidesItAfterTheLinger()
        {
            Tooltip tooltip = null;
            VisualElement target = null;
            yield return SetUpTooltip((t, tip, manipulator) =>
            {
                target = t;
                tooltip = tip;
                manipulator.LongPressDelay = 60;
                manipulator.LongPressLinger = 60;
            });

            Send.PointerDown(target, touch: true);
            Assert.AreEqual(Visibility.Hidden, tooltip.style.visibility.value, "not yet");
            yield return new WaitForSecondsRealtime(0.3f);
            Assert.AreEqual(Visibility.Visible, tooltip.style.visibility.value, "held long enough");

            Send.PointerUp(target, touch: true);
            Assert.AreEqual(Visibility.Visible, tooltip.style.visibility.value, "it lingers so the text can be read");
            yield return new WaitForSecondsRealtime(0.3f);
            Assert.AreEqual(Visibility.Hidden, tooltip.style.visibility.value);
        }

        [UnityTest]
        public IEnumerator Tooltip_ShortTouch_ShowsNothing()
        {
            Tooltip tooltip = null;
            VisualElement target = null;
            yield return SetUpTooltip((t, tip, manipulator) =>
            {
                target = t;
                tooltip = tip;
                manipulator.LongPressDelay = 200;
            });

            Send.PointerDown(target, touch: true);
            Send.PointerUp(target, touch: true);
            yield return new WaitForSecondsRealtime(0.4f);
            Assert.AreEqual(Visibility.Hidden, tooltip.style.visibility.value);
        }

        [UnityTest]
        public IEnumerator Tooltip_AMousePress_ShowsItByHoverOnly_SoNoLongPressLingerHidesIt()
        {
            Tooltip tooltip = null;
            VisualElement target = null;
            yield return SetUpTooltip((t, tip, manipulator) =>
            {
                target = t;
                tooltip = tip;
                manipulator.LongPressDelay = 30;
                manipulator.LongPressLinger = 30;
            });

            Send.PointerDown(target, touch: false);   // the panel reports the mouse over the target: the hover shows the tooltip
            yield return new WaitForSecondsRealtime(0.2f);
            Assert.AreEqual(Visibility.Visible, tooltip.style.visibility.value, "the mouse has hover for tooltips");

            Send.PointerUp(target, touch: false);
            yield return new WaitForSecondsRealtime(0.2f);
            Assert.AreEqual(Visibility.Visible, tooltip.style.visibility.value, "a mouse is not a long press: no linger timer closes the tooltip");
        }

        [UnityTest]
        public IEnumerator Tooltip_IsPlacedInTheLocalSpaceOfItsParent()
        {
            _panel = InteractionPanel.Create();
            var offset = new VisualElement();
            offset.style.position = Position.Absolute;
            offset.style.left = 200;
            offset.style.top = 100;
            offset.style.width = 400;
            offset.style.height = 300;
            _panel.Screen.Add(offset);
            var target = new Button { text = "Target", tooltip = "R:Right" };
            target.style.position = Position.Absolute;
            target.style.left = 50;
            target.style.top = 40;
            target.style.width = 60;
            target.style.height = 30;
            offset.Add(target);
            var tooltip = NewTooltip();
            offset.Add(tooltip);
            yield return InteractionPanel.Frames();

            tooltip.Show(target);

            // The tooltip's parent starts at (200, 100) in the panel; the target is 50 px into it. "R:" puts the tooltip 5 px right of the target.
            Assert.AreEqual(50f + 60f + 5f, tooltip.style.left.value.value, 0.5f);
        }

        [UnityTest]
        public IEnumerator Popup_KeepsFocusInside_ReturnsItOnHide_AndClosesOnCancel()
        {
            _panel = InteractionPanel.Create();
            var before = new Button { text = "before", name = "before" };
            var outside = new Button { text = "outside", name = "outside" };
            _panel.Screen.Add(before);
            _panel.Screen.Add(outside);
            var popup = NewPopup();
            var inside = new Button { text = "inside", name = "inside" };
            popup.Add(inside);
            _panel.Screen.Add(popup);
            yield return InteractionPanel.Frames();

            before.Focus();
            popup.Show();
            yield return new WaitForSecondsRealtime(0.15f);   // a popup that was hidden resolves its visibility, and so can take focus, a few milliseconds later
            Assert.IsTrue(popup.Contains(_panel.Panel.focusController.focusedElement as VisualElement), "the popup takes focus");

            outside.Focus();
            yield return InteractionPanel.Frames();
            Assert.IsTrue(popup.Contains(_panel.Panel.focusController.focusedElement as VisualElement), "focus cannot leave an open popup");

            inside.Focus();
            Send.Cancel(inside);
            Assert.AreEqual(Visibility.Hidden, popup.style.visibility.value, "Cancel closes it");
            Assert.AreEqual("before", _panel.FocusedName, "and focus goes back to where it was");
        }

        [UnityTest]
        public IEnumerator Popup_Cancel_WhileClosed_IsNotConsumed()
        {
            _panel = InteractionPanel.Create();
            var popup = NewPopup();
            _panel.Screen.Add(popup);
            yield return InteractionPanel.Frames();

            var reached = false;
            _panel.Screen.RegisterCallback<NavigationCancelEvent>(_ => reached = true);
            Send.Cancel(popup.Q(className: "fdp-popup-panel"));
            Assert.IsTrue(reached);
        }

        [UnityTest]
        public IEnumerator Popup_RegistersWithTheBackDispatcher_WhileOpen()
        {
            _panel = InteractionPanel.Create();
            var popup = NewPopup();
            _panel.Screen.Add(popup);
            yield return InteractionPanel.Frames();

            Action backAction = null;
            var disposed = 0;
            PopupPanel.BackRegistrar = action =>
            {
                backAction = action;
                return new Registration(() => disposed++);
            };

            popup.Show();
            Assert.IsNotNull(backAction, "registered on Show");
            Assert.AreEqual(0, disposed);

            backAction();   // the dispatcher calls the action on back
            Assert.AreEqual(Visibility.Hidden, popup.style.visibility.value);
            Assert.AreEqual(1, disposed, "unregistered on Hide");

            backAction = null;
            popup.Show();
            popup.Show();   // showing twice registers once
            Assert.IsNotNull(backAction);
            popup.Hide();
            Assert.AreEqual(2, disposed);
        }

        private sealed class Registration : IDisposable
        {
            private readonly Action _onDispose;
            public Registration(Action onDispose) => _onDispose = onDispose;
            public void Dispose() => _onDispose();
        }

        [UnityTest]
        public IEnumerator ColorField_OpensThePickerOnSubmit()
        {
            _panel = InteractionPanel.Create();
            var field = new ColorField("Colour");
            var popup = new ColorPopup { };
            field.ColorPopup = popup;
            _panel.Screen.Add(field);
            _panel.Screen.Add(popup);
            yield return InteractionPanel.Frames();

            var swatch = field.Q(className: "fdp-color-field__input");
            Assert.IsNotNull(swatch);
            Assert.IsTrue(swatch.focusable, "the swatch can take keyboard and gamepad focus");

            Send.Submit(swatch);
            Assert.AreEqual(Visibility.Visible, popup.style.visibility.value);
        }

        [UnityTest]
        public IEnumerator Slider2D_Submit_StartsEditing_ArrowsMoveTheValue_SubmitEnds()
        {
            _panel = InteractionPanel.Create();
            var slider = new Slider2D(Vector2.zero, new Vector2(10f, 20f));
            slider.style.width = 100;
            slider.style.height = 100;
            slider.SetValueWithoutNotify(new Vector2(5f, 10f));
            _panel.Screen.Add(slider);
            yield return InteractionPanel.Frames();

            Assert.IsTrue(slider.focusable);
            Send.Move(slider, NavigationMoveEvent.Direction.Right);
            Assert.AreEqual(new Vector2(5f, 10f), slider.value, "arrows only move focus until editing starts");

            Send.Submit(slider);
            Assert.IsTrue(slider.ClassListContains("fdp-slider-2d--editing"));

            Send.Move(slider, NavigationMoveEvent.Direction.Right);   // 5 percent of the 0..10 range
            Assert.AreEqual(5.5f, slider.value.x, 0.001f);
            Send.Move(slider, NavigationMoveEvent.Direction.Up);      // 5 percent of the 0..20 range
            Assert.AreEqual(11f, slider.value.y, 0.001f);
            Send.Move(slider, NavigationMoveEvent.Direction.Left);
            Send.Move(slider, NavigationMoveEvent.Direction.Down);
            Assert.AreEqual(new Vector2(5f, 10f).x, slider.value.x, 0.001f);
            Assert.AreEqual(new Vector2(5f, 10f).y, slider.value.y, 0.001f);

            Send.Submit(slider);
            Assert.IsFalse(slider.ClassListContains("fdp-slider-2d--editing"));
        }

        [UnityTest]
        public IEnumerator Slider2D_ValueStaysInRange_AndCancelAndFocusLossEndEditing()
        {
            _panel = InteractionPanel.Create();
            var slider = new Slider2D(Vector2.zero, Vector2.one);
            slider.SetValueWithoutNotify(new Vector2(1f, 1f));
            var other = new Button { text = "other" };
            _panel.Screen.Add(slider);
            _panel.Screen.Add(other);
            yield return InteractionPanel.Frames();

            Send.Submit(slider);
            Send.Move(slider, NavigationMoveEvent.Direction.Right);
            Send.Move(slider, NavigationMoveEvent.Direction.Up);
            Assert.AreEqual(Vector2.one, slider.value, "clamped to the maximum");

            var reached = false;
            _panel.Screen.RegisterCallback<NavigationCancelEvent>(_ => reached = true);
            Send.Cancel(slider);
            Assert.IsFalse(slider.ClassListContains("fdp-slider-2d--editing"));
            Assert.IsFalse(reached, "Cancel that ends editing is consumed");

            Send.Submit(slider);
            slider.Focus();
            other.Focus();
            yield return null;
            Assert.IsFalse(slider.ClassListContains("fdp-slider-2d--editing"), "losing focus ends editing");
        }

        [UnityTest]
        public IEnumerator Slider2D_TabStillMovesFocusWhileEditing()
        {
            _panel = InteractionPanel.Create();
            var slider = new Slider2D(Vector2.zero, Vector2.one);
            _panel.Screen.Add(slider);
            yield return InteractionPanel.Frames();
            Send.Submit(slider);

            var reached = false;
            _panel.Screen.RegisterCallback<NavigationMoveEvent>(_ => reached = true);
            Send.Move(slider, NavigationMoveEvent.Direction.Next);
            Assert.IsTrue(reached, "Next and Previous (Tab) are not consumed, so the control can always be left");
        }
    }
}
