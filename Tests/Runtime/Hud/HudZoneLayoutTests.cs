using System.Collections;
using NUnit.Framework;
using UnityEngine.TestTools;

namespace FutureDigitalPlus.UIKit.Hud.Tests
{
    public class HudZoneLayoutTests
    {
        private HudPanel _panel;

        [SetUp]
        public void SetUp() => LogAssert.ignoreFailingMessages = true;

        [TearDown]
        public void TearDown()
        {
            LogAssert.ignoreFailingMessages = false;
            _panel?.Dispose();
        }

        [UnityTest]
        public IEnumerator MeterBar_AddedToAZone_WithNoWidthOfItsOwn_IsStillWideEnoughToRead()
        {
            _panel = HudPanel.Create();
            var zones = new HudZones();
            _panel.Root.Add(zones);
            var bar = zones.Add(HudZone.TopLeft, new MeterBar { Size = MeterBarSize.Large, LabelMode = MeterLabelMode.ValueAndMax });
            bar.Set(184f, 240f);
            var bare = zones.Add(HudZone.TopLeft, new MeterBar());
            yield return HudPanel.WaitSeconds(0.3f);
            Assert.GreaterOrEqual(bar.resolvedStyle.width, 150f, "a zone hugs its content; the bar must not shrink to its label");
            Assert.GreaterOrEqual(bare.resolvedStyle.width, 150f, "a bar with no label must not collapse to a dot");
        }
    }
}
