using UnityEngine.UIElements;

namespace FutureDigitalPlus.UIKit.Hud
{
    /// <summary>
    /// Decides whether animation is reduced for an element. The kit's accessibility layer puts the class
    /// <c>fdp-reduce-motion</c> on the panel root; any element below it counts as reduced.
    /// </summary>
    internal static class MotionGate
    {
        public const string ReduceMotionClass = "fdp-reduce-motion";

        public static bool IsReduced(VisualElement element)
        {
            for (var e = element; e != null; e = e.hierarchy.parent)
                if (e.ClassListContains(ReduceMotionClass)) return true;
            return false;
        }
    }
}
