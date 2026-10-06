using System;
using UnityEngine;
using UnityEngine.UIElements;

namespace FutureDigitalPlus.UIKit.Hud
{
    /// <summary>
    /// A label that counts down to a moment (event end, daily reset, respawn). It reads time from <see cref="Clock"/>
    /// (unscaled game time by default; use <see cref="UtcSeconds"/> for server or wall-clock deadlines), rewrites its
    /// text only when the displayed second changes, and checks the clock four times a second from the element
    /// scheduler, only while a countdown runs.
    /// </summary>
    [UxmlElement]
    public partial class CountdownLabel : Label
    {
        /// <summary>USS class of the element.</summary>
        public const string UssClassName = "fdp-countdown-label";

        private readonly CountdownModel _model;
        private readonly TickDriver _driver;
        private CountdownStyle _style = CountdownStyle.Compact;
        private CountdownUnits _units = CountdownUnits.Default;
        private string _expiredText;
        private long _shown = long.MinValue;
        private Func<double> _clock = DefaultClock;

        /// <summary>Creates a stopped countdown that shows <c>0s</c>.</summary>
        public CountdownLabel()
        {
            AddToClassList(UssClassName);
            pickingMode = PickingMode.Ignore;
            _model = new CountdownModel(ReadClock);
            _model.Expired += OnExpired;
            _driver = new TickDriver(this, Step, 250);
            Render(true);
        }

        /// <summary>Raised once when the countdown reaches zero.</summary>
        public event Action Expired;

        /// <summary>Wall-clock seconds since the Unix epoch (UTC). Assign it to <see cref="Clock"/> for server deadlines.</summary>
        public static double UtcSeconds() => DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() / 1000d;

        /// <summary>Where time comes from, in seconds. Default: <c>Time.unscaledTimeAsDouble</c>. Set it before starting.</summary>
        public Func<double> Clock
        {
            get => _clock;
            set => _clock = value ?? DefaultClock;
        }

        /// <summary>How the time is written.</summary>
        [UxmlAttribute]
        public CountdownStyle Format
        {
            get => _style;
            set
            {
                _style = value;
                Render(true);
            }
        }

        /// <summary>Text shown after expiry; <c>null</c> keeps the formatted zero (<c>0s</c>).</summary>
        [UxmlAttribute]
        public string ExpiredText
        {
            get => _expiredText;
            set
            {
                _expiredText = value;
                Render(true);
            }
        }

        /// <summary>Unit suffixes of the compact style (replace to localize).</summary>
        public CountdownUnits Units
        {
            get => _units;
            set
            {
                _units = value;
                Render(true);
            }
        }

        /// <summary>True while the countdown runs.</summary>
        public bool IsRunning => _model.IsRunning;

        /// <summary>Seconds left.</summary>
        public double Remaining => _model.Remaining;

        /// <summary>Starts a countdown that ends <paramref name="durationSeconds"/> from now.</summary>
        public void StartIn(double durationSeconds)
        {
            _model.Start(durationSeconds);
            Begin();
        }

        /// <summary>Starts a countdown that ends at <paramref name="endTime"/> on the <see cref="Clock"/> timeline.</summary>
        public void StartAt(double endTime)
        {
            _model.StartAt(endTime);
            Begin();
        }

        /// <summary>Stops the countdown without raising <see cref="Expired"/>.</summary>
        public void Stop()
        {
            _model.Stop();
            _driver.Stop();
            Render(true);
        }

        internal bool Step(float deltaSeconds)
        {
            _model.Poll();
            Render(false);
            return _model.IsRunning;
        }

        private static double DefaultClock() => Time.unscaledTimeAsDouble;

        private double ReadClock() => _clock();

        private void Begin()
        {
            Render(true);
            if (_model.Poll()) Render(true);
            if (_model.IsRunning) _driver.Request();
        }

        private void OnExpired()
        {
            Render(true);
            Expired?.Invoke();
        }

        private void Render(bool force)
        {
            var whole = _model.RemainingWholeSeconds;
            if (!force && whole == _shown) return;
            _shown = whole;
            text = _model.IsExpired && _expiredText != null
                ? _expiredText
                : CountdownModel.Format(_model.Remaining, _style, _units);
        }
    }
}
