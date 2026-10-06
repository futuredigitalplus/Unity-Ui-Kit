using UnityEngine;

namespace FutureDigitalPlus.UIKit.Interaction
{
    /// <summary>A hidden object (not saved with a scene, destroyed when play mode ends) that gives the input backends one <see cref="IInputBackend.Poll"/> per frame. Created by <see cref="InputFeed"/> while something listens.</summary>
    internal sealed class InteractionDriver : MonoBehaviour
    {
        internal static InteractionDriver Create()
        {
            var go = new GameObject("[UIKit Interaction]") { hideFlags = HideFlags.HideInHierarchy | HideFlags.HideInInspector };
            DontDestroyOnLoad(go);
            return go.AddComponent<InteractionDriver>();
        }

        private void Update() => InputFeed.Poll();

        internal void Dispose()
        {
            if (this != null) Destroy(gameObject);
        }
    }
}
