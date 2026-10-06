using System;
using NUnit.Framework;

namespace FutureDigitalPlus.UIKit.Foundation.Tests
{
    [TestFixture]
    public sealed class FoundationSmallTypesTests
    {
        [TearDown]
        public void TearDown()
        {
            UiAccessibility.Reset();
            UiLog.Sink = null;
        }

        // ---- UiAccessibility and UiMotion ----

        [Test]
        public void UiAccessibility_StartsAtTheDefaults()
        {
            UiAccessibility.Reset();
            Assert.That(UiAccessibility.Current, Is.EqualTo(UiAccessibilitySettings.Default));
            Assert.That(UiAccessibility.ReduceMotion, Is.False);
            Assert.That(UiAccessibility.Current.UiScale, Is.EqualTo(1f));
            Assert.That(UiAccessibility.Current.TextScale, Is.EqualTo(1f));
        }

        [Test]
        public void UiAccessibility_SetRaisesChangedOnlyWhenTheValueDiffers()
        {
            int raised = 0;
            UiAccessibilitySettings last = default;
            UiAccessibility.Changed += settings => { raised++; last = settings; };
            UiAccessibility.Set(UiAccessibilitySettings.Default);
            Assert.That(raised, Is.EqualTo(0));
            UiAccessibility.Set(UiAccessibilitySettings.Default.WithReduceMotion(true));
            Assert.That(raised, Is.EqualTo(1));
            Assert.That(last.ReduceMotion, Is.True);
            UiAccessibility.Set(UiAccessibilitySettings.Default.WithReduceMotion(true));
            Assert.That(raised, Is.EqualTo(1), "an equal value is not a change");
        }

        [Test]
        public void UiAccessibility_ResetDropsTheListenersAndTheValue()
        {
            int raised = 0;
            UiAccessibility.Changed += settings => raised++;
            UiAccessibility.Set(UiAccessibilitySettings.Default.WithBoldText(true));
            UiAccessibility.Reset();
            Assert.That(UiAccessibility.Current.BoldText, Is.False);
            UiAccessibility.Set(UiAccessibilitySettings.Default.WithBoldText(true));
            Assert.That(raised, Is.EqualTo(1), "the listener was removed by Reset");
        }

        [Test]
        public void UiMotion_ScalesToZeroWhenTheyAskedForReducedMotion()
        {
            Assert.That(UiMotion.Scale(250), Is.EqualTo(250));
            Assert.That(UiMotion.Scale(0.25f), Is.EqualTo(0.25f));
            UiAccessibility.Set(UiAccessibilitySettings.Default.WithReduceMotion(true));
            Assert.That(UiMotion.Scale(250), Is.EqualTo(0));
            Assert.That(UiMotion.Scale(0.25f), Is.EqualTo(0f));
        }

        [Test]
        public void UiAccessibilitySettings_ClampsScalesAndKeepsTheOtherValuesInWith()
        {
            UiAccessibilitySettings s = new UiAccessibilitySettings(true, 10f, 0.1f, true, false);
            Assert.That(s.UiScale, Is.EqualTo(4f));
            Assert.That(s.TextScale, Is.EqualTo(0.5f));
            Assert.That(new UiAccessibilitySettings(false, float.NaN, float.NaN, false, false).UiScale, Is.EqualTo(1f));
            UiAccessibilitySettings t = s.WithTextScale(1.5f).WithUiScale(2f).WithHighContrast(false).WithBoldText(true);
            Assert.That(t.ReduceMotion, Is.True);
            Assert.That(t.TextScale, Is.EqualTo(1.5f));
            Assert.That(t.UiScale, Is.EqualTo(2f));
            Assert.That(t.HighContrast, Is.False);
            Assert.That(t.BoldText, Is.True);
            Assert.That(t.Equals((object)t), Is.True);
            Assert.That(t.GetHashCode(), Is.EqualTo(s.WithTextScale(1.5f).WithUiScale(2f).WithHighContrast(false).WithBoldText(true).GetHashCode()));
        }

        // ---- LocaleStyle ----

        [TestCase("ar", true)]
        [TestCase("ar-EG", true)]
        [TestCase("he", true)]
        [TestCase("iw", true)]
        [TestCase("fa_IR", true)]
        [TestCase("ur", true)]
        [TestCase("en", false)]
        [TestCase("de-DE", false)]
        [TestCase("zh-Hans", false)]
        [TestCase("", false)]
        [TestCase(null, false)]
        public void LocaleStyle_KnowsTheRightToLeftLanguages(string code, bool expected)
        {
            Assert.That(LocaleStyle.IsRightToLeft(code), Is.EqualTo(expected));
        }

        [Test]
        public void LocaleStyle_LanguageOfKeepsOnlyTheLanguagePart()
        {
            Assert.That(LocaleStyle.LanguageOf("pt-BR"), Is.EqualTo("pt"));
            Assert.That(LocaleStyle.LanguageOf("ZH_hans"), Is.EqualTo("zh"));
            Assert.That(LocaleStyle.LanguageOf(" "), Is.EqualTo("en"));
        }

        // ---- SafeAreaInsets ----

        [Test]
        public void WithMinimum_RaisesEveryEdgeToThePercentOfItsDimension()
        {
            SafeAreaInsets insets = new SafeAreaInsets(0f, 10f, 200f, 0f).WithMinimum(5f, 1000f, 500f);
            Assert.That(insets.Left, Is.EqualTo(50f).Within(0.001f));
            Assert.That(insets.Top, Is.EqualTo(25f).Within(0.001f));
            Assert.That(insets.Right, Is.EqualTo(200f), "the larger device inset wins");
            Assert.That(insets.Bottom, Is.EqualTo(25f).Within(0.001f));
        }

        [Test]
        public void WithMinimum_ZeroOrNegativePercentChangesNothing()
        {
            SafeAreaInsets insets = new SafeAreaInsets(1f, 2f, 3f, 4f);
            Assert.That(insets.WithMinimum(0f, 1000f, 500f), Is.EqualTo(insets));
            Assert.That(insets.WithMinimum(-5f, 1000f, 500f), Is.EqualTo(insets));
        }

        [Test]
        public void Equality_ToleratesSubPixelJitter()
        {
            Assert.That(new SafeAreaInsets(0f, 47.001f, 0f, 34f), Is.EqualTo(new SafeAreaInsets(0f, 47f, 0f, 34.002f)));
            Assert.That(new SafeAreaInsets(0f, 47.1f, 0f, 34f), Is.Not.EqualTo(new SafeAreaInsets(0f, 47f, 0f, 34f)));
        }

        [TestCase(null, SafeAreaEdges.All)]
        [TestCase("", SafeAreaEdges.All)]
        [TestCase("all", SafeAreaEdges.All)]
        [TestCase("none", SafeAreaEdges.None)]
        [TestCase("top,bottom", SafeAreaEdges.Top | SafeAreaEdges.Bottom)]
        [TestCase("Left Right", SafeAreaEdges.Left | SafeAreaEdges.Right)]
        [TestCase("horizontal", SafeAreaEdges.Left | SafeAreaEdges.Right)]
        [TestCase("vertical;top", SafeAreaEdges.Top | SafeAreaEdges.Bottom)]
        [TestCase("sideways", SafeAreaEdges.None)]
        public void SafeAreaEdgesParser_ReadsTheUxmlAttribute(string value, SafeAreaEdges expected)
        {
            Assert.That(SafeAreaEdgesParser.Parse(value), Is.EqualTo(expected));
        }

        // ---- DialogTemplate ----

        [Test]
        public void DialogTemplate_CreatesIndependentRequestsWithItsValues()
        {
            DialogTemplate template = new DialogTemplate
            {
                Id = "reset",
                Title = UiText.FromLiteral("Reset?"),
                Message = UiText.FromLiteral("All progress is lost."),
                Priority = DialogPriority.High,
                DismissOnBack = false,
                DismissOnScrim = true,
                DedupeKey = "reset"
            }.AddButton(new DialogButton(UiText.FromLiteral("No"), DialogButtonRole.Cancel)).AddButton(new DialogButton(UiText.FromLiteral("Yes"), DialogButtonRole.Destructive));

            object content = new object();
            DialogOutcome? seen = null;
            DialogRequest a = template.CreateRequest(outcome => seen = outcome, content);
            DialogRequest b = template.CreateRequest();

            Assert.That(a, Is.Not.SameAs(b));
            Assert.That(a.Id, Is.EqualTo("reset"));
            Assert.That(a.Priority, Is.EqualTo(DialogPriority.High));
            Assert.That(a.DismissOnBack, Is.False);
            Assert.That(a.DismissOnScrim, Is.True);
            Assert.That(a.DedupeKey, Is.EqualTo("reset"));
            Assert.That(a.Content, Is.SameAs(content));
            Assert.That(a.Buttons.Count, Is.EqualTo(2));
            Assert.That(a.CancelIndex, Is.EqualTo(0));
            a.AddButton(new DialogButton(UiText.FromLiteral("Extra")));
            Assert.That(b.Buttons.Count, Is.EqualTo(2), "adding a button to one request does not touch the template or other requests");
            DialogQueue queue = new DialogQueue();
            queue.Enqueue(a);
            queue.RequestBack();
            Assert.That(seen.HasValue, Is.False, "DismissOnBack is off, so back does not close it");
            queue.RequestScrimTap();
            Assert.That(seen.Value.Reason, Is.EqualTo(DialogCloseReason.Scrim));
        }

        [Test]
        public void DialogTemplate_RefusesANullButton()
        {
            Assert.Throws<ArgumentNullException>(() => new DialogTemplate().AddButton(null));
        }

        // ---- UiLog ----

        [Test]
        public void UiLog_WithoutASinkDropsMessages_AndWithOneDeliversLevelTextAndException()
        {
            UiLog.Warning("nobody listens");
            UiLog.Error(new InvalidOperationException("dropped"));
            UiLogLevel level = UiLogLevel.Info;
            string text = null;
            Exception exception = null;
            UiLog.Sink = (l, t, e) => { level = l; text = t; exception = e; };
            UiLog.Warning("careful");
            Assert.That(level, Is.EqualTo(UiLogLevel.Warning));
            Assert.That(text, Is.EqualTo("careful"));
            Assert.That(exception, Is.Null);
            InvalidOperationException boom = new InvalidOperationException("boom");
            UiLog.Error(boom);
            Assert.That(level, Is.EqualTo(UiLogLevel.Error));
            Assert.That(text, Is.EqualTo("boom"));
            Assert.That(exception, Is.SameAs(boom));
            UiLog.Warning(boom);
            Assert.That(level, Is.EqualTo(UiLogLevel.Warning));
            UiLog.Write(UiLogLevel.Info, "fyi");
            Assert.That(level, Is.EqualTo(UiLogLevel.Info));
        }

        // ---- LoadingToken ----

        sealed class RecordingOverlay : ILoadingOverlay
        {
            public int Ended;
            public bool BlocksInput { get { return false; } }
            public LoadingToken Begin(UiText message = null, bool blockInput = true) { return new LoadingToken(this, 1); }
            public void End(LoadingToken token) { Ended++; }
            public void Update(LoadingToken token, UiText message, bool showSpinner) { }
            public void SetProgress(LoadingToken token, float normalized) { }
            public bool HandleBack() { return false; }
        }

        [Test]
        public void LoadingToken_DisposeEndsThePeriod_AndADefaultTokenIsInvalidAndHarmless()
        {
            RecordingOverlay overlay = new RecordingOverlay();
            using (LoadingToken token = overlay.Begin())
                Assert.That(token.IsValid, Is.True);
            Assert.That(overlay.Ended, Is.EqualTo(1));
            LoadingToken none = default;
            Assert.That(none.IsValid, Is.False);
            Assert.DoesNotThrow(() => none.Dispose());
            Assert.That(overlay.Ended, Is.EqualTo(1));
        }
    }
}
