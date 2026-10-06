using UnityEngine.UIElements;

namespace FutureDigitalPlus.UIKit.Language
{
    /// <summary>A <c>Button</c> whose text comes from a localization key; same attributes as <see cref="LocalizedLabel"/>.</summary>
    [UxmlElement]
    public partial class LocalizedButton : Button
    {
        private readonly LocalizedTextBinder _binder;
        private string _key;
        private string _table;
        private string _fallback;
        private object[] _args;

        /// <summary>Creates a button bound to <see cref="UiLocalization.Default"/>.</summary>
        public LocalizedButton()
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
}
