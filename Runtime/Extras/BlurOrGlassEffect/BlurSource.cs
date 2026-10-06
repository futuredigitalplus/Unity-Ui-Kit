using System;
using UnityEngine;

namespace FutureDigitalPlus.UIKit.Extras
{
    /// <summary>
    /// A render pipeline independent source of blurred textures for <see cref="BlurPanel"/>. It runs a Kawase blur
    /// (<c>Hidden/FutureDigitalPlus/UIKit/Blur/Kawase</c>) with <c>Graphics.Blit</c> between two render textures, so it needs no
    /// renderer feature and works in the Built-in pipeline, URP and HDRP. It blurs once, when you call it: capture a frame when
    /// a menu opens, hand the result to <see cref="BlurPanel.SetImage"/>, and release it when the menu closes.
    /// Call from the main thread, outside a render callback. Not for every frame on phones (see the Blur guide).
    /// </summary>
    public static class BlurSource
    {
        /// <summary>Resources path of the blur shader.</summary>
        public const string ShaderResource = "FutureDigitalPlus/UIKit/Extras/Shaders/Kawase";

        /// <summary>Largest accepted size divisor.</summary>
        public const int MaxDownsample = 16;

        /// <summary>Largest accepted pass count.</summary>
        public const int MaxIterations = 12;

        private static Material _material;
        private static readonly int OffsetId = Shader.PropertyToID("_Offset");

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics() => _material = null;

        /// <summary>True when the blur shader is in the build and the current graphics device can run it.</summary>
        public static bool IsSupported
        {
            get
            {
                var shader = Resources.Load<Shader>(ShaderResource);
                return shader != null && shader.isSupported;
            }
        }

        /// <summary>The size of a blurred texture: <paramref name="sourceSize"/> divided by <paramref name="downsample"/>, never below 1.</summary>
        /// <param name="sourceSize">Width or height of the source in pixels.</param>
        /// <param name="downsample">Size divisor, clamped to 1..<see cref="MaxDownsample"/>.</param>
        public static int TargetSize(int sourceSize, int downsample) =>
            Math.Max(1, sourceSize / Math.Min(MaxDownsample, Math.Max(1, downsample)));

        /// <summary>Clamps a pass count to 1..<see cref="MaxIterations"/>. More passes blur wider and cost one full pass of the (reduced) texture each.</summary>
        public static int ClampIterations(int iterations) => Math.Min(MaxIterations, Math.Max(1, iterations));

        /// <summary>
        /// Blurs <paramref name="source"/> into a new render texture of <c>source size / downsample</c>. The caller owns the
        /// result: call <c>Release()</c> and destroy it when done.
        /// </summary>
        /// <param name="source">The texture to blur. It is not modified.</param>
        /// <param name="downsample">Size divisor of the result (1 keeps the size; 2 to 4 is typical and cheaper). Clamped to 1..<see cref="MaxDownsample"/>.</param>
        /// <param name="iterations">Number of Kawase passes, clamped to 1..<see cref="MaxIterations"/>. About 4 gives a soft glass look.</param>
        /// <returns>The blurred texture, or null (with a warning) when the shader is missing or unsupported.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="source"/> is null.</exception>
        public static RenderTexture Blur(Texture source, int downsample = 2, int iterations = 4)
        {
            if (source == null) throw new ArgumentNullException(nameof(source));

            var result = new RenderTexture(TargetSize(source.width, downsample), TargetSize(source.height, downsample), 0, RenderTextureFormat.ARGB32)
            {
                filterMode = FilterMode.Bilinear,
                wrapMode = TextureWrapMode.Clamp,
                name = "FdpBlur"
            };

            if (!Blur(source, result, iterations))
            {
                result.Release();
                UnityEngine.Object.Destroy(result);
                return null;
            }

            return result;
        }

        /// <summary>
        /// Blurs <paramref name="source"/> into an existing render texture, at that texture's size. Use it to refresh a
        /// texture a <see cref="BlurPanel"/> already shows.
        /// </summary>
        /// <param name="source">The texture to blur. It is not modified.</param>
        /// <param name="destination">The render texture that receives the result; it must not be <paramref name="source"/>.</param>
        /// <param name="iterations">Number of Kawase passes, clamped to 1..<see cref="MaxIterations"/>.</param>
        /// <returns>False (with a warning) when the shader is missing or unsupported; <paramref name="destination"/> is then untouched.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="source"/> or <paramref name="destination"/> is null.</exception>
        /// <exception cref="ArgumentException"><paramref name="source"/> and <paramref name="destination"/> are the same texture.</exception>
        public static bool Blur(Texture source, RenderTexture destination, int iterations = 4)
        {
            if (source == null) throw new ArgumentNullException(nameof(source));
            if (destination == null) throw new ArgumentNullException(nameof(destination));
            if (ReferenceEquals(source, destination)) throw new ArgumentException("Source and destination must differ.", nameof(destination));

            var material = GetMaterial();
            if (material == null) return false;

            int passes = ClampIterations(iterations);

            // Pass k writes to the destination when (passes - 1 - k) is even and to a scratch texture otherwise, so the last
            // pass always lands in the destination. With one pass there is no scratch texture.
            RenderTexture scratch = passes > 1
                ? RenderTexture.GetTemporary(destination.width, destination.height, 0, RenderTextureFormat.ARGB32)
                : null;

            try
            {
                if (scratch != null) scratch.filterMode = FilterMode.Bilinear;
                destination.filterMode = FilterMode.Bilinear;

                Texture input = source;
                for (int k = 0; k < passes; k++)
                {
                    RenderTexture output = (passes - 1 - k) % 2 == 0 ? destination : scratch;
                    material.SetFloat(OffsetId, k);
                    Graphics.Blit(input, output, material);
                    input = output;
                }
            }
            finally
            {
                if (scratch != null) RenderTexture.ReleaseTemporary(scratch);
            }

            return true;
        }

        /// <summary>
        /// Renders <paramref name="camera"/> once into a reduced size texture and blurs it. The camera keeps its own
        /// <c>targetTexture</c>. Use it for a "blurred game world behind this menu" capture; the result does not include UI
        /// Toolkit panels. Untested on URP and HDRP at the time of writing (the Built-in, URP and HDRP blur itself is tested).
        /// </summary>
        /// <param name="camera">The camera to capture. It does not have to be enabled.</param>
        /// <param name="downsample">Size divisor, relative to the camera's pixel size (or its target texture size).</param>
        /// <param name="iterations">Number of Kawase passes, clamped to 1..<see cref="MaxIterations"/>.</param>
        /// <returns>The blurred texture the caller owns, or null (with a warning) when the camera has no size or the shader is unsupported.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="camera"/> is null.</exception>
        public static RenderTexture FromCamera(Camera camera, int downsample = 2, int iterations = 4)
        {
            if (camera == null) throw new ArgumentNullException(nameof(camera));

            var previousTarget = camera.targetTexture;
            int width = previousTarget != null ? previousTarget.width : camera.pixelWidth;
            int height = previousTarget != null ? previousTarget.height : camera.pixelHeight;

            if (width < 1 || height < 1)
            {
                Debug.LogWarning("[BlurSource] The camera has no pixel size (headless run?); nothing was blurred.");
                return null;
            }

            var capture = RenderTexture.GetTemporary(TargetSize(width, downsample), TargetSize(height, downsample), 24, RenderTextureFormat.ARGB32);
            try
            {
                camera.targetTexture = capture;
                camera.Render();
            }
            finally
            {
                camera.targetTexture = previousTarget;
            }

            try
            {
                return Blur(capture, 1, iterations);
            }
            finally
            {
                RenderTexture.ReleaseTemporary(capture);
            }
        }

        private static Material GetMaterial()
        {
            if (_material != null) return _material;

            var shader = Resources.Load<Shader>(ShaderResource);
            if (shader == null || !shader.isSupported)
            {
                Debug.LogWarning($"[BlurSource] Shader '{ShaderResource}' is missing or not supported on this device; nothing was blurred.");
                return null;
            }

            _material = new Material(shader) { hideFlags = HideFlags.HideAndDontSave };
            return _material;
        }
    }
}
