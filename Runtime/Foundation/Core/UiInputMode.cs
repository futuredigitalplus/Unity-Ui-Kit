namespace FutureDigitalPlus.UIKit.Foundation
{
    /// <summary>The kind of device the player used last. It decides the touch-size tier of controls and whether the focus ring shows.</summary>
    public enum UiInputMode
    {
        /// <summary>Mouse or trackpad.</summary>
        Pointer = 0,

        /// <summary>A finger or pen on a touch screen.</summary>
        Touch = 1,

        /// <summary>A keyboard used for navigation.</summary>
        Keyboard = 2,

        /// <summary>A gamepad or remote.</summary>
        Gamepad = 3
    }
}
