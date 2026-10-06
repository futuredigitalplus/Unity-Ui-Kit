using System.Runtime.CompilerServices;
using FutureDigitalPlus.UIKit.DesignSystem.Theme.Data;
using UnityEngine;
using UnityEngine.UIElements;

namespace FutureDigitalPlus.UIKit.DesignSystem.Theme.Applier
{
    /// <summary>The generated stylesheets of one runtime theme: the patcher (built once) and the sheet of the newest revision.</summary>
    internal sealed class SheetEntry
    {
        public ThemeSheetPatcher Patcher;
        public SheetLease Current;
        public bool Warned;
    }

    /// <summary>
    /// One generated stylesheet for one revision of a runtime theme, counted by the roots that wear it. It is destroyed as
    /// soon as the last root lets go of it.
    /// </summary>
    internal sealed class SheetLease
    {
        public SheetEntry Entry;
        public StyleSheet Sheet;
        public int Revision;
        public int Refs;
    }

    /// <summary>
    /// Owns the stylesheets generated for runtime themes. A sheet is never changed once attached (Unity caches resolved
    /// styles by sheet), so every new revision of a theme gets a new clone; a clone is destroyed when the last root that wears
    /// it lets go (a root that swaps to a newer revision does exactly that). No clone outlives its users, and the clone of a
    /// destroyed theme goes with its last root.
    /// </summary>
    internal static class RuntimeThemeSheets
    {
        private static readonly ConditionalWeakTable<ThemeData, SheetEntry> Entries = new();

        /// <summary>How many generated sheets are alive right now (a leak check for tests).</summary>
        public static int LiveCount { get; private set; }

        /// <summary>
        /// Returns a lease on the sheet of the theme's CURRENT revision, generating it when needed. The caller owns one
        /// reference and must <see cref="Release"/> it. Null when the template cannot be patched.
        /// </summary>
        public static SheetLease Acquire(ThemeData theme)
        {
            if (!theme || !theme.IsRuntime) return null;

            var entry = Entries.GetOrCreateValue(theme);
            var revision = theme.ComputeRevision();

            if (entry.Current != null && entry.Current.Revision == revision)
            {
                entry.Current.Refs++;
                return entry.Current;
            }

            var template = theme.RuntimeTemplate.StyleSheet;
            if (entry.Patcher == null && !ThemeSheetPatcher.TryCreate(template, out entry.Patcher))
            {
                if (!entry.Warned && !template)
                {
                    entry.Warned = true;
                    Debug.LogWarning("[ThemeRuntime] Runtime theme '" + theme.name + "' is based on '" + theme.RuntimeTemplate.name +
                                     "', which has no baked stylesheet to patch. Bake the template (Design System > Rebake All Themes).",
                                     theme.RuntimeTemplate);
                }

                return null;
            }

            if (!entry.Patcher.TryBuild(theme, out var sheet)) return null;

            // The previous revision stays alive for the roots that still wear it; they release it when they swap.
            entry.Current = new SheetLease { Entry = entry, Sheet = sheet, Revision = revision, Refs = 1 };
            LiveCount++;
            return entry.Current;
        }

        /// <summary>Gives a reference back; a sheet nobody wears any more is destroyed.</summary>
        public static void Release(SheetLease lease)
        {
            if (lease == null || lease.Sheet == null) return;

            lease.Refs--;
            if (lease.Refs > 0) return;

            if (lease.Entry != null && lease.Entry.Current == lease) lease.Entry.Current = null;
            DestroyObject(lease.Sheet);
            lease.Sheet = null;
            LiveCount--;
        }

        /// <summary>Called when a runtime theme is destroyed: its sheet is forgotten (roots that still wear it keep it until they release it).</summary>
        public static void OnThemeDestroyed(ThemeData theme)
        {
            if (Entries.TryGetValue(theme, out var entry)) entry.Current = null;
        }

        /// <summary>Destroys a generated object in Play Mode and in Edit Mode alike.</summary>
        public static void DestroyObject(Object obj)
        {
            if (!obj) return;

            if (Application.isPlaying) Object.Destroy(obj);
            else Object.DestroyImmediate(obj);
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics() => LiveCount = 0;
    }
}
