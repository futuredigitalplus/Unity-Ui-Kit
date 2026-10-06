namespace FutureDigitalPlus.UIKit.Foundation
{
    /// <summary>Where a request is in its life inside a <see cref="DialogQueue"/>.</summary>
    public enum DialogHandleState
    {
        /// <summary>Waiting behind the dialog that is showing.</summary>
        Queued = 0,
        /// <summary>The current dialog.</summary>
        Showing = 1,
        /// <summary>Closed; <see cref="DialogHandle.Result"/> is set.</summary>
        Closed = 2
    }
}
