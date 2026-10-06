// FutureDigitalPlus: renamed by Tools~/rename/rename.py (namespaces, resource paths); see SOURCE.md "Rename record".
using UnityEngine;

namespace FutureDigitalPlus.UIKit.Components
{
    /// <summary>
    /// The default logger for FutureDigitalPlus.UIKit.Components. Uses Debug.Log.
    /// </summary>
    public class DebugLogger : ILogger
    {
        public void Log(string message, object caller)
        {
            Debug.LogFormat("[{0}] {1}", caller.GetType().Name, message);
        }
        
        public void LogWarning(string message, object caller)
        {
            Debug.LogWarningFormat("[{0}] {1}", caller.GetType().Name, message);
        }
        
        public void LogError(string message, object caller)
        {
            Debug.LogErrorFormat("[{0}] {1}", caller.GetType().Name, message);
        }
    }
}
