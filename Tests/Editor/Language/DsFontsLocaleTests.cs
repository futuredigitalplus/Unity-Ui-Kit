using FutureDigitalPlus.UIKit.DesignSystem.Typography;
using NUnit.Framework;

namespace FutureDigitalPlus.UIKit.Language.Tests
{
    public class DsFontsLocaleTests
    {
        private DsScripts.HanRegion _before;

        [SetUp]
        public void SetUp() => _before = DsScripts.Han;

        [TearDown]
        public void TearDown() => DsScripts.Han = _before;

        [TestCase("ja", DsScripts.HanRegion.Japanese)]
        [TestCase("ja-JP", DsScripts.HanRegion.Japanese)]
        [TestCase("ko", DsScripts.HanRegion.Korean)]
        [TestCase("zh-Hant", DsScripts.HanRegion.TraditionalChinese)]
        [TestCase("zh-TW", DsScripts.HanRegion.TraditionalChinese)]
        [TestCase("zh-HK", DsScripts.HanRegion.TraditionalChinese)]
        [TestCase("zh-Hans", DsScripts.HanRegion.SimplifiedChinese)]
        [TestCase("zh-CN", DsScripts.HanRegion.SimplifiedChinese)]
        public void TheHanRegionFollowsTheLocale(string locale, DsScripts.HanRegion expected)
        {
            DsScripts.Han = expected == DsScripts.HanRegion.Korean ? DsScripts.HanRegion.Japanese : DsScripts.HanRegion.Korean;

            Assert.IsTrue(DsFonts.ApplyLocale(locale));

            Assert.AreEqual(expected, DsScripts.Han);
        }

        [TestCase("en")]
        [TestCase("ar")]
        [TestCase("")]
        [TestCase(null)]
        public void ALanguageWithoutHanCharacters_LeavesTheRegionAlone(string locale)
        {
            DsScripts.Han = DsScripts.HanRegion.TraditionalChinese;

            DsFonts.ApplyLocale(locale);

            Assert.AreEqual(DsScripts.HanRegion.TraditionalChinese, DsScripts.Han);
        }
    }
}
