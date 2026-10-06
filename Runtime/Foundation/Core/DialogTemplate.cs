using System;
using System.Collections.Generic;

namespace FutureDigitalPlus.UIKit.Foundation
{
    /// <summary>
    /// A dialog a game shows more than once (a confirm before a reset, a "no connection" notice), described once and turned into a <see cref="DialogRequest"/> each time.
    /// Keep templates in a static class or a catalog of your own; the kit ships none.
    /// </summary>
    public sealed class DialogTemplate
    {
        readonly List<DialogButton> _buttons = new List<DialogButton>();

        /// <summary>Stable id of the dialog, copied to <see cref="DialogRequest.Id"/>.</summary>
        public string Id { get; set; }

        /// <summary>Heading.</summary>
        public UiText Title { get; set; } = UiText.Empty;

        /// <summary>Body text.</summary>
        public UiText Message { get; set; } = UiText.Empty;

        /// <summary>Order among waiting dialogs.</summary>
        public DialogPriority Priority { get; set; } = DialogPriority.Normal;

        /// <summary>Back closes the dialog. Default true.</summary>
        public bool DismissOnBack { get; set; } = true;

        /// <summary>A tap on the scrim closes the dialog.</summary>
        public bool DismissOnScrim { get; set; }

        /// <summary>The dialog closes without action when the app becomes active again.</summary>
        public bool DismissOnAppResume { get; set; }

        /// <summary>While a dialog with this key is queued or showing, a second one is ignored. Null disables the check.</summary>
        public string DedupeKey { get; set; }

        /// <summary>The buttons in display order.</summary>
        public IReadOnlyList<DialogButton> Buttons
        {
            get { return _buttons; }
        }

        /// <summary>Appends a button and returns this template, so calls chain.</summary>
        public DialogTemplate AddButton(DialogButton button)
        {
            if (button == null)
                throw new ArgumentNullException(nameof(button));
            _buttons.Add(button);
            return this;
        }

        /// <summary>A fresh request with this template's values. <paramref name="content"/> is the optional custom body (a <c>VisualElement</c> in the view layer).</summary>
        public DialogRequest CreateRequest(Action<DialogOutcome> callback = null, object content = null)
        {
            DialogRequest request = new DialogRequest
            {
                Id = Id,
                Title = Title,
                Message = Message,
                Priority = Priority,
                DismissOnBack = DismissOnBack,
                DismissOnScrim = DismissOnScrim,
                DismissOnAppResume = DismissOnAppResume,
                DedupeKey = DedupeKey,
                Callback = callback,
                Content = content
            };
            for (int i = 0; i < _buttons.Count; i++)
                request.AddButton(_buttons[i]);
            return request;
        }
    }
}
