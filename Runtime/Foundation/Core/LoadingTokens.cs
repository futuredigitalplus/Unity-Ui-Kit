using System;
using System.Collections.Generic;

namespace FutureDigitalPlus.UIKit.Foundation
{
    /// <summary>
    /// Reference-counted loading state with anti-flicker timing: the overlay becomes visible only after
    /// <see cref="ShowDelaySeconds"/> (fast work never flashes it) and then stays for at least <see cref="MinVisibleSeconds"/>. Input is blocked
    /// from the moment a blocking token begins, not when the overlay appears, so a tap during the delay cannot start a second action.
    /// Pure: time is injected, nothing is polled. The view calls <see cref="Tick"/> when <see cref="NextDueSeconds"/> says a visual change is due.
    /// </summary>
    public sealed class LoadingTokens
    {
        /// <summary>Default delay before the overlay graphic appears.</summary>
        public const float DefaultShowDelaySeconds = 0.25f;
        /// <summary>Default minimum time the overlay stays once it appeared.</summary>
        public const float DefaultMinVisibleSeconds = 0.4f;

        sealed class Token
        {
            public int Id;
            public UiText Message;
            public bool BlockInput;
            public bool ShowSpinner = true;
            public float Progress = -1f;
        }

        readonly Func<double> _now;
        readonly List<Token> _tokens = new List<Token>();
        int _nextId = 1;
        double _firstBeginAt;
        double _visibleSince;
        bool _visible;
        bool _lastBlocks;

        /// <summary>Seconds between the first token beginning and the overlay appearing.</summary>
        public float ShowDelaySeconds { get; set; } = DefaultShowDelaySeconds;

        /// <summary>Seconds the overlay stays visible once shown, even when the work finished earlier.</summary>
        public float MinVisibleSeconds { get; set; } = DefaultMinVisibleSeconds;

        /// <summary>Raised when visibility, blocking, message, spinner or progress changed.</summary>
        public event Action Changed;

        /// <summary>Creates the counter. <paramref name="nowSeconds"/> is the clock; use unscaled time so a paused game still shows loading.</summary>
        public LoadingTokens(Func<double> nowSeconds)
        {
            if (nowSeconds == null)
                throw new ArgumentNullException(nameof(nowSeconds));
            _now = nowSeconds;
        }

        /// <summary>Number of tokens that have begun and not ended.</summary>
        public int ActiveCount
        {
            get { return _tokens.Count; }
        }

        /// <summary>Whether the overlay graphic is shown (after the delay, until the minimum time has passed).</summary>
        public bool IsVisible
        {
            get { return _visible; }
        }

        /// <summary>True while any alive token blocks input, including during the show delay.</summary>
        public bool BlocksInput
        {
            get
            {
                for (int i = 0; i < _tokens.Count; i++)
                {
                    if (_tokens[i].BlockInput)
                        return true;
                }
                return false;
            }
        }

        /// <summary>Message of the newest token that has one, or empty.</summary>
        public UiText Message
        {
            get
            {
                for (int i = _tokens.Count - 1; i >= 0; i--)
                {
                    if (_tokens[i].Message != null && !_tokens[i].Message.IsEmpty)
                        return _tokens[i].Message;
                }
                return UiText.Empty;
            }
        }

        /// <summary>Spinner of the newest token (true when there is none).</summary>
        public bool ShowSpinner
        {
            get { return _tokens.Count == 0 || _tokens[_tokens.Count - 1].ShowSpinner; }
        }

        /// <summary>Progress 0 to 1 of the newest token that reports one, or -1 for indeterminate.</summary>
        public float Progress
        {
            get
            {
                for (int i = _tokens.Count - 1; i >= 0; i--)
                {
                    if (_tokens[i].Progress >= 0f)
                        return _tokens[i].Progress;
                }
                return -1f;
            }
        }

        /// <summary>Starts a loading period and returns its id for <see cref="End"/>, <see cref="Update"/> and <see cref="SetProgress"/>.</summary>
        public int Begin(UiText message, bool blockInput)
        {
            Token token = new Token { Id = _nextId++, Message = message, BlockInput = blockInput };
            if (_tokens.Count == 0)
                _firstBeginAt = _now();
            _tokens.Add(token);
            Evaluate();
            return token.Id;
        }

        /// <summary>Ends a token. Unknown ids are ignored (ending twice is harmless).</summary>
        public bool End(int id)
        {
            int index = IndexOf(id);
            if (index < 0)
                return false;
            _tokens.RemoveAt(index);
            Evaluate();
            return true;
        }

        /// <summary>Changes the message and spinner of a token. Returns false for an unknown id.</summary>
        public bool Update(int id, UiText message, bool showSpinner)
        {
            int index = IndexOf(id);
            if (index < 0)
                return false;
            _tokens[index].Message = message;
            _tokens[index].ShowSpinner = showSpinner;
            Evaluate(true);
            return true;
        }

        /// <summary>Sets progress 0 to 1 of a token (clamped), or a negative value for indeterminate. Returns false for an unknown id.</summary>
        public bool SetProgress(int id, float normalized)
        {
            int index = IndexOf(id);
            if (index < 0)
                return false;
            float clamped = float.IsNaN(normalized) || normalized < 0f ? 0f : (normalized > 1f ? 1f : normalized);
            _tokens[index].Progress = clamped;
            Evaluate(true);
            return true;
        }

        /// <summary>Re-evaluates visibility against the clock. Call it when <see cref="NextDueSeconds"/> has elapsed.</summary>
        public void Tick()
        {
            Evaluate();
        }

        /// <summary>Seconds from now until visibility changes by itself (the delay ends or the minimum time ends), or -1 when nothing is pending.</summary>
        public double NextDueSeconds
        {
            get
            {
                double now = _now();
                if (_tokens.Count > 0 && !_visible)
                {
                    double due = _firstBeginAt + ShowDelaySeconds - now;
                    return due < 0 ? 0 : due;
                }
                if (_tokens.Count == 0 && _visible)
                {
                    double due = _visibleSince + MinVisibleSeconds - now;
                    return due < 0 ? 0 : due;
                }
                return -1;
            }
        }

        int IndexOf(int id)
        {
            for (int i = 0; i < _tokens.Count; i++)
            {
                if (_tokens[i].Id == id)
                    return i;
            }
            return -1;
        }

        void Evaluate(bool contentChanged = false)
        {
            double now = _now();
            bool changed = contentChanged;
            if (_tokens.Count > 0)
            {
                if (!_visible && now - _firstBeginAt >= ShowDelaySeconds)
                {
                    _visible = true;
                    _visibleSince = now;
                    changed = true;
                }
            }
            else if (_visible && now - _visibleSince >= MinVisibleSeconds)
            {
                _visible = false;
                changed = true;
            }

            bool blocks = BlocksInput;
            if (blocks != _lastBlocks)
            {
                _lastBlocks = blocks;
                changed = true;
            }

            if (!changed)
                return;
            Action handler = Changed;
            if (handler != null)
                handler();
        }
    }
}
