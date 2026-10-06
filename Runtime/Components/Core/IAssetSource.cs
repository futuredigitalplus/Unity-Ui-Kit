// FutureDigitalPlus: renamed by Tools~/rename/rename.py (namespaces, resource paths); see SOURCE.md "Rename record".
using System.Threading.Tasks;
using UnityEngine;

namespace FutureDigitalPlus.UIKit.Components
{
    /// <summary>
    /// Loads assets for FutureDigitalPlus.UIKit.Components. Handles both path resolution
    /// and asset loading for a given backend (Resources, AssetDatabase, etc.).
    /// </summary>
    public interface IAssetSource
    {
        Task<T> LoadAsset<T>(string assetPath) where T : Object;
        Task<bool> AssetExists(string assetPath);
    }
}
