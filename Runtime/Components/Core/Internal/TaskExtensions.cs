// FutureDigitalPlus: renamed by Tools~/rename/rename.py (namespaces, resource paths); see SOURCE.md "Rename record".
using System.Collections;
using System.Runtime.ExceptionServices;
using System.Threading.Tasks;
using UnityEngine.TestTools;

namespace FutureDigitalPlus.UIKit.Components.Internal
{
    [ExcludeFromCoverage]
    internal static class TaskExtensions
    {
        public static IEnumerator AsEnumerator(this Task task)
        {
            while (!task.IsCompleted)
                yield return null;

            if (task.IsFaulted)
                ExceptionDispatchInfo.Capture(task.Exception!).Throw();

            yield return null;
        }
    }
}
