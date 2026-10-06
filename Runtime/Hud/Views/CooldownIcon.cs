using System;
using System.Globalization;
using UnityEngine;
using UnityEngine.UIElements;

namespace FutureDigitalPlus.UIKit.Hud
{
    /// <summary>
    /// An ability or item icon with a cooldown: a dark wedge sweeps away clockwise as the cooldown runs down, the
    /// remaining seconds are written over the icon, and a short flash plus the <c>fdp-cooldown-icon--ready</c> class
    /// mark the moment it becomes ready. The logic is a <see cref="CooldownModel"/>; the element scheduler drives it
    /// only while a cooldown runs. Give the icon with <see cref="SetIcon"/> or through USS on
    /// <c>.fdp-cooldown-icon__icon</c>.
    /// </summary>
    [UxmlElement]
    public partial class CooldownIcon : VisualElement
    {
        /// <summary>USS class of the element.</summary>
        public const string UssClassName = "fdp-cooldown-icon";

        private const string ReadyClass = UssClassName + "--ready";
        private const string FlashClass = UssClassName + "--flash";
        private const int FlashMilliseconds = 80;

        private readonly CooldownModel _model = new CooldownModel();
        private readonly VisualElement _icon = new VisualElement { name = "icon" };
        private readonly PieSweep _sweep = new PieSweep { name = "sweep" };
        private readonly Label _text = new Label { name = "remaining" };
        private readonly VisualElement _flash = new VisualElement { name = "flash" };
        private readonly TickDriver _driver;
        private IVisualElementScheduledItem _flashEnd;
        private Action _endFlash;
        private bool _showText = true;
        private string _shownText;

        /// <summary>Creates a ready icon.</summary>
        public CooldownIcon()
        {
            AddToClassList(UssClassName);
            pickingMode = PickingMode.Ignore;
            _icon.AddToClassList(UssClassName + "__icon");
            _sweep.AddToClassList(UssClassName + "__sweep");
            _text.AddToClassList(UssClassName + "__text");
            _flash.AddToClassList(UssClassName + "__flash");
            _icon.pickingMode = _text.pickingMode = _flash.pickingMode = PickingMode.Ignore;
            Add(_icon);
            Add(_sweep);
            Add(_text);
            Add(_flash);
            _driver = new TickDriver(this, Step);
            RefreshVisuals();
        }

        /// <summary>Raised when a running cooldown reaches zero (or is reset or reduced to zero).</summary>
        public event Action Ready;

        /// <summary>True when nothing is cooling down.</summary>
        public bool IsReady => _model.IsReady;

        /// <summary>Seconds left on the cooldown.</summary>
        public float RemainingSeconds => _model.Remaining;

        /// <summary>Whether the remaining seconds are written over the icon (default true).</summary>
        [UxmlAttribute]
        public bool ShowText
        {
            get => _showText;
            set
            {
                _showText = value;
                RefreshVisuals();
            }
        }

        /// <summary>Sets the icon picture (a texture, sprite or vector image through <see cref="Background"/>).</summary>
        public void SetIcon(Background background)
        {
            _icon.style.backgroundImage = new StyleBackground(background);
        }

        /// <summary>Starts a cooldown of <paramref name="seconds"/>.</summary>
        public void StartCooldown(float seconds)
        {
            _model.Start(seconds);
            RefreshVisuals();
            if (!_model.IsReady) _driver.Request();
        }

        /// <summary>Makes the ability ready now.</summary>
        public void ResetCooldown()
        {
            if (_model.Reset()) OnBecameReady();
            RefreshVisuals();
        }

        /// <summary>Takes <paramref name="seconds"/> off the cooldown.</summary>
        public void ReduceCooldown(float seconds)
        {
            if (_model.Reduce(seconds)) OnBecameReady();
            RefreshVisuals();
        }

        internal bool Step(float deltaSeconds)
        {
            if (_model.Tick(deltaSeconds)) OnBecameReady();
            RefreshVisuals();
            return !_model.IsReady;
        }

        private void OnBecameReady()
        {
            if (!MotionGate.IsReduced(this))
            {
                AddToClassList(FlashClass);
                if (_endFlash == null) _endFlash = () => RemoveFromClassList(FlashClass);
                _flashEnd?.Pause();
                _flashEnd = schedule.Execute(_endFlash).StartingIn(FlashMilliseconds);
            }

            Ready?.Invoke();
        }

        private void RefreshVisuals()
        {
            _sweep.Fraction = _model.Fraction;
            EnableInClassList(ReadyClass, _model.IsReady);

            if (!_showText || _model.IsReady)
            {
                if (_shownText == null) return;
                _shownText = null;
                _text.text = string.Empty;
                return;
            }

            var remaining = _model.Remaining;
            var text = remaining >= 10f
                ? ((int)Math.Ceiling(remaining)).ToString(CultureInfo.InvariantCulture)
                : (Math.Ceiling(remaining * 10f) / 10d).ToString("0.0", CultureInfo.InvariantCulture);
            if (string.Equals(text, _shownText, StringComparison.Ordinal)) return;
            _shownText = text;
            _text.text = text;
        }
    }
}
