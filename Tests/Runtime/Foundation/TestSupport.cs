using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UIElements;

namespace FutureDigitalPlus.UIKit.Foundation.Tests
{
    /// <summary>In-memory <see cref="IUiStateStore"/> for tests.</summary>
    public sealed class MemoryStateStore : IUiStateStore
    {
        readonly Dictionary<string, string> _values = new Dictionary<string, string>();

        public int Saves { get; private set; }

        public string Load(string key)
        {
            string value;
            return _values.TryGetValue(key, out value) ? value : null;
        }

        public void Save(string key, string value)
        {
            Saves++;
            _values[key] = value;
        }

        public void Delete(string key)
        {
            _values.Remove(key);
        }
    }

    /// <summary>An <see cref="IUiInputModeSource"/> whose mode a test sets.</summary>
    public sealed class FakeInputModeSource : IUiInputModeSource
    {
        UiInputMode _mode;

        public FakeInputModeSource(UiInputMode mode)
        {
            _mode = mode;
        }

        public UiInputMode Mode
        {
            get { return _mode; }
        }

        public event Action<UiInputMode> ModeChanged;

        public void Set(UiInputMode mode)
        {
            _mode = mode;
            Action<UiInputMode> handler = ModeChanged;
            if (handler != null)
                handler(mode);
        }
    }

    /// <summary>An <see cref="IBackKeySource"/> a test presses by hand.</summary>
    public sealed class FakeBackKeySource : IBackKeySource
    {
        public bool Pressed;

        public bool WasPressedThisFrame()
        {
            bool value = Pressed;
            Pressed = false;
            return value;
        }
    }

    /// <summary>A screen that records its lifecycle in a shared log and builds one button, so tests can see the order of calls.</summary>
    public sealed class ProbeScreen : UiScreen
    {
        readonly string _id;
        readonly List<string> _log;
        readonly PickingMode _picking;

        public ProbeScreen(string id, List<string> log, PickingMode picking = PickingMode.Position)
        {
            _id = id;
            _log = log;
            _picking = picking;
        }

        public override PickingMode RootPicking
        {
            get { return _picking; }
        }

        public string State;

        public bool HandlesBack;

        public Button Button { get; private set; }

        public override string UxmlName
        {
            get { return null; }
        }

        public override string SaveState()
        {
            return State;
        }

        public override void RestoreState(string state)
        {
            State = state;
            _log.Add(_id + ":restore:" + state);
        }

        public override bool OnBack()
        {
            _log.Add(_id + ":back");
            return HandlesBack;
        }

        protected override void OnBuild()
        {
            _log.Add(_id + ":build");
            Button = new Button { name = "btn-" + _id, text = _id };
            Root.Add(Button);
            OnClick(Button, () => _log.Add(_id + ":click"));
        }

        protected override void OnShow(object args)
        {
            _log.Add(_id + ":show:" + (args ?? "null"));
        }

        protected override void OnHide()
        {
            _log.Add(_id + ":hide");
        }

        protected override void OnDispose()
        {
            _log.Add(_id + ":dispose");
        }
    }

    /// <summary>
    /// A UIDocument that renders into a fixed-size RenderTexture, so layout does not depend on the Game view. Create it in a [UnitySetUp], dispose it in TearDown.
    /// </summary>
    public sealed class FoundationPanel
    {
        public GameObject Host;
        public UIDocument Document;
        public PanelSettings Settings;
        public RenderTexture Target;
        public int Width;
        public int Height;

        public VisualElement Root
        {
            get { return Document.rootVisualElement; }
        }

        public IPanel Panel
        {
            get { return Root.panel; }
        }

        /// <summary>Creates the panel. Unity logs a warning or error for a PanelSettings without a runtime theme, so the caller ignores failing messages during creation.</summary>
        public static FoundationPanel Create(int width = 800, int height = 600, float scale = 1f)
        {
            FoundationPanel panel = new FoundationPanel { Width = width, Height = height };
            panel.Target = new RenderTexture(width, height, 0);
            panel.Settings = ScriptableObject.CreateInstance<PanelSettings>();
            panel.Settings.name = "FoundationTestPanelSettings";
            // The default runtime theme, like any game's PanelSettings has: it gives the document root its size and text its font.
            panel.Settings.themeStyleSheet = Resources.Load<ThemeStyleSheet>("FutureDigitalPlus/UIKit/Tests/DefaultRuntimeTheme");
            panel.Settings.scaleMode = PanelScaleMode.ConstantPixelSize;
            panel.Settings.scale = scale;
            panel.Settings.targetTexture = panel.Target;
            // The host is inactive while the document is configured, so UIDocument builds its tree once with everything set.
            panel.Host = new GameObject("FoundationTestHost");
            panel.Host.SetActive(false);
            panel.Document = panel.Host.AddComponent<UIDocument>();
            panel.Document.panelSettings = panel.Settings;
            panel.Host.SetActive(true);
            return panel;
        }

        public static IEnumerator Frames(int count = 3)
        {
            for (int i = 0; i < count; i++)
                yield return null;
        }

        /// <summary>A pointer press and release on <paramref name="target"/>, like a mouse click.</summary>
        public void Click(VisualElement target)
        {
            Vector2 center = target.worldBound.center;
            Event down = new Event { type = EventType.MouseDown, mousePosition = center, button = 0 };
            using (PointerDownEvent e = PointerDownEvent.GetPooled(down))
            {
                e.target = target;
                Panel.visualTree.SendEvent(e);
            }
            Event up = new Event { type = EventType.MouseUp, mousePosition = center, button = 0 };
            using (PointerUpEvent e = PointerUpEvent.GetPooled(up))
            {
                e.target = target;
                Panel.visualTree.SendEvent(e);
            }
        }

        /// <summary>A pointer press only (a tap on the scrim of a dialog starts with it).</summary>
        public void PointerDown(VisualElement target)
        {
            Event down = new Event { type = EventType.MouseDown, mousePosition = target.worldBound.center, button = 0 };
            using (PointerDownEvent e = PointerDownEvent.GetPooled(down))
            {
                e.target = target;
                Panel.visualTree.SendEvent(e);
            }
        }

        public void Submit(VisualElement target)
        {
            using (NavigationSubmitEvent e = NavigationSubmitEvent.GetPooled())
            {
                e.target = target;
                Panel.visualTree.SendEvent(e);
            }
        }

        public void Cancel(VisualElement target)
        {
            using (NavigationCancelEvent e = NavigationCancelEvent.GetPooled())
            {
                e.target = target;
                Panel.visualTree.SendEvent(e);
            }
        }

        public void Dispose()
        {
            if (Host != null)
                UnityEngine.Object.Destroy(Host);
            if (Settings != null)
                UnityEngine.Object.Destroy(Settings);
            if (Target != null)
            {
                Target.Release();
                UnityEngine.Object.Destroy(Target);
            }
        }
    }

    /// <summary>Shared setup helpers.</summary>
    public static class TestEnvironment
    {
        /// <summary>Resets the static state a test may have changed (time scale, log filtering).</summary>
        public static void ResetStatics()
        {
            Time.timeScale = 1f;
            LogAssert.ignoreFailingMessages = false;
        }
    }
}
