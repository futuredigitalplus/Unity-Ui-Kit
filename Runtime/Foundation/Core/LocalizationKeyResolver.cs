using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace FutureDigitalPlus.UIKit.Foundation
{
    /// <summary>
    /// Turns <see cref="UiText"/> into a display string. Looks a key up in the table named by the text (or in <see cref="DefaultTables"/>
    /// in order), falls back to the text's own fallback, then to a visible placeholder, and NEVER throws and never returns null, so a UI
    /// still renders when the Localization package is not set up, a table is missing or a key was not imported.
    /// A table that reports <see cref="LookupStatus.TableMissing"/> (or whose lookup throws) is not asked again until <see cref="ResetMissing"/>,
    /// so a missing table costs one failed lookup per locale change, not one per label.
    /// Pure C#: the Unity side supplies the lookup (<c>UnityStringTableLookup</c>).
    /// </summary>
    public sealed class LocalizationKeyResolver
    {
        /// <summary>Tables tried, in order, for a key that names no table, when the resolver was created without a list: just <see cref="LocalizationKey.DefaultTable"/>.</summary>
        public static readonly string[] DefaultTables = { LocalizationKey.DefaultTable };

        readonly TableLookup _lookup;
        readonly string[] _tables;
        readonly HashSet<string> _missingTables = new HashSet<string>(StringComparer.Ordinal);

        /// <summary>Format of the placeholder for a key that resolved nowhere and has no fallback; <c>{0}</c> is the key. Default <c>[{0}]</c>.</summary>
        public string MissingKeyFormat { get; set; } = "[{0}]";

        /// <summary>Raised once per table the first time it is found missing (after the last <see cref="ResetMissing"/>), for one log line.</summary>
        public event Action<string> TableMissing;

        /// <summary>Creates a resolver. <paramref name="tables"/> are the tables tried for keys without a table, in order.</summary>
        public LocalizationKeyResolver(TableLookup lookup, string[] tables = null)
        {
            _lookup = lookup;
            _tables = tables != null && tables.Length > 0 ? tables : DefaultTables;
        }

        /// <summary>Tables that answered <see cref="LookupStatus.TableMissing"/> and are not asked again until <see cref="ResetMissing"/>.</summary>
        public IReadOnlyCollection<string> MissingTables
        {
            get { return _missingTables; }
        }

        /// <summary>Forget which tables were missing (call on locale change and after the localization system finished initialising).</summary>
        public void ResetMissing()
        {
            _missingTables.Clear();
        }

        /// <summary>Display string for <paramref name="text"/>. Never null, never throws.</summary>
        public string Resolve(UiText text)
        {
            if (ReferenceEquals(text, null) || text.IsEmpty)
                return string.Empty;
            try
            {
                return ResolveCore(text);
            }
            catch (Exception ex)
            {
                UiLog.Warning(ex);
                return text.Fallback ?? text.Literal ?? text.Key ?? string.Empty;
            }
        }

        string ResolveCore(UiText text)
        {
            if (text.Parts != null)
            {
                StringBuilder builder = new StringBuilder();
                for (int i = 0; i < text.Parts.Count; i++)
                {
                    if (i > 0)
                        builder.Append(text.Separator);
                    builder.Append(Resolve(text.Parts[i]));
                }
                return builder.ToString();
            }

            if (text.Literal != null)
                return text.Args.Count == 0 ? text.Literal : Format(text.Literal, text.Args);

            object[] args = ToArray(text.Args);
            if (_lookup != null)
            {
                if (text.Table != null)
                {
                    string found;
                    if (TryTable(text.Table, text.Key, args, out found))
                        return found;
                }
                else
                {
                    for (int i = 0; i < _tables.Length; i++)
                    {
                        string found;
                        if (TryTable(_tables[i], text.Key, args, out found))
                            return found;
                    }
                }
            }

            if (text.Fallback != null)
                return text.Args.Count == 0 ? text.Fallback : Format(text.Fallback, text.Args);
            return Format(MissingKeyFormat, new object[] { text.Key });
        }

        bool TryTable(string table, string key, object[] args, out string result)
        {
            result = null;
            if (_missingTables.Contains(table))
                return false;
            LookupStatus status;
            string text;
            try
            {
                status = _lookup(table, key, args, out text);
            }
            catch (Exception ex)
            {
                UiLog.Warning(ex);
                MarkMissing(table);
                return false;
            }

            if (status == LookupStatus.Found && text != null)
            {
                result = text;
                return true;
            }
            if (status == LookupStatus.TableMissing)
                MarkMissing(table);
            return false;
        }

        void MarkMissing(string table)
        {
            if (!_missingTables.Add(table))
                return;
            Action<string> handler = TableMissing;
            if (handler != null)
                handler(table);
        }

        static object[] ToArray(IReadOnlyList<object> args)
        {
            object[] array = new object[args.Count];
            for (int i = 0; i < array.Length; i++)
                array[i] = args[i];
            return array;
        }

        /// <summary>string.Format with the invariant culture that returns the pattern unchanged when it is not a valid format string.</summary>
        public static string Format(string pattern, IReadOnlyList<object> args)
        {
            if (string.IsNullOrEmpty(pattern) || args == null || args.Count == 0)
                return pattern ?? string.Empty;
            return Format(pattern, ToArray(args));
        }

        /// <summary>The same for an argument array.</summary>
        public static string Format(string pattern, object[] args)
        {
            if (string.IsNullOrEmpty(pattern) || args == null || args.Length == 0)
                return pattern ?? string.Empty;
            try
            {
                return string.Format(CultureInfo.InvariantCulture, pattern, args);
            }
            catch (FormatException)
            {
                return pattern;
            }
        }
    }
}
