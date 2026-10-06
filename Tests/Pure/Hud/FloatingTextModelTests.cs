using NUnit.Framework;

namespace FutureDigitalPlus.UIKit.Hud.Tests
{
    public class FloatingTextModelTests
    {
        [Test]
        public void Spawn_UsesFreeSlots()
        {
            var m = new FloatingTextModel(3);
            m.BeginFrame();
            var a = m.Spawn(0f, 100f, "1");
            var b = m.Spawn(200f, 100f, "2");
            Assert.AreNotEqual(a, b);
            Assert.AreEqual(2, m.ActiveCount);
            Assert.IsTrue(m.Get(a).Active);
            Assert.AreEqual("2", m.Get(b).Text);
        }

        [Test]
        public void Tick_RisesAndFades_ThenFreesTheSlot()
        {
            var m = new FloatingTextModel(2) { Lifetime = 1f, FadeStart = 0.5f, RiseDistance = 100f };
            m.BeginFrame();
            var s = m.Spawn(0f, 300f, "x");
            Assert.AreEqual(1f, m.Get(s).Alpha);

            Assert.IsTrue(m.Tick(0.25f));
            Assert.Less(m.Get(s).Y, 300f, "rises (y decreases)");
            Assert.AreEqual(1f, m.Get(s).Alpha);

            m.Tick(0.5f); // t = 0.75: halfway through the fade
            Assert.AreEqual(0.5f, m.Get(s).Alpha, 1e-4f);

            Assert.IsFalse(m.Tick(0.5f));
            Assert.IsFalse(m.Get(s).Active);
            Assert.AreEqual(0, m.ActiveCount);
            Assert.IsNull(m.Get(s).Text);
        }

        [Test]
        public void Rise_EndsAtTheRiseDistance()
        {
            var m = new FloatingTextModel(1) { Lifetime = 1f, RiseDistance = 80f };
            m.BeginFrame();
            var s = m.Spawn(0f, 200f, "x");
            m.Tick(0.999f);
            Assert.AreEqual(120f, m.Get(s).Y, 0.5f);
        }

        [Test]
        public void Critical_PopsThenSettles_AndLivesLonger()
        {
            var m = new FloatingTextModel(2) { Lifetime = 1f, CriticalLifetimeScale = 2f, PopScale = 2f };
            m.BeginFrame();
            var c = m.Spawn(0f, 0f, "!", FloatingTextKind.Critical);
            var n = m.Spawn(500f, 0f, "n");
            Assert.AreEqual(2f, m.Get(c).Scale);
            Assert.AreEqual(1f, m.Get(n).Scale);
            Assert.AreEqual(2f, m.Get(c).Life);
            m.Tick(0.1f);
            Assert.AreEqual(1.5f, m.Get(c).Scale, 1e-4f);
            m.Tick(0.2f);
            Assert.AreEqual(1f, m.Get(c).Scale, 1e-4f);
        }

        [Test]
        public void Spawn_AtTheSameSpot_IsPushedUp()
        {
            var m = new FloatingTextModel(8) { OverlapStep = 20f, OverlapWidth = 40f };
            m.BeginFrame();
            var a = m.Spawn(100f, 100f, "a");
            var b = m.Spawn(105f, 100f, "b");
            var c = m.Spawn(110f, 100f, "c");
            Assert.AreEqual(100f, m.Get(a).Y);
            Assert.AreEqual(80f, m.Get(b).Y);
            Assert.AreEqual(60f, m.Get(c).Y);
        }

        [Test]
        public void Spawn_Far_IsNotPushed()
        {
            var m = new FloatingTextModel(4) { OverlapStep = 20f, OverlapWidth = 40f };
            m.BeginFrame();
            m.Spawn(100f, 100f, "a");
            var b = m.Spawn(300f, 100f, "b");
            Assert.AreEqual(100f, m.Get(b).Y);
        }

        [Test]
        public void PerFrameCap_DropsTheRest_UntilTheNextFrame()
        {
            var m = new FloatingTextModel(20) { MaxSpawnsPerFrame = 3, OverlapStep = 0f };
            m.BeginFrame();
            for (var i = 0; i < 3; i++) Assert.GreaterOrEqual(m.Spawn(i * 100f, 0f, "x"), 0);
            Assert.AreEqual(-1, m.Spawn(900f, 0f, "x"));
            Assert.AreEqual(3, m.ActiveCount);
            m.BeginFrame();
            Assert.GreaterOrEqual(m.Spawn(900f, 0f, "x"), 0);
        }

        [Test]
        public void FullPool_ReplacesTheOldest()
        {
            var m = new FloatingTextModel(2) { OverlapStep = 0f };
            m.BeginFrame();
            var a = m.Spawn(0f, 0f, "a");
            m.Tick(0.3f);
            var b = m.Spawn(500f, 0f, "b");
            m.Tick(0.1f);
            var c = m.Spawn(900f, 0f, "c");
            Assert.AreEqual(a, c, "the oldest slot is reused");
            Assert.AreEqual("c", m.Get(c).Text);
            Assert.AreEqual("b", m.Get(b).Text);
            Assert.AreEqual(2, m.ActiveCount);
        }

        [Test]
        public void Clear_FreesEverything()
        {
            var m = new FloatingTextModel(3);
            m.BeginFrame();
            m.Spawn(0f, 0f, "a");
            m.Clear();
            Assert.AreEqual(0, m.ActiveCount);
            Assert.IsFalse(m.Tick(1f));
        }

        [Test]
        public void Capacity_IsAtLeastOne()
        {
            Assert.AreEqual(1, new FloatingTextModel(0).Capacity);
        }
    }

    public class WorldAnchorMathTests
    {
        // Rectangle 0,0 .. 1000,600; centre 500,300.
        private static AnchorResult Resolve(float x, float y, float depth = 5f, float margin = 20f, OffScreenMode mode = OffScreenMode.Clamp)
            => WorldAnchorMath.Resolve(x, y, depth, 0f, 0f, 1000f, 600f, margin, mode);

        [Test]
        public void Inside_IsOnScreen_AtTheSamePosition()
        {
            var r = Resolve(300f, 200f);
            Assert.AreEqual(AnchorVisibility.OnScreen, r.Visibility);
            Assert.IsTrue(r.Show);
            Assert.AreEqual(300f, r.X);
            Assert.AreEqual(200f, r.Y);
        }

        [Test]
        public void Right_OfTheScreen_ClampsToTheRightEdge()
        {
            var r = Resolve(1500f, 300f);
            Assert.AreEqual(AnchorVisibility.OffScreen, r.Visibility);
            Assert.AreEqual(980f, r.X, 1e-3f);
            Assert.AreEqual(300f, r.Y, 1e-3f);
            Assert.AreEqual(0f, r.ArrowAngleDegrees, 1e-3f);
        }

        [Test]
        public void Below_Clamps_ToTheBottomEdge_AngleDown()
        {
            var r = Resolve(500f, 900f);
            Assert.AreEqual(500f, r.X, 1e-3f);
            Assert.AreEqual(580f, r.Y, 1e-3f);
            Assert.AreEqual(90f, r.ArrowAngleDegrees, 1e-3f);
        }

        [Test]
        public void Diagonal_ClampsToTheCorner()
        {
            var r = Resolve(5000f, -5000f);
            Assert.IsTrue(r.Show);
            Assert.That(r.X, Is.InRange(600f, 980f));
            Assert.AreEqual(20f, r.Y, 1e-3f);
            Assert.Less(r.ArrowAngleDegrees, 0f, "points up and to the right");
        }

        [Test]
        public void HideMode_DoesNotShowOffScreenAnchors()
        {
            var r = Resolve(1500f, 300f, mode: OffScreenMode.Hide);
            Assert.IsFalse(r.Show);
            Assert.AreEqual(AnchorVisibility.OffScreen, r.Visibility);
        }

        [Test]
        public void Behind_FlipsTheDirection()
        {
            // projected to the right of centre, but behind the camera: the way to turn is left
            var r = Resolve(800f, 300f, depth: -2f);
            Assert.AreEqual(AnchorVisibility.Behind, r.Visibility);
            Assert.AreEqual(20f, r.X, 1e-3f);
            Assert.AreEqual(180f, System.Math.Abs(r.ArrowAngleDegrees), 1e-3f);
        }

        [Test]
        public void Behind_AtTheExactCentre_PicksADirection()
        {
            var r = Resolve(500f, 300f, depth: -1f);
            Assert.AreEqual(AnchorVisibility.Behind, r.Visibility);
            Assert.AreEqual(580f, r.Y, 1e-3f);
        }

        [Test]
        public void Margin_CountsAsOffScreen()
        {
            var r = Resolve(990f, 300f, margin: 20f);
            Assert.AreEqual(AnchorVisibility.OffScreen, r.Visibility);
            Assert.AreEqual(980f, r.X, 1e-3f);
        }

        [Test]
        public void ZeroSizedRectangle_DoesNotProduceNaN()
        {
            var r = WorldAnchorMath.Resolve(10f, 10f, 1f, 5f, 5f, 5f, 5f, 0f, OffScreenMode.Clamp);
            Assert.IsFalse(float.IsNaN(r.X));
            Assert.IsFalse(float.IsNaN(r.Y));
        }
    }
}
