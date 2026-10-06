using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace FutureDigitalPlus.UIKit.Lint
{
    /// <summary>
    /// What <c>Resources.Load</c> can find in the package: the paths below any folder called <c>Resources</c>, without file extension.
    /// </summary>
    public sealed class ResourceIndex
    {
        private readonly HashSet<string> _assets = new HashSet<string>(StringComparer.Ordinal);
        private readonly HashSet<string> _folders = new HashSet<string>(StringComparer.Ordinal);

        /// <summary>Builds the index from package relative file paths (forward slashes). <c>.meta</c> files and files in hidden folders are ignored.</summary>
        public ResourceIndex(IEnumerable<string> packageFiles)
        {
            foreach (string file in packageFiles)
            {
                if (file.EndsWith(".meta", StringComparison.Ordinal)) continue;
                if (MetaRules.IsHidden(file)) continue;

                string[] parts = file.Split('/');
                for (int i = 0; i < parts.Length - 1; i++)
                {
                    if (parts[i] != "Resources") continue;

                    // Everything after this Resources folder, joined; the last segment loses its extension.
                    var rest = new List<string>();
                    for (int j = i + 1; j < parts.Length; j++) rest.Add(parts[j]);

                    string last = rest[rest.Count - 1];
                    int dot = last.LastIndexOf('.');
                    if (dot > 0) rest[rest.Count - 1] = last.Substring(0, dot);

                    _assets.Add(string.Join("/", rest));
                    for (int k = 1; k < rest.Count; k++) _folders.Add(string.Join("/", rest.GetRange(0, k)));
                }
            }
        }

        /// <summary>True when <c>Resources.Load(path)</c> would find an asset (extension-less path).</summary>
        public bool HasAsset(string path) => _assets.Contains(Normalize(path));

        /// <summary>True when <c>Resources.LoadAll(path)</c> would find a folder.</summary>
        public bool HasFolder(string path) => _folders.Contains(Normalize(path));

        private static string Normalize(string path) => (path ?? string.Empty).Replace('\\', '/').Trim('/');
    }

    /// <summary>One literal passed to <c>Resources.Load*</c>.</summary>
    public readonly struct ResourceLiteral
    {
        /// <summary>The path, as written.</summary>
        public string Path { get; }

        /// <summary>1 based line.</summary>
        public int Line { get; }

        /// <summary>True for <c>Resources.LoadAll</c>, which takes a folder.</summary>
        public bool IsFolder { get; }

        /// <summary>Creates a literal record.</summary>
        public ResourceLiteral(string path, int line, bool isFolder)
        {
            Path = path;
            Line = line;
            IsFolder = isFolder;
        }
    }

    /// <summary>Lint rules for resource paths in C# and USS.</summary>
    public static class ResourceRules
    {
        private static readonly Regex LoadCall = new Regex(
            @"\bResources\s*\.\s*(Load|LoadAsync|LoadAll)\s*(?:<[^>()]*>)?\s*\(\s*(?:""((?:[^""\\]|\\.)*)""|([A-Za-z_][A-Za-z0-9_.]*)\s*[,)])",
            RegexOptions.Compiled);

        private static readonly Regex ConstString = new Regex(
            @"\bconst\s+string\s+([A-Za-z_][A-Za-z0-9_]*)\s*=\s*""((?:[^""\\]|\\.)*)""", RegexOptions.Compiled);

        /// <summary>
        /// The string literals passed to <c>Resources.Load</c>, <c>LoadAsync</c> and <c>LoadAll</c>. An identifier argument is resolved when it is a
        /// <c>const string</c> declared in <paramref name="constants"/> (name to value, built with <see cref="ConstStrings"/>); other expressions are skipped.
        /// </summary>
        public static IEnumerable<ResourceLiteral> LoadLiterals(string csText, IReadOnlyDictionary<string, string> constants = null)
        {
            foreach (Match m in LoadCall.Matches(csText))
            {
                string path = null;
                if (m.Groups[2].Success) path = m.Groups[2].Value;
                else if (m.Groups[3].Success && constants != null)
                {
                    string id = m.Groups[3].Value;
                    int dot = id.LastIndexOf('.');
                    if (dot >= 0) id = id.Substring(dot + 1);
                    constants.TryGetValue(id, out path);
                }

                if (path == null) continue;

                int line = 1;
                for (int i = 0; i < m.Index; i++) if (csText[i] == '\n') line++;
                yield return new ResourceLiteral(path, line, m.Groups[1].Value == "LoadAll");
            }
        }

        /// <summary>
        /// The <c>const string</c> declarations of a C# file as name to value. A name declared twice with different values is dropped
        /// (the lint cannot know which one a call site means).
        /// </summary>
        public static void ConstStrings(string csText, IDictionary<string, string> into, ISet<string> ambiguous)
        {
            foreach (Match m in ConstString.Matches(csText))
            {
                string name = m.Groups[1].Value;
                string value = m.Groups[2].Value;
                if (ambiguous.Contains(name)) continue;

                if (into.TryGetValue(name, out string existing) && existing != value)
                {
                    into.Remove(name);
                    ambiguous.Add(name);
                }
                else
                {
                    into[name] = value;
                }
            }
        }

        /// <summary>Findings for every <c>Resources.Load*</c> literal of <paramref name="csText"/> that no <c>Resources</c> folder of the package answers.</summary>
        public static IEnumerable<LintFinding> UnresolvedLoads(string file, string csText, IReadOnlyDictionary<string, string> constants, ResourceIndex index)
        {
            foreach (var lit in LoadLiterals(csText, constants))
            {
                bool ok = lit.IsFolder ? index.HasFolder(lit.Path) : index.HasAsset(lit.Path);
                if (!ok)
                    yield return new LintFinding("resource-unresolved", file, lit.Line, $"Resources.Load{(lit.IsFolder ? "All" : "")}(\"{lit.Path}\") finds nothing in the package.");
            }
        }

        /// <summary>Findings for every <c>resource("...")</c> and relative <c>@import url("...")</c> of a sheet that does not resolve.</summary>
        /// <param name="file">Package relative path of the sheet, for the finding.</param>
        /// <param name="sheetFolder">Package relative folder of the sheet, with no trailing slash.</param>
        /// <param name="ussText">The sheet's text.</param>
        /// <param name="index">The resources the package contains.</param>
        /// <param name="fileExists">Answers whether a package relative file path exists.</param>
        public static IEnumerable<LintFinding> UnresolvedSheetReferences(string file, string sheetFolder, string ussText, ResourceIndex index, Func<string, bool> fileExists)
        {
            foreach (var (path, line) in UssRules.ResourceReferences(ussText))
                if (!index.HasAsset(path))
                    yield return new LintFinding("resource-unresolved", file, line, $"resource(\"{path}\") finds nothing in the package.");

            foreach (var (path, line) in UssRules.ImportReferences(ussText))
            {
                if (path.Contains("://") || path.StartsWith("/", StringComparison.Ordinal)) continue;
                if (!fileExists(CombineAndNormalize(sheetFolder, path)))
                    yield return new LintFinding("resource-unresolved", file, line, $"@import url(\"{path}\") finds no file next to the sheet.");
            }
        }

        /// <summary>Joins a folder and a relative path (with <c>..</c> and <c>.</c> segments) into a package relative path.</summary>
        public static string CombineAndNormalize(string folder, string relative)
        {
            var stack = new List<string>();
            foreach (string part in (folder + "/" + relative).Replace('\\', '/').Split('/'))
            {
                if (part.Length == 0 || part == ".") continue;
                if (part == "..") { if (stack.Count > 0) stack.RemoveAt(stack.Count - 1); }
                else stack.Add(part);
            }

            return string.Join("/", stack);
        }
    }
}
