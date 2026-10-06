using System;
using System.Collections;
using FutureDigitalPlus.UIKit.Extras;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace FutureDigitalPlus.UIKit.PlatformSmoke.Tests
{
    /// <summary>
    /// <c>BlurSource</c> is a Kawase blur run with <c>Graphics.Blit</c>, so it must produce the same result in the Built-in pipeline, URP and HDRP:
    /// run this suite in each variant (<c>Tools~/Matrix/run-matrix.sh</c>).
    /// </summary>
    public class BlurSourceTests
    {
        private const int N = 64;

        private Texture2D _step;
        private RenderTexture _result;

        [SetUp]
        public void SetUp()
        {
            // A hard vertical edge: white left half, black right half.
            _step = new Texture2D(N, N, TextureFormat.RGBA32, false) { filterMode = FilterMode.Bilinear, wrapMode = TextureWrapMode.Clamp };
            var pixels = new Color32[N * N];
            for (int y = 0; y < N; y++)
                for (int x = 0; x < N; x++)
                    pixels[y * N + x] = x < N / 2 ? new Color32(255, 255, 255, 255) : new Color32(0, 0, 0, 255);
            _step.SetPixels32(pixels);
            _step.Apply();
        }

        [TearDown]
        public void TearDown()
        {
            if (_result != null) { _result.Release(); UnityEngine.Object.Destroy(_result); }
            if (_step != null) UnityEngine.Object.Destroy(_step);
        }

        private static void RequireGraphics()
        {
            if (PlatformTestUtil.NoGraphics) Assert.Ignore("No graphics device (-nographics).");
            Assert.IsTrue(BlurSource.IsSupported, "the Kawase shader is in the package and supported by " + SystemInfo.graphicsDeviceType);
        }

        [TestCase(100, 1, 100)]
        [TestCase(100, 2, 50)]
        [TestCase(100, 3, 33)]
        [TestCase(3, 16, 1)]
        [TestCase(100, 0, 100)]
        [TestCase(100, -4, 100)]
        [TestCase(1600, 1000, 100)]
        public void TargetSize_DividesAndClamps(int source, int downsample, int expected) =>
            Assert.AreEqual(expected, BlurSource.TargetSize(source, downsample));

        [TestCase(-5, 1)]
        [TestCase(0, 1)]
        [TestCase(4, 4)]
        [TestCase(500, BlurSource.MaxIterations)]
        public void ClampIterations_StaysInRange(int iterations, int expected) =>
            Assert.AreEqual(expected, BlurSource.ClampIterations(iterations));

        [Test]
        public void NullArguments_Throw()
        {
            Assert.Throws<ArgumentNullException>(() => BlurSource.Blur(null));
            Assert.Throws<ArgumentNullException>(() => BlurSource.FromCamera(null));
            Assert.Throws<ArgumentNullException>(() => BlurSource.Blur(_step, null));
        }

        [Test]
        public void Blur_SourceAndDestinationMustDiffer()
        {
            var rt = new RenderTexture(8, 8, 0);
            try { Assert.Throws<ArgumentException>(() => BlurSource.Blur(rt, rt)); }
            finally { rt.Release(); UnityEngine.Object.Destroy(rt); }
        }

        [Test]
        public void Blur_ProducesATextureOfTheDownsampledSize()
        {
            RequireGraphics();
            _result = BlurSource.Blur(_step, 2, 2);

            Assert.IsNotNull(_result);
            Assert.AreEqual(N / 2, _result.width);
            Assert.AreEqual(N / 2, _result.height);
        }

        [Test]
        public void Blur_SoftensAHardEdge_AndKeepsFarPixels()
        {
            RequireGraphics();
            _result = BlurSource.Blur(_step, 1, 5);
            Assert.IsNotNull(_result);

            float farLeft = PlatformTestUtil.ReadPixel(_result, 2, N / 2).r;
            float farRight = PlatformTestUtil.ReadPixel(_result, N - 3, N / 2).r;
            float edge = PlatformTestUtil.ReadPixel(_result, N / 2, N / 2).r;
            float nearEdge = PlatformTestUtil.ReadPixel(_result, N / 2 - 3, N / 2).r;
            Debug.Log($"FDPMATRIX blur pipeline={PlatformTestUtil.Pipeline()} farLeft={farLeft:F2} nearEdge={nearEdge:F2} edge={edge:F2} farRight={farRight:F2}");

            Assert.Greater(farLeft, 0.95f, "far from the edge stays white");
            Assert.Less(farRight, 0.05f, "far from the edge stays black");
            Assert.That(edge, Is.InRange(0.2f, 0.8f), "the edge pixel is a blend");
            Assert.Less(nearEdge, 0.99f, "the blur reaches a few texels into the white side");
        }

        [Test]
        public void Blur_IntoAnExistingTexture_WritesThatTexture()
        {
            RequireGraphics();
            _result = new RenderTexture(N, N, 0, RenderTextureFormat.ARGB32);

            Assert.IsTrue(BlurSource.Blur(_step, _result, 1), "one pass writes straight into the destination");
            float edge = PlatformTestUtil.ReadPixel(_result, N / 2, N / 2).r;
            Assert.That(edge, Is.InRange(0.2f, 0.8f));

            Assert.IsTrue(BlurSource.Blur(_step, _result, 4), "an even or odd pass count ends in the destination too");
            Assert.That(PlatformTestUtil.ReadPixel(_result, N / 2, N / 2).r, Is.InRange(0.2f, 0.8f));
            Assert.Greater(PlatformTestUtil.ReadPixel(_result, 2, N / 2).r, 0.95f);
        }

        [UnityTest]
        public IEnumerator FromCamera_BlursWhatTheCameraSees_AndRestoresTheCamera()
        {
            RequireGraphics();

            var go = new GameObject("BlurCamera");
            var cam = go.AddComponent<Camera>();
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(1f, 0f, 0f, 1f);
            cam.enabled = false;
            yield return null;

            try
            {
                if (cam.pixelWidth < 1 || cam.pixelHeight < 1) Assert.Ignore("The camera has no pixel size here (no Game view).");

                _result = BlurSource.FromCamera(cam, 4, 3);

                Assert.IsNotNull(_result);
                Assert.IsNull(cam.targetTexture, "the camera's own target texture is restored");
                Assert.AreEqual(Mathf.Max(1, cam.pixelWidth / 4), _result.width);

                Color c = PlatformTestUtil.ReadPixel(_result, _result.width / 2, _result.height / 2);
                Debug.Log($"FDPMATRIX blurcamera pipeline={PlatformTestUtil.Pipeline()} center={c}");
                // HDRP exposes and tone maps the camera, so a clear colour of 1 comes out lower there (0.57 measured); in the other pipelines it stays 1.
                Assert.Greater(c.r, 0.4f, "a uniform red scene stays red");
                Assert.Less(c.g, 0.1f);
            }
            finally
            {
                UnityEngine.Object.Destroy(go);
            }
        }
    }
}
