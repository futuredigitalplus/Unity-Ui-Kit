using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;

namespace FutureDigitalPlus.UIKit.Lint.Tests
{
    public class UssRulesTests
    {
        private static string[] Rules(IEnumerable<LintFinding> f) => f.Select(x => x.Rule).ToArray();

        [Test]
        public void ForbiddenFeatures_FlagsEachUnsupportedFeature()
        {
            const string sheet =
                ".ds-a { gap: 4px; box-shadow: 0 0 2px black; z-index: 3; width: calc(100% - 4px); }\n" +
                ".ds-b::before { color: red; }\n" +
                ".ds-b + .ds-c { color: red; }\n" +
                ".ds-b ~ .ds-c { color: red; }\n" +
                ".ds-d:not(.ds-e) { color: red; }\n" +
                "@media (min-width: 1px) { .ds-f { color: red; } }\n" +
                "@keyframes spin { from { top: 0; } }\n" +
                "Button[type=a] { color: red; }\n";

            var rules = Rules(UssRules.ForbiddenFeatures("a.uss", sheet));

            Assert.AreEqual(3, rules.Count(r => r == "uss-forbidden-property"), "gap, box-shadow, z-index");
            Assert.AreEqual(1, rules.Count(r => r == "uss-forbidden-calc"));
            Assert.AreEqual(2, rules.Count(r => r == "uss-forbidden-at-rule"));
            Assert.AreEqual(5, rules.Count(r => r == "uss-forbidden-selector"), "::before, +, ~, :not, [attr]");
        }

        [Test]
        public void ForbiddenFeatures_AcceptsWhatUssHas()
        {
            const string sheet =
                "@import url(\"Other.uss\");\n" +
                ":root { --fdp-a: 1px; }\n" +
                ".ds-a:hover > .ds-b .unity-label.ds-c:focus { margin-top: 1px; translate: 0 2px; transition-duration: 100ms; }\n" +
                "/* gap: z-index: calc(1) @media ::before in a comment */\n" +
                ".ds-x { -unity-font-style: bold; background-image: resource(\"gap/z-index\"); }\n";

            Assert.IsEmpty(UssRules.ForbiddenFeatures("a.uss", sheet).ToList());
        }

        [Test]
        public void ForbiddenFeatures_ReportsTheLineOfTheDeclaration()
        {
            var f = UssRules.ForbiddenFeatures("a.uss", ".ds-a {\n  color: red;\n  gap: 4px;\n}\n").Single();
            Assert.AreEqual(3, f.Line);
            Assert.AreEqual("a.uss", f.File);
        }

        [Test]
        public void UndefinedVariables_FdpNeedsADeclarationOrAFallback()
        {
            var declared = new HashSet<string> { "--fdp-known" };
            const string sheet =
                ".ds-a { color: var(--fdp-known); margin: var(--fdp-missing); padding: var(--fdp-other, 4px); }\n" +
                ".ds-b { top: var(--fdp-x, var(--fdp-y)); left: var(--unity-thing); width: var(--fdp-w ,1px); }\n";

            var found = UssRules.UndefinedVariables("a.uss", sheet, declared).Select(f => f.Message).ToList();

            Assert.AreEqual(2, found.Count, string.Join(" | ", found));
            Assert.That(found[0], Does.Contain("--fdp-missing"));
            Assert.That(found[1], Does.Contain("--fdp-y"), "a nested var() inside a fallback is checked on its own");
        }

        [Test]
        public void DeclaredCustomProperties_ReadsEveryRule()
        {
            var names = UssRules.DeclaredCustomProperties(":root { --fdp-a: 1px; }\n.ds-t { --fdp-b: red; color: blue; }").ToList();
            CollectionAssert.AreEqual(new[] { "--fdp-a", "--fdp-b" }, names);
        }

        [Test]
        public void BareClassSelectors_RequireAKitPrefix_IsClassesMustBeChained()
        {
            const string sheet =
                ".ds-a .fdp-b .unity-c { top: 0; }\n" +
                ".ds-a.is-active { top: 0; }\n" +
                ".is-active { top: 0; }\n" +
                ".card:hover { top: 0; }\n" +
                ".ds-a > .mobile, .ds-b { top: 0; }\n" +
                "Button.theme-light { top: 0; }\n";

            var classes = UssRules.BareClassSelectors("a.uss", sheet).Select(f => f.Message).ToList();

            Assert.AreEqual(4, classes.Count, string.Join(" | ", classes));
            Assert.That(classes[0], Does.Contain("'.is-active'"));
            Assert.That(classes[1], Does.Contain("'.card'"));
            Assert.That(classes[2], Does.Contain("'.mobile'"));
            Assert.That(classes[3], Does.Contain("'.theme-light'"));
        }

        [Test]
        public void BareClassSelectors_AllowedClassPredicate()
        {
            var found = UssRules.BareClassSelectors("a.uss", ".fadeIn { top: 0; } .other { top: 0; }", c => c == "fadeIn").ToList();
            Assert.AreEqual(1, found.Count);
            Assert.That(found[0].Message, Does.Contain("'.other'"));
        }

        [Test]
        public void ResourceAndImportReferences_AreExtracted()
        {
            const string sheet = "@import url(\"../Tokens.uss\");\n.ds-a { background-image: resource(\"Pkg/Icons/check\"); }\n.ds-b { background-image: url(\"project://database/x.png\"); }";

            CollectionAssert.AreEqual(new[] { ("Pkg/Icons/check", 2) }, UssRules.ResourceReferences(sheet).ToArray());
            CollectionAssert.AreEqual(new[] { ("../Tokens.uss", 1) }, UssRules.ImportReferences(sheet).ToArray());
        }
    }
}
