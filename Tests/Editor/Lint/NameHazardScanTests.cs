#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using NUnit.Framework;

namespace FutureDigitalPlus.UIKit.Lint.Tests
{
    /// <summary>
    /// Rule D9 of the plan: no namespace segment or public type of the kit may equal the name of a loaded Unity type unless it is in
    /// <see cref="LintAllowList.NameHazards"/>. Unity only (it reads the loaded assemblies); <c>Tools~/CompileCheck</c> runs the same rule without Unity.
    /// </summary>
    public class NameHazardScanTests
    {
        private static IEnumerable<Type> LoadableTypes(Assembly a)
        {
            try { return a.GetTypes(); }
            catch (ReflectionTypeLoadException e) { return e.Types.Where(t => t != null); }
        }

        [Test]
        public void NoNewNameEqualsAUnityTypeName()
        {
            var assemblies = AppDomain.CurrentDomain.GetAssemblies();

            var unityNames = new HashSet<string>(StringComparer.Ordinal);
            foreach (var a in assemblies.Where(a => a.GetName().Name.StartsWith("UnityEngine", StringComparison.Ordinal) || a.GetName().Name.StartsWith("UnityEditor", StringComparison.Ordinal)))
                foreach (var t in LoadableTypes(a))
                    if (t.IsPublic && !t.IsNested) unityNames.Add(t.Name.Split('`')[0]);

            // Assemblies of the kit that ship (not tests, not samples): their name starts with the root namespace.
            var namespaces = new HashSet<string>(StringComparer.Ordinal);
            var publicTypes = new HashSet<string>(StringComparer.Ordinal);
            foreach (var a in assemblies.Where(a => a.GetName().Name.StartsWith(NameHazardRules.RootNamespace, StringComparison.Ordinal)))
            {
                string n = a.GetName().Name;
                if (n.Contains(".Tests") || n.Contains(".Samples")) continue;

                foreach (var t in LoadableTypes(a))
                {
                    if (t.IsNested || string.IsNullOrEmpty(t.Namespace)) continue;
                    if (t.Name.Contains("<")) continue; // compiler generated
                    namespaces.Add(t.Namespace);
                    if (t.IsPublic) publicTypes.Add(t.Namespace + "." + t.Name);
                }
            }

            Assert.That(unityNames, Is.Not.Empty, "no Unity assemblies found");
            Assert.That(namespaces, Is.Not.Empty, "no kit assemblies found");

            var found = NameHazardRules.Find(namespaces, publicTypes, unityNames, new HashSet<string>(LintAllowList.NameHazards)).ToList();
            Assert.That(found, Is.Empty, "\n" + string.Join("\n", found.Select(f => f.ToString())));
        }
    }
}
#endif
