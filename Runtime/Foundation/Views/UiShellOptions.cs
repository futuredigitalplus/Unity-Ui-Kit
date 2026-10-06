namespace FutureDigitalPlus.UIKit.Foundation
{
    /// <summary>Settings of a <see cref="UiShell"/>. Every member has a working default; a game usually sets only <see cref="Assets"/> and <see cref="StateStore"/>.</summary>
    public sealed class UiShellOptions
    {
        /// <summary>Where screens and templates find their UXML and USS. Null: no asset source (screens built in code, or from <see cref="ScreenDefinition"/> references).</summary>
        public IUiAssetSource Assets { get; set; }

        /// <summary>Where the navigation stack is saved. Null: navigation is not saved or restored.</summary>
        public IUiStateStore StateStore { get; set; }

        /// <summary>The shell's own screen catalog. Null creates an empty one (available as <see cref="UiShell.Catalog"/>).</summary>
        public ScreenCatalog Catalog { get; set; }

        /// <summary>When true (default) the shell also takes the screens registered on <see cref="ScreenCatalog.Default"/>.</summary>
        public bool UseDefaultCatalog { get; set; } = true;

        /// <summary>Polled by <see cref="UiShell.Tick"/> for the back key. Null: only <see cref="UiShell.RequestBack"/> and navigation-cancel events go back.</summary>
        public IBackKeySource BackKeySource { get; set; }

        /// <summary>Tells the shell which input device the player uses (the Interaction module provides one). Null: the platform default.</summary>
        public IUiInputModeSource InputModeSource { get; set; }

        /// <summary>Resolves <see cref="UiText"/>. Null: literals, key fallbacks and <c>[KEY]</c> placeholders.</summary>
        public UiTextSource Text { get; set; }

        /// <summary>Saves the stack when it changes and restores it at <see cref="UiShell.Boot"/> (needs <see cref="StateStore"/>). Default true.</summary>
        public bool RestoreNavigation { get; set; } = true;

        /// <summary>Key under which the stack is stored.</summary>
        public string NavigationStateKey { get; set; } = "fdp/navigation";

        /// <summary>Adds Foundation.Tokens.uss and Foundation.uss to the shell root. Default true.</summary>
        public bool InstallStyles { get; set; } = true;

        /// <summary>Adds the design system's <c>ds-root</c> and <c>ds-root--hud</c> classes to the shell root, so text colour and size cascade. Default true.</summary>
        public bool ApplyDesignSystemRootClasses { get; set; } = true;

        /// <summary>Maximum number of screens on the stack. Default 12.</summary>
        public int MaxDepth { get; set; } = ScreenStack.DefaultMaxDepth;

        /// <summary>False plays no screen transitions. Reduce motion also switches them off.</summary>
        public bool TransitionsEnabled { get; set; } = true;

        /// <summary>When the safe area applies; see <see cref="SafeAreaMode"/>. Ignored when <see cref="SafeArea"/> is given.</summary>
        public SafeAreaMode SafeAreaMode { get; set; } = SafeAreaMode.Auto;

        /// <summary>Minimum margin in percent of the panel size; negative means 5 percent on consoles and tvOS and zero elsewhere. Ignored when <see cref="SafeArea"/> is given.</summary>
        public float MinimumMarginPercent { get; set; } = -1f;

        /// <summary>A provider to use instead of creating one (tests, shared providers).</summary>
        public SafeAreaProvider SafeArea { get; set; }

        /// <summary>Where toasts appear.</summary>
        public ToastPlacement ToastPlacement { get; set; } = ToastPlacement.Bottom;

        /// <summary>Turns navigation-cancel events (Escape, gamepad B, Android back through the event system) into a back press. Default true.</summary>
        public bool HandleNavigationCancel { get; set; } = true;
    }
}
