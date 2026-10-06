namespace FutureDigitalPlus.UIKit.Interaction
{
    /// <summary>The kind of input device the player used last. It decides whether focus rings are shown and which hit sizes make sense.</summary>
    public enum InputDeviceKind
    {
        /// <summary>Mouse, pen or any other pointer that can hover. Hover states are meaningful; focus rings are hidden.</summary>
        Pointer = 0,

        /// <summary>A finger on a touch screen. No hover, no focus ring, large hit areas.</summary>
        Touch = 1,

        /// <summary>A keyboard. Focus rings are visible.</summary>
        Keyboard = 2,

        /// <summary>A gamepad, joystick or D-pad remote. Focus rings are visible.</summary>
        Gamepad = 3,
    }
}
