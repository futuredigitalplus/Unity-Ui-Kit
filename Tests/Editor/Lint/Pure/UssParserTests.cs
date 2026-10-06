using System.Linq;
using NUnit.Framework;

namespace FutureDigitalPlus.UIKit.Lint.Tests
{
    public class UssParserTests
    {
        [Test]
        public void StripComments_KeepsLineCountAndStrings()
        {
            string s = UssParser.StripComments("a { /* x\ny */ b: \"/* not a comment */\"; }");
            Assert.AreEqual(1, s.Count(c => c == '\n'));
            Assert.That(s, Does.Contain("\"/* not a comment */\""));
            Assert.That(s, Does.Not.Contain("x"));
        }

        [Test]
        public void Parse_ReadsRulesDeclarationsAndLines()
        {
            const string sheet = "/* header */\n.ds-a, .ds-b > .ds-c\n{\n  color: red;\n  margin: 1px 2px;\n}\n\n.ds-d { background-image: resource(\"a/b;c\"); }\n";
            var rules = UssParser.Parse(sheet);

            Assert.AreEqual(2, rules.Count);
            Assert.AreEqual(".ds-a, .ds-b > .ds-c", rules[0].Selector);
            Assert.AreEqual(2, rules[0].Line);
            Assert.AreEqual(2, rules[0].Declarations.Count);
            Assert.AreEqual("color", rules[0].Declarations[0].Name);
            Assert.AreEqual(4, rules[0].Declarations[0].Line);
            Assert.AreEqual("1px 2px", rules[0].Declarations[1].Value);
            Assert.AreEqual(5, rules[0].Declarations[1].Line);
            Assert.AreEqual("resource(\"a/b;c\")", rules[1].Declarations[0].Value, "a ';' inside parentheses does not end the declaration");
            Assert.AreEqual(8, rules[1].Line);
        }

        [Test]
        public void Parse_AtRules_ImportHasNoBody_MediaHasNestedBlock()
        {
            var rules = UssParser.Parse("@import url(\"X.uss\");\n@media (a) { .x { color: red; } }\n.ds-y { top: 0; }");

            Assert.AreEqual(3, rules.Count);
            Assert.IsTrue(rules[0].IsAtRule);
            Assert.AreEqual("@import url(\"X.uss\")", rules[0].Selector);
            Assert.IsTrue(rules[1].IsAtRule);
            Assert.AreEqual(0, rules[1].Declarations.Count, "the body of an at-rule is not parsed as declarations");
            Assert.AreEqual(".ds-y", rules[2].Selector, "parsing continues after a nested block");
            Assert.AreEqual(3, rules[2].Line);
        }

        [Test]
        public void Parse_CustomPropertiesAndNoTrailingSemicolon()
        {
            var rules = UssParser.Parse(":root { --fdp-a: 1px; --fdp-b: var(--fdp-a, 2px) }");
            Assert.AreEqual(2, rules[0].Declarations.Count);
            Assert.AreEqual("--fdp-b", rules[0].Declarations[1].Name);
            Assert.AreEqual("var(--fdp-a, 2px)", rules[0].Declarations[1].Value);
        }
    }
}
