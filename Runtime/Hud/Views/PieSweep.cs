using UnityEngine;
using UnityEngine.UIElements;

namespace FutureDigitalPlus.UIKit.Hud
{
    /// <summary>
    /// A filled pie wedge over a rectangle, used as the cooldown sweep: the wedge starts at the top and covers
    /// <see cref="Fraction"/> of the circle clockwise. The colour is the USS <c>-unity-background-image-tint-color</c>.
    /// The pie is as large as the rectangle's diagonal, so it covers square corners; clip it with <c>overflow: hidden</c>.
    /// </summary>
    internal sealed class PieSweep : VisualElement
    {
        private float _fraction;

        public PieSweep()
        {
            pickingMode = PickingMode.Ignore;
            generateVisualContent += OnGenerateVisualContent;
            RegisterCallback<CustomStyleResolvedEvent>(_ => MarkDirtyRepaint());
        }

        public float Fraction
        {
            get => _fraction;
            set
            {
                value = Mathf.Clamp01(value);
                if (Mathf.Approximately(value, _fraction)) return;
                _fraction = value;
                MarkDirtyRepaint();
            }
        }

        private void OnGenerateVisualContent(MeshGenerationContext context)
        {
            var color = resolvedStyle.unityBackgroundImageTintColor;
            var rect = contentRect;
            if (_fraction <= 0f || color.a <= 0f || rect.width <= 0f || rect.height <= 0f) return;

            var painter = context.painter2D;
            var center = rect.center;
            var radius = Mathf.Sqrt(rect.width * rect.width + rect.height * rect.height) * 0.5f;
            painter.fillColor = color;
            painter.BeginPath();
            painter.MoveTo(center);
            if (_fraction >= 0.9999f)
            {
                painter.Arc(center, radius, Angle.Degrees(0f), Angle.Degrees(360f));
            }
            else
            {
                painter.Arc(center, radius, Angle.Degrees(-90f), Angle.Degrees(-90f + _fraction * 360f));
            }

            painter.ClosePath();
            painter.Fill();
        }
    }
}
