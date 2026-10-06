using System.Collections;
using UnityEngine;
using UnityEngine.UIElements;

namespace FutureDigitalPlus.UIKit.Tests
{
    /// <summary>
    /// A UIDocument rendering into a fixed-size RenderTexture, so layout results do not depend on the Game view.
    /// Create with <see cref="Create"/>, call <see cref="Dispose"/> in TearDown.
    /// </summary>
    internal sealed class PanelHarness
    {
        public const int Width = 800;
        public const int Height = 600;

        public GameObject Host;
        public UIDocument Document;
        public PanelSettings Settings;
        public RenderTexture Target;

        public VisualElement Root => Document.rootVisualElement;

        /// <param name="configure">Runs on the inactive host before the UIDocument enables (add components here).</param>
        public static PanelHarness Create(System.Action<GameObject> configure = null)
        {
            var h = new PanelHarness();
            h.Target = new RenderTexture(Width, Height, 0);
            h.Settings = UIKitInstaller.CreatePanelSettings();
            h.Settings.scaleMode = PanelScaleMode.ConstantPixelSize;
            h.Settings.scale = 1f;
            h.Settings.targetTexture = h.Target;

            // Inactive while configured, so UIDocument builds its tree once with everything set.
            h.Host = new GameObject("UIKitTestHost");
            h.Host.SetActive(false);
            h.Document = h.Host.AddComponent<UIDocument>();
            h.Document.panelSettings = h.Settings;
            configure?.Invoke(h.Host);
            h.Host.SetActive(true);
            return h;
        }

        public static IEnumerator Frames(int count = 3)
        {
            for (var i = 0; i < count; i++) yield return null;
        }

        public void Dispose()
        {
            if (Host != null) Object.Destroy(Host);
            if (Settings != null) Object.Destroy(Settings);
            if (Target != null) { Target.Release(); Object.Destroy(Target); }
        }
    }
}
