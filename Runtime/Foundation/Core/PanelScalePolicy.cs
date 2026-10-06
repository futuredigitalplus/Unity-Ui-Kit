using System;

namespace FutureDigitalPlus.UIKit.Foundation
{
    /// <summary>
    /// Chooses the panel scale from a <see cref="PanelScaleProfile"/> and the device. On a phone or tablet the device DPI is real, so the panel keeps physical size
    /// (a 44 unit button is about 7 mm everywhere). In the Editor, in a desktop window and wherever the DPI is missing or implausible, the DPI describes the monitor and not
    /// the device being simulated, so the scale is derived from the window size instead and the panel measures a fixed number of units on its short side.
    /// Pure arithmetic, no Unity types; <c>PanelScaleDriver</c> applies the result to a <c>PanelSettings</c>.
    /// </summary>
    public static class PanelScalePolicy
    {
        /// <summary>The largest user scale that is used (10). A corrupt saved setting (infinity, 1e30) must not make the panel scale unusable; the smallest is 0.1.</summary>
        public const float MaxUserScale = 10f;

        /// <summary>The scale for this profile and device.</summary>
        public static PanelScale Compute(in PanelScaleProfile profile, in PanelScaleInput input)
        {
            float user = input.UserScale > 0.1f ? Math.Min(input.UserScale, MaxUserScale) : 0.1f;
            float referenceDpi = profile.referenceDpi > 0f ? profile.referenceDpi : 160f;
            float shortPixels = Math.Min(input.Width, input.Height);
            float minScale = profile.minScale > 0f ? profile.minScale : 1f;
            float maxScale = profile.maxScale >= minScale ? profile.maxScale : minScale;

            if (profile.strategy == PanelScaleStrategy.ConstantPixel)
                return FromPixelScale(profile.constantScale > 0f ? profile.constantScale * user : user, referenceDpi, shortPixels);

            // A DPI of 0 (or NaN) is "unknown", whatever bounds a profile that nobody filled in has: dividing by it would give a scale of 0.
            if (profile.strategy == PanelScaleStrategy.PhysicalSize && input.Handheld && input.Dpi > 0f && input.Dpi >= profile.minTrustedDpi && input.Dpi <= profile.maxTrustedDpi)
            {
                float effectiveDpi = referenceDpi / user;
                float pixelsPerUnit = input.Dpi / effectiveDpi;
                return new PanelScale(true, 1f, effectiveDpi, pixelsPerUnit, shortPixels / pixelsPerUnit);
            }

            if (!(shortPixels > 0f))
                return FromPixelScale(minScale * user, referenceDpi, 0f);

            float units = profile.strategy == PanelScaleStrategy.ReferenceResolution ? profile.referenceShortUnits : UnitsFor(profile, input, shortPixels);
            if (!(units > 0f))
                units = profile.phoneShortUnits > 0f ? profile.phoneShortUnits : 390f;
            float scale = Math.Min(maxScale, Math.Max(minScale, shortPixels / units));
            return FromPixelScale(scale * user, referenceDpi, shortPixels);
        }

        static float UnitsFor(in PanelScaleProfile profile, in PanelScaleInput input, float shortPixels)
        {
            if (profile.simulateHandheldInWindow)
                return shortPixels <= profile.phoneMaxShortPixels ? profile.phoneShortUnits : profile.tabletShortUnits;
            switch (input.Device)
            {
                case DeviceClass.Phone:
                    return profile.phoneShortUnits;
                case DeviceClass.Tablet:
                    return profile.tabletShortUnits;
                case DeviceClass.Console:
                    return profile.consoleShortUnits;
                case DeviceClass.Web:
                    return profile.webShortUnits;
                default:
                    return profile.desktopShortUnits;
            }
        }

        static PanelScale FromPixelScale(float pixelScale, float referenceDpi, float shortPixels)
        {
            return new PanelScale(false, pixelScale, referenceDpi, pixelScale, pixelScale > 0f ? shortPixels / pixelScale : 0f);
        }
    }
}
