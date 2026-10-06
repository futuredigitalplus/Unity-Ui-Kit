using System;

namespace FutureDigitalPlus.UIKit.Foundation
{
    /// <summary>The facts a layout can depend on: orientation, size class and input mode. Computed from the size of the panel in units, never from the device model.</summary>
    public readonly struct UiProfile : IEquatable<UiProfile>
    {
        /// <summary>Largest short side, in units, of the <see cref="UiSizeClass.Compact"/> tier (exclusive).</summary>
        public const float RegularMinUnits = 600f;

        /// <summary>Short side, in units, from which the tier is <see cref="UiSizeClass.Expanded"/>.</summary>
        public const float ExpandedMinUnits = 840f;

        /// <summary>Whether the panel is wider than tall.</summary>
        public UiOrientation Orientation { get; }

        /// <summary>Size tier by the short side.</summary>
        public UiSizeClass SizeClass { get; }

        /// <summary>The device the player used last.</summary>
        public UiInputMode InputMode { get; }

        /// <summary>
        /// Whether the tier is <see cref="UiSizeClass.Regular"/> or <see cref="UiSizeClass.Expanded"/>, that is, the short side is at least
        /// <see cref="RegularMinUnits"/>. For layouts that have only two tiers, "phone" and "tablet or larger".
        /// </summary>
        public bool IsRegularOrLarger
        {
            get { return SizeClass != UiSizeClass.Compact; }
        }

        /// <summary>Creates a profile.</summary>
        public UiProfile(UiOrientation orientation, UiSizeClass sizeClass, UiInputMode inputMode)
        {
            Orientation = orientation;
            SizeClass = sizeClass;
            InputMode = inputMode;
        }

        /// <summary>
        /// The profile of a panel that is <paramref name="widthUnits"/> by <paramref name="heightUnits"/> units. A panel without a size yet
        /// (zero, negative or NaN) is landscape and compact; a square panel is landscape.
        /// </summary>
        public static UiProfile Compute(float widthUnits, float heightUnits, UiInputMode inputMode)
        {
            if (!(widthUnits > 0f) || !(heightUnits > 0f))
                return new UiProfile(UiOrientation.Landscape, UiSizeClass.Compact, inputMode);
            UiOrientation orientation = widthUnits < heightUnits ? UiOrientation.Portrait : UiOrientation.Landscape;
            float shortSide = Math.Min(widthUnits, heightUnits);
            UiSizeClass sizeClass = shortSide < RegularMinUnits ? UiSizeClass.Compact : shortSide < ExpandedMinUnits ? UiSizeClass.Regular : UiSizeClass.Expanded;
            return new UiProfile(orientation, sizeClass, inputMode);
        }

        /// <summary>Same orientation, size class and input mode.</summary>
        public bool Equals(UiProfile other)
        {
            return Orientation == other.Orientation && SizeClass == other.SizeClass && InputMode == other.InputMode;
        }

        /// <summary>Same orientation, size class and input mode.</summary>
        public override bool Equals(object obj)
        {
            return obj is UiProfile && Equals((UiProfile)obj);
        }

        /// <summary>Hash of the three parts.</summary>
        public override int GetHashCode()
        {
            return ((int)Orientation * 31 + (int)SizeClass) * 31 + (int)InputMode;
        }

        /// <summary>For example <c>Portrait/Compact/Touch</c>.</summary>
        public override string ToString()
        {
            return Orientation + "/" + SizeClass + "/" + InputMode;
        }
    }
}
