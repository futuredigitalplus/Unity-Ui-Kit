namespace FutureDigitalPlus.UIKit.Foundation
{
    /// <summary>Which orientation a <see cref="ResponsiveRule"/> applies to. The numbers are stored by rule assets: append, never renumber.</summary>
    public enum ResponsiveOrientation
    {
        /// <summary>Both orientations (the fallback when no orientation-specific rule exists).</summary>
        Any = 0,

        /// <summary>Taller than wide.</summary>
        Portrait = 1,

        /// <summary>Wider than tall.</summary>
        Landscape = 2,
    }
}
