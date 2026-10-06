using System;
using System.Collections.Generic;
using FutureDigitalPlus.UIKit.Foundation;
using UnityEngine.UIElements;

namespace FutureDigitalPlus.UIKit.Language
{
    /// <summary>
    /// Writes the active language onto a root element so stylesheets can react to it: the class <c>fdp-lang-&lt;code&gt;</c>
    /// (see <see cref="LocaleStyle"/>; for example a font switch per script), the class <see cref="RtlClass"/> for a right-to-left
    /// language (which switches on the mirrored layout of <c>Rtl.uss</c>), and <see cref="VisualElement.languageDirection"/>.
    /// </summary>
    public static class LocaleClassApplier
    {
        /// <summary>Added to the root while the language is written right to left.</summary>
        public const string RtlClass = "fdp-rtl";

        /// <summary>
        /// Puts the classes of <paramref name="localeCode"/> on <paramref name="root"/> and removes those of the language it had
        /// before. Idempotent.
        /// </summary>
        /// <param name="root">The root element (document root or panel root).</param>
        /// <param name="localeCode">A language code such as <c>en</c>, <c>ar</c>, <c>zh-Hant</c>.</param>
        /// <returns>True when the language is right to left.</returns>
        public static bool Apply(VisualElement root, string localeCode)
        {
            if (root == null) return false;

            var wanted = LocaleStyle.ClassFor(localeCode);
            var stale = new List<string>();
            foreach (var existing in root.GetClasses())
                if (existing.StartsWith(LocaleStyle.ClassPrefix, StringComparison.Ordinal) && existing != wanted)
                    stale.Add(existing);

            foreach (var name in stale) root.RemoveFromClassList(name);
            root.AddToClassList(wanted);

            var rtl = LocaleStyle.IsRightToLeft(localeCode);
            root.EnableInClassList(RtlClass, rtl);
            root.languageDirection = rtl ? LanguageDirection.RTL : LanguageDirection.LTR;
            return rtl;
        }

        /// <summary>
        /// Applies the language of <paramref name="localization"/> to <paramref name="root"/> now and again every time it changes,
        /// for as long as the root is on a panel. Dispose the result to stop earlier.
        /// </summary>
        /// <param name="root">The root element.</param>
        /// <param name="localization">The localization to follow; null uses <see cref="UiLocalization.Default"/>.</param>
        /// <returns>The subscription.</returns>
        public static IDisposable Bind(VisualElement root, UiLocalization localization = null)
        {
            if (root == null) throw new ArgumentNullException(nameof(root));

            return new Subscription(root, localization ?? UiLocalization.Default);
        }

        private sealed class Subscription : IDisposable
        {
            private readonly VisualElement _root;
            private readonly UiLocalization _localization;
            private bool _subscribed;
            private bool _disposed;

            public Subscription(VisualElement root, UiLocalization localization)
            {
                _root = root;
                _localization = localization;
                root.RegisterCallback<AttachToPanelEvent>(OnAttach);
                root.RegisterCallback<DetachFromPanelEvent>(OnDetach);
                Subscribe();
                Refresh();
            }

            public void Dispose()
            {
                if (_disposed) return;

                _disposed = true;
                _root.UnregisterCallback<AttachToPanelEvent>(OnAttach);
                _root.UnregisterCallback<DetachFromPanelEvent>(OnDetach);
                Unsubscribe();
            }

            private void OnAttach(AttachToPanelEvent evt)
            {
                Subscribe();
                Refresh();
            }

            private void OnDetach(DetachFromPanelEvent evt) => Unsubscribe();

            private void Subscribe()
            {
                if (_subscribed) return;

                _localization.Changed += Refresh;
                _subscribed = true;
            }

            private void Unsubscribe()
            {
                if (!_subscribed) return;

                _localization.Changed -= Refresh;
                _subscribed = false;
            }

            private void Refresh() => Apply(_root, _localization.CurrentLocale);
        }
    }
}
