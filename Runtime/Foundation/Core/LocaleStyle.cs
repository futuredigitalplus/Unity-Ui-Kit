namespace FutureDigitalPlus.UIKit.Foundation
{
    /// <summary>Maps a locale code to the USS class and text direction the root element gets (a font switch per script, right-to-left layout). Pure.</summary>
    public static class LocaleStyle
    {
        /// <summary>Prefix of the language class on the root: <c>fdp-lang-</c>.</summary>
        public const string ClassPrefix = "fdp-lang-";

        /// <summary>Class of a locale code: <c>zh-Hans</c> gives <c>fdp-lang-zh-hans</c>, <c>pt-BR</c> gives <c>fdp-lang-pt-br</c>, empty gives <c>fdp-lang-en</c>.</summary>
        public static string ClassFor(string localeCode)
        {
            if (string.IsNullOrWhiteSpace(localeCode))
                return ClassPrefix + "en";
            return ClassPrefix + localeCode.Trim().Replace('_', '-').ToLowerInvariant();
        }

        /// <summary>Language part only (before the first dash), lower case, "en" when empty.</summary>
        public static string LanguageOf(string localeCode)
        {
            if (string.IsNullOrWhiteSpace(localeCode))
                return "en";
            string normalized = localeCode.Trim().Replace('_', '-');
            int dash = normalized.IndexOf('-');
            return (dash < 0 ? normalized : normalized.Substring(0, dash)).ToLowerInvariant();
        }

        /// <summary>True for the languages written right to left: Arabic, Hebrew, Persian, Urdu, Pashto, Sindhi, Uyghur, Yiddish, Divehi and Kurdish (Sorani).</summary>
        public static bool IsRightToLeft(string localeCode)
        {
            switch (LanguageOf(localeCode))
            {
                case "ar":
                case "he":
                case "iw":
                case "fa":
                case "ur":
                case "ps":
                case "sd":
                case "ug":
                case "yi":
                case "dv":
                case "ku":
                    return true;
                default:
                    return false;
            }
        }
    }
}
