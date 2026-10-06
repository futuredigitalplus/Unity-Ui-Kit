namespace FutureDigitalPlus.UIKit.Foundation
{
    /// <summary>Priorities of back handlers in <see cref="BackDispatcher"/>; higher runs first. Use these constants, not literals.</summary>
    public static class BackPriority
    {
        /// <summary>A blocking loading token swallows back (unless it allows cancel).</summary>
        public const int Loading = 1000;

        /// <summary>The open modal dialog.</summary>
        public const int Dialog = 900;

        /// <summary>Overlays that belong to a screen (a sheet, a menu).</summary>
        public const int Overlay = 500;

        /// <summary>The top screen's own OnBack.</summary>
        public const int Screen = 100;

        /// <summary>The router's pop.</summary>
        public const int Router = 0;
    }
}
