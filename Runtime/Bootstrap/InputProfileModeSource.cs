using System;
using FutureDigitalPlus.UIKit.Foundation;
using FutureDigitalPlus.UIKit.Interaction;

namespace FutureDigitalPlus.UIKit.Bootstrap
{
    /// <summary>
    /// Lets a <see cref="UiShell"/> take its input mode from an <see cref="InputProfile"/> (Interaction module): the shell's touch-size tier and focus ring then follow the
    /// device the player used last. The two modules do not reference each other, so this adapter lives here; it only renames the enum values.
    /// </summary>
    public sealed class InputProfileModeSource : IUiInputModeSource
    {
        private readonly InputProfile _profile;

        /// <summary>Follows <paramref name="profile"/> until it is disposed. A null profile reports <see cref="UiInputMode.Pointer"/> for ever.</summary>
        public InputProfileModeSource(InputProfile profile)
        {
            _profile = profile;
            if (_profile != null)
                _profile.ModeChanged += OnProfileModeChanged;
        }

        /// <inheritdoc />
        public UiInputMode Mode => _profile == null ? UiInputMode.Pointer : Map(_profile.Mode);

        /// <inheritdoc />
        public event Action<UiInputMode> ModeChanged;

        /// <summary>Stops following the profile.</summary>
        public void Detach()
        {
            if (_profile != null)
                _profile.ModeChanged -= OnProfileModeChanged;
        }

        /// <summary>The shell's name for an Interaction device kind.</summary>
        public static UiInputMode Map(InputDeviceKind kind)
        {
            switch (kind)
            {
                case InputDeviceKind.Touch: return UiInputMode.Touch;
                case InputDeviceKind.Keyboard: return UiInputMode.Keyboard;
                case InputDeviceKind.Gamepad: return UiInputMode.Gamepad;
                default: return UiInputMode.Pointer;
            }
        }

        private void OnProfileModeChanged(InputDeviceKind kind) => ModeChanged?.Invoke(Map(kind));
    }
}
