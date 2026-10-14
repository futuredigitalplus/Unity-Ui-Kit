using System.IO;
using UnityEditor;
using UnityEngine;

namespace FutureDigitalPlus.UIKit.Foundation.Responsive
{
    /// <summary>Finds and creates the project's <see cref="ResponsiveRulesAsset"/>, and records edits to it with undo.</summary>
    public static class ResponsiveRulesStore
    {
        /// <summary>Where <see cref="Create"/> puts a new asset: a Resources folder, so the game loads it with <c>Resources.Load&lt;ResponsiveRulesAsset&gt;("ResponsiveRules")</c>.</summary>
        public const string DefaultAssetPath = "Assets/Resources/ResponsiveRules.asset";

        /// <summary>The project's rules asset (the one at <see cref="DefaultAssetPath"/> when there are several), or null.</summary>
        public static ResponsiveRulesAsset Find()
        {
            var atDefault = AssetDatabase.LoadAssetAtPath<ResponsiveRulesAsset>(DefaultAssetPath);
            if (atDefault != null)
                return atDefault;
            string[] guids = AssetDatabase.FindAssets("t:" + nameof(ResponsiveRulesAsset));
            foreach (string guid in guids)
            {
                var asset = AssetDatabase.LoadAssetAtPath<ResponsiveRulesAsset>(AssetDatabase.GUIDToAssetPath(guid));
                if (asset != null)
                    return asset;
            }
            return null;
        }

        /// <summary>Creates an empty rules asset at <paramref name="assetPath"/> (default <see cref="DefaultAssetPath"/>), or returns the one that is there.</summary>
        public static ResponsiveRulesAsset Create(string assetPath = DefaultAssetPath)
        {
            var existing = AssetDatabase.LoadAssetAtPath<ResponsiveRulesAsset>(assetPath);
            if (existing != null)
                return existing;
            string folder = Path.GetDirectoryName(assetPath)?.Replace('\\', '/');
            if (!string.IsNullOrEmpty(folder) && !AssetDatabase.IsValidFolder(folder))
            {
                string parent = Path.GetDirectoryName(folder)?.Replace('\\', '/');
                AssetDatabase.CreateFolder(parent, Path.GetFileName(folder));
            }
            var asset = ScriptableObject.CreateInstance<ResponsiveRulesAsset>();
            AssetDatabase.CreateAsset(asset, assetPath);
            AssetDatabase.SaveAssets();
            return asset;
        }

        /// <summary>Stores <paramref name="rule"/> in <paramref name="asset"/> with undo.</summary>
        public static void Set(ResponsiveRulesAsset asset, ResponsiveRule rule)
        {
            if (asset == null)
                return;
            Undo.RecordObject(asset, "Responsive rule");
            asset.Rules.Set(rule);
            EditorUtility.SetDirty(asset);
        }

        /// <summary>Removes the rule of <paramref name="form"/> and <paramref name="orientation"/> from <paramref name="asset"/> with undo.</summary>
        public static void Clear(ResponsiveRulesAsset asset, DeviceFormFactor form, ResponsiveOrientation orientation)
        {
            if (asset == null)
                return;
            Undo.RecordObject(asset, "Responsive rule");
            asset.Rules.Clear(form, orientation);
            EditorUtility.SetDirty(asset);
        }
    }
}
