using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UIElements;

namespace FutureDigitalPlus.UIKit.Foundation.Tests
{
    /// <summary>
    /// Base of the PlayMode tests: a render-texture panel, a shell with three probe screens (Home, Settings, About) and teardown. PlayMode-only behaviour is
    /// reported as ignored when the tests run in EditMode.
    /// </summary>
    public abstract class PlayModeBase
    {
        protected FoundationPanel Panel;
        protected UiShell Shell;
        protected List<string> Log;
        protected MemoryStateStore Store;

        [SetUp]
        public void BaseSetUp()
        {
            Log = new List<string>();
            Store = new MemoryStateStore();
        }

        [TearDown]
        public void BaseTearDown()
        {
            if (Shell != null)
                Shell.Dispose();
            Shell = null;
            if (Panel != null)
                Panel.Dispose();
            Panel = null;
            ScreenCatalog.Default.Clear();
            TestEnvironment.ResetStatics();
        }

        /// <summary>Creates the render-texture panel and waits for the first layout. Unity warns about a PanelSettings without a runtime theme, so failing messages are ignored only while it is created.</summary>
        protected IEnumerator CreatePanel(int width = 800, int height = 600, float scale = 1f)
        {
            if (!Application.isPlaying)
                Assert.Ignore("Needs a runtime panel (Play Mode).");
            LogAssert.ignoreFailingMessages = true;
            Panel = FoundationPanel.Create(width, height, scale);
            yield return FoundationPanel.Frames(2);
            LogAssert.ignoreFailingMessages = false;
            Assert.IsNotNull(Panel.Root);
        }

        /// <summary>Creates a shell on the panel's root with the probe screens registered. Transitions are off unless the test is about them.</summary>
        protected UiShell CreateShell(UiShellOptions options = null, bool designSystem = true)
        {
            UiShellOptions settings = options ?? new UiShellOptions();
            if (options == null)
                settings.TransitionsEnabled = false;
            if (settings.StateStore == null)
                settings.StateStore = Store;
            if (designSystem)
                FoundationStyles.InstallDesignSystem(Panel.Root.parent ?? Panel.Root);
            Shell = new UiShell(Panel.Root, settings);
            Shell.Catalog.Register("Home", () => new ProbeScreen("Home", Log));
            Shell.Catalog.Register("Settings", () => new ProbeScreen("Settings", Log));
            Shell.Catalog.Register("About", () => new ProbeScreen("About", Log));
            return Shell;
        }

        protected static IEnumerator Real(float seconds)
        {
            yield return new WaitForSecondsRealtime(seconds);
        }

        protected VisualElement Layer(UiLayer layer)
        {
            return Shell.Context.GetLayer(layer);
        }

        protected static float Millis(TimeValue time)
        {
            return time.unit == TimeUnit.Millisecond ? time.value : time.value * 1000f;
        }
    }
}
