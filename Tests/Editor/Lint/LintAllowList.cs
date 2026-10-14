using System;
using System.Collections.Generic;
using System.Linq;

namespace FutureDigitalPlus.UIKit.Lint
{
    /// <summary>
    /// Known and accepted exceptions to the lint rules. Every entry has a reason; an entry that no longer matches anything fails
    /// <see cref="LintPackageTests.AllowListHasNoStaleEntries"/>, so the list cannot rot.
    /// </summary>
    internal static class LintAllowList
    {
        internal readonly struct Entry
        {
            public readonly string Rule;
            public readonly string FilePrefix;
            public readonly string MessagePart;
            public readonly string Reason;

            public Entry(string rule, string filePrefix, string messagePart, string reason)
            {
                Rule = rule;
                FilePrefix = filePrefix;
                MessagePart = messagePart;
                Reason = reason;
            }

            public bool Matches(LintFinding f) =>
                f.Rule == Rule && f.File.StartsWith(FilePrefix, StringComparison.Ordinal) && f.Message.Contains(MessagePart);
        }

        /// <summary>Exceptions to file level rules.</summary>
        public static readonly Entry[] Entries = new Entry[0];

        /// <summary>
        /// Accepted name hazards: a namespace accepts that segment in it and below it, a full type name accepts that type. These are the nine
        /// upstream names (<c>Behaviour</c>, <c>UIDocument</c>, <c>Editor</c>, <c>Animation</c>, <c>EditorHelpers</c>, <c>Scope</c>, <c>ILogger</c>,
        /// <c>Easing</c>, <c>ColorField</c>): renaming them would make every upstream diff expensive (<c>Tools~/rename/rename-map.md</c>, explicit exception 1).
        /// Compile checked: none of them changes the meaning of a name inside the kit (a member or an explicit <c>UnityEditor.</c> qualifier wins), and a
        /// consumer file that imports both a kit namespace and the Unity one aliases the type. New names must not be added here.
        /// The one exception is <c>ScrollView</c>, the name chosen for the module: kit <c>Runtime</c> and <c>Editor</c> code writes
        /// <c>UnityEngine.UIElements.ScrollView</c> (a bare <c>ScrollView</c> is CS0118, never a silent change; see <c>Runtime/ScrollView/SOURCE.md</c>).
        /// </summary>
        public static readonly string[] NameHazards =
        {
            "FutureDigitalPlus.UIKit.DesignSystem.Behaviour",
            "FutureDigitalPlus.UIKit.DesignSystem.Behaviour.UIDocument",
            "FutureDigitalPlus.UIKit.DesignSystem.Theme.Applier.UIDocument",
            "FutureDigitalPlus.UIKit.DesignSystem.Editor",
            "FutureDigitalPlus.UIKit.Components.Editor",
            "FutureDigitalPlus.UIKit.Animation",
            "FutureDigitalPlus.UIKit.DesignSystem.Editor.EditorHelpers",
            "FutureDigitalPlus.UIKit.Components.Scope",
            "FutureDigitalPlus.UIKit.Components.ILogger",
            "FutureDigitalPlus.UIKit.Animation.Easing",
            "FutureDigitalPlus.UIKit.Extras.ColorField",
            "FutureDigitalPlus.UIKit.ScrollView",
        };

        /// <summary>The findings that no entry accepts.</summary>
        public static IEnumerable<LintFinding> Filter(IEnumerable<LintFinding> findings) =>
            findings.Where(f => !Entries.Any(e => e.Matches(f)));
    }
}
