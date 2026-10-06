using UnityEngine;
using UnityEngine.UIElements;

namespace FutureDigitalPlus.UIKit.Foundation
{
    /// <summary>Where a panel is drawn. The device safe area only makes sense for <see cref="Screen"/>.</summary>
    public enum UiPanelKind
    {
        /// <summary>The element is not attached to a runtime panel yet (or the panel is an editor panel).</summary>
        Detached = 0,

        /// <summary>A screen-space panel on the main display: the device safe area applies.</summary>
        Screen = 1,

        /// <summary>A screen-space panel on another display: the main display's safe area does not apply.</summary>
        OtherDisplay = 2,

        /// <summary>A panel rendered into a RenderTexture (a diegetic screen, a minimap): no safe area.</summary>
        RenderTexture = 3,

        /// <summary>A world-space panel (Unity 6000.5 and later): no safe area.</summary>
        WorldSpace = 4
    }

    /// <summary>Detects the <see cref="UiPanelKind"/> of an element.</summary>
    public static class UiPanelKinds
    {
        /// <summary>The kind of the panel that holds <paramref name="element"/>.</summary>
        public static UiPanelKind Detect(VisualElement element)
        {
            if (element == null || element.panel == null)
                return UiPanelKind.Detached;
            IRuntimePanel runtime = element.panel as IRuntimePanel;
            if (runtime == null)
                return UiPanelKind.Detached;
            PanelSettings settings = runtime.panelSettings;
            if (settings == null)
                return UiPanelKind.Screen;
#if UNITY_6000_5_OR_NEWER
            if (settings.renderMode == PanelRenderMode.WorldSpace)
                return UiPanelKind.WorldSpace;
#endif
            if (settings.targetTexture != null)
                return UiPanelKind.RenderTexture;
            return settings.targetDisplay != 0 ? UiPanelKind.OtherDisplay : UiPanelKind.Screen;
        }
    }
}
