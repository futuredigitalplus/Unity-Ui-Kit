// FutureDigitalPlus: renamed by Tools~/rename/rename.py (namespaces, resource paths); see SOURCE.md "Rename record".
using System;
using System.Collections.Generic;
using System.IO;
using FutureDigitalPlus.UIKit.DesignSystem.Theme.Data;
using FutureDigitalPlus.UIKit.DesignSystem.Theme.Palette;
using FutureDigitalPlus.UIKit.Location;
using UnityEditor;
using UnityEngine;

namespace FutureDigitalPlus.UIKit.DesignSystem.Editor.Theming
{
    /// <summary>
    /// Generates the two themes the package ships with, so a consumer who installs it gets
    /// working themes rather than an empty authoring tool.
    ///
    ///   Resources/FutureDigitalPlus/UIKit/DesignSystem/Themes/Dark   — scope <c>:root</c>. The design system's own palette,
    ///                                identical to the values in DesignTokens.uss. This is the
    ///                                starting point you duplicate to make a brand theme.
    ///   Resources/FutureDigitalPlus/UIKit/DesignSystem/Themes/Light  — scope <c>.theme-light</c>. Day mode.
    ///
    /// FutureDigitalPlus: four more are generated (<c>HighContrastDark</c> <c>.theme-hc-dark</c>, <c>HighContrastLight</c>
    /// <c>.theme-hc-light</c>, <c>NeutralDark</c> <c>:root</c>, <c>NeutralLight</c> <c>.theme-neutral-light</c>). The colours of
    /// all six are data in <see cref="ThemePalettes"/> (pure, unit-tested for contrast); this class only writes them into assets.
    ///
    /// Light is CLASS-scoped on purpose, and that is what makes the day/night pair work: the
    /// sheet sits on the root permanently and paints nothing until `theme-light` shows up on the
    /// element, so switching themes is one class toggle and the var() cascade repaints the tree.
    /// It also means Light and Dark coexist in one panel without fighting, which a pair of
    /// `:root` themes could not do.
    ///
    /// The colours are written as hex and parsed, not as float triples, so this file can be
    /// diffed against DesignTokens.uss and ShowcaseTheme.uss by eye.
    /// </summary>
    public static class ThemePresets
    {
        // FutureDigitalPlus: upstream hard-codes "Assets/DesignSystem/Resources/UI/Themes". Here the built-in themes live
        // under Runtime/DesignSystem/Resources/FutureDigitalPlus/UIKit/DesignSystem/Themes of the kit, wherever the kit is:
        // the folder is resolved by KitLocation (a UPM package or a plain folder below Assets, same source for both).
        // Regenerating writes into the kit, so it only works when the kit is editable (classic folder, embedded package or
        // package installed from a local path); see Generate().
        private const string ThemesSubfolder   = "Runtime/DesignSystem/Resources/FutureDigitalPlus/UIKit/DesignSystem/Themes";

        /// <summary>The asset folder the built-in themes are written to (inside the kit, in either layout).</summary>
        public static string ThemesFolder => KitLocation.AssetPath(ThemesSubfolder);
        /// <summary>Asset path of the <c>Dark</c> theme.</summary>
        public static string DarkPath     => ThemesFolder + "/Dark.asset";
        /// <summary>Asset path of the <c>Light</c> theme.</summary>
        public static string LightPath    => ThemesFolder + "/Light.asset";

        // FutureDigitalPlus: the other generated themes.
        /// <summary>Asset path of the <c>HighContrastDark</c> theme.</summary>
        public static string HighContrastDarkPath  => ThemesFolder + "/HighContrastDark.asset";
        /// <summary>Asset path of the <c>HighContrastLight</c> theme.</summary>
        public static string HighContrastLightPath => ThemesFolder + "/HighContrastLight.asset";
        /// <summary>Asset path of the <c>NeutralDark</c> theme.</summary>
        public static string NeutralDarkPath       => ThemesFolder + "/NeutralDark.asset";
        /// <summary>Asset path of the <c>NeutralLight</c> theme.</summary>
        public static string NeutralLightPath      => ThemesFolder + "/NeutralLight.asset";

        /// <summary>How many themes <see cref="Generate"/> writes.</summary>
        public const int GeneratedCount = 6;

        // FutureDigitalPlus: packages installed from a registry or git URL are read-only.
        private static bool PackageIsEditable => KitLocation.IsWritable;

        /// <summary>Resources path for <c>Resources.Load&lt;ThemeData&gt;</c>.</summary>
        public const string DarkResource  = "FutureDigitalPlus/UIKit/DesignSystem/Themes/Dark";
        /// <summary>Resources path of the Light theme.</summary>
        public const string LightResource = "FutureDigitalPlus/UIKit/DesignSystem/Themes/Light";

        /// <summary>Menu entry: writes and bakes the built-in themes (see <see cref="Generate"/>).</summary>
        [MenuItem("Tools/Future Digital Plus UI Kit/Design System/Generate Built-in Themes")]
        public static void GenerateMenu()
        {
            var made = Generate();
            Debug.Log($"[ThemePresets] Generated and baked {made} built-in theme(s) into {ThemesFolder}.");
        }

        /// <summary>
        /// Batch entry point:
        /// <c>Unity -batchmode -quit -executeMethod FutureDigitalPlus.UIKit.DesignSystem.Editor.Theming.ThemePresets.GenerateBatch</c>
        /// </summary>
        public static void GenerateBatch()
        {
            var made = Generate();
            Debug.Log($"[ThemePresets] batch: generated {made} built-in theme(s).");

            if (Application.isBatchMode)
                EditorApplication.Exit(made == GeneratedCount ? 0 : 1);
        }

        /// <summary>
        /// Writes the six built-in themes from <see cref="ThemePalettes"/> (creating missing assets, updating existing ones in place so GUIDs
        /// survive) and bakes them. Does nothing in a read-only package.
        /// </summary>
        /// <returns>How many themes were baked; <see cref="GeneratedCount"/> on success, 0 in a read-only package.</returns>
        public static int Generate()
        {
            // FutureDigitalPlus: refuse to write into a read-only (registry / git) package.
            if (!PackageIsEditable)
            {
                Debug.LogWarning("[ThemePresets] The built-in themes live inside a read-only package " +
                                 "install, so they cannot be regenerated here. Embed the package " +
                                 "(copy it into Packages/), install it from a local path or use the " +
                                 "classic copy below Assets/ to regenerate them; to make your own " +
                                 "theme, duplicate Dark.asset into Assets/ instead.");
                return 0;
            }

            var themes = new List<ThemeData>
            {
                Upsert(DarkPath,  ThemeData.RootScope, ApplyDark),
                Upsert(LightPath, ".theme-light",      ApplyLight),
                // FutureDigitalPlus: high-contrast and neutral themes.
                Upsert(HighContrastDarkPath,  ".theme-hc-dark",       t => ApplyHighContrast(t, ThemePalettes.HighContrastDark)),
                Upsert(HighContrastLightPath, ".theme-hc-light",      t => ApplyHighContrast(t, ThemePalettes.HighContrastLight)),
                Upsert(NeutralDarkPath,       ThemeData.RootScope,    t => ApplyPalette(t, ThemePalettes.NeutralDark)),
                Upsert(NeutralLightPath,      ".theme-neutral-light", t => ApplyPalette(t, ThemePalettes.NeutralLight)),
            };

            AssetDatabase.SaveAssets();
            var baked = ThemeBaker.BakeMany(themes, out var failed);

            if (failed > 0)
                Debug.LogError($"[ThemePresets] {failed} built-in theme(s) failed to bake.");

            AssetDatabase.Refresh();
            return baked;
        }

        // ──────────────────────────────────────────────────────────────────────
        // Colours come from ThemePalettes (pure data, contrast-tested). Dark mirrors the :root block of
        // DesignTokens.uss; an EditMode test checks that. Light is class-scoped (see the header). Rarity is
        // deliberately NOT re-tinted per theme: rarity carries meaning, and meaning does not change with the lights.
        // ──────────────────────────────────────────────────────────────────────

        // FutureDigitalPlus: the hex values that used to be written here moved to ThemePalettes, so the audit
        // and the editor read one table. The palettes also changed (contrast fixes), see Documentation~/DesignSystem/THEMING.md.
        private static void ApplyDark(ThemeData t)  => ApplyPalette(t, ThemePalettes.Dark);
        private static void ApplyLight(ThemeData t) => ApplyPalette(t, ThemePalettes.Light);

        private static void ApplyPalette(ThemeData t, System.Collections.Generic.IReadOnlyDictionary<string, Rgba> palette)
        {
            t.ApplyColorMap(palette);
            ApplyMetrics(t);
        }

        // High contrast also thickens borders by one pixel (outlines are what a low-vision user finds first).
        private static void ApplyHighContrast(ThemeData t, System.Collections.Generic.IReadOnlyDictionary<string, Rgba> palette)
        {
            ApplyPalette(t, palette);
            t.borderThin += 1f;
            t.borderRegular += 1f;
        }

        // Geometry and motion are the same in both moods: turning the lights on does not make a
        // button a different size. Kept explicit rather than left at the field defaults so a
        // generated theme is a complete statement of the design system, not a diff against it.
        private static void ApplyMetrics(ThemeData t)
        {
            t.fontSizeH1 = 26f; t.fontSizeH2 = 20f; t.fontSizeH3 = 16f;
            t.fontSizeBody1 = 14f; t.fontSizeBody2 = 12f; t.fontSizeCaption = 11f; t.fontSizeDisplay = 34f;

            t.radiusXs = 4f; t.radiusSm = 6f; t.radiusMd = 8f; t.radiusLg = 12f; t.radiusXl = 16f;

            t.spacing1 = 4f; t.spacing2 = 8f; t.spacing3 = 12f; t.spacing4 = 16f;
            t.spacing5 = 20f; t.spacing6 = 24f; t.spacing8 = 32f;

            t.borderThin = 1f; t.borderRegular = 2f;

            t.transitionFast = 120f; t.transitionMedium = 200f; t.transitionSlow = 320f;
        }

        // ──────────────────────────────────────────────────────────────────────
        // Plumbing
        // ──────────────────────────────────────────────────────────────────────

        /// <summary>
        /// Creates the theme if it is missing, otherwise rewrites the one that is there. Updating
        /// in place matters: the asset's GUID survives, so a ThemeApplier in a scene that already
        /// points at this theme keeps pointing at it after a regenerate.
        /// </summary>
        public static ThemeData Upsert(string assetPath, string scope, Action<ThemeData> fill)
        {
            EnsureFolder(Path.GetDirectoryName(assetPath)?.Replace('\\', '/'));

            var theme = AssetDatabase.LoadAssetAtPath<ThemeData>(assetPath);
            if (!theme)
            {
                theme = ScriptableObject.CreateInstance<ThemeData>();
                AssetDatabase.CreateAsset(theme, assetPath);
            }

            theme.scopeSelector = scope;
            fill(theme);

            EditorUtility.SetDirty(theme);
            return theme;
        }

        /// <summary>Creates every missing folder on the way to <paramref name="folder"/>.</summary>
        public static void EnsureFolder(string folder)
        {
            if (string.IsNullOrEmpty(folder) || AssetDatabase.IsValidFolder(folder)) return;

            var parts = folder.Split('/');
            var built = parts[0];                       // "Assets"

            for (var i = 1; i < parts.Length; i++)
            {
                var next = built + "/" + parts[i];
                if (!AssetDatabase.IsValidFolder(next))
                    AssetDatabase.CreateFolder(built, parts[i]);
                built = next;
            }
        }

        /// <summary>Parses <c>#RRGGBB</c> into a colour with the given alpha (magenta and an error when it is not a colour).</summary>
        public static Color Hex(string hex, float alpha = 1f)
        {
            if (!ColorUtility.TryParseHtmlString(hex, out var c))
            {
                Debug.LogError($"[ThemePresets] '{hex}' is not a colour.");
                return Color.magenta;
            }

            c.a = alpha;
            return c;
        }
    }
}
