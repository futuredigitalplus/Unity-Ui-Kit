using System;

namespace FutureDigitalPlus.UIKit.Foundation
{
    /// <summary>
    /// The numbers behind <see cref="PanelScalePolicy"/>: the strategy, the units the short side of a panel should measure on each kind of device, and the limits.
    /// Start from a preset (<see cref="CardPortrait"/>, <see cref="DesktopHud"/>, <see cref="Console"/>, <see cref="Web"/>, <see cref="World"/>) and change fields.
    /// A struct of plain values, so it can be a serialized field.
    /// </summary>
    [Serializable]
    public struct PanelScaleProfile
    {
        /// <summary>How the scale is derived.</summary>
        public PanelScaleStrategy strategy;

        /// <summary>Reference DPI of physical scaling: one unit is 1/<c>referenceDpi</c> inch. 160 makes a unit a density-independent point.</summary>
        public float referenceDpi;

        /// <summary>DPI values below this are not trusted (desktop monitors report 72, 96 or 110).</summary>
        public float minTrustedDpi;

        /// <summary>DPI values above this are not trusted.</summary>
        public float maxTrustedDpi;

        /// <summary>Units of the short side on a phone-sized window.</summary>
        public float phoneShortUnits;

        /// <summary>Units of the short side on a tablet-sized window.</summary>
        public float tabletShortUnits;

        /// <summary>Units of the short side in a desktop window.</summary>
        public float desktopShortUnits;

        /// <summary>Units of the short side on a console or TV.</summary>
        public float consoleShortUnits;

        /// <summary>Units of the short side in a browser.</summary>
        public float webShortUnits;

        /// <summary>Units of the short side for <see cref="PanelScaleStrategy.ReferenceResolution"/>.</summary>
        public float referenceShortUnits;

        /// <summary>The pixel scale for <see cref="PanelScaleStrategy.ConstantPixel"/>.</summary>
        public float constantScale;

        /// <summary>
        /// When true, whatever is not scaled by physical size (a desktop, web or console window, or a handheld without a trustworthy DPI) is treated like the handheld it simulates: windows with a short side up to <see cref="phoneMaxShortPixels"/> pixels
        /// get <see cref="phoneShortUnits"/>, larger ones <see cref="tabletShortUnits"/>. The card game uses this so the editor Game view looks like the device.
        /// </summary>
        public bool simulateHandheldInWindow;

        /// <summary>Windows with a short side up to this many pixels count as phone-sized when <see cref="simulateHandheldInWindow"/> is on.</summary>
        public float phoneMaxShortPixels;

        /// <summary>Smallest pixel scale of the window-derived strategies.</summary>
        public float minScale;

        /// <summary>Largest pixel scale of the window-derived strategies.</summary>
        public float maxScale;

        /// <summary>
        /// Portrait card or puzzle game: physical size on phones and tablets (a 44 unit button is about 7 mm everywhere); windows and the editor look like the device
        /// they simulate (390 units short side on a phone-sized window, 820 on a tablet-sized one). The proven constants of the first game on the kit.
        /// </summary>
        public static PanelScaleProfile CardPortrait()
        {
            PanelScaleProfile p = Common();
            p.strategy = PanelScaleStrategy.PhysicalSize;
            p.simulateHandheldInWindow = true;
            return p;
        }

        /// <summary>A game with HUDs and menus on handhelds and desktop: physical size on handhelds, a 720 unit short side in desktop windows (1080p gives a scale of 1.5), 540 on consoles (1080p gives 2).</summary>
        public static PanelScaleProfile DesktopHud()
        {
            PanelScaleProfile p = Common();
            p.strategy = PanelScaleStrategy.PhysicalSize;
            return p;
        }

        /// <summary>A console or TV game: the short side is 540 units whatever the output resolution, so 1080p renders at scale 2 and 4K at 4.</summary>
        public static PanelScaleProfile Console()
        {
            PanelScaleProfile p = Common();
            p.strategy = PanelScaleStrategy.WindowDerived;
            return p;
        }

        /// <summary>A browser game: the short side is 720 units whatever the canvas size.</summary>
        public static PanelScaleProfile Web()
        {
            PanelScaleProfile p = Common();
            p.strategy = PanelScaleStrategy.WindowDerived;
            return p;
        }

        /// <summary>A world-space or render-texture panel: scale 1, one unit per texel.</summary>
        public static PanelScaleProfile World()
        {
            PanelScaleProfile p = Common();
            p.strategy = PanelScaleStrategy.ConstantPixel;
            return p;
        }

        static PanelScaleProfile Common()
        {
            return new PanelScaleProfile
            {
                referenceDpi = 160f,
                minTrustedDpi = 120f,
                maxTrustedDpi = 700f,
                phoneShortUnits = 390f,
                tabletShortUnits = 820f,
                desktopShortUnits = 720f,
                consoleShortUnits = 540f,
                webShortUnits = 720f,
                referenceShortUnits = 720f,
                constantScale = 1f,
                simulateHandheldInWindow = false,
                phoneMaxShortPixels = 1100f,
                minScale = 1f,
                maxScale = 4f
            };
        }
    }
}
