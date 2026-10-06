using UnityEngine.Scripting;

// The backend registers itself from a RuntimeInitializeOnLoadMethod and nothing references this assembly by type, so managed stripping must keep it.
[assembly: AlwaysLinkAssembly]
