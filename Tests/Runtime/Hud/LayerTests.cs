using System.Collections;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UIElements;

namespace FutureDigitalPlus.UIKit.Hud.Tests
{
    public class LayerTests
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

        // ---- FloatingTextLayer ----

        [UnityTest]
        public IEnumerator FloatingText_SpawnShowsALabelThatRisesAndFades()
        {
            _panel = HudPanel.Create();
            var layer = AddFullScreen(new FloatingTextLayer(4));
            layer.Model.Lifetime = 1f;
            yield return HudPanel.Frames();

            Assert.IsTrue(layer.Spawn(new Vector2(400f, 300f), "128"));
            var label = layer.Q<Label>();
            Assert.AreEqual("128", label.text);
            Assert.AreEqual(DisplayStyle.Flex, label.style.display.value);
            var startY = label.style.translate.value.y.value;

            layer.Step(0.5f);
            Assert.Less(label.style.translate.value.y.value, startY, "rises");
            layer.Step(0.45f);
            Assert.Less(label.style.opacity.value, 1f, "fades");
            layer.Step(0.1f);
            Assert.AreEqual(DisplayStyle.None, label.style.display.value, "hidden when the text ended");
        }

        [UnityTest]
        public IEnumerator FloatingText_Labels_AreReusedFromThePool()
        {
            _panel = HudPanel.Create();
            var layer = AddFullScreen(new FloatingTextLayer(3));
            layer.Model.OverlapStep = 0f;
            layer.Model.MaxSpawnsPerFrame = 100;
            yield return HudPanel.Frames();

            for (var round = 0; round < 5; round++)
            {
                for (var i = 0; i < 3; i++) layer.Spawn(new Vector2(100f + i * 200f, 300f), "x");
                layer.Step(10f);
            }

            Assert.AreEqual(3, layer.Query<Label>().ToList().Count, "never more labels than slots");
        }

        [UnityTest]
        public IEnumerator FloatingText_KindsSetStyleClasses()
        {
            _panel = HudPanel.Create();
            var layer = AddFullScreen(new FloatingTextLayer(4));
            layer.Model.MaxSpawnsPerFrame = 100;
            yield return HudPanel.Frames();
            layer.Spawn(new Vector2(100f, 300f), "9", FloatingTextKind.Critical);
            layer.Spawn(new Vector2(300f, 300f), "+5", FloatingTextKind.Heal);
            var labels = layer.Query<Label>().ToList();
            Assert.IsTrue(labels[0].ClassListContains("fdp-floating-text--critical"));
            Assert.IsTrue(labels[1].ClassListContains("fdp-floating-text--heal"));
            Assert.IsFalse(labels[1].ClassListContains("fdp-floating-text--critical"));
        }

        [UnityTest]
        public IEnumerator FloatingText_PerFrameCap_DropsTheExcess()
        {
            _panel = HudPanel.Create();
            var layer = AddFullScreen(new FloatingTextLayer(20));
            layer.Model.MaxSpawnsPerFrame = 2;
            layer.Model.OverlapStep = 0f;
            yield return HudPanel.Frames();
            var accepted = 0;
            for (var i = 0; i < 5; i++)
                if (layer.Spawn(new Vector2(i * 100f, 300f), "x")) accepted++;
            Assert.AreEqual(2, accepted);
            yield return null;
            Assert.IsTrue(layer.Spawn(new Vector2(0f, 300f), "x"), "the next frame accepts again");
        }

        [UnityTest]
        public IEnumerator FloatingText_ClearTexts_HidesEverything()
        {
            _panel = HudPanel.Create();
            var layer = AddFullScreen(new FloatingTextLayer(4));
            yield return HudPanel.Frames();
            layer.Spawn(new Vector2(100f, 100f), "a");
            layer.ClearTexts();
            Assert.AreEqual(DisplayStyle.None, layer.Q<Label>().style.display.value);
            Assert.AreEqual(0, layer.Model.ActiveCount);
        }

        [UnityTest]
        public IEnumerator FloatingText_Step_AllocatesNothing()
        {
            _panel = HudPanel.Create();
            var layer = AddFullScreen(new FloatingTextLayer(8));
            layer.Model.Lifetime = 1000f;
            layer.Model.MaxSpawnsPerFrame = 100;
            layer.Model.OverlapStep = 0f;
            yield return HudPanel.Frames();
            if (!AllocProbe.IsAvailable()) Assert.Ignore("The runtime does not report per-thread allocations.");
            for (var i = 0; i < 6; i++) layer.Spawn(new Vector2(i * 120f, 300f), "12");
            Assert.AreEqual(0L, AllocProbe.Measure(() => layer.Step(0.016f)));
        }

        // ---- WorldAnchorLayer ----

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
        public IEnumerator WorldAnchor_InFrontOfTheCamera_IsShownOnScreen()
        {
            _panel = HudPanel.Create();
            var layer = AddFullScreen(new WorldAnchorLayer { Camera = CreateCamera() });
            var plate = new Label("Goblin");
            var anchor = layer.Pin(plate, Vector3.zero);
            yield return HudPanel.Frames(3);
            layer.UpdateAnchors();

            Assert.AreEqual(AnchorVisibility.OnScreen, anchor.Result.Visibility);
            Assert.IsTrue(anchor.Result.Show);
            Assert.AreEqual(HudPanel.Width * 0.5f, anchor.Result.X, HudPanel.Width * 0.15f);
            Assert.AreEqual(HudPanel.Height * 0.5f, anchor.Result.Y, HudPanel.Height * 0.15f);
        }

        [UnityTest]
        public IEnumerator WorldAnchor_Projection_AgreesWithUnityOnAScreenPanel()
        {
            _panel = HudPanel.Create(toScreen: true);
            var camera = CreateCamera();
            camera.targetTexture = null;
            var layer = AddFullScreen(new WorldAnchorLayer { Camera = camera });
            yield return HudPanel.Frames(3);
            foreach (var world in new[] { Vector3.zero, new Vector3(2f, 1f, 5f), new Vector3(-3f, -1.5f, 0f) })
            {
                var ours = WorldAnchorLayer.ProjectToPanel(layer.panel, camera, world);
                var unitys = RuntimePanelUtils.CameraTransformWorldToPanel(layer.panel, world, camera);
                Assert.AreEqual(unitys.x, ours.x, 0.5f, "x of " + world);
                Assert.AreEqual(unitys.y, ours.y, 0.5f, "y of " + world);
            }
        }

        [UnityTest]
        public IEnumerator WorldAnchor_BehindTheCamera_IsHiddenOrClamped()
        {
            _panel = HudPanel.Create();
            var layer = AddFullScreen(new WorldAnchorLayer { Camera = CreateCamera() });
            var hidden = layer.Pin(new Label("hide"), new Vector3(0f, 0f, -50f), OffScreenMode.Hide);
            var clamped = layer.Pin(new Label("clamp"), new Vector3(0f, 0f, -50f), OffScreenMode.Clamp);
            yield return HudPanel.Frames(3);
            layer.UpdateAnchors();

            Assert.AreEqual(AnchorVisibility.Behind, hidden.Result.Visibility);
            Assert.IsFalse(hidden.Result.Show);
            Assert.AreEqual(DisplayStyle.None, hidden.Holder.style.display.value);
            Assert.IsTrue(clamped.Result.Show);
            Assert.AreEqual(DisplayStyle.Flex, clamped.Holder.style.display.value);
        }

        [UnityTest]
        public IEnumerator WorldAnchor_FarToTheSide_ClampsToTheEdgeWithAnArrow()
        {
            _panel = HudPanel.Create();
            var layer = AddFullScreen(new WorldAnchorLayer { Camera = CreateCamera(), EdgeMargin = 20f });
            var anchor = layer.Pin(new Label("quest"), new Vector3(500f, 0f, 0f), OffScreenMode.Clamp);
            yield return HudPanel.Frames(3);
            layer.UpdateAnchors();

            Assert.AreEqual(AnchorVisibility.OffScreen, anchor.Result.Visibility);
            Assert.AreEqual(HudPanel.Width - 20f, anchor.Result.X, 1f);
            Assert.AreEqual(DisplayStyle.Flex, anchor.Arrow.style.display.value);
        }

        [UnityTest]
        public IEnumerator WorldAnchor_FollowsATransform_AndRemoveCleansUp()
        {
            _panel = HudPanel.Create();
            var layer = AddFullScreen(new WorldAnchorLayer { Camera = CreateCamera() });
            var target = new GameObject("Target").transform;
            try
            {
                var anchor = layer.Pin(new Label("t"), target, Vector3.zero);
                yield return HudPanel.Frames(3);
                layer.UpdateAnchors();
                var x0 = anchor.Result.X;
                target.position = new Vector3(2f, 0f, 0f);
                layer.UpdateAnchors();
                Assert.Greater(anchor.Result.X, x0, "moved right with the target");

                Assert.IsTrue(layer.Remove(anchor));
                Assert.AreEqual(0, layer.Count);
                Assert.IsNull(anchor.Holder.parent);
                Assert.IsFalse(layer.Remove(anchor));
            }
            finally
            {
                Object.Destroy(target.gameObject);
            }
        }

        [UnityTest]
        public IEnumerator WorldAnchor_WithoutACamera_HidesTheAnchors()
        {
            _panel = HudPanel.Create();
            var layer = AddFullScreen(new WorldAnchorLayer());
            var anchor = layer.Pin(new Label("t"), Vector3.zero);
            yield return HudPanel.Frames(3);
            layer.UpdateAnchors();
            Assert.AreEqual(DisplayStyle.None, anchor.Holder.style.display.value);
        }

        [UnityTest]
        public IEnumerator WorldAnchor_Update_AllocatesNothing()
        {
            _panel = HudPanel.Create();
            var layer = AddFullScreen(new WorldAnchorLayer { Camera = CreateCamera() });
            for (var i = 0; i < 5; i++) layer.Pin(new Label("n"), new Vector3(i - 2f, 0f, 0f));
            yield return HudPanel.Frames(3);
            if (!AllocProbe.IsAvailable()) Assert.Ignore("The runtime does not report per-thread allocations.");
            Assert.AreEqual(0L, AllocProbe.Measure(() => layer.UpdateAnchors()));
        }

        // ---- TimedFeed ----

        [UnityTest]
        public IEnumerator Feed_PushAddsALine_AndMergesIdenticalKeys()
        {
            _panel = HudPanel.Create();
            var feed = new TimedFeed();
            _panel.Root.Add(feed);
            yield return HudPanel.Frames();

            feed.Push("+10 gold", "gold", "success");
            feed.Push("+10 gold", "gold", "success");
            feed.Push("Door opened");
            var labels = feed.Query<Label>().ToList();
            Assert.AreEqual(2, labels.Count);
            Assert.AreEqual("+10 gold x2", labels[0].text);
            Assert.IsTrue(labels[0].ClassListContains("fdp-timed-feed__item--success"));
            Assert.AreEqual("Door opened", labels[1].text);
        }

        [UnityTest]
        public IEnumerator Feed_NewestOnTop_InsertsAtTheTop()
        {
            _panel = HudPanel.Create();
            var feed = new TimedFeed { NewestOnTop = true };
            _panel.Root.Add(feed);
            yield return HudPanel.Frames();
            feed.Push("first");
            feed.Push("second");
            Assert.AreEqual("second", feed.Query<Label>().ToList()[0].text);
        }

        [UnityTest]
        public IEnumerator Feed_FadesThenRemovesLines()
        {
            _panel = HudPanel.Create();
            var feed = new TimedFeed { TimeToLive = 1f };
            _panel.Root.Add(feed);
            yield return HudPanel.Frames();
            feed.Push("hello");
            var label = feed.Q<Label>();
            feed.Step(0.8f);
            Assert.IsTrue(label.ClassListContains("fdp-timed-feed__item--fading"));
            feed.Step(0.5f);
            Assert.AreEqual(0, feed.childCount);
        }

        [UnityTest]
        public IEnumerator Feed_MaxEntries_DropsTheOldestLine()
        {
            _panel = HudPanel.Create();
            var feed = new TimedFeed { MaxEntries = 2 };
            _panel.Root.Add(feed);
            yield return HudPanel.Frames();
            feed.Push("a");
            feed.Push("b");
            feed.Push("c");
            var texts = feed.Query<Label>().ToList().Select(l => l.text).ToArray();
            CollectionAssert.AreEqual(new[] { "b", "c" }, texts);
        }

        [UnityTest]
        public IEnumerator Feed_RunsFromTheScheduler_ThenSleeps()
        {
            _panel = HudPanel.Create();
            var feed = new TimedFeed { TimeToLive = 0.3f };
            _panel.Root.Add(feed);
            yield return HudPanel.Frames();
            feed.Push("short lived");
            yield return HudPanel.WaitSeconds(0.9f);
            Assert.AreEqual(0, feed.childCount);
        }

        // ---- HudZones ----

        [UnityTest]
        public IEnumerator Zones_AddWidgetsToTheirZone()
        {
            _panel = HudPanel.Create();
            var zones = AddFullScreen(new HudZones());
            var meter = zones.Add(HudZone.TopLeft, new MeterBar());
            yield return HudPanel.Frames();
            Assert.AreSame(zones.Zone(HudZone.TopLeft), meter.parent);
            Assert.AreEqual(PickingMode.Ignore, zones.Zone(HudZone.BottomRight).pickingMode);
            Assert.AreEqual(PickingMode.Ignore, zones.pickingMode);
        }

        [UnityTest]
        public IEnumerator Zones_PlaceTheZonesInTheirCorners()
        {
            _panel = HudPanel.Create();
            var zones = AddFullScreen(new HudZones());
            var tl = zones.Add(HudZone.TopLeft, new VisualElement { name = "tl", style = { width = 50, height = 20 } });
            var br = zones.Add(HudZone.BottomRight, new VisualElement { name = "br", style = { width = 50, height = 20 } });
            var tc = zones.Add(HudZone.TopCenter, new VisualElement { name = "tc", style = { width = 50, height = 20 } });
            yield return HudPanel.Frames(3);

            Assert.AreEqual(0f, tl.worldBound.xMin, 1f);
            Assert.AreEqual(0f, tl.worldBound.yMin, 1f);
            Assert.AreEqual(HudPanel.Width, br.worldBound.xMax, 1f);
            Assert.AreEqual(HudPanel.Height, br.worldBound.yMax, 1f);
            Assert.AreEqual(HudPanel.Width * 0.5f, tc.worldBound.center.x, 1f);
        }

        [UnityTest]
        public IEnumerator Zones_TitleSafeMarginMovesTheFrameIn()
        {
            _panel = HudPanel.Create();
            var zones = AddFullScreen(new HudZones { TitleSafePercent = 5f });
            var tl = zones.Add(HudZone.TopLeft, new VisualElement { name = "tl", style = { width = 50, height = 20 } });
            yield return HudPanel.Frames(3);
            Assert.AreEqual(HudPanel.Width * 0.05f, tl.worldBound.xMin, 1.5f);
            Assert.AreEqual(HudPanel.Height * 0.05f, tl.worldBound.yMin, 1.5f);
        }

        // ---- the six-widget HUD of the acceptance criteria ----

        [UnityTest]
        public IEnumerator SixWidgetHud_BuildsAndLaysOut()
        {
            _panel = HudPanel.Create();
            var zones = AddFullScreen(new HudZones { TitleSafePercent = 2f });
            var health = zones.Add(HudZone.TopLeft, new MeterBar { MaxValue = 100f, Value = 70f, LabelMode = MeterLabelMode.ValueAndMax, style = { width = 220 } });
            var coins = zones.Add(HudZone.TopRight, new CurrencyLabel());
            var timer = zones.Add(HudZone.TopCenter, new CountdownLabel());
            var skill = zones.Add(HudZone.BottomRight, new CooldownIcon());
            var charge = zones.Add(HudZone.BottomLeft, new RadialProgress { Value = 0.6f });
            var feed = zones.Add(HudZone.MiddleLeft, new TimedFeed());
            coins.SnapTo(1250);
            timer.StartIn(95d);
            skill.StartCooldown(8f);
            feed.Push("Checkpoint reached");
            yield return HudPanel.Frames(3);

            foreach (VisualElement widget in new VisualElement[] { health, coins, timer, skill, charge, feed })
            {
                Assert.IsNotNull(widget.panel, widget.GetType().Name + " is on the panel");
                Assert.Greater(widget.layout.width, 0f, widget.GetType().Name + " has a size");
            }

            Assert.AreEqual("70 / 100", health.Q<Label>("label").text);
            StringAssert.StartsWith("01:3", timer.text, "95 seconds, give or take a tick of the clock");
        }
    }
}
