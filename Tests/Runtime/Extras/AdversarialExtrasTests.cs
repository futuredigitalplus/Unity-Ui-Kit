using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UIElements;

namespace FutureDigitalPlus.UIKit.Extras.Tests
{
    /// <summary>The tooltip with targets that have no text, targets that leave the panel under the pointer, and a null target.</summary>
    public class AdversarialExtrasTests
    {
        private GameObject _go;
        private PanelSettings _settings;
        private RenderTexture _target;
        private VisualElement _container;

        [SetUp]
        public void SetUp()
        {
            _target = new RenderTexture(800, 600, 0);
            _settings = ScriptableObject.CreateInstance<PanelSettings>();
            _settings.scaleMode = PanelScaleMode.ConstantPixelSize;
            _settings.targetTexture = _target;
            _go = new GameObject("AdversarialExtras");
            _go.SetActive(false);
            var document = _go.AddComponent<UIDocument>();
            document.panelSettings = _settings;
            _go.SetActive(true);
            _container = new VisualElement();
            _container.style.width = 800;
            _container.style.height = 600;
            document.rootVisualElement.Add(_container);
            LogAssert.ignoreFailingMessages = true;
        }

        [TearDown]
        public void TearDown()
        {
            LogAssert.ignoreFailingMessages = false;
            Object.Destroy(_go);
            Object.Destroy(_settings);
            if (_target != null) _target.Release();
            Object.Destroy(_target);
        }

        private static void Hover(VisualElement element)
        {
            using (var e = PointerEnterEvent.GetPooled(new Event { type = EventType.MouseMove, mousePosition = element.worldBound.center }))
            {
                e.target = element;
                element.SendEvent(e);
            }
        }

        private Button AddButton(string tooltipText, Tooltip tooltip)
        {
            var button = new Button { text = "b", tooltip = tooltipText };
            button.style.position = Position.Absolute;
            button.style.left = 100;
            button.style.top = 100;
            button.style.width = 80;
            button.style.height = 30;
            _container.Add(button);
            button.AddManipulator(new TooltipManipulator(tooltip));
            return button;
        }

        private Tooltip AddTooltip(int fadeMilliseconds)
        {
            var tooltip = new Tooltip();
            typeof(Tooltip).GetProperty("FadeTime").SetValue(tooltip, fadeMilliseconds);
            typeof(Tooltip).GetProperty("Delay").SetValue(tooltip, 0);
            _container.Add(tooltip);
            return tooltip;
        }

        [UnityTest]
        public IEnumerator Hover_OnATargetWithoutTooltipText_ShowsNothing()
        {
            if (!Application.isPlaying) Assert.Ignore("Needs a runtime panel (Play Mode).");
            var tooltip = AddTooltip(0);
            var button = AddButton("", tooltip);
            yield return null;
            yield return null;

            Hover(button);
            Assert.AreEqual(Visibility.Hidden, tooltip.style.visibility.value, "an empty bubble is worse than none");

            button.tooltip = "Now it has text";
            Hover(button);
            Assert.AreEqual(Visibility.Visible, tooltip.style.visibility.value);
        }

        [UnityTest]
        public IEnumerator TargetRemovedWhileItsTooltipIsUp_ClosesTheTooltip()
        {
            if (!Application.isPlaying) Assert.Ignore("Needs a runtime panel (Play Mode).");
            var tooltip = AddTooltip(0);
            var button = AddButton("Settings", tooltip);
            yield return null;
            yield return null;
            Hover(button);
            Assert.AreEqual(Visibility.Visible, tooltip.style.visibility.value);

            button.RemoveFromHierarchy();
            yield return null;

            Assert.AreEqual(Visibility.Hidden, tooltip.style.visibility.value, "the pointer-out of a removed element never comes");
        }

        [UnityTest]
        public IEnumerator TargetRemoved_WhileAnotherTargetHasTheTooltip_LeavesItUp()
        {
            if (!Application.isPlaying) Assert.Ignore("Needs a runtime panel (Play Mode).");
            var tooltip = AddTooltip(0);
            var first = AddButton("First", tooltip);
            var second = AddButton("Second", tooltip);
            yield return null;
            yield return null;
            Hover(second);
            Assert.AreEqual(Visibility.Visible, tooltip.style.visibility.value);

            first.RemoveFromHierarchy();
            yield return null;

            Assert.AreEqual(Visibility.Visible, tooltip.style.visibility.value, "the tooltip belongs to the second target now");
        }

        [Test]
        public void Show_WithANullTarget_DoesNotThrow()
        {
            var tooltip = new Tooltip();
            Assert.DoesNotThrow(() => tooltip.Show(null));
            Assert.DoesNotThrow(() => tooltip.Close());
        }
    }
}
