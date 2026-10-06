using System;
using System.Runtime.CompilerServices;
using UnityEngine;
using UnityEngine.UIElements;

namespace FutureDigitalPlus.UIKit.Foundation
{
    /// <summary>
    /// Turns <see cref="UiText"/> into display strings for the views of one <see cref="UiShell"/> and keeps bound elements current when the locale changes.
    /// It is a seam, not a localization system: pass the resolver of your localization (the Language module's, Unity Localization, your own table) to the
    /// constructor and call <see cref="NotifyChanged"/> when the selected locale changes. With no resolver the text of a literal, the fallback of a key, or
    /// a visible <c>[KEY]</c> placeholder is shown, and nothing throws.
    /// </summary>
    public sealed class UiTextSource
    {
        sealed class Binding
        {
            readonly UiTextSource _owner;
            readonly VisualElement _element;
            Action<string> _setter;
            UiText _text = UiText.Empty;
            bool _subscribed;

            public Binding(UiTextSource owner, VisualElement element)
            {
                _owner = owner;
                _element = element;
                element.RegisterCallback<AttachToPanelEvent>(OnAttach);
                element.RegisterCallback<DetachFromPanelEvent>(OnDetach);
                if (element.panel != null)
                    Subscribe();
            }

            public void Set(UiText text, Action<string> setter)
            {
                _text = text ?? UiText.Empty;
                _setter = setter;
                Apply();
            }

            public void Apply()
            {
                if (_setter != null)
                    _setter(_owner.Resolve(_text));
            }

            void OnAttach(AttachToPanelEvent evt)
            {
                Subscribe();
                Apply();
            }

            void OnDetach(DetachFromPanelEvent evt)
            {
                if (!_subscribed)
                    return;
                _owner.Changed -= Apply;
                _subscribed = false;
            }

            void Subscribe()
            {
                if (_subscribed)
                    return;
                _owner.Changed += Apply;
                _subscribed = true;
            }
        }

        readonly Func<UiText, string> _resolver;
        readonly LocalizationKeyResolver _fallbackResolver;
        readonly ConditionalWeakTable<VisualElement, Binding> _bindings = new ConditionalWeakTable<VisualElement, Binding>();

        /// <summary>Raised by <see cref="NotifyChanged"/>. Bound elements listen while they are attached to a panel.</summary>
        public event Action Changed;

        /// <summary>Creates a text source. <paramref name="resolver"/> null uses literals, key fallbacks and <c>[KEY]</c> placeholders only.</summary>
        public UiTextSource(Func<UiText, string> resolver = null)
        {
            _resolver = resolver;
            if (resolver == null)
                _fallbackResolver = new LocalizationKeyResolver(NoTables);
        }

        /// <summary>The display string for <paramref name="text"/>. Never null, never throws.</summary>
        public string Resolve(UiText text)
        {
            if (ReferenceEquals(text, null) || text.IsEmpty)
                return string.Empty;
            try
            {
                if (_resolver == null)
                    return _fallbackResolver.Resolve(text);
                return _resolver(text) ?? string.Empty;
            }
            catch (Exception ex)
            {
                Debug.LogWarning("FutureDigitalPlus.UIKit.Foundation: text resolver threw: " + ex.Message);
                return text.Fallback ?? text.Literal ?? text.Key ?? string.Empty;
            }
        }

        /// <summary>Tells bound elements that strings may have changed (locale switch, tables loaded). Call it from the main thread.</summary>
        public void NotifyChanged()
        {
            if (_fallbackResolver != null)
                _fallbackResolver.ResetMissing();
            Action handler = Changed;
            if (handler != null)
                handler();
        }

        /// <summary>Shows <paramref name="text"/> in <paramref name="element"/> now and again whenever <see cref="NotifyChanged"/> runs while the element is attached.</summary>
        public void Bind(TextElement element, UiText text)
        {
            if (element == null)
                return;
            Bind(element, text, value => element.text = value);
        }

        /// <summary>Same for any element and property, for example <c>Bind(toggle, text, value =&gt; toggle.label = value)</c>. A second call on the same element replaces the first.</summary>
        public void Bind(VisualElement owner, UiText text, Action<string> setter)
        {
            if (owner == null || setter == null)
                return;
            Binding binding;
            if (!_bindings.TryGetValue(owner, out binding))
            {
                binding = new Binding(this, owner);
                _bindings.Add(owner, binding);
            }
            binding.Set(text, setter);
        }

        static LookupStatus NoTables(string table, string key, object[] args, out string text)
        {
            text = null;
            return LookupStatus.TableMissing;
        }
    }
}
