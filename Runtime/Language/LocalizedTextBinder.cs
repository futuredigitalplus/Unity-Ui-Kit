using System;
using FutureDigitalPlus.UIKit.Foundation;
using UnityEngine.UIElements;

namespace FutureDigitalPlus.UIKit.Language
{
    /// <summary>
    /// Keeps one text property of a UI Toolkit element in sync with a <see cref="UiText"/>: resolves it now, again whenever the
    /// language changes (while the element is attached to a panel), and never throws when a string table is missing. The element
    /// holds the callbacks, so nobody has to keep or dispose the binder; it unsubscribes when the element leaves the panel.
    /// <code>
    /// label.BindText(UiText.FromKey("HOMEBTN-PLAY"));
    /// toggle.BindText(UiText.FromKey("SETTING-SOUND"), text =&gt; toggle.label = text);
    /// </code>
    /// </summary>
    public sealed class LocalizedTextBinder
    {
        private readonly VisualElement _owner;
        private readonly Action<string> _setter;
        private readonly UiLocalization _localization;
        private UiText _text = UiText.Empty;
        private UiLocalization _subscribedTo;

        // True once SetText ran. Until then the element's text belongs to whoever assigned it directly (a screen that sets Label.text on
        // a row template), and attaching to a panel must not overwrite it with the empty default.
        private bool _bound;

        /// <summary>Creates a binder.</summary>
        /// <param name="owner">The element whose lifetime (attach and detach) the binding follows.</param>
        /// <param name="setter">Writes the resolved string, for example <c>value =&gt; label.text = value</c>.</param>
        /// <param name="localization">The localization to use; null uses <see cref="UiLocalization.Default"/> (looked up on every resolve, so replacing the default takes effect).</param>
        public LocalizedTextBinder(VisualElement owner, Action<string> setter, UiLocalization localization = null)
        {
            _owner = owner ?? throw new ArgumentNullException(nameof(owner));
            _setter = setter ?? throw new ArgumentNullException(nameof(setter));
            _localization = localization;

            owner.RegisterCallback<AttachToPanelEvent>(OnAttach);
            owner.RegisterCallback<DetachFromPanelEvent>(OnDetach);
            if (owner.panel != null) Subscribe();
        }

        /// <summary>The text last set.</summary>
        public UiText Text => _text;

        private UiLocalization Localization => _localization ?? UiLocalization.Default;

        /// <summary>Sets the text and applies it immediately.</summary>
        public void SetText(UiText text)
        {
            _text = text ?? UiText.Empty;
            _bound = true;
            Apply();
        }

        /// <summary>Resolves the text again and writes it. Does nothing before the first <see cref="SetText"/>.</summary>
        public void Apply()
        {
            if (!_bound) return;

            _setter(Localization.Resolve(_text));
        }

        private void OnAttach(AttachToPanelEvent evt)
        {
            Subscribe();
            Apply();
        }

        private void OnDetach(DetachFromPanelEvent evt)
        {
            if (_subscribedTo == null) return;

            _subscribedTo.Changed -= Apply;
            _subscribedTo = null;
        }

        private void Subscribe()
        {
            if (_subscribedTo != null) return;

            _subscribedTo = Localization;
            _subscribedTo.Changed += Apply;
        }
    }
}
