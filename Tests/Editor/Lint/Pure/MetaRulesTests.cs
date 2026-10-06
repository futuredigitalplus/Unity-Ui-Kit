using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;

namespace FutureDigitalPlus.UIKit.Lint.Tests
{
    public class MetaRulesTests
    {
        [TestCase("Samples~/A/b.cs", true)]
        [TestCase("Documentation~", true)]
        [TestCase("Runtime/.gitkeep", true)]
        [TestCase("Runtime/A.cs", false)]
        [TestCase("Runtime/A~", true)]
        public void IsHidden(string path, bool hidden) => Assert.AreEqual(hidden, MetaRules.IsHidden(path));

        [Test]
        public void Check_FindsMissingOrphanAndHiddenMetas()
        {
            var paths = new[]
            {
                "Runtime", "Runtime.meta",
                "Runtime/A.cs", "Runtime/A.cs.meta",
                "Runtime/B.cs",                                  // missing meta
                "Runtime/C.cs.meta",                             // orphan meta
                "Samples~", "Samples~/X.cs",                     // hidden, no meta: fine
                "Docs~", "Docs~.meta",                           // meta for a hidden folder
                "Runtime/.gitkeep",                              // dot file, no meta: fine
            };

            var found = MetaRules.Check(paths).ToList();

            Assert.That(found.Select(f => f.Rule + " " + f.File), Is.EquivalentTo(new[]
            {
                "meta-missing Runtime/B.cs",
                "meta-orphan Runtime/C.cs.meta",
                "meta-in-hidden-folder Docs~.meta",
            }));
        }

        [Test]
        public void ParseGuid_ReadsTheGuidLine()
        {
            Assert.AreEqual("0123456789abcdef0123456789abcdef", MetaRules.ParseGuid("fileFormatVersion: 2\nguid: 0123456789ABCDEF0123456789abcdef\nMonoImporter:\n"));
            Assert.IsNull(MetaRules.ParseGuid("fileFormatVersion: 2\n"));
            Assert.IsNull(MetaRules.ParseGuid(null));
        }

        [Test]
        public void DuplicateGuids_FlagsRepeatsAndMissingGuids()
        {
            const string g1 = "guid: 11111111111111111111111111111111\n";
            const string g2 = "guid: 22222222222222222222222222222222\n";
            var metas = new List<KeyValuePair<string, string>>
            {
                new KeyValuePair<string, string>("a.meta", g1),
                new KeyValuePair<string, string>("b.meta", g2),
                new KeyValuePair<string, string>("c.meta", g1),
                new KeyValuePair<string, string>("d.meta", "fileFormatVersion: 2\n"),
            };

            var found = MetaRules.DuplicateGuids(metas).ToList();

            Assert.That(found.Select(f => f.Rule + " " + f.File), Is.EquivalentTo(new[] { "meta-duplicate-guid c.meta", "meta-no-guid d.meta" }));
            Assert.That(found.First(f => f.File == "c.meta").Message, Does.Contain("a.meta"));
        }
    }
}
