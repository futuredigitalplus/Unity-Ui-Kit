using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace FutureDigitalPlus.UIKit.Lint
{
    /// <summary>Reads the baked theme assets (<c>Themes/*.asset</c>, YAML) without Unity.</summary>
    public static class ThemeAssetRules
    {
        private static readonly Regex CustomName = new Regex(@"m_CustomName:\s*(--[A-Za-z0-9_-]+)", RegexOptions.Compiled);

        /// <summary>The custom property names a baked theme stylesheet declares (<c>m_CustomName: --fdp-color-bg</c>).</summary>
        public static IEnumerable<string> CustomPropertyNames(string assetYaml)
        {
            foreach (Match m in CustomName.Matches(assetYaml ?? string.Empty))
                yield return m.Groups[1].Value;
        }
    }
}
