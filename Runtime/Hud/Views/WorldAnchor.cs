using UnityEngine;
using UnityEngine.UIElements;

namespace FutureDigitalPlus.UIKit.Hud
{
    /// <summary>Where an anchored element sits relative to its projected point.</summary>
    public enum AnchorPivot
    {
        /// <summary>The element is centred on the point.</summary>
        Center,

        /// <summary>The element's bottom edge is at the point (a name plate above a character).</summary>
        Above,

        /// <summary>The element's top edge is at the point.</summary>
        Below,
    }

    /// <summary>
    /// One element pinned to a point in the 3D world by a <see cref="WorldAnchorLayer"/>: it follows a
    /// <see cref="Transform"/> (or a fixed world position) plus an offset, hides or clamps to the screen edge when the
    /// point is off screen, and shows a direction arrow while clamped. Create it with <see cref="WorldAnchorLayer.Pin(VisualElement, Transform, Vector3, OffScreenMode, AnchorPivot)"/>.
    /// </summary>
    public sealed class WorldAnchor
    {
        private Transform _target;
        private bool _hadTarget;

        internal WorldAnchor(VisualElement content, Transform target, Vector3 worldPosition, Vector3 offset, OffScreenMode mode, AnchorPivot pivot)
        {
            Content = content;
            Target = target;
            WorldPosition = worldPosition;
            Offset = offset;
            Mode = mode;
            Holder = new VisualElement { pickingMode = PickingMode.Ignore };
            Holder.AddToClassList("fdp-world-anchor");
            var wrapper = new VisualElement { pickingMode = PickingMode.Ignore };
            wrapper.AddToClassList("fdp-world-anchor__content");
            Pivot = pivot;
            wrapper.EnableInClassList("fdp-world-anchor__content--above", pivot == AnchorPivot.Above);
            wrapper.EnableInClassList("fdp-world-anchor__content--below", pivot == AnchorPivot.Below);
            wrapper.Add(content);
            Holder.Add(wrapper);
            Arrow = new ArrowElement();
            Arrow.AddToClassList("fdp-world-anchor__arrow");
            Arrow.style.display = DisplayStyle.None;
            Holder.Add(Arrow);
        }

        internal VisualElement Holder { get; }

        internal VisualElement Arrow { get; }

        /// <summary>The element that is pinned.</summary>
        public VisualElement Content { get; }

        /// <summary>The transform followed, or null when the anchor sits at <see cref="WorldPosition"/>.</summary>
        public Transform Target
        {
            get => _target;
            set
            {
                _target = value;
                _hadTarget = value != null;
            }
        }

        /// <summary>The world position used when <see cref="Target"/> is null.</summary>
        public Vector3 WorldPosition { get; set; }

        /// <summary>World-space offset added to the target position (for example <c>Vector3.up * 2</c> above a head).</summary>
        public Vector3 Offset { get; set; }

        /// <summary>What to do when the point is off screen.</summary>
        public OffScreenMode Mode { get; set; }

        /// <summary>Where the content sits relative to the point.</summary>
        public AnchorPivot Pivot { get; }

        /// <summary>The result of the last update.</summary>
        public AnchorResult Result { get; internal set; }

        /// <summary>True when the anchor followed a transform that has been destroyed since. Such an anchor is hidden; it must not jump to <see cref="WorldPosition"/>.</summary>
        internal bool TargetLost => _hadTarget && _target == null;

        internal Vector3 CurrentWorldPosition => (_target != null ? _target.position : WorldPosition) + Offset;
    }

    /// <summary>A small triangle that points right at rotation 0; the colour is the USS <c>color</c>.</summary>
    internal sealed class ArrowElement : VisualElement
    {
        public ArrowElement()
        {
            pickingMode = PickingMode.Ignore;
            generateVisualContent += OnGenerateVisualContent;
            RegisterCallback<CustomStyleResolvedEvent>(_ => MarkDirtyRepaint());
        }

        private void OnGenerateVisualContent(MeshGenerationContext context)
        {
            var rect = contentRect;
            if (rect.width <= 0f || rect.height <= 0f) return;
            var painter = context.painter2D;
            painter.fillColor = resolvedStyle.color;
            painter.BeginPath();
            painter.MoveTo(new Vector2(rect.xMin, rect.yMin));
            painter.LineTo(new Vector2(rect.xMax, rect.center.y));
            painter.LineTo(new Vector2(rect.xMin, rect.yMax));
            painter.ClosePath();
            painter.Fill();
        }
    }
}
