using UnityEngine.Localization;
using UnityEngine.UIElements;

namespace FutureDigitalPlus.UIKit.Language
{
    /// <summary>Binds a Unity Localization <see cref="LocalizedString"/> to a UI Toolkit text element.</summary>
    public static class LocalizedStringBinding
    {
        /// <summary>
        /// Shows <paramref name="localized"/> in <paramref name="element"/> and keeps it current: while the element is on a panel the
        /// <see cref="LocalizedString.StringChanged"/> event writes the text (it fires when the locale changes and when smart-string
        /// arguments change); the subscription is removed when the element leaves the panel.
        /// </summary>
        /// <param name="element">The label, button or other text element.</param>
        /// <param name="localized">The string to show.</param>
        public static void BindText(this TextElement element, LocalizedString localized)
        {
            if (element == null || localized == null) return;

            void OnChanged(string value) => element.text = value;

            void Attach(AttachToPanelEvent evt) => localized.StringChanged += OnChanged;
            void Detach(DetachFromPanelEvent evt) => localized.StringChanged -= OnChanged;

            element.RegisterCallback<AttachToPanelEvent>(Attach);
            element.RegisterCallback<DetachFromPanelEvent>(Detach);
            if (element.panel != null) localized.StringChanged += OnChanged;
        }
    }
}
