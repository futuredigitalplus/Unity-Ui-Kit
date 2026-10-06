using System;
using System.Collections.Generic;
using NUnit.Framework;

namespace FutureDigitalPlus.UIKit.Foundation.Tests
{
    [TestFixture]
    public sealed class LocalizationKeyResolverTests
    {
        static readonly string[] Tables = { "Core", "Borderline", "UI" };

        sealed class FakeTables
        {
            public readonly Dictionary<string, Dictionary<string, string>> Tables = new Dictionary<string, Dictionary<string, string>>();
            public readonly List<string> Calls = new List<string>();
            public string ThrowForTable;

            public FakeTables Add(string table, string key, string text)
            {
                Dictionary<string, string> entries;
                if (!Tables.TryGetValue(table, out entries))
                {
                    entries = new Dictionary<string, string>();
                    Tables[table] = entries;
                }
                entries[key] = text;
                return this;
            }

            public LookupStatus Lookup(string table, string key, object[] args, out string text)
            {
                Calls.Add(table + ":" + key);
                text = null;
                if (table == ThrowForTable)
                    throw new InvalidOperationException("table exploded");
                Dictionary<string, string> entries;
                if (!Tables.TryGetValue(table, out entries))
                    return LookupStatus.TableMissing;
                string value;
                if (!entries.TryGetValue(key, out value))
                    return LookupStatus.EntryMissing;
                text = args != null && args.Length > 0 ? string.Format(value, args) : value;
                return LookupStatus.Found;
            }
        }

        [Test]
        public void Resolve_FindsTheKeyInTheCoreTable()
        {
            FakeTables fake = new FakeTables().Add("Core", "HOMEBTN-PLAY", "Play Current Game");
            LocalizationKeyResolver resolver = new LocalizationKeyResolver(fake.Lookup, Tables);
            Assert.That(resolver.Resolve(UiText.FromKey("HOMEBTN-PLAY")), Is.EqualTo("Play Current Game"));
        }

        [Test]
        public void Resolve_FallsThroughToTheBorderlineTable()
        {
            FakeTables fake = new FakeTables().Add("Core", "A", "a").Add("Borderline", "GOTOFAV-ALERT-TITLE", "My Favorite Games");
            LocalizationKeyResolver resolver = new LocalizationKeyResolver(fake.Lookup, Tables);
            Assert.That(resolver.Resolve(UiText.FromKey("GOTOFAV-ALERT-TITLE")), Is.EqualTo("My Favorite Games"));
            Assert.That(fake.Calls, Is.EqualTo(new[] { "Core:GOTOFAV-ALERT-TITLE", "Borderline:GOTOFAV-ALERT-TITLE" }));
        }

        [Test]
        public void Resolve_ANamedTable_IsTheOnlyOneAsked()
        {
            FakeTables fake = new FakeTables().Add("Core", "K", "core").Add("Other", "K", "other");
            LocalizationKeyResolver resolver = new LocalizationKeyResolver(fake.Lookup, Tables);
            Assert.That(resolver.Resolve(UiText.FromKeyIn("Other", "K")), Is.EqualTo("other"));
            Assert.That(resolver.Resolve(UiText.FromKey("Other:K")), Is.EqualTo("other"));
            Assert.That(fake.Calls, Is.EqualTo(new[] { "Other:K", "Other:K" }));
        }

        [Test]
        public void Resolve_AMissingTable_UsesTheFallbackAndIsAskedOnlyOnce()
        {
            FakeTables fake = new FakeTables();
            LocalizationKeyResolver resolver = new LocalizationKeyResolver(fake.Lookup, Tables);
            List<string> reported = new List<string>();
            resolver.TableMissing += reported.Add;

            UiText text = UiText.FromKey("HOMEBTN-PLAY").WithFallback("Play");
            Assert.That(resolver.Resolve(text), Is.EqualTo("Play"));
            Assert.That(resolver.Resolve(text), Is.EqualTo("Play"));
            Assert.That(resolver.Resolve(UiText.FromKey("OTHER")), Is.EqualTo("[OTHER]"));

            Assert.That(fake.Calls, Is.EqualTo(new[] { "Core:HOMEBTN-PLAY", "Borderline:HOMEBTN-PLAY", "UI:HOMEBTN-PLAY" }), "each missing table is asked once");
            Assert.That(reported, Is.EqualTo(new[] { "Core", "Borderline", "UI" }));
            Assert.That(resolver.MissingTables, Is.EquivalentTo(new[] { "Core", "Borderline", "UI" }));
        }

        [Test]
        public void Resolve_AMissingKey_ShowsThePlaceholderWhenThereIsNoFallback()
        {
            FakeTables fake = new FakeTables().Add("Core", "A", "a");
            LocalizationKeyResolver resolver = new LocalizationKeyResolver(fake.Lookup, Tables);
            Assert.That(resolver.Resolve(UiText.FromKey("NOPE")), Is.EqualTo("[NOPE]"));
            resolver.MissingKeyFormat = "!{0}!";
            Assert.That(resolver.Resolve(UiText.FromKey("NOPE")), Is.EqualTo("!NOPE!"));
        }

        [Test]
        public void Resolve_AMissingEntry_DoesNotMarkTheTableMissing()
        {
            FakeTables fake = new FakeTables().Add("Core", "A", "a");
            LocalizationKeyResolver resolver = new LocalizationKeyResolver(fake.Lookup, new[] { "Core" });
            resolver.Resolve(UiText.FromKey("NOPE"));
            resolver.Resolve(UiText.FromKey("A"));
            Assert.That(resolver.MissingTables, Is.Empty);
            Assert.That(resolver.Resolve(UiText.FromKey("A")), Is.EqualTo("a"));
        }

        [Test]
        public void Resolve_ALookupThatThrows_IsContainedAndTheTableIsDropped()
        {
            FakeTables fake = new FakeTables { ThrowForTable = "Core" };
            fake.Add("Borderline", "K", "found in borderline");
            LocalizationKeyResolver resolver = new LocalizationKeyResolver(fake.Lookup, Tables);
            Assert.That(resolver.Resolve(UiText.FromKey("K")), Is.EqualTo("found in borderline"));
            Assert.That(resolver.MissingTables, Does.Contain("Core"));
            int callsAfterFirst = fake.Calls.Count;
            resolver.Resolve(UiText.FromKey("K"));
            Assert.That(fake.Calls.Count, Is.EqualTo(callsAfterFirst + 1), "Core is not asked again, only Borderline");
        }

        [Test]
        public void ResetMissing_AsksTheTablesAgain()
        {
            FakeTables fake = new FakeTables();
            LocalizationKeyResolver resolver = new LocalizationKeyResolver(fake.Lookup, new[] { "Core" });
            Assert.That(resolver.Resolve(UiText.FromKey("K")), Is.EqualTo("[K]"));
            fake.Add("Core", "K", "now there");
            Assert.That(resolver.Resolve(UiText.FromKey("K")), Is.EqualTo("[K]"), "still cached as missing");
            resolver.ResetMissing();
            Assert.That(resolver.Resolve(UiText.FromKey("K")), Is.EqualTo("now there"));
        }

        [Test]
        public void Resolve_WithoutALookup_UsesFallbacksOnly()
        {
            LocalizationKeyResolver resolver = new LocalizationKeyResolver(null);
            Assert.That(resolver.Resolve(UiText.FromKey("K").WithFallback("Fallback")), Is.EqualTo("Fallback"));
            Assert.That(resolver.Resolve(UiText.FromKey("K")), Is.EqualTo("[K]"));
        }

        [Test]
        public void Resolve_PassesArgumentsToTheTableAndFormatsFallbacks()
        {
            FakeTables fake = new FakeTables().Add("Core", "MOVES", "Moves: {0}");
            LocalizationKeyResolver resolver = new LocalizationKeyResolver(fake.Lookup, Tables);
            Assert.That(resolver.Resolve(UiText.FromKey("MOVES", 12)), Is.EqualTo("Moves: 12"));

            LocalizationKeyResolver empty = new LocalizationKeyResolver(new FakeTables().Lookup, Tables);
            Assert.That(empty.Resolve(UiText.FromKey("MOVES", 12).WithFallback("Moves: {0}")), Is.EqualTo("Moves: 12"));
        }

        [Test]
        public void Resolve_UsesTheInvariantCultureForNumbers()
        {
            LocalizationKeyResolver resolver = new LocalizationKeyResolver(null);
            Assert.That(resolver.Resolve(UiText.FromKey("K", 1234.5).WithFallback("{0}")), Is.EqualTo("1234.5"));
        }

        [Test]
        public void Resolve_ABrokenFormatPattern_ReturnsThePatternUnchanged()
        {
            LocalizationKeyResolver resolver = new LocalizationKeyResolver(null);
            Assert.That(resolver.Resolve(UiText.FromKey("K", 1).WithFallback("{0")), Is.EqualTo("{0"));
        }

        [Test]
        public void Resolve_LiteralsAndCompositions()
        {
            FakeTables fake = new FakeTables().Add("Core", "BY", "By").Add("Core", "NAME", "Name");
            LocalizationKeyResolver resolver = new LocalizationKeyResolver(fake.Lookup, Tables);
            Assert.That(resolver.Resolve(UiText.FromLiteral("Server says hi")), Is.EqualTo("Server says hi"));
            Assert.That(resolver.Resolve(UiText.Compose(" ", UiText.FromKey("BY"), UiText.FromKey("NAME"))), Is.EqualTo("By Name"));
            Assert.That(resolver.Resolve(UiText.Compose("\n", UiText.FromKey("BY"), UiText.FromLiteral("42"))), Is.EqualTo("By\n42"));
        }

        [Test]
        public void Resolve_EmptyOrNull_GivesAnEmptyString()
        {
            LocalizationKeyResolver resolver = new LocalizationKeyResolver(null);
            Assert.That(resolver.Resolve(null), Is.EqualTo(string.Empty));
            Assert.That(resolver.Resolve(UiText.Empty), Is.EqualTo(string.Empty));
        }
    }
}
