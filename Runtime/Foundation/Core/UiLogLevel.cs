namespace FutureDigitalPlus.UIKit.Foundation
{
    /// <summary>Severity of a message the foundation logs through <see cref="UiLog"/>.</summary>
    public enum UiLogLevel
    {
        /// <summary>Information.</summary>
        Info = 0,

        /// <summary>Something went wrong and was handled.</summary>
        Warning = 1,

        /// <summary>Something failed; the operation was skipped.</summary>
        Error = 2
    }
}
