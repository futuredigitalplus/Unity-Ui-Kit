using System.IO;
using UnityEditor;
using UnityEditor.Build.Player;
using UnityEngine;

namespace FdpMatrix
{
    /// <summary>Compiles the project's runtime assemblies (the package included, no Editor code, no tests) for iOS, Android, macOS, Windows and Linux standalone and WebGL; a target whose platform module is not installed is reported as skipped. Run with <c>-executeMethod FdpMatrix.PlayerCompileCheck.Run</c>.</summary>
    public static class PlayerCompileCheck
    {
        /// <summary>Exits the editor with 0 when every installed target compiles, 1 otherwise. One <c>PLAYERCOMPILE</c> log line per target.</summary>
        public static void Run()
        {
            var targets = new[]
            {
                (BuildTargetGroup.iOS, BuildTarget.iOS),
                (BuildTargetGroup.Android, BuildTarget.Android),
                (BuildTargetGroup.Standalone, BuildTarget.StandaloneOSX),
                (BuildTargetGroup.Standalone, BuildTarget.StandaloneWindows64),
                (BuildTargetGroup.Standalone, BuildTarget.StandaloneLinux64),
                (BuildTargetGroup.WebGL, BuildTarget.WebGL),
            };

            bool failed = false;
            foreach (var (group, target) in targets)
            {
                if (!BuildPipeline.IsBuildTargetSupported(group, target))
                {
                    Debug.Log($"PLAYERCOMPILE {target} skipped: the platform module is not installed");
                    continue;
                }

                try
                {
                    var settings = new ScriptCompilationSettings { group = group, target = target, options = ScriptCompilationOptions.None };
                    var result = PlayerBuildInterface.CompilePlayerScripts(settings, Path.Combine(Path.GetTempPath(), "fdp_matrix_pcc_" + target));
                    Debug.Log($"PLAYERCOMPILE {target}: assemblies={result.assemblies.Count}");
                    if (result.assemblies == null || result.assemblies.Count == 0) failed = true;
                }
                catch (System.Exception e)
                {
                    Debug.LogError($"PLAYERCOMPILE {target} threw: {e}");
                    failed = true;
                }
            }

            EditorApplication.Exit(failed ? 1 : 0);
        }
    }
}
