// FutureDigitalPlus: renamed by Tools~/rename/rename.py (namespaces, resource paths); see SOURCE.md "Rename record".
using System.Runtime.CompilerServices;
using FutureDigitalPlus.UIKit.DesignSystem.Theme.Data;
using FutureDigitalPlus.UIKit.DesignSystem.Theme.Palette;
using UnityEngine;
using UnityEngine.UIElements;

namespace FutureDigitalPlus.UIKit.DesignSystem.Theme.Applier
{
    /// <summary>
    /// Puts a <see cref="ThemeData"/> onto a <see cref="VisualElement"/> root, and takes it off
    /// again. Two things happen: the theme's baked stylesheet is added to the root, and — if the
    /// theme is class-scoped — the class it is scoped to goes on alongside it.
    ///
    /// That is the entire theming mechanism. The <c>ThemeApplier</c> components are thin wrappers
    /// over these two calls, and it is a static seam rather than component-only because a host
    /// that builds its panels in code has nowhere to hang a MonoBehaviour — the showcase's
    /// world-space corridor spawns one PanelRenderer per section at runtime and repaints them all
    /// from a single call.
    ///
    /// EACH ROOT REMEMBERS WHAT IT IS WEARING, so no caller has to. <see cref="Apply"/> takes the
    /// previous theme off before putting the new one on, which means swapping, re-applying and
    /// clearing are all just <c>Apply</c>. Callers that track "the theme I applied last" get this
    /// wrong the moment a root is rebuilt under them (a PanelRenderer hands you a brand new
    /// VisualElement on every UI reload) and leave a dead stylesheet behind, with the old theme
    /// still painting underneath the new one.
    ///
    /// WHERE YOU ADD THE SHEET MATTERS. UI Toolkit resolves equal-specificity rules by stylesheet
    /// order, and a sheet added LAST to an element wins ties on that element. Unity scores `:root`
    /// and a class selector identically (both 256), so a theme's `:root { --token: … }` block does
    /// NOT outrank a `.theme-light { --token: … }` block by specificity — it wins only by going on
    /// last. So a theme has to land on the same element the base tokens resolve on, which is the
    /// panel or document root where `DesignSystem.uss` itself lands. Put it on an ancestor instead
    /// and any token block attached further down will shadow it for that subtree, because a rule
    /// matching an element beats a value the element merely inherited.
    /// </summary>
    public static class ThemeRuntime
    {
        // Weak keys: when a root is thrown away (and a PanelRenderer throws one away on every UI
        // reload) its entry goes with it. A plain Dictionary here would pin every root the
        // showcase has ever built.
        //
        // FutureDigitalPlus: the table now holds a small record instead of the bare theme, because a runtime theme also
        // owns a generated stylesheet (a lease on it) that has to be released when the root takes the theme off.
        private sealed class WornRecord
        {
            public ThemeData Theme;
            public StyleSheet Sheet;
            public SheetLease Lease;      // null for a baked theme
            public int Revision;
            public string ScopeClass;     // FutureDigitalPlus: remembered, because the theme object may be destroyed while it is worn
        }

        private static readonly ConditionalWeakTable<VisualElement, WornRecord> Worn = new();

        // FutureDigitalPlus: what a root wore before SetHighContrast(true) (so SetHighContrast(false) can restore it) and the theme that
        // stands for high contrast on it (so a theme applied afterwards by someone else is never mistaken for it).
        private sealed class HighContrastRecord
        {
            public ThemeData Before;
            public ThemeData Variant;
        }

        private static readonly ConditionalWeakTable<VisualElement, HighContrastRecord> HighContrastRecords = new();

        /// <summary>Root class added for <see cref="NoTransitionMillis"/> milliseconds while a theme is swapped, so colours change at once instead of fading.</summary>
        public const string NoTransitionClass = "fdp-no-transition";

        /// <summary>How long <see cref="NoTransitionClass"/> stays on a root after a theme swap.</summary>
        public const int NoTransitionMillis = 120;

        /// <summary>How many stylesheets generated for runtime themes are alive (a leak check for tests).</summary>
        public static int LiveRuntimeSheets => RuntimeThemeSheets.LiveCount;

        /// <summary>The theme <paramref name="root"/> is currently wearing, or null.</summary>
        public static ThemeData WornBy(VisualElement root) =>
            root != null && Worn.TryGetValue(root, out var record) ? record.Theme : null;

        /// <summary>
        /// Dresses <paramref name="root"/> in <paramref name="theme"/>, taking off whatever it was
        /// wearing first. Passing null just undresses it, so this one call covers "apply", "swap"
        /// and "back to the base tokens".
        ///
        /// Returns false when nothing was applied: no root, no theme, or a theme that has never
        /// been baked and therefore has no stylesheet to add. A theme made with
        /// <see cref="ThemeData.CreateRuntime"/> needs no bake: its stylesheet is generated here from its baked template,
        /// and generated again (a new one, the old one destroyed) when its values changed since the last call.
        ///
        /// When the root is already on a panel and was wearing another theme, the root gets the
        /// <see cref="NoTransitionClass"/> class for <see cref="NoTransitionMillis"/> ms so the swap is a cut, not a
        /// full-tree colour fade.
        /// </summary>
        public static bool Apply(VisualElement root, ThemeData theme)
        {
            if (root == null) return false;

            // Re-applying the theme a root ALREADY wears, correctly, is a no-op — and has to be.
            // Adding or removing a stylesheet bumps the panel's style version, and a version bump
            // re-resolves every element under that panel from scratch; a re-resolve rebuilds each
            // element's ComputedStyle, which is a handful of ref-counted native blocks. So the
            // Clear-then-Add below is not "cheap because nothing changes", it is two full-panel
            // restyles that produce an identical result.
            //
            // That is affordable once. The showcase repaints ~30 world panels from a single theme
            // change, and most of those panels are already wearing the theme being applied, so
            // without this guard one toggle bought ~60 pointless full-panel restyles. It is the
            // difference between a theme swap being free and it being the most expensive thing on
            // the frame.
            //
            // "Correctly" is the load-bearing word: the sheet must actually still be attached and
            // the scope class actually still on the element. Callers legitimately strip both (the
            // corridor re-asserts `theme-light` after every paint), so trusting the bookkeeping
            // alone would let a root drift out of its own theme and never be repainted.
            //
            // FutureDigitalPlus: for a runtime theme "the same theme" also means the same revision.
            if (theme && Worn.TryGetValue(root, out var current) && current.Theme == theme)
            {
                var wornClass = theme.ScopeClass;
                var sameRevision = !theme.IsRuntime || current.Revision == theme.ComputeRevision();
                if (sameRevision && current.Sheet && root.styleSheets.Contains(current.Sheet) &&
                    (wornClass == null || root.ClassListContains(wornClass)))
                    return true;
            }

            // FutureDigitalPlus: the new sheet is resolved BEFORE the old one is taken off, so the previous revision's
            // lease is still held while the new one is acquired (it is destroyed only after the swap).
            StyleSheet sheet = null;
            SheetLease lease = null;
            if (theme)
            {
                if (theme.IsRuntime)
                {
                    lease = RuntimeThemeSheets.Acquire(theme);
                    sheet = lease?.Sheet;
                }
                else
                {
                    sheet = theme.StyleSheet;
                }

                if (!sheet)
                {
                    Clear(root);
                    return false;
                }
            }

            var hadTheme = Worn.TryGetValue(root, out _);
            Clear(root);

            if (!theme) return false;

            // FutureDigitalPlus: a swap on a live panel is a cut, not a fade.
            if (hadTheme && root.panel != null) SuppressTransitions(root);

            var scopeClass = theme.ScopeClass;
            if (scopeClass != null && !root.ClassListContains(scopeClass))
                root.AddToClassList(scopeClass);

            // Last in the list, so it wins ties against everything already there.
            if (!root.styleSheets.Contains(sheet))
                root.styleSheets.Add(sheet);

            Worn.Remove(root);
            Worn.Add(root, new WornRecord
            {
                Theme = theme,
                Sheet = sheet,
                Lease = lease,
                Revision = theme.IsRuntime ? theme.ComputeRevision() : 0,
                ScopeClass = scopeClass,
            });
            return true;
        }

        /// <summary>
        /// Takes whatever theme <paramref name="root"/> is wearing back off, returning it to the
        /// base tokens. Idempotent, and a no-op on a root that was never themed.
        /// </summary>
        public static void Clear(VisualElement root)
        {
            if (root == null || !Worn.TryGetValue(root, out var record)) return;

            Worn.Remove(root);

            // FutureDigitalPlus: the sheet to remove is the one recorded at apply time (a runtime theme's sheet is a
            // generated clone, not theme.StyleSheet), and the lease on it is released after it is detached.
            if (record.Sheet && root.styleSheets.Contains(record.Sheet))
                root.styleSheets.Remove(record.Sheet);

            RuntimeThemeSheets.Release(record.Lease);

            // FutureDigitalPlus: the class comes from the record, not from the theme: a runtime theme that was destroyed while worn
            // (a game that disposes its theme before the screen) would otherwise leave its scope class on the root for good.
            if (record.ScopeClass != null)
                root.RemoveFromClassList(record.ScopeClass);
        }

        /// <summary>
        /// Adds a theme's stylesheet WITHOUT its scope class, so the rules are present but inert
        /// until something else puts the class on the element. That is how a day / night PAIR
        /// works: both sheets sit on the root permanently and one class toggle picks which paints.
        ///
        /// The root is NOT recorded as wearing this theme, because it isn't — <see cref="Clear"/>
        /// will not take it off again. This installs a permanent layer; use <see cref="Apply"/> for
        /// a theme you intend to swap.
        ///
        /// Meaningless for a <c>:root</c> theme: with no class to gate it, it paints the moment it
        /// is added, which is what <see cref="Apply"/> already does. Returns false for a runtime theme (its generated
        /// stylesheet has a lifetime this call does not track); use <see cref="Apply"/>.
        /// </summary>
        public static bool InstallSheet(VisualElement root, ThemeData theme)
        {
            if (root == null || !theme || theme.IsRuntime) return false;

            var sheet = theme.StyleSheet;
            if (!sheet) return false;

            if (!root.styleSheets.Contains(sheet))
                root.styleSheets.Add(sheet);

            return true;
        }

        /// <summary>
        /// Switches <paramref name="root"/> between its theme and a high-contrast version of it. The version is the
        /// built-in <c>HighContrastDark</c> / <c>HighContrastLight</c> theme when it exists in Resources and was baked,
        /// otherwise a runtime variant derived from the worn theme (<see cref="ThemeVariants.HighContrast"/>), so it works
        /// for brand themes too. Turning it off puts the previous theme back, unless another theme was applied to the root while
        /// high contrast was on: that newer choice stays. A root that wears no theme is treated as wearing the built-in Dark theme.
        /// </summary>
        /// <param name="root">The root to switch.</param>
        /// <param name="enabled">True for high contrast, false to restore.</param>
        /// <param name="level">How strict the derived palette is when no baked counterpart is used.</param>
        /// <returns>True when the root now wears the requested version.</returns>
        public static bool SetHighContrast(VisualElement root, bool enabled, ContrastLevel level = ContrastLevel.High)
        {
            if (root == null) return false;

            if (!enabled)
            {
                if (!HighContrastRecords.TryGetValue(root, out var record)) return true;

                HighContrastRecords.Remove(root);

                // FutureDigitalPlus: only a root that still wears the high contrast variant goes back to what it wore before. A theme that was applied
                // meanwhile (a settings screen, a ThemeManager.Set) is the player's newer choice and stays; the variant, which is ours, goes either way.
                var wornHc = WornBy(root);
                var restored = true;
                if (wornHc && wornHc == record.Variant) restored = Apply(root, record.Before);

                if (record.Variant && record.Variant.IsRuntime) RuntimeThemeSheets.DestroyObject(record.Variant);
                return restored;
            }

            if (HighContrastRecords.TryGetValue(root, out var current))
            {
                if (IsHighContrast(root)) return true;   // already on

                // The record is stale: the theme was cleared or replaced while high contrast was on.
                HighContrastRecords.Remove(root);
                if (current.Variant && current.Variant.IsRuntime) RuntimeThemeSheets.DestroyObject(current.Variant);
            }

            var source = WornBy(root);
            if (!source) source = Resources.Load<ThemeData>(ThemeData.DarkResource);
            if (!source) return false;

            var variant = ThemeVariants.HighContrastFor(source, level);
            if (!variant || !Apply(root, variant))
            {
                if (variant && variant.IsRuntime) RuntimeThemeSheets.DestroyObject(variant);
                return false;
            }

            HighContrastRecords.Add(root, new HighContrastRecord { Before = source, Variant = variant });
            return true;
        }

        /// <summary>True while <see cref="SetHighContrast"/> has this root in high contrast (false once the root was cleared or dressed in another theme).</summary>
        public static bool IsHighContrast(VisualElement root)
        {
            if (root == null || !HighContrastRecords.TryGetValue(root, out var record)) return false;

            var worn = WornBy(root);
            return worn && worn == record.Variant;
        }

        private static void SuppressTransitions(VisualElement root)
        {
            root.AddToClassList(NoTransitionClass);
            root.schedule.Execute(() => root.RemoveFromClassList(NoTransitionClass)).StartingIn(NoTransitionMillis);
        }
    }
}
