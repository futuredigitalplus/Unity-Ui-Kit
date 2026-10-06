namespace FutureDigitalPlus.UIKit.Foundation
{
    /// <summary>The ordered layers of a <see cref="UiShell"/>, bottom to top.</summary>
    public enum UiLayer
    {
        /// <summary>Full-screen pages managed by the <see cref="ScreenRouter"/>.</summary>
        Screens = 0,

        /// <summary>Heads-up displays that float above the screens.</summary>
        Hud = 1,

        /// <summary>Sheets, menus and popups above the HUD.</summary>
        Overlays = 2,

        /// <summary>Modal dialogs.</summary>
        Dialogs = 3,

        /// <summary>Short non-blocking messages.</summary>
        Toasts = 4,

        /// <summary>The loading overlay.</summary>
        Loading = 5
    }
}
