using System;
using UnityEngine;
using UnityEngine.UIElements;

namespace FutureDigitalPlus.UIKit.Foundation
{
    /// <summary>What the <see cref="PanelScaleDriver"/> reads from the device: the facts the scale policy needs.</summary>
    public struct PanelScaleEnvironment
    {
        /// <summary>True on phones and tablets (a handheld device whose DPI describes the device itself).</summary>
        public bool Handheld;

        /// <summary>The kind of device (phone, tablet, desktop, console, web).</summary>
        public DeviceClass Device;

        /// <summary>Screen DPI as reported by the device (0 when unknown).</summary>
        public float Dpi;

        /// <summary>Screen width in pixels.</summary>
        public int Width;

        /// <summary>Screen height in pixels.</summary>
        public int Height;
    }

    /// <summary>
    /// Applies the scale policy to a PanelSettings and keeps it current. It never edits the asset it was given: it works on a runtime copy
    /// (<see cref="Settings"/>) that you assign to the UIDocument, and re-evaluates when the screen width, height or DPI change (rotation, window resize,
    /// moving to another monitor). <see cref="UserScale"/> is the player's "UI size" setting. Call <see cref="Tick"/> once per frame (or when the window
    /// changes) from your host. Inputs come from <c>UnityEngine.Device</c>, so the Device Simulator exercises the whole chain.
    /// <code>
    /// var driver = new PanelScaleDriver(document.panelSettings, PanelScaleProfile.CardPortrait());
    /// document.panelSettings = driver.Settings;
    /// // every frame: driver.Tick();
    /// </code>
    /// </summary>
    public sealed class PanelScaleDriver : IDisposable
    {
        readonly PanelSettings _source;
        PanelScaleProfile _profile;
        float _userScale = 1f;
        PanelScaleEnvironment _lastEnvironment;
        bool _hasEvaluated;
        bool _disposed;

        /// <summary>The runtime copy of the PanelSettings. Assign it to the UIDocument. Destroyed by <see cref="Dispose"/>.</summary>
        public PanelSettings Settings { get; }

        /// <summary>Where the device facts come from. Defaults to <see cref="ReadDevice"/>; tests replace it.</summary>
        public Func<PanelScaleEnvironment> EnvironmentSource { get; set; } = ReadDevice;

        /// <summary>The scale that was applied last.</summary>
        public PanelScale Scale { get; private set; }

        /// <summary>Screen pixels per panel unit under the applied scale (what a game that lays out in pixels needs). Zero before the first evaluation.</summary>
        public float PixelsPerUnit { get; private set; }

        /// <summary>Raised after a new scale was applied.</summary>
        public event Action<PanelScale> Applied;

        /// <summary>Creates the driver and the runtime copy of <paramref name="source"/>, and evaluates once.</summary>
        public PanelScaleDriver(PanelSettings source, PanelScaleProfile profile)
        {
            if (source == null)
                throw new ArgumentNullException(nameof(source));
            _source = source;
            _profile = profile;
            Settings = UnityEngine.Object.Instantiate(source);
            Settings.name = source.name + " (runtime)";
            Settings.hideFlags = HideFlags.HideAndDontSave;
            Tick();
        }

        /// <summary>The profile in use. Setting it re-evaluates on the next <see cref="Tick"/>.</summary>
        public PanelScaleProfile Profile
        {
            get { return _profile; }
            set
            {
                _profile = value;
                _hasEvaluated = false;
            }
        }

        /// <summary>The player's UI size multiplier (1 = as designed, usually 0.8 to 1.6). Setting it re-evaluates on the next <see cref="Tick"/>.</summary>
        public float UserScale
        {
            get { return _userScale; }
            set
            {
                _userScale = value > 0.1f ? value : 0.1f;
                _hasEvaluated = false;
            }
        }

        /// <summary>The same source asset the driver copied. The asset is never modified.</summary>
        public PanelSettings Source
        {
            get { return _source; }
        }

        /// <summary>Reads the environment and applies a new scale when width, height, DPI, profile or user scale changed. Returns true when a scale was applied.</summary>
        public bool Tick()
        {
            if (_disposed)
                return false;
            PanelScaleEnvironment environment = EnvironmentSource();
            if (_hasEvaluated && SameEnvironment(environment, _lastEnvironment))
                return false;
            _lastEnvironment = environment;
            _hasEvaluated = true;

            PanelScaleInput input = new PanelScaleInput(environment.Handheld, environment.Device, environment.Dpi, environment.Width, environment.Height, _userScale);
            PanelScale scale = PanelScalePolicy.Compute(_profile, input);
            Scale = scale;
            if (scale.Physical)
            {
                Settings.scaleMode = PanelScaleMode.ConstantPhysicalSize;
                Settings.referenceDpi = scale.ReferenceDpi;
                Settings.fallbackDpi = scale.ReferenceDpi;
                PixelsPerUnit = scale.PixelsPerUnit;
            }
            else
            {
                Settings.scaleMode = PanelScaleMode.ConstantPixelSize;
                Settings.scale = scale.PixelScale;
                PixelsPerUnit = scale.PixelsPerUnit;
            }

            Action<PanelScale> handler = Applied;
            if (handler != null)
                handler(scale);
            return true;
        }

        /// <summary>Destroys the runtime copy. Assign the original settings back to the document first when it should outlive the driver.</summary>
        public void Dispose()
        {
            if (_disposed)
                return;
            _disposed = true;
            if (Settings != null)
            {
                if (Application.isPlaying)
                    UnityEngine.Object.Destroy(Settings);
                else
                    UnityEngine.Object.DestroyImmediate(Settings);
            }
        }

        /// <summary>Reads the device through <c>UnityEngine.Device</c>: handheld flag, device class, DPI and screen size.</summary>
        public static PanelScaleEnvironment ReadDevice()
        {
            int width = UnityEngine.Device.Screen.width;
            int height = UnityEngine.Device.Screen.height;
            float dpi = UnityEngine.Device.Screen.dpi;
            RuntimePlatform platform = UnityEngine.Device.Application.platform;
            bool handheld = platform == RuntimePlatform.IPhonePlayer || platform == RuntimePlatform.Android;
            return new PanelScaleEnvironment
            {
                Handheld = handheld,
                Device = DetectDevice(platform, handheld, dpi, width, height),
                Dpi = dpi,
                Width = width,
                Height = height
            };
        }

        /// <summary>
        /// The device class of a platform. Phones and tablets are told apart by the short side in density-independent points (160 per inch): 600 and more is a tablet,
        /// the same breakpoint as <see cref="UiSizeClass.Regular"/>.
        /// </summary>
        public static DeviceClass DetectDevice(RuntimePlatform platform, bool handheld, float dpi, int width, int height)
        {
            if (platform == RuntimePlatform.WebGLPlayer)
                return DeviceClass.Web;
            if (UiProfileWatcher.PlatformName(platform) == "console")
                return DeviceClass.Console;
            if (platform == RuntimePlatform.VisionOS)
                return DeviceClass.WorldSpace;
            if (!handheld)
                return DeviceClass.Desktop;
            float shortPixels = Math.Min(width, height);
            float shortPoints = dpi > 0f ? shortPixels / (dpi / 160f) : shortPixels;
            return shortPoints >= 600f ? DeviceClass.Tablet : DeviceClass.Phone;
        }

        static bool SameEnvironment(PanelScaleEnvironment a, PanelScaleEnvironment b)
        {
            return a.Width == b.Width && a.Height == b.Height && Mathf.Approximately(a.Dpi, b.Dpi) && a.Handheld == b.Handheld && a.Device == b.Device;
        }
    }
}
