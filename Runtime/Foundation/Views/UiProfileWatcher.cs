using System;
using UnityEngine;
using UnityEngine.UIElements;

namespace FutureDigitalPlus.UIKit.Foundation
{
    /// <summary>
    /// Keeps the layout classes of the shell root current, so a screen changes layout with plain descendant selectors (USS has no media queries):
    /// <c>fdp-portrait</c> or <c>fdp-landscape</c>; <c>fdp-compact</c>, <c>fdp-regular</c> or <c>fdp-expanded</c> (plus <c>fdp-regular-up</c> for the last two) from the panel size in units (so split view and
    /// resizable windows get the right class); <c>fdp-platform-ios|android|windows|macos|linux|webgl|console|xr</c> once; and the input mode classes
    /// <c>fdp-input-touch|mouse|keyboard|gamepad</c> plus <c>fdp-input-nav</c> for keyboard and gamepad, from an <see cref="IUiInputModeSource"/>, or from the
    /// platform when no source is given (touch on phones and tablets, gamepad on consoles, mouse elsewhere).
    /// Driven by <see cref="GeometryChangedEvent"/> of the root and by the source's mode changes, never by polling. Locale and accessibility classes belong to
    /// the Language and Interaction modules.
    /// </summary>
    public sealed class UiProfileWatcher : IDisposable
    {
        static readonly UiInputMode[] AllModes = { UiInputMode.Pointer, UiInputMode.Touch, UiInputMode.Keyboard, UiInputMode.Gamepad };

        readonly VisualElement _root;
        readonly IUiInputModeSource _inputSource;
        UiInputMode _mode;
        bool _disposed;

        /// <summary>The profile that is applied right now.</summary>
        public UiProfile Profile { get; private set; }

        /// <summary>Raised when orientation, size class or input mode changed.</summary>
        public event Action<UiProfile> Changed;

        /// <summary>Starts watching <paramref name="root"/>. <paramref name="inputSource"/> may be null.</summary>
        public UiProfileWatcher(VisualElement root, IUiInputModeSource inputSource = null)
        {
            if (root == null)
                throw new ArgumentNullException(nameof(root));
            _root = root;
            _inputSource = inputSource;
            _mode = inputSource != null ? inputSource.Mode : DefaultInputMode(UnityEngine.Device.Application.platform);
            ApplyPlatformClass(UnityEngine.Device.Application.platform);
            ApplyProfile(true);
            _root.RegisterCallback<GeometryChangedEvent>(OnGeometryChanged);
            if (_inputSource != null)
                _inputSource.ModeChanged += OnModeChanged;
        }

        /// <summary>Stops watching. The classes stay on the root.</summary>
        public void Dispose()
        {
            if (_disposed)
                return;
            _disposed = true;
            _root.UnregisterCallback<GeometryChangedEvent>(OnGeometryChanged);
            if (_inputSource != null)
                _inputSource.ModeChanged -= OnModeChanged;
        }

        /// <summary>The input mode a platform starts with: touch on phones and tablets, gamepad on consoles and tvOS, mouse everywhere else.</summary>
        public static UiInputMode DefaultInputMode(RuntimePlatform platform)
        {
            switch (platform)
            {
                case RuntimePlatform.IPhonePlayer:
                case RuntimePlatform.Android:
                    return UiInputMode.Touch;
                case RuntimePlatform.tvOS:
                case RuntimePlatform.Switch:
                case RuntimePlatform.PS4:
                case RuntimePlatform.PS5:
                case RuntimePlatform.XboxOne:
                case RuntimePlatform.GameCoreXboxOne:
                case RuntimePlatform.GameCoreXboxSeries:
                    return UiInputMode.Gamepad;
                default:
                    return UiInputMode.Pointer;
            }
        }

        /// <summary>The platform part of the <c>fdp-platform-*</c> class for <paramref name="platform"/>: <c>ios</c>, <c>android</c>, <c>windows</c>, <c>macos</c>, <c>linux</c>, <c>webgl</c>, <c>console</c>, <c>xr</c>, or <c>other</c>.</summary>
        public static string PlatformName(RuntimePlatform platform)
        {
            switch (platform)
            {
                case RuntimePlatform.IPhonePlayer:
                    return "ios";
                case RuntimePlatform.Android:
                    return "android";
                case RuntimePlatform.WindowsPlayer:
                case RuntimePlatform.WindowsEditor:
                    return "windows";
                case RuntimePlatform.OSXPlayer:
                case RuntimePlatform.OSXEditor:
                    return "macos";
                case RuntimePlatform.LinuxPlayer:
                case RuntimePlatform.LinuxEditor:
                    return "linux";
                case RuntimePlatform.WebGLPlayer:
                    return "webgl";
                case RuntimePlatform.tvOS:
                case RuntimePlatform.Switch:
                case RuntimePlatform.PS4:
                case RuntimePlatform.PS5:
                case RuntimePlatform.XboxOne:
                case RuntimePlatform.GameCoreXboxOne:
                case RuntimePlatform.GameCoreXboxSeries:
                    return "console";
                case RuntimePlatform.VisionOS:
                    return "xr";
                default:
                    return "other";
            }
        }

        /// <summary>The input part of the <c>fdp-input-*</c> class: <c>mouse</c>, <c>touch</c>, <c>keyboard</c> or <c>gamepad</c>.</summary>
        public static string InputName(UiInputMode mode)
        {
            switch (mode)
            {
                case UiInputMode.Touch:
                    return "touch";
                case UiInputMode.Keyboard:
                    return "keyboard";
                case UiInputMode.Gamepad:
                    return "gamepad";
                default:
                    return "mouse";
            }
        }

        void OnGeometryChanged(GeometryChangedEvent evt)
        {
            ApplyProfile(false);
        }

        void OnModeChanged(UiInputMode mode)
        {
            _mode = mode;
            ApplyProfile(false);
        }

        void ApplyProfile(bool force)
        {
            Rect layout = _root.layout;
            UiProfile next = UiProfile.Compute(layout.width, layout.height, _mode);
            if (!force && next.Equals(Profile))
                return;
            Profile = next;
            _root.EnableInClassList(UiNames.ClassPortrait, next.Orientation == UiOrientation.Portrait);
            _root.EnableInClassList(UiNames.ClassLandscape, next.Orientation == UiOrientation.Landscape);
            _root.EnableInClassList(UiNames.ClassCompact, next.SizeClass == UiSizeClass.Compact);
            _root.EnableInClassList(UiNames.ClassRegular, next.SizeClass == UiSizeClass.Regular);
            _root.EnableInClassList(UiNames.ClassExpanded, next.SizeClass == UiSizeClass.Expanded);
            _root.EnableInClassList(UiNames.ClassRegularUp, next.IsRegularOrLarger);
            for (int i = 0; i < AllModes.Length; i++)
                _root.EnableInClassList(UiNames.ClassInputPrefix + InputName(AllModes[i]), AllModes[i] == next.InputMode);
            _root.EnableInClassList(UiNames.ClassInputNav, next.InputMode == UiInputMode.Keyboard || next.InputMode == UiInputMode.Gamepad);
            Action<UiProfile> handler = Changed;
            if (handler != null)
                handler(next);
        }

        void ApplyPlatformClass(RuntimePlatform platform)
        {
            _root.AddToClassList(UiNames.ClassPlatformPrefix + PlatformName(platform));
        }
    }
}
