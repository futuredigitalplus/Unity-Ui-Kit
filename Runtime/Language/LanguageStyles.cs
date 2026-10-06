using UnityEngine;
using UnityEngine.UIElements;

namespace FutureDigitalPlus.UIKit.Language
{
    /// <summary>Loads and attaches the stylesheets of the Language module.</summary>
    public static class LanguageStyles
    {
        /// <summary>The Resources path of the right-to-left stylesheet.</summary>
        public const string RtlResource = "FutureDigitalPlus/UIKit/Language/Styles/Rtl";

        /// <summary>
        /// Adds <c>Rtl.uss</c> to <paramref name="panelRoot"/> (idempotent). Do it after the design-system base sheet, so a tie with
        /// a component rule is won by order. The rules are inert until <see cref="LocaleClassApplier"/> puts <c>fdp-rtl</c> on a root.
        /// </summary>
        /// <param name="panelRoot">The element that holds the design-system base sheet (the panel root).</param>
        /// <returns>False when the sheet could not be loaded or the root is null.</returns>
        public static bool Install(VisualElement panelRoot)
        {
            if (panelRoot == null) return false;

            var sheet = Resources.Load<StyleSheet>(RtlResource);
            if (sheet == null)
            {
                Debug.LogWarning("[LanguageStyles] Resources/" + RtlResource + " was not found; right-to-left layouts will not be mirrored.");
                return false;
            }

            if (!panelRoot.styleSheets.Contains(sheet)) panelRoot.styleSheets.Add(sheet);
            return true;
        }
    }
}
