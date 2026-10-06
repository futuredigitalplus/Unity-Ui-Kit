using System;
using System.Collections.Generic;

namespace FutureDigitalPlus.UIKit.DesignSystem.Typography
{
    /// <summary>
    /// Which operating-system font families to try for which script, and in which order. Pure data and logic (no UnityEngine), so it
    /// is unit-tested without an editor. Only family NAMES are listed: the fonts themselves belong to the OS and are never copied
    /// into a game. The caller filters by the families that are really installed.
    /// <para>
    /// Two kinds of script. Han, kana and Hangul need no shaping, so any system font is a valid fallback; the language of the player
    /// decides which Han typeface leads (Han unification). Arabic, Hebrew, Thai and the Indic scripts must be SHAPED, so a system font
    /// only helps when the text generator can shape it; <see cref="DsOsFonts"/> checks that before it uses one.
    /// </para>
    /// </summary>
    public static class DsOsFontOrder
    {
        /// <summary>Japanese group key.</summary>
        public const string Japanese = "ja";

        /// <summary>Korean group key.</summary>
        public const string Korean = "ko";

        /// <summary>Simplified Chinese group key.</summary>
        public const string ChineseSimplified = "zh-Hans";

        /// <summary>Traditional Chinese group key.</summary>
        public const string ChineseTraditional = "zh-Hant";

        /// <summary>The scripts that need shaping and have a system-font candidate list.</summary>
        public static readonly IReadOnlyList<string> ShapingScripts =
            new[] { "arabic", "hebrew", "thai", "devanagari", "bengali", "tamil", "khmer" };

        private static readonly (string group, string[] families)[] HanGroups =
        {
            (ChineseSimplified, new[] { "PingFang SC", "Noto Sans CJK SC", "Noto Sans SC", "Source Han Sans SC", "Hiragino Sans GB", "Heiti SC", "STHeiti", "Microsoft YaHei", "Droid Sans Fallback" }),
            (ChineseTraditional, new[] { "PingFang TC", "PingFang HK", "Noto Sans CJK TC", "Noto Sans TC", "Source Han Sans TC", "Heiti TC", "Microsoft JhengHei" }),
            (Japanese, new[] { "Hiragino Sans", "Hiragino Kaku Gothic ProN", "Noto Sans CJK JP", "Noto Sans JP", "Source Han Sans JP", "Yu Gothic", "Meiryo" }),
            (Korean, new[] { "Apple SD Gothic Neo", "Noto Sans CJK KR", "Noto Sans KR", "Source Han Sans KR", "Malgun Gothic" }),
        };

        private static readonly (string script, string[] families)[] ShapingFamilies =
        {
            ("arabic", new[] { "Geeza Pro", "Noto Sans Arabic", "Noto Naskh Arabic", "Segoe UI", "Tahoma" }),
            ("hebrew", new[] { "Arial Hebrew", "Noto Sans Hebrew", "Segoe UI", "Arial" }),
            ("thai", new[] { "Thonburi", "Noto Sans Thai", "Leelawadee UI", "Tahoma" }),
            ("devanagari", new[] { "Kohinoor Devanagari", "Devanagari Sangam MN", "Noto Sans Devanagari", "Nirmala UI" }),
            ("bengali", new[] { "Bangla Sangam MN", "Noto Sans Bengali", "Nirmala UI" }),
            ("tamil", new[] { "Tamil Sangam MN", "Noto Sans Tamil", "Nirmala UI" }),
            ("khmer", new[] { "Khmer Sangam MN", "Noto Sans Khmer", "Leelawadee UI", "Khmer UI" }),
        };

        /// <summary>
        /// Unity lists system fonts as <c>Family - Style</c> (on macOS <c>Hiragino Sans - W3</c>). Groups the names as family to its
        /// styles. A name without <c> - </c> is a family with the style <c>Regular</c>.
        /// </summary>
        public static Dictionary<string, List<string>> GroupByFamily(IEnumerable<string> systemFontNames)
        {
            var result = new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase);
            if (systemFontNames == null) return result;

            foreach (var name in systemFontNames)
            {
                if (string.IsNullOrWhiteSpace(name)) continue;

                var cut = name.LastIndexOf(" - ", StringComparison.Ordinal);
                var family = cut > 0 ? name.Substring(0, cut).Trim() : name.Trim();
                var style = cut > 0 ? name.Substring(cut + 3).Trim() : "Regular";

                if (!result.TryGetValue(family, out var styles)) result[family] = styles = new List<string>();
                styles.Add(style);
            }

            return result;
        }

        /// <summary>
        /// The regular-weight style among a family's styles: <c>Regular</c>, else the names CJK families use for it (W3, Medium, Light,
        /// Normal), else the first one.
        /// </summary>
        public static string PickStyle(IList<string> styles)
        {
            if (styles == null || styles.Count == 0) return "Regular";

            foreach (var wanted in new[] { "Regular", "W3", "Medium", "Light", "Normal" })
                foreach (var style in styles)
                    if (string.Equals(style, wanted, StringComparison.OrdinalIgnoreCase)) return style;

            return styles[0];
        }

        /// <summary>
        /// The Han group a locale belongs to (<c>ja</c>, <c>ko</c>, <c>zh-Hant</c>, every other Chinese is <c>zh-Hans</c>), or null for a
        /// language without Han characters.
        /// </summary>
        public static string HanGroupOf(string localeCode)
        {
            if (string.IsNullOrEmpty(localeCode)) return null;

            var code = localeCode.Trim();
            if (LanguageIs(code, "ja")) return Japanese;
            if (LanguageIs(code, "ko")) return Korean;
            if (!LanguageIs(code, "zh")) return null;

            return code.IndexOf("Hant", StringComparison.OrdinalIgnoreCase) >= 0 ||
                   code.EndsWith("-TW", StringComparison.OrdinalIgnoreCase) ||
                   code.EndsWith("-HK", StringComparison.OrdinalIgnoreCase) ||
                   code.EndsWith("-MO", StringComparison.OrdinalIgnoreCase)
                ? ChineseTraditional
                : ChineseSimplified;
        }

        /// <summary>
        /// Candidate Han families in fallback order: the locale's own group first, then the others, each group in its own preference
        /// order; only families in <paramref name="installed"/> (case-insensitive), each at most once.
        /// </summary>
        public static List<string> HanCandidates(string localeCode, ICollection<string> installed)
        {
            var result = new List<string>();
            var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var have = new HashSet<string>(installed ?? Array.Empty<string>(), StringComparer.OrdinalIgnoreCase);

            var first = HanGroupOf(localeCode);
            if (first != null) AddHanGroup(first, have, seen, result);
            foreach (var (group, _) in HanGroups) AddHanGroup(group, have, seen, result);
            return result;
        }

        /// <summary>
        /// The shaping script of a locale (<c>arabic</c>, <c>hebrew</c>, <c>thai</c>, <c>devanagari</c>, <c>bengali</c>, <c>tamil</c>,
        /// <c>khmer</c>), or null when its language needs none of them.
        /// </summary>
        public static string ShapingScriptOf(string localeCode)
        {
            if (string.IsNullOrEmpty(localeCode)) return null;

            var code = localeCode.Trim();
            foreach (var language in new[] { "ar", "fa", "ur", "ps", "sd", "ug", "ckb" })
                if (LanguageIs(code, language)) return "arabic";
            foreach (var language in new[] { "he", "yi", "iw" })
                if (LanguageIs(code, language)) return "hebrew";
            if (LanguageIs(code, "th")) return "thai";
            foreach (var language in new[] { "hi", "mr", "ne", "sa", "kok", "mai" })
                if (LanguageIs(code, language)) return "devanagari";
            foreach (var language in new[] { "bn", "as" })
                if (LanguageIs(code, language)) return "bengali";
            if (LanguageIs(code, "ta")) return "tamil";
            if (LanguageIs(code, "km")) return "khmer";
            return null;
        }

        /// <summary>The installed families to try for a shaping script, in preference order, each once; empty for an unknown script.</summary>
        public static List<string> ShapingCandidates(string script, ICollection<string> installed)
        {
            var result = new List<string>();
            if (string.IsNullOrEmpty(script)) return result;

            var have = new HashSet<string>(installed ?? Array.Empty<string>(), StringComparer.OrdinalIgnoreCase);
            foreach (var (name, families) in ShapingFamilies)
            {
                if (name != script) continue;

                foreach (var family in families)
                    if (have.Contains(family) && !result.Contains(family)) result.Add(family);
            }

            return result;
        }

        private static void AddHanGroup(string group, HashSet<string> have, HashSet<string> seen, List<string> result)
        {
            foreach (var (name, families) in HanGroups)
            {
                if (name != group) continue;

                foreach (var family in families)
                    if (have.Contains(family) && seen.Add(family)) result.Add(family);
            }
        }

        private static bool LanguageIs(string code, string language) =>
            code.StartsWith(language, StringComparison.OrdinalIgnoreCase) &&
            (code.Length == language.Length || code[language.Length] == '-' || code[language.Length] == '_');
    }
}
