using System.Collections.Generic;

namespace FutureDigitalPlus.UIKit.Foundation.Responsive
{
    /// <summary>
    /// What a game tells the Responsive Manager: the scale profile its panel uses and the screens worth reviewing, and how to open one while the game runs.
    /// Implement it once in an editor assembly of the game, with a public parameterless constructor; the Manager finds it by type. Without one the Manager uses
    /// <see cref="PanelScaleProfile.CardPortrait"/> and offers no screen list (the screen that is open is previewed).
    /// <code>
    /// public sealed class MyPreviewHost : IResponsivePreviewHost
    /// {
    ///     public PanelScaleProfile Profile =&gt; PanelScaleProfile.CardPortrait();
    ///     public IReadOnlyList&lt;ResponsivePreviewScreen&gt; Screens { get; } = new[] { new ResponsivePreviewScreen("Settings", "Settings") };
    ///     public bool Show(ResponsivePreviewScreen screen) { MyRouter.Push(screen.Id, screen.Args); return true; }
    /// }
    /// </code>
    /// </summary>
    public interface IResponsivePreviewHost
    {
        /// <summary>The profile the game's <see cref="PanelScaleDriver"/> was created with (the Manager computes sizes outside Play mode with it).</summary>
        PanelScaleProfile Profile { get; }

        /// <summary>The screens the Manager offers, in display order.</summary>
        IReadOnlyList<ResponsivePreviewScreen> Screens { get; }

        /// <summary>Opens <paramref name="screen"/> in the running game. Called in Play mode only. False when it cannot be opened now.</summary>
        bool Show(ResponsivePreviewScreen screen);
    }
}
