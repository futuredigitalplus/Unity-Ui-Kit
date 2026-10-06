using NUnit.Framework;

namespace FutureDigitalPlus.UIKit.Foundation.Tests
{
    [TestFixture]
    public sealed class UiTextTests
    {
        [Test]
        public void FromKey_ParsesAQualifiedKey()
        {
            UiText text = UiText.FromKey("Borderline:NOFAVORITESFOUND");
            Assert.That(text.Table, Is.EqualTo("Borderline"));
            Assert.That(text.Key, Is.EqualTo("NOFAVORITESFOUND"));
            Assert.That(UiText.FromKey("HOMEBTN-PLAY").Table, Is.Null);
        }

        [Test]
        public void FromKey_EmptyGivesTheEmptyText()
        {
            Assert.That(UiText.FromKey(null).IsEmpty, Is.True);
            Assert.That(UiText.FromKey("  ").IsEmpty, Is.True);
            Assert.That(UiText.FromLiteral(null).IsEmpty, Is.True);
            Assert.That(UiText.FromLiteral(string.Empty).IsEmpty, Is.False, "an empty literal is text, not nothing");
        }

        [Test]
        public void Equality_ComparesContentIncludingArguments()
        {
            Assert.That(UiText.FromKey("A", 1), Is.EqualTo(UiText.FromKey("A", 1)));
            Assert.That(UiText.FromKey("A", 1), Is.Not.EqualTo(UiText.FromKey("A", 2)));
            Assert.That(UiText.FromKey("A"), Is.Not.EqualTo(UiText.FromLiteral("A")));
            Assert.That(UiText.FromKey("A"), Is.Not.EqualTo(UiText.FromKeyIn("T", "A")));
            Assert.That(UiText.FromKey("A").GetHashCode(), Is.EqualTo(UiText.FromKey("A").GetHashCode()));
            Assert.That(UiText.Compose(" ", UiText.FromKey("A"), UiText.FromKey("B")), Is.EqualTo(UiText.Compose(" ", UiText.FromKey("A"), UiText.FromKey("B"))));
            Assert.That(UiText.Compose(" ", UiText.FromKey("A")), Is.Not.EqualTo(UiText.Compose("-", UiText.FromKey("A"))));
        }

        [Test]
        public void WithFallbackAndWithArgs_ReturnNewInstances()
        {
            UiText original = UiText.FromKey("K", 1);
            UiText fallback = original.WithFallback("x");
            UiText args = original.WithArgs(2, 3);
            Assert.That(original.Fallback, Is.Null);
            Assert.That(fallback.Fallback, Is.EqualTo("x"));
            Assert.That(fallback.Args.Count, Is.EqualTo(1));
            Assert.That(args.Args.Count, Is.EqualTo(2));
            Assert.That(original.Args.Count, Is.EqualTo(1));
        }

        [Test]
        public void KeyParse_HandlesTheSpellings()
        {
            string table;
            string key;
            LocalizationKey.Parse("Core:OPTIONS...", out table, out key);
            Assert.That(table, Is.EqualTo("Core"));
            Assert.That(key, Is.EqualTo("OPTIONS..."));
            LocalizationKey.Parse("OPTIONS...", out table, out key);
            Assert.That(table, Is.Null);
            Assert.That(key, Is.EqualTo("OPTIONS..."));
            LocalizationKey.Parse(" Core : K ", out table, out key);
            Assert.That(table, Is.EqualTo("Core"));
            Assert.That(key, Is.EqualTo("K"));
            LocalizationKey.Parse(null, out table, out key);
            Assert.That(key, Is.Null);
            LocalizationKey.Parse("K:", out table, out key);
            Assert.That(key, Is.EqualTo("K"));
        }

        [Test]
        public void LocaleStyle_MapsCodesToClassesAndDirection()
        {
            Assert.That(LocaleStyle.ClassFor("ar"), Is.EqualTo("fdp-lang-ar"));
            Assert.That(LocaleStyle.ClassFor("zh-Hans"), Is.EqualTo("fdp-lang-zh-hans"));
            Assert.That(LocaleStyle.ClassFor("pt_BR"), Is.EqualTo("fdp-lang-pt-br"));
            Assert.That(LocaleStyle.ClassFor(null), Is.EqualTo("fdp-lang-en"));
            Assert.That(LocaleStyle.LanguageOf("zh-Hant"), Is.EqualTo("zh"));
            Assert.That(LocaleStyle.IsRightToLeft("ar"), Is.True);
            Assert.That(LocaleStyle.IsRightToLeft("he"), Is.True);
            Assert.That(LocaleStyle.IsRightToLeft("HE-IL"), Is.True);
            Assert.That(LocaleStyle.IsRightToLeft("en"), Is.False);
            Assert.That(LocaleStyle.IsRightToLeft(""), Is.False);
        }
    }
}
