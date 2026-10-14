namespace FutureDigitalPlus.UIKit.Foundation
{
    /// <summary>How a foldable is held. The size of the screen does not tell it: a host or a preview sets it (<c>ResponsiveRootBinder.Posture</c>).</summary>
    public enum DevicePosture
    {
        /// <summary>Flat: a normal phone or tablet, or a foldable that is fully open or closed.</summary>
        Flat = 0,

        /// <summary>Half opened (tabletop or book): a hinge crosses the screen along a <see cref="HingeAxis"/>.</summary>
        HalfOpened = 1,
    }
}
