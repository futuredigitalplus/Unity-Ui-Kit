using UnityEngine;
using UnityEngine.UIElements;

namespace FutureDigitalPlus.UIKit.Hud
{
    /// <summary>The nine anchor areas of <see cref="HudZones"/>.</summary>
    public enum HudZone
    {
        /// <summary>Top left corner.</summary>
        TopLeft,

        /// <summary>Top edge, centred.</summary>
        TopCenter,

        /// <summary>Top right corner.</summary>
        TopRight,

        /// <summary>Left edge, centred vertically.</summary>
        MiddleLeft,

        /// <summary>Centre of the screen.</summary>
        Center,

        /// <summary>Right edge, centred vertically.</summary>
        MiddleRight,

        /// <summary>Bottom left corner.</summary>
        BottomLeft,

        /// <summary>Bottom edge, centred.</summary>
        BottomCenter,

        /// <summary>Bottom right corner.</summary>
        BottomRight,
    }

    /// <summary>
    /// The skeleton of a HUD: a full-size, pass-through container with nine zones (corners, edges, centre) that HUD
    /// widgets are added to, plus an optional title-safe margin for TVs. Everything that is not a widget lets pointer
    /// events through to the game. Put it inside the safe area container of the shell; the title-safe margin is added
    /// on top of that. Zones are children of an inner frame that carries the margin.
    /// </summary>
    [UxmlElement]
    public partial class HudZones : VisualElement
    {
        /// <summary>USS class of the element.</summary>
        public const string UssClassName = "fdp-hud-zones";

        private static readonly string[] ZoneClasses =
        {
            "top-left", "top-center", "top-right", "middle-left", "center", "middle-right", "bottom-left", "bottom-center",
            "bottom-right",
        };

        private readonly VisualElement[] _zones = new VisualElement[ZoneClasses.Length];
        private readonly VisualElement _frame = new VisualElement { name = "frame", pickingMode = PickingMode.Ignore };
        private float _titleSafePercent;

        /// <summary>Creates the zones.</summary>
        public HudZones()
        {
            AddToClassList(UssClassName);
            pickingMode = PickingMode.Ignore;
            _frame.AddToClassList(UssClassName + "__frame");
            Add(_frame);
            for (var i = 0; i < _zones.Length; i++)
            {
                var zone = new VisualElement { name = ZoneClasses[i], pickingMode = PickingMode.Ignore };
                zone.AddToClassList(UssClassName + "__zone");
                zone.AddToClassList(UssClassName + "__zone--" + ZoneClasses[i]);
                _zones[i] = zone;
                _frame.Add(zone);
            }

            RegisterCallback<GeometryChangedEvent>(OnGeometryChanged);
        }

        /// <summary>Margin kept free at every edge as a percentage of the width / height (0 to 20; TVs use about 5).</summary>
        [UxmlAttribute]
        public float TitleSafePercent
        {
            get => _titleSafePercent;
            set
            {
                _titleSafePercent = Mathf.Clamp(value, 0f, 20f);
                ApplyMargin();
            }
        }

        /// <summary>The container of a zone. Add widgets to it.</summary>
        public VisualElement Zone(HudZone zone) => _zones[(int)zone];

        /// <summary>Adds a widget to a zone and returns it.</summary>
        public T Add<T>(HudZone zone, T widget) where T : VisualElement
        {
            _zones[(int)zone].Add(widget);
            return widget;
        }

        private void OnGeometryChanged(GeometryChangedEvent evt) => ApplyMargin();

        private void ApplyMargin()
        {
            var w = layout.width;
            var h = layout.height;
            if (float.IsNaN(w) || float.IsNaN(h)) return;
            var x = w * _titleSafePercent * 0.01f;
            var y = h * _titleSafePercent * 0.01f;
            _frame.style.left = x;
            _frame.style.right = x;
            _frame.style.top = y;
            _frame.style.bottom = y;
        }
    }
}
