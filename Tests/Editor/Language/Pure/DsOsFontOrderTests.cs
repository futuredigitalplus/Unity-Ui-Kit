using System.Collections.Generic;
using FutureDigitalPlus.UIKit.DesignSystem.Typography;
using NUnit.Framework;

namespace FutureDigitalPlus.UIKit.Language.Tests
{
    public class DsOsFontOrderTests
    {
        private static readonly string[] Apple = { "PingFang SC", "PingFang TC", "Hiragino Sans", "Apple SD Gothic Neo", "Helvetica" };

        [TestCase("ja", "ja")]
        [TestCase("ja-JP", "ja")]
        [TestCase("ko", "ko")]
        [TestCase("zh-Hans", "zh-Hans")]
        [TestCase("zh-CN", "zh-Hans")]
        [TestCase("zh", "zh-Hans")]
        [TestCase("zh-Hant", "zh-Hant")]
        [TestCase("zh-TW", "zh-Hant")]
        [TestCase("zh-HK", "zh-Hant")]
        [TestCase("zh-MO", "zh-Hant")]
        public void TheHanGroupFollowsTheLocale(string code, string group)
        {
            Assert.AreEqual(group, DsOsFontOrder.HanGroupOf(code));
        }

        [TestCase("en")]
        [TestCase("de")]
        [TestCase("ar")]
        [TestCase("")]
        [TestCase(null)]
        public void ALocaleWithoutHanCharactersHasNoHanGroup(string code)
        {
            Assert.IsNull(DsOsFontOrder.HanGroupOf(code));
        }

        [Test]
        public void ALanguageThatOnlySharesAFirstLetterIsNotMistaken()
        {
            // "kok" (Konkani) and "jv" (Javanese) share a first letter with ko and ja
            Assert.IsNull(DsOsFontOrder.HanGroupOf("kok"));
            Assert.IsNull(DsOsFontOrder.HanGroupOf("jv"));
        }

        [Test]
        public void TheLocalesOwnTypefaceComesFirst()
        {
            Assert.AreEqual("Hiragino Sans", DsOsFontOrder.HanCandidates("ja", Apple)[0]);
            Assert.AreEqual("Apple SD Gothic Neo", DsOsFontOrder.HanCandidates("ko", Apple)[0]);
            Assert.AreEqual("PingFang TC", DsOsFontOrder.HanCandidates("zh-Hant", Apple)[0]);
            Assert.AreEqual("PingFang SC", DsOsFontOrder.HanCandidates("zh-Hans", Apple)[0]);
        }

        [Test]
        public void EveryInstalledHanFontIsKeptAsAFallbackAndNothingElse()
        {
            var list = DsOsFontOrder.HanCandidates("en", Apple);
            CollectionAssert.AreEquivalent(new[] { "PingFang SC", "PingFang TC", "Hiragino Sans", "Apple SD Gothic Neo" }, list);
            Assert.AreEqual(list.Count, new HashSet<string>(list).Count, "no duplicates");
        }

        [Test]
        public void NothingInstalledMeansNoCandidates()
        {
            Assert.IsEmpty(DsOsFontOrder.HanCandidates("ja", new string[0]));
            Assert.IsEmpty(DsOsFontOrder.HanCandidates("ja", null));
            Assert.IsEmpty(DsOsFontOrder.ShapingCandidates("arabic", new string[0]));
        }

        [Test]
        public void FamilyNamesMatchWithoutRegardToCase()
        {
            CollectionAssert.Contains(DsOsFontOrder.HanCandidates("ja", new[] { "hiragino sans" }), "Hiragino Sans");
            CollectionAssert.Contains(DsOsFontOrder.ShapingCandidates("arabic", new[] { "geeza pro" }), "Geeza Pro");
        }

        [Test]
        public void SystemFontNamesAreGroupedByFamily()
        {
            var map = DsOsFontOrder.GroupByFamily(new[] { "Hiragino Sans - W3", "Hiragino Sans - W6", "Heiti SC - Medium", "Plain", "", null });
            CollectionAssert.AreEquivalent(new[] { "Hiragino Sans", "Heiti SC", "Plain" }, map.Keys);
            CollectionAssert.AreEqual(new[] { "W3", "W6" }, map["hiragino sans"]);
            Assert.AreEqual("Regular", map["Plain"][0]);
            Assert.IsEmpty(DsOsFontOrder.GroupByFamily(null));
        }

        [Test]
        public void TheRegularStyleIsPreferred()
        {
            Assert.AreEqual("Regular", DsOsFontOrder.PickStyle(new[] { "Bold", "Regular" }));
            Assert.AreEqual("W3", DsOsFontOrder.PickStyle(new[] { "W6", "W3" }));
            Assert.AreEqual("Heavy", DsOsFontOrder.PickStyle(new[] { "Heavy", "Black" }));
            Assert.AreEqual("Regular", DsOsFontOrder.PickStyle(new string[0]));
            Assert.AreEqual("Regular", DsOsFontOrder.PickStyle(null));
        }

        [TestCase("ar", "arabic")]
        [TestCase("ar-SA", "arabic")]
        [TestCase("fa", "arabic")]
        [TestCase("ur", "arabic")]
        [TestCase("he", "hebrew")]
        [TestCase("th", "thai")]
        [TestCase("hi", "devanagari")]
        [TestCase("mr", "devanagari")]
        [TestCase("bn", "bengali")]
        [TestCase("ta", "tamil")]
        [TestCase("km", "khmer")]
        public void AShapingLanguageMapsToItsScript(string code, string script)
        {
            Assert.AreEqual(script, DsOsFontOrder.ShapingScriptOf(code));
        }

        [TestCase("en")]
        [TestCase("ja")]
        [TestCase("zh-Hans")]
        [TestCase("de")]
        [TestCase("hu")]     // starts with "h" like hi and he
        [TestCase("tr")]     // starts with "t" like th and ta
        [TestCase("")]
        [TestCase(null)]
        public void ALanguageThatNeedsNoShapingHasNoScript(string code)
        {
            Assert.IsNull(DsOsFontOrder.ShapingScriptOf(code));
        }

        [Test]
        public void EveryShapingScriptHasCandidates_InPreferenceOrder_EachOnce()
        {
            var everything = new[]
            {
                "Geeza Pro", "Noto Sans Arabic", "Segoe UI", "Tahoma", "Arial Hebrew", "Arial", "Thonburi", "Kohinoor Devanagari",
                "Nirmala UI", "Bangla Sangam MN", "Tamil Sangam MN", "Khmer Sangam MN", "Leelawadee UI",
            };

            foreach (var script in DsOsFontOrder.ShapingScripts)
            {
                var list = DsOsFontOrder.ShapingCandidates(script, everything);
                Assert.IsNotEmpty(list, script);
                Assert.AreEqual(list.Count, new HashSet<string>(list).Count, script + " has duplicates");
            }

            Assert.AreEqual("Geeza Pro", DsOsFontOrder.ShapingCandidates("arabic", everything)[0]);
            Assert.AreEqual("Noto Sans Arabic", DsOsFontOrder.ShapingCandidates("arabic", new[] { "Tahoma", "Noto Sans Arabic" })[0]);
        }

        [Test]
        public void AnUnknownScriptHasNoCandidates()
        {
            Assert.IsEmpty(DsOsFontOrder.ShapingCandidates("klingon", Apple));
            Assert.IsEmpty(DsOsFontOrder.ShapingCandidates(null, Apple));
        }
    }
}
