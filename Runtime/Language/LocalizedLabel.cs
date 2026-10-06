using FutureDigitalPlus.UIKit.Foundation;
using UnityEngine.UIElements;

namespace FutureDigitalPlus.UIKit.Language
{
    /// <summary>
    /// A <c>Label</c> whose text comes from a localization key, for UXML:
    /// <c>&lt;fdp:LocalizedLabel key="HOMEBTN-PLAY" fallback="Play" /&gt;</c> with <c>xmlns:fdp="FutureDigitalPlus.UIKit.Language"</c>.
    /// Attributes: <c>key</c> (also <c>Table:KEY</c>), <c>table</c> (optional; default: the localization's default tables) and
    /// <c>fallback</c> (shown when the key cannot be resolved). Arguments for <c>{0}</c> placeholders are set from C# with
    /// <see cref="SetArgs"/>. A missing table or key shows the fallback, or <c>[KEY]</c>, and logs nothing per label.
    /// </summary>
    [UxmlElement]
    public partial class LocalizedLabel : Label
    {
        private readonly LocalizedTextBinder _binder;
        private string _key;
        private string _table;
        private string _fallback;
        private object[] _args;

        /// <summary>Creates a label bound to <see cref="UiLocalization.Default"/>.</summary>
        public LocalizedLabel()
        {
            _binder = new LocalizedTextBinder(this, value => text = value);
        }

        /// <summary>The localization key, or <c>Table:KEY</c>.</summary>
        [UxmlAttribute("key")]
        public string Key
        {
            get => _key;
            set
            {
                _key = value;
                Rebind();
            }
        }

        /// <summary>The table name; empty tries the default tables.</summary>
        [UxmlAttribute("table")]
        public string Table
        {
            get => _table;
            set
            {
                _table = value;
                Rebind();
            }
        }

        /// <summary>The text shown when the key cannot be resolved.</summary>
        [UxmlAttribute("fallback")]
        public string Fallback
        {
            get => _fallback;
            set
            {
                _fallback = value;
                Rebind();
            }
        }

        /// <summary>Sets the <c>{0}</c> style arguments and resolves again.</summary>
        /// <param name="args">The positional arguments.</param>
        public void SetArgs(params object[] args)
        {
            _args = args;
            Rebind();
        }

        private void Rebind() => _binder.SetText(LocalizedTextFactory.Create(_key, _table, _fallback, _args));
    }

    /// <summary>Builds the <see cref="UiText"/> of the localized UXML elements from their attributes.</summary>
    internal static class LocalizedTextFactory
    {
        public static UiText Create(string key, string table, string fallback, object[] args)
        {
            if (string.IsNullOrEmpty(key)) return UiText.Empty;

            var text = string.IsNullOrEmpty(table) ? UiText.FromKey(key, args) : UiText.FromKeyIn(table, key, args);
            return string.IsNullOrEmpty(fallback) ? text : text.WithFallback(fallback);
        }
    }
}
