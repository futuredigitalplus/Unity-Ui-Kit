using FutureDigitalPlus.UIKit.Foundation;
using NUnit.Framework;

namespace FutureDigitalPlus.UIKit.Language.Tests
{
    public class DictionaryTextSourceTests
    {
        private static DictionaryTextSource Make(string locale = "en")
        {
            var source = new DictionaryTextSource(locale);
            source.Add("en", "UI", "PLAY", "Play");
            source.Add("de", "UI", "PLAY", "Spielen");
            source.Add("pt", "UI", "PLAY", "Jogar");
            source.Add("pt-BR", "UI", "SAVE", "Salvar (BR)");
            source.Add("en", "UI", "SAVE", "Save");
            source.Add("en", "UI", "GREETING", "Hello {0}, you have {1} coins");
            source.Add("en", null, "PLAIN", "No table given");
            return source;
        }

        [Test]
        public void TheActiveLocaleAnswersFirst()
        {
            Assert.AreEqual(LookupStatus.Found, Make("de").TryGet("UI", "PLAY", null, out var text));
            Assert.AreEqual("Spielen", text);
        }

        [Test]
        public void ARegionalLocaleFallsBackToItsLanguageThenToTheFallbackLocale()
        {
            var source = Make("pt-BR");
            source.TryGet("UI", "SAVE", null, out var save);
            source.TryGet("UI", "PLAY", null, out var play);
            Assert.AreEqual("Salvar (BR)", save, "exact locale");
            Assert.AreEqual("Jogar", play, "language of the locale");

            source.SetLocale("fr");
            source.TryGet("UI", "PLAY", null, out play);
            Assert.AreEqual("Play", play, "fallback locale");
        }

        [Test]
        public void TheFallbackLocaleIsConfigurable()
        {
            var source = Make("fr");
            source.FallbackLocale = "de";
            source.TryGet("UI", "PLAY", null, out var text);
            Assert.AreEqual("Spielen", text);
        }

        [Test]
        public void AMissingTableAndAMissingEntryAreTold_Apart()
        {
            var source = Make();
            Assert.AreEqual(LookupStatus.TableMissing, source.TryGet("Nope", "PLAY", null, out var none));
            Assert.IsNull(none);
            Assert.AreEqual(LookupStatus.EntryMissing, source.TryGet("UI", "NOPE", null, out none));
            Assert.IsNull(none);
            Assert.AreEqual(LookupStatus.EntryMissing, source.TryGet("UI", null, null, out none));
        }

        [Test]
        public void ABlankTableMeansTheDefaultTable()
        {
            var source = Make();
            Assert.AreEqual(LookupStatus.Found, source.TryGet(null, "PLAIN", null, out var text));
            Assert.AreEqual("No table given", text);
            Assert.AreEqual(LookupStatus.Found, source.TryGet(DictionaryTextSource.DefaultTable, "PLAIN", null, out text));
        }

        [Test]
        public void ArgumentsFillThePlaceholders_AndABrokenPatternIsReturnedAsIs()
        {
            var source = Make();
            source.TryGet("UI", "GREETING", new object[] { "Ana", 7 }, out var text);
            Assert.AreEqual("Hello Ana, you have 7 coins", text);

            source.Add("en", "UI", "BROKEN", "Open { brace");
            source.TryGet("UI", "BROKEN", new object[] { 1 }, out text);
            Assert.AreEqual("Open { brace", text);
        }

        [Test]
        public void LocaleCodesAreNormalisedAndCaseInsensitive()
        {
            var source = new DictionaryTextSource("pt_br");
            Assert.AreEqual("pt-br", source.CurrentLocale.ToLowerInvariant());
            source.Add("PT-br", "UI", "X", "ok");
            Assert.AreEqual(LookupStatus.Found, source.TryGet("UI", "X", null, out _));
            Assert.AreEqual("en", new DictionaryTextSource(null).CurrentLocale);
            Assert.AreEqual("en", new DictionaryTextSource("  ").CurrentLocale);
        }

        [Test]
        public void SetLocaleRaisesChangedOnlyWhenTheLocaleDiffers()
        {
            var source = Make();
            var raised = 0;
            source.Changed += () => raised++;

            source.SetLocale("en");
            Assert.AreEqual(0, raised);
            source.SetLocale("EN");
            Assert.AreEqual(0, raised, "same locale, other case");
            source.SetLocale("de");
            Assert.AreEqual(1, raised);
            source.NotifyChanged();
            Assert.AreEqual(2, raised);
        }

        [Test]
        public void AddAndClearDoNotRaiseChanged()
        {
            var source = Make();
            var raised = 0;
            source.Changed += () => raised++;
            source.Add("en", "UI", "NEW", "new");
            source.Clear();
            Assert.AreEqual(0, raised);
            Assert.AreEqual(LookupStatus.TableMissing, source.TryGet("UI", "NEW", null, out _));
        }

        [Test]
        public void AnEmptyKeyOrANullValueIsIgnored()
        {
            var source = new DictionaryTextSource();
            source.Add("en", "UI", "", "x");
            source.Add("en", "UI", null, "x");
            source.Add("en", "UI", "K", null);
            Assert.IsEmpty(source.Locales);
        }

        // ----- CSV

        [Test]
        public void CsvLoadsOneEntryPerLocaleCell()
        {
            var source = new DictionaryTextSource("de");
            var added = source.LoadCsv("table,key,en,de,ar\nUI,PLAY,Play,Spielen,العب\nUI,SAVE,Save,,\n");
            Assert.AreEqual(4, added, "an empty cell adds nothing");
            source.TryGet("UI", "PLAY", null, out var text);
            Assert.AreEqual("Spielen", text);
            source.SetLocale("ar");
            source.TryGet("UI", "PLAY", null, out text);
            Assert.AreEqual("العب", text);
            source.TryGet("UI", "SAVE", null, out text);
            Assert.AreEqual("Save", text, "falls back to en where ar is empty");
        }

        [Test]
        public void CsvHandlesQuotesCommasLineBreaksCrLfAndBom()
        {
            var csv = "﻿table,key,en\r\n" +
                      ",A,\"One, two\"\r\n" +
                      "UI,B,\"She said \"\"hi\"\"\"\r\n" +
                      "UI,C,\"line one\nline two\"\r\n" +
                      "\r\n" +
                      "UI,D,last";
            var source = new DictionaryTextSource();
            Assert.AreEqual(4, source.LoadCsv(csv));

            source.TryGet(null, "A", null, out var a);
            source.TryGet("UI", "B", null, out var b);
            source.TryGet("UI", "C", null, out var c);
            source.TryGet("UI", "D", null, out var d);
            Assert.AreEqual("One, two", a);
            Assert.AreEqual("She said \"hi\"", b);
            Assert.AreEqual("line one\nline two", c);
            Assert.AreEqual("last", d, "no trailing newline");
        }

        [Test]
        public void CsvWithoutRowsOrEmptyLoadsNothing()
        {
            var source = new DictionaryTextSource();
            Assert.AreEqual(0, source.LoadCsv(null));
            Assert.AreEqual(0, source.LoadCsv(""));
            Assert.AreEqual(0, source.LoadCsv("table,key,en"));
            Assert.AreEqual(0, source.LoadCsv("table,key,en\nonly"));
        }

        // ----- as a backend

        [Test]
        public void TheResolverShowsTheTextTheFallbackOrAPlaceholder_AndNeverThrows()
        {
            var source = Make();
            var resolver = new LocalizationKeyResolver(
                (string table, string key, object[] args, out string text) => source.TryGet(table, key, args, out text),
                new[] { "UI" });

            Assert.AreEqual("Play", resolver.Resolve(UiText.FromKey("PLAY")));
            Assert.AreEqual("Hello Ana, you have 7 coins", resolver.Resolve(UiText.FromKey("GREETING", "Ana", 7)));
            Assert.AreEqual("Fallback", resolver.Resolve(UiText.FromKey("NOPE").WithFallback("Fallback")));
            Assert.AreEqual("[NOPE]", resolver.Resolve(UiText.FromKey("NOPE")));
        }
    }
}
