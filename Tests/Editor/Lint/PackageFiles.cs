using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
#if !UNITY_EDITOR
using System.Reflection;
#endif

namespace FutureDigitalPlus.UIKit.Lint
{
    /// <summary>
    /// Read access to the kit's own files for the lint tests. In Unity the root comes from <c>KitLocation</c> (a UPM package or a plain
    /// folder below Assets, same source for both); in the dotnet runner
    /// (<c>Tools~/CompileCheck/LintRunner</c>) from the assembly metadata <c>PackageRoot</c> or the environment variable <c>FDP_PACKAGE_ROOT</c>.
    /// </summary>
    internal static class PackageFiles
    {
        private static string _root;
        private static List<string> _paths;

        /// <summary>Absolute path of the package folder, or null when it cannot be found.</summary>
        public static string Root
        {
            get
            {
                if (_root != null) return _root;
#if UNITY_EDITOR
                _root = FutureDigitalPlus.UIKit.Location.KitLocation.Install == FutureDigitalPlus.UIKit.Location.KitInstall.NotFound
                    ? null
                    : FutureDigitalPlus.UIKit.Location.KitLocation.FullPath;
#else
                _root = Environment.GetEnvironmentVariable("FDP_PACKAGE_ROOT");
                if (string.IsNullOrEmpty(_root))
                {
                    foreach (var attr in typeof(PackageFiles).Assembly.GetCustomAttributes<AssemblyMetadataAttribute>())
                        if (attr.Key == "PackageRoot") _root = attr.Value;
                }
#endif
                if (!string.IsNullOrEmpty(_root)) _root = Path.GetFullPath(_root).TrimEnd('/', '\\');
                return _root;
            }
        }

        /// <summary>Package relative paths (forward slashes, no trailing slash) of every file and folder below the root, hidden folders included.</summary>
        public static IReadOnlyList<string> AllPaths
        {
            get
            {
                if (_paths != null) return _paths;

                var list = new List<string>();
                string root = Root;
                if (root != null)
                {
                    int cut = root.Length + 1;
                    foreach (string entry in Directory.EnumerateFileSystemEntries(root, "*", SearchOption.AllDirectories))
                    {
                        string rel = entry.Substring(cut).Replace('\\', '/');
                        // Build output of the tools in Tools~ (bin, obj, out) is not package content.
                        if (rel.StartsWith("Tools~/", StringComparison.Ordinal) && IsBuildOutput(rel)) continue;
                        list.Add(rel);
                    }
                }

                _paths = list;
                return _paths;
            }
        }

        /// <summary>Package relative paths of the files only.</summary>
        public static IEnumerable<string> Files => AllPaths.Where(p => File.Exists(Path.Combine(Root, p)));

        /// <summary>Package relative files that match a suffix and sit below one of the given folders.</summary>
        public static IEnumerable<string> FilesUnder(string extension, params string[] folders) =>
            Files.Where(p => p.EndsWith(extension, StringComparison.Ordinal) && folders.Any(f => p.StartsWith(f + "/", StringComparison.Ordinal)));

        private static bool IsBuildOutput(string rel)
        {
            foreach (string part in rel.Split('/'))
                if (part == "bin" || part == "obj" || part == "out") return true;
            return false;
        }

        public static string Read(string relative) => File.ReadAllText(Path.Combine(Root, relative));

        public static bool FileExists(string relative) => File.Exists(Path.Combine(Root, relative));
    }
}
