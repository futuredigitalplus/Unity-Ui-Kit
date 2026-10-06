using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UIElements;

namespace FutureDigitalPlus.UIKit.Gameplay.Tests
{
    /// <summary>
    /// A UIDocument rendering into a fixed-size RenderTexture (layout does not depend on the Game view), with the design
    /// system base sheet and the Gameplay sheet installed on the document root. Dispose it in TearDown.
    /// </summary>
    internal sealed class GameplayPanel
    {
        public const int Width = 800;
        public const int Height = 600;
        private const string DesignSystemSheet = "FutureDigitalPlus/UIKit/DesignSystem/Styles/DesignSystem";

        public GameObject Host;
        public UIDocument Document;
        public PanelSettings Settings;
        public RenderTexture Target;

        public VisualElement Root => Document.rootVisualElement;

        public static GameplayPanel Create(bool navigationMode = false)
        {
            var p = new GameplayPanel();
            p.Target = new RenderTexture(Width, Height, 0);
            p.Settings = ScriptableObject.CreateInstance<PanelSettings>();
            // The default runtime theme, like any game's PanelSettings has: it gives the document root its size and text its font.
            p.Settings.themeStyleSheet = Resources.Load<ThemeStyleSheet>("FutureDigitalPlus/UIKit/Tests/DefaultRuntimeTheme");
            p.Settings.scaleMode = PanelScaleMode.ConstantPixelSize;
            p.Settings.scale = 1f;
            p.Settings.targetTexture = p.Target;
            p.Settings.clearColor = true;
            p.Settings.colorClearValue = new Color(0f, 0f, 0f, 0f);

            p.Host = new GameObject("GameplayTestHost");
            p.Host.SetActive(false);
            p.Document = p.Host.AddComponent<UIDocument>();
            p.Document.panelSettings = p.Settings;
            p.Host.SetActive(true);

            var root = p.Document.rootVisualElement;
            var sheet = Resources.Load<StyleSheet>(DesignSystemSheet);
            if (sheet != null) root.styleSheets.Add(sheet);
            GameplayStyles.Install(root);
            root.AddToClassList("ds-root");
            if (navigationMode) root.AddToClassList("fdp-input-nav");
            return p;
        }

        public static IEnumerator Frames(int count = 3)
        {
            for (var i = 0; i < count; i++) yield return null;
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


    /// <summary>Dispatches UI Toolkit events straight to an element, the way the panel would after reading a device.</summary>
    internal static class Send
    {
        public static void Submit(VisualElement target, EventModifiers modifiers = EventModifiers.None)
        {
            using (var e = NavigationSubmitEvent.GetPooled(modifiers))
            {
                e.target = target;
                target.SendEvent(e);
            }
        }

        public static void Cancel(VisualElement target)
        {
            using (var e = NavigationCancelEvent.GetPooled())
            {
                e.target = target;
                target.SendEvent(e);
            }
        }

        public static void Move(VisualElement target, NavigationMoveEvent.Direction direction)
        {
            using (var e = NavigationMoveEvent.GetPooled(direction))
            {
                e.target = target;
                target.SendEvent(e);
            }
        }

        public static void Key(VisualElement target, KeyCode key)
        {
            using (var e = KeyDownEvent.GetPooled('\0', key, EventModifiers.None))
            {
                e.target = target;
                target.SendEvent(e);
            }
        }

        public static void PointerDown(VisualElement target, Vector2 position, int button = 0, EventModifiers modifiers = EventModifiers.None)
        {
            var mouse = new Event { type = EventType.MouseDown, mousePosition = position, button = button, modifiers = modifiers };
            using (var e = PointerDownEvent.GetPooled(mouse))
            {
                e.target = target;
                target.SendEvent(e);
            }
        }

        public static void PointerMove(VisualElement target, Vector2 position)
        {
            var mouse = new Event { type = EventType.MouseMove, mousePosition = position };
            using (var e = PointerMoveEvent.GetPooled(mouse))
            {
                e.target = target;
                target.SendEvent(e);
            }
        }

        public static void PointerUp(VisualElement target, Vector2 position, int button = 0)
        {
            var mouse = new Event { type = EventType.MouseUp, mousePosition = position, button = button };
            using (var e = PointerUpEvent.GetPooled(mouse))
            {
                e.target = target;
                target.SendEvent(e);
            }
        }
    }
}
