// FutureDigitalPlus: renamed by Tools~/rename/rename.py (namespaces, resource paths); see SOURCE.md "Rename record".
using System.Threading.Tasks;
using UnityEngine;

namespace FutureDigitalPlus.UIKit.Components
{
    /// <summary>
    /// An IAssetResolver which loads assets from Resources.
    /// </summary>
    [System.Obsolete("Use ResourcesAssetSource instead.")]
    public class ResourcesAssetResolver : IAssetResolver
    {
        public Task<T> LoadAsset<T>(string assetPath) where T : Object
        {
            var request = Resources.LoadAsync<T>(assetPath);

            var taskCompletionSource = new TaskCompletionSource<T>();

            request.completed += _ => taskCompletionSource.SetResult(request.asset as T);
            
            return taskCompletionSource.Task;
        }

        public Task<bool> AssetExists(string assetPath)
        {
            var request = Resources.LoadAsync(assetPath);
            
            var taskCompletionSource = new TaskCompletionSource<bool>();
            
            request.completed += _ => taskCompletionSource.SetResult(request.asset != null);
            
            return taskCompletionSource.Task;
        }
    }
}
