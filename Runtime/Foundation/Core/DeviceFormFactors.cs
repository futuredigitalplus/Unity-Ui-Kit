using System;

namespace FutureDigitalPlus.UIKit.Foundation
{
    /// <summary>
    /// Classifies a panel by its size in units (one unit is 1/160 inch on a handheld, see <see cref="PanelScalePolicy"/>) into a <see cref="DeviceFormFactor"/>,
    /// and names the root class of each form factor. Size only: it works on every platform without a native plugin, and it cannot see a hinge
    /// (<see cref="DevicePosture"/> is set separately). The thresholds were measured on the cover and inner screens of current flip and fold phones:
    /// flip covers are 376 to 414 units on the short side with a ratio of 1.04 to 1.18; passport fold covers 467 to 486 with 1.45 to 1.58; fold inner screens
    /// 701 to 888 with 1.04 to 1.25; phones have a ratio of 1.78 or more and tablets 1.33 to 1.43.
    /// </summary>
    public static class DeviceFormFactors
    {
        /// <summary>Short side, in units, from which a screen is a tablet or a fold inner screen (the <see cref="UiProfile.RegularMinUnits"/> breakpoint).</summary>
        public const float LargeMinShortUnits = 600f;

        /// <summary>Long side over short side below which a screen counts as nearly square (flip cover, fold inner).</summary>
        public const float SquareMaxRatio = 1.3f;

        /// <summary>Long side over short side below which a small screen counts as a passport fold cover rather than a phone.</summary>
        public const float CoverMaxRatio = 1.75f;

        /// <summary>The prefix of the form-factor root classes: <c>fdp-ff-phone</c>, <c>fdp-ff-pad</c>, <c>fdp-ff-flip-cover</c>, <c>fdp-ff-fold-cover</c>, <c>fdp-ff-fold-inner</c>.</summary>
        public const string ClassPrefix = "fdp-ff-";

        /// <summary>Root class of a half-opened foldable (<see cref="DevicePosture.HalfOpened"/>).</summary>
        public const string ClassHalfOpened = "fdp-posture-half-opened";

        /// <summary>Root class of a horizontal hinge (with <see cref="ClassHalfOpened"/>).</summary>
        public const string ClassHingeHorizontal = "fdp-hinge-horizontal";

        /// <summary>Root class of a vertical hinge (with <see cref="ClassHalfOpened"/>).</summary>
        public const string ClassHingeVertical = "fdp-hinge-vertical";

        /// <summary>Every form factor, in enum order.</summary>
        public static readonly DeviceFormFactor[] All =
        {
            DeviceFormFactor.Phone, DeviceFormFactor.Pad, DeviceFormFactor.FlipCover, DeviceFormFactor.FoldCover, DeviceFormFactor.FoldInner
        };

        /// <summary>
        /// The form factor of a panel <paramref name="widthUnits"/> by <paramref name="heightUnits"/> units. Orientation does not matter. A size that is not positive
        /// or not finite (no layout yet) is a <see cref="DeviceFormFactor.Phone"/>.
        /// </summary>
        public static DeviceFormFactor Classify(float widthUnits, float heightUnits)
        {
            if (!(widthUnits > 0f) || !(heightUnits > 0f) || float.IsInfinity(widthUnits) || float.IsInfinity(heightUnits))
                return DeviceFormFactor.Phone;
            float shortSide = Math.Min(widthUnits, heightUnits);
            float ratio = Math.Max(widthUnits, heightUnits) / shortSide;
            if (shortSide >= LargeMinShortUnits)
                return ratio < SquareMaxRatio ? DeviceFormFactor.FoldInner : DeviceFormFactor.Pad;
            if (ratio < SquareMaxRatio)
                return DeviceFormFactor.FlipCover;
            return ratio < CoverMaxRatio ? DeviceFormFactor.FoldCover : DeviceFormFactor.Phone;
        }

        /// <summary>The root class of <paramref name="form"/>, for example <c>fdp-ff-flip-cover</c>.</summary>
        public static string ClassName(DeviceFormFactor form)
        {
            return ClassPrefix + Slug(form);
        }

        /// <summary>The short name of <paramref name="form"/> used in classes and files: <c>phone</c>, <c>pad</c>, <c>flip-cover</c>, <c>fold-cover</c>, <c>fold-inner</c>.</summary>
        public static string Slug(DeviceFormFactor form)
        {
            switch (form)
            {
                case DeviceFormFactor.Pad: return "pad";
                case DeviceFormFactor.FlipCover: return "flip-cover";
                case DeviceFormFactor.FoldCover: return "fold-cover";
                case DeviceFormFactor.FoldInner: return "fold-inner";
                default: return "phone";
            }
        }

        /// <summary>A display name for tools: Phone, Pad, Flip cover, Fold cover, Fold inner.</summary>
        public static string DisplayName(DeviceFormFactor form)
        {
            switch (form)
            {
                case DeviceFormFactor.Pad: return "Pad";
                case DeviceFormFactor.FlipCover: return "Flip cover";
                case DeviceFormFactor.FoldCover: return "Fold cover";
                case DeviceFormFactor.FoldInner: return "Fold inner";
                default: return "Phone";
            }
        }

        /// <summary>
        /// The form factor whose rule a form factor without one of its own falls back to: the covers fall back to <see cref="DeviceFormFactor.Phone"/>, a fold inner screen
        /// to <see cref="DeviceFormFactor.Pad"/>; phone and pad have no fallback (they return themselves).
        /// </summary>
        public static DeviceFormFactor FallbackOf(DeviceFormFactor form)
        {
            switch (form)
            {
                case DeviceFormFactor.FlipCover:
                case DeviceFormFactor.FoldCover:
                    return DeviceFormFactor.Phone;
                case DeviceFormFactor.FoldInner:
                    return DeviceFormFactor.Pad;
                default:
                    return form;
            }
        }

        /// <summary>The hinge direction of a screen held in <paramref name="landscape"/> whose hinge runs <paramref name="axisInPortrait"/> when it is held upright.</summary>
        public static HingeAxis HingeAs(HingeAxis axisInPortrait, bool landscape)
        {
            if (!landscape || axisInPortrait == HingeAxis.None)
                return axisInPortrait;
            return axisInPortrait == HingeAxis.Horizontal ? HingeAxis.Vertical : HingeAxis.Horizontal;
        }
    }
}
