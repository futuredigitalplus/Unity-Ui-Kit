using NUnit.Framework;

namespace FutureDigitalPlus.UIKit.Language.Tests
{
    public class DictionaryTextSourceJsonTests
    {
        [Test]
        public void EntriesAreLoadedWithTheirLocaleAndTable()
        {
            var source = new DictionaryTextSource("de");
            var added = source.LoadJson(
                "{\"entries\":[{\"locale\":\"en\",\"table\":\"UI\",\"key\":\"HOME\",\"value\":\"Home\"}," +
                "{\"locale\":\"de\",\"table\":\"UI\",\"key\":\"HOME\",\"value\":\"Start\"}," +
                "{\"locale\":\"de\",\"key\":\"NOTABLE\",\"value\":\"ohne Tabelle\"}]}");

            Assert.AreEqual(3, added);
            source.TryGet("UI", "HOME", null, out var home);
            source.TryGet(null, "NOTABLE", null, out var noTable);
            Assert.AreEqual("Start", home);
            Assert.AreEqual("ohne Tabelle", noTable, "a missing table means the default table");
        }

        [Test]
        public void EntriesWithoutAKeyOrValueAreSkipped()
        {
            var source = new DictionaryTextSource();
            var added = source.LoadJson("{\"entries\":[{\"locale\":\"en\",\"key\":\"\",\"value\":\"x\"},{\"locale\":\"en\",\"key\":\"K\"},{\"locale\":\"en\",\"key\":\"OK\",\"value\":\"fine\"}]}");

            Assert.AreEqual(1, added);
        }

        [TestCase(null)]
        [TestCase("")]
        [TestCase("   ")]
        [TestCase("not json at all")]
        [TestCase("{\"other\":1}")]
        public void TextThatIsNotTheExpectedJsonLoadsNothing(string json)
        {
            Assert.AreEqual(0, new DictionaryTextSource().LoadJson(json));
        }

        [Test]
        public void ANullSourceLoadsNothing()
        {
            DictionaryTextSource source = null;
            Assert.AreEqual(0, source.LoadJson("{\"entries\":[]}"));
        }
    }
}
