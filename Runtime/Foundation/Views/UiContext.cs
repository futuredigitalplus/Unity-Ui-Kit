using System;
using System.Collections.Generic;
using UnityEngine.UIElements;

namespace FutureDigitalPlus.UIKit.Foundation
{
    /// <summary>
    /// Everything a screen may use, handed to <see cref="UiScreen.Build"/>: navigation, dialogs, toasts, loading, input blocking, back handling, text
    /// resolution, layer access and asset lookup. Created by <see cref="UiShell"/>; tests can build one around a plain <c>VisualElement</c>.
    /// </summary>
    public sealed class UiContext
    {
        readonly Dictionary<UiLayer, VisualElement> _layers = new Dictionary<UiLayer, VisualElement>();

        /// <summary>The shell root element (class <c>fdp-shell</c>): carries the profile, platform, input, locale and theme classes.</summary>
        public VisualElement Root { get; }

        /// <summary>Finds the UXML and USS of screens and templates.</summary>
        public IUiAssetSource Assets { get; }

        /// <summary>Resolves <see cref="UiText"/> and rebinds labels on locale change.</summary>
        public UiTextSource Text { get; }

        /// <summary>Stack navigation between screens.</summary>
        public IScreenRouter Router { get; set; }

        /// <summary>Modal dialogs.</summary>
        public IDialogService Dialogs { get; set; }

        /// <summary>Short non-blocking messages.</summary>
        public IToastService Toasts { get; set; }

        /// <summary>The loading overlay.</summary>
        public ILoadingOverlay Loading { get; set; }

        /// <summary>Input blocking (dialogs, loading, transitions) and the pick test for the game.</summary>
        public UiInputGate Input { get; }

        /// <summary>The chain of back handlers; see <see cref="BackPriority"/>.</summary>
        public BackDispatcher Back { get; }

        /// <summary>Layout, platform and input classes of the root; null in a bare context.</summary>
        public UiProfileWatcher Profile { get; set; }

        /// <summary>Safe-area insets for the panels under the root.</summary>
        public SafeAreaProvider SafeArea { get; set; }

        /// <summary>Where screens keep small persistent values (selected tab, last page); null when the host gave none.</summary>
        public IUiStateStore State { get; set; }

        /// <summary>Creates a context around <paramref name="root"/>. <paramref name="text"/> null creates a default <see cref="UiTextSource"/>.</summary>
        public UiContext(VisualElement root, IUiAssetSource assets, UiTextSource text = null)
        {
            Root = root;
            Assets = assets;
            Text = text ?? new UiTextSource();
            Input = new UiInputGate(root);
            Back = new BackDispatcher();
        }

        /// <summary>The element of <paramref name="layer"/>, or null when this context has no such layer.</summary>
        public VisualElement GetLayer(UiLayer layer)
        {
            VisualElement element;
            return _layers.TryGetValue(layer, out element) ? element : null;
        }

        /// <summary>Registers the element that serves <paramref name="layer"/> (the shell does this for its own layers).</summary>
        public void SetLayer(UiLayer layer, VisualElement element)
        {
            if (element == null)
                throw new ArgumentNullException(nameof(element));
            _layers[layer] = element;
        }

        /// <summary>The display string for <paramref name="text"/>.</summary>
        public string Resolve(UiText text)
        {
            return Text.Resolve(text);
        }

        /// <summary>Shows <paramref name="text"/> in a label, and keeps it current when the locale changes.</summary>
        public void SetText(TextElement element, UiText text)
        {
            Text.Bind(element, text);
        }
    }
}
