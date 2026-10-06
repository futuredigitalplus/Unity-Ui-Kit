using UnityEngine;

namespace FutureDigitalPlus.UIKit.Foundation
{
    /// <summary>Resets the static state of the foundation when Unity enters play mode without reloading the domain, and points the pure core's log at the console.</summary>
    internal static class FoundationRuntime
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics()
        {
            FocusTrap.ResetStatics();
            UiAccessibility.Reset();
            SafeAreaProvider.Default.ResetToDeviceDefaults();
            UiLog.Sink = null;
        }

        /// <summary>Gives <see cref="UiLog"/> a sink that writes to the Unity console, unless the game set its own.</summary>
        internal static void EnsureLogSink()
        {
            if (UiLog.Sink == null)
                UiLog.Sink = WriteToConsole;
        }

        static void WriteToConsole(UiLogLevel level, string message, System.Exception exception)
        {
            string text = "FutureDigitalPlus.UIKit.Foundation: " + message;
            if (exception != null && level == UiLogLevel.Error)
                Debug.LogException(exception);
            else if (level == UiLogLevel.Error)
                Debug.LogError(text);
            else if (level == UiLogLevel.Warning)
                Debug.LogWarning(text);
            else
                Debug.Log(text);
        }
    }
}
