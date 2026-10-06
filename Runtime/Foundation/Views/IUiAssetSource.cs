using UnityEngine.UIElements;

namespace FutureDigitalPlus.UIKit.Foundation
{
    /// <summary>
    /// Finds UXML and USS assets by name. A screen named <c>Home</c> asks for the UXML <c>Home</c> and the style sheet <c>Home</c>.
    /// Implementations: <see cref="ResourcesAssetSource"/>, <see cref="ScreenDefinitionAssetSource"/>, <see cref="CompositeAssetSource"/>;
    /// a game can add its own (Addressables, asset bundles).
    /// </summary>
    public interface IUiAssetSource
    {
        /// <summary>The template with this name (file name without extension), or null.</summary>
        VisualTreeAsset LoadUxml(string name);

        /// <summary>The style sheet with this name (file name without extension), or null.</summary>
        StyleSheet LoadStyle(string name);
    }
}
