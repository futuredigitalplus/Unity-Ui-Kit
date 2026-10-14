using UnityEngine.UIElements;

namespace FutureDigitalPlus.UIKit.Foundation
{
    /// <summary>
    /// The reading direction an element is in, for views that mirror themselves or their keys. UI Toolkit's public
    /// <c>languageDirection</c> is only what was set on that element (<c>Inherit</c> on almost every one); this walks up to the panel root
    /// with the rule of <see cref="ReadingDirection"/>: the nearest element that sets a direction decides, an element with <c>fdp-rtl</c>
    /// counts as right to left, nothing on the way means left to right.
    /// </summary>
    public static class UiDirection
    {
        /// <summary>True when <paramref name="element"/> is in a right-to-left context. False for null.</summary>
        public static bool IsRightToLeft(VisualElement element)
        {
            return element != null && ReadingDirection.IsRightToLeft(element, e => e.hierarchy.parent, e => OwnRightToLeft(e),
                e => e.ClassListContains(ReadingDirection.RtlClass));
        }

        private static bool? OwnRightToLeft(VisualElement element)
        {
            switch (element.languageDirection)
            {
                case LanguageDirection.RTL: return true;
                case LanguageDirection.LTR: return false;
                default: return null;
            }
        }
    }
}
