using System.Collections;
using FutureDigitalPlus.UIKit.Interaction;
using UnityEngine;
using UnityEngine.UIElements;

namespace FutureDigitalPlus.UIKit.Interaction.Tests
{
    /// <summary>
    /// A UIDocument that renders into a fixed-size RenderTexture, with the design system base sheet on the panel root, the Interaction sheets on the document
    /// root and a <c>ds-root</c> container to put controls in. No facade involved, so these tests do not depend on how it installs things.
    /// </summary>
    internal sealed class InteractionPanel
    {
        public const string BaseSheet = "FutureDigitalPlus/UIKit/DesignSystem/Styles/DesignSystem";

        public GameObject Host;
        public UIDocument Document;
        public PanelSettings Settings;
        public RenderTexture Target;

        /// <summary>The document root: classes written by <see cref="InputProfile"/> and <see cref="AccessibilityApplier"/> go here.</summary>
        public VisualElement Root => Document.rootVisualElement;

        /// <summary>A <c>ds-root</c> child of <see cref="Root"/>; controls live here.</summary>
        public VisualElement Screen;

        public IPanel Panel => Root.panel;

        public static InteractionPanel Create(int width = 800, int height = 600, System.Action<GameObject> configure = null, bool styles = true)
        {
            var p = new InteractionPanel();
            p.Target = new RenderTexture(width, height, 0);
            p.Settings = ScriptableObject.CreateInstance<PanelSettings>();
            p.Settings.scaleMode = PanelScaleMode.ConstantPixelSize;
            p.Settings.scale = 1f;
            p.Settings.targetTexture = p.Target;

            // Inactive while configured, so the UIDocument builds its tree once with everything in place.
            p.Host = new GameObject("InteractionTestHost");
            p.Host.SetActive(false);
            p.Document = p.Host.AddComponent<UIDocument>();
            p.Document.panelSettings = p.Settings;
            configure?.Invoke(p.Host);
            p.Host.SetActive(true);

            if (styles)
            {
                var holder = p.Root.parent ?? p.Root;
                holder.styleSheets.Add(Resources.Load<StyleSheet>(BaseSheet));
                InteractionStyles.Install(p.Root);
            }

            p.Screen = new VisualElement { name = "screen" };
            p.Screen.AddToClassList("ds-root");
            p.Screen.style.flexGrow = 1;
            p.Root.Add(p.Screen);
            return p;
        }

        public static IEnumerator Frames(int count = 3)
        {
            for (var i = 0; i < count; i++) yield return null;
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

        public static T WithClasses<T>(T element, string classes) where T : VisualElement
        {
            foreach (var c in classes.Split(' ')) element.AddToClassList(c);
            return element;
        }

        public string FocusedName => (Panel.focusController.focusedElement as VisualElement)?.name ?? "-";
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

        public static void Click(VisualElement target)
        {
            using (var e = ClickEvent.GetPooled())
            {
                e.target = target;
                target.SendEvent(e);
            }
        }

        public static void PointerDown(VisualElement target, bool touch)
        {
            var center = target.worldBound.center;
            if (touch)
            {
                var finger = new Touch { fingerId = 0, position = center, phase = TouchPhase.Began };
                using (var e = PointerDownEvent.GetPooled(finger, EventModifiers.None))
                {
                    e.target = target;
                    target.SendEvent(e);
                }
            }
            else
            {
                var mouse = new Event { type = EventType.MouseDown, mousePosition = center, button = 0 };
                using (var e = PointerDownEvent.GetPooled(mouse))
                {
                    e.target = target;
                    target.SendEvent(e);
                }
            }
        }

        public static void PointerUp(VisualElement target, bool touch)
        {
            var center = target.worldBound.center;
            if (touch)
            {
                var finger = new Touch { fingerId = 0, position = center, phase = TouchPhase.Ended };
                using (var e = PointerUpEvent.GetPooled(finger, EventModifiers.None))
                {
                    e.target = target;
                    target.SendEvent(e);
                }
            }
            else
            {
                var mouse = new Event { type = EventType.MouseUp, mousePosition = center, button = 0 };
                using (var e = PointerUpEvent.GetPooled(mouse))
                {
                    e.target = target;
                    target.SendEvent(e);
                }
            }
        }

        public static void PointerMove(VisualElement target, bool touch)
        {
            var center = target.worldBound.center;
            if (touch)
            {
                var finger = new Touch { fingerId = 0, position = center, phase = TouchPhase.Moved };
                using (var e = PointerMoveEvent.GetPooled(finger, EventModifiers.None))
                {
                    e.target = target;
                    target.SendEvent(e);
                }
            }
            else
            {
                var mouse = new Event { type = EventType.MouseMove, mousePosition = center };
                using (var e = PointerMoveEvent.GetPooled(mouse))
                {
                    e.target = target;
                    target.SendEvent(e);
                }
            }
        }
    }
}
