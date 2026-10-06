using System.Collections.Generic;
using UnityEngine.UIElements;

namespace FutureDigitalPlus.UIKit.Foundation
{
    /// <summary>Asks several sources in order and returns the first hit (for example screen definitions first, then Resources).</summary>
    public sealed class CompositeAssetSource : IUiAssetSource
    {
        readonly List<IUiAssetSource> _sources = new List<IUiAssetSource>();

        /// <summary>Creates a composite over <paramref name="sources"/>; null entries are skipped.</summary>
        public CompositeAssetSource(params IUiAssetSource[] sources)
        {
            if (sources == null)
                return;
            for (int i = 0; i < sources.Length; i++)
            {
                if (sources[i] != null)
                    _sources.Add(sources[i]);
            }
        }

        /// <inheritdoc />
        public VisualTreeAsset LoadUxml(string name)
        {
            for (int i = 0; i < _sources.Count; i++)
            {
                VisualTreeAsset asset = _sources[i].LoadUxml(name);
                if (asset != null)
                    return asset;
            }
            return null;
        }

        /// <inheritdoc />
        public StyleSheet LoadStyle(string name)
        {
            for (int i = 0; i < _sources.Count; i++)
            {
                StyleSheet asset = _sources[i].LoadStyle(name);
                if (asset != null)
                    return asset;
            }
            return null;
        }
    }
}
