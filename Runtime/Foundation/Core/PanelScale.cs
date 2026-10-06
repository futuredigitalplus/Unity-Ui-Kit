namespace FutureDigitalPlus.UIKit.Foundation
{
    /// <summary>How a UI Toolkit panel is scaled: true physical size, or a plain pixel scale. Computed by <see cref="PanelScalePolicy"/>.</summary>
    public readonly struct PanelScale
    {
        /// <summary>Creates a scale.</summary>
        public PanelScale(bool physical, float pixelScale, float referenceDpi, float pixelsPerUnit, float shortSideUnits)
        {
            Physical = physical;
            PixelScale = pixelScale;
            ReferenceDpi = referenceDpi;
            PixelsPerUnit = pixelsPerUnit;
            ShortSideUnits = shortSideUnits;
        }

        /// <summary>True: <c>ConstantPhysicalSize</c> with <see cref="ReferenceDpi"/> (phones and tablets with a trustworthy DPI). False: <c>ConstantPixelSize</c> with <see cref="PixelScale"/>.</summary>
        public bool Physical { get; }

        /// <summary>Scale for <c>ConstantPixelSize</c> when <see cref="Physical"/> is false (the user scale is included).</summary>
        public float PixelScale { get; }

        /// <summary>Reference DPI for <c>ConstantPhysicalSize</c> when <see cref="Physical"/> is true (the user scale is folded in: a larger user scale gives a smaller reference DPI).</summary>
        public float ReferenceDpi { get; }

        /// <summary>Screen pixels per panel unit under this scale.</summary>
        public float PixelsPerUnit { get; }

        /// <summary>Length of the short side of the panel in units under this scale.</summary>
        public float ShortSideUnits { get; }
    }
}
