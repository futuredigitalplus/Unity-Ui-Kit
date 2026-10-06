using FutureDigitalPlus.UIKit.DesignSystem.Editor.Theming;
using FutureDigitalPlus.UIKit.DesignSystem.Theme.Data;
using NUnit.Framework;
using UnityEditor;
using UnityEngine.UIElements;

namespace FutureDigitalPlus.UIKit.Tests.DesignSystemModule
{
    /// <summary>
    /// New smoke tests for the editor side of the module, including the local changes made to
    /// ThemePresets (package-relative theme folder). None of them writes any asset.
    /// </summary>
    public class DesignSystemEditorTests
    {
        // GUIDs the upstream .meta files give these assets; kept stable on purpose.
        private const string DesignSystemUssGuid = "2fe121bab235f2a4f8cbc07737f87fe2";
        private const string ThemePreviewUxmlGuid = "ae1eea539fc84b67aee9a953483d8b42";

        [Test]
        public void ThemePresets_FolderResolvesInsideTheKit_AndHoldsTheBuiltInThemes()
        {
            // in either layout: a UPM package (Packages/<id>) or a plain folder below Assets
            StringAssert.StartsWith(FutureDigitalPlus.UIKit.Location.KitLocation.AssetRoot + "/", ThemePresets.ThemesFolder);
            Assert.AreNotEqual(FutureDigitalPlus.UIKit.Location.KitInstall.NotFound, FutureDigitalPlus.UIKit.Location.KitLocation.Install);
            StringAssert.EndsWith("Runtime/DesignSystem/Resources/FutureDigitalPlus/UIKit/DesignSystem/Themes", ThemePresets.ThemesFolder);

            Assert.IsNotNull(AssetDatabase.LoadAssetAtPath<ThemeData>(ThemePresets.DarkPath), ThemePresets.DarkPath);
            Assert.IsNotNull(AssetDatabase.LoadAssetAtPath<ThemeData>(ThemePresets.LightPath), ThemePresets.LightPath);
        }

        [Test]
        public void BuiltInLightTheme_KeepsItsBakedStyleSheetAsSubAsset()
        {
            var light = AssetDatabase.LoadAssetAtPath<ThemeData>(ThemePresets.LightPath);

            Assert.IsNotNull(light.StyleSheet);
            Assert.IsTrue(AssetDatabase.IsSubAsset(light.StyleSheet));
        }

        [Test]
        public void BuiltInThemes_BakedSheetsCarryOnlyThePrefixedTokenNames()
        {
            // The baked StyleSheet sub-asset stores every token as a custom property name (m_CustomName). A name that is
            // not --fdp-* would mean the theme and DesignTokens.uss no longer agree, and the theme would stop applying.
            foreach (var path in new[] { ThemePresets.DarkPath, ThemePresets.LightPath })
            {
                var yaml = System.IO.File.ReadAllText(path);
                var names = System.Text.RegularExpressions.Regex.Matches(yaml, @"m_CustomName: (\S+)");
                Assert.Greater(names.Count, 50, path + " carries its baked tokens");
                foreach (System.Text.RegularExpressions.Match m in names)
                    StringAssert.StartsWith("--fdp-", m.Groups[1].Value, path);
            }
        }

        [Test]
        public void UpstreamGuids_ResolveToTheShippedAssets()
        {
            var ussPath = AssetDatabase.GUIDToAssetPath(DesignSystemUssGuid);
            StringAssert.EndsWith("/FutureDigitalPlus/UIKit/DesignSystem/Styles/DesignSystem.uss", ussPath);

            var previewPath = AssetDatabase.GUIDToAssetPath(ThemePreviewUxmlGuid);
            StringAssert.EndsWith("/Theme/ThemePreview.uxml", previewPath);
            Assert.IsNotNull(AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(previewPath));
        }

        [Test]
        public void ThemePreviewUxml_StyleSheetReference_ResolvesByGuid_InEitherLayout()
        {
            var previewPath = AssetDatabase.GUIDToAssetPath(ThemePreviewUxmlGuid);
            var dependencies = AssetDatabase.GetDependencies(previewPath, false);
            Assert.Contains(AssetDatabase.GUIDToAssetPath(DesignSystemUssGuid), dependencies, "ThemePreview.uxml must depend on DesignSystem.uss");
            StringAssert.DoesNotContain("Packages/", System.IO.File.ReadAllText(
                FutureDigitalPlus.UIKit.Location.KitLocation.FullPathOf("Editor/DesignSystem/Theme/ThemePreview.uxml")), "no install path in the UXML");
        }

        [Test]
        public void BlueprintShader_ImportsWithoutCompilerMessages_OnThisEditor()
        {
            // The GPU material pipeline that uses this shader needs Unity 6000.5, but the shader file itself lives in
            // Resources and is imported on every editor. It must at least import cleanly.
            var shader = AssetDatabase.LoadAssetAtPath<UnityEngine.Shader>(
                AssetDatabase.GUIDToAssetPath("69d2f9c29236ca3479b9610da2d42146"));
            Assert.IsNotNull(shader, "DsFxBlueprint.shader loads by its upstream GUID");
            Assert.AreEqual(0, UnityEditor.ShaderUtil.GetShaderMessageCount(shader), "no shader compiler messages");
        }
    }
}
