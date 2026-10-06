using UnityEngine;
using UnityEngine.UIElements;

namespace FutureDigitalPlus.UIKit.Foundation
{
    /// <summary>
    /// Loads UXML and USS from a Resources folder: <c>Resources/&lt;folder&gt;/&lt;Name&gt;</c>. Pass the folder your game keeps its screens in,
    /// for example <c>new ResourcesAssetSource("Screens")</c>.
    /// </summary>
    public sealed class ResourcesAssetSource : IUiAssetSource
    {
        /// <summary>Folder used when none is given.</summary>
        public const string DefaultFolder = "UI/";

        readonly string _folder;

        /// <summary>Creates a source for <paramref name="folder"/> (a trailing slash is added when missing; null or empty means the Resources root).</summary>
        public ResourcesAssetSource(string folder = DefaultFolder)
        {
            if (string.IsNullOrEmpty(folder))
                _folder = string.Empty;
            else
                _folder = folder.EndsWith("/") ? folder : folder + "/";
        }

        /// <inheritdoc />
        public VisualTreeAsset LoadUxml(string name)
        {
            return string.IsNullOrEmpty(name) ? null : Resources.Load<VisualTreeAsset>(_folder + name);
        }

        /// <inheritdoc />
        public StyleSheet LoadStyle(string name)
        {
            return string.IsNullOrEmpty(name) ? null : Resources.Load<StyleSheet>(_folder + name);
        }
    }
}
