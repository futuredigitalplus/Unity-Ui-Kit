namespace FutureDigitalPlus.UIKit.Foundation
{
    /// <summary>Size tier of a panel from its short side in panel units (1 unit = 1 dp on handhelds). Layout switches by tier, not by device.</summary>
    public enum UiSizeClass
    {
        /// <summary>Short side below 600 units: phones, split view, small windows.</summary>
        Compact = 0,

        /// <summary>600 up to 840 units: large phones in landscape, small tablets, medium windows.</summary>
        Regular = 1,

        /// <summary>840 units and more: tablets, desktop and TV.</summary>
        Expanded = 2
    }
}
