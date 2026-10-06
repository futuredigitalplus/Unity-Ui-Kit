using System;
using FutureDigitalPlus.UIKit.Foundation;

namespace FutureDigitalPlus.UIKit.Language
{
    /// <summary>
    /// Where the text of a language comes from: Unity Localization (see the <c>Language.UnityLocalization</c> adapter), a
    /// <see cref="DictionaryTextSource"/>, a server, a game's own tables. <see cref="UiLocalization"/> asks it for one entry at a
    /// time and listens to <see cref="Changed"/>.
    /// </summary>
    public interface ILocalizationBackend
    {
        /// <summary>The code of the active language, for example <c>en</c>, <c>zh-Hans</c>, <c>pt-BR</c>. Never null.</summary>
        string CurrentLocale { get; }

        /// <summary>
        /// Raised on the main thread after the active language changed or the backend finished loading its tables, so a lookup that
        /// failed before may succeed now.
        /// </summary>
        event Action Changed;

        /// <summary>
        /// Looks one entry up. Must not throw. Returns <see cref="LookupStatus.TableMissing"/> when the table does not exist or is not
        /// loaded yet (the caller then stops asking that table until <see cref="Changed"/>), <see cref="LookupStatus.EntryMissing"/>
        /// when only the key is unknown.
        /// </summary>
        /// <param name="table">The table name; never null here.</param>
        /// <param name="key">The entry key.</param>
        /// <param name="args">Positional arguments for <c>{0}</c> style placeholders; may be null or empty.</param>
        /// <param name="text">The text when the result is <see cref="LookupStatus.Found"/>, otherwise null.</param>
        LookupStatus TryGet(string table, string key, object[] args, out string text);
    }

    /// <summary>
    /// The backend a <see cref="UiLocalization"/> has until something better is set: every lookup reports a missing table, so labels show
    /// the fallback of their <see cref="UiText"/> or a visible <c>[KEY]</c> placeholder, and nothing ever throws.
    /// </summary>
    public sealed class NullLocalizationBackend : ILocalizationBackend
    {
        /// <summary>The shared instance.</summary>
        public static readonly NullLocalizationBackend Instance = new NullLocalizationBackend();

        private NullLocalizationBackend()
        {
        }

        /// <summary>Always <c>en</c>.</summary>
        public string CurrentLocale => "en";

        /// <summary>Never raised.</summary>
        public event Action Changed
        {
            add { }
            remove { }
        }

        /// <summary>Always <see cref="LookupStatus.TableMissing"/>.</summary>
        public LookupStatus TryGet(string table, string key, object[] args, out string text)
        {
            text = null;
            return LookupStatus.TableMissing;
        }
    }
}
