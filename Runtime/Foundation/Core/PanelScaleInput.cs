namespace FutureDigitalPlus.UIKit.Foundation
{
    /// <summary>What the scale policy needs to know about the device right now. Plain numbers, so the policy needs no Unity types.</summary>
    public readonly struct PanelScaleInput
    {
        /// <summary>True on phones and tablets (a handheld whose DPI describes the device itself).</summary>
        public bool Handheld { get; }

        /// <summary>The kind of device.</summary>
        public DeviceClass Device { get; }

        /// <summary>Screen DPI as reported by the device; 0 when unknown.</summary>
        public float Dpi { get; }

        /// <summary>Panel target width in pixels.</summary>
        public int Width { get; }

        /// <summary>Panel target height in pixels.</summary>
        public int Height { get; }

        /// <summary>The player's UI size multiplier (1 = as designed).</summary>
        public float UserScale { get; }

        /// <summary>Creates the input.</summary>
        public PanelScaleInput(bool handheld, DeviceClass device, float dpi, int width, int height, float userScale)
        {
            Handheld = handheld;
            Device = device;
            Dpi = dpi;
            Width = width;
            Height = height;
            UserScale = userScale;
        }
    }
}
