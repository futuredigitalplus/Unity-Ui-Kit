namespace FutureDigitalPlus.UIKit.Foundation
{
    /// <summary>What a dialog callback receives. <see cref="ButtonIndex"/> is -1 when no button was pressed and no cancel button exists.</summary>
    public readonly struct DialogOutcome
    {
        /// <summary>Index of the pressed button, or of the cancel button for back and scrim closes, or -1.</summary>
        public int ButtonIndex { get; }

        /// <summary>Why the dialog closed.</summary>
        public DialogCloseReason Reason { get; }

        /// <summary>Creates a result.</summary>
        public DialogOutcome(int buttonIndex, DialogCloseReason reason)
        {
            ButtonIndex = buttonIndex;
            Reason = reason;
        }

        /// <summary>True when the player pressed a button (not back, scrim or a programmatic close).</summary>
        public bool IsButton
        {
            get { return Reason == DialogCloseReason.Button; }
        }

        /// <summary>Button index and reason, for logs.</summary>
        public override string ToString()
        {
            return "button " + ButtonIndex + " (" + Reason + ")";
        }
    }
}
