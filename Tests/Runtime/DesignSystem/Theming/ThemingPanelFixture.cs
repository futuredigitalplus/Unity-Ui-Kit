using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UIElements;

namespace FutureDigitalPlus.UIKit.Tests.DesignSystemModule
{
    /// <summary>
    /// Base class of the theming PlayMode tests: a UIDocument on a RenderTexture panel with the design-system base sheet on
    /// the panel root (a theme goes on the document root below it, see <c>Runtime/DesignSystem/SOURCE.md</c>).
    /// </summary>
    public abstract class ThemingPanelFixture
    {
        protected const string BaseSheet = "FutureDigitalPlus/UIKit/DesignSystem/Styles/DesignSystem";

        private GameObject _host;
        private PanelSettings _settings;
        private RenderTexture _target;

        /// <summary>The document root: themes are applied here.</summary>
        protected VisualElement Root { get; private set; }

        /// <summary>The document component, for tests that need a second panel root.</summary>
        protected UIDocument Document { get; private set; }

        [TearDown]
        public void TearDownPanel()
        {
            LogAssert.ignoreFailingMessages = false;
            if (_host != null) Object.Destroy(_host);
            if (_settings != null) Object.Destroy(_settings);
            if (_target != null) { _target.Release(); Object.Destroy(_target); }
        }

        /// <summary>Creates the panel; yields until its root exists.</summary>
        protected IEnumerator CreatePanel(bool withBaseSheet = true)
        {
            if (!Application.isPlaying) Assert.Ignore("Needs a runtime panel (Play Mode).");

            LogAssert.ignoreFailingMessages = true;   // a bare PanelSettings may log about its missing runtime theme
            _target = new RenderTexture(640, 480, 0);
            _settings = ScriptableObject.CreateInstance<PanelSettings>();
            _settings.scaleMode = PanelScaleMode.ConstantPixelSize;
            _settings.targetTexture = _target;

            _host = new GameObject("DsThemingPanelTests");
            _host.SetActive(false);
            Document = _host.AddComponent<UIDocument>();
            Document.panelSettings = _settings;
            _host.SetActive(true);

            yield return null;
            yield return null;

            Root = Document.rootVisualElement;
            Assert.IsNotNull(Root);
            if (withBaseSheet) Root.parent.styleSheets.Add(Resources.Load<StyleSheet>(BaseSheet));
        }

        /// <summary>A <c>.ds-root</c> screen under <see cref="Root"/> holding the elements the tests measure.</summary>
        protected Probes AddProbes(VisualElement parent = null)
        {
            var screen = new VisualElement();
            screen.AddToClassList("ds-root");

            var primary = new Button { text = "Go" };
            primary.AddToClassList("ds-btn");
            primary.AddToClassList("ds-btn--primary");
            var danger = new Button { text = "Delete" };
            danger.AddToClassList("ds-btn");
            danger.AddToClassList("ds-btn--danger");
            var h1 = new Label("Title");
            h1.AddToClassList("ds-h1");
            var body = new Label("Body");
            body.AddToClassList("ds-body-1");
            var card = new VisualElement();
            card.AddToClassList("ds-card");

            screen.Add(primary);
            screen.Add(danger);
            screen.Add(h1);
            screen.Add(body);
            screen.Add(card);
            (parent ?? Root).Add(screen);
            return new Probes { Screen = screen, Primary = primary, Danger = danger, H1 = h1, Body = body, Card = card };
        }

        /// <summary>Elements whose resolved style depends on one token each.</summary>
        protected struct Probes
        {
            public VisualElement Screen;
            public Button Primary;
            public Button Danger;
            public Label H1;
            public Label Body;
            public VisualElement Card;
        }

        /// <summary>Waits long enough for transitions (a button fill animates for at most 200 ms) and for a restyle.</summary>
        protected static WaitForSeconds Settle() => new WaitForSeconds(0.45f);

        protected static void AssertColor(Color expected, Color actual, string what, float tolerance = 0.012f)
        {
            Assert.AreEqual(expected.r, actual.r, tolerance, what + " red");
            Assert.AreEqual(expected.g, actual.g, tolerance, what + " green");
            Assert.AreEqual(expected.b, actual.b, tolerance, what + " blue");
        }
    }
}
