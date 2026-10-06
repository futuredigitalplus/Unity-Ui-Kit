using System;
using NUnit.Framework;

namespace FutureDigitalPlus.UIKit.Gameplay.Tests
{
    /// <summary>The allocation probe of these tests sees a step that allocates and is quiet for one that does not (otherwise "allocates nothing" proves nothing).</summary>
    public class AllocProbeTests
    {
        [Test]
        public void SeesAnAllocatingStep_AndNotAQuietOne()
        {
            if (!AllocProbe.IsAvailable()) Assert.Ignore("The runtime does not report per-thread allocations.");

            long quiet = AllocProbe.Measure(() => { });
            long loud = AllocProbe.Measure(() => GC.KeepAlive(new byte[256]));

            Assert.AreEqual(0L, quiet, "an empty step");
            Assert.Greater(loud, 0L, "a step that allocates 256 bytes");
        }
    }
}
