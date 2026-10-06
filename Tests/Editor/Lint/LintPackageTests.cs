using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using NUnit.Framework;

namespace FutureDigitalPlus.UIKit.Lint.Tests
{
    /// <summary>
    /// Rule tests over the package's own files (EditMode). The rules are pure functions (<c>Pure/</c>), tested on small inputs in
    /// <see cref="UssRulesTests"/> and the other rule test classes; this class applies them to the real package. A failing test lists every
    /// finding as <c>file:line: [rule] message</c>. Accepted exceptions live in <see cref="LintAllowList"/>.
    /// </summary>
    public class LintPackageTests
    {
        private static List<LintFinding> _raw;

        [OneTimeSetUp]
        public void RequirePackage()
        {
            if (PackageFiles.Root == null || !Directory.Exists(PackageFiles.Root))
                Assert.Inconclusive("The package folder could not be located (set FDP_PACKAGE_ROOT for the dotnet runner).");
        }

        private static IEnumerable<string> Sheets => PackageFiles.FilesUnder(".uss", "Runtime", "Editor");

        private static IEnumerable<string> CSharp => PackageFiles.FilesUnder(".cs", "Runtime", "Editor");

        /// <summary>All findings of all rules on the real package, before the allow-list (computed once).</summary>
        private static List<LintFinding> Raw()
        {
            if (_raw != null) return _raw;

            var all = new List<LintFinding>();

            // USS features, variables, classes.
            var declared = new HashSet<string>(StringComparer.Ordinal);
            foreach (string f in Sheets)
                foreach (string name in UssRules.DeclaredCustomProperties(PackageFiles.Read(f))) declared.Add(name);
            foreach (string f in PackageFiles.FilesUnder(".asset", "Runtime").Where(p => p.Contains("/Themes/")))
                foreach (string name in ThemeAssetRules.CustomPropertyNames(PackageFiles.Read(f))) declared.Add(name);

            foreach (string f in Sheets)
            {
                string text = PackageFiles.Read(f);
                all.AddRange(UssRules.ForbiddenFeatures(f, text));
                all.AddRange(UssRules.UndefinedVariables(f, text, declared));
            }

            // Animation's classes are camelCase upstream names that exist only on its own elements; its sheets are attached to those elements alone,
            // so a host class cannot match (rename plan, explicit exception 2).
            foreach (string f in PackageFiles.FilesUnder(".uss", "Runtime"))
            {
                bool animation = f.StartsWith("Runtime/Animation/", StringComparison.Ordinal);
                all.AddRange(UssRules.BareClassSelectors(f, PackageFiles.Read(f), animation ? (Func<string, bool>)(_ => true) : null));
            }

            // Resource paths.
            var index = new ResourceIndex(PackageFiles.AllPaths);
            var constants = new Dictionary<string, string>();
            var ambiguous = new HashSet<string>();
            foreach (string f in CSharp) ResourceRules.ConstStrings(PackageFiles.Read(f), constants, ambiguous);
            foreach (string f in CSharp) all.AddRange(ResourceRules.UnresolvedLoads(f, PackageFiles.Read(f), constants, index));
            foreach (string f in Sheets)
                all.AddRange(ResourceRules.UnresolvedSheetReferences(f, f.Substring(0, f.LastIndexOf('/')), PackageFiles.Read(f), index, PackageFiles.FileExists));

            // Assembly definitions (the package, its tests and the samples; Tools~ holds scratch-project scripts, not package code).
            foreach (string f in PackageFiles.Files.Where(p => p.EndsWith(".asmdef", StringComparison.Ordinal) && !p.StartsWith("Tools~/", StringComparison.Ordinal)))
                all.AddRange(AsmdefRules.Check(f, PackageFiles.Read(f)));

            // Metas.
            all.AddRange(MetaRules.Check(PackageFiles.AllPaths));
            var metas = PackageFiles.Files.Where(p => p.EndsWith(".meta", StringComparison.Ordinal) && !MetaRules.IsHidden(p))
                .Select(p => new KeyValuePair<string, string>(p, PackageFiles.Read(p)));
            all.AddRange(MetaRules.DuplicateGuids(metas));

            _raw = all;
            return _raw;
        }

        private static void AssertNone(Func<string, bool> ruleFilter)
        {
            var left = LintAllowList.Filter(Raw().Where(f => ruleFilter(f.Rule))).ToList();
            Assert.That(left, Is.Empty, "\n" + string.Join("\n", left.Select(f => f.ToString())));
        }

        [Test]
        public void UssUsesOnlyFeaturesUssHas() => AssertNone(r => r.StartsWith("uss-forbidden-", StringComparison.Ordinal));

        [Test]
        public void EveryFdpVariableIsDeclaredOrHasAFallback() => AssertNone(r => r == "uss-undefined-variable");

        [Test]
        public void ClassSelectorsCarryAKitPrefix() => AssertNone(r => r == "uss-bare-class");

        [Test]
        public void ResourcePathsResolve() => AssertNone(r => r == "resource-unresolved");

        [Test]
        public void EveryVisibleFileAndFolderHasAMetaAndNothingHiddenHasOne() =>
            AssertNone(r => r == "meta-missing" || r == "meta-orphan" || r == "meta-in-hidden-folder");

        [Test]
        public void MetaGuidsAreUnique() => AssertNone(r => r == "meta-duplicate-guid" || r == "meta-no-guid");

        [Test]
        public void AssembliesDependOnUnityAndTheKitOnly() => AssertNone(r => r == "asmdef-foreign-reference");

        [Test]
        public void OptionalIntegrationsSitBehindADefineConstraint() => AssertNone(r => r == "asmdef-optional-unconstrained");

        [Test]
        public void PureLayersHaveNoEngineReference() => AssertNone(r => r == "asmdef-core-engine");

        [Test]
        public void AllowListHasNoStaleEntries()
        {
            var raw = Raw();
            var stale = LintAllowList.Entries.Where(e => !raw.Any(e.Matches)).ToList();
            Assert.That(stale, Is.Empty, "allow-list entries that match no finding: " + string.Join("; ", stale.Select(e => $"{e.Rule} {e.FilePrefix} '{e.MessagePart}'")));
        }
    }
}
