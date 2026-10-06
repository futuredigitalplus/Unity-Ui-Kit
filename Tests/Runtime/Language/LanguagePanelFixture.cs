using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UIElements;

namespace FutureDigitalPlus.UIKit.Language.Tests
{
    /// <summary>
    /// Base class of the Language PlayMode tests: a UIDocument on a RenderTexture panel with the design-system base sheet on the
    /// panel root (<see cref="Root.parent"/>) and the right-to-left sheet after it, the way <see cref="LanguageStyles.Install"/> is meant to be used.
    /// </summary>
    public abstract class LanguagePanelFixture
    {
        protected const string BaseSheet = "FutureDigitalPlus/UIKit/DesignSystem/Styles/DesignSystem";

        private GameObject _host;
        private PanelSettings _settings;
        private RenderTexture _target;

        /// <summary>The document root.</summary>
        protected VisualElement Root { get; private set; }

        /// <summary>The document component.</summary>
        protected UIDocument Document { get; private set; }

        [TearDown]
        public void TearDownPanel()
        {
            LogAssert.ignoreFailingMessages = false;
            if (_host != null) Object.Destroy(_host);
            if (_settings != null) Object.Destroy(_settings);
            if (_target != null)
            {
                _target.Release();
                Object.Destroy(_target);
            }
        }

        /// <summary>Creates the panel and yields until its root exists.</summary>
        /// <param name="installRtl">Whether <see cref="LanguageStyles.Install"/> runs.</param>
        protected IEnumerator CreatePanel(bool installRtl = true)
        {
            if (!Application.isPlaying) Assert.Ignore("Needs a runtime panel (Play Mode).");

            LogAssert.ignoreFailingMessages = true;   // a bare PanelSettings may log about its missing runtime theme
            _target = new RenderTexture(640, 480, 0);
            _settings = ScriptableObject.CreateInstance<PanelSettings>();
            _settings.themeStyleSheet = Resources.Load<ThemeStyleSheet>("FutureDigitalPlus/UIKit/Tests/DefaultRuntimeTheme");   // like any game's PanelSettings
            _settings.scaleMode = PanelScaleMode.ConstantPixelSize;
            _settings.targetTexture = _target;

            _host = new GameObject("LanguageTests");
            _host.SetActive(false);
            Document = _host.AddComponent<UIDocument>();
            Document.panelSettings = _settings;
            _host.SetActive(true);

            yield return null;
            yield return null;

            Root = Document.rootVisualElement;
            Assert.IsNotNull(Root);
            Root.parent.styleSheets.Add(Resources.Load<StyleSheet>(BaseSheet));
            if (installRtl) Assert.IsTrue(LanguageStyles.Install(Root.parent), "Rtl.uss loads from Resources");
        }

        /// <summary>Waits for style resolution and layout.</summary>
        protected static IEnumerator Settle()
        {
            yield return null;
            yield return null;
            yield return new WaitForSeconds(0.3f);
        }
    }
}
