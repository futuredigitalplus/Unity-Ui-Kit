using System;
using System.Collections.Generic;

namespace FutureDigitalPlus.UIKit.Foundation
{
    /// <summary>
    /// Everything a dialog needs, with no Unity types. Build one directly, or from a <see cref="DialogTemplate"/> your game keeps for its recurring dialogs.
    /// Buttons are listed in display order; the first <see cref="DialogButtonRole.Cancel"/> button is what back and scrim taps press.
    /// </summary>
    public sealed class DialogRequest
    {
        readonly List<DialogButton> _buttons = new List<DialogButton>();

        /// <summary>Stable id for logs, analytics and tests, for example <c>D1.Restart</c>.</summary>
        public string Id { get; set; }

        /// <summary>Heading of the dialog; may be empty.</summary>
        public UiText Title { get; set; } = UiText.Empty;

        /// <summary>Body text; may be empty.</summary>
        public UiText Message { get; set; } = UiText.Empty;

        /// <summary>Optional custom content (a keypad, a tile grid). The view layer accepts a UI Toolkit <c>VisualElement</c> here; the pure queue never looks at it.</summary>
        public object Content { get; set; }

        /// <summary>Order among waiting dialogs.</summary>
        public DialogPriority Priority { get; set; } = DialogPriority.Normal;

        /// <summary>Back and Escape close the dialog (with the cancel button's index). When false, back is still swallowed while the dialog is up.</summary>
        public bool DismissOnBack { get; set; } = true;

        /// <summary>A tap outside the dialog closes it like the cancel button.</summary>
        public bool DismissOnScrim { get; set; }

        /// <summary>The dialog closes without action when the app becomes active again (for dialogs that only make sense for the moment the player left the app).</summary>
        public bool DismissOnAppResume { get; set; }

        /// <summary>
        /// While a dialog with the same key is queued or showing, a second request with that key is ignored (returns the existing handle),
        /// so a double tap cannot stack two Restart confirms. Null disables the check.
        /// </summary>
        public string DedupeKey { get; set; }

        /// <summary>Receives the result on the main thread, exactly once, also when the dialog is cleared. May enqueue further dialogs.</summary>
        public Action<DialogOutcome> Callback { get; set; }

        /// <summary>The buttons in display order.</summary>
        public IReadOnlyList<DialogButton> Buttons
        {
            get { return _buttons; }
        }

        /// <summary>Appends a button and returns this request, so calls chain.</summary>
        public DialogRequest AddButton(DialogButton button)
        {
            if (button == null)
                throw new ArgumentNullException(nameof(button));
            _buttons.Add(button);
            return this;
        }

        /// <summary>Index of the first cancel button, or -1.</summary>
        public int CancelIndex
        {
            get
            {
                for (int i = 0; i < _buttons.Count; i++)
                {
                    if (_buttons[i].Role == DialogButtonRole.Cancel)
                        return i;
                }
                return -1;
            }
        }
    }
}
