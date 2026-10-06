using UnityEngine;
using UnityEngine.UIElements;

namespace FutureDigitalPlus.UIKit.Hud
{
    /// <summary>
    /// Installs the HUD stylesheet. Call <see cref="Install"/> once per panel (on the document root or any ancestor of
    /// the HUD elements) after the design system base styles. The sheet only uses design system tokens, each with a
    /// fallback value, so the elements stay legible even without a theme.
    /// </summary>
    public static class HudStyles
    {
        /// <summary>Resources path of the stylesheet (without extension).</summary>
        public const string ResourcePath = "FutureDigitalPlus/UIKit/Hud/Styles/Hud";

        /// <summary>Adds the HUD stylesheet to <paramref name="root"/> unless it is already there. Returns false when the root is null or the sheet is missing.</summary>
        public static bool Install(VisualElement root)
        {
            if (root == null) return false;
            var sheet = Resources.Load<StyleSheet>(ResourcePath);
            if (sheet == null)
            {
                Debug.LogWarning($"[UIKit.Hud] Stylesheet '{ResourcePath}' not found in Resources.");
                return false;
            }

            if (!root.styleSheets.Contains(sheet)) root.styleSheets.Add(sheet);
            return true;
        }
    }
}
