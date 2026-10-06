using NUnit.Framework;

namespace FutureDigitalPlus.UIKit.Foundation.Tests
{
    [TestFixture]
    public sealed class LoadingTokensTests
    {
        double _now;
        LoadingTokens _tokens;
        int _changes;

        [SetUp]
        public void SetUp()
        {
            _now = 100.0;
            _changes = 0;
            _tokens = new LoadingTokens(() => _now);
            _tokens.Changed += () => _changes++;
        }

        [Test]
        public void FastWork_NeverShowsTheOverlay()
        {
            int id = _tokens.Begin(UiText.FromKey("LOADING"), true);
            _now += 0.2;
            _tokens.Tick();
            Assert.That(_tokens.IsVisible, Is.False);
            _tokens.End(id);
            Assert.That(_tokens.IsVisible, Is.False);
            Assert.That(_tokens.ActiveCount, Is.EqualTo(0));
        }

        [Test]
        public void SlowWork_ShowsAfterTheDelay()
        {
            _tokens.Begin(null, true);
            _now += 0.249;
            _tokens.Tick();
            Assert.That(_tokens.IsVisible, Is.False);
            _now += 0.002;
            _tokens.Tick();
            Assert.That(_tokens.IsVisible, Is.True);
        }

        [Test]
        public void OnceVisible_StaysForTheMinimumTimeAfterTheWorkEnded()
        {
            int id = _tokens.Begin(null, true);
            _now += 0.3;
            _tokens.Tick();
            Assert.That(_tokens.IsVisible, Is.True);
            _now += 0.1;
            _tokens.End(id);
            Assert.That(_tokens.IsVisible, Is.True, "visible for only 0.1 s so far");
            _now += 0.299;
            _tokens.Tick();
            Assert.That(_tokens.IsVisible, Is.True);
            _now += 0.002;
            _tokens.Tick();
            Assert.That(_tokens.IsVisible, Is.False);
        }

        [Test]
        public void InputIsBlockedFromTheMomentABlockingTokenBegins()
        {
            int id = _tokens.Begin(null, true);
            Assert.That(_tokens.BlocksInput, Is.True);
            Assert.That(_tokens.IsVisible, Is.False);
            _tokens.End(id);
            Assert.That(_tokens.BlocksInput, Is.False);
        }

        [Test]
        public void ANonBlockingToken_DoesNotBlockInput()
        {
            _tokens.Begin(UiText.FromLiteral("hint"), false);
            Assert.That(_tokens.BlocksInput, Is.False);
        }

        [Test]
        public void TokensAreReferenceCounted()
        {
            int a = _tokens.Begin(UiText.FromLiteral("a"), true);
            int b = _tokens.Begin(UiText.FromLiteral("b"), true);
            _now += 1;
            _tokens.Tick();
            _tokens.End(a);
            Assert.That(_tokens.IsVisible, Is.True);
            Assert.That(_tokens.BlocksInput, Is.True);
            Assert.That(_tokens.Message.Literal, Is.EqualTo("b"));
            _tokens.End(b);
            Assert.That(_tokens.BlocksInput, Is.False);
        }

        [Test]
        public void End_Twice_IsHarmless()
        {
            int id = _tokens.Begin(null, true);
            Assert.That(_tokens.End(id), Is.True);
            Assert.That(_tokens.End(id), Is.False);
            Assert.That(_tokens.End(12345), Is.False);
        }

        [Test]
        public void Update_ChangesMessageAndSpinner()
        {
            int id = _tokens.Begin(UiText.FromLiteral("Signing in"), true);
            Assert.That(_tokens.ShowSpinner, Is.True);
            _tokens.Update(id, UiText.FromLiteral("Waiting for Game Center"), false);
            Assert.That(_tokens.Message.Literal, Is.EqualTo("Waiting for Game Center"));
            Assert.That(_tokens.ShowSpinner, Is.False);
        }

        [Test]
        public void SetProgress_IsClampedAndReported()
        {
            int id = _tokens.Begin(null, true);
            Assert.That(_tokens.Progress, Is.EqualTo(-1f));
            _tokens.SetProgress(id, 0.4f);
            Assert.That(_tokens.Progress, Is.EqualTo(0.4f).Within(0.0001f));
            _tokens.SetProgress(id, 7f);
            Assert.That(_tokens.Progress, Is.EqualTo(1f));
            _tokens.SetProgress(id, -3f);
            Assert.That(_tokens.Progress, Is.EqualTo(0f));
        }

        [Test]
        public void NextDueSeconds_SaysWhenVisibilityChangesByItself()
        {
            Assert.That(_tokens.NextDueSeconds, Is.EqualTo(-1.0));
            int id = _tokens.Begin(null, true);
            Assert.That(_tokens.NextDueSeconds, Is.EqualTo(0.25).Within(0.0001));
            _now += 0.25;
            _tokens.Tick();
            Assert.That(_tokens.NextDueSeconds, Is.EqualTo(-1.0), "shown and still loading: nothing pending");
            _tokens.End(id);
            Assert.That(_tokens.NextDueSeconds, Is.EqualTo(0.4).Within(0.0001));
            _now += 0.4005;
            Assert.That(_tokens.NextDueSeconds, Is.EqualTo(0.0).Within(0.0001));
            _tokens.Tick();
            Assert.That(_tokens.NextDueSeconds, Is.EqualTo(-1.0));
        }

        [Test]
        public void TheTimingValuesAreConfigurable()
        {
            _tokens.ShowDelaySeconds = 1f;
            _tokens.MinVisibleSeconds = 2f;
            _tokens.Begin(null, true);
            _now += 0.9;
            _tokens.Tick();
            Assert.That(_tokens.IsVisible, Is.False);
            _now += 0.2;
            _tokens.Tick();
            Assert.That(_tokens.IsVisible, Is.True);
        }

        [Test]
        public void Changed_IsRaisedForVisibleAndBlockingTransitions()
        {
            int id = _tokens.Begin(null, true);
            int afterBegin = _changes;
            Assert.That(afterBegin, Is.GreaterThan(0));
            _now += 0.3;
            _tokens.Tick();
            Assert.That(_changes, Is.GreaterThan(afterBegin));
            _tokens.Tick();
            int stable = _changes;
            _tokens.Tick();
            Assert.That(_changes, Is.EqualTo(stable), "no change, no event");
            _tokens.End(id);
            Assert.That(_changes, Is.GreaterThan(stable));
        }
    }
}
