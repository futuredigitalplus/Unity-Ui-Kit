using System.Collections;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Rendering;

namespace FutureDigitalPlus.UIKit.PlatformSmoke.Tests
{
    /// <summary>Small helpers shared by the platform smoke tests.</summary>
    internal static class PlatformTestUtil
    {
        /// <summary>Name of the active render pipeline asset type, or "Built-in".</summary>
        public static string Pipeline() =>
            GraphicsSettings.currentRenderPipeline == null ? "Built-in" : GraphicsSettings.currentRenderPipeline.GetType().Name;

        /// <summary>The renderer type of a URP asset (for example <c>UniversalRenderer</c> or <c>Renderer2D</c>), found by reflection so no pipeline package is referenced.</summary>
        public static string Renderer()
        {
            var rp = GraphicsSettings.currentRenderPipeline;
            if (rp == null) return "n/a";
            var property = rp.GetType().GetProperty("scriptableRenderer");
            return property?.GetValue(rp)?.GetType().Name ?? rp.GetType().Name;
        }

        /// <summary>True when there is no graphics device (batch mode with -nographics): render tests cannot run.</summary>
        public static bool NoGraphics => SystemInfo.graphicsDeviceType == GraphicsDeviceType.Null;

        /// <summary>Yields frames until the task finishes or <paramref name="seconds"/> of real time pass.</summary>
        public static IEnumerator Await(Task task, float seconds = 10f)
        {
            double end = Time.realtimeSinceStartupAsDouble + seconds;
            while (!task.IsCompleted && Time.realtimeSinceStartupAsDouble < end) yield return null;
        }

        /// <summary>Reads one pixel of a render texture (0,0 = bottom left) as a gamma colour.</summary>
        public static Color ReadPixel(RenderTexture rt, int x, int y)
        {
            var previous = RenderTexture.active;
            RenderTexture.active = rt;
            var tex = new Texture2D(1, 1, TextureFormat.RGBA32, false);
            try
            {
                tex.ReadPixels(new Rect(x, y, 1, 1), 0, 0);
                tex.Apply();
                return tex.GetPixel(0, 0);
            }
            finally
            {
                RenderTexture.active = previous;
                Object.Destroy(tex);
            }
        }
    }
}
