namespace FutureDigitalPlus.UIKit.Foundation
{
    /// <summary>Modal dialogs, one at a time. Presets (<c>Confirm</c>, <c>Alert</c>, <c>Choice</c>, <c>Input</c>) are extension methods in <c>DialogPresets</c>.</summary>
    public interface IDialogService
    {
        /// <summary>True while a dialog is showing.</summary>
        bool IsModalOpen { get; }

        /// <summary>Raised when <see cref="IsModalOpen"/> changes.</summary>
        event System.Action<bool> ModalStateChanged;

        /// <summary>Shows a dialog now, or queues it behind the one that is showing.</summary>
        DialogHandle Show(DialogRequest request);

        /// <summary>Closes one dialog, showing or waiting, as a programmatic close. Returns false when it was already closed.</summary>
        bool Cancel(DialogHandle handle);

        /// <summary>Closes the current dialog and drops the waiting ones.</summary>
        void CloseAll();

        /// <summary>Handles a back press. Returns true when a dialog is open.</summary>
        bool HandleBack();

        /// <summary>Call when the app becomes active again, so dialogs that asked for it close.</summary>
        void OnAppResumed();
    }
}
