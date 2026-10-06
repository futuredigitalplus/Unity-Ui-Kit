using System.IO;
using System.Linq;
using NUnit.Framework;
using UnityEditor;

namespace FutureDigitalPlus.UIKit.Location.Tests
{
    /// <summary>The kit finds itself in both layouts (UPM package and plain folder below Assets) and no source hard-codes either location.</summary>
    public class KitLocationTests
    {
        [Test]
        public void TheKitIsFound_AndTheRootIsTheParentOfTheRuntimeFolder()
        {
            Assert.AreNotEqual(KitInstall.NotFound, KitLocation.Install);
            string runtime = AssetDatabase.GUIDToAssetPath(KitLocation.RuntimeFolderGuid);
            Assert.AreEqual(KitLocation.AssetRoot + "/Runtime", runtime);
            Assert.IsTrue(AssetDatabase.IsValidFolder(KitLocation.AssetPath("Runtime")));
            Assert.IsTrue(Directory.Exists(KitLocation.FullPathOf("Runtime")), KitLocation.FullPath);
        }

        [Test]
        public void TheLayoutFollowsTheAssetRoot()
        {
            bool underAssets = KitLocation.AssetRoot.StartsWith("Assets/");
            Assert.AreEqual(underAssets, KitLocation.IsClassic);
            Assert.AreEqual(underAssets ? KitInstall.Classic : KitInstall.Package, KitLocation.Install);
            if (underAssets) Assert.IsTrue(KitLocation.IsWritable, "a folder below Assets is always writable");
            if (!underAssets) StringAssert.StartsWith("Packages/", KitLocation.AssetRoot);
        }

        [Test]
        public void PathHelpers_JoinRelativePaths()
        {
            Assert.AreEqual(KitLocation.AssetRoot, KitLocation.AssetPath(""));
            Assert.AreEqual(KitLocation.AssetRoot + "/Tests", KitLocation.AssetPath("Tests"));
            Assert.AreEqual(KitLocation.AssetRoot + "/Tests", KitLocation.AssetPath("/Tests"));
            Assert.AreEqual(KitLocation.FullPath, KitLocation.FullPathOf(null));
            Assert.IsTrue(File.Exists(KitLocation.FullPathOf("Runtime.meta")));
        }

        [Test]
        public void NoEditorOrRuntimeSource_HardCodesTheInstallLocation()
        {
            // Runtime/ and Editor/ must work in both layouts. Only KitLocation itself names the two locations.
            string[] forbidden = { "Packages/com.futuredigitalplus.uikit", "Assets/FutureDigitalPlus/UIKit" };
            var offenders = new System.Collections.Generic.List<string>();
            foreach (string folder in new[] { "Runtime", "Editor" })
            {
                foreach (string file in Directory.EnumerateFiles(KitLocation.FullPathOf(folder), "*.*", SearchOption.AllDirectories))
                {
                    string ext = Path.GetExtension(file);
                    if (ext != ".cs" && ext != ".uss" && ext != ".uxml" && ext != ".asset" && ext != ".asmdef" && ext != ".shader" && ext != ".tss") continue;
                    if (Path.GetFileName(file) == "KitLocation.cs") continue;
                    string text = File.ReadAllText(file);
                    foreach (string needle in forbidden)
                        if (text.Contains(needle)) offenders.Add(file.Substring(KitLocation.FullPath.Length + 1) + " mentions " + needle);
                }
            }

            Assert.IsEmpty(offenders, string.Join("\n", offenders));
        }
    }
}
