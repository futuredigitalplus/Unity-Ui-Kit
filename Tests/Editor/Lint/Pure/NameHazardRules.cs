using System;
using System.Collections.Generic;

namespace FutureDigitalPlus.UIKit.Lint
{
    /// <summary>
    /// Finds names of the kit that equal a Unity type name. A namespace segment that equals a type name (<c>Input</c>, <c>Layout</c>, <c>Scale</c>, <c>Motion</c>,
    /// <c>Platform</c>, <c>Navigation</c>, <c>Console</c>) hides that type in every file of the parent namespace (CS0234 or a silent change of meaning),
    /// and a public type with a Unity type's name forces every consumer that imports both namespaces to alias one of them.
    /// </summary>
    public static class NameHazardRules
    {
        /// <summary>The root namespace of the kit.</summary>
        public const string RootNamespace = "FutureDigitalPlus.UIKit";

        /// <summary>Finds the hazards.</summary>
        /// <param name="namespaces">Every namespace that has at least one public type of the kit (for example <c>FutureDigitalPlus.UIKit.Foundation</c>).</param>
        /// <param name="publicTypes">Full names of the kit's public top-level types, with namespace; generic arity suffixes (<c>`1</c>) are allowed.</param>
        /// <param name="unityTypeNames">Simple names of the public top-level types of the Unity assemblies that are loaded.</param>
        /// <param name="allowed">Accepted findings (reasons in the allow-list file): a namespace such as <c>FutureDigitalPlus.UIKit.Animation</c> accepts that
        /// segment in it and in every namespace below it; a full type name accepts that one type.</param>
        public static IEnumerable<LintFinding> Find(IEnumerable<string> namespaces, IEnumerable<string> publicTypes, ISet<string> unityTypeNames, ISet<string> allowed)
        {
            var seenSegments = new HashSet<string>(StringComparer.Ordinal);

            foreach (string ns in namespaces)
            {
                if (!ns.StartsWith(RootNamespace + ".", StringComparison.Ordinal)) continue;

                string[] segments = ns.Substring(RootNamespace.Length + 1).Split('.');
                string prefix = RootNamespace;
                foreach (string seg in segments)
                {
                    prefix += "." + seg;
                    if (!unityTypeNames.Contains(seg) || allowed.Contains(prefix)) continue;
                    if (!seenSegments.Add(prefix)) continue;

                    yield return new LintFinding("name-hazard-namespace", prefix, 0,
                        $"namespace segment '{seg}' equals the Unity type '{seg}' and hides it in every file of '{RootNamespace}.*' that sits above it.");
                }
            }

            foreach (string full in publicTypes)
            {
                int dot = full.LastIndexOf('.');
                string simple = dot < 0 ? full : full.Substring(dot + 1);
                int tick = simple.IndexOf('`');
                if (tick >= 0) simple = simple.Substring(0, tick);
                string noArity = dot < 0 ? simple : full.Substring(0, dot + 1) + simple;

                if (!unityTypeNames.Contains(simple) || allowed.Contains(noArity)) continue;

                yield return new LintFinding("name-hazard-type", full, 0,
                    $"public type '{simple}' has the same simple name as a Unity type; a file that imports both namespaces needs an alias.");
            }
        }
    }
}
