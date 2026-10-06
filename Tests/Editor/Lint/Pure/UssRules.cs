using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace FutureDigitalPlus.UIKit.Lint
{
    /// <summary>
    /// Lint rules for the USS sheets that ship in the package (<c>Runtime/**/Resources/**/*.uss</c>). Pure functions on text, so the same code runs in
    /// Unity EditMode and in the dotnet runner.
    /// </summary>
    public static class UssRules
    {
        /// <summary>Properties USS does not have (they are silently ignored or warned about by Unity).</summary>
        public static readonly string[] ForbiddenProperties = { "gap", "row-gap", "column-gap", "box-shadow", "z-index" };

        /// <summary>Class prefixes a sheet of the kit may style. <c>is-*</c> state classes are allowed when chained to one of these.</summary>
        public static readonly string[] OwnedClassPrefixes = { "ds-", "fdp-", "unity-" };

        private static readonly Regex VarCall = new Regex(@"\bvar\s*\(", RegexOptions.Compiled);
        private static readonly Regex CalcCall = new Regex(@"\bcalc\s*\(", RegexOptions.Compiled);
        private static readonly Regex ClassName = new Regex(@"\.(-?[A-Za-z_][A-Za-z0-9_-]*)", RegexOptions.Compiled);
        private static readonly Regex UnsupportedPseudo = new Regex(@":(not|nth-[a-z-]+|has|is|where|first-of-type|last-of-type|only-of-type)\b", RegexOptions.Compiled);
        private static readonly Regex ResourceCall = new Regex(@"\bresource\s*\(\s*(['""])(.*?)\1\s*\)", RegexOptions.Compiled);
        private static readonly Regex ImportUrl = new Regex(@"^@import\s+url\s*\(\s*(['""])(.*?)\1\s*\)", RegexOptions.Compiled);

        /// <summary>
        /// Features that do not exist in USS: <c>gap</c>, <c>box-shadow</c>, <c>z-index</c>, <c>calc()</c>, any at-rule but <c>@import</c> (<c>@media</c>,
        /// <c>@keyframes</c>, ...), pseudo-elements, sibling combinators, attribute selectors and the pseudo-classes <c>:not</c>, <c>:nth-*</c>, <c>:has</c>, <c>:is</c>, <c>:where</c>.
        /// </summary>
        public static IEnumerable<LintFinding> ForbiddenFeatures(string file, string text)
        {
            foreach (var rule in UssParser.Parse(text))
            {
                if (rule.IsAtRule)
                {
                    if (!rule.Selector.StartsWith("@import", StringComparison.Ordinal))
                        yield return new LintFinding("uss-forbidden-at-rule", file, rule.Line, $"USS has no '{FirstWord(rule.Selector)}'.");
                    continue;
                }

                foreach (string problem in SelectorProblems(rule.Selector))
                    yield return new LintFinding("uss-forbidden-selector", file, rule.Line, $"{problem} in '{Shorten(rule.Selector)}'.");

                foreach (var d in rule.Declarations)
                {
                    foreach (string prop in ForbiddenProperties)
                        if (string.Equals(d.Name, prop, StringComparison.OrdinalIgnoreCase))
                            yield return new LintFinding("uss-forbidden-property", file, d.Line, $"USS has no '{prop}'.");

                    if (CalcCall.IsMatch(d.Value))
                        yield return new LintFinding("uss-forbidden-calc", file, d.Line, $"USS has no calc(): '{d.Name}: {Shorten(d.Value)}'.");
                }
            }
        }

        private static IEnumerable<string> SelectorProblems(string selector)
        {
            string s = StripStrings(selector);

            if (s.Contains("::")) yield return "pseudo-element";
            if (s.IndexOf('[') >= 0) yield return "attribute selector";

            var m = UnsupportedPseudo.Match(s);
            if (m.Success) yield return $"pseudo-class ':{m.Groups[1].Value}'";

            // '+' and '~' are sibling combinators; outside parentheses they cannot be anything else in a selector.
            int paren = 0;
            foreach (char c in s)
            {
                if (c == '(') paren++;
                else if (c == ')') paren = paren > 0 ? paren - 1 : 0;
                else if (paren == 0 && c == '+') { yield return "adjacent sibling combinator '+'"; break; }
                else if (paren == 0 && c == '~') { yield return "general sibling combinator '~'"; break; }
            }
        }

        /// <summary>Names of all custom properties a sheet declares (<c>--name: value</c>), in any rule.</summary>
        public static IEnumerable<string> DeclaredCustomProperties(string text)
        {
            foreach (var rule in UssParser.Parse(text))
                foreach (var d in rule.Declarations)
                    if (d.Name.StartsWith("--", StringComparison.Ordinal))
                        yield return d.Name;
        }

        /// <summary>
        /// Every <c>var(--fdp-x)</c> use must name a declared property or carry a fallback (<c>var(--fdp-x, 8px)</c>). Variables of other
        /// prefixes (<c>--unity-*</c>, <c>--ds-font-*</c>) are not checked.
        /// </summary>
        /// <param name="file">Package relative path of the sheet, for the finding.</param>
        /// <param name="text">The sheet's text.</param>
        /// <param name="declared">Names declared anywhere in the package: sheets and baked themes.</param>
        public static IEnumerable<LintFinding> UndefinedVariables(string file, string text, ISet<string> declared)
        {
            foreach (var rule in UssParser.Parse(text))
            {
                foreach (var d in rule.Declarations)
                {
                    foreach (Match m in VarCall.Matches(d.Value))
                    {
                        int open = m.Index + m.Length;
                        int comma = -1;
                        int depth = 0;
                        int close = -1;

                        for (int i = open; i < d.Value.Length; i++)
                        {
                            char c = d.Value[i];
                            if (c == '(') depth++;
                            else if (c == ')')
                            {
                                if (depth == 0) { close = i; break; }
                                depth--;
                            }
                            else if (c == ',' && depth == 0 && comma < 0) comma = i;
                        }

                        if (close < 0) close = d.Value.Length;
                        string name = d.Value.Substring(open, (comma >= 0 ? comma : close) - open).Trim();

                        if (!name.StartsWith("--fdp-", StringComparison.Ordinal)) continue;
                        if (comma >= 0) continue;           // has a fallback
                        if (declared.Contains(name)) continue;

                        yield return new LintFinding("uss-undefined-variable", file, d.Line, $"var({name}) is declared nowhere in the package and has no fallback.");
                    }
                }
            }
        }

        /// <summary>
        /// Class selectors must start with one of <see cref="OwnedClassPrefixes"/> so a host's classes can never match; <c>is-*</c> state
        /// classes only count when chained to such a class in the same compound selector (<c>.ds-btn.is-active</c>).
        /// </summary>
        /// <param name="file">Package relative path of the sheet, for the finding.</param>
        /// <param name="text">The sheet's text.</param>
        /// <param name="allowedClass">Optional exact class names that are allowed anyway (for example a module whose upstream names are scoped by sheet attachment).</param>
        public static IEnumerable<LintFinding> BareClassSelectors(string file, string text, Func<string, bool> allowedClass = null)
        {
            foreach (var rule in UssParser.Parse(text))
            {
                if (rule.IsAtRule) continue;

                foreach (string compound in Compounds(StripStrings(rule.Selector)))
                {
                    var classes = new List<string>();
                    foreach (Match m in ClassName.Matches(compound)) classes.Add(m.Groups[1].Value);

                    bool owned = classes.Exists(IsOwned);

                    foreach (string c in classes)
                    {
                        if (IsOwned(c)) continue;
                        if (allowedClass != null && allowedClass(c)) continue;
                        if (owned && c.StartsWith("is-", StringComparison.Ordinal)) continue;

                        yield return new LintFinding("uss-bare-class", file, rule.Line,
                            $"class '.{c}' has no 'ds-', 'fdp-' or 'unity-' prefix (selector '{Shorten(rule.Selector)}').");
                    }
                }
            }
        }

        /// <summary>The <c>resource("...")</c> paths a sheet uses (path as written, without extension).</summary>
        public static IEnumerable<(string path, int line)> ResourceReferences(string text)
        {
            foreach (var rule in UssParser.Parse(text))
                foreach (var d in rule.Declarations)
                    foreach (Match m in ResourceCall.Matches(d.Value))
                        yield return (m.Groups[2].Value, d.Line);
        }

        /// <summary>The relative <c>@import url("...")</c> targets of a sheet.</summary>
        public static IEnumerable<(string path, int line)> ImportReferences(string text)
        {
            foreach (var rule in UssParser.Parse(text))
            {
                if (!rule.IsAtRule) continue;
                var m = ImportUrl.Match(rule.Selector);
                if (m.Success) yield return (m.Groups[2].Value, rule.Line);
            }
        }

        private static bool IsOwned(string className)
        {
            foreach (string p in OwnedClassPrefixes)
                if (className.StartsWith(p, StringComparison.Ordinal)) return true;
            return false;
        }

        private static IEnumerable<string> Compounds(string selector)
        {
            // A compound selector is what sits between whitespace, '>' and ',' (no sibling combinators here: another rule flags those).
            foreach (string part in Regex.Split(selector, @"[\s>,]+"))
                if (part.Length > 0) yield return part;
        }

        private static string StripStrings(string s) => Regex.Replace(s, @"(['""]).*?\1", "\"\"");

        private static string FirstWord(string s)
        {
            int end = 0;
            while (end < s.Length && !char.IsWhiteSpace(s[end])) end++;
            return s.Substring(0, end);
        }

        private static string Shorten(string s)
        {
            s = Regex.Replace(s, @"\s+", " ");
            return s.Length <= 80 ? s : s.Substring(0, 77) + "...";
        }
    }
}
