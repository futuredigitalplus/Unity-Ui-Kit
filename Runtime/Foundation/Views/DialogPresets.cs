using System;
using System.Collections.Generic;
using UnityEngine.UIElements;

namespace FutureDigitalPlus.UIKit.Foundation
{
    /// <summary>
    /// The four dialogs almost every game needs, as extension methods of <see cref="IDialogService"/>: <see cref="Confirm"/>, <see cref="Alert"/>,
    /// <see cref="Choice"/> and <see cref="Input"/>. Button captions default to the English texts below with the localization keys <c>FDP-OK</c>,
    /// <c>FDP-CANCEL</c>, <c>FDP-YES</c> and <c>FDP-NO</c>, so a game that defines those keys gets translated buttons and one that does not still gets English ones.
    /// </summary>
    public static class DialogPresets
    {
        /// <summary>"OK" (key <c>FDP-OK</c>).</summary>
        public static UiText Ok
        {
            get { return UiText.FromKey("FDP-OK").WithFallback("OK"); }
        }

        /// <summary>"Cancel" (key <c>FDP-CANCEL</c>).</summary>
        public static UiText Cancel
        {
            get { return UiText.FromKey("FDP-CANCEL").WithFallback("Cancel"); }
        }

        /// <summary>
        /// A question with a cancel button and a confirm button. <paramref name="onResult"/> receives true only when the confirm button was pressed
        /// (back, scrim and programmatic closes give false). <paramref name="destructive"/> styles the confirm button in the danger colour.
        /// <paramref name="dedupeKey"/> keeps a double tap from stacking two identical questions.
        /// </summary>
        public static DialogHandle Confirm(this IDialogService dialogs, UiText title, UiText message, Action<bool> onResult,
            UiText confirmLabel = null, UiText cancelLabel = null, bool destructive = false, string dedupeKey = null)
        {
            if (dialogs == null)
                throw new ArgumentNullException(nameof(dialogs));
            DialogRequest request = new DialogRequest
            {
                Id = "fdp.confirm",
                Title = title ?? UiText.Empty,
                Message = message ?? UiText.Empty,
                DedupeKey = dedupeKey,
                Callback = result =>
                {
                    if (onResult != null)
                        onResult(result.IsButton && result.ButtonIndex == 1);
                }
            };
            request.AddButton(new DialogButton(cancelLabel ?? Cancel, DialogButtonRole.Cancel));
            request.AddButton(new DialogButton(confirmLabel ?? Ok, destructive ? DialogButtonRole.Destructive : DialogButtonRole.Default));
            return dialogs.Show(request);
        }

        /// <summary>A message with one button. <paramref name="onClosed"/> runs however the dialog closes (button, back, scrim, code).</summary>
        public static DialogHandle Alert(this IDialogService dialogs, UiText title, UiText message, Action onClosed = null, UiText buttonLabel = null, string dedupeKey = null)
        {
            if (dialogs == null)
                throw new ArgumentNullException(nameof(dialogs));
            DialogRequest request = new DialogRequest
            {
                Id = "fdp.alert",
                Title = title ?? UiText.Empty,
                Message = message ?? UiText.Empty,
                DedupeKey = dedupeKey,
                Callback = result =>
                {
                    if (onClosed != null)
                        onClosed();
                }
            };
            request.AddButton(new DialogButton(buttonLabel ?? Ok, DialogButtonRole.Cancel));
            return dialogs.Show(request);
        }

        /// <summary>
        /// A list of choices as buttons, in order, and optionally a cancel button last. <paramref name="onChoice"/> receives the index in
        /// <paramref name="choices"/>, or -1 when the dialog was cancelled or closed otherwise.
        /// </summary>
        public static DialogHandle Choice(this IDialogService dialogs, UiText title, UiText message, IReadOnlyList<UiText> choices, Action<int> onChoice,
            bool allowCancel = true, UiText cancelLabel = null, string dedupeKey = null)
        {
            if (dialogs == null)
                throw new ArgumentNullException(nameof(dialogs));
            if (choices == null)
                throw new ArgumentNullException(nameof(choices));
            int count = choices.Count;
            DialogRequest request = new DialogRequest
            {
                Id = "fdp.choice",
                Title = title ?? UiText.Empty,
                Message = message ?? UiText.Empty,
                DedupeKey = dedupeKey,
                DismissOnBack = allowCancel,
                Callback = result =>
                {
                    if (onChoice != null)
                        onChoice(result.IsButton && result.ButtonIndex >= 0 && result.ButtonIndex < count ? result.ButtonIndex : -1);
                }
            };
            for (int i = 0; i < count; i++)
                request.AddButton(new DialogButton(choices[i], DialogButtonRole.Default));
            if (allowCancel)
                request.AddButton(new DialogButton(cancelLabel ?? Cancel, DialogButtonRole.Cancel));
            return dialogs.Show(request);
        }

        /// <summary>
        /// A single-line text prompt (a name, a code). <paramref name="onResult"/> receives the text when the confirm button was pressed and null when the dialog
        /// was cancelled or closed otherwise. <paramref name="text"/> resolves the placeholder; null shows it as a literal or fallback.
        /// </summary>
        public static DialogHandle Input(this IDialogService dialogs, UiText title, Action<string> onResult, string initialValue = "", UiText placeholder = null,
            UiText confirmLabel = null, UiText cancelLabel = null, int maxLength = -1, UiTextSource text = null, string dedupeKey = null)
        {
            if (dialogs == null)
                throw new ArgumentNullException(nameof(dialogs));
            TextField field = new TextField { value = initialValue ?? string.Empty, maxLength = maxLength };
            field.AddToClassList("ds-input");
            field.AddToClassList("fdp-dialog__input");
            if (placeholder != null && !placeholder.IsEmpty)
                field.textEdition.placeholder = (text ?? new UiTextSource()).Resolve(placeholder);
            DialogRequest request = new DialogRequest
            {
                Id = "fdp.input",
                Title = title ?? UiText.Empty,
                Content = field,
                DedupeKey = dedupeKey,
                Callback = result =>
                {
                    if (onResult != null)
                        onResult(result.IsButton && result.ButtonIndex == 1 ? field.value : null);
                }
            };
            request.AddButton(new DialogButton(cancelLabel ?? Cancel, DialogButtonRole.Cancel));
            request.AddButton(new DialogButton(confirmLabel ?? Ok, DialogButtonRole.Default));
            return dialogs.Show(request);
        }
    }
}
