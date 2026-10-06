namespace FutureDigitalPlus.UIKit.Foundation
{
    /// <summary>How <see cref="PanelScalePolicy"/> derives the scale of a panel.</summary>
    public enum PanelScaleStrategy
    {
        /// <summary>Handhelds with a trustworthy DPI use true physical size (1 unit is 1/160 inch); everything else is derived from the window like <see cref="WindowDerived"/>.</summary>
        PhysicalSize = 0,

        /// <summary>The scale is derived from the window: the short side of the panel measures about the device's reference number of units, whatever the DPI.</summary>
        WindowDerived = 1,

        /// <summary>The short side of the panel always measures <see cref="PanelScaleProfile.referenceShortUnits"/> units, on every device.</summary>
        ReferenceResolution = 2,

        /// <summary>A fixed pixel scale, <see cref="PanelScaleProfile.constantScale"/>, whatever the window (render textures, world-space panels).</summary>
        ConstantPixel = 3
    }
}
