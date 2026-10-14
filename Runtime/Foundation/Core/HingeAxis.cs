namespace FutureDigitalPlus.UIKit.Foundation
{
    /// <summary>The direction of the hinge line across a screen, as the screen is held now.</summary>
    public enum HingeAxis
    {
        /// <summary>No hinge crosses the screen.</summary>
        None = 0,

        /// <summary>The hinge runs left to right across the middle (tabletop: content above, controls below).</summary>
        Horizontal = 1,

        /// <summary>The hinge runs top to bottom down the middle (book).</summary>
        Vertical = 2,
    }
}
