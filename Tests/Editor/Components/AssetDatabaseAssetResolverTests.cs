// FutureDigitalPlus: renamed by Tools~/rename/rename.py (namespaces, resource paths); see SOURCE.md "Rename record".
using System.Collections;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using NUnit.Framework;
using FutureDigitalPlus.UIKit.Components.Editor;
using UnityEditor;
using UnityEngine.TestTools;
using UnityEngine.UIElements;

namespace FutureDigitalPlus.UIKit.Components.Tests.Editor
{
    // FutureDigitalPlus: upstream test assets lived under Assets/UIComponents.Tests/... in the upstream project.
    // They now live inside this package, so every asset path used by these tests is defined here, in one place.
    // Attribute arguments must be constants, so the install location of the kit has to be written once here:
    // KitAssetRoot reads "Packages/com.futuredigitalplus.uikit" in the UPM source, and Tools~/make_classic_kit.py rewrites this
    // one line to "Assets/FutureDigitalPlus/UIKit" (or the --dst folder) for the classic layout. The test
    // Kit_Asset_Root_Constant_Matches_The_Install fails with a clear message when the kit sits somewhere else.
    // The "ambiguous" assets are created and deleted by the tests, so they are written to Assets/ (packages can be read-only).
    internal static class TestAssetPaths
    {
        public const string KitAssetRoot = "Packages/com.futuredigitalplus.uikit"; // CLASSIC-REWRITE:KitAssetRoot
        public const string EditorTestsRoot = KitAssetRoot + "/Tests/Editor/Components";
        public const string ConventionRoot = EditorTestsRoot + "/ConventionTestAssets/";
        public const string ExplicitAssetsRoot = EditorTestsRoot + "/ExplicitTestAssets";
        public const string ExplicitUss = ExplicitAssetsRoot + "/Component.uss";
        public const string ExplicitUxml = ExplicitAssetsRoot + "/Component.uxml";
        public const string AmbiguousParent = "Assets";
        public const string AmbiguousFolderName = "UIKitTests.AmbiguousConventionTestAssets";
        public const string AmbiguousRoot = AmbiguousParent + "/" + AmbiguousFolderName;
    }

    internal sealed class CustomEditorTestAssetSource : IAssetSource
    {
        public Task<T> LoadAsset<T>(string assetPath) where T : UnityEngine.Object
        {
            return Task.FromResult<T>(null);
        }

        public Task<bool> AssetExists(string assetPath)
        {
            return Task.FromResult(false);
        }
    }

    [Dependency(typeof(IAssetSource), provide: typeof(AssetDatabaseAssetSource))]
    [AssetRoot(TestAssetPaths.ConventionRoot)]
    [Layout]
    [Stylesheet]
    internal partial class ConventionTestComponent : UIComponent {}

    [Dependency(typeof(IAssetSource), provide: typeof(AssetDatabaseAssetSource))]
    [AssetRoot(TestAssetPaths.ConventionRoot)]
    [Layout]
    [Stylesheet]
    [SharedStylesheet("SharedTestStyle")]
    internal partial class FlatComponent : UIComponent {}

    [Dependency(typeof(IAssetSource), provide: typeof(AssetDatabaseAssetSource))]
    [AssetRoot(TestAssetPaths.ConventionRoot)]
    [Layout]
    [Stylesheet]
    internal partial class MissingConventionComponent : UIComponent {}

    [Dependency(typeof(IAssetSource), provide: typeof(AssetDatabaseAssetSource))]
    [AssetRoot(TestAssetPaths.AmbiguousRoot + "/")]
    [Layout]
    internal partial class AmbiguousConventionComponent : UIComponent {}

    // FutureDigitalPlus: AddressableValidatedComponent and NonAddressableProjectAssetComponent (and their tests)
    // were removed: they need the Addressables package plus an Addressables group configured in the project.
    // See Runtime/Components/SOURCE.md.

    [Dependency(typeof(IAssetSource), provide: typeof(CustomEditorTestAssetSource))]
    [Layout(TestAssetPaths.ExplicitUxml)]
    internal partial class CustomSourceExplicitAssetComponent : UIComponent {}

    [Dependency(typeof(IAssetSource), provide: typeof(CustomEditorTestAssetSource))]
    // FutureDigitalPlus: upstream used a sample asset (Components/ResourcesExampleComponent) here; the sample is not
    // imported with the package, so the test Resources asset FutureDigitalPlus/UIKit/Tests/Components/LayoutAttributeTests is used instead.
    [Layout("FutureDigitalPlus/UIKit/Tests/Components/LayoutAttributeTests")]
    [Stylesheet("FutureDigitalPlus/UIKit/Tests/Components/MissingCustomSourceStyle")]
    internal partial class CustomSourceResourceFallbackComponent : UIComponent {}

    [TestFixture]
    public class AssetDatabaseAssetSourceTests : AssetSourceTestSuite<AssetDatabaseAssetSource>
    {
        private const string AmbiguousConventionRoot = TestAssetPaths.AmbiguousRoot;

        [Test]
        public void Kit_Asset_Root_Constant_Matches_The_Install()
        {
            Assert.AreEqual(FutureDigitalPlus.UIKit.Location.KitLocation.AssetRoot, TestAssetPaths.KitAssetRoot,
                "TestAssetPaths.KitAssetRoot names the folder these tests read their assets from; it is rewritten by " +
                "Tools~/make_classic_kit.py for the classic layout. Re-run the converter with --dst " +
                "pointing at this folder, or edit the constant when the kit was moved.");
        }

        [UnityTest]
        public IEnumerator Should_Be_Able_To_Load_Existing_Asset()
        {
            yield return Assert_Loads_Existing_Asset<StyleSheet>(
                TestAssetPaths.ExplicitUss
            );
            yield return Assert_Loads_Existing_Asset<VisualTreeAsset>(
                TestAssetPaths.ExplicitUxml
            );
        }

        [UnityTest]
        public IEnumerator Should_Be_Able_To_Tell_If_Asset_Exists()
        {
            yield return Assert_Tells_If_Asset_Exists(
                TestAssetPaths.ExplicitUss
            );
            yield return Assert_Tells_If_Asset_Exists(
                TestAssetPaths.ExplicitUxml
            );
        }

        [Test]
        public void Resolves_Convention_Layout_In_Nested_Directory()
        {
            var source = new AssetDatabaseAssetSource();
            var task = source.LoadAsset<VisualTreeAsset>(
                TestAssetPaths.ConventionRoot + "ConventionTestComponent");
            Assert.That(task.Result, Is.Not.Null);
        }

        [Test]
        public void Resolves_Convention_Layout_In_Flat_Directory()
        {
            var source = new AssetDatabaseAssetSource();
            var task = source.LoadAsset<VisualTreeAsset>(
                TestAssetPaths.ConventionRoot + "FlatComponent");
            Assert.That(task.Result, Is.Not.Null);
        }

        [Test]
        public void Resolves_Convention_Path_By_Requested_Asset_Type()
        {
            var source = new AssetDatabaseAssetSource();

            var layoutTask = source.LoadAsset<VisualTreeAsset>(
                TestAssetPaths.ExplicitAssetsRoot + "/Component"); // FutureDigitalPlus: was a sample asset
            Assert.That(layoutTask.Result, Is.Not.Null);

            var stylesheetTask = source.LoadAsset<StyleSheet>(
                TestAssetPaths.ExplicitAssetsRoot + "/Component"); // FutureDigitalPlus: was a sample asset
            Assert.That(stylesheetTask.Result, Is.Not.Null);
        }

        [Test]
        public void Passes_Through_Explicit_Path()
        {
            var source = new AssetDatabaseAssetSource();
            var task = source.LoadAsset<StyleSheet>(
                TestAssetPaths.ExplicitUss);
            Assert.That(task.Result, Is.Not.Null);
        }

        [Test]
        public void Returns_Null_For_Unresolvable_Convention_Path()
        {
            var source = new AssetDatabaseAssetSource();
            var task = source.LoadAsset<VisualTreeAsset>("Assets/NonExistent/SomeComponent");
            Assert.That(task.Result, Is.Null);
        }

        [Test]
        public void Returns_Null_And_False_For_Ambiguous_Convention_Path()
        {
            CreateAmbiguousConventionAssets();

            try
            {
                var source = new AssetDatabaseAssetSource();
                const string path = TestAssetPaths.AmbiguousRoot + "/AmbiguousConventionComponent";

                var loadTask = source.LoadAsset<VisualTreeAsset>(path);
                var existsTask = source.AssetExists(path);

                Assert.That(loadTask.Result, Is.Null);
                Assert.That(existsTask.Result, Is.False);
            }
            finally
            {
                DeleteAmbiguousConventionAssets();
            }
        }

        [Test]
        public void ConventionValidator_Resolves_Nested_Convention_Paths()
        {
            var results = ConventionValidator.ValidateAll()
                .Where(result => result.ComponentType == typeof(ConventionTestComponent))
                .ToArray();

            Assert.That(results, Has.Length.EqualTo(2));
            Assert.That(results.All(result => result.Exists), Is.True);
            Assert.That(results.All(result => !result.IsAmbiguous), Is.True);

            var layoutResult = results.Single(result => result.AssetKind == "Layout");
            Assert.That(layoutResult.ResolvedPath,
                Is.EqualTo(TestAssetPaths.ConventionRoot + "ConventionTestComponent/ConventionTestComponent.uxml"));

            var stylesheetResult = results.Single(result => result.AssetKind == "Stylesheet");
            Assert.That(stylesheetResult.ResolvedPath,
                Is.EqualTo(TestAssetPaths.ConventionRoot + "ConventionTestComponent/ConventionTestComponent.style.uss"));
        }

        [Test]
        public void ConventionValidator_Resolves_Flat_Convention_And_Shared_Stylesheet_Paths()
        {
            var results = ConventionValidator.ValidateAll()
                .Where(result => result.ComponentType == typeof(FlatComponent))
                .ToArray();

            Assert.That(results, Has.Length.EqualTo(3));
            Assert.That(results.All(result => result.Exists), Is.True);
            Assert.That(results.All(result => !result.IsAmbiguous), Is.True);

            var resolvedPaths = results
                .Select(result => result.ResolvedPath)
                .OrderBy(path => path)
                .ToArray();

            Assert.That(resolvedPaths, Is.EqualTo(new[]
            {
                TestAssetPaths.ConventionRoot + "FlatComponent.style.uss",
                TestAssetPaths.ConventionRoot + "FlatComponent.uxml",
                TestAssetPaths.ConventionRoot + "SharedTestStyle.uss"
            }));
        }

        [Test]
        public void ConventionValidator_Reports_Missing_Convention_Paths()
        {
            var results = ConventionValidator.ValidateAll()
                .Where(result => result.ComponentType == typeof(MissingConventionComponent))
                .ToArray();

            Assert.That(results, Has.Length.EqualTo(2));
            Assert.That(results.All(result => !result.Exists), Is.True);
            Assert.That(results.All(result => !result.IsAmbiguous), Is.True);
            Assert.That(results.All(result => result.ResolvedPath == null), Is.True);
        }

        [Test]
        public void ConventionValidator_Reports_Ambiguous_Convention_Paths()
        {
            CreateAmbiguousConventionAssets();

            try
            {
                var results = ConventionValidator.ValidateAll()
                    .Where(result => result.ComponentType == typeof(AmbiguousConventionComponent))
                    .ToArray();

                Assert.That(results, Has.Length.EqualTo(1));
                Assert.That(results[0].Exists, Is.False);
                Assert.That(results[0].IsAmbiguous, Is.True);
                Assert.That(results[0].ResolvedPath, Is.Null);
                Assert.That(results[0].CandidatePaths, Is.EqualTo(new[]
                {
                    TestAssetPaths.AmbiguousRoot + "/OptionA/AmbiguousConventionComponent.uxml",
                    TestAssetPaths.AmbiguousRoot + "/OptionB/AmbiguousConventionComponent.uxml"
                }));
            }
            finally
            {
                DeleteAmbiguousConventionAssets();
            }
        }

        // FutureDigitalPlus: ConventionValidator_Resolves_Addressable_Keys and
        // ConventionValidator_Does_Not_Treat_NonAddressable_Project_Assets_As_Valid removed (need Addressables).

        [Test]
        public void ConventionValidator_Uses_Fallback_Direct_AssetDatabase_Path_For_Custom_Sources()
        {
            var results = ConventionValidator.ValidateAll()
                .Where(result => result.ComponentType == typeof(CustomSourceExplicitAssetComponent))
                .ToArray();

            Assert.That(results, Has.Length.EqualTo(1));
            Assert.That(results[0].Exists, Is.True);
            Assert.That(results[0].IsAmbiguous, Is.False);
            Assert.That(results[0].ResolvedPath,
                Is.EqualTo(TestAssetPaths.ExplicitUxml));
        }

        [Test]
        public void ConventionValidator_Uses_Fallback_Resources_Paths_For_Custom_Sources()
        {
            var results = ConventionValidator.ValidateAll()
                .Where(result => result.ComponentType == typeof(CustomSourceResourceFallbackComponent))
                .ToArray();

            Assert.That(results, Has.Length.EqualTo(2));

            var layoutResult = results.Single(result => result.AssetKind == "Layout");
            Assert.That(layoutResult.Exists, Is.True);
            Assert.That(layoutResult.IsAmbiguous, Is.False);
            Assert.That(layoutResult.ResolvedPath, Is.EqualTo("FutureDigitalPlus/UIKit/Tests/Components/LayoutAttributeTests"));

            var stylesheetResult = results.Single(result => result.AssetKind == "Stylesheet");
            Assert.That(stylesheetResult.Exists, Is.False);
            Assert.That(stylesheetResult.IsAmbiguous, Is.False);
            Assert.That(stylesheetResult.ResolvedPath, Is.Null);
        }

        [Test]
        public void ValidateAndReport_Summarizes_Current_Validation_Findings()
        {
            CreateAmbiguousConventionAssets();

            try
            {
                var results = ConventionValidator.ValidateAll();
                var summary = ConventionValidator.ValidateAndReport();

                Assert.That(summary.TotalCount, Is.EqualTo(results.Count));
                Assert.That(summary.UnresolvedCount,
                    Is.EqualTo(results.Count(result => !result.Exists && !result.IsAmbiguous)));
                Assert.That(summary.AmbiguousCount,
                    Is.EqualTo(results.Count(result => result.IsAmbiguous)));
                Assert.That(summary.HasFailures, Is.True);
            }
            finally
            {
                DeleteAmbiguousConventionAssets();
            }
        }

        [Test]
        public void ValidateBatchModeScope_Excludes_Test_Assemblies()
        {
            CreateAmbiguousConventionAssets();

            try
            {
                var results = ConventionValidator.ValidateBatchModeScope();

                Assert.That(results.Any(result => result.ComponentType == typeof(ConventionTestComponent)), Is.False);
                Assert.That(results.Any(result => result.ComponentType == typeof(MissingConventionComponent)), Is.False);
                Assert.That(results.Any(result => result.ComponentType == typeof(AmbiguousConventionComponent)), Is.False);
                Assert.That(results.All(result =>
                    !result.ComponentType.Assembly.GetName().Name.StartsWith("FutureDigitalPlus.UIKit.Components.Tests", System.StringComparison.Ordinal)), Is.True);
            }
            finally
            {
                DeleteAmbiguousConventionAssets();
            }
        }

        [Test]
        public void ValidateBatchModeAndReport_Excludes_TestOnly_Failures()
        {
            CreateAmbiguousConventionAssets();

            try
            {
                var allSummary = ConventionValidator.ValidateAndReport();
                var summary = ConventionValidator.ValidateBatchModeAndReport();

                Assert.That(allSummary.HasFailures, Is.True);
                Assert.That(summary.HasFailures, Is.False);
                Assert.That(summary.UnresolvedCount, Is.Zero);
                Assert.That(summary.AmbiguousCount, Is.Zero);
            }
            finally
            {
                DeleteAmbiguousConventionAssets();
            }
        }

        private static void CreateAmbiguousConventionAssets()
        {
            DeleteAmbiguousConventionAssets();

            AssetDatabase.CreateFolder(TestAssetPaths.AmbiguousParent, TestAssetPaths.AmbiguousFolderName);
            AssetDatabase.CreateFolder(AmbiguousConventionRoot, "OptionA");
            AssetDatabase.CreateFolder(AmbiguousConventionRoot, "OptionB");

            File.WriteAllText(
                AmbiguousConventionRoot + "/OptionA/AmbiguousConventionComponent.uxml",
                MinimalUxml);
            File.WriteAllText(
                AmbiguousConventionRoot + "/OptionB/AmbiguousConventionComponent.uxml",
                MinimalUxml);

            AssetDatabase.Refresh();
        }

        private static void DeleteAmbiguousConventionAssets()
        {
            if (AssetDatabase.IsValidFolder(AmbiguousConventionRoot))
            {
                AssetDatabase.DeleteAsset(AmbiguousConventionRoot);
                AssetDatabase.Refresh();
            }
        }

        private const string MinimalUxml =
            "<ui:UXML xmlns:ui=\"UnityEngine.UIElements\"><ui:VisualElement /></ui:UXML>";
    }
}
