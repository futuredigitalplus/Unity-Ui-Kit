using System;

namespace FutureDigitalPlus.UIKit.Foundation
{
    /// <summary>
    /// One screen of a device in a responsive catalog: its pixel size held upright, its DPI, the safe-area insets in pixels for each orientation, and the hinge of a
    /// foldable screen that can be half opened. A phone has one screen; a flip or fold phone has a cover screen and a main or inner one. Plain serializable values
    /// (the catalog is JSON).
    /// </summary>
    [Serializable]
    public sealed class ResponsiveScreenSpec
    {
        /// <summary>Name of the screen in its device: Main, Cover, Inner.</summary>
        public string name = "Main";

        /// <summary>Width in pixels, held upright.</summary>
        public int width;

        /// <summary>Height in pixels, held upright.</summary>
        public int height;

        /// <summary>Pixels per inch.</summary>
        public float dpi;

        /// <summary>The hinge direction when the screen is held upright, or <see cref="HingeAxis.None"/> for a screen that cannot be half opened.</summary>
        public HingeAxis hinge;

        /// <summary>Safe-area insets in pixels when held upright: top, bottom, left, right.</summary>
        public int[] safePortrait = new int[4];

        /// <summary>Safe-area insets in pixels when held sideways: top, bottom, left, right.</summary>
        public int[] safeLandscape = new int[4];

        /// <summary>The pixel size as held: width and height swapped in landscape.</summary>
        public void PixelSize(bool landscape, out int pixelWidth, out int pixelHeight)
        {
            int shortSide = Math.Min(width, height);
            int longSide = Math.Max(width, height);
            bool upright = width <= height;
            if (landscape == upright)
            {
                pixelWidth = longSide;
                pixelHeight = shortSide;
            }
            else
            {
                pixelWidth = shortSide;
                pixelHeight = longSide;
            }
        }

        /// <summary>The safe-area insets in pixels for an orientation, as top, bottom, left, right (zeros when the catalog has none).</summary>
        public SafeAreaInsets Insets(bool landscape)
        {
            int[] inset = landscape ? safeLandscape : safePortrait;
            if (inset == null || inset.Length < 4)
                return SafeAreaInsets.Zero;
            // stored top, bottom, left, right; SafeAreaInsets takes left, top, right, bottom
            return new SafeAreaInsets(Math.Max(0, inset[2]), Math.Max(0, inset[0]), Math.Max(0, inset[3]), Math.Max(0, inset[1]));
        }

        /// <summary>True when the device is a tablet by the platform rule (short side of 600 density-independent points or more).</summary>
        public bool IsTablet
        {
            get
            {
                float shortSide = Math.Min(width, height);
                return dpi > 0f && shortSide / (dpi / 160f) >= 600f;
            }
        }

        /// <summary>The panel scale this screen gets from <paramref name="profile"/> as a handheld, with a user scale of <paramref name="userScale"/>.</summary>
        public PanelScale Scale(in PanelScaleProfile profile, bool landscape, float userScale = 1f)
        {
            PixelSize(landscape, out int w, out int h);
            return PanelScalePolicy.Compute(profile, new PanelScaleInput(true, IsTablet ? DeviceClass.Tablet : DeviceClass.Phone, dpi, w, h, userScale));
        }

        /// <summary>The form factor this screen is classified as under <paramref name="profile"/> (at the policy scale, without a rule).</summary>
        public DeviceFormFactor FormFactor(in PanelScaleProfile profile)
        {
            PanelScale scale = Scale(profile, false);
            float pixels = scale.PixelsPerUnit > 0f ? scale.PixelsPerUnit : 1f;
            return DeviceFormFactors.Classify(width / pixels, height / pixels);
        }
    }
}
