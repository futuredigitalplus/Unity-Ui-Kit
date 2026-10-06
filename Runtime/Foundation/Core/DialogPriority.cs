namespace FutureDigitalPlus.UIKit.Foundation
{
    /// <summary>Order of waiting dialogs. The dialog on screen is never pre-empted; priority only orders the queue behind it.</summary>
    public enum DialogPriority
    {
        /// <summary>Ordinary dialogs, in arrival order.</summary>
        Normal = 0,

        /// <summary>Follow-ups of a dialog the player just answered (the second confirm of "Reset defaults") and results (win, loss).</summary>
        High = 1,

        /// <summary>Errors the player must see before anything else.</summary>
        System = 2
    }
}
