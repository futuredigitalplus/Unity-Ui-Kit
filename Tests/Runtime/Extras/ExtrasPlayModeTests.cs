// FutureDigitalPlus: new smoke tests for the Extras module (not from upstream).
using System.Collections;
using System.Collections.Generic;
using FutureDigitalPlus.UIKit.Extras;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UIElements;

namespace FutureDigitalPlus.UIKit.Extras.Tests
{
    /// <summary>
    /// PlayMode tests that need a real UI Toolkit panel with a layout pass (aspect ratio padding, tooltip placement,
    /// click driven popups). The panel renders into a fixed size RenderTexture so the results do not depend on the
    /// Game view size.
    /// </summary>
    public class ExtrasPlayModeTests
    {
        private const int PanelWidth = 1600;
        private const int PanelHeight = 900;

        private GameObject go;
        private PanelSettings panelSettings;
        private RenderTexture target;
        private VisualElement container;

        [SetUp]
        public void SetUp()
        {
            target = new RenderTexture(PanelWidth, PanelHeight, 0);
            panelSettings = ScriptableObject.CreateInstance<PanelSettings>();
            panelSettings.scaleMode = PanelScaleMode.ConstantPixelSize;
            panelSettings.targetTexture = target;

            // create inactive so the PanelSettings is assigned before the UIDocument's OnEnable runs
            go = new GameObject("ExtrasTests");
            go.SetActive(false);
            var doc = go.AddComponent<UIDocument>();
            doc.panelSettings = panelSettings;
            go.SetActive(true);

            container = new VisualElement();
            container.style.width = PanelWidth;
            container.style.height = PanelHeight;
            container.style.flexDirection = FlexDirection.Row;
            doc.rootVisualElement.Add(container);
        }

        [TearDown]
        public void TearDown()
        {
            Object.Destroy(go);
            Object.Destroy(panelSettings);
            if (target != null) target.Release();
            Object.Destroy(target);
        }

        private static IEnumerator WaitForLayout()
        {
            yield return null;
            yield return null;
            yield return null;
        }

        private static void Submit(VisualElement element)
        {
            using (var evt = NavigationSubmitEvent.GetPooled())
            {
                evt.target = element;
                element.SendEvent(evt);
            }
        }

        // UxmlFactory based creation, the same path UXML documents take.
#pragma warning disable CS0618 // UxmlFactory / IUxmlAttributes are deprecated in Unity 6 but still the upstream API
        private class Bag : IUxmlAttributes
        {
            private readonly Dictionary<string, string> values;
            public Bag(params string[] keyValues)
            {
                values = new Dictionary<string, string>();
                for (var i = 0; i + 1 < keyValues.Length; i += 2) values[keyValues[i]] = keyValues[i + 1];
            }
            public bool TryGetAttributeValue(string attributeName, out string value) => values.TryGetValue(attributeName, out value);
        }
#pragma warning restore CS0618

        [UnityTest]
        public IEnumerator AspectRatioPadding_WiderThanDesign_AddsEqualSideMargins()
        {
#pragma warning disable CS0618
            var padding = (AspectRatioPadding)new AspectRatioPadding.UxmlFactory().Create(new Bag("width", "4", "height", "3"), CreationContext.Default);
#pragma warning restore CS0618
            Assert.AreEqual(4, padding.RatioWidth);
            Assert.AreEqual(3, padding.RatioHeight);
            container.Add(padding);

            yield return WaitForLayout();

            // 1600x900 shown at a 4:3 design ratio: the 4:3 area is 1200 wide, so 200 px on each side.
            Assert.AreEqual(200f, padding.resolvedStyle.marginLeft, 1f);
            Assert.AreEqual(200f, padding.resolvedStyle.marginRight, 1f);
        }

        [UnityTest]
        public IEnumerator AspectRatioPadding_NarrowerThanDesign_HasNoMargins()
        {
            container.style.width = 600;
            container.style.height = 900;
#pragma warning disable CS0618
            var padding = (AspectRatioPadding)new AspectRatioPadding.UxmlFactory().Create(new Bag("width", "4", "height", "3"), CreationContext.Default);
#pragma warning restore CS0618
            container.Add(padding);

            yield return WaitForLayout();

            Assert.AreEqual(0f, padding.resolvedStyle.marginLeft, 0.01f);
            Assert.AreEqual(0f, padding.resolvedStyle.marginRight, 0.01f);
        }

        [UnityTest]
        public IEnumerator Tooltip_ShowWithRightHint_PlacesItNextToTheTarget()
        {
            var button = new Button { text = "Target", tooltip = "R:Settings" };
            button.style.position = Position.Absolute;
            button.style.left = 100;
            button.style.top = 100;
            button.style.width = 50;
            button.style.height = 30;
            container.Add(button);

#pragma warning disable CS0618
            var tooltip = (Tooltip)new Tooltip.UxmlFactory().Create(new Bag("fade-time", "0"), CreationContext.Default);
#pragma warning restore CS0618
            container.Add(tooltip);
            button.AddManipulator(new TooltipManipulator(tooltip));

            yield return WaitForLayout();

            tooltip.Show(button);

            Assert.AreEqual(Visibility.Visible, tooltip.style.visibility.value);
            Assert.AreEqual("Settings", tooltip.Q<Label>(className: "fdp-tooltip__label").text);
            // "R:" puts it 5 px right of the target (x = 100 + 50 + 5), vertical centre aligned.
            Assert.AreEqual(155f, tooltip.style.left.value.value, 0.5f);

            tooltip.Close();
            Assert.AreEqual(Visibility.Hidden, tooltip.style.visibility.value);
        }

        [UnityTest]
        public IEnumerator PopupMessage_AcceptButton_InvokesCallback()
        {
            var popup = new PopupMessage();
            container.Add(popup);
            yield return WaitForLayout();

            var accepted = 0;
            var cancelled = 0;
            popup.Show("Title", "Body", () => accepted++, () => cancelled++);

            Submit(popup.Q<Button>(className: "fdp-popup-message__confirm-button"));
            Assert.AreEqual(1, accepted);
            Assert.AreEqual(0, cancelled);

            Submit(popup.Q<Button>(className: "fdp-popup-message__cancel-button"));
            Assert.AreEqual(1, cancelled);

            popup.Hide();
        }

        [UnityTest]
        public IEnumerator ColorPopup_Submit_ReturnsTheChosenColor()
        {
            var popup = new ColorPopup();
            container.Add(popup);
            yield return WaitForLayout();

            var chosen = Color.clear;
            var wanted = new Color(0.2f, 0.4f, 0.6f, 0.5f);
            popup.Show(wanted, c => chosen = c);
            yield return null;

            Submit(popup.Q<Button>(className: "fdp-color-popup__submit-button"));

            Assert.AreEqual(wanted.r, chosen.r, 0.002f);
            Assert.AreEqual(wanted.g, chosen.g, 0.002f);
            Assert.AreEqual(wanted.b, chosen.b, 0.002f);
            Assert.AreEqual(wanted.a, chosen.a, 0.002f);
        }

        [UnityTest]
        public IEnumerator ColorField_Click_OpensTheLinkedPopup()
        {
            var popup = new ColorPopup();
            var field = new ColorField("Fill") { ColorPopup = popup, value = Color.green };
            container.Add(field);
            container.Add(popup);
            yield return WaitForLayout();

            var input = field.Q(className: "fdp-color-field__input");
            using (var click = ClickEvent.GetPooled())
            {
                click.target = input;
                input.SendEvent(click);
            }

            Assert.AreEqual(Visibility.Visible, popup.style.visibility.value);

            popup.Hide();
        }
    }
}
