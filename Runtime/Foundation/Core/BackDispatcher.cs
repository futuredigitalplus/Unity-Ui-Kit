using System;
using System.Collections.Generic;

namespace FutureDigitalPlus.UIKit.Foundation
{
    /// <summary>
    /// Ordered chain of back handlers: loading overlay, modal dialog, screen overlays, the screen, the router. The first handler that returns
    /// true consumed the press. Equal priorities run newest first. Pure, no Unity types.
    /// </summary>
    public sealed class BackDispatcher
    {
        sealed class Handler
        {
            public int Token;
            public int Priority;
            public Func<bool> Callback;
        }

        readonly List<Handler> _handlers = new List<Handler>();
        int _nextToken = 1;

        /// <summary>Number of registered handlers.</summary>
        public int Count
        {
            get { return _handlers.Count; }
        }

        /// <summary>Adds a handler and returns the token for <see cref="Unregister"/>.</summary>
        public int Register(int priority, Func<bool> handler)
        {
            if (handler == null)
                throw new ArgumentNullException(nameof(handler));
            Handler entry = new Handler { Token = _nextToken++, Priority = priority, Callback = handler };
            int index = 0;
            while (index < _handlers.Count && _handlers[index].Priority > priority)
                index++;
            _handlers.Insert(index, entry);
            return entry.Token;
        }

        /// <summary>Removes a handler by the token <see cref="Register"/> returned. Returns false when the token is unknown.</summary>
        public bool Unregister(int token)
        {
            for (int i = 0; i < _handlers.Count; i++)
            {
                if (_handlers[i].Token == token)
                {
                    _handlers.RemoveAt(i);
                    return true;
                }
            }
            return false;
        }

        /// <summary>Runs the handlers from the highest priority down and returns true when one consumed the press.</summary>
        public bool Dispatch()
        {
            Handler[] snapshot = _handlers.ToArray();
            for (int i = 0; i < snapshot.Length; i++)
            {
                if (!_handlers.Contains(snapshot[i]))
                    continue;
                if (snapshot[i].Callback())
                    return true;
            }
            return false;
        }
    }
}
