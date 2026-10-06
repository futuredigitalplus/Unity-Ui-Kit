using System.Collections.Generic;
using NUnit.Framework;

namespace FutureDigitalPlus.UIKit.Foundation.Tests
{
    /// <summary>Null and degenerate inputs of the text and loading types.</summary>
    [TestFixture]
    public sealed class UiTextEdgeTests
    {
        [Test]
        public void Compose_WithANullPart_TreatsItAsEmpty()
        {
            UiText composed = UiText.Compose(" ", UiText.FromLiteral("a"), null, UiText.FromLiteral("b"));
            UiText same = UiText.Compose(" ", UiText.FromLiteral("a"), null, UiText.FromLiteral("b"));
            Assert.DoesNotThrow(() => composed.Equals(same));
            Assert.That(composed.Equals(same), Is.True);
            Assert.That(new LocalizationKeyResolver(null).Resolve(composed), Is.EqualTo("a  b"));
        }

        [Test]
        public void Compose_DoesNotShareItsPartsArrayWithTheCaller()
        {
            UiText[] parts = { UiText.FromLiteral("a"), UiText.FromLiteral("b") };
            UiText composed = UiText.Compose("+", parts);
            parts[0] = UiText.FromLiteral("changed");
            Assert.That(new LocalizationKeyResolver(null).Resolve(composed), Is.EqualTo("a+b"));
        }

        [Test]
        public void EqualTexts_HaveEqualHashCodes()
        {
            UiText a = UiText.FromKey("Borderline:KEY", 1, "x").WithFallback("fb");
            UiText b = UiText.FromKey("Borderline:KEY", 1, "x").WithFallback("fb");
            Assert.That(a.Equals(b), Is.True);
            Assert.That(a.GetHashCode(), Is.EqualTo(b.GetHashCode()));
        }

        [Test]
        public void SafeAreaInsets_ThatAreEqualWithinTheTolerance_HaveEqualHashCodes()
        {
            SafeAreaInsets a = new SafeAreaInsets(10f, 59.99f, 0f, 34f);
            SafeAreaInsets b = new SafeAreaInsets(10.004f, 60f, 0.004f, 34.004f);
            Assert.That(a.Equals(b), Is.True);
            Assert.That(a.GetHashCode(), Is.EqualTo(b.GetHashCode()));
        }

        [Test]
        public void SetProgress_WithNaN_ReportsNoDeterminateProgressInsteadOfNaN()
        {
            double now = 0;
            LoadingTokens tokens = new LoadingTokens(() => now);
            int id = tokens.Begin(null, true);
            tokens.SetProgress(id, float.NaN);
            Assert.That(float.IsNaN(tokens.Progress), Is.False);
            Assert.That(tokens.Progress, Is.EqualTo(0f));
        }

        [Test]
        public void TheResolver_DoesNotAskAMissingTableAgainUntilReset()
        {
            int calls = 0;
            LocalizationKeyResolver resolver = new LocalizationKeyResolver((string table, string key, object[] args, out string text) =>
            {
                calls++;
                text = null;
                return LookupStatus.TableMissing;
            }, new[] { "Core", "Borderline", "UI" });
            Assert.That(resolver.Resolve(UiText.FromKey("A").WithFallback("fa")), Is.EqualTo("fa"));
            int afterFirst = calls;
            Assert.That(afterFirst, Is.EqualTo(3), "Core, Borderline and UI, once each");
            resolver.Resolve(UiText.FromKey("B"));
            Assert.That(calls, Is.EqualTo(afterFirst));
            resolver.ResetMissing();
            resolver.Resolve(UiText.FromKey("C"));
            Assert.That(calls, Is.EqualTo(afterFirst + 3));
        }

        [Test]
        public void AnEntryMissingInCore_IsFoundInBorderline()
        {
            List<string> asked = new List<string>();
            LocalizationKeyResolver resolver = new LocalizationKeyResolver((string table, string key, object[] args, out string text) =>
            {
                asked.Add(table);
                text = table == "Borderline" ? "from borderline" : null;
                return table == "Borderline" ? LookupStatus.Found : LookupStatus.EntryMissing;
            }, new[] { "Core", "Borderline", "UI" });
            Assert.That(resolver.Resolve(UiText.FromKey("GCDIALOGTITLE")), Is.EqualTo("from borderline"));
            Assert.That(asked, Is.EqualTo(new[] { "Core", "Borderline" }));
        }

        [Test]
        public void AFallbackWithAnUnbalancedBrace_IsShownAsIs()
        {
            LocalizationKeyResolver resolver = new LocalizationKeyResolver(null);
            Assert.That(resolver.Resolve(UiText.FromKey("K", 5).WithFallback("Moves {0 left")), Is.EqualTo("Moves {0 left"));
        }
    }
}
