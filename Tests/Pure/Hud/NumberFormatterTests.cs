using System.Globalization;
using NUnit.Framework;

namespace FutureDigitalPlus.UIKit.Hud.Tests
{
    public class NumberFormatterTests
    {
        private static readonly CultureInfo De = new CultureInfo("de-DE");
        private static readonly CultureInfo Us = new CultureInfo("en-US");

        [Test]
        public void Group_UsesTheInjectedCulture()
        {
            Assert.AreEqual("1,234,567", NumberFormatter.Group(1234567L, Us));
            Assert.AreEqual("1.234.567", NumberFormatter.Group(1234567L, De));
        }

        [Test]
        public void Group_NullCulture_IsInvariant()
        {
            Assert.AreEqual("1,234", NumberFormatter.Group(1234L, null));
            Assert.AreEqual("-12", NumberFormatter.Group(-12L, null));
        }

        [TestCase(0d, "0")]
        [TestCase(999d, "999")]
        [TestCase(1000d, "1K")]
        [TestCase(1234d, "1.2K")]
        [TestCase(1250d, "1.3K")]
        [TestCase(15999d, "16K")]
        [TestCase(999999d, "1M")]
        [TestCase(1000000d, "1M")]
        [TestCase(2500000d, "2.5M")]
        [TestCase(1.2e9d, "1.2B")]
        [TestCase(3.4e12d, "3.4T")]
        [TestCase(5e15d, "5000T")]
        [TestCase(-1234d, "-1.2K")]
        public void Compact_Invariant(double value, string expected)
        {
            Assert.AreEqual(expected, NumberFormatter.Compact(value));
        }

        [Test]
        public void Compact_DecimalSeparatorFollowsCulture()
        {
            Assert.AreEqual("1,2K", NumberFormatter.Compact(1234d, 1, De));
        }

        [Test]
        public void Compact_MaxDecimals()
        {
            Assert.AreEqual("1.23K", NumberFormatter.Compact(1234d, 2));
            Assert.AreEqual("1K", NumberFormatter.Compact(1234d, 0));
            Assert.AreEqual("1.234K", NumberFormatter.Compact(1234d, 3));
            Assert.AreEqual("1.234K", NumberFormatter.Compact(1234d, 9), "decimals are capped at 3");
        }

        [Test]
        public void Compact_BelowCompactFrom_StaysGrouped()
        {
            Assert.AreEqual("1,234", NumberFormatter.Compact(1234d, 1, Us, 10000d));
            Assert.AreEqual("12.3K", NumberFormatter.Compact(12345d, 1, Us, 10000d));
        }

        [Test]
        public void Compact_CustomSuffixes()
        {
            Assert.AreEqual("1,5 Mio", NumberFormatter.Compact(1500000d, 1, De, 1000d, new[] { " Tsd", " Mio", " Mrd", " Bio" }));
        }

        [Test]
        public void Compact_NonFinite_DoesNotThrow()
        {
            Assert.DoesNotThrow(() => NumberFormatter.Compact(double.NaN));
            Assert.DoesNotThrow(() => NumberFormatter.Compact(double.PositiveInfinity));
        }

        [Test]
        public void Format_Styles()
        {
            Assert.AreEqual("1234567", NumberFormatter.Format(1234567.4, NumberFormatStyle.Plain, Us));
            Assert.AreEqual("1,234,567", NumberFormatter.Format(1234567.4, NumberFormatStyle.Grouped, Us));
            Assert.AreEqual("1.2M", NumberFormatter.Format(1234567.4, NumberFormatStyle.Compact, Us));
        }

        [Test]
        public void Signed_AddsPlusOnlyForPositive()
        {
            Assert.AreEqual("+25", NumberFormatter.Signed(25));
            Assert.AreEqual("-10", NumberFormatter.Signed(-10));
            Assert.AreEqual("0", NumberFormatter.Signed(0));
            Assert.AreEqual("+1,500", NumberFormatter.Signed(1500, Us));
        }
    }
}
