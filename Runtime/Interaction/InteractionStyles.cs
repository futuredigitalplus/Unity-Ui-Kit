using UnityEngine;
using UnityEngine.UIElements;

namespace FutureDigitalPlus.UIKit.Interaction
{
    /// <summary>
    /// Attaches the module's two stylesheets to an element: <c>Focus.uss</c> (the focus ring for keyboard and gamepad) and <c>A11y.uss</c> (reduced motion,
    /// text size steps, bold text). Both are written against the design system's tokens with fallbacks, and every rule is scoped by a root class
    /// (<c>fdp-input-nav</c>, <c>fdp-reduce-motion</c>, <c>fdp-text-scale-*</c>, <c>fdp-bold-text</c>), so a game that binds neither <see cref="InputProfile"/> nor
    /// <see cref="AccessibilityApplier"/> sees no change.
    /// Install on the document root, below the element that holds the design system base sheet, so these rules win ties.
    /// </summary>
    public static class InteractionStyles
    {
        /// <summary>Resources path of the focus ring sheet.</summary>
        public const string FocusResource = "FutureDigitalPlus/UIKit/Interaction/Styles/Focus";

        /// <summary>Resources path of the accessibility sheet.</summary>
        public const string AccessibilityResource = "FutureDigitalPlus/UIKit/Interaction/Styles/A11y";

        /// <summary>Adds both sheets to <paramref name="root"/> once. Returns false (after one warning) when a sheet is missing from Resources.</summary>
        public static bool Install(VisualElement root)
        {
            if (root == null) return false;
            var ok = Add(root, FocusResource);
            return Add(root, AccessibilityResource) && ok;
        }

        /// <summary>Removes the two sheets from <paramref name="root"/>.</summary>
        public static void Uninstall(VisualElement root)
        {
            if (root == null) return;
            Remove(root, FocusResource);
            Remove(root, AccessibilityResource);
        }

        private static bool Add(VisualElement root, string resource)
        {
            var sheet = Resources.Load<StyleSheet>(resource);
            if (sheet == null)
            {
                Debug.LogWarning($"[UIKit] Interaction stylesheet '{resource}' not found in Resources.");
                return false;
            }

            if (!root.styleSheets.Contains(sheet)) root.styleSheets.Add(sheet);
            return true;
        }

        private static void Remove(VisualElement root, string resource)
        {
            var sheet = Resources.Load<StyleSheet>(resource);
            if (sheet != null) root.styleSheets.Remove(sheet);
        }
    }
}
