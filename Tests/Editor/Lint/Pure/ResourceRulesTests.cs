using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;

namespace FutureDigitalPlus.UIKit.Lint.Tests
{
    public class ResourceRulesTests
    {
        private static readonly string[] Files =
        {
            "Runtime/A/Resources/Pkg/Styles/Base.uss",
            "Runtime/A/Resources/Pkg/Styles/Base.uss.meta",
            "Runtime/A/Resources/Pkg/Icons/check.svg",
            "Tests/Runtime/Resources/Pkg/Tests/Layout.uxml",
            "Runtime/B/Pkg/NotResources/x.uss",
            "Samples~/S/Resources/Pkg/Sample/y.uss",
        };

        [Test]
        public void ResourceIndex_StripsExtensionsAndKnowsFolders()
        {
            var index = new ResourceIndex(Files);

            Assert.IsTrue(index.HasAsset("Pkg/Styles/Base"));
            Assert.IsTrue(index.HasAsset("Pkg/Icons/check"));
            Assert.IsTrue(index.HasAsset("Pkg/Tests/Layout"));
            Assert.IsFalse(index.HasAsset("Pkg/Styles/Base.uss"), "Resources.Load takes no extension");
            Assert.IsFalse(index.HasAsset("Pkg/NotResources/x"));
            Assert.IsFalse(index.HasAsset("Pkg/Sample/y"), "files in hidden folders are not imported");
            Assert.IsTrue(index.HasFolder("Pkg/Icons"));
            Assert.IsTrue(index.HasFolder("Pkg"));
            Assert.IsFalse(index.HasFolder("Pkg/Nope"));
        }

        [Test]
        public void LoadLiterals_FindsLiteralsAndResolvesConsts()
        {
            const string cs =
                "var a = Resources.Load<StyleSheet>(\"Pkg/Styles/Base\");\n" +
                "var b = Resources.Load<Shader>(Options.BaseSheet);\n" +
                "var c = Resources.LoadAll<Texture>(\"Pkg/Icons\");\n" +
                "var d = Resources.Load<Font>(computed + \"x\");\n" +
                "var e = Resources.Load(unknownConst);\n" +
                "// Resources.Load(\"in/comment\") is still seen: the lint reads text, not syntax\n";
            var constants = new Dictionary<string, string> { ["BaseSheet"] = "Pkg/Styles/Base" };

            var lits = ResourceRules.LoadLiterals(cs, constants).ToList();

            Assert.AreEqual(4, lits.Count);
            Assert.AreEqual("Pkg/Styles/Base", lits[0].Path);
            Assert.AreEqual(1, lits[0].Line);
            Assert.AreEqual("Pkg/Styles/Base", lits[1].Path);
            Assert.IsTrue(lits[2].IsFolder);
            Assert.AreEqual("in/comment", lits[3].Path);
        }

        [Test]
        public void ConstStrings_DropsAmbiguousNames()
        {
            var map = new Dictionary<string, string>();
            var ambiguous = new HashSet<string>();
            ResourceRules.ConstStrings("const string A = \"x\"; public const string B = \"y\";", map, ambiguous);
            ResourceRules.ConstStrings("const string A = \"z\"; const string B = \"y\";", map, ambiguous);

            Assert.IsFalse(map.ContainsKey("A"));
            Assert.AreEqual("y", map["B"]);
            Assert.IsTrue(ambiguous.Contains("A"));
        }

        [Test]
        public void UnresolvedLoads_ReportsOnlyMissingPaths()
        {
            var index = new ResourceIndex(Files);
            const string cs = "Resources.Load<StyleSheet>(\"Pkg/Styles/Base\");\nResources.Load<StyleSheet>(\"Pkg/Styles/Gone\");";

            var f = ResourceRules.UnresolvedLoads("X.cs", cs, new Dictionary<string, string>(), index).Single();

            Assert.AreEqual("resource-unresolved", f.Rule);
            Assert.AreEqual(2, f.Line);
            Assert.That(f.Message, Does.Contain("Pkg/Styles/Gone"));
        }

        [Test]
        public void UnresolvedSheetReferences_ChecksResourceAndImport()
        {
            var index = new ResourceIndex(Files);
            var existing = new HashSet<string> { "Runtime/A/Resources/Pkg/Styles/Tokens.uss" };
            const string uss = "@import url(\"Tokens.uss\");\n@import url(\"Missing.uss\");\n.ds-a { background-image: resource(\"Pkg/Icons/check\"); }\n.ds-b { background-image: resource(\"Pkg/Icons/gone\"); }";

            var found = ResourceRules.UnresolvedSheetReferences("Runtime/A/Resources/Pkg/Styles/Base.uss", "Runtime/A/Resources/Pkg/Styles", uss, index, existing.Contains).ToList();

            Assert.AreEqual(2, found.Count, string.Join(" | ", found));
            Assert.That(found.Select(f => f.Line), Is.EquivalentTo(new[] { 2, 4 }));
        }

        [Test]
        public void CombineAndNormalize_HandlesDotSegments()
        {
            Assert.AreEqual("a/c/d.uss", ResourceRules.CombineAndNormalize("a/b", "../c/./d.uss"));
            Assert.AreEqual("a/b/x.uss", ResourceRules.CombineAndNormalize("a/b", "x.uss"));
        }
    }
}
