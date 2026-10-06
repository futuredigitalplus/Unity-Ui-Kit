namespace FutureDigitalPlus.UIKit.Foundation
{
    /// <summary>
    /// Element names and USS classes shared by the foundation C# and by UXML templates that override the built-in dialog, toast and loading views.
    /// A template found in the asset source under one of the <c>Template*</c> names replaces the code-built view when it contains the named elements.
    /// </summary>
    public static class UiNames
    {
        // ---- Shell skeleton ----

        /// <summary>Name of the shell root element.</summary>
        public const string Root = "fdp-shell";

        /// <summary>Name of the layer that holds the screens (the router outlet).</summary>
        public const string LayerScreens = "layer-screens";

        /// <summary>Name of the layer for heads-up displays, above the screens.</summary>
        public const string LayerHud = "layer-hud";

        /// <summary>Name of the layer for sheets, menus and other overlays that belong to a screen but must sit above the HUD.</summary>
        public const string LayerOverlays = "layer-overlays";

        /// <summary>Name of the layer that holds modal dialogs.</summary>
        public const string LayerDialogs = "layer-dialogs";

        /// <summary>Name of the layer that holds toasts (inside a safe-area panel).</summary>
        public const string LayerToasts = "layer-toasts";

        /// <summary>Name of the topmost layer: the loading overlay.</summary>
        public const string LayerLoading = "layer-loading";

        // ---- Template names (file name without extension) ----

        /// <summary>UXML template that replaces the code-built dialog frame.</summary>
        public const string TemplateDialogFrame = "DialogFrame";

        /// <summary>UXML template that replaces the code-built toast.</summary>
        public const string TemplateToast = "Toast";

        /// <summary>UXML template that replaces the code-built loading overlay.</summary>
        public const string TemplateLoadingOverlay = "LoadingOverlay";

        // ---- Dialog frame elements ----

        /// <summary>The element that dims the screen behind a dialog and absorbs input.</summary>
        public const string DialogScrim = "dialog-scrim";

        /// <summary>The safe-area panel inside the scrim.</summary>
        public const string DialogSafe = "dialog-safe";

        /// <summary>The dialog card.</summary>
        public const string DialogPanel = "dialog";

        /// <summary>The scrolling body (title, message, content).</summary>
        public const string DialogBody = "dialog-body";

        /// <summary>The title label.</summary>
        public const string DialogTitle = "dialog-title";

        /// <summary>The message label.</summary>
        public const string DialogMessage = "dialog-message";

        /// <summary>The container for custom content.</summary>
        public const string DialogContent = "dialog-content";

        /// <summary>The button row.</summary>
        public const string DialogButtons = "dialog-buttons";

        // ---- Toast elements ----

        /// <summary>The toast element.</summary>
        public const string ToastRoot = "toast";

        /// <summary>The toast message label.</summary>
        public const string ToastLabel = "toast-label";

        // ---- Loading overlay elements ----

        /// <summary>The element that blocks input and dims the screen while loading.</summary>
        public const string LoadingBackdrop = "loading-backdrop";

        /// <summary>The spinner element.</summary>
        public const string LoadingSpinner = "loading-spinner";

        /// <summary>The message label.</summary>
        public const string LoadingMessage = "loading-message";

        /// <summary>The determinate progress track.</summary>
        public const string LoadingProgress = "loading-progress";

        /// <summary>The fill inside the progress track.</summary>
        public const string LoadingProgressFill = "loading-progress-fill";

        // ---- Foundation classes ----

        /// <summary>Class of the shell root.</summary>
        public const string ClassShell = "fdp-shell";

        /// <summary>Class of every layer.</summary>
        public const string ClassLayer = "fdp-layer";

        /// <summary>Class of every screen root.</summary>
        public const string ClassScreen = "fdp-screen";

        /// <summary>Added while a screen slides in after a push or replace.</summary>
        public const string ClassScreenEnter = "fdp-screen--enter";

        /// <summary>Added while a screen slides in after a pop.</summary>
        public const string ClassScreenEnterBack = "fdp-screen--enter-back";

        /// <summary>Added while a screen leaves and will be disposed.</summary>
        public const string ClassScreenExit = "fdp-screen--exit";

        /// <summary>Added while a screen is covered by a pushed screen and stays alive hidden.</summary>
        public const string ClassScreenBehind = "fdp-screen--behind";

        /// <summary>Added to a scrim, toast or overlay once it should fade in.</summary>
        public const string ClassVisible = "fdp-visible";

        /// <summary>Class of the dialog scrim.</summary>
        public const string ClassScrim = "fdp-scrim";

        /// <summary>Class of the safe-area panel inside the dialog scrim.</summary>
        public const string ClassDialogSafe = "fdp-dialog-safe";

        /// <summary>Class of the dialog card (next to the design system's <c>ds-dialog</c> or <c>ds-modal</c>).</summary>
        public const string ClassDialog = "fdp-dialog";

        /// <summary>Added to the dialog card when custom content is present.</summary>
        public const string ClassDialogWithContent = "fdp-dialog--content";

        /// <summary>Added to the button row when every caption fits in an equal share of the row.</summary>
        public const string ClassDialogButtonsEqual = "fdp-dialog__buttons--equal";

        /// <summary>Class of a toast (next to <c>ds-toast</c>).</summary>
        public const string ClassToast = "fdp-toast";

        /// <summary>Prefix of the design system's toast kind classes: <c>ds-toast--success</c>, <c>info</c>, <c>warning</c>, <c>danger</c>.</summary>
        public const string ClassToastKindPrefix = "ds-toast--";

        /// <summary>Class of the toast safe-area panel when toasts sit at the top of the screen.</summary>
        public const string ClassToastsTop = "fdp-toasts--top";

        // ---- Profile classes written to the shell root by <see cref="UiProfileWatcher"/> ----

        /// <summary>Panel taller than wide.</summary>
        public const string ClassPortrait = "fdp-portrait";

        /// <summary>Panel at least as wide as tall.</summary>
        public const string ClassLandscape = "fdp-landscape";

        /// <summary>Short side below 600 units.</summary>
        public const string ClassCompact = "fdp-compact";

        /// <summary>Short side from 600 to below 840 units.</summary>
        public const string ClassRegular = "fdp-regular";

        /// <summary>Short side of 840 units or more.</summary>
        public const string ClassExpanded = "fdp-expanded";

        /// <summary>
        /// Class of the shell root while the size class is <see cref="UiSizeClass.Regular"/> or <see cref="UiSizeClass.Expanded"/>
        /// (<see cref="UiProfile.IsRegularOrLarger"/>), next to <c>fdp-regular</c> or <c>fdp-expanded</c>. For layouts with two tiers.
        /// </summary>
        public const string ClassRegularUp = "fdp-regular-up";

        /// <summary>Prefix of the platform classes (<c>fdp-platform-ios</c> and so on).</summary>
        public const string ClassPlatformPrefix = "fdp-platform-";

        /// <summary>Prefix of the input mode classes (<c>fdp-input-touch</c>, <c>fdp-input-mouse</c>, <c>fdp-input-keyboard</c>, <c>fdp-input-gamepad</c>).</summary>
        public const string ClassInputPrefix = "fdp-input-";

        /// <summary>Set while the input mode is keyboard or gamepad (directional navigation is in use).</summary>
        public const string ClassInputNav = "fdp-input-nav";
    }
}
