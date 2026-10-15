using System;
using System.Collections.Generic;
using NUnit.Framework;

namespace FutureDigitalPlus.UIKit.Foundation.Tests
{
    [TestFixture]
    public sealed class LocaleFallbackLookupTests
    {
        // A locale chain and one table per locale. A value of null marks an entry that cannot be formatted; a locale without a table
        // answers TableMissing.
        sealed class FakeLocales
        {
            public readonly Dictionary<string, string[]> Fallbacks = new Dictionary<string, string[]>();
            public readonly Dictionary<string, Dictionary<string, string>> Tables = new Dictionary<string, Dictionary<string, string>>();
            public readonly List<string> Asked = new List<string>();
            public string ThrowFallbacksOf;

            public FakeLocales Chain(string locale, params string[] fallbacks)
            {
                Fallbacks[locale] = fallbacks;
                return this;
            }

            public FakeLocales Entry(string locale, string key, string value)
            {
                Dictionary<string, string> table;
                if (!Tables.TryGetValue(locale, out table))
                {
                    table = new Dictionary<string, string>();
                    Tables[locale] = table;
                }
                if (key != null)
                    table[key] = value;
                return this;
            }

            public IEnumerable<string> FallbacksOf(string locale)
            {
                if (locale == ThrowFallbacksOf)
                    throw new InvalidOperationException("broken metadata");
                string[] fallbacks;
                return Fallbacks.TryGetValue(locale, out fallbacks) ? fallbacks : null;
            }

            public LocaleEntryLookup<string> For(string key)
            {
                return (string locale, out string text) =>
                {
                    Asked.Add(locale ?? "<null>");
                    text = null;
                    Dictionary<string, string> table;
                    if (locale == null || !Tables.TryGetValue(locale, out table))
                        return LookupStatus.TableMissing;
                    string value;
                    if (!table.TryGetValue(key, out value) || string.IsNullOrEmpty(value))
                        return LookupStatus.EntryMissing; // missing, empty, or (null) a format failure: all pass the key on
                    text = value;
                    return LookupStatus.Found;
                };
            }

            public LookupStatus Find(string selected, string key, out string text)
            {
                return LocaleFallbackLookup.Find(selected, FallbacksOf, For(key), out text);
            }
        }

        static FakeLocales Spanish()
        {
            return new FakeLocales()
                .Chain("es-AR", "es").Chain("es", "en")
                .Entry("es-AR", "HELLO", "Che, hola")
                .Entry("es", "HELLO", "Hola").Entry("es", "PLAY", "Jugar")
                .Entry("en", "HELLO", "Hello").Entry("en", "PLAY", "Play").Entry("en", "QUIT", "Quit");
        }

        [Test]
        public void TheSelectedLocaleWins_AndNoFallbackIsAsked()
        {
            FakeLocales fake = Spanish();
            string text;
            Assert.That(fake.Find("es-AR", "HELLO", out text), Is.EqualTo(LookupStatus.Found));
            Assert.That(text, Is.EqualTo("Che, hola"));
            Assert.That(fake.Asked, Is.EqualTo(new[] { "es-AR" }));
        }

        [Test]
        public void AMissingRegionalEntry_ComesFromTheParentLanguage_BeforeEnglish()
        {
            FakeLocales fake = Spanish();
            string text;
            Assert.That(fake.Find("es-AR", "PLAY", out text), Is.EqualTo(LookupStatus.Found));
            Assert.That(text, Is.EqualTo("Jugar"));
            Assert.That(fake.Asked, Is.EqualTo(new[] { "es-AR", "es" }));
        }

        [Test]
        public void TheWholeChainIsWalked()
        {
            FakeLocales fake = Spanish();
            string text;
            Assert.That(fake.Find("es-AR", "QUIT", out text), Is.EqualTo(LookupStatus.Found));
            Assert.That(text, Is.EqualTo("Quit"));
            Assert.That(fake.Asked, Is.EqualTo(new[] { "es-AR", "es", "en" }));
        }

        [Test]
        public void AnEmptyOrUnformattableEntry_PassesTheKeyOn()
        {
            FakeLocales fake = Spanish().Entry("es-AR", "PLAY", string.Empty).Entry("es", "QUIT", null);
            string text;
            Assert.That(fake.Find("es-AR", "PLAY", out text), Is.EqualTo(LookupStatus.Found));
            Assert.That(text, Is.EqualTo("Jugar"));
            Assert.That(fake.Find("es-AR", "QUIT", out text), Is.EqualTo(LookupStatus.Found));
            Assert.That(text, Is.EqualTo("Quit"));
        }

        [Test]
        public void AKeyNoLocaleHas_IsAMissingEntry_NotAMissingTable()
        {
            FakeLocales fake = Spanish();
            string text;
            Assert.That(fake.Find("es-AR", "NOPE", out text), Is.EqualTo(LookupStatus.EntryMissing));
            Assert.That(text, Is.Null);
        }

        [Test]
        public void TheSelectedLocalesMissingTable_IsReported_AndNoFallbackIsAsked()
        {
            FakeLocales fake = Spanish().Chain("es-MX", "es");
            string text;
            Assert.That(fake.Find("es-MX", "PLAY", out text), Is.EqualTo(LookupStatus.TableMissing));
            Assert.That(text, Is.Null);
            Assert.That(fake.Asked, Is.EqualTo(new[] { "es-MX" }));
        }

        [Test]
        public void AFallbackLocalesMissingTable_IsSkipped()
        {
            FakeLocales fake = new FakeLocales().Chain("pt-BR", "pt").Chain("pt", "en")
                .Entry("pt-BR", "A", "a").Entry("en", "PLAY", "Play");
            string text;
            Assert.That(fake.Find("pt-BR", "PLAY", out text), Is.EqualTo(LookupStatus.Found));
            Assert.That(text, Is.EqualTo("Play"));
            Assert.That(fake.Asked, Is.EqualTo(new[] { "pt-BR", "pt", "en" }));
        }

        [Test]
        public void AChainThatLoopsBack_EndsAfterEachLocaleOnce()
        {
            FakeLocales fake = new FakeLocales().Chain("a", "b").Chain("b", "c", "a").Chain("c", "a", "b")
                .Entry("a", "X", "x").Entry("b", "X", "x").Entry("c", "X", "x");
            string text;
            Assert.That(fake.Find("a", "NOPE", out text), Is.EqualTo(LookupStatus.EntryMissing));
            Assert.That(fake.Asked, Is.EqualTo(new[] { "a", "b", "c" }));
        }

        [Test]
        public void FallbacksAreBreadthFirst_InDeclaredOrder()
        {
            FakeLocales fake = new FakeLocales().Chain("fr-CA", "fr", "en").Chain("fr", "de")
                .Entry("fr-CA", "A", "a").Entry("fr", "A", "a").Entry("en", "A", "a").Entry("de", "A", "a");
            string text;
            fake.Find("fr-CA", "NOPE", out text);
            Assert.That(fake.Asked, Is.EqualTo(new[] { "fr-CA", "fr", "en", "de" }));
        }

        [Test]
        public void AnEndlessChain_StopsAtMaxLocales()
        {
            FakeLocales fake = new FakeLocales();
            for (int i = 0; i < 100; i++)
                fake.Chain("l" + i, "l" + (i + 1)).Entry("l" + i, "A", "a");
            string text;
            Assert.That(fake.Find("l0", "NOPE", out text), Is.EqualTo(LookupStatus.EntryMissing));
            Assert.That(fake.Asked.Count, Is.EqualTo(LocaleFallbackLookup.MaxLocales));
        }

        [Test]
        public void FallbacksThatThrow_EndTheWalk_AsAMissingEntry()
        {
            List<string> logged = new List<string>();
            Action<UiLogLevel, string, Exception> previous = UiLog.Sink;
            UiLog.Sink = (level, message, exception) => logged.Add(message);
            try
            {
                FakeLocales fake = Spanish();
                fake.ThrowFallbacksOf = "es";
                string text;
                Assert.That(fake.Find("es-AR", "QUIT", out text), Is.EqualTo(LookupStatus.EntryMissing));
                Assert.That(fake.Asked, Is.EqualTo(new[] { "es-AR", "es" }));
                Assert.That(logged, Is.EqualTo(new[] { "broken metadata" }));
            }
            finally
            {
                UiLog.Sink = previous;
            }
        }

        [Test]
        public void NoSelectedLocale_AsksOnce_AndFollowsNothing()
        {
            int calls = 0;
            string text;
            LookupStatus status = LocaleFallbackLookup.Find<string>(null, l => new[] { "en" }, (string l, out string t) =>
            {
                calls++;
                t = null;
                return LookupStatus.EntryMissing;
            }, out text);
            Assert.That(status, Is.EqualTo(LookupStatus.EntryMissing));
            Assert.That(calls, Is.EqualTo(1));
        }

        [Test]
        public void NoFallbackFunction_IsJustTheSelectedLocale()
        {
            FakeLocales fake = Spanish();
            string text;
            Assert.That(LocaleFallbackLookup.Find("es-AR", null, fake.For("PLAY"), out text), Is.EqualTo(LookupStatus.EntryMissing));
            Assert.That(fake.Asked, Is.EqualTo(new[] { "es-AR" }));
        }
    }
}
