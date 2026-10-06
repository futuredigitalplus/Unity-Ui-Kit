using System.Globalization;
using FutureDigitalPlus.UIKit.Animation.Extensions;
using NUnit.Framework;
using UnityEngine.UIElements;

namespace FutureDigitalPlus.UIKit.PlatformSmoke.Tests
{
    /// <summary>USS and XML numbers use '.' as the decimal separator whatever the device culture ("0.5" read with de-DE is 5, not 0.5).</summary>
    public class ParseCultureTests
    {
        private CultureInfo _previous;

        [SetUp]
        public void UseGerman()
        {
            _previous = CultureInfo.CurrentCulture;
            try { CultureInfo.CurrentCulture = new CultureInfo("de-DE"); }
            catch (CultureNotFoundException) { Assert.Ignore("de-DE is not available in this runtime (invariant globalization)."); }
        }

        [TearDown]
        public void RestoreCulture() => CultureInfo.CurrentCulture = _previous;

        [Test]
        public void TheTestCultureReallyUsesACommaAsDecimalSeparator() =>
            Assert.AreEqual(",", CultureInfo.CurrentCulture.NumberFormat.NumberDecimalSeparator);

        [Test]
        public void ToStyleFloat_ReadsADot()
        {
            Assert.AreEqual(0.5f, UIParser.ToStyleFloat("0.5px").value, 1e-6f);
            Assert.AreEqual(12.25f, UIParser.ToStyleFloat("12.25").value, 1e-6f);
        }

        [Test]
        public void ToStyleLength_ReadsADot()
        {
            var length = UIParser.ToStyleLength("33.5%").value;
            Assert.AreEqual(33.5f, length.value, 1e-6f);
            Assert.AreEqual(LengthUnit.Percent, length.unit);
        }

        [Test]
        public void ToStyleColor_ReadsADecimalAlpha()
        {
            var c = UIParser.ToStyleColor("rgba(255, 0, 0, 0.5)").value;
            Assert.AreEqual(1f, c.r, 1e-6f);
            Assert.AreEqual(0.5f, c.a, 1e-6f);
        }

        [Test]
        public void TryParseFloat_ReadsADot() => Assert.AreEqual(0.25f, "0.25".TryParseFloat(), 1e-6f);

        [Test]
        public void ToStyleInt_ReadsPixels() => Assert.AreEqual(12, UIParser.ToStyleInt("12px").value);
    }
}
