using System;
using System.Globalization;
using UnityEngine.UIElements;

namespace FutureDigitalPlus.UIKit.Hud
{
    /// <summary>What a <see cref="MeterBar"/> writes on the bar.</summary>
    public enum MeterLabelMode
    {
        /// <summary>No text.</summary>
        None,

        /// <summary>The value (<c>184</c>).</summary>
        Value,

        /// <summary>Value and maximum (<c>184 / 240</c>).</summary>
        ValueAndMax,

        /// <summary>The fill as a percentage (<c>76%</c>).</summary>
        Percent,
    }

    /// <summary>Height of a <see cref="MeterBar"/>, matching the design system meter sizes.</summary>
    public enum MeterBarSize
    {
        /// <summary>20 px, with room for a label.</summary>
        Default,

        /// <summary>10 px status pip.</summary>
        Small,

        /// <summary>24 px, for boss or cast bars.</summary>
        Large,
    }

    /// <summary>
    /// A HUD value bar on the design system meter skin (<c>ds-meter</c>): value and maximum, a label, optional
    /// segments, a damage trail that lingers behind a decrease, and warning / danger colours with hysteresis. The fill
    /// moves by the skin's width transition; the trail is driven by the element scheduler only while it moves, so a
    /// resting bar does no work. Call <see cref="HudStyles.Install"/> for the extra styling.
    /// </summary>
    [UxmlElement]
    public partial class MeterBar : VisualElement
    {
        /// <summary>USS class of the element.</summary>
        public const string UssClassName = "fdp-meter-bar";

        private readonly MeterModel _model = new MeterModel(100f);
        private readonly VisualElement _trail = new VisualElement { name = "trail" };
        private readonly VisualElement _fill = new VisualElement { name = "fill" };
        private readonly VisualElement _segments = new VisualElement { name = "segments" };
        private readonly Label _label = new Label { name = "label" };
        private readonly TickDriver _driver;

        private float _value = 100f;
        private float _max = 100f;
        private MeterLabelMode _labelMode = MeterLabelMode.None;
        private MeterBarSize _size = MeterBarSize.Default;
        private bool _showTrail = true;
        private int _segmentCount;
        private long _shownValue = long.MinValue;
        private long _shownMax = long.MinValue;
        private MeterLabelMode _shownMode = (MeterLabelMode)(-1);

        /// <summary>Creates a full bar with a maximum of 100.</summary>
        public MeterBar()
        {
            AddToClassList("ds-meter");
            AddToClassList(UssClassName);
            pickingMode = PickingMode.Ignore;

            _trail.AddToClassList("fdp-meter-bar__trail");
            _fill.AddToClassList("ds-meter__fill");
            _segments.AddToClassList("fdp-meter-bar__segments");
            _label.AddToClassList("ds-meter__label");
            _trail.pickingMode = _fill.pickingMode = _segments.pickingMode = _label.pickingMode = PickingMode.Ignore;
            Add(_trail);
            Add(_fill);
            Add(_segments);
            Add(_label);

            _driver = new TickDriver(this, Step);
            Refresh();
        }

        /// <summary>Current value, clamped to 0 .. <see cref="MaxValue"/>. A decrease starts the damage trail.</summary>
        [UxmlAttribute]
        public float Value
        {
            get => _model.Value;
            set
            {
                _value = value;
                _model.SetValue(value);
                Refresh();
            }
        }

        /// <summary>Maximum value. Changing it moves the bar without a trail.</summary>
        [UxmlAttribute]
        public float MaxValue
        {
            get => _max;
            set
            {
                _max = Math.Max(0f, value);
                _model.Set(_value, _max);
                Refresh();
            }
        }

        /// <summary>What is written on the bar.</summary>
        [UxmlAttribute]
        public MeterLabelMode LabelMode
        {
            get => _labelMode;
            set
            {
                _labelMode = value;
                Refresh();
            }
        }

        /// <summary>Height of the bar.</summary>
        [UxmlAttribute]
        public MeterBarSize Size
        {
            get => _size;
            set
            {
                _size = value;
                EnableInClassList("ds-meter--sm", value == MeterBarSize.Small);
                EnableInClassList("ds-meter--lg", value == MeterBarSize.Large);
            }
        }

        /// <summary>Whether a decrease leaves a damage trail (default true). Reduced motion turns the trail off.</summary>
        [UxmlAttribute]
        public bool ShowTrail
        {
            get => _showTrail;
            set
            {
                _showTrail = value;
                Refresh();
            }
        }

        /// <summary>Number of equal segments drawn as thin dividers (0 or 1 for none).</summary>
        [UxmlAttribute]
        public int Segments
        {
            get => _segmentCount;
            set
            {
                value = Math.Max(0, value);
                if (value == _segmentCount) return;
                _segmentCount = value;
                RebuildSegments();
            }
        }

        /// <summary>True when a high value is the bad end (heat, overload): warning and danger apply to the top of the bar.</summary>
        [UxmlAttribute]
        public bool HighIsBad
        {
            get => _model.Mode == MeterThresholdMode.HighIsBad;
            set
            {
                _model.Mode = value ? MeterThresholdMode.HighIsBad : MeterThresholdMode.LowIsBad;
                _model.Set(_value, _max);
                Refresh();
            }
        }

        /// <summary>Fraction (of the good side) below which the bar turns to the warning colour (default 0.5).</summary>
        [UxmlAttribute]
        public float WarningThreshold
        {
            get => _model.WarningThreshold;
            set
            {
                _model.WarningThreshold = value;
                _model.Set(_value, _max);
                Refresh();
            }
        }

        /// <summary>Fraction (of the good side) below which the bar turns to the danger colour (default 0.25).</summary>
        [UxmlAttribute]
        public float DangerThreshold
        {
            get => _model.DangerThreshold;
            set
            {
                _model.DangerThreshold = value;
                _model.Set(_value, _max);
                Refresh();
            }
        }

        /// <summary>Culture used for the label numbers; <c>null</c> is the invariant culture.</summary>
        public CultureInfo Culture { get; set; }

        /// <summary>Current warning level.</summary>
        public MeterLevel Level => _model.Level;

        /// <summary>Sets value and maximum in one step.</summary>
        public void Set(float value, float max)
        {
            _value = value;
            _max = Math.Max(0f, max);
            _model.Set(value, _max);
            Refresh();
        }

        /// <summary>Advances the damage trail by <paramref name="deltaSeconds"/>. The scheduler calls this; tests and manual drivers may too.</summary>
        internal bool Step(float deltaSeconds)
        {
            var moving = _model.Tick(deltaSeconds);
            ApplyTrail();
            return moving;
        }

        private bool TrailEnabled => _showTrail && !MotionGate.IsReduced(this);

        private void Refresh()
        {
            _fill.style.width = new StyleLength(Length.Percent(_model.Fraction * 100f));
            var level = _model.Level;
            _fill.EnableInClassList("is-warning", level == MeterLevel.Warning);
            _fill.EnableInClassList("is-danger", level == MeterLevel.Danger);

            if (TrailEnabled)
            {
                _trail.style.display = DisplayStyle.Flex;
                ApplyTrail();
                if (_model.IsTrailMoving) _driver.Request();
            }
            else
            {
                _trail.style.display = DisplayStyle.None;
            }

            RefreshLabel();
        }

        private void ApplyTrail()
        {
            _trail.style.width = new StyleLength(Length.Percent(_model.TrailFraction * 100f));
        }

        private void RefreshLabel()
        {
            if (_labelMode == MeterLabelMode.None)
            {
                if (_shownMode == MeterLabelMode.None) return;
                _shownMode = MeterLabelMode.None;
                _label.style.display = DisplayStyle.None;
                return;
            }

            var value = (long)Math.Round(_model.Value, MidpointRounding.AwayFromZero);
            var max = (long)Math.Round(_model.Max, MidpointRounding.AwayFromZero);
            if (_shownMode == _labelMode && _shownValue == value && _shownMax == max) return;

            _shownMode = _labelMode;
            _shownValue = value;
            _shownMax = max;
            _label.style.display = DisplayStyle.Flex;
            switch (_labelMode)
            {
                case MeterLabelMode.Value:
                    _label.text = NumberFormatter.Group(value, Culture);
                    break;
                case MeterLabelMode.ValueAndMax:
                    _label.text = NumberFormatter.Group(value, Culture) + " / " + NumberFormatter.Group(max, Culture);
                    break;
                default:
                    _label.text = ((long)Math.Round(_model.Fraction * 100d, MidpointRounding.AwayFromZero)).ToString(CultureInfo.InvariantCulture) + "%";
                    break;
            }
        }

        private void RebuildSegments()
        {
            _segments.Clear();
            for (var i = 1; i < _segmentCount; i++)
            {
                var divider = new VisualElement { pickingMode = PickingMode.Ignore };
                divider.AddToClassList("fdp-meter-bar__divider");
                divider.style.left = new StyleLength(Length.Percent(100f * i / _segmentCount));
                _segments.Add(divider);
            }
        }
    }
}
