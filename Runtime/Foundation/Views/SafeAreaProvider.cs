using System;
using System.Runtime.CompilerServices;
using UnityEngine;
using UnityEngine.UIElements;

namespace FutureDigitalPlus.UIKit.Foundation
{
    /// <summary>
    /// Current safe-area insets in panel units for the panels it is attached to, shared by every <see cref="SafeAreaPanel"/> below them.
    /// UI Toolkit does not apply the device safe area to a panel, so a host calls <see cref="Poll"/> (the <see cref="UiShell"/> does, every frame and on
    /// geometry changes): one comparison of a few numbers, and the arithmetic only when one of them changed. Rotation, window resizes, a home indicator and a
    /// notch all arrive through those numbers, so there is no per-device table.
    /// The provider reports zero insets for panels that are not the screen (render textures, world space, other displays) unless <see cref="Mode"/> says otherwise,
    /// and applies a minimum margin on televisions and consoles (<see cref="MinimumMarginPercent"/>).
    /// One instance per shell; <see cref="Default"/> serves panels that have no shell. Main thread only.
    /// </summary>
    public sealed class SafeAreaProvider
    {
        /// <summary>Margin used on consoles and tvOS when <see cref="MinimumMarginPercent"/> is negative: 5 percent of the panel size on every edge.</summary>
        public const float TvMarginPercent = 5f;

        static readonly ConditionalWeakTable<VisualElement, SafeAreaProvider> Owners = new ConditionalWeakTable<VisualElement, SafeAreaProvider>();

        Rect _lastSafe;
        Vector2Int _lastScreen;
        Vector2 _lastPanel;
        UiPanelKind _lastKind;
        float _lastMargin = -1f;
        bool _hasValue;

        /// <summary>The provider of panels that are not under a shell.</summary>
        public static SafeAreaProvider Default { get; } = new SafeAreaProvider();

        /// <summary>Where the device safe area comes from, in screen pixels with the origin at the bottom left. Defaults to the device screen; tests replace it.</summary>
        public Func<Rect> SafeAreaSource { get; set; } = () => UnityEngine.Device.Screen.safeArea;

        /// <summary>Where the screen size in pixels comes from. Defaults to the device screen.</summary>
        public Func<Vector2Int> ScreenSizeSource { get; set; } = () => new Vector2Int(UnityEngine.Device.Screen.width, UnityEngine.Device.Screen.height);

        /// <summary>Which panels get insets. Changing it takes effect on the next <see cref="Poll"/>.</summary>
        public SafeAreaMode Mode { get; set; } = SafeAreaMode.Auto;

        /// <summary>
        /// Minimum margin to every screen edge as a percent of the panel size, merged with the device insets (the larger wins). Negative (the default) means
        /// <see cref="TvMarginPercent"/> on consoles and tvOS and zero elsewhere; zero or more is used as given.
        /// </summary>
        public float MinimumMarginPercent { get; set; } = -1f;

        /// <summary>Which kind of panel the last <see cref="Poll"/> saw.</summary>
        public UiPanelKind PanelKind { get; private set; }

        /// <summary>Insets in panel units.</summary>
        public SafeAreaInsets Insets { get; private set; } = SafeAreaInsets.Zero;

        /// <summary>Device insets in screen pixels (what a game layout that works in pixels needs). Zero when the panel is not a screen panel.</summary>
        public SafeAreaInsets PixelInsets { get; private set; } = SafeAreaInsets.Zero;

        /// <summary>Raised with the new panel-unit insets after they changed.</summary>
        public event Action<SafeAreaInsets> Changed;

        /// <summary>The provider that serves <paramref name="element"/>: the nearest ancestor registered with <see cref="Attach"/>, or <see cref="Default"/>.</summary>
        public static SafeAreaProvider For(VisualElement element)
        {
            for (VisualElement current = element; current != null; current = current.hierarchy.parent)
            {
                SafeAreaProvider provider;
                if (Owners.TryGetValue(current, out provider))
                    return provider;
            }
            return Default;
        }

        /// <summary>Makes this provider the one that serves every <see cref="SafeAreaPanel"/> under <paramref name="root"/>.</summary>
        public void Attach(VisualElement root)
        {
            if (root == null)
                return;
            Owners.Remove(root);
            Owners.Add(root, this);
        }

        /// <summary>Stops serving the elements under <paramref name="root"/> (they fall back to <see cref="Default"/>).</summary>
        public void Detach(VisualElement root)
        {
            if (root != null)
                Owners.Remove(root);
        }

        /// <summary>
        /// Recomputes when the screen, the safe area, the panel size, the panel kind or the margin changed. <paramref name="panelRoot"/> is the element that
        /// covers the panel (the shell root). Returns true when the insets changed.
        /// </summary>
        public bool Poll(VisualElement panelRoot)
        {
            if (panelRoot == null)
                return false;
            float panelWidth = panelRoot.resolvedStyle.width;
            float panelHeight = panelRoot.resolvedStyle.height;
            if (!(panelWidth > 0f) || !(panelHeight > 0f))
                return false; // layout has not run yet; the next call asks again

            UiPanelKind kind = UiPanelKinds.Detect(panelRoot);
            Rect safe = SafeAreaSource();
            Vector2Int screen = ScreenSizeSource();
            float margin = EffectiveMarginPercent();
            Vector2 panel = new Vector2(panelWidth, panelHeight);
            if (_hasValue && kind == _lastKind && screen == _lastScreen && safe == _lastSafe && panel == _lastPanel && Mathf.Approximately(margin, _lastMargin))
                return false;
            _lastKind = kind;
            _lastScreen = screen;
            _lastSafe = safe;
            _lastPanel = panel;
            _lastMargin = margin;
            _hasValue = true;
            PanelKind = kind;

            bool applies = Applies(kind);
            SafeAreaInsets pixels = applies
                ? SafeAreaMath.FromScreen(screen.x, screen.y, safe.x, safe.y, safe.width, safe.height)
                : SafeAreaInsets.Zero;
            // Units per pixel is the same on both axes in every scale mode the foundation uses.
            float unitsPerPixel = screen.x > 0 ? panelWidth / screen.x : 0f;
            SafeAreaInsets units = SafeAreaMath.ToPanel(pixels, unitsPerPixel);
            if (applies && margin > 0f)
                units = units.WithMinimum(margin, panelWidth, panelHeight);
            return Apply(pixels, units);
        }

        /// <summary>Forces the next <see cref="Poll"/> to recompute (after the app regains focus from a system sheet, for example).</summary>
        public void Invalidate()
        {
            _hasValue = false;
        }

        /// <summary>Back to reading the device with the default mode and margin. Called when Unity enters play mode without a domain reload.</summary>
        internal void ResetToDeviceDefaults()
        {
            SafeAreaSource = () => UnityEngine.Device.Screen.safeArea;
            ScreenSizeSource = () => new Vector2Int(UnityEngine.Device.Screen.width, UnityEngine.Device.Screen.height);
            Mode = SafeAreaMode.Auto;
            MinimumMarginPercent = -1f;
            Insets = SafeAreaInsets.Zero;
            PixelInsets = SafeAreaInsets.Zero;
            Changed = null;
            Invalidate();
        }

        bool Applies(UiPanelKind kind)
        {
            switch (Mode)
            {
                case SafeAreaMode.Never:
                    return false;
                case SafeAreaMode.Always:
                    return true;
                default:
                    return kind == UiPanelKind.Screen;
            }
        }

        float EffectiveMarginPercent()
        {
            if (MinimumMarginPercent >= 0f)
                return MinimumMarginPercent;
            return IsTelevisionPlatform() ? TvMarginPercent : 0f;
        }

        static bool IsTelevisionPlatform()
        {
            return UnityEngine.Device.Application.isConsolePlatform || UnityEngine.Device.Application.platform == RuntimePlatform.tvOS;
        }

        bool Apply(SafeAreaInsets pixels, SafeAreaInsets units)
        {
            bool changed = !units.Equals(Insets) || !pixels.Equals(PixelInsets);
            PixelInsets = pixels;
            Insets = units;
            if (!changed)
                return false;
            Action<SafeAreaInsets> handler = Changed;
            if (handler != null)
                handler(units);
            return true;
        }
    }
}
