namespace FutureDigitalPlus.UIKit.Foundation
{
    /// <summary>Where the foundation keeps small pieces of UI state between runs: the navigation stack, a selected tab. Values are plain strings.</summary>
    public interface IUiStateStore
    {
        /// <summary>The stored value, or null when there is none.</summary>
        string Load(string key);

        /// <summary>Stores a value under a key.</summary>
        void Save(string key, string value);

        /// <summary>Removes a key. Removing a key that does not exist is harmless.</summary>
        void Delete(string key);
    }
}
