namespace FutureDigitalPlus.UIKit.Foundation
{
    /// <summary>The loading overlay as screens and games see it: a counter of loading periods that shows a spinner and optionally blocks input.</summary>
    public interface ILoadingOverlay
    {
        /// <summary>True while a blocking token is alive.</summary>
        bool BlocksInput { get; }

        /// <summary>Starts a loading period. The overlay appears after a short delay so quick work never flashes it. Dispose the token to end it.</summary>
        LoadingToken Begin(UiText message = null, bool blockInput = true);

        /// <summary>Ends a loading period.</summary>
        void End(LoadingToken token);

        /// <summary>Changes the message and spinner of a period.</summary>
        void Update(LoadingToken token, UiText message, bool showSpinner);

        /// <summary>Shows progress 0 to 1 for a period (negative: indeterminate).</summary>
        void SetProgress(LoadingToken token, float normalized);

        /// <summary>Consumes a back press while a blocking period is alive. Returns true when it did.</summary>
        bool HandleBack();
    }
}
