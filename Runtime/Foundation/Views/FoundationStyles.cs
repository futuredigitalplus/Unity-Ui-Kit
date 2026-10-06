using UnityEngine;
using UnityEngine.UIElements;

namespace FutureDigitalPlus.UIKit.Foundation
{
    /// <summary>
    /// Installs the foundation's style sheets: <c>Foundation.Tokens.uss</c> (the <c>--fdp-foundation-*</c> values, declared on the <c>fdp-shell</c> class) and
    /// <c>Foundation.uss</c> (layers, screen transitions, dialog, toast, loading, top bar, list row, banner). Both read the design system's tokens with
    /// literal fallbacks, so the order against the design system sheets does not matter and a missing design system only costs the colours.
    /// </summary>
    public static class FoundationStyles
    {
        /// <summary>Resources path of the foundation tokens sheet.</summary>
        public const string TokensResource = "FutureDigitalPlus/UIKit/Foundation/Styles/Foundation.Tokens";

        /// <summary>Resources path of the foundation component sheet.</summary>
        public const string StylesResource = "FutureDigitalPlus/UIKit/Foundation/Styles/Foundation";

        /// <summary>Resources path of the design system master sheet (all <c>ds-*</c> components and the default tokens).</summary>
        public const string DesignSystemResource = "FutureDigitalPlus/UIKit/DesignSystem/Styles/DesignSystem";

        /// <summary>Adds the foundation sheets to <paramref name="root"/> when they are not there yet. Returns false when a sheet could not be loaded.</summary>
        public static bool Install(VisualElement root)
        {
            if (root == null)
                return false;
            bool ok = Add(root, TokensResource);
            return Add(root, StylesResource) && ok;
        }

        /// <summary>Removes the foundation sheets from <paramref name="root"/>.</summary>
        public static void Uninstall(VisualElement root)
        {
            if (root == null)
                return;
            Remove(root, TokensResource);
            Remove(root, StylesResource);
        }

        /// <summary>
        /// Adds the design system master sheet to <paramref name="holder"/> when it is not there yet: a convenience for a game that uses the foundation without
        /// the facade. Put it on the panel root (the parent of the UIDocument root) so a theme can win from a descendant; the facade's installer does that
        /// and also installs themes, so prefer it when you use the facade.
        /// </summary>
        public static bool InstallDesignSystem(VisualElement holder)
        {
            return holder != null && Add(holder, DesignSystemResource);
        }

        static bool Add(VisualElement element, string resource)
        {
            StyleSheet sheet = Resources.Load<StyleSheet>(resource);
            if (sheet == null)
            {
                Debug.LogWarning("FutureDigitalPlus.UIKit.Foundation: style sheet '" + resource + "' was not found in Resources.");
                return false;
            }
            if (!element.styleSheets.Contains(sheet))
                element.styleSheets.Add(sheet);
            return true;
        }

        static void Remove(VisualElement element, string resource)
        {
            StyleSheet sheet = Resources.Load<StyleSheet>(resource);
            if (sheet != null)
                element.styleSheets.Remove(sheet);
        }
    }
}
