namespace FutureDigitalPlus.UIKit.Foundation
{
    /// <summary>The kind of device a panel is shown on, as far as the scale policy cares.</summary>
    public enum DeviceClass
    {
        /// <summary>A handheld whose short side is below 600 dp.</summary>
        Phone = 0,

        /// <summary>A handheld whose short side is 600 dp or more.</summary>
        Tablet = 1,

        /// <summary>Windows, macOS or Linux, in a window or full screen.</summary>
        Desktop = 2,

        /// <summary>A console, a handheld console or a TV platform viewed from a distance.</summary>
        Console = 3,

        /// <summary>A browser (WebGL).</summary>
        Web = 4,

        /// <summary>A panel in the world or a headset.</summary>
        WorldSpace = 5
    }
}
