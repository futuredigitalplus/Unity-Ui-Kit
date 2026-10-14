using System;
using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace FutureDigitalPlus.UIKit.Foundation.Responsive
{
    /// <summary>
    /// Shows the running game's panel as it looks on a catalog device, in Play mode: the panel of a <see cref="PanelScaleDriver"/> is rendered into a texture of the
    /// device's pixel size, the driver is fed the device's size and DPI (pixel exact, so the monitor's DPI plays no part), the shell's safe area gets the device's
    /// insets, and the <see cref="ResponsiveRootBinder"/> of the driver, if any, gets the posture. <see cref="Dispose"/> puts everything back.
    /// </summary>
    public sealed class ResponsivePreviewSession : IDisposable
    {
        readonly Func<PanelScaleEnvironment> _savedEnvironment;
        readonly RenderTexture _savedTarget;
        readonly bool _savedClear;
        readonly Color _savedClearColor;
        readonly bool _savedRunInBackground;
        readonly SafeAreaProvider _safeArea;
        readonly Func<Rect> _savedSafeArea;
        readonly Func<Vector2Int> _savedScreenSize;
        readonly SafeAreaMode _savedSafeMode;
        readonly ResponsiveRootBinder _binder;
        readonly DevicePosture _savedPosture;
        readonly HingeAxis _savedHinge;
        RenderTexture _texture;
        int _changedFrame;
        double _changedTime;
        bool _disposed;

        /// <summary>How long <see cref="IsSettled"/> waits at most, in seconds, for a panel that never reaches the device's size.</summary>
        public const double MaxSettleSeconds = 3.0;

        ResponsivePreviewSession(PanelScaleDriver driver, UIDocument document)
        {
            Driver = driver;
            Document = document;
            PanelSettings settings = driver.Settings;
            _savedEnvironment = driver.EnvironmentSource;
            _savedTarget = settings.targetTexture;
            _savedClear = settings.clearColor;
            _savedClearColor = settings.colorClearValue;
            _savedRunInBackground = Application.runInBackground;
            Application.runInBackground = true;

            _safeArea = FindSafeArea(document);
            _savedSafeArea = _safeArea.SafeAreaSource;
            _savedScreenSize = _safeArea.ScreenSizeSource;
            _savedSafeMode = _safeArea.Mode;

            foreach (ResponsiveRootBinder binder in ResponsiveRootBinder.Active)
            {
                if (binder.Driver == driver)
                {
                    _binder = binder;
                    break;
                }
            }
            if (_binder != null)
            {
                _savedPosture = _binder.Posture;
                _savedHinge = _binder.Hinge;
            }
        }

        /// <summary>The driver whose panel is previewed.</summary>
        public PanelScaleDriver Driver { get; }

        /// <summary>The document that shows the panel, or null when none uses the driver's settings.</summary>
        public UIDocument Document { get; }

        /// <summary>The image of the panel at the device's pixel size; null before the first <see cref="Show"/>.</summary>
        public RenderTexture Texture
        {
            get { return _texture; }
        }

        /// <summary>The screen shown last.</summary>
        public ResponsiveScreenSpec Screen { get; private set; }

        /// <summary>True when the screen is shown sideways.</summary>
        public bool Landscape { get; private set; }

        /// <summary>The posture shown (<see cref="DevicePosture.Flat"/> for a screen without a hinge).</summary>
        public DevicePosture Posture { get; private set; }

        /// <summary>The hinge direction as the screen is held (for an overlay), <see cref="HingeAxis.None"/> when it has none.</summary>
        public HingeAxis Hinge { get; private set; }

        /// <summary>True when the driver has a root binder, so the posture reaches the screens as classes.</summary>
        public bool HasBinder
        {
            get { return _binder != null; }
        }

        /// <summary>The panel's visual tree (for audits and scrolling), or null.</summary>
        public VisualElement PanelRoot
        {
            get { return Document != null && Document.rootVisualElement != null && Document.rootVisualElement.panel != null ? Document.rootVisualElement.panel.visualTree : null; }
        }

        /// <summary>True while the driver is alive and the session is not disposed.</summary>
        public bool IsAlive
        {
            get
            {
                if (_disposed || Driver.Settings == null)
                    return false;
                foreach (PanelScaleDriver active in PanelScaleDriver.Active)
                {
                    if (active == Driver)
                        return true;
                }
                return false;
            }
        }

        /// <summary>
        /// Starts a session on the first live driver of a running game. Null outside Play mode or when no driver exists (the game creates none, or it was not
        /// created yet); <paramref name="reason"/> then says why.
        /// </summary>
        public static ResponsivePreviewSession Start(out string reason)
        {
            reason = null;
            if (!Application.isPlaying)
            {
                reason = "Enter Play mode to preview the game's screens.";
                return null;
            }
            if (PanelScaleDriver.Active.Count == 0)
            {
                reason = "No PanelScaleDriver is running. The game creates one for its panel (see the Foundation documentation).";
                return null;
            }
            PanelScaleDriver driver = PanelScaleDriver.Active[0];
            UIDocument document = null;
            foreach (UIDocument candidate in UnityEngine.Object.FindObjectsByType<UIDocument>(FindObjectsSortMode.None))
            {
                if (candidate.panelSettings == driver.Settings)
                {
                    document = candidate;
                    break;
                }
            }
            return new ResponsivePreviewSession(driver, document);
        }

        /// <summary>Shows <paramref name="screen"/> held in <paramref name="landscape"/> and <paramref name="posture"/>, on <paramref name="background"/>.</summary>
        public void Show(ResponsiveScreenSpec screen, bool landscape, DevicePosture posture, Color background)
        {
            if (screen == null)
                throw new ArgumentNullException(nameof(screen));
            if (_disposed)
                return;
            screen.PixelSize(landscape, out int width, out int height);
            EnsureTexture(width, height);

            var environment = new PanelScaleEnvironment
            {
                Handheld = true,
                Device = screen.IsTablet ? DeviceClass.Tablet : DeviceClass.Phone,
                Dpi = screen.dpi,
                Width = width,
                Height = height,
                PixelExact = true,
            };
            Driver.EnvironmentSource = () => environment;
            PanelSettings settings = Driver.Settings;
            settings.clearColor = true;
            settings.colorClearValue = background;
            if (settings.targetTexture != _texture)
                settings.targetTexture = _texture;

            SafeAreaInsets insets = screen.Insets(landscape);
            var safe = new Rect(insets.Left, insets.Bottom, Mathf.Max(0f, width - insets.Left - insets.Right), Mathf.Max(0f, height - insets.Top - insets.Bottom));
            var size = new Vector2Int(width, height);
            _safeArea.SafeAreaSource = () => safe;
            _safeArea.ScreenSizeSource = () => size;
            _safeArea.Mode = SafeAreaMode.Always;
            _safeArea.Invalidate();

            Hinge = DeviceFormFactors.HingeAs(screen.hinge, landscape);
            Posture = posture == DevicePosture.HalfOpened && Hinge != HingeAxis.None ? DevicePosture.HalfOpened : DevicePosture.Flat;
            if (_binder != null)
            {
                _binder.Hinge = Posture == DevicePosture.HalfOpened ? Hinge : HingeAxis.None;
                _binder.Posture = Posture;
            }

            Screen = screen;
            Landscape = landscape;
            MarkChanged();
            Driver.Tick();
            if (_binder != null)
                _binder.Refresh();
            EditorApplication.QueuePlayerLoopUpdate();
        }

        /// <summary>Starts the wait of <see cref="IsSettled"/> again (call it after the game was told to open another screen).</summary>
        public void MarkChanged()
        {
            _changedFrame = Time.frameCount;
            _changedTime = EditorApplication.timeSinceStartup;
        }

        /// <summary>
        /// True when the panel shows the last change: at least two game frames and <paramref name="minSeconds"/> passed since it (screen transitions need time), and the
        /// panel's layout has the device's size in units. Also true after <see cref="MaxSettleSeconds"/>, so a caller never waits forever. Editor updates run faster
        /// than game frames, so counting them is not enough.
        /// </summary>
        public bool IsSettled(double minSeconds)
        {
            double elapsed = EditorApplication.timeSinceStartup - _changedTime;
            if (elapsed >= MaxSettleSeconds)
                return true;
            if (Time.frameCount < _changedFrame + 2 || elapsed < minSeconds)
                return false;
            VisualElement root = PanelRoot;
            float pixels = Driver.PixelsPerUnit;
            if (root == null || _texture == null || !(pixels > 0f))
                return true;
            return Mathf.Abs(root.layout.width - _texture.width / pixels) < 1f && Mathf.Abs(root.layout.height - _texture.height / pixels) < 1f;
        }

        /// <summary>Writes the current image as a PNG to <paramref name="path"/> (folders are created).</summary>
        public void SavePng(string path)
        {
            if (_texture == null)
                return;
            string folder = Path.GetDirectoryName(path);
            if (!string.IsNullOrEmpty(folder))
                Directory.CreateDirectory(folder);
            RenderTexture previous = RenderTexture.active;
            var image = new Texture2D(_texture.width, _texture.height, TextureFormat.RGBA32, false);
            try
            {
                RenderTexture.active = _texture;
                image.ReadPixels(new Rect(0, 0, _texture.width, _texture.height), 0, 0);
                image.Apply();
                File.WriteAllBytes(path, image.EncodeToPNG());
            }
            finally
            {
                RenderTexture.active = previous;
                UnityEngine.Object.DestroyImmediate(image);
            }
        }

        /// <summary>Puts the driver, the panel, the safe area and the binder back as they were, and frees the texture.</summary>
        public void Dispose()
        {
            if (_disposed)
                return;
            _disposed = true;
            Application.runInBackground = _savedRunInBackground;
            if (Driver.Settings != null)
            {
                Driver.Settings.targetTexture = _savedTarget;
                Driver.Settings.clearColor = _savedClear;
                Driver.Settings.colorClearValue = _savedClearColor;
            }
            Driver.EnvironmentSource = _savedEnvironment;
            _safeArea.SafeAreaSource = _savedSafeArea;
            _safeArea.ScreenSizeSource = _savedScreenSize;
            _safeArea.Mode = _savedSafeMode;
            _safeArea.Invalidate();
            if (_binder != null)
            {
                _binder.Hinge = _savedHinge;
                _binder.Posture = _savedPosture;
            }
            if (Driver.Settings != null)
                Driver.Tick();
            if (_texture != null)
            {
                _texture.Release();
                UnityEngine.Object.DestroyImmediate(_texture);
                _texture = null;
            }
            if (Application.isPlaying)
                EditorApplication.QueuePlayerLoopUpdate();
        }

        void EnsureTexture(int width, int height)
        {
            if (_texture != null && _texture.width == width && _texture.height == height)
                return;
            RenderTexture old = _texture;
            _texture = new RenderTexture(width, height, 24, RenderTextureFormat.ARGB32)
            {
                name = "Responsive preview " + width + "x" + height,
                hideFlags = HideFlags.HideAndDontSave,
            };
            _texture.Create();
            if (Driver.Settings != null)
                Driver.Settings.targetTexture = _texture;
            if (old != null)
            {
                old.Release();
                UnityEngine.Object.DestroyImmediate(old);
            }
        }

        // the shell attaches its provider to its root, below the document's root
        static SafeAreaProvider FindSafeArea(UIDocument document)
        {
            if (document == null || document.rootVisualElement == null)
                return SafeAreaProvider.Default;
            SafeAreaProvider found = null;
            document.rootVisualElement.Query<VisualElement>().ForEach(element =>
            {
                if (found != null)
                    return;
                SafeAreaProvider provider = SafeAreaProvider.For(element);
                if (provider != SafeAreaProvider.Default)
                    found = provider;
            });
            return found ?? SafeAreaProvider.Default;
        }
    }
}
