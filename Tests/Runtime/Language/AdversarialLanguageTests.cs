using System;
using FutureDigitalPlus.UIKit.Foundation;
using NUnit.Framework;
using UnityEngine.TestTools;

namespace FutureDigitalPlus.UIKit.Language.Tests
{
    /// <summary>Text lookups and locale changes with hostile data: broken patterns, null parts, malformed files, a backend that throws, a language switched while labels are bound.</summary>
    public class AdversarialLanguageTests
    {
        private sealed class ThrowingBackend : ILocalizationBackend
        {
            public string CurrentLocale { get { throw new InvalidOperationException("locale"); } }
            public event Action Changed;
            public LookupStatus TryGet(string table, string key, object[] args, out string text) { throw new InvalidOperationException("lookup"); }
            public void Raise() => Changed?.Invoke();
        }

        [SetUp]
        public void SetUp() => LogAssert.ignoreFailingMessages = true;

        [TearDown]
        public void TearDown() => LogAssert.ignoreFailingMessages = false;

        [Test]
        public void BrokenFormatPatterns_AreShownAsTheyAre()
        {
            var source = new DictionaryTextSource("en");
            source.Add("en", "UI", "open", "Hello {0");
            source.Add("en", "UI", "index", "Hello {7}");
            source.Add("en", "UI", "brace", "100%} done {{ok}}");
            var localization = new UiLocalization(source);

            Assert.AreEqual("Hello {0", localization.Resolve(UiText.FromKey("open").WithArgs("x")));
            Assert.AreEqual("Hello {7}", localization.Resolve(UiText.FromKey("index").WithArgs("x")));
            Assert.DoesNotThrow(() => localization.Resolve(UiText.FromKey("brace").WithArgs("x")));
            Assert.DoesNotThrow(() => localization.Resolve(UiText.FromLiteral("{0} {1}").WithArgs(null, 5)));
        }

        [Test]
        public void ABackendThatThrows_FallsBackToTheTextInCodeWithoutThrowing()
        {
            var backend = new ThrowingBackend();
            var localization = new UiLocalization(backend) { LogMissingTables = false };

            Assert.AreEqual("en", localization.CurrentLocale);
            Assert.AreEqual("Play", localization.Resolve(UiText.FromKey("play").WithFallback("Play")));
            Assert.AreEqual("[play]", localization.Resolve(UiText.FromKey("play")));
            Assert.DoesNotThrow(() => backend.Raise());
        }

        [Test]
        public void MalformedJsonAndCsv_AreIgnoredNotThrown()
        {
            var source = new DictionaryTextSource("en");
            foreach (var json in new[] { "{", "[]", "null", "{\"entries\":null}", "{\"entries\":[null,{}]}", "\0", "{\"entries\":[{\"key\":\"a\"}]}" })
                Assert.DoesNotThrow(() => source.LoadJson(json), json);
            foreach (var csv in new[] { "\"", "table,key\n\"unterminated", ",,,\n,,,", "\n\n\n", "a,b,c\n1", "Table,Key,en\nT,k,\"v\"\"x\"" })
                Assert.DoesNotThrow(() => source.LoadCsv(csv), csv);
            Assert.AreEqual(0, source.LoadJson(null));
            Assert.AreEqual(0, source.LoadCsv(null));
            Assert.AreEqual(LookupStatus.Found, source.TryGet("T", "k", null, out var text));
            Assert.AreEqual("v\"x", text);
        }

        [Test]
        public void LocaleNamesFromTheOutsideWorld_AreNormalised()
        {
            var source = new DictionaryTextSource(null, null);
            Assert.AreEqual("en", source.CurrentLocale);
            source.Add("pt_BR", "UI", "k", "olá");
            source.Add("en", "UI", "k", "hi");
            source.SetLocale("pt_BR");
            Assert.AreEqual("pt-BR", source.CurrentLocale);
            Assert.AreEqual(LookupStatus.Found, source.TryGet("UI", "k", null, out var text));
            source.SetLocale("  ");
            Assert.AreEqual("en", source.CurrentLocale);
            Assert.AreEqual(LookupStatus.TableMissing, source.TryGet("Nope", "k", null, out text));
            Assert.AreEqual(LookupStatus.EntryMissing, source.TryGet("UI", null, null, out text));
        }
    }
}
