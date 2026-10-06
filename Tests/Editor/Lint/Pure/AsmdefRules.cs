using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace FutureDigitalPlus.UIKit.Lint
{
    /// <summary>
    /// Lint rules for the assembly definitions of the package: the kit depends on nothing outside Unity and itself, every optional integration sits behind
    /// a define constraint (so the package compiles with none of the optional packages installed), and the pure layers have no engine reference.
    /// </summary>
    public static class AsmdefRules
    {
        private static readonly Regex NameField = new Regex("\"name\"\\s*:\\s*\"([^\"]+)\"", RegexOptions.Compiled);
        private static readonly Regex NoEngine = new Regex("\"noEngineReferences\"\\s*:\\s*true", RegexOptions.Compiled);

        /// <summary>References that only exist when an optional package is installed. An assembly that references one must be constrained by a define.</summary>
        public static readonly string[] OptionalPackageAssemblies =
        {
            "Unity.Addressables", "Unity.ResourceManager", "Unity.InputSystem", "Unity.Localization", "Unity.TextMeshPro", "Unity.Cinemachine",
            "UnityEngine.UI", "Unity.RenderPipelines.Core.Runtime", "Unity.RenderPipelines.Universal.Runtime", "Unity.RenderPipelines.HighDefinition.Runtime",
        };

        /// <summary>The assembly name of an asmdef text, or null.</summary>
        public static string NameOf(string json)
        {
            var m = NameField.Match(json);
            return m.Success ? m.Groups[1].Value : null;
        }

        /// <summary>The strings of a top level array field such as <c>references</c> or <c>defineConstraints</c>; empty when the field is absent.</summary>
        public static List<string> StringArray(string json, string field)
        {
            var result = new List<string>();
            var m = Regex.Match(json, "\"" + Regex.Escape(field) + "\"\\s*:\\s*\\[(.*?)\\]", RegexOptions.Singleline);
            if (!m.Success) return result;
            foreach (Match s in Regex.Matches(m.Groups[1].Value, "\"([^\"]*)\""))
                result.Add(s.Groups[1].Value);
            return result;
        }

        /// <summary>Checks one asmdef. <paramref name="path"/> is only used in the findings.</summary>
        public static IEnumerable<LintFinding> Check(string path, string json)
        {
            string name = NameOf(json);
            var references = StringArray(json, "references");
            var constraints = StringArray(json, "defineConstraints");

            foreach (string reference in references)
            {
                bool ours = reference.StartsWith("FutureDigitalPlus.UIKit", StringComparison.Ordinal);
                bool unity = reference.StartsWith("Unity.", StringComparison.Ordinal) || reference.StartsWith("UnityEngine.", StringComparison.Ordinal)
                    || reference.StartsWith("UnityEditor.", StringComparison.Ordinal) || reference.StartsWith("GUID:", StringComparison.Ordinal);
                if (!ours && !unity)
                    yield return new LintFinding("asmdef-foreign-reference", path, 0, $"'{name}' references '{reference}': the kit may depend on Unity and on itself only (no game code).");
            }

            if (constraints.Count == 0)
            {
                foreach (string reference in references)
                    if (Array.IndexOf(OptionalPackageAssemblies, reference) >= 0)
                        yield return new LintFinding("asmdef-optional-unconstrained", path, 0,
                            $"'{name}' references '{reference}', which exists only when an optional package is installed, but has no defineConstraints: the package would not compile without it.");
            }

            if (name != null && name.EndsWith(".Core", StringComparison.Ordinal))
            {
                if (!NoEngine.IsMatch(json))
                    yield return new LintFinding("asmdef-core-engine", path, 0, $"'{name}' is a pure layer and must set noEngineReferences to true.");
                foreach (string reference in references)
                    if (!reference.EndsWith(".Core", StringComparison.Ordinal))
                        yield return new LintFinding("asmdef-core-engine", path, 0, $"'{name}' is a pure layer and references '{reference}', which is not a Core assembly.");
            }
        }
    }
}
