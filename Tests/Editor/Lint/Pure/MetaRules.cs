using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace FutureDigitalPlus.UIKit.Lint
{
    /// <summary>Lint rules for Unity <c>.meta</c> files of a package that is committed with them (so GUIDs stay stable for consumers).</summary>
    public static class MetaRules
    {
        private static readonly Regex GuidLine = new Regex(@"^guid:\s*([0-9a-fA-F]{32})\s*$", RegexOptions.Multiline | RegexOptions.Compiled);

        /// <summary>
        /// True for paths Unity does not import: a segment that ends with <c>~</c> or starts with <c>.</c>, or a file ending with <c>~</c>.
        /// (<c>Samples~</c>, <c>Documentation~</c>, <c>Tools~</c>, <c>.gitkeep</c>.)
        /// </summary>
        public static bool IsHidden(string path)
        {
            foreach (string part in path.Split('/'))
                if (part.Length > 0 && (part.EndsWith("~", StringComparison.Ordinal) || part[0] == '.')) return true;
            return false;
        }

        /// <summary>
        /// Every visible file and folder needs a <c>.meta</c>, every <c>.meta</c> needs its file or folder, and nothing hidden may have one.
        /// </summary>
        /// <param name="paths">Package relative paths of all files and folders (forward slashes, no trailing slash), meta files included.</param>
        public static IEnumerable<LintFinding> Check(IEnumerable<string> paths)
        {
            var all = new HashSet<string>(paths, StringComparer.Ordinal);

            foreach (string p in all)
            {
                bool isMeta = p.EndsWith(".meta", StringComparison.Ordinal);

                if (isMeta)
                {
                    string target = p.Substring(0, p.Length - ".meta".Length);
                    if (IsHidden(target))
                        yield return new LintFinding("meta-in-hidden-folder", p, 0, "Hidden folders (ending with '~') and dot files must not have a .meta file.");
                    else if (!all.Contains(target))
                        yield return new LintFinding("meta-orphan", p, 0, "The file or folder this .meta belongs to does not exist.");
                    continue;
                }

                if (IsHidden(p)) continue;
                if (!all.Contains(p + ".meta"))
                    yield return new LintFinding("meta-missing", p, 0, "No .meta file (let Unity generate it; do not write it by hand).");
            }
        }

        /// <summary>The <c>guid:</c> of a <c>.meta</c> file's text, or null when it has none.</summary>
        public static string ParseGuid(string metaText)
        {
            var m = GuidLine.Match(metaText ?? string.Empty);
            return m.Success ? m.Groups[1].Value.ToLowerInvariant() : null;
        }

        /// <summary>Two <c>.meta</c> files with the same GUID, or one without a GUID.</summary>
        /// <param name="metas">Package relative path of each <c>.meta</c> file with its text.</param>
        public static IEnumerable<LintFinding> DuplicateGuids(IEnumerable<KeyValuePair<string, string>> metas)
        {
            var first = new Dictionary<string, string>(StringComparer.Ordinal);

            foreach (var kv in metas)
            {
                string guid = ParseGuid(kv.Value);
                if (guid == null)
                {
                    yield return new LintFinding("meta-no-guid", kv.Key, 0, "The .meta file has no 'guid:' line.");
                    continue;
                }

                if (first.TryGetValue(guid, out string other))
                    yield return new LintFinding("meta-duplicate-guid", kv.Key, 0, $"GUID {guid} is also used by {other}.");
                else
                    first[guid] = kv.Key;
            }
        }
    }
}
