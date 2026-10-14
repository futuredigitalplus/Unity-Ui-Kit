using System;

namespace FutureDigitalPlus.UIKit.Foundation
{
    /// <summary>
    /// Decides whether an element reads right to left. Pure: the caller walks its own tree through the delegates.
    /// <para>
    /// UI Toolkit's public <c>VisualElement.languageDirection</c> returns only the value set on that element (<c>Inherit</c> on almost every
    /// element), not the direction it inherits, so reading it on a control below the root says nothing. The kit walks from the element up
    /// to the panel root instead: the nearest element that sets a direction (left to right or right to left) decides; an element that
    /// carries <see cref="RtlClass"/> (the class <c>LocaleClassApplier</c> puts on the root next to the direction) counts as right to left;
    /// with neither on the way, the element reads left to right.
    /// </para>
    /// </summary>
    public static class ReadingDirection
    {
        /// <summary>The class on a root while the language is written right to left: <c>fdp-rtl</c>. Stylesheets mirror their rows under it.</summary>
        public const string RtlClass = "fdp-rtl";

        /// <summary>
        /// The answer one element gives on its own: its direction when it sets one (<paramref name="ownRightToLeft"/> true or false),
        /// otherwise right to left when it carries <see cref="RtlClass"/>, otherwise null (its parent decides).
        /// </summary>
        /// <param name="ownRightToLeft">True when the element sets right to left, false when it sets left to right, null when it inherits.</param>
        /// <param name="hasRtlClass">True when the element carries <see cref="RtlClass"/>.</param>
        public static bool? OfElement(bool? ownRightToLeft, bool hasRtlClass)
        {
            if (ownRightToLeft.HasValue)
                return ownRightToLeft.Value;
            return hasRtlClass ? true : (bool?)null;
        }

        /// <summary>
        /// Walks from <paramref name="element"/> up through <paramref name="parent"/> and returns the first answer of <see cref="OfElement"/>;
        /// false (left to right) when no element on the way decides or <paramref name="element"/> is null.
        /// </summary>
        /// <typeparam name="T">The node type (a <c>VisualElement</c> in the views, anything in tests).</typeparam>
        /// <param name="element">Where the walk starts (the element itself counts).</param>
        /// <param name="parent">The parent of a node, null at the top.</param>
        /// <param name="ownRightToLeft">The direction a node sets: true, false, or null when it inherits.</param>
        /// <param name="hasRtlClass">True when a node carries <see cref="RtlClass"/>.</param>
        public static bool IsRightToLeft<T>(T element, Func<T, T> parent, Func<T, bool?> ownRightToLeft, Func<T, bool> hasRtlClass) where T : class
        {
            if (parent == null) throw new ArgumentNullException(nameof(parent));
            if (ownRightToLeft == null) throw new ArgumentNullException(nameof(ownRightToLeft));
            if (hasRtlClass == null) throw new ArgumentNullException(nameof(hasRtlClass));

            for (var node = element; node != null; node = parent(node))
            {
                var answer = OfElement(ownRightToLeft(node), hasRtlClass(node));
                if (answer.HasValue)
                    return answer.Value;
            }

            return false;
        }
    }
}
