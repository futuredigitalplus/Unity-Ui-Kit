using System;
using System.Collections.Generic;

namespace FutureDigitalPlus.UIKit.Foundation
{
    /// <summary>
    /// A piece of player-facing text that is either a localization key (looked up in the string tables at display time), a literal
    /// string that is already localized (server or platform messages, numbers), or a composition of several of those. Immutable;
    /// two instances with the same content are equal, which the toast queue uses to coalesce repeats.
    /// Pure C#, no UnityEngine; <see cref="LocalizationKeyResolver"/> turns it into a display string.
    /// </summary>
    public sealed class UiText : IEquatable<UiText>
    {
        /// <summary>The empty text.</summary>
        public static readonly UiText Empty = new UiText(null, null, null, null, null, null, null);

        static readonly object[] NoArgs = new object[0];

        /// <summary>Table name, or null to try the default tables (<see cref="LocalizationKeyResolver.DefaultTables"/>).</summary>
        public string Table { get; }

        /// <summary>Localization key such as <c>HOMEBTN-PLAY</c>, or null for a literal or a composition.</summary>
        public string Key { get; }

        /// <summary>Text shown when the key cannot be resolved (missing table, missing entry, localization not initialised). May be null.</summary>
        public string Fallback { get; }

        /// <summary>Already localized text, or null.</summary>
        public string Literal { get; }

        /// <summary>Positional arguments for <c>{0}</c> style placeholders (Smart Strings in the tables, string.Format for fallbacks).</summary>
        public IReadOnlyList<object> Args { get; }

        /// <summary>Parts of a composition, or null.</summary>
        public IReadOnlyList<UiText> Parts { get; }

        /// <summary>Separator between the parts of a composition.</summary>
        public string Separator { get; }

        UiText(string table, string key, string fallback, string literal, object[] args, UiText[] parts, string separator)
        {
            Table = table;
            Key = key;
            Fallback = fallback;
            Literal = literal;
            Args = args ?? NoArgs;
            Parts = parts;
            Separator = separator;
        }

        /// <summary>True when there is no key, literal or composition.</summary>
        public bool IsEmpty
        {
            get { return Key == null && Literal == null && Parts == null; }
        }

        /// <summary>True for a localization key.</summary>
        public bool IsKey
        {
            get { return Key != null; }
        }

        /// <summary>A key in the default tables. Accepts the qualified form <c>Table:KEY</c> (see <see cref="LocalizationKey.Parse"/>).</summary>
        public static UiText FromKey(string key, params object[] args)
        {
            string table;
            string entry;
            LocalizationKey.Parse(key, out table, out entry);
            if (string.IsNullOrEmpty(entry))
                return Empty;
            return new UiText(table, entry, null, null, args, null, null);
        }

        /// <summary>A key in one named table.</summary>
        public static UiText FromKeyIn(string table, string key, params object[] args)
        {
            if (string.IsNullOrEmpty(key))
                return Empty;
            return new UiText(string.IsNullOrEmpty(table) ? null : table, key, null, null, args, null, null);
        }

        /// <summary>Text that needs no lookup.</summary>
        public static UiText FromLiteral(string text)
        {
            if (text == null)
                return Empty;
            return new UiText(null, null, null, text, null, null, null);
        }

        /// <summary>Several texts joined with <paramref name="separator"/> (for example a two-word button label built from two keys).</summary>
        public static UiText Compose(string separator, params UiText[] parts)
        {
            if (parts == null || parts.Length == 0)
                return Empty;
            UiText[] copy = new UiText[parts.Length];
            for (int i = 0; i < copy.Length; i++)
                copy[i] = parts[i] ?? Empty; // a null part must not crash Equals or the resolver later
            return new UiText(null, null, null, null, null, copy, separator ?? string.Empty);
        }

        /// <summary>Same text with a fallback for the case that the key cannot be resolved.</summary>
        public UiText WithFallback(string fallback)
        {
            return new UiText(Table, Key, fallback, Literal, ArgsArray(), PartsArray(), Separator);
        }

        /// <summary>Same key with other placeholder arguments.</summary>
        public UiText WithArgs(params object[] args)
        {
            return new UiText(Table, Key, Fallback, Literal, args, PartsArray(), Separator);
        }

        object[] ArgsArray()
        {
            if (Args.Count == 0)
                return NoArgs;
            object[] copy = new object[Args.Count];
            for (int i = 0; i < copy.Length; i++)
                copy[i] = Args[i];
            return copy;
        }

        UiText[] PartsArray()
        {
            if (Parts == null)
                return null;
            UiText[] copy = new UiText[Parts.Count];
            for (int i = 0; i < copy.Length; i++)
                copy[i] = Parts[i];
            return copy;
        }

        /// <summary>Same content (table, key, fallback, literal, arguments, parts and separator).</summary>
        public bool Equals(UiText other)
        {
            if (ReferenceEquals(other, null))
                return false;
            if (ReferenceEquals(other, this))
                return true;
            if (Table != other.Table || Key != other.Key || Fallback != other.Fallback || Literal != other.Literal || Separator != other.Separator)
                return false;
            if (Args.Count != other.Args.Count)
                return false;
            for (int i = 0; i < Args.Count; i++)
            {
                if (!Equals(Args[i], other.Args[i]))
                    return false;
            }
            if ((Parts == null) != (other.Parts == null))
                return false;
            if (Parts != null)
            {
                if (Parts.Count != other.Parts.Count)
                    return false;
                for (int i = 0; i < Parts.Count; i++)
                {
                    if (!Parts[i].Equals(other.Parts[i]))
                        return false;
                }
            }
            return true;
        }

        /// <summary>Same content.</summary>
        public override bool Equals(object obj)
        {
            return Equals(obj as UiText);
        }

        /// <summary>Hash of the content.</summary>
        public override int GetHashCode()
        {
            unchecked
            {
                int hash = 17;
                hash = hash * 31 + (Table == null ? 0 : Table.GetHashCode());
                hash = hash * 31 + (Key == null ? 0 : Key.GetHashCode());
                hash = hash * 31 + (Literal == null ? 0 : Literal.GetHashCode());
                hash = hash * 31 + Args.Count;
                return hash;
            }
        }

        /// <summary>A readable form for logs: the key, the literal or the composition.</summary>
        public override string ToString()
        {
            if (Key != null)
                return (Table == null ? string.Empty : Table + ":") + Key;
            if (Literal != null)
                return "\"" + Literal + "\"";
            if (Parts != null)
                return "compose(" + Parts.Count + ")";
            return "(empty)";
        }
    }
}
