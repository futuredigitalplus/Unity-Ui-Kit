using System;
using System.Globalization;
using UnityEngine.UIElements;

namespace FutureDigitalPlus.UIKit.Hud
{
    /// <summary>
    /// A currency counter: an icon, a rolling number (<see cref="CountUpLabel"/>) and a small "+25" / "-10" note that
    /// pops up next to it when the value changes and fades away. Give the icon with <see cref="SetIcon"/> or through
    /// USS on <c>.fdp-currency-label__icon</c>.
    /// </summary>
    [UxmlElement]
    public partial class CurrencyLabel : VisualElement
    {
        /// <summary>USS class of the element.</summary>
        public const string UssClassName = "fdp-currency-label";

        private const string ShowClass = UssClassName + "__delta--show";
        private const string GainClass = UssClassName + "__delta--gain";
        private const string LossClass = UssClassName + "__delta--loss";

        private readonly VisualElement _icon = new VisualElement { name = "icon" };
        private readonly CountUpLabel _count = new CountUpLabel { name = "count" };
        private readonly Label _delta = new Label { name = "delta" };
        private Action _hideDelta;
        private IVisualElementScheduledItem _hide;
        private long _value;
        private int _deltaMilliseconds = 900;
        private bool _showDelta = true;

        /// <summary>Creates a counter showing 0.</summary>
        public CurrencyLabel()
        {
            AddToClassList(UssClassName);
            pickingMode = PickingMode.Ignore;
            _icon.AddToClassList(UssClassName + "__icon");
            _count.AddToClassList(UssClassName + "__count");
            _delta.AddToClassList(UssClassName + "__delta");
            _icon.pickingMode = _delta.pickingMode = PickingMode.Ignore;
            Add(_icon);
            Add(_count);
            Add(_delta);
        }

        /// <summary>The amount. Setting it rolls the number and, when it changed, pops the change note.</summary>
        [UxmlAttribute]
        public long Value
        {
            get => _value;
            set
            {
                var change = value - _value;
                _value = value;
                _count.Value = value;
                if (change != 0L && _showDelta && panel != null) PopDelta(change);
            }
        }

        /// <summary>Whether the change note is shown (default true).</summary>
        [UxmlAttribute]
        public bool ShowDelta
        {
            get => _showDelta;
            set => _showDelta = value;
        }

        /// <summary>Seconds the number takes to roll (default 0.6).</summary>
        [UxmlAttribute]
        public float DurationSeconds
        {
            get => _count.DurationSeconds;
            set => _count.DurationSeconds = value;
        }

        /// <summary>How the number is written (default grouped).</summary>
        [UxmlAttribute]
        public NumberFormatStyle Format
        {
            get => _count.Format;
            set => _count.Format = value;
        }

        /// <summary>Milliseconds the change note stays before it fades (default 900).</summary>
        [UxmlAttribute]
        public int DeltaMilliseconds
        {
            get => _deltaMilliseconds;
            set => _deltaMilliseconds = Math.Max(0, value);
        }

        /// <summary>Culture for the numbers; <c>null</c> is the invariant culture.</summary>
        public CultureInfo Culture
        {
            get => _count.Culture;
            set => _count.Culture = value;
        }

        /// <summary>Sets the icon picture.</summary>
        public void SetIcon(Background background)
        {
            _icon.style.backgroundImage = new StyleBackground(background);
        }

        /// <summary>Jumps to <paramref name="value"/> without a roll or a change note.</summary>
        public void SnapTo(long value)
        {
            _value = value;
            _count.SnapTo(value);
        }

        private void PopDelta(long change)
        {
            _delta.text = NumberFormatter.Signed(change, Culture);
            _delta.EnableInClassList(GainClass, change > 0L);
            _delta.EnableInClassList(LossClass, change < 0L);
            _delta.AddToClassList(ShowClass);
            if (_hideDelta == null) _hideDelta = () => _delta.RemoveFromClassList(ShowClass);
            _hide?.Pause();
            _hide = schedule.Execute(_hideDelta).StartingIn(_deltaMilliseconds);
        }
    }
}
