using UnityEngine;

namespace FutureDigitalPlus.UIKit.Foundation
{
    /// <summary>
    /// <see cref="IUiStateStore"/> over <c>PlayerPrefs</c>: small plain-text values (the navigation stack, the selected tab) that stay on the device.
    /// Keys are prefixed so several games or shells on one machine do not collide: <c>&lt;prefix&gt;/&lt;key&gt;</c>.
    /// </summary>
    public sealed class PlayerPrefsUiStateStore : IUiStateStore
    {
        readonly string _prefix;

        /// <summary>Creates a store whose keys start with <paramref name="prefix"/> (for example the game's name). Null or empty uses <c>fdp</c>.</summary>
        public PlayerPrefsUiStateStore(string prefix = "fdp")
        {
            _prefix = string.IsNullOrEmpty(prefix) ? "fdp" : prefix;
        }

        /// <inheritdoc />
        public string Load(string key)
        {
            string full = Full(key);
            return PlayerPrefs.HasKey(full) ? PlayerPrefs.GetString(full) : null;
        }

        /// <inheritdoc />
        public void Save(string key, string value)
        {
            if (value == null)
            {
                Delete(key);
                return;
            }
            PlayerPrefs.SetString(Full(key), value);
        }

        /// <inheritdoc />
        public void Delete(string key)
        {
            PlayerPrefs.DeleteKey(Full(key));
        }

        string Full(string key)
        {
            return _prefix + "/" + key;
        }
    }
}
