using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace FdpMatrix
{
    /// <summary>
    /// Creates a URP asset (3D UniversalRenderer or 2D Renderer2D) and makes it the project's render pipeline. Run with
    /// <c>-executeMethod FdpMatrix.UrpSetup.Use3D</c> or <c>Use2D</c>; the editor exits afterwards.
    /// </summary>
    public static class UrpSetup
    {
        /// <summary>The 3D UniversalRenderer.</summary>
        public static void Use3D() => Setup(false);

        /// <summary>The 2D Renderer2D.</summary>
        public static void Use2D() => Setup(true);

        private static void Setup(bool twoD)
        {
            Directory.CreateDirectory("Assets/FdpMatrix/Pipelines");
            ScriptableRendererData rendererData = twoD
                ? ScriptableObject.CreateInstance<Renderer2DData>()
                : ScriptableObject.CreateInstance<UniversalRendererData>();

            AssetDatabase.CreateAsset(rendererData, "Assets/FdpMatrix/Pipelines/" + (twoD ? "Renderer2D" : "Renderer3D") + ".asset");
            ResourceReloader.ReloadAllNullIn(rendererData, UniversalRenderPipelineAsset.packagePath);
            EditorUtility.SetDirty(rendererData);

            var asset = UniversalRenderPipelineAsset.Create(rendererData);
            AssetDatabase.CreateAsset(asset, "Assets/FdpMatrix/Pipelines/" + (twoD ? "Urp2D" : "Urp3D") + ".asset");
            AssetDatabase.SaveAssets();

            GraphicsSettings.defaultRenderPipeline = asset;
            for (int i = 0; i < QualitySettings.names.Length; i++)
            {
                QualitySettings.SetQualityLevel(i);
                QualitySettings.renderPipeline = asset;
            }

            AssetDatabase.SaveAssets();
            Debug.Log("FDPMATRIXSETUP urp twoD=" + twoD + " pipeline=" + GraphicsSettings.defaultRenderPipeline);
            EditorApplication.Exit(0);
        }
    }
}
