using UnityEngine;
using UnityEngine.UIElements;

namespace FutureDigitalPlus.UIKit.Controls
{
    /// <summary>
    /// Installs the stylesheet of the Controls module (tab bar, context menu, select field, bottom sheet, grid and list
    /// helpers, reward popup). Call <see cref="Install"/> once per panel after the design system base styles. Popups
    /// are added to the top element of the panel the control lives in, so the sheet must be installed on that element or
    /// above it (the document root or the panel root).
    /// </summary>
    public static class ControlsStyles
    {
        /// <summary>Resources path of the stylesheet (without extension).</summary>
        public const string ResourcePath = "FutureDigitalPlus/UIKit/Controls/Styles/Controls";

        /// <summary>Adds the stylesheet to <paramref name="root"/> unless it is already there. Returns false when the root is null or the sheet is missing.</summary>
        public static bool Install(VisualElement root)
        {
            if (root == null) return false;
            var sheet = Resources.Load<StyleSheet>(ResourcePath);
            if (sheet == null)
            {
                Debug.LogWarning($"[UIKit.Controls] Stylesheet '{ResourcePath}' not found in Resources.");
                return false;
            }

            if (!root.styleSheets.Contains(sheet)) root.styleSheets.Add(sheet);
            return true;
        }
    }
}
