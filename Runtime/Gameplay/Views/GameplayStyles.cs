using UnityEngine;
using UnityEngine.UIElements;

namespace FutureDigitalPlus.UIKit.Gameplay
{
    /// <summary>
    /// Installs the stylesheet of the Gameplay module (item slots, slot grids, the drag ghost). Call <see cref="Install"/>
    /// once per panel after the design system base styles, on the document root or any ancestor of the grids. The ghost that
    /// follows the pointer during a drag is added to the top element of the panel, so install on that element or above.
    /// </summary>
    public static class GameplayStyles
    {
        /// <summary>Resources path of the stylesheet (without extension).</summary>
        public const string ResourcePath = "FutureDigitalPlus/UIKit/Gameplay/Styles/Gameplay";

        /// <summary>Adds the stylesheet to <paramref name="root"/> unless it is already there. Returns false when the root is null or the sheet is missing.</summary>
        public static bool Install(VisualElement root)
        {
            if (root == null) return false;
            var sheet = Resources.Load<StyleSheet>(ResourcePath);
            if (sheet == null)
            {
                Debug.LogWarning($"[UIKit.Gameplay] Stylesheet '{ResourcePath}' not found in Resources.");
                return false;
            }

            if (!root.styleSheets.Contains(sheet)) root.styleSheets.Add(sheet);
            return true;
        }
    }
}
