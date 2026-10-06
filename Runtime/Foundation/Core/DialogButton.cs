namespace FutureDigitalPlus.UIKit.Foundation
{
    /// <summary>One button of a dialog.</summary>
    public sealed class DialogButton
    {
        /// <summary>Text of the button.</summary>
        public UiText Label { get; }

        /// <summary>How the button is styled and when back or a scrim tap presses it.</summary>
        public DialogButtonRole Role { get; }

        /// <summary>Creates a button; a null label becomes empty text.</summary>
        public DialogButton(UiText label, DialogButtonRole role = DialogButtonRole.Default)
        {
            Label = label ?? UiText.Empty;
            Role = role;
        }

        /// <summary>A button from a localization key with a fallback shown when the key cannot be resolved.</summary>
        public static DialogButton Of(string key, string fallback, DialogButtonRole role = DialogButtonRole.Default)
        {
            return new DialogButton(UiText.FromKey(key).WithFallback(fallback), role);
        }
    }
}
