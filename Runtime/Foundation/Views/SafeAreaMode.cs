namespace FutureDigitalPlus.UIKit.Foundation
{
    /// <summary>When a <see cref="SafeAreaProvider"/> reports insets.</summary>
    public enum SafeAreaMode
    {
        /// <summary>Only for screen-space panels on the main display (<see cref="UiPanelKind.Screen"/>). The default.</summary>
        Auto = 0,

        /// <summary>For every panel kind, with the screen's safe area (tests, panels that are known to cover the screen).</summary>
        Always = 1,

        /// <summary>Never: all insets are zero.</summary>
        Never = 2
    }
}
