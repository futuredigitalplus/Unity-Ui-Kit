using System;

namespace FutureDigitalPlus.UIKit.Foundation
{
    /// <summary>
    /// Where the pure foundation code reports problems it handled (a callback that threw). The core has no Unity dependency, so it only calls <see cref="Sink"/>;
    /// <c>UiShell</c> points an empty sink at the Unity console. A game or a test may set its own.
    /// </summary>
    public static class UiLog
    {
        /// <summary>Receives level, message and the exception when there is one. Null drops messages.</summary>
        public static Action<UiLogLevel, string, Exception> Sink;

        /// <summary>Logs a message.</summary>
        public static void Write(UiLogLevel level, string message)
        {
            Action<UiLogLevel, string, Exception> sink = Sink;
            if (sink != null)
                sink(level, message, null);
        }

        /// <summary>Logs a warning.</summary>
        public static void Warning(string message)
        {
            Write(UiLogLevel.Warning, message);
        }

        /// <summary>Logs an error.</summary>
        public static void Error(string message)
        {
            Write(UiLogLevel.Error, message);
        }

        /// <summary>Logs an exception as a warning (handled, the operation went on).</summary>
        public static void Warning(Exception exception)
        {
            Action<UiLogLevel, string, Exception> sink = Sink;
            if (sink != null && exception != null)
                sink(UiLogLevel.Warning, exception.Message, exception);
        }

        /// <summary>Logs an exception as an error.</summary>
        public static void Error(Exception exception)
        {
            Action<UiLogLevel, string, Exception> sink = Sink;
            if (sink != null && exception != null)
                sink(UiLogLevel.Error, exception.Message, exception);
        }
    }
}
