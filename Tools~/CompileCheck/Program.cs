// Tools~/CompileCheck: compile all assemblies of the package with Roslyn against Unity's reference assemblies (no Unity process).
// usage: compile-check <plan.json> [--docs <doc-scope.txt>] [--hazards] [--dump-generated <dir>] [--package-root <dir>]
// The plan comes from plan.py. Exit code 0 = no error, no doc finding, no name hazard; 1 otherwise.
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.Json;
using FutureDigitalPlus.UIKit.Lint;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

internal static class Program
{
    private static int Main(string[] args)
    {
        if (args.Length == 0) { Console.Error.WriteLine("usage: compile-check <plan.json> [--docs <doc-scope.txt>] [--hazards] [--dump-generated <dir>] [--package-root <dir>]"); return 2; }

        string Option(string name) { int i = Array.IndexOf(args, name); return i >= 0 && i + 1 < args.Length ? args[i + 1] : null; }

        var plan = JsonDocument.Parse(File.ReadAllText(args[0])).RootElement;
        string docsFile = Option("--docs");
        bool hazards = args.Contains("--hazards");
        string dump = Option("--dump-generated");
        string packageRoot = Path.GetFullPath(Option("--package-root") ?? plan.GetProperty("packageRoot").GetString());

        var docScope = docsFile == null ? new List<string>() : File.ReadAllLines(docsFile)
            .Select(l => l.Trim()).Where(l => l.Length > 0 && !l.StartsWith("#")).ToList();

        var baseRefs = plan.GetProperty("baseRefs").EnumerateArray().Select(e => (MetadataReference)MetadataReference.CreateFromFile(e.GetString())).ToList();
        var editorRefs = plan.GetProperty("editorRefs").EnumerateArray().Select(e => (MetadataReference)MetadataReference.CreateFromFile(e.GetString())).ToList();
        var built = new Dictionary<string, MetadataReference>();

        int errors = 0;
        int docFindings = 0;
        var kitNamespaces = new HashSet<string>();
        var kitPublicTypes = new HashSet<string>();
        var unityTypeNames = new HashSet<string>();

        if (hazards)
        {
            // Public top-level type names of every UnityEngine* and UnityEditor* assembly (what a loaded Unity editor has).
            var unityComp = CSharpCompilation.Create("UnityTypes", references: baseRefs.Concat(editorRefs));
            foreach (var r in baseRefs.Concat(editorRefs))
                if (unityComp.GetAssemblyOrModuleSymbol(r) is IAssemblySymbol asm && (asm.Name.StartsWith("UnityEngine") || asm.Name.StartsWith("UnityEditor")))
                    CollectUnityTypes(asm.GlobalNamespace, unityTypeNames);
        }

        foreach (var a in plan.GetProperty("assemblies").EnumerateArray())
        {
            string name = a.GetProperty("name").GetString();
            string kind = a.GetProperty("kind").GetString();
            var defines = a.GetProperty("defines").EnumerateArray().Select(e => e.GetString()).ToArray();

            var parse = CSharpParseOptions.Default.WithLanguageVersion(LanguageVersion.CSharp9).WithPreprocessorSymbols(defines)
                .WithDocumentationMode(docScope.Count > 0 ? DocumentationMode.Diagnose : DocumentationMode.None);
            var trees = a.GetProperty("files").EnumerateArray()
                .Select(f => CSharpSyntaxTree.ParseText(File.ReadAllText(f.GetString()), parse, f.GetString())).ToList();

            var refs = new List<MetadataReference>(baseRefs);
            if (a.GetProperty("editor").GetBoolean()) refs.AddRange(editorRefs);
            foreach (var r in a.GetProperty("refs").EnumerateArray())
            {
                string rn = r.GetString();
                if (built.TryGetValue(rn, out var m)) refs.Add(m);
                else if (File.Exists(rn)) refs.Add(MetadataReference.CreateFromFile(rn));
                else Console.WriteLine($"  [{name}] missing reference {rn}");
            }

            var options = new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary, allowUnsafe: a.GetProperty("unsafe").GetBoolean(),
                nullableContextOptions: NullableContextOptions.Disable);
            var comp = CSharpCompilation.Create(name, trees, refs, options);

            int generated = 0;
            string genDir = a.GetProperty("generatorDir").GetString();
            if (!string.IsNullOrEmpty(genDir))
            {
                var dll = Directory.GetFiles(genDir, "*Roslyn.Generation.dll").First();
                var generators = Assembly.LoadFrom(dll).GetTypes()
                    .Where(t => typeof(ISourceGenerator).IsAssignableFrom(t) && !t.IsAbstract)
                    .Select(t => (ISourceGenerator)Activator.CreateInstance(t)).ToArray();
                var driver = CSharpGeneratorDriver.Create(generators, parseOptions: parse);
                driver = (CSharpGeneratorDriver)driver.RunGeneratorsAndUpdateCompilation(comp, out var outComp, out _);
                generated = driver.GetRunResult().GeneratedTrees.Length;
                if (!string.IsNullOrEmpty(dump))
                    foreach (var gt in driver.GetRunResult().GeneratedTrees)
                    {
                        var dir = Path.Combine(dump, name);
                        Directory.CreateDirectory(dir);
                        File.WriteAllText(Path.Combine(dir, Path.GetFileName(gt.FilePath)), gt.GetText().ToString());
                    }
                comp = (CSharpCompilation)outComp;
            }

            var all = comp.GetDiagnostics();
            var errs = all.Where(d => d.Severity == DiagnosticSeverity.Error).ToList();
            int warnings = all.Count(d => d.Severity == DiagnosticSeverity.Warning && !IsDocDiagnostic(d.Id));

            // XML documentation findings, only for the files in scope.
            var docs = new List<Diagnostic>();
            if (docScope.Count > 0)
                foreach (var d in all.Where(d => IsDocDiagnostic(d.Id) && d.Location.IsInSource))
                    if (InScope(RelativePath(packageRoot, d.Location.SourceTree.FilePath), docScope)) docs.Add(d);

            Console.WriteLine($"{(errs.Count == 0 && docs.Count == 0 ? "OK  " : "FAIL")} {name,-62} files={trees.Count,3} generated={generated,3} errors={errs.Count} warnings={warnings} docs={docs.Count}");
            foreach (var d in errs.Take(8)) Console.WriteLine("      " + Shorten(d.ToString()));
            foreach (var d in docs.Take(12)) Console.WriteLine("      " + Shorten(d.ToString()));
            if (docs.Count > 12) Console.WriteLine($"      ... {docs.Count - 12} more doc findings");

            errors += errs.Count;
            docFindings += docs.Count;

            if (hazards && (kind == "runtime" || kind == "editor"))
                CollectKitNames(comp.Assembly.GlobalNamespace, kitNamespaces, kitPublicTypes);

            built[name] = comp.ToMetadataReference();

        }

        int hazardCount = 0;
        if (hazards)
        {
            var found = NameHazardRules.Find(kitNamespaces, kitPublicTypes, unityTypeNames, new HashSet<string>(LintAllowList.NameHazards)).ToList();
            Console.WriteLine($"Name hazards: {found.Count} (kit namespaces {kitNamespaces.Count}, public types {kitPublicTypes.Count}, Unity type names {unityTypeNames.Count}, allow-list {LintAllowList.NameHazards.Length})");
            foreach (var f in found) Console.WriteLine("      " + f);
            hazardCount = found.Count;
        }

        Console.WriteLine($"TOTAL errors: {errors}, doc findings: {docFindings}, name hazards: {hazardCount}");
        return errors + docFindings + hazardCount == 0 ? 0 : 1;
    }

    // CS1570..CS1592: malformed or missing XML documentation comments.
    private static bool IsDocDiagnostic(string id) =>
        id.StartsWith("CS15", StringComparison.Ordinal) && int.TryParse(id.Substring(2), out int n) && n >= 1570 && n <= 1592;

    private static string RelativePath(string root, string file)
    {
        string full = Path.GetFullPath(file);
        return full.StartsWith(root, StringComparison.Ordinal) ? full.Substring(root.Length).TrimStart('/', '\\').Replace('\\', '/') : full;
    }

    // A line "!suffix" excludes files that end with it; every other line is a path prefix that includes.
    private static bool InScope(string relative, List<string> scope) =>
        scope.Any(p => !p.StartsWith("!") && relative.StartsWith(p, StringComparison.Ordinal))
        && !scope.Any(p => p.StartsWith("!") && relative.EndsWith(p.Substring(1), StringComparison.Ordinal));

    private static string Shorten(string s) => s.Length <= 300 ? s : s.Substring(0, 300) + "...";

    private static void CollectKitNames(INamespaceSymbol ns, HashSet<string> namespaces, HashSet<string> publicTypes)
    {
        bool hasTypes = false;
        foreach (var t in ns.GetTypeMembers())
        {
            hasTypes = true;
            if (t.DeclaredAccessibility == Accessibility.Public)
                publicTypes.Add(t.ContainingNamespace.ToDisplayString() + "." + t.MetadataName);
        }

        if (hasTypes && !ns.IsGlobalNamespace) namespaces.Add(ns.ToDisplayString());
        foreach (var child in ns.GetNamespaceMembers()) CollectKitNames(child, namespaces, publicTypes);
    }

    private static void CollectUnityTypes(INamespaceSymbol ns, HashSet<string> names)
    {
        foreach (var t in ns.GetTypeMembers())
            if (t.DeclaredAccessibility == Accessibility.Public)
            {
                string n = t.Name;
                names.Add(n);
            }
        foreach (var child in ns.GetNamespaceMembers()) CollectUnityTypes(child, names);
    }
}
