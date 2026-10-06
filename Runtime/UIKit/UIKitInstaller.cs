using System;
using System.Runtime.CompilerServices;
using FutureDigitalPlus.UIKit.DesignSystem.Behaviour;
using FutureDigitalPlus.UIKit.DesignSystem.Theme.Applier;
using FutureDigitalPlus.UIKit.DesignSystem.Theme.Data;
using UnityEngine;
using UnityEngine.UIElements;

namespace FutureDigitalPlus.UIKit
{
    /// <summary>
    /// The one entry point of the facade. <see cref="Install"/> puts the design system base styles (on the panel root),
    /// a theme (on the document root), the <c>fdp-input-touch</c> class and the device safe area on a UIDocument root.
    /// <code>
    /// var kit = UIKitInstaller.Install(uiDocument.rootVisualElement, new UIKitOptions { Theme = myTheme });
    /// kit.SetTheme(otherTheme);   // swap later
    /// kit.Dispose();              // take everything off again
    /// </code>
    /// </summary>
    public static class UIKitInstaller
    {
        /// <summary>Class the design system's Mobile.uss keys on.</summary>
        public const string MobileClass = "fdp-input-touch";

        /// <summary>
        /// Installs the kit on <paramref name="root"/>. Returns null when root is null. A root has at most one installation: installing again
        /// disposes the previous one first, so two installations never fight over the theme, the padding and the base sheet.
        /// </summary>
        public static UIKitInstallation Install(VisualElement root, UIKitOptions options = null)
        {
            if (root == null) return null;
            UIKitInstallation.Active(root)?.Dispose();
            var installation = new UIKitInstallation(root, options ?? new UIKitOptions());
            installation.Refresh();
            return installation;
        }

        /// <summary>
        /// Creates a runtime <see cref="PanelSettings"/> that scales with the screen. Pass the Unity runtime
        /// theme (<c>.tss</c>) you want (Unity's default one imports <c>unity-theme://default</c>). With none Unity logs
        /// "No Theme Style Sheet set to PanelSettings", the document root gets no height and text no font.
        /// The caller owns the returned object (destroy it when done).
        /// </summary>
        public static PanelSettings CreatePanelSettings(ThemeStyleSheet runtimeTheme = null,
            Vector2Int? referenceResolution = null, float match = 0.5f)
        {
            var settings = ScriptableObject.CreateInstance<PanelSettings>();
            settings.name = "UIKitPanelSettings";
            settings.themeStyleSheet = runtimeTheme;
            settings.scaleMode = PanelScaleMode.ScaleWithScreenSize;
            settings.referenceResolution = referenceResolution ?? new Vector2Int(1080, 1920);
            settings.screenMatchMode = PanelScreenMatchMode.MatchWidthOrHeight;
            settings.match = Mathf.Clamp01(match);
            return settings;
        }
    }

    /// <summary>Handle of one <see cref="UIKitInstaller.Install"/>: change the theme, refresh, or take it all off.</summary>
    public sealed class UIKitInstallation : IDisposable
    {
        // How many installations rely on the base sheet on a given holder element (several UIDocuments can share one panel).
        private static readonly ConditionalWeakTable<VisualElement, StrongBox<int>> BaseSheetUsers = new();

        // The live installation of a root: there is at most one.
        private static readonly ConditionalWeakTable<VisualElement, UIKitInstallation> Installations = new();

        private StyleSheet _baseSheet;
        private VisualElement _baseHolder;
        private const long BehaviourScanMilliseconds = 250;

        private IVisualElementScheduledItem _poll;
        private IVisualElementScheduledItem _behaviourPoll;
        private Vector2 _lastScreen;
        private Rect _lastSafe;
        private bool _disposed;
        // Inline padding the host had set before we took over the safe-area padding; restored on Dispose.
        private StyleLength _prevPaddingLeft, _prevPaddingTop, _prevPaddingRight, _prevPaddingBottom;

        /// <summary>The element the kit was installed on (the document root).</summary>
        public VisualElement Root { get; }

        /// <summary>The options this installation was made with.</summary>
        public UIKitOptions Options { get; }

        /// <summary>
        /// Where the safe area comes from. The default is the device's <c>Screen.safeArea</c> for a screen-space panel on the main display and the whole screen (no padding)
        /// for a panel that renders into a <c>RenderTexture</c>, a world-space panel or a panel on another display; a source you assign yourself is applied to every panel.
        /// </summary>
        public Func<Rect> SafeAreaSource { get; set; }

        /// <summary>Where the screen size (pixels) comes from. Defaults to <c>Screen.width/height</c>.</summary>
        public Func<Vector2> ScreenSizeSource { get; set; } = () => new Vector2(Screen.width, Screen.height);

        /// <summary>Insets currently applied to the root, in panel units.</summary>
        public SafeAreaInsets AppliedInsets { get; private set; } = SafeAreaInsets.None;

        /// <summary>True while the root carries the touch-size class (<c>fdp-input-touch</c>).</summary>
        public bool IsMobile => Root.ClassListContains(UIKitInstaller.MobileClass);

        // The device's safe area; PlayMode tests replace it to fake a notch.
        internal static Func<Rect> DeviceSafeArea = () => Screen.safeArea;

        internal static UIKitInstallation Active(VisualElement root) =>
            Installations.TryGetValue(root, out var installation) ? installation : null;

        internal UIKitInstallation(VisualElement root, UIKitOptions options)
        {
            Root = root;
            Options = options;
            SafeAreaSource = ScreenSafeArea;
            Installations.Remove(root);
            Installations.Add(root, this);

            if (options.ApplySafeArea)
            {
                _prevPaddingLeft = root.style.paddingLeft;
                _prevPaddingTop = root.style.paddingTop;
                _prevPaddingRight = root.style.paddingRight;
                _prevPaddingBottom = root.style.paddingBottom;
            }

            if (options.InstallBaseStyles)
            {
                _baseSheet = Resources.Load<StyleSheet>(UIKitOptions.BaseStylesResource);
                if (_baseSheet == null)
                    Debug.LogWarning($"[UIKit] Base stylesheet '{UIKitOptions.BaseStylesResource}' not found in Resources.");
                else
                    PlaceBaseStyles();
            }

            root.RegisterCallback<AttachToPanelEvent>(OnAttachToPanel);
            root.RegisterCallback<GeometryChangedEvent>(OnGeometryChanged);

            // The screen can change without the panel noticing (rotation with the same panel size,
            // fold/unfold, split screen). A cheap poll catches the safe area moving.
            if (options.ApplySafeArea)
                _poll = root.schedule.Execute(RefreshSafeArea).Every(500);

            // Screens are built after the install (a shell clones them on first use), so a single pass would leave their toggles without a knob, their tabs
            // and drop-downs without behaviour. The design system's own host re-scans every 250 ms for the same reason; each helper does nothing once its work is done.
            if (options.EnsureBehaviours)
                _behaviourPoll = root.schedule.Execute(() => DesignSystemBehaviourBase<UIDocument>.EnsureAll(Root)).Every(BehaviourScanMilliseconds);
        }

        /// <summary>
        /// Puts the base stylesheet on the panel root (the parent of a UIDocument root) and the theme on the document
        /// root. The base sheet declares its tokens under <c>:root</c>; on Unity 6000.3 that outranks a class-scoped
        /// theme (<c>.theme-light</c>) that sits on the SAME element whatever the sheet order, so the theme only wins
        /// from a descendant of the element holding the base sheet. A root that is not attached yet has no parent: the
        /// sheet goes on the root for now and moves to the panel root on attach.
        /// </summary>
        private void PlaceBaseStyles()
        {
            if (_baseSheet == null || _disposed) return;

            var holder = Root.parent ?? Root;
            if (_baseHolder == holder) return;

            ReleaseBaseStyles();

            if (holder.styleSheets.Contains(_baseSheet) && !BaseSheetUsers.TryGetValue(holder, out _))
                return; // the host put it there itself (for example from its UXML): not ours to move or remove

            var users = BaseSheetUsers.GetOrCreateValue(holder);
            if (users.Value++ == 0 && !holder.styleSheets.Contains(_baseSheet))
                holder.styleSheets.Add(_baseSheet);
            _baseHolder = holder;
        }

        private void ReleaseBaseStyles()
        {
            if (_baseHolder == null) return;
            if (BaseSheetUsers.TryGetValue(_baseHolder, out var users) && --users.Value <= 0)
            {
                _baseHolder.styleSheets.Remove(_baseSheet);
                BaseSheetUsers.Remove(_baseHolder);
            }

            _baseHolder = null;
        }

        private void OnAttachToPanel(AttachToPanelEvent evt) => PlaceBaseStyles();

        /// <summary>Swaps the theme (null returns to the base tokens).</summary>
        public void SetTheme(ThemeData theme)
        {
            Options.Theme = theme;
            if (!_disposed) ApplyTheme();
        }

        /// <summary>Re-applies theme, mobile class and safe area from the current options and screen.</summary>
        public void Refresh()
        {
            if (_disposed) return;
            ApplyTheme();
            RefreshMobile();
            RefreshSafeArea();
            if (Options.EnsureBehaviours)
                DesignSystemBehaviourBase<UIDocument>.EnsureAll(Root);
        }

        private void ApplyTheme() => ThemeRuntime.Apply(Root, Options.Theme);

        private void OnGeometryChanged(GeometryChangedEvent evt)
        {
            RefreshMobile();
            RefreshSafeArea();
        }

        private void RefreshMobile()
        {
            if (_disposed || ShellPresent()) return; // with a shell the input profile owns the class (it follows the device, not the width)
            bool on;
            switch (Options.Mobile)
            {
                case MobileMode.On: on = true; break;
                case MobileMode.Off: on = false; break;
                default:
                    var width = Root.layout.width;
                    // Before the first layout the width is NaN: decide by platform only.
                    var narrow = !float.IsNaN(width) && width > 0f && width < Options.MobileBreakpoint;
                    on = Application.isMobilePlatform || narrow;
                    break;
            }

            Root.EnableInClassList(UIKitInstaller.MobileClass, on);
        }

        private void RefreshSafeArea()
        {
            if (_disposed || !Options.ApplySafeArea || ShellPresent()) return;

            var screen = ScreenSizeSource();
            var safe = SafeAreaSource();
            // The panel's own size, not the root's: a UIDocument root that has no content may be 0 high.
            var sizeSource = Root.panel != null ? Root.panel.visualTree : Root;
            var panel = new Vector2(sizeSource.layout.width, sizeSource.layout.height);
            var insets = SafeAreaInsets.FromScreen(safe, screen, panel);
            if (SameInsets(insets, AppliedInsets) && screen == _lastScreen && safe == _lastSafe) return;

            _lastScreen = screen;
            _lastSafe = safe;
            AppliedInsets = insets;
            Root.style.paddingLeft = insets.Left;
            Root.style.paddingTop = insets.Top;
            Root.style.paddingRight = insets.Right;
            Root.style.paddingBottom = insets.Bottom;
        }

        // A UiShell (Foundation module) pads its own top bars, dialogs and toasts by the safe area, wants its scrims to reach the screen edges and lets the input
        // profile write fdp-input-touch by device. Padding the document root as well would apply the notch twice and leave a gap round every scrim, and two writers
        // of one class would fight, so once the shell's root class shows up inside this root the facade gives its padding back and stops writing the class.
        // The two modules share nothing but this class name.
        private const string ShellRootClass = "fdp-shell";

        private bool _shellPads;

        private bool ShellPresent()
        {
            if (_shellPads) return true;
            if (Root.Q(className: ShellRootClass) == null) return false;
            _shellPads = true;
            if (Options.ApplySafeArea)
            {
                RestorePadding();
                AppliedInsets = SafeAreaInsets.None;
            }

            return true;
        }

        // The device safe area belongs to a screen-space panel on the main display; an in-game monitor (RenderTexture), a world-space panel or a second display
        // has no notch.
        private Rect ScreenSafeArea()
        {
            var screen = ScreenSizeSource();
            var full = new Rect(0f, 0f, screen.x, screen.y);
            if (Root.panel is IRuntimePanel runtime && runtime.panelSettings != null)
            {
                var settings = runtime.panelSettings;
#if UNITY_6000_5_OR_NEWER
                if (settings.renderMode == PanelRenderMode.WorldSpace) return full;
#endif
                if (settings.targetTexture != null || settings.targetDisplay != 0) return full;
            }

            return DeviceSafeArea();
        }

        private void RestorePadding()
        {
            Root.style.paddingLeft = _prevPaddingLeft;
            Root.style.paddingTop = _prevPaddingTop;
            Root.style.paddingRight = _prevPaddingRight;
            Root.style.paddingBottom = _prevPaddingBottom;
        }

        // Within the 0.05 that ToString("0.#") used to compare, without allocating two strings on every poll.
        private static bool SameInsets(SafeAreaInsets a, SafeAreaInsets b) =>
            Mathf.Abs(a.Left - b.Left) < 0.05f && Mathf.Abs(a.Top - b.Top) < 0.05f &&
            Mathf.Abs(a.Right - b.Right) < 0.05f && Mathf.Abs(a.Bottom - b.Bottom) < 0.05f;

        /// <summary>Takes the theme, mobile class, safe-area padding and (if we added it) the base styles off again.</summary>
        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;

            if (Installations.TryGetValue(Root, out var active) && active == this) Installations.Remove(Root);
            _poll?.Pause();
            _behaviourPoll?.Pause();
            Root.UnregisterCallback<GeometryChangedEvent>(OnGeometryChanged);
            Root.UnregisterCallback<AttachToPanelEvent>(OnAttachToPanel);
            ThemeRuntime.Clear(Root);
            if (!_shellPads) Root.RemoveFromClassList(UIKitInstaller.MobileClass); // with a shell the input profile owns it
            ReleaseBaseStyles();

            if (Options.ApplySafeArea)
                RestorePadding();

            AppliedInsets = SafeAreaInsets.None;
        }
    }
}
