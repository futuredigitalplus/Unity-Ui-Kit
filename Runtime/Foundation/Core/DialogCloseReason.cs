namespace FutureDigitalPlus.UIKit.Foundation
{
    /// <summary>Why a dialog closed. <see cref="Button"/> is the only reason with a real button index.</summary>
    public enum DialogCloseReason
    {
        /// <summary>A button was pressed.</summary>
        Button = 0,

        /// <summary>The platform back button, Escape or a navigation-cancel event.</summary>
        Back = 1,

        /// <summary>A tap outside the dialog (only when the request allows it).</summary>
        Scrim = 2,

        /// <summary>Closed by code through <see cref="DialogQueue.Cancel"/>.</summary>
        Programmatic = 3,

        /// <summary>The app became active again and the request asked to be dismissed without action.</summary>
        AppResumed = 4,

        /// <summary>The queue was cleared (screen change, shutdown).</summary>
        Cleared = 5
    }
}
