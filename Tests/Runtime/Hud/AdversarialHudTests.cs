using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UIElements;

namespace FutureDigitalPlus.UIKit.Hud.Tests
{
    /// <summary>HUD widgets fed NaN and infinities, anchors whose target dies, widgets removed while they animate.</summary>
    public class AdversarialHudTests
    {
        private HudPanel _panel;
        private GameObject _cameraObject;

        [SetUp]
        public void SetUp() => LogAssert.ignoreFailingMessages = true;

        [TearDown]
        public void TearDown()
        {
            LogAssert.ignoreFailingMessages = false;
            if (_cameraObject != null) Object.Destroy(_cameraObject);
            _panel?.Dispose();
        }

        private T AddFullScreen<T>(T element) where T : VisualElement
        {
            element.style.position = Position.Absolute;
            element.style.left = 0;
            element.style.top = 0;
            element.style.right = 0;
            element.style.bottom = 0;
            _panel.Root.Add(element);
            return element;
        }

        private Camera CreateCamera()
        {
            _cameraObject = new GameObject("HudTestCamera");
            _cameraObject.transform.position = new Vector3(0f, 0f, -10f);
            var camera = _cameraObject.AddComponent<Camera>();
            camera.targetTexture = _panel.Target;
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.fieldOfView = 60f;
            return camera;
        }

        [UnityTest]
        public IEnumerator WorldAnchor_TargetDestroyed_HidesTheAnchorInsteadOfJumpingToTheOrigin()
        {
            _panel = HudPanel.Create();
            var layer = AddFullScreen(new WorldAnchorLayer { Camera = CreateCamera() });
            var target = new GameObject("Enemy");
            target.transform.position = new Vector3(2f, 0f, 0f);
            var anchor = layer.Pin(new Label("hp"), target.transform);
            yield return HudPanel.Frames(3);
            layer.UpdateAnchors();
            Assert.AreEqual(DisplayStyle.Flex, anchor.Holder.style.display.value);

            Object.DestroyImmediate(target);
            layer.UpdateAnchors();

            Assert.AreEqual(DisplayStyle.None, anchor.Holder.style.display.value, "the enemy died: its health bar must not sit at the world origin");
            Assert.IsTrue(layer.Remove(anchor));
        }

        [UnityTest]
        public IEnumerator WorldAnchor_CameraDestroyed_HidesTheAnchorsWithoutThrowing()
        {
            _panel = HudPanel.Create();
            var camera = CreateCamera();
            var layer = AddFullScreen(new WorldAnchorLayer { Camera = camera });
            var target = new GameObject("Probe");
            var anchor = layer.Pin(new Label("t"), target.transform);
            yield return HudPanel.Frames(3);
            layer.UpdateAnchors();

            Object.DestroyImmediate(camera);
            Assert.DoesNotThrow(() => layer.UpdateAnchors());
            Assert.AreEqual(DisplayStyle.None, anchor.Holder.style.display.value);
            Object.Destroy(target);
        }

        [UnityTest]
        public IEnumerator Widgets_HostileValues_DoNotPoisonOrKeepTheDriverBusy()
        {
            _panel = HudPanel.Create();
            var meter = AddFullScreen(new MeterBar());
            var radial = AddFullScreen(new RadialProgress { SmoothSeconds = 0.2f });
            var count = AddFullScreen(new CountUpLabel());
            var cooldown = AddFullScreen(new CooldownIcon());
            var countdown = AddFullScreen(new CountdownLabel());
            yield return HudPanel.Frames();

            meter.Set(float.NaN, float.NaN);
            meter.Value = float.PositiveInfinity;
            meter.MaxValue = float.NaN;
            radial.Value = float.NaN;
            radial.Value = float.PositiveInfinity;
            radial.Value = 0f / 0f;
            count.Value = double.NaN;
            count.Value = double.PositiveInfinity;
            cooldown.StartCooldown(float.NaN);
            countdown.StartIn(double.NaN);
            yield return HudPanel.Frames(4);

            Assert.IsFalse(float.IsNaN(meter.Value));
            Assert.IsTrue(cooldown.IsReady, "a NaN cooldown is no cooldown");
            Assert.IsFalse(countdown.text.Contains("NaN"), countdown.text);
            Assert.IsFalse(count.text.Contains("NaN"), count.text);
        }

        [UnityTest]
        public IEnumerator Widgets_RemovedWhileTheyAnimate_StopTickingAndCanBeReAdded()
        {
            _panel = HudPanel.Create();
            var meter = AddFullScreen(new MeterBar { MaxValue = 100f, Value = 100f });
            var count = AddFullScreen(new CountUpLabel { DurationSeconds = 1f });
            yield return HudPanel.Frames();
            meter.Value = 10f;
            count.Value = 5000d;
            yield return null;

            meter.RemoveFromHierarchy();
            count.RemoveFromHierarchy();
            yield return new WaitForSecondsRealtime(0.3f);
            _panel.Root.Add(meter);
            _panel.Root.Add(count);
            yield return new WaitForSecondsRealtime(1.5f);

            Assert.AreEqual(5000d, count.DisplayedValue, 0.5d, "the roll finished after the label came back");
            Assert.IsFalse(float.IsNaN(meter.Value));
        }
    }
}
