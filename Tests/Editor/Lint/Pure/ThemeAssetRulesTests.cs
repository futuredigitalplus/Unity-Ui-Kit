using System.Linq;
using NUnit.Framework;

namespace FutureDigitalPlus.UIKit.Lint.Tests
{
    public class ThemeAssetRulesTests
    {
        [Test]
        public void CustomPropertyNames_ReadsBakedStyleSheetNames()
        {
            const string yaml = "    m_CustomName: --fdp-color-bg\n    m_CustomName: --fdp-space-2\n    m_Name: not a property\n";
            CollectionAssert.AreEqual(new[] { "--fdp-color-bg", "--fdp-space-2" }, ThemeAssetRules.CustomPropertyNames(yaml).ToArray());
            Assert.IsEmpty(ThemeAssetRules.CustomPropertyNames(null).ToList());
        }
    }
}
