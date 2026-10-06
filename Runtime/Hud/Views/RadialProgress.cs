using System;
using UnityEngine;
using UnityEngine.UIElements;

namespace FutureDigitalPlus.UIKit.Hud
{
    /// <summary>
    /// A circular progress ring drawn with <c>Painter2D</c>. The ring colour is the element's USS <c>color</c> and
    /// the track colour its <c>-unity-background-image-tint-color</c>, so a theme styles it with ordinary tokens
    /// (<see cref="HudStyles"/> maps them to the primary and elevated-surface colours). The ring repaints only when
    /// the value, a style or the size changes; an optional smoothing animation runs from the element scheduler only
    /// while the ring is moving.
    /// </summary>
    [UxmlElement]
    public partial class RadialProgress : VisualElement
    {
        /// <summary>USS class of the element.</summary>
        public const string UssClassName = "fdp-radial-progress";

        private readonly TickDriver _driver;
        private float _value;
        private float _shown;
        private float _thickness = 6f;
        private float _startAngle = -90f;
        private bool _clockwise = true;
        private float _smoothSeconds;

        /// <summary>Creates an empty ring.</summary>
        public RadialProgress()
        {
            AddToClassList(UssClassName);
            pickingMode = PickingMode.Ignore;
            generateVisualContent += OnGenerateVisualContent;
            RegisterCallback<CustomStyleResolvedEvent>(OnStyleResolved);
            _driver = new TickDriver(this, Step);
        }

        /// <summary>Progress from 0 to 1.</summary>
        [UxmlAttribute]
        public float Value
        {
            get => _value;
            set
            {
                value = float.IsNaN(value) ? 0f : Mathf.Clamp01(value);   // 0 / 0 is a common way to get NaN; it would keep the driver repainting for ever
                if (Mathf.Approximately(value, _value)) return;
                _value = value;
                if (_smoothSeconds > 0f && !MotionGate.IsReduced(this)) _driver.Request();
                else Snap();
            }
        }

        /// <summary>Ring width in panel units.</summary>
        [UxmlAttribute]
        public float Thickness
        {
            get => _thickness;
            set
            {
                _thickness = Mathf.Max(0f, value);
                MarkDirtyRepaint();
            }
        }

        /// <summary>Where the ring starts, in degrees: 0 is the right, -90 (default) the top, 90 the bottom.</summary>
        [UxmlAttribute]
        public float StartAngle
        {
            get => _startAngle;
            set
            {
                _startAngle = value;
                MarkDirtyRepaint();
            }
        }

        /// <summary>True (default) when the ring fills clockwise.</summary>
        [UxmlAttribute]
        public bool Clockwise
        {
            get => _clockwise;
            set
            {
                _clockwise = value;
                MarkDirtyRepaint();
            }
        }

        /// <summary>Seconds a full-ring change takes to animate; 0 (default) changes at once. Reduced motion always changes at once.</summary>
        [UxmlAttribute]
        public float SmoothSeconds
        {
            get => _smoothSeconds;
            set => _smoothSeconds = Mathf.Max(0f, value);
        }

        internal bool Step(float deltaSeconds)
        {
            var step = _smoothSeconds > 0f ? deltaSeconds / _smoothSeconds : 1f;
            _shown = Mathf.MoveTowards(_shown, _value, step);
            MarkDirtyRepaint();
            return !Mathf.Approximately(_shown, _value);
        }

        private void Snap()
        {
            _shown = _value;
            MarkDirtyRepaint();
        }

        private void OnStyleResolved(CustomStyleResolvedEvent evt) => MarkDirtyRepaint();

        private void OnGenerateVisualContent(MeshGenerationContext context)
        {
            var rect = contentRect;
            var radius = Mathf.Min(rect.width, rect.height) * 0.5f - _thickness * 0.5f;
            if (radius <= 0f || _thickness <= 0f) return;

            var painter = context.painter2D;
            var center = rect.center;
            painter.lineWidth = _thickness;
            painter.lineCap = LineCap.Butt;

            var track = resolvedStyle.unityBackgroundImageTintColor;
            if (track.a > 0f)
            {
                painter.strokeColor = track;
                painter.BeginPath();
                painter.Arc(center, radius, Angle.Degrees(0f), Angle.Degrees(360f));
                painter.Stroke();
            }

            if (_shown <= 0f) return;
            var sweep = Mathf.Min(_shown * 360f, 359.9f);
            painter.strokeColor = resolvedStyle.color;
            painter.BeginPath();
            painter.Arc(center, radius, Angle.Degrees(_startAngle),
                Angle.Degrees(_startAngle + (_clockwise ? sweep : -sweep)),
                _clockwise ? ArcDirection.Clockwise : ArcDirection.CounterClockwise);
            painter.Stroke();
        }
    }
}
