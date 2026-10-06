using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.HighDefinition;

namespace FdpMatrix
{
    /// <summary>Creates a default HDRP asset and makes it the project's render pipeline. Run with <c>-executeMethod FdpMatrix.HdrpSetup.Use</c>; the editor exits afterwards.</summary>
    public static class HdrpSetup
    {
        /// <summary>A bare <c>HDRenderPipelineAsset</c> is enough for UI Toolkit painting.</summary>
        public static void Use()
        {
            Directory.CreateDirectory("Assets/FdpMatrix/Pipelines");
            var asset = ScriptableObject.CreateInstance<HDRenderPipelineAsset>();
            AssetDatabase.CreateAsset(asset, "Assets/FdpMatrix/Pipelines/Hdrp.asset");
            AssetDatabase.SaveAssets();

            GraphicsSettings.defaultRenderPipeline = asset;
            for (int i = 0; i < QualitySettings.names.Length; i++)
            {
                QualitySettings.SetQualityLevel(i);
                QualitySettings.renderPipeline = asset;
            }

            AssetDatabase.SaveAssets();
            Debug.Log("FDPMATRIXSETUP hdrp pipeline=" + GraphicsSettings.defaultRenderPipeline);
            EditorApplication.Exit(0);
        }
    }
}
