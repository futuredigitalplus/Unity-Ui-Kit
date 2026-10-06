using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UIElements;

namespace FdpMatrix
{
    /// <summary>
    /// Exports an IL2CPP player project (no Xcode or Gradle compile) with Managed Stripping High to prove that shaders, stripping and the IL2CPP conversion
    /// work with the package. The scene's only content is a UIDocument whose UXML names kit elements, so they are reachable only through UXML.
    /// Run with <c>-buildTarget iOS|Android -executeMethod FdpMatrix.PlayerExport.Ios|Android</c>. The package is never referenced by type here.
    /// </summary>
    public static class PlayerExport
    {
        private const string Uxml =
            "<ui:UXML xmlns:ui=\"UnityEngine.UIElements\" xmlns:ex=\"FutureDigitalPlus.UIKit.Extras\" xmlns:an=\"FutureDigitalPlus.UIKit.Animation\">" +
            "<ex:BlurPanel/><ex:Tooltip/><an:AnimatedFoldout text=\"x\"/><an:MaskedInputField/></ui:UXML>";

        /// <summary>iOS export into <c>Build/ios</c>.</summary>
        public static void Ios() => Export(BuildTarget.iOS, NamedBuildTarget.iOS, "Build/ios");

        /// <summary>Android Gradle project export into <c>Build/android</c> (ARM64).</summary>
        public static void Android() => Export(BuildTarget.Android, NamedBuildTarget.Android, "Build/android");

        private static void Export(BuildTarget target, NamedBuildTarget named, string path)
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            File.WriteAllText("Assets/FdpMatrixUxmlOnly.uxml", Uxml);
            AssetDatabase.ImportAsset("Assets/FdpMatrixUxmlOnly.uxml");

            var go = new GameObject("ui");
            var document = go.AddComponent<UIDocument>();
            document.visualTreeAsset = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>("Assets/FdpMatrixUxmlOnly.uxml");
            EditorSceneManager.SaveScene(scene, "Assets/FdpMatrixExport.unity");

            PlayerSettings.SetScriptingBackend(named, ScriptingImplementation.IL2CPP);
            PlayerSettings.SetManagedStrippingLevel(named, ManagedStrippingLevel.High);
            PlayerSettings.SetApplicationIdentifier(named, "com.fdp.matrix");
            if (target == BuildTarget.Android)
            {
                EditorUserBuildSettings.exportAsGoogleAndroidProject = true;
                PlayerSettings.Android.targetArchitectures = AndroidArchitecture.ARM64;
            }

            Directory.CreateDirectory(path);
            var report = BuildPipeline.BuildPlayer(new BuildPlayerOptions
            {
                scenes = new[] { "Assets/FdpMatrixExport.unity" },
                locationPathName = path,
                target = target,
                options = BuildOptions.None
            });

            Debug.Log($"BUILDRESULT {target} result={report.summary.result} errors={report.summary.totalErrors} warnings={report.summary.totalWarnings} totalSize={report.summary.totalSize}");
            foreach (var packed in report.packedAssets)
                foreach (var content in packed.contents.Where(c => c.sourceAssetPath.Contains("futuredigitalplus") && c.sourceAssetPath.EndsWith(".shader")))
                    Debug.Log($"BUILDASSET {content.sourceAssetPath} size={content.packedSize}");
            foreach (var step in report.steps)
                foreach (var message in step.messages.Where(m => m.type == LogType.Error || m.type == LogType.Warning).Take(30))
                    Debug.Log($"BUILDMSG {message.type} {message.content}");

            EditorApplication.Exit(report.summary.result == BuildResult.Succeeded ? 0 : 1);
        }
    }
}
