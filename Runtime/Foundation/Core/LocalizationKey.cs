namespace FutureDigitalPlus.UIKit.Foundation
{
    /// <summary>Parsing of the <c>Table:KEY</c> spelling used in UXML attributes and C# call sites. Pure.</summary>
    public static class LocalizationKey
    {
        /// <summary>The table tried for a key that names none, unless the resolver is given other tables.</summary>
        public const string DefaultTable = "UI";

        /// <summary>
        /// Splits <c>Table:KEY</c> into table and key. A spec without a colon is a plain key (table null). Keys never contain a colon,
        /// so the first colon is always the separator.
        /// </summary>
        public static void Parse(string spec, out string table, out string key)
        {
            table = null;
            key = null;
            if (string.IsNullOrEmpty(spec))
                return;
            string trimmed = spec.Trim();
            if (trimmed.Length == 0)
                return;
            int colon = trimmed.IndexOf(':');
            if (colon < 0)
            {
                key = trimmed;
                return;
            }
            string left = trimmed.Substring(0, colon).Trim();
            string right = trimmed.Substring(colon + 1).Trim();
            if (right.Length == 0)
            {
                // "KEY:" is a plain key with a stray colon.
                key = left.Length == 0 ? null : left;
                return;
            }
            table = left.Length == 0 ? null : left;
            key = right;
        }
    }
}
