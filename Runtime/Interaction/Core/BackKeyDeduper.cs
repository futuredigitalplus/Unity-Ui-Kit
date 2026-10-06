namespace FutureDigitalPlus.UIKit.Interaction
{
    /// <summary>
    /// Lets a "back" press through once per frame. One physical press can be reported by several sources in the same frame (a device
    /// poll and the <c>NavigationCancelEvent</c> of a focused element); the first report wins, the rest are dropped.
    /// </summary>
    public sealed class BackKeyDeduper
    {
        private int _lastFrame = int.MinValue;

        /// <summary>True for the first call in <paramref name="frame"/>, false for further calls in the same frame.</summary>
        public bool TryAccept(int frame)
        {
            if (frame == _lastFrame) return false;
            _lastFrame = frame;
            return true;
        }

        /// <summary>Forgets the last frame, so the next call is accepted.</summary>
        public void Reset() => _lastFrame = int.MinValue;
    }
}
