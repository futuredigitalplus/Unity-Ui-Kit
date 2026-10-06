using System;

namespace FutureDigitalPlus.UIKit.Foundation
{
    /// <summary>One loading period. Dispose it (a <c>using</c> block) to end it; ending twice is harmless.</summary>
    public readonly struct LoadingToken : IDisposable
    {
        readonly ILoadingOverlay _owner;

        /// <summary>Identifier inside the overlay; 0 means not valid.</summary>
        public int Id { get; }

        /// <summary>Creates a token. Only overlays call this.</summary>
        public LoadingToken(ILoadingOverlay owner, int id)
        {
            _owner = owner;
            Id = id;
        }

        /// <summary>False for a default token (nothing to end).</summary>
        public bool IsValid
        {
            get { return _owner != null && Id != 0; }
        }

        /// <summary>True when <paramref name="overlay"/> is the overlay that issued this token. Ids are only unique inside one overlay, so an overlay ignores tokens of another.</summary>
        public bool IsIssuedBy(ILoadingOverlay overlay)
        {
            return IsValid && ReferenceEquals(_owner, overlay);
        }

        /// <summary>Ends the loading period.</summary>
        public void Dispose()
        {
            if (_owner != null)
                _owner.End(this);
        }
    }
}
