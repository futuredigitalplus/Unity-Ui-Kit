using System;
using FutureDigitalPlus.UIKit.Foundation;
using UnityEngine.UIElements;

namespace FutureDigitalPlus.UIKit.Language
{
    /// <summary>Extension methods that bind a <see cref="UiText"/> to the text of an element (see <see cref="LocalizedTextBinder"/>).</summary>
    public static class LocalizedTextExtensions
    {
        /// <summary>Binds a <c>Label</c>, <c>Button</c> or any other <c>TextElement</c> to a localized text.</summary>
        /// <param name="element">The element whose <c>text</c> is kept current.</param>
        /// <param name="text">What to show.</param>
        /// <param name="localization">The localization to use; null uses <see cref="UiLocalization.Default"/>.</param>
        /// <returns>The binder; nobody needs to keep it.</returns>
        public static LocalizedTextBinder BindText(this TextElement element, UiText text, UiLocalization localization = null)
        {
            var binder = new LocalizedTextBinder(element, value => element.text = value, localization);
            binder.SetText(text);
            return binder;
        }

        /// <summary>Binds any other text property (for example <c>toggle.label</c>) through a setter.</summary>
        /// <param name="owner">The element whose lifetime the binding follows.</param>
        /// <param name="text">What to show.</param>
        /// <param name="setter">Writes the resolved string.</param>
        /// <param name="localization">The localization to use; null uses <see cref="UiLocalization.Default"/>.</param>
        /// <returns>The binder.</returns>
        public static LocalizedTextBinder BindText(this VisualElement owner, UiText text, Action<string> setter, UiLocalization localization = null)
        {
            var binder = new LocalizedTextBinder(owner, setter, localization);
            binder.SetText(text);
            return binder;
        }

        /// <summary>Shortcut for <c>BindText(UiText.FromKey(key, args))</c>.</summary>
        /// <param name="element">The element whose <c>text</c> is kept current.</param>
        /// <param name="key">The key, or <c>Table:KEY</c>.</param>
        /// <param name="args">Positional arguments for <c>{0}</c> placeholders.</param>
        /// <returns>The binder.</returns>
        public static LocalizedTextBinder BindKey(this TextElement element, string key, params object[] args) =>
            element.BindText(UiText.FromKey(key, args));
    }
}
