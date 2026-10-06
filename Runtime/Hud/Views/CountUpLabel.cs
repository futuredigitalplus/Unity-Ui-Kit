using System;
using System.Globalization;
using UnityEngine.UIElements;

namespace FutureDigitalPlus.UIKit.Hud
{
    /// <summary>
    /// A label for a number that rolls from its old value to a new one (score, coins, damage dealt). Text is rewritten
    /// only when the shown whole number changes; the element scheduler drives the roll only while it runs, using
    /// unscaled time so it also rolls while the game is paused. Reduced motion, or a label that is not on a panel yet,
    /// changes the value at once.
    /// </summary>
    [UxmlElement]
    public partial class CountUpLabel : Label
    {
        /// <summary>USS class of the element.</summary>
        public const string UssClassName = "fdp-count-up-label";

        private readonly CountUpModel _model = new CountUpModel();
        private readonly TickDriver _driver;
        private double _value;
        private float _duration = 0.6f;
        private NumberFormatStyle _format = NumberFormatStyle.Grouped;
        private string _prefix = string.Empty;
        private string _suffix = string.Empty;
        private long _shown = long.MinValue;

        /// <summary>Creates a label showing 0.</summary>
        public CountUpLabel()
        {
            AddToClassList(UssClassName);
            pickingMode = PickingMode.Ignore;
            _driver = new TickDriver(this, Step);
            Render(true);
        }

        /// <summary>The number to show. Setting it rolls from the current number over <see cref="DurationSeconds"/>.</summary>
        [UxmlAttribute]
        public double Value
        {
            get => _value;
            set
            {
                if (value.Equals(_value) && !_model.IsRunning) return;
                _value = value;
                if (panel == null || _duration <= 0f || MotionGate.IsReduced(this))
                {
                    _model.Snap(value);
                    Render(false);
                    return;
                }

                _model.AnimateTo(value, _duration);
                _driver.Request();
            }
        }

        /// <summary>Seconds a roll takes (default 0.6); 0 changes at once.</summary>
        [UxmlAttribute]
        public float DurationSeconds
        {
            get => _duration;
            set => _duration = Math.Max(0f, value);
        }

        /// <summary>How the number is written (default grouped).</summary>
        [UxmlAttribute]
        public NumberFormatStyle Format
        {
            get => _format;
            set
            {
                _format = value;
                Render(true);
            }
        }

        /// <summary>Text written before the number (for example <c>x</c> or <c>$</c>).</summary>
        [UxmlAttribute]
        public string Prefix
        {
            get => _prefix;
            set
            {
                _prefix = value ?? string.Empty;
                Render(true);
            }
        }

        /// <summary>Text written after the number (for example <c> pts</c>).</summary>
        [UxmlAttribute]
        public string Suffix
        {
            get => _suffix;
            set
            {
                _suffix = value ?? string.Empty;
                Render(true);
            }
        }

        /// <summary>Culture for grouping and decimals; <c>null</c> is the invariant culture.</summary>
        public CultureInfo Culture { get; set; }

        /// <summary>The number currently on screen (between the old and the new value while rolling).</summary>
        public double DisplayedValue => _model.Current;

        /// <summary>Jumps to <paramref name="value"/> without a roll.</summary>
        public void SnapTo(double value)
        {
            _value = value;
            _model.Snap(value);
            Render(false);
        }

        internal bool Step(float deltaSeconds)
        {
            _model.Tick(deltaSeconds);
            Render(false);
            return _model.IsRunning;
        }

        private void Render(bool force)
        {
            var rounded = (long)Math.Round(_model.Current, MidpointRounding.AwayFromZero);
            if (!force && rounded == _shown) return;
            _shown = rounded;
            text = _prefix + NumberFormatter.Format(rounded, _format, Culture) + _suffix;
        }
    }
}
