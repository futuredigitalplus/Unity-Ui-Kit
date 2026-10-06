using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace FutureDigitalPlus.UIKit.Hud
{
    /// <summary>
    /// A transparent layer that pins HUD elements to points in the 3D world (name plates, objective markers,
    /// off-screen arrows). The caller supplies the <see cref="Camera"/>, so any camera setup (including Cinemachine,
    /// which drives a normal camera) works without a dependency. Each frame, from the element scheduler, every anchor
    /// is projected into panel units (the same numbers as <c>RuntimePanelUtils.CameraTransformWorldToPanel</c> on a panel that
    /// covers the screen, and right on a panel that renders into a RenderTexture, where that Unity call is not) and placed with
    /// a transform, so no layout pass is triggered. Place the layer inside the safe area container to keep clamped anchors off the notch.
    /// </summary>
    [UxmlElement]
    public partial class WorldAnchorLayer : VisualElement
    {
        /// <summary>USS class of the element.</summary>
        public const string UssClassName = "fdp-world-anchor-layer";

        private readonly List<WorldAnchor> _anchors = new List<WorldAnchor>(8);
        private readonly TickDriver _driver;
        private float _edgeMargin = 24f;
        private float _arrowDistance = 18f;

        /// <summary>Creates an empty layer.</summary>
        public WorldAnchorLayer()
        {
            AddToClassList(UssClassName);
            pickingMode = PickingMode.Ignore;
            _driver = new TickDriver(this, Step);
        }

        /// <summary>The camera used to project the anchors. Anchors are hidden while it is null.</summary>
        public Camera Camera { get; set; }

        /// <summary>Distance kept between a clamped anchor and the layer's edge (default 24).</summary>
        [UxmlAttribute]
        public float EdgeMargin
        {
            get => _edgeMargin;
            set => _edgeMargin = Mathf.Max(0f, value);
        }

        /// <summary>Distance of the direction arrow from a clamped anchor's centre (default 18).</summary>
        [UxmlAttribute]
        public float ArrowDistance
        {
            get => _arrowDistance;
            set => _arrowDistance = Mathf.Max(0f, value);
        }

        /// <summary>Number of anchors.</summary>
        public int Count => _anchors.Count;

        /// <summary>Pins <paramref name="content"/> to <paramref name="target"/> (plus <paramref name="worldOffset"/>).</summary>
        public WorldAnchor Pin(VisualElement content, Transform target, Vector3 worldOffset = default,
            OffScreenMode mode = OffScreenMode.Hide, AnchorPivot pivot = AnchorPivot.Center)
        {
            return AddAnchor(new WorldAnchor(content, target, Vector3.zero, worldOffset, mode, pivot));
        }

        /// <summary>Pins <paramref name="content"/> to a fixed world position.</summary>
        public WorldAnchor Pin(VisualElement content, Vector3 worldPosition,
            OffScreenMode mode = OffScreenMode.Hide, AnchorPivot pivot = AnchorPivot.Center)
        {
            return AddAnchor(new WorldAnchor(content, null, worldPosition, Vector3.zero, mode, pivot));
        }

        /// <summary>Unpins an anchor and removes its element from the layer. Returns false when it is not on this layer.</summary>
        public bool Remove(WorldAnchor anchor)
        {
            if (anchor == null || !_anchors.Remove(anchor)) return false;
            anchor.Holder.RemoveFromHierarchy();
            if (_anchors.Count == 0) _driver.Stop();
            return true;
        }

        /// <summary>Removes every anchor.</summary>
        public void RemoveAll()
        {
            for (var i = 0; i < _anchors.Count; i++) _anchors[i].Holder.RemoveFromHierarchy();
            _anchors.Clear();
            _driver.Stop();
        }

        /// <summary>Projects every anchor now. The scheduler calls this each frame; call it directly after moving the camera in the same frame.</summary>
        public void UpdateAnchors()
        {
            Step(0f);
        }

        internal bool Step(float deltaSeconds)
        {
            var camera = Camera;
            var panel = this.panel;
            var rect = contentRect;
            var canProject = camera != null && panel != null && rect.width > 0f && rect.height > 0f;

            for (var i = 0; i < _anchors.Count; i++)
            {
                var anchor = _anchors[i];
                if (!canProject || anchor.TargetLost)
                {
                    anchor.Holder.style.display = DisplayStyle.None;
                    continue;
                }

                var world = anchor.CurrentWorldPosition;
                var depth = camera.WorldToScreenPoint(world).z;
                var projected = this.WorldToLocal(ProjectToPanel(panel, camera, world));
                var result = WorldAnchorMath.Resolve(projected.x, projected.y, depth, rect.xMin, rect.yMin, rect.xMax,
                    rect.yMax, _edgeMargin, anchor.Mode);
                anchor.Result = result;

                anchor.Holder.style.display = result.Show ? DisplayStyle.Flex : DisplayStyle.None;
                if (!result.Show) continue;

                anchor.Holder.style.translate = new StyleTranslate(new Translate(result.X, result.Y));
                var showArrow = result.Visibility != AnchorVisibility.OnScreen;
                anchor.Arrow.style.display = showArrow ? DisplayStyle.Flex : DisplayStyle.None;
                if (!showArrow) continue;

                var radians = result.ArrowAngleDegrees * Mathf.Deg2Rad;
                anchor.Arrow.style.translate = new StyleTranslate(new Translate(
                    Mathf.Cos(radians) * _arrowDistance, Mathf.Sin(radians) * _arrowDistance));
                anchor.Arrow.style.rotate = new StyleRotate(new Rotate(Angle.Degrees(result.ArrowAngleDegrees)));
            }

            return _anchors.Count > 0;
        }

        // RuntimePanelUtils.CameraTransformWorldToPanel flips the screen position with Screen.height, which is only right for a panel that
        // covers the screen. The viewport fraction of the camera's target, scaled to the panel's own size, is right for every panel.
        internal static Vector2 ProjectToPanel(IPanel panel, Camera camera, Vector3 world)
        {
            var size = panel.visualTree.layout.size;
            if (float.IsNaN(size.x) || size.x <= 0f || size.y <= 0f)
                return RuntimePanelUtils.CameraTransformWorldToPanel(panel, world, camera);

            var target = camera.targetTexture;
            var targetWidth = target != null ? target.width : Screen.width;
            var targetHeight = target != null ? target.height : Screen.height;
            var screen = camera.WorldToScreenPoint(world);
            return new Vector2(screen.x / targetWidth * size.x, size.y - screen.y / targetHeight * size.y);
        }

        private WorldAnchor AddAnchor(WorldAnchor anchor)
        {
            _anchors.Add(anchor);
            Add(anchor.Holder);
            anchor.Holder.style.display = DisplayStyle.None;
            _driver.Request();
            return anchor;
        }
    }
}
