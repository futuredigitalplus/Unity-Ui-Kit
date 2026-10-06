namespace FutureDigitalPlus.UIKit.Foundation
{
    /// <summary>Short messages that fade by themselves.</summary>
    public interface IToastService
    {
        /// <summary>Shows a toast. An equal toast that is already visible restarts its timer instead of stacking.</summary>
        void Show(UiText text, ToastKind kind = ToastKind.Info, float seconds = ToastItem.DefaultSeconds);

        /// <summary>Shows a toast from a string that needs no lookup.</summary>
        void ShowRaw(string text, ToastKind kind = ToastKind.Info, float seconds = ToastItem.DefaultSeconds);

        /// <summary>Removes the visible toast and the waiting ones.</summary>
        void Clear();
    }
}
