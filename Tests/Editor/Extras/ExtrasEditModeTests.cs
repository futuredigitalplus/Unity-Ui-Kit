// FutureDigitalPlus: new smoke tests for the Extras module (not from upstream).
using FutureDigitalPlus.UIKit.Extras;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.UIElements;

namespace FutureDigitalPlus.UIKit.Extras.Tests
{
    /// <summary>
    /// EditMode smoke tests: every Extras element can be constructed without a panel, finds its Resources style sheet
    /// and keeps its public behaviour. Layout dependent behaviour is covered by the PlayMode tests.
    /// </summary>
    public class ExtrasEditModeTests
    {
        private static readonly string[] StyleSheetResources =
        {
            "FutureDigitalPlus/UIKit/Extras/Styles/TooltipStyleSheet",
            "FutureDigitalPlus/UIKit/Extras/Styles/PopupPanelStyleSheet",
            "FutureDigitalPlus/UIKit/Extras/Styles/PopupMessageStyleSheet",
            "FutureDigitalPlus/UIKit/Extras/Styles/PopupTextFieldStyleSheet",
            "FutureDigitalPlus/UIKit/Extras/Styles/ColorFieldStyleSheet",
            "FutureDigitalPlus/UIKit/Extras/Styles/ColorPopupStyleSheet",
            "FutureDigitalPlus/UIKit/Extras/Styles/Slider2DStyleSheet",
        };

        [TestCaseSource(nameof(StyleSheetResources))]
        public void StyleSheetResource_IsFoundByResourcesLoad(string path)
        {
            Assert.IsNotNull(Resources.Load<StyleSheet>(path), path);
        }

        [Test]
        public void Elements_ConstructAndCarryTheirStyleSheet()
        {
            AssertHasStyleSheet(new Tooltip());
            AssertHasStyleSheet(new PopupPanel());
            AssertHasStyleSheet(new PopupMessage());
            AssertHasStyleSheet(new PopupTextField());
            AssertHasStyleSheet(new ColorField());
            AssertHasStyleSheet(new ColorPopup());
            AssertHasStyleSheet(new Slider2D());
            Assert.IsNotNull(new BlurPanel());
            Assert.IsNotNull(new AspectRatioPadding());
        }

        private static void AssertHasStyleSheet(VisualElement element)
        {
            Assert.GreaterOrEqual(element.styleSheets.count, 1, element.GetType().Name);
        }

        [Test]
        public void GetDocumentRoot_ReturnsTopMostAncestor()
        {
            var root = new VisualElement();
            var mid = new VisualElement();
            var leaf = new VisualElement();
            root.Add(mid);
            mid.Add(leaf);

            Assert.AreSame(root, leaf.GetDocumentRoot());
            Assert.AreSame(root, root.GetDocumentRoot());
        }

        [Test]
        public void Slider2D_ClampsValueToItsRange()
        {
            var slider = new Slider2D(Vector2.zero, Vector2.one);
            slider.SetValueWithoutNotify(new Vector2(2f, -1f));
            Assert.AreEqual(new Vector2(1f, 0f), slider.value);

            slider.SetValueWithoutNotify(new Vector2(0.25f, 0.75f));
            Assert.AreEqual(new Vector2(0.25f, 0.75f), slider.value);
        }

        [Test]
        public void BlurPanel_SetImage_StoresTextureAndStretches()
        {
            var texture = new Texture2D(8, 8);
            try
            {
                var panel = new BlurPanel();
                panel.SetImage(texture);
                Assert.AreSame(texture, panel.image);
                Assert.AreEqual(ScaleMode.StretchToFill, panel.scaleMode);
                // Detached (no size yet): sourceRect must stay finite, not become Infinity/NaN.
                var r = panel.sourceRect;
                Assert.IsFalse(float.IsNaN(r.width) || float.IsInfinity(r.width) || float.IsNaN(r.x) || float.IsInfinity(r.x), $"sourceRect={r}");
            }
            finally
            {
                Object.DestroyImmediate(texture);
            }
        }

        [Test]
        public void PopupMessage_Show_HidesButtonsWithoutCallbacks()
        {
            var popup = new PopupMessage();
            popup.Show("Heading", "Body", "Close");

            Assert.AreEqual(Visibility.Visible, popup.style.visibility.value);
            Assert.AreEqual("Heading", popup.Q<Label>(className: "fdp-popup-message__heading").text);
            Assert.AreEqual("Body", popup.Q<Label>(className: "fdp-popup-message__text").text);
            // the three argument overload passes no accept callback, so only the cancel button is shown
            Assert.AreEqual(DisplayStyle.None, popup.Q<Button>(className: "fdp-popup-message__confirm-button").style.display.value);
            Assert.AreEqual(DisplayStyle.Flex, popup.Q<Button>(className: "fdp-popup-message__cancel-button").style.display.value);
            Assert.AreEqual("Close", popup.Q<Button>(className: "fdp-popup-message__cancel-button").text);
        }

        [Test]
        public void PopupTextField_Show_AppliesTextAndMaxLength()
        {
            var popup = new PopupTextField();
            popup.Show("Name", "Enter a name", "abc", 12, _ => { }, () => { });

            var field = popup.Q<TextField>(className: "fdp-popup-textfield__edit");
            Assert.AreEqual("abc", field.value);
            Assert.AreEqual(12, field.maxLength);
        }
    }
}
