using System;
using System.Collections.Generic;

namespace FutureDigitalPlus.UIKit.Foundation
{
    /// <summary>
    /// Counts the reasons input to the table and to screens is blocked (modal dialog, blocking loading token, running screen transition).
    /// Each reason is a token with a name for debugging; releasing twice is harmless. Pure.
    /// </summary>
    public sealed class InputBlockCounter
    {
        readonly Dictionary<int, string> _reasons = new Dictionary<int, string>();
        int _nextToken = 1;

        /// <summary>True while at least one reason is held.</summary>
        public bool IsBlocked
        {
            get { return _reasons.Count > 0; }
        }

        /// <summary>Number of reasons held.</summary>
        public int Count
        {
            get { return _reasons.Count; }
        }

        /// <summary>Raised with the new state when <see cref="IsBlocked"/> flips.</summary>
        public event Action<bool> Changed;

        /// <summary>Adds a reason and returns the token for <see cref="Release"/>.</summary>
        public int Acquire(string reason)
        {
            bool wasBlocked = IsBlocked;
            int token = _nextToken++;
            _reasons[token] = reason ?? string.Empty;
            if (!wasBlocked)
                Raise(true);
            return token;
        }

        /// <summary>Removes a reason. Returns false when the token is unknown (a second release is harmless).</summary>
        public bool Release(int token)
        {
            if (!_reasons.Remove(token))
                return false;
            if (!IsBlocked)
                Raise(false);
            return true;
        }

        /// <summary>Names of the current reasons, for a debug overlay.</summary>
        public IReadOnlyCollection<string> Reasons
        {
            get { return _reasons.Values; }
        }

        /// <summary>Drops every reason.</summary>
        public void Clear()
        {
            if (_reasons.Count == 0)
                return;
            _reasons.Clear();
            Raise(false);
        }

        void Raise(bool blocked)
        {
            Action<bool> handler = Changed;
            if (handler != null)
                handler(blocked);
        }
    }
}
