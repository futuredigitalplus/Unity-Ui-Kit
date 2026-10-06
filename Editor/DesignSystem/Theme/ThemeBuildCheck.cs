using System.Collections.Generic;
using System.Linq;
using FutureDigitalPlus.UIKit.DesignSystem.Theme.Data;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace FutureDigitalPlus.UIKit.DesignSystem.Editor.Theming
{
    /// <summary>The bake state of a theme asset, as far as it can be judged without baking.</summary>
    public enum ThemeBakeState
    {
        /// <summary>A stylesheet exists and the stored hash equals the hash of the values.</summary>
        Ok = 0,

        /// <summary>There is no baked stylesheet, so the theme paints nothing at runtime.</summary>
        NotBaked = 1,

        /// <summary>The values changed after the last bake: the stylesheet in the build would show old colours.</summary>
        Stale = 2,

        /// <summary>A stylesheet exists but no hash was recorded (baked by an older version of this package); re-bake to verify.</summary>
        Unverified = 3,
    }

    /// <summary>
    /// Fails a player build when a theme that ships in it has no baked stylesheet or a stale one, and warns about themes whose
    /// bake cannot be verified. "Ships in it" means: every <see cref="ThemeData"/> under a <c>Resources</c> folder, and every
    /// one an enabled build scene depends on. A runtime-created theme is not an asset and is never checked.
    /// </summary>
    public sealed class ThemeBuildCheck : IPreprocessBuildWithReport
    {
        /// <inheritdoc />
        public int callbackOrder => 0;

        /// <summary>Judges one theme asset (pure; used by the build check, the Theme Configurator and the tests).</summary>
        public static ThemeBakeState Evaluate(ThemeData theme)
        {
            if (!theme || theme.IsRuntime) return ThemeBakeState.Ok;
            if (!theme.StyleSheet) return ThemeBakeState.NotBaked;
            if (string.IsNullOrEmpty(theme.BakedHash)) return ThemeBakeState.Unverified;
            return theme.BakedHash == theme.ComputeBakeHash() ? ThemeBakeState.Ok : ThemeBakeState.Stale;
        }

        /// <summary>The theme assets that end up in a player build (see the type summary).</summary>
        public static List<ThemeData> ThemesInBuild()
        {
            var paths = new HashSet<string>();

            foreach (var guid in AssetDatabase.FindAssets("t:" + nameof(ThemeData)))
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                if (path.Contains("/Resources/")) paths.Add(path);
            }

            var scenes = EditorBuildSettings.scenes.Where(s => s.enabled).Select(s => s.path).ToArray();
            if (scenes.Length > 0)
            {
                foreach (var dependency in AssetDatabase.GetDependencies(scenes, true))
                    if (dependency.EndsWith(".asset") && AssetDatabase.LoadAssetAtPath<ThemeData>(dependency))
                        paths.Add(dependency);
            }

            return paths.Select(AssetDatabase.LoadAssetAtPath<ThemeData>).Where(t => t).ToList();
        }

        /// <inheritdoc />
        public void OnPreprocessBuild(BuildReport report)
        {
            var errors = new List<string>();

            foreach (var theme in ThemesInBuild())
            {
                var path = AssetDatabase.GetAssetPath(theme);
                switch (Evaluate(theme))
                {
                    case ThemeBakeState.NotBaked:
                        errors.Add($"'{path}' has no baked stylesheet");
                        break;
                    case ThemeBakeState.Stale:
                        errors.Add($"'{path}' was edited after its last bake");
                        break;
                    case ThemeBakeState.Unverified:
                        Debug.LogWarning($"[ThemeBuildCheck] '{path}' has a stylesheet but no recorded bake hash, so it cannot be " +
                                         "checked. Run Design System > Rebake All Themes once.", theme);
                        break;
                }
            }

            if (errors.Count > 0)
                throw new BuildFailedException(
                    "Design System themes in this build are not baked: " + string.Join("; ", errors) +
                    ". Open each in the Theme Configurator and press Save, or run Tools > Future Digital Plus UI Kit > Design System > Rebake All Themes.");
        }
    }
}
