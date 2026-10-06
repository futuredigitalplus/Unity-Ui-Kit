namespace FutureDigitalPlus.UIKit.Foundation
{
    /// <summary>Tells the shell that the platform back key was pressed (Android back, Escape, a gamepad button). The Interaction module has implementations for both Unity input systems.</summary>
    public interface IBackKeySource
    {
        /// <summary>True during the frame in which the back key went down. May be called more than once per frame.</summary>
        bool WasPressedThisFrame();
    }
}
