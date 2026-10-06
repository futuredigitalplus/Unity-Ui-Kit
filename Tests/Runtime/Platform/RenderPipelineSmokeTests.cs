using System.Collections;
using FutureDigitalPlus.UIKit.DesignSystem.Theme.Applier;
using FutureDigitalPlus.UIKit.DesignSystem.Theme.Data;
using FutureDigitalPlus.UIKit.Extras;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UIElements;

namespace FutureDigitalPlus.UIKit.PlatformSmoke.Tests
{
    /// <summary>
    /// Proves on the project's own render pipeline that the kit paints. The kit has no camera, pass or pipeline specific shader (UI Toolkit draws the
    /// panel itself), so the same tests must pass in the Built-in pipeline, URP 3D and 2D, and HDRP, in Gamma and Linear. A game that adds the package to
    /// <c>testables</c> re-proves this on its own pipeline. The facade is deliberately not used: only the design system.
    /// </summary>
    public class RenderPipelineSmokeTests
    {
        private const string BaseSheet = "FutureDigitalPlus/UIKit/DesignSystem/Styles/DesignSystem";
        private const string ThemeFolder = "FutureDigitalPlus/UIKit/DesignSystem/Themes/";
        private const string FxShader = "FutureDigitalPlus/UIKit/DesignSystem/Shaders/DsFxBlueprint";

        [SetUp]
        public void SkipWithoutGraphics()
        {
            if (PlatformTestUtil.NoGraphics) Assert.Ignore("No graphics device (-nographics).");
        }

        [Test]
        public void Environment_IsLoggedForTheMatrix()
        {
            Debug.Log($"FDPMATRIX env pipeline={PlatformTestUtil.Pipeline()} renderer={PlatformTestUtil.Renderer()} device={SystemInfo.graphicsDeviceType} " +
                      $"colorSpace={QualitySettings.activeColorSpace} unity={Application.unityVersion} platform={Application.platform} " +
                      $"isMobile={Application.isMobilePlatform} isConsole={Application.isConsolePlatform} dpi={Screen.dpi} safeArea={Screen.safeArea} cutouts={Screen.cutouts.Length}");
            Assert.Pass();
        }

        [Test]
        public void FxShader_IsInTheBuildAndSupported()
        {
            // The shader is drawn by UI Toolkit's renderer, never by a camera pass, so it is pipeline independent; this proves it compiles for the device.
            var shader = Resources.Load<Shader>(FxShader);
            Assert.IsNotNull(shader, "the shader is in a Resources folder of the package");
            Debug.Log($"FDPMATRIX fxshader pipeline={PlatformTestUtil.Pipeline()} supported={shader.isSupported} passes={shader.passCount}");
            Assert.IsTrue(shader.isSupported);

            var material = new Material(shader);
            Assert.IsNotNull(material);
            Object.Destroy(material);
        }

        [Test]
        public void BlurShader_IsInTheBuildAndSupported()
        {
            var shader = Resources.Load<Shader>(BlurSource.ShaderResource);
            Assert.IsNotNull(shader, "the shader is in a Resources folder of the package");
            Debug.Log($"FDPMATRIX blurshader pipeline={PlatformTestUtil.Pipeline()} supported={shader.isSupported}");
            Assert.IsTrue(shader.isSupported);
        }

        [UnityTest]
        public IEnumerator ThemedPanel_PaintsTheThemeColours([Values(false, true)] bool withCamera, [Values("Dark", "Light")] string theme)
        {
            GameObject cameraObject = null;
            if (withCamera)
            {
                // A camera-less menu scene is a common setup; with a camera the panel must still paint over it.
                cameraObject = new GameObject("SmokeCamera");
                var c = cameraObject.AddComponent<Camera>();
                c.clearFlags = CameraClearFlags.SolidColor;
                c.backgroundColor = Color.cyan;
            }

            var target = new RenderTexture(256, 256, 0, RenderTextureFormat.ARGB32);
            var settings = ScriptableObject.CreateInstance<PanelSettings>();
            settings.scaleMode = PanelScaleMode.ConstantPixelSize;
            settings.scale = 1f;
            settings.targetTexture = target;
            settings.clearColor = true;
            settings.colorClearValue = Color.magenta; // an unpainted pixel is magenta, never a theme colour

            LogAssert.ignoreFailingMessages = true; // a PanelSettings without a runtime theme asset may log
            var host = new GameObject("SmokePanel");
            host.SetActive(false);
            var document = host.AddComponent<UIDocument>();
            document.panelSettings = settings;
            host.SetActive(true);

            try
            {
                yield return null;
                yield return null;

                var root = document.rootVisualElement;
                root.style.position = Position.Absolute;
                root.style.left = 0; root.style.top = 0; root.style.right = 0; root.style.bottom = 0;

                var baseSheet = Resources.Load<StyleSheet>(BaseSheet);
                var data = Resources.Load<ThemeData>(ThemeFolder + theme);
                Assert.IsNotNull(baseSheet, "base sheet");
                Assert.IsNotNull(data, "theme " + theme);

                // The base tokens go on the panel root, the theme on the document root below it (a class-scoped theme only wins from a descendant).
                (root.parent ?? root).styleSheets.Add(baseSheet);
                Assert.IsTrue(ThemeRuntime.Apply(root, data), "the theme has a baked stylesheet");

                var screen = new VisualElement();
                screen.AddToClassList("ds-root");
                screen.style.position = Position.Absolute;
                screen.style.left = 0; screen.style.top = 0; screen.style.right = 0; screen.style.bottom = 0;
                var button = new Button { text = "Hi" };
                button.AddToClassList("ds-btn");
                button.AddToClassList("ds-btn--primary");
                button.style.position = Position.Absolute;
                button.style.left = 20; button.style.top = 20; button.style.width = 120; button.style.height = 48;
                screen.Add(button);
                root.Add(screen);

                for (int i = 0; i < 6; i++) yield return null;
                yield return new WaitForEndOfFrame();

                Color expected = button.resolvedStyle.backgroundColor;
                Rect b = button.worldBound;
                // 4 px inside the left edge, on the vertical middle: clear of the text and of the rounded corners. The texture origin is bottom left.
                Color sampled = PlatformTestUtil.ReadPixel(target, Mathf.RoundToInt(b.xMin) + 4, target.height - Mathf.RoundToInt(b.center.y));

                Debug.Log($"FDPMATRIX paint pipeline={PlatformTestUtil.Pipeline()} renderer={PlatformTestUtil.Renderer()} camera={withCamera} theme={theme} " +
                          $"expected={expected} sampled={sampled} buttonBounds={b} panelScale={root.panel.scaledPixelsPerPoint}");

                Assert.Greater(expected.a, 0.9f, "the primary button has an opaque background colour from the theme (is the theme applied?)");
                Assert.IsFalse(Near(sampled, Color.magenta, 0.05f), "something was drawn there (magenta is the clear colour)");
                Assert.IsTrue(Near(sampled, expected, 0.1f), $"the pixel {sampled} is the theme's primary colour {expected}");
            }
            finally
            {
                LogAssert.ignoreFailingMessages = false;
                Object.Destroy(host);
                Object.Destroy(settings);
                target.Release();
                Object.Destroy(target);
                if (cameraObject != null) Object.Destroy(cameraObject);
            }
        }

        private static bool Near(Color a, Color b, float tolerance) =>
            Mathf.Abs(a.r - b.r) <= tolerance && Mathf.Abs(a.g - b.g) <= tolerance && Mathf.Abs(a.b - b.b) <= tolerance;
    }
}
