using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UIElements;

namespace FutureDigitalPlus.UIKit.Hud.Tests
{
    /// <summary>
    /// A UIDocument rendering into a fixed-size RenderTexture (layout does not depend on the Game view), with the design
    /// system base sheet and the HUD sheet installed. Dispose it in TearDown.
    /// </summary>
    internal sealed class HudPanel
    {
        public const int Width = 800;
        public const int Height = 600;
        private const string DesignSystemSheet = "FutureDigitalPlus/UIKit/DesignSystem/Styles/DesignSystem";

        public GameObject Host;
        public UIDocument Document;
        public PanelSettings Settings;
        public RenderTexture Target;

        public VisualElement Root => Document.rootVisualElement;

        public static HudPanel Create(bool reduceMotion = false, bool toScreen = false)
        {
            var p = new HudPanel();
            if (!toScreen) p.Target = new RenderTexture(Width, Height, 24);   // with a depth buffer: a camera that renders into it needs one in URP's render graph
            p.Settings = ScriptableObject.CreateInstance<PanelSettings>();
            // The default runtime theme, like any game's PanelSettings has: it gives the document root its size and text its font.
            p.Settings.themeStyleSheet = Resources.Load<ThemeStyleSheet>("FutureDigitalPlus/UIKit/Tests/DefaultRuntimeTheme");
            p.Settings.scaleMode = PanelScaleMode.ConstantPixelSize;
            p.Settings.scale = 1f;
            if (!toScreen) p.Settings.targetTexture = p.Target;
            p.Settings.clearColor = true;
            p.Settings.colorClearValue = new Color(0f, 0f, 0f, 0f);

            p.Host = new GameObject("HudTestHost");
            p.Host.SetActive(false);
            p.Document = p.Host.AddComponent<UIDocument>();
            p.Document.panelSettings = p.Settings;
            p.Host.SetActive(true);

            var root = p.Document.rootVisualElement;
            var sheet = Resources.Load<StyleSheet>(DesignSystemSheet);
            if (sheet != null) root.styleSheets.Add(sheet);
            HudStyles.Install(root);
            root.AddToClassList("ds-root");
            root.AddToClassList("ds-root--hud");
            if (reduceMotion) root.AddToClassList("fdp-reduce-motion");
            return p;
        }

        public static IEnumerator Frames(int count = 3)
        {
            for (var i = 0; i < count; i++) yield return null;
        }

        public static IEnumerator WaitSeconds(float seconds)
        {
            var end = Time.realtimeSinceStartup + seconds;
            while (Time.realtimeSinceStartup < end) yield return null;
        }

        /// <summary>Reads one pixel of the panel's render texture; <paramref name="yFromTop"/> counts from the top, like panel coordinates.</summary>
        public Color ReadPixel(int x, int yFromTop)
        {
            var previous = RenderTexture.active;
            RenderTexture.active = Target;
            var texture = new Texture2D(1, 1, TextureFormat.RGBA32, false);
            texture.ReadPixels(new Rect(x, Height - 1 - yFromTop, 1, 1), 0, 0);
            texture.Apply();
            var color = texture.GetPixel(0, 0);
            UnityEngine.Object.Destroy(texture);
            RenderTexture.active = previous;
            return color;
        }

        public void Dispose()
        {
            if (Host != null) UnityEngine.Object.Destroy(Host);
            if (Settings != null) UnityEngine.Object.Destroy(Settings);
            if (Target != null)
            {
                Target.Release();
                UnityEngine.Object.Destroy(Target);
            }
        }
    }

    /// <summary>
    /// Measures managed allocations of a delegate on the current thread. Only a runtime that counts allocations per thread (IL2CPP, .NET) can: the Mono editor cannot, and
    /// there is no honest substitute (its heap counter is flat while the collector frees what a step allocated, and moves with other threads' allocations), so the tests that use it skip themselves there.
    /// </summary>
    internal static class AllocProbe
    {
        /// <summary>True when the runtime reports allocations (a known allocation is seen).</summary>
        public static bool IsAvailable()
        {
            var before = GC.GetAllocatedBytesForCurrentThread();
            var junk = new byte[4096];
            GC.KeepAlive(junk);
            return GC.GetAllocatedBytesForCurrentThread() - before >= 4096;
        }

        /// <summary>Bytes allocated by <paramref name="action"/> run <paramref name="runs"/> times after <paramref name="warmUp"/> runs.</summary>
        public static long Measure(Action action, int warmUp = 5, int runs = 200)
        {
            for (var i = 0; i < warmUp; i++) action();
            var before = GC.GetAllocatedBytesForCurrentThread();
            for (var i = 0; i < runs; i++) action();
            return GC.GetAllocatedBytesForCurrentThread() - before;
        }
    }
}
