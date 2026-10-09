using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace FutureDigitalPlus.UIKit.Foundation
{
    /// <summary>
    /// A screen's assets as one ScriptableObject with direct references: the UXML, its style sheet and optional extra sheets. Create it with
    /// <c>Assets &gt; Create &gt; FDP UI Kit &gt; Foundation &gt; Screen Definition</c>; there is no registry to build and no Resources
    /// folder to keep in sync. Return it from <see cref="UiScreen.Definition"/>, or register it with <see cref="ScreenCatalog.Register(ScreenDefinition, System.Func{UiScreen})"/>.
    /// </summary>
    [CreateAssetMenu(menuName = "FDP UI Kit/Foundation/Screen Definition", fileName = "ScreenDefinition")]
    public sealed class ScreenDefinition : ScriptableObject
    {
        [SerializeField] string _id;
        [SerializeField] VisualTreeAsset _layout;
        [SerializeField] StyleSheet _style;
        [SerializeField] List<StyleSheet> _extraStyles = new List<StyleSheet>();

        /// <summary>The screen id (router id). Falls back to the asset name when empty.</summary>
        public string Id
        {
            get { return string.IsNullOrEmpty(_id) ? name : _id; }
        }

        /// <summary>The UXML of the screen, or null for a screen built in code.</summary>
        public VisualTreeAsset Layout
        {
            get { return _layout; }
        }

        /// <summary>The style sheet that belongs to this screen, or null.</summary>
        public StyleSheet Style
        {
            get { return _style; }
        }

        /// <summary>Further style sheets added after <see cref="Style"/>.</summary>
        public IReadOnlyList<StyleSheet> ExtraStyles
        {
            get { return _extraStyles; }
        }

        /// <summary>Creates a definition at run time (tests, tools). The caller owns the object.</summary>
        public static ScreenDefinition Create(string id, VisualTreeAsset layout, StyleSheet style = null)
        {
            ScreenDefinition definition = CreateInstance<ScreenDefinition>();
            definition._id = id;
            definition._layout = layout;
            definition._style = style;
            definition.name = id;
            return definition;
        }
    }

    /// <summary>An <see cref="IUiAssetSource"/> over a list of <see cref="ScreenDefinition"/> assets: the UXML or style sheet whose id equals the requested name.</summary>
    public sealed class ScreenDefinitionAssetSource : IUiAssetSource
    {
        readonly Dictionary<string, ScreenDefinition> _byId = new Dictionary<string, ScreenDefinition>(System.StringComparer.Ordinal);

        /// <summary>Creates the source; null entries are skipped and a later definition with the same id wins.</summary>
        public ScreenDefinitionAssetSource(IEnumerable<ScreenDefinition> definitions)
        {
            if (definitions == null)
                return;
            foreach (ScreenDefinition definition in definitions)
            {
                // The Unity null check also skips destroyed assets.
                if (definition != null)
                    _byId[definition.Id] = definition;
            }
        }

        /// <inheritdoc />
        public VisualTreeAsset LoadUxml(string name)
        {
            ScreenDefinition definition;
            return name != null && _byId.TryGetValue(name, out definition) ? definition.Layout : null;
        }

        /// <inheritdoc />
        public StyleSheet LoadStyle(string name)
        {
            ScreenDefinition definition;
            return name != null && _byId.TryGetValue(name, out definition) ? definition.Style : null;
        }
    }
}
