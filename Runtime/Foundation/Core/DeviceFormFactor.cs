namespace FutureDigitalPlus.UIKit.Foundation
{
    /// <summary>
    /// The shape of the screen a panel is shown on, as <see cref="DeviceFormFactors.Classify"/> reads it from the panel size in units. Finer than
    /// <see cref="UiSizeClass"/>: it tells the cover screens of flip and fold phones and the near-square inner screen of a fold from a phone and a tablet.
    /// The numbers are stored by rule assets: append, never renumber.
    /// </summary>
    public enum DeviceFormFactor
    {
        /// <summary>A phone, or the tall cover screen of a fold that is shaped like one.</summary>
        Phone = 0,

        /// <summary>A tablet, or a large inner screen with the aspect ratio of a tablet.</summary>
        Pad = 1,

        /// <summary>The small, nearly square cover screen of a closed flip phone (about 380 to 490 units a side).</summary>
        FlipCover = 2,

        /// <summary>The wide "passport" cover screen of a closed fold (short side under 600 units, ratio 1.3 to 1.75).</summary>
        FoldCover = 3,

        /// <summary>The large, nearly square inner screen of an opened fold (short side 600 units and more, ratio under 1.3).</summary>
        FoldInner = 4,
    }
}
