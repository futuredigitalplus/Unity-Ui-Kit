using System;
using System.Collections.Generic;
using System.Linq;
using FutureDigitalPlus.UIKit.DesignSystem.Theme.Palette;
using NUnit.Framework;

namespace FutureDigitalPlus.UIKit.Tests.DesignSystemModule
{
    /// <summary>
    /// Pure tests of the colour maths, the contrast audit, the palette derivation and the shipped palettes. They use
    /// no UnityEngine type, so the same file also runs under <c>dotnet test</c> (see <c>Documentation~/DesignSystem/THEMING.md</c>).
    /// </summary>
    public class PaletteTests
    {
        private static Rgba Hex(string hex) => Rgba.FromHex(hex);

        // ----- ColorMath

        [Test]
        public void Ratio_BlackOnWhite_Is21_AndSameColourIs1()
        {
            Assert.AreEqual(21f, ColorMath.Ratio(Hex("#000000"), Hex("#FFFFFF")), 0.01f);
            Assert.AreEqual(1f, ColorMath.Ratio(Hex("#336699"), Hex("#336699")), 0.0001f);
        }

        [Test]
        public void Ratio_IsSymmetric_AndMatchesTheWcagReferencePair()
        {
            var a = Hex("#767676");
            var b = Hex("#FFFFFF");
            Assert.AreEqual(ColorMath.Ratio(a, b), ColorMath.Ratio(b, a), 0.0001f);
            Assert.AreEqual(4.54f, ColorMath.Ratio(a, b), 0.01f, "#767676 on white is the classic 4.54:1 AA pair");
        }

        [Test]
        public void Ratio_CompositesATranslucentForegroundOverTheBackground()
        {
            var bg = Hex("#000000");
            var half = new Rgba(1f, 1f, 1f, 0.5f);
            var plain = ColorMath.Ratio(half, bg, compositeForeground: false);
            var seen = ColorMath.Ratio(half, bg, compositeForeground: true);
            Assert.Greater(plain, seen, "a half-transparent white is seen as mid grey, not white");
            Assert.AreEqual(ColorMath.Ratio(new Rgba(0.5f, 0.5f, 0.5f), bg), seen, 0.0001f);
        }

        [Test]
        public void FromHex_ParsesSixAndEightDigits_AndRejectsGarbage()
        {
            Assert.AreEqual(new Rgba(1f, 0f, 0f), Hex("#FF0000"));
            Assert.AreEqual(0.5f, Hex("#FF000080").A, 0.01f);
            Assert.Throws<FormatException>(() => Rgba.FromHex("#12345"));
            Assert.Throws<FormatException>(() => Rgba.FromHex(null));
        }

        [TestCase("#777777", "#FFFFFF", 7f)]
        [TestCase("#CCCCCC", "#000000", 4.5f)]
        [TestCase("#3B82F6", "#FFFFFF", 4.5f)]
        [TestCase("#22C55E", "#131A24", 7f)]
        public void EnsureRatio_ReachesTheRatio_AndMovesTheColourOnlyWhenNeeded(string fg, string bg, float min)
        {
            var result = ColorMath.EnsureRatio(Hex(fg), Hex(bg), min);
            Assert.GreaterOrEqual(ColorMath.Ratio(result, Hex(bg)) + 0.01f, min);

            if (ColorMath.Ratio(Hex(fg), Hex(bg)) >= min)
                Assert.AreEqual(Hex(fg), result, "an already passing colour is returned untouched");
        }

        [Test]
        public void EnsureRatio_WhenNothingCanPass_ReturnsAnExtreme()
        {
            // Mid grey on mid grey cannot reach 21:1; the helper gives pure black or white instead of looping.
            var result = ColorMath.EnsureRatio(Hex("#808080"), Hex("#808080"), 30f);
            Assert.IsTrue(result.Equals(new Rgba(0f, 0f, 0f)) || result.Equals(new Rgba(1f, 1f, 1f)));
        }

        [Test]
        public void BestInk_PicksTheHigherContrastOfBlackAndWhite()
        {
            Assert.AreEqual(new Rgba(0f, 0f, 0f), ColorMath.BestInk(Hex("#22C55E")));
            Assert.AreEqual(new Rgba(1f, 1f, 1f), ColorMath.BestInk(Hex("#1E40AF")));
        }

        // ----- shipped palettes

        public static IEnumerable<TestCaseData> Shipped()
        {
            yield return new TestCaseData("Dark", ThemePalettes.Dark, ContrastLevel.Standard);
            yield return new TestCaseData("Light", ThemePalettes.Light, ContrastLevel.Standard);
            yield return new TestCaseData("NeutralDark", ThemePalettes.NeutralDark, ContrastLevel.Standard);
            yield return new TestCaseData("NeutralLight", ThemePalettes.NeutralLight, ContrastLevel.Standard);
            yield return new TestCaseData("HighContrastDark", ThemePalettes.HighContrastDark, ContrastLevel.High);
            yield return new TestCaseData("HighContrastLight", ThemePalettes.HighContrastLight, ContrastLevel.High);
        }

        [TestCaseSource(nameof(Shipped))]
        public void ShippedPalette_PassesTheContrastAudit(string name, IReadOnlyDictionary<string, Rgba> palette, ContrastLevel level)
        {
            var failures = ContrastAudit.Failures(palette, level);
            Assert.IsEmpty(failures, name + " fails: " + string.Join("; ", failures.Select(f => f.ToString())));
        }

        [TestCaseSource(nameof(Shipped))]
        public void ShippedPalette_AuditActuallyMeasuresEveryPairFamily(string name, IReadOnlyDictionary<string, Rgba> palette, ContrastLevel level)
        {
            var checks = ContrastAudit.Run(palette, level);
            foreach (ContrastCheckKind kind in Enum.GetValues(typeof(ContrastCheckKind)))
                Assert.IsTrue(checks.Any(c => c.Kind == kind), name + " has no " + kind + " check (a missing token would hide a failure)");
        }

        [Test]
        public void Audit_ReportsAFailingPair_AndSkipsTokensThePaletteLacks()
        {
            var weak = new Dictionary<string, Rgba>
            {
                [ThemeTokens.Bg] = Hex("#FFFFFF"),
                [ThemeTokens.TextPrimary] = Hex("#BBBBBB"),
            };

            var checks = ContrastAudit.Run(weak);
            Assert.AreEqual(1, checks.Count, "only the one pair whose two tokens exist is measured");
            Assert.IsFalse(checks[0].Passed);
            Assert.AreEqual(ColorMath.TextAa, checks[0].Required);
        }

        [Test]
        public void Audit_RequiresMoreAtTheHighLevel()
        {
            Assert.AreEqual(4.5f, ContrastAudit.RequiredText(ContrastLevel.Standard));
            Assert.AreEqual(7f, ContrastAudit.RequiredText(ContrastLevel.High));
            Assert.AreEqual(3f, ContrastAudit.RequiredNonText(ContrastLevel.Standard));
            Assert.AreEqual(4.5f, ContrastAudit.RequiredNonText(ContrastLevel.High));
        }

        [Test]
        public void ShippedDarkAndLight_AreNotTheHighContrastPalettes()
        {
            // The standard themes meet AA but are not AAA: if they passed the High audit there would be nothing to ship.
            Assert.IsNotEmpty(ContrastAudit.Failures(ThemePalettes.Dark, ContrastLevel.High));
            Assert.IsNotEmpty(ContrastAudit.Failures(ThemePalettes.Light, ContrastLevel.High));
        }

        // ----- derivation

        [Test]
        public void HighContrast_OfALowContrastPalette_PassesTheHighAudit_AndKeepsTheBrandHue()
        {
            var source = new Dictionary<string, Rgba>(ThemePalettes.Dark)
            {
                [ThemeTokens.Primary] = Hex("#8B5A2B"),                    // a muddy brown brand colour
                [ThemeTokens.TextSecondary] = Hex("#555E70"),              // too weak on the dark surfaces
            };

            var hc = ThemeDerivation.HighContrast(source, ContrastLevel.High);

            Assert.IsEmpty(ContrastAudit.Failures(hc, ContrastLevel.High));
            var p = hc[ThemeTokens.Primary];
            Assert.Greater(p.R, p.G, "brown keeps red above green");
            Assert.Greater(p.G, p.B, "brown keeps green above blue");
            Assert.AreEqual(Hex("#FFFFFF"), hc[ThemeTokens.TextPrimary]);
        }

        [Test]
        public void HighContrast_DoesNotMutateItsSource()
        {
            var before = ThemePalettes.Light[ThemeTokens.TextPrimary];
            ThemeDerivation.HighContrast(ThemePalettes.Light, ContrastLevel.High);
            Assert.AreEqual(before, ThemePalettes.Light[ThemeTokens.TextPrimary]);
        }

        [Test]
        public void HighContrast_StandardLevel_KeepsTheNeutralsAndOnlyFixesFailures()
        {
            var hc = ThemeDerivation.HighContrast(ThemePalettes.Dark, ContrastLevel.Standard);
            Assert.AreEqual(ThemePalettes.Dark[ThemeTokens.Bg], hc[ThemeTokens.Bg]);
            Assert.AreEqual(ThemePalettes.Dark[ThemeTokens.Surface], hc[ThemeTokens.Surface]);
            Assert.IsEmpty(ContrastAudit.Failures(hc, ContrastLevel.Standard));
        }

        [Test]
        public void IsDark_FollowsTheBackgroundLuminance()
        {
            Assert.IsTrue(ThemeDerivation.IsDark(ThemePalettes.Dark));
            Assert.IsFalse(ThemeDerivation.IsDark(ThemePalettes.Light));
            Assert.IsTrue(ThemeDerivation.IsDark(new Dictionary<string, Rgba>()), "no background counts as dark");
        }

        [TestCase("#3B82F6", "#0B0F17", true)]
        [TestCase("#EF4444", "#101820", true)]
        [TestCase("#16A34A", "#F8FAFC", false)]
        [TestCase("#7C3AED", "#FFFFFF", false)]
        [TestCase("#F59E0B", "#1B1B1B", true)]
        public void FromBrand_ProducesTextAndBorderColoursThatPassAa_AndKeepsThePrimaryUntouched(string primary, string bg, bool dark)
        {
            var palette = ThemeDerivation.FromBrand(Hex(primary), Hex(bg), dark);

            Assert.AreEqual(Hex(primary), palette[ThemeTokens.Primary], "the brand colour is never altered");

            // Only the pairs whose tokens FromBrand produces exist in the dictionary; those must pass.
            var failures = ContrastAudit.Failures(palette).Where(f => f.Kind != ContrastCheckKind.Accent).ToList();
            Assert.IsEmpty(failures, string.Join("; ", failures.Select(f => f.ToString())));
        }

        [Test]
        public void FromBrand_TextOnAccent_IsTheBetterInkForThePrimary()
        {
            var yellow = ThemeDerivation.FromBrand(Hex("#FACC15"), Hex("#101010"), true);
            Assert.AreEqual(new Rgba(0f, 0f, 0f), yellow[ThemeTokens.TextOnAccent]);

            var navy = ThemeDerivation.FromBrand(Hex("#1E3A8A"), Hex("#FFFFFF"), false);
            Assert.AreEqual(new Rgba(1f, 1f, 1f), navy[ThemeTokens.TextOnAccent]);
        }

        [Test]
        public void FromBrand_AWeakBrandColour_IsReportedByTheAudit_NotSilentlyChanged()
        {
            var palette = ThemeDerivation.FromBrand(Hex("#FFEE99"), Hex("#FFFFFF"), false);
            Assert.AreEqual(Hex("#FFEE99"), palette[ThemeTokens.Primary]);
            Assert.IsTrue(ContrastAudit.Failures(palette).Any(f => f.Foreground == ThemeTokens.Primary),
                "pale yellow as an accent on white must be reported");
        }

        [Test]
        public void ShippedPalettes_DefineEveryColourToken()
        {
            var names = typeof(ThemeTokens).GetFields().Where(f => f.IsLiteral).Select(f => (string)f.GetRawConstantValue()).ToList();
            foreach (var palette in new[] { ThemePalettes.Dark, ThemePalettes.Light, ThemePalettes.NeutralDark, ThemePalettes.NeutralLight })
                foreach (var name in names)
                    Assert.IsTrue(palette.ContainsKey(name), name + " is missing");
        }
    }
}
