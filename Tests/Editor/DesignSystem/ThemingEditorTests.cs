using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using FutureDigitalPlus.UIKit.DesignSystem.Editor.Theming;
using FutureDigitalPlus.UIKit.DesignSystem.Theme.Data;
using FutureDigitalPlus.UIKit.DesignSystem.Theme.Palette;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace FutureDigitalPlus.UIKit.Tests.DesignSystemModule
{
    /// <summary>
    /// EditMode tests of the theming work: the built-in theme assets against their palettes, the contrast audit over every
    /// shipped theme, token access, extra tokens, the bake hash and build check, and the rules the stylesheets have to follow.
    /// The built-in assets are written by <c>Tools &gt; Future Digital Plus UI Kit &gt; Design System &gt; Generate Built-in Themes</c>
    /// (<see cref="ThemePresets"/>); the tests named <c>BuiltIn...</c> fail until that has been run after a palette change.
    /// </summary>
    public class ThemingEditorTests
    {
        private static readonly string[] ShippedNames =
            { "Dark", "Light", "NeutralDark", "NeutralLight", "HighContrastDark", "HighContrastLight" };

        private readonly List<string> _paths = new();
        private readonly List<Object> _objects = new();

        [TearDown]
        public void TearDown()
        {
            foreach (var path in _paths) AssetDatabase.DeleteAsset(path);
            _paths.Clear();
            foreach (var o in _objects) if (o) Object.DestroyImmediate(o);
            _objects.Clear();
        }

        private static ThemeData Shipped(string name) =>
            AssetDatabase.LoadAssetAtPath<ThemeData>(ThemePresets.ThemesFolder + "/" + name + ".asset");

        private ThemeData Scratch()
        {
            var theme = ScriptableObject.CreateInstance<ThemeData>();
            _objects.Add(theme);
            return theme;
        }

        // ----- shipped themes

        [TestCase("Dark", ContrastLevel.Standard)]
        [TestCase("Light", ContrastLevel.Standard)]
        [TestCase("NeutralDark", ContrastLevel.Standard)]
        [TestCase("NeutralLight", ContrastLevel.Standard)]
        [TestCase("HighContrastDark", ContrastLevel.High)]
        [TestCase("HighContrastLight", ContrastLevel.High)]
        public void BuiltInTheme_PassesTheContrastAudit(string name, ContrastLevel level)
        {
            var theme = Shipped(name);
            Assert.IsNotNull(theme, name + ".asset is missing: run Generate Built-in Themes");

            var failures = ContrastAudit.Failures(theme.ToColorMap(), level);
            Assert.IsEmpty(failures, name + ": " + string.Join("; ", failures.Select(f => f.ToString())));
        }

        [Test]
        public void BuiltInThemes_AreWrittenFromThePalettes_AndBakedFromThoseValues()
        {
            var palettes = new Dictionary<string, IReadOnlyDictionary<string, Rgba>>
            {
                ["Dark"] = ThemePalettes.Dark, ["Light"] = ThemePalettes.Light,
                ["NeutralDark"] = ThemePalettes.NeutralDark, ["NeutralLight"] = ThemePalettes.NeutralLight,
                ["HighContrastDark"] = ThemePalettes.HighContrastDark, ["HighContrastLight"] = ThemePalettes.HighContrastLight,
            };

            foreach (var name in ShippedNames)
            {
                var theme = Shipped(name);
                Assert.IsNotNull(theme, name + ".asset is missing: run Generate Built-in Themes");

                var colors = theme.ToColorMap();
                foreach (var pair in palettes[name])
                {
                    Assert.IsTrue(colors.TryGetValue(pair.Key, out var c), name + " lacks " + pair.Key);
                    Assert.AreEqual(pair.Value.R, c.R, 0.003f, name + " " + pair.Key + " red");
                    Assert.AreEqual(pair.Value.G, c.G, 0.003f, name + " " + pair.Key + " green");
                    Assert.AreEqual(pair.Value.B, c.B, 0.003f, name + " " + pair.Key + " blue");
                    Assert.AreEqual(pair.Value.A, c.A, 0.003f, name + " " + pair.Key + " alpha");
                }

                Assert.AreEqual(ThemeBakeState.Ok, ThemeBuildCheck.Evaluate(theme),
                    name + " is not baked from its current values (run Generate Built-in Themes)");
            }
        }

        [Test]
        public void BuiltInThemes_BakedSheetsCarryEveryToken_IncludingFocusRingAndSelection()
        {
            foreach (var name in ShippedNames)
            {
                var theme = Shipped(name);
                Assert.IsNotNull(theme, name);
                Assert.IsTrue(ThemeSheetPatcher.TryCreate(theme.StyleSheet, out var patcher), name + " sheet layout");

                foreach (var token in ThemeData.BuiltInTokenNames)
                    CollectionAssert.Contains(patcher.TokenNames, token, name + " sheet lacks " + token);
            }
        }

        [Test]
        public void BuiltInScopes_FollowTheNamingConvention()
        {
            Assert.AreEqual(":root", Shipped("Dark").Scope);
            Assert.AreEqual("theme-light", Shipped("Light").ScopeClass);
            Assert.AreEqual("theme-hc-dark", Shipped("HighContrastDark").ScopeClass);
            Assert.AreEqual("theme-hc-light", Shipped("HighContrastLight").ScopeClass);
            Assert.AreEqual(":root", Shipped("NeutralDark").Scope);
            Assert.AreEqual("theme-neutral-light", Shipped("NeutralLight").ScopeClass);
        }

        [Test]
        public void DesignTokensUss_MirrorsTheDarkPalette()
        {
            var uss = StripComments(File.ReadAllText(StylePath("DesignTokens.uss")));
            var root = uss.Substring(uss.IndexOf(":root {", System.StringComparison.Ordinal));
            root = root.Substring(0, root.IndexOf("\n}", System.StringComparison.Ordinal));

            foreach (var pair in ThemePalettes.Dark)
            {
                var m = Regex.Match(root, Regex.Escape(pair.Key) + @":\s*([^;]+);");
                if (pair.Key == ThemeTokens.FocusRing)
                {
                    Assert.IsFalse(m.Success, "the focus ring is deliberately undefined in the base sheet (it falls back to the text colour)");
                    continue;
                }

                Assert.IsTrue(m.Success, "DesignTokens.uss does not define " + pair.Key);
                var expected = pair.Value;
                var written = ParseUssColor(m.Groups[1].Value.Trim());
                Assert.AreEqual(expected.R, written.R, 0.004f, pair.Key + " red");
                Assert.AreEqual(expected.G, written.G, 0.004f, pair.Key + " green");
                Assert.AreEqual(expected.B, written.B, 0.004f, pair.Key + " blue");
                Assert.AreEqual(expected.A, written.A, 0.006f, pair.Key + " alpha");
            }
        }

        private static Rgba ParseUssColor(string text)
        {
            if (text.StartsWith("#")) return Rgba.FromHex(text);

            var m = Regex.Match(text, @"rgba\(\s*(\d+),\s*(\d+),\s*(\d+),\s*([0-9.]+)\s*\)");
            Assert.IsTrue(m.Success, "cannot read '" + text + "' as a colour");
            float F(int i) => float.Parse(m.Groups[i].Value, System.Globalization.CultureInfo.InvariantCulture);
            return new Rgba(F(1) / 255f, F(2) / 255f, F(3) / 255f, F(4));
        }

        // ----- stylesheet rules

        private static string StylesFolder
        {
            get
            {
                return FutureDigitalPlus.UIKit.Location.KitLocation.FullPathOf("Runtime/DesignSystem/Resources/FutureDigitalPlus/UIKit/DesignSystem/Styles");
            }
        }

        private static string StylePath(string file) => Path.Combine(StylesFolder, file);

        private static string StripComments(string uss) => Regex.Replace(uss, @"/\*.*?\*/", "", RegexOptions.Singleline);

        [Test]
        public void MobileUss_HasNoLiteralFontSize_SoThemeSizesApplyOnPhones()
        {
            var uss = StripComments(File.ReadAllText(StylePath("Mobile.uss")));
            Assert.IsFalse(Regex.IsMatch(uss, @"font-size:\s*\d+px"), "Mobile.uss must not hard-code font sizes (the theme tokens apply)");
        }

        [Test]
        public void ControlsUss_HasNoLiteralTransitionDurations()
        {
            var uss = StripComments(File.ReadAllText(StylePath("Controls.uss")));
            Assert.IsFalse(Regex.IsMatch(uss, @"transition-duration:\s*\d+m?s"), "durations come from --fdp-transition-* tokens");
        }

        [Test]
        public void InputsUss_FocusRuleTargetsTheTextField_AndSelectionFollowsTheTheme()
        {
            var uss = StripComments(File.ReadAllText(StylePath("Inputs.uss")));
            StringAssert.Contains(".ds-input:focus .unity-base-text-field__input", uss, "Unity 6000.3 puts :focus on the TextField");
            StringAssert.Contains(".ds-textarea:focus .unity-base-text-field__input", uss);

            var selection = Regex.Matches(uss, @"--unity-selection-color:\s*([^;]+);").Cast<Match>().Select(m => m.Groups[1].Value).ToList();
            Assert.GreaterOrEqual(selection.Count, 3);
            foreach (var value in selection)
                StringAssert.StartsWith("var(--fdp-color-selection", value.Trim(), "selection colour follows --fdp-color-selection");
        }

        [Test]
        public void ButtonsUss_FilledVariants_UseTheOnAccentInk()
        {
            var uss = StripComments(File.ReadAllText(StylePath("Buttons.uss")));
            foreach (var variant in new[] { "primary", "secondary", "tertiary", "danger" })
            {
                var m = Regex.Match(uss, @"\.ds-btn--" + variant + @"\s*\{[^}]*\}");
                Assert.IsTrue(m.Success, variant);
                StringAssert.Contains("color: var(--fdp-color-text-on-accent)", m.Value, "ds-btn--" + variant);
            }
        }

        [Test]
        public void DesignSystemUss_ImportsEverySheetItShips()
        {
            var master = File.ReadAllText(StylePath("DesignSystem.uss"));
            var imported = Regex.Matches(master, @"@import url\(""([^""]+)""\)").Cast<Match>().Select(m => m.Groups[1].Value).ToList();

            foreach (var sheet in new[] { "Lists.uss", "Mobile.uss", "Theming.uss" })
                CollectionAssert.Contains(imported, sheet);

            Assert.Greater(imported.IndexOf("Theming.uss"), imported.IndexOf("Mobile.uss"), "Theming.uss is last so it wins ties");
            Assert.Greater(imported.IndexOf("Mobile.uss"), imported.IndexOf("Lists.uss"), "the touch tier comes after the list skins");
        }

        [Test]
        public void EveryTokenTheSheetsUse_IsDefinedOrHasAFallback()
        {
            var defined = new HashSet<string>(
                Regex.Matches(File.ReadAllText(StylePath("DesignTokens.uss")), @"(--fdp-[a-z0-9-]+)\s*:").Cast<Match>().Select(m => m.Groups[1].Value));
            foreach (var token in ThemeData.BuiltInTokenNames) defined.Add(token);
            foreach (var m in Regex.Matches(File.ReadAllText(StylePath("Mobile.uss")), @"(--fdp-[a-z0-9-]+)\s*:").Cast<Match>()) defined.Add(m.Groups[1].Value);

            var problems = new List<string>();
            foreach (var file in Directory.GetFiles(StylesFolder, "*.uss"))
            {
                var uss = StripComments(File.ReadAllText(file));
                foreach (Match use in Regex.Matches(uss, @"var\((--fdp-[a-z0-9-]+)(,)?"))
                {
                    var token = use.Groups[1].Value;
                    var hasFallback = use.Groups[2].Success;
                    if (!defined.Contains(token) && !hasFallback)
                        problems.Add(Path.GetFileName(file) + ": " + token);
                }
            }

            Assert.IsEmpty(problems.Distinct(), "tokens used without a definition or a fallback");
        }

        // ----- ThemeData

        [Test]
        public void TokenNames_AreUniqueAndPrefixed()
        {
            var names = ThemeData.BuiltInTokenNames;
            Assert.AreEqual(names.Count, names.Distinct().Count());
            foreach (var name in names) StringAssert.StartsWith("--fdp-", name);
        }

        [Test]
        public void EveryColourTokenConstant_IsABuiltInToken()
        {
            var builtIn = new HashSet<string>(ThemeData.BuiltInTokenNames);
            foreach (var constant in typeof(ThemeTokens).GetFields().Where(f => f.IsLiteral))
                Assert.IsTrue(builtIn.Contains((string)constant.GetRawConstantValue()), constant.Name);
        }

        [Test]
        public void ColorMap_RoundTrips()
        {
            var theme = Scratch();
            var map = new Dictionary<string, Rgba>
            {
                [ThemeTokens.Primary] = new Rgba(0.1f, 0.2f, 0.3f, 0.4f),
                [ThemeTokens.Bg] = new Rgba(0.5f, 0.6f, 0.7f),
            };

            theme.ApplyColorMap(map);
            var back = theme.ToColorMap();

            Assert.AreEqual(map[ThemeTokens.Primary], back[ThemeTokens.Primary]);
            Assert.AreEqual(map[ThemeTokens.Bg], back[ThemeTokens.Bg]);
        }

        [Test]
        public void SetToken_ChangesBuiltIns_AndAddsExtras()
        {
            var theme = Scratch();

            Assert.IsTrue(theme.SetToken("--fdp-color-primary", Color.magenta));
            Assert.AreEqual(Color.magenta, theme.primaryColor);

            Assert.IsTrue(theme.SetToken("--fdp-font-size-h1", 31f));
            Assert.AreEqual(31f, theme.fontSizeH1);

            Assert.IsFalse(theme.SetToken("--fdp-color-primary", 3f), "a colour token takes no number");
            Assert.IsFalse(theme.SetToken("--fdp-font-size-h1", Color.red), "a px token takes no colour");
            Assert.IsFalse(theme.SetToken("not-a-token", Color.red));
            Assert.IsFalse(theme.SetToken("--has space", Color.red));

            Assert.IsTrue(theme.SetToken("--mygame-color-health", Color.green));
            Assert.IsTrue(theme.SetToken("--mygame-hud-w", 220f));
            Assert.IsTrue(theme.SetToken("--mygame-hud-w", 240f), "setting again updates in place");
            Assert.AreEqual(2, theme.extraTokens.Count);

            Assert.IsTrue(theme.TryGetToken("--mygame-hud-w", out var kind, out _, out var number));
            Assert.AreEqual(ThemeTokenKind.Px, kind);
            Assert.AreEqual(240f, number);
            Assert.IsFalse(theme.TryGetToken("--nothing", out _, out _, out _));
        }

        [Test]
        public void ExtraTokens_AreEmittedInTheUssBlock_AndInvalidOrDuplicateOnesAreSkipped()
        {
            var theme = Scratch();
            theme.SetToken("--mygame-color-health", new Color(1f, 0f, 0f));
            theme.SetToken("--mygame-hud-w", 220f);
            theme.SetToken("--mygame-fade", 300f, ThemeTokenKind.Ms);
            theme.extraTokens.Add(new ThemeExtraToken { name = "--fdp-color-primary", kind = ThemeTokenKind.Color });   // shadows a built-in
            theme.extraTokens.Add(new ThemeExtraToken { name = "bad name", kind = ThemeTokenKind.Px, number = 1f });

            var uss = theme.GenerateUssString();

            StringAssert.Contains("--mygame-color-health: #FF0000;", uss);
            StringAssert.Contains("--mygame-hud-w: 220px;", uss);
            StringAssert.Contains("--mygame-fade: 300ms;", uss);
            Assert.AreEqual(1, Regex.Matches(uss, @"--fdp-color-primary:").Count, "a built-in token is emitted once");
            StringAssert.DoesNotContain("bad name", uss);
        }

        [Test]
        public void GeneratedUss_UsesTheInvariantCulture_ForTheNewTokens()
        {
            var old = System.Threading.Thread.CurrentThread.CurrentCulture;
            try
            {
                System.Threading.Thread.CurrentThread.CurrentCulture = new System.Globalization.CultureInfo("tr-TR");
                var theme = Scratch();
                theme.SetToken("--mygame-ratio", 1.5f);
                StringAssert.Contains("--mygame-ratio: 1.5px;", theme.GenerateUssString());
                StringAssert.Contains("--fdp-color-selection: rgba(", theme.GenerateUssString());
            }
            finally
            {
                System.Threading.Thread.CurrentThread.CurrentCulture = old;
            }
        }

        [Test]
        public void ComputeRevision_ChangesWithEveryKindOfValue_AndOnlyThen()
        {
            var theme = Scratch();
            var r0 = theme.ComputeRevision();
            Assert.AreEqual(r0, theme.ComputeRevision(), "stable");

            theme.primaryColor = Color.red;
            var r1 = theme.ComputeRevision();
            Assert.AreNotEqual(r0, r1);

            theme.fontSizeH1 += 1f;
            var r2 = theme.ComputeRevision();
            Assert.AreNotEqual(r1, r2);

            theme.SetToken("--mygame-x", 3f);
            var r3 = theme.ComputeRevision();
            Assert.AreNotEqual(r2, r3);

            theme.scopeSelector = ".theme-other";
            Assert.AreNotEqual(r3, theme.ComputeRevision());
        }

        [Test]
        public void BakeHash_IsStable_AndTracksTheGeneratedStylesheet()
        {
            var theme = Scratch();
            var h = theme.ComputeBakeHash();
            Assert.AreEqual(16, h.Length);
            Assert.AreEqual(h, theme.ComputeBakeHash());

            theme.warningColor = Color.cyan;
            Assert.AreNotEqual(h, theme.ComputeBakeHash());
        }

        [Test]
        public void IsValidTokenName_AcceptsCustomPropertyNamesOnly()
        {
            Assert.IsTrue(ThemeData.IsValidTokenName("--a"));
            Assert.IsTrue(ThemeData.IsValidTokenName("--my_game-Color-2"));
            Assert.IsFalse(ThemeData.IsValidTokenName("--"));
            Assert.IsFalse(ThemeData.IsValidTokenName("-a"));
            Assert.IsFalse(ThemeData.IsValidTokenName("--a b"));
            Assert.IsFalse(ThemeData.IsValidTokenName("--a;"));
            Assert.IsFalse(ThemeData.IsValidTokenName(null));
        }

        // ----- bake state and the patcher on a real baked sheet

        [Test]
        public void BuildCheck_ReportsNotBaked_Ok_AndStale()
        {
            var theme = Scratch();
            theme.scopeSelector = ".theme-fdp-check";
            Assert.AreEqual(ThemeBakeState.NotBaked, ThemeBuildCheck.Evaluate(theme));

            var path = AssetDatabase.GenerateUniqueAssetPath("Assets/FdpUiKitBuildCheck.asset");
            AssetDatabase.CreateAsset(theme, path);
            _paths.Add(path);
            _objects.Remove(theme);

            Assert.IsTrue(ThemeBaker.Bake(theme));
            Assert.AreEqual(ThemeBakeState.Ok, ThemeBuildCheck.Evaluate(theme));
            Assert.IsNotEmpty(theme.BakedHash);

            theme.dangerColor = Color.yellow;
            Assert.AreEqual(ThemeBakeState.Stale, ThemeBuildCheck.Evaluate(theme));

            ThemeBaker.Bake(theme);
            Assert.AreEqual(ThemeBakeState.Ok, ThemeBuildCheck.Evaluate(theme));
        }

        [Test]
        public void BuildCheck_AThemeWithASheetButNoHash_IsUnverified_NotAnError()
        {
            var theme = Scratch();
            var path = AssetDatabase.GenerateUniqueAssetPath("Assets/FdpUiKitUnverified.asset");
            AssetDatabase.CreateAsset(theme, path);
            _paths.Add(path);
            _objects.Remove(theme);
            ThemeBaker.Bake(theme);

            theme.SetBakedHash(null);
            Assert.AreEqual(ThemeBakeState.Unverified, ThemeBuildCheck.Evaluate(theme));
        }

        [Test]
        public void BakedExtraTokens_ArePatchable_AndOnesTheTemplateLacksAreIgnored()
        {
            var template = Scratch();
            template.scopeSelector = ".theme-fdp-extra";
            template.SetToken("--mygame-color-health", Color.red);
            template.SetToken("--mygame-hud-w", 200f);
            var path = AssetDatabase.GenerateUniqueAssetPath("Assets/FdpUiKitExtra.asset");
            AssetDatabase.CreateAsset(template, path);
            _paths.Add(path);
            _objects.Remove(template);
            Assert.IsTrue(ThemeBaker.Bake(template));

            Assert.IsTrue(ThemeSheetPatcher.TryCreate(template.StyleSheet, out var patcher));
            CollectionAssert.Contains(patcher.TokenNames, "--mygame-color-health");
            CollectionAssert.Contains(patcher.TokenNames, "--mygame-hud-w");

            var runtime = ThemeData.CreateRuntime(template);
            _objects.Add(runtime);
            runtime.SetToken("--mygame-color-health", Color.green);
            runtime.SetToken("--mygame-not-in-template", 5f);

            Assert.IsTrue(patcher.TryBuild(runtime, out var sheet));
            _objects.Add(sheet);
            Assert.IsNotNull(sheet);
        }

        [Test]
        public void Patcher_LeavesTheTemplateUntouched()
        {
            var dark = Shipped("Dark");
            Assert.IsTrue(ThemeSheetPatcher.TryCreate(dark.StyleSheet, out var patcher));

            var before = JsonUtility.ToJson(dark.StyleSheet);
            var runtime = ThemeData.CreateRuntime(dark);
            _objects.Add(runtime);
            runtime.primaryColor = Color.magenta;
            Assert.IsTrue(patcher.TryBuild(runtime, out var clone));
            _objects.Add(clone);

            Assert.AreEqual(before, JsonUtility.ToJson(dark.StyleSheet));
            Assert.AreNotEqual(before, JsonUtility.ToJson(clone), "the clone carries the new value");
        }

        [Test]
        public void ThemeVariants_DoNotTouchTheirSource_AndPassTheirAudits()
        {
            var dark = Shipped("Dark");
            var before = dark.ComputeRevision();

            var hc = ThemeVariants.HighContrast(dark);
            _objects.Add(hc);
            Assert.AreEqual(before, dark.ComputeRevision());
            Assert.IsEmpty(ContrastAudit.Failures(hc.ToColorMap(), ContrastLevel.High));
            Assert.AreEqual(dark.borderThin + 1f, hc.borderThin);

            var big = ThemeVariants.WithTextScale(dark, 1.5f);
            _objects.Add(big);
            Assert.AreEqual(Mathf.Round(dark.fontSizeBody1 * 1.5f), big.fontSizeBody1);
            Assert.AreEqual(dark.radiusMd, big.radiusMd, "only sizes of text change");

            var capped = ThemeVariants.WithTextScale(dark, 9f);
            _objects.Add(capped);
            Assert.AreEqual(Mathf.Round(dark.fontSizeH1 * 2f), capped.fontSizeH1, "clamped to x2");
        }

        [Test]
        public void FromBrand_KeepsThePrimary_AndTheTextPassesAa()
        {
            var brand = ThemeData.FromBrand(new Color(0.23f, 0.51f, 0.96f), new Color(0.04f, 0.06f, 0.09f), dark: true);
            Assert.IsNotNull(brand, "needs the built-in Dark theme in Resources");
            _objects.Add(brand);

            Assert.AreEqual(0.23f, brand.primaryColor.r, 0.001f);
            var failures = ContrastAudit.Failures(brand.ToColorMap()).Where(f => f.Kind == ContrastCheckKind.Text || f.Kind == ContrastCheckKind.NonText).ToList();
            Assert.IsEmpty(failures, string.Join("; ", failures.Select(f => f.ToString())));
        }

        [Test]
        public void MenuItems_AreUnderTheKitName()
        {
            var methods = typeof(ThemePresets).Assembly.GetTypes()
                .SelectMany(t => t.GetMethods(System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic))
                .SelectMany(m => m.GetCustomAttributes(typeof(MenuItem), false).Cast<MenuItem>())
                .Select(a => a.menuItem)
                .ToList();

            Assert.IsNotEmpty(methods);
            foreach (var item in methods) StringAssert.Contains("Future Digital Plus UI Kit", item);
        }
    }
}
