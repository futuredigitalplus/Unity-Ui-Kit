using System.Collections;
using UnityEngine;
using UnityEngine.UIElements;

namespace FutureDigitalPlus.UIKit.Controls.Tests
{
    /// <summary>
    /// A UIDocument rendering into a fixed-size RenderTexture (layout does not depend on the Game view), with the design
    /// system base sheet and the Controls sheet installed on the document root. Dispose it in TearDown.
    /// </summary>
    internal sealed class ControlsPanel
    {
        public const int Width = 800;
        public const int Height = 600;
        private const string DesignSystemSheet = "FutureDigitalPlus/UIKit/DesignSystem/Styles/DesignSystem";

        public GameObject Host;
        public UIDocument Document;
        public PanelSettings Settings;
        public RenderTexture Target;

        public VisualElement Root => Document.rootVisualElement;

        public static ControlsPanel Create(bool reduceMotion = false)
        {
            var p = new ControlsPanel();
            p.Target = new RenderTexture(Width, Height, 0);
            p.Settings = ScriptableObject.CreateInstance<PanelSettings>();
            // The default runtime theme, like any game's PanelSettings has: it gives the document root its size and text its font.
            p.Settings.themeStyleSheet = Resources.Load<ThemeStyleSheet>("FutureDigitalPlus/UIKit/Tests/DefaultRuntimeTheme");
            p.Settings.scaleMode = PanelScaleMode.ConstantPixelSize;
            p.Settings.scale = 1f;
            p.Settings.targetTexture = p.Target;
            p.Settings.clearColor = true;
            p.Settings.colorClearValue = new Color(0f, 0f, 0f, 0f);

            p.Host = new GameObject("ControlsTestHost");
            p.Host.SetActive(false);
            p.Document = p.Host.AddComponent<UIDocument>();
            p.Document.panelSettings = p.Settings;
            p.Host.SetActive(true);

            var root = p.Document.rootVisualElement;
            var sheet = Resources.Load<StyleSheet>(DesignSystemSheet);
            if (sheet != null) root.styleSheets.Add(sheet);
            ControlsStyles.Install(root);
            root.AddToClassList("ds-root");
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

        public void Dispose()
        {
            if (Host != null) Object.Destroy(Host);
            if (Settings != null) Object.Destroy(Settings);
            if (Target != null)
            {
                Target.Release();
                Object.Destroy(Target);
            }
        }
    }

    /// <summary>Dispatches UI Toolkit events straight to an element, the way the panel would after reading a device.</summary>
    internal static class Send
    {
        public static void Submit(VisualElement target)
        {
            using (var e = NavigationSubmitEvent.GetPooled())
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

        public static void Key(VisualElement target, KeyCode key, char character = '\0')
        {
            using (var e = KeyDownEvent.GetPooled(character, key, EventModifiers.None))
            {
                e.target = target;
                target.SendEvent(e);
            }
        }

        public static void PointerDown(VisualElement target, Vector2 position, int button = 0)
        {
            var mouse = new Event { type = EventType.MouseDown, mousePosition = position, button = button };
            using (var e = PointerDownEvent.GetPooled(mouse))
            {
                e.target = target;
                target.SendEvent(e);
            }
        }

        public static void TouchDown(VisualElement target, Vector2 position)
        {
            var finger = new Touch { fingerId = 0, position = position, phase = TouchPhase.Began };
            using (var e = PointerDownEvent.GetPooled(finger, EventModifiers.None))
            {
                e.target = target;
                target.SendEvent(e);
            }
        }

        public static void TouchUp(VisualElement target, Vector2 position)
        {
            var finger = new Touch { fingerId = 0, position = position, phase = TouchPhase.Ended };
            using (var e = PointerUpEvent.GetPooled(finger, EventModifiers.None))
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

        /// <summary>A left press and release at the centre of the element.</summary>
        public static void Tap(VisualElement target)
        {
            var center = target.worldBound.center;
            PointerDown(target, center);
            PointerUp(target, center);
        }
    }
}
