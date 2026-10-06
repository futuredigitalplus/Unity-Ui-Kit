using System;
using System.Collections.Generic;
using System.Text;
using FutureDigitalPlus.UIKit.Foundation;

namespace FutureDigitalPlus.UIKit.Language
{
    /// <summary>
    /// An in-memory <see cref="ILocalizationBackend"/>: strings added from code, a CSV file or JSON (see
    /// <c>DictionaryTextSourceJson</c>). For prototypes, tests, small games and server-delivered text; no package is needed.
    /// A lookup tries the exact locale (<c>pt-BR</c>), then its language (<c>pt</c>), then the fallback locale (default <c>en</c>).
    /// Pure C#; main thread only.
    /// </summary>
    public sealed class DictionaryTextSource : ILocalizationBackend
    {
        // locale -> table -> key -> text
        private readonly Dictionary<string, Dictionary<string, Dictionary<string, string>>> _data =
            new Dictionary<string, Dictionary<string, Dictionary<string, string>>>(StringComparer.OrdinalIgnoreCase);

        private string _locale;

        /// <summary>Creates a source whose active locale is <paramref name="locale"/>.</summary>
        /// <param name="locale">The active language code; null or blank means <c>en</c>.</param>
        /// <param name="fallbackLocale">The language tried last; null or blank means <c>en</c>.</param>
        public DictionaryTextSource(string locale = "en", string fallbackLocale = "en")
        {
            _locale = Normalize(locale);
            FallbackLocale = Normalize(fallbackLocale);
        }

        /// <summary>The language tried when the active one has no such entry.</summary>
        public string FallbackLocale { get; set; }

        /// <inheritdoc />
        public string CurrentLocale => _locale;

        /// <inheritdoc />
        public event Action Changed;

        /// <summary>The locales that have at least one entry.</summary>
        public IReadOnlyCollection<string> Locales => _data.Keys;

        /// <summary>Makes <paramref name="locale"/> the active language and raises <see cref="Changed"/> when it differs from the current one.</summary>
        public void SetLocale(string locale)
        {
            var normalized = Normalize(locale);
            if (string.Equals(normalized, _locale, StringComparison.OrdinalIgnoreCase)) return;

            _locale = normalized;
            Changed?.Invoke();
        }

        /// <summary>Adds or replaces one entry. Does not raise <see cref="Changed"/>; call <see cref="NotifyChanged"/> after a batch.</summary>
        /// <param name="locale">The language the text is in.</param>
        /// <param name="table">The table name; blank means <c>Default</c>.</param>
        /// <param name="key">The entry key.</param>
        /// <param name="value">The text, with optional <c>{0}</c> placeholders.</param>
        public void Add(string locale, string table, string key, string value)
        {
            if (string.IsNullOrEmpty(key) || value == null) return;

            var l = Normalize(locale);
            var t = string.IsNullOrWhiteSpace(table) ? DefaultTable : table.Trim();

            if (!_data.TryGetValue(l, out var tables))
                _data[l] = tables = new Dictionary<string, Dictionary<string, string>>(StringComparer.Ordinal);
            if (!tables.TryGetValue(t, out var entries))
                tables[t] = entries = new Dictionary<string, string>(StringComparer.Ordinal);

            entries[key] = value;
        }

        /// <summary>The table used when none is given.</summary>
        public const string DefaultTable = "Default";

        /// <summary>Raises <see cref="Changed"/>, for example after loading a file.</summary>
        public void NotifyChanged() => Changed?.Invoke();

        /// <summary>Removes every entry of every locale.</summary>
        public void Clear() => _data.Clear();

        /// <inheritdoc />
        public LookupStatus TryGet(string table, string key, object[] args, out string text)
        {
            text = null;
            var tableName = string.IsNullOrEmpty(table) ? DefaultTable : table;
            var tableFound = false;

            foreach (var locale in Chain())
            {
                if (!_data.TryGetValue(locale, out var tables) || !tables.TryGetValue(tableName, out var entries)) continue;

                tableFound = true;
                if (!entries.TryGetValue(key ?? string.Empty, out var pattern)) continue;

                text = args != null && args.Length > 0 ? LocalizationKeyResolver.Format(pattern, args) : pattern;
                return LookupStatus.Found;
            }

            return tableFound ? LookupStatus.EntryMissing : LookupStatus.TableMissing;
        }

        // The exact locale, its language, then the fallback (and its language), each at most once.
        private IEnumerable<string> Chain()
        {
            var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var candidate in new[] { _locale, LanguageOnly(_locale), FallbackLocale, LanguageOnly(FallbackLocale) })
                if (!string.IsNullOrEmpty(candidate) && seen.Add(candidate)) yield return candidate;
        }

        private static string LanguageOnly(string locale)
        {
            var cut = locale?.IndexOf('-') ?? -1;
            return cut > 0 ? locale.Substring(0, cut) : locale;
        }

        private static string Normalize(string locale) =>
            string.IsNullOrWhiteSpace(locale) ? "en" : locale.Trim().Replace('_', '-');

        /// <summary>
        /// Loads entries from CSV text. The first row is the header: <c>table,key,&lt;locale&gt;,&lt;locale&gt;...</c> (for example
        /// <c>table,key,en,de,ar</c>); every other row is one entry with its text per locale. Fields may be quoted with <c>"</c>
        /// (<c>""</c> inside a quoted field is a quote, a quoted field may contain commas and line breaks). A blank table
        /// cell means <see cref="DefaultTable"/>; an empty text cell adds nothing for that locale. Does not raise <see cref="Changed"/>.
        /// </summary>
        /// <param name="csv">The file content.</param>
        /// <returns>How many texts were added.</returns>
        public int LoadCsv(string csv)
        {
            if (string.IsNullOrEmpty(csv)) return 0;

            var rows = ParseCsv(csv);
            if (rows.Count < 2) return 0;

            var header = rows[0];
            var added = 0;

            for (var r = 1; r < rows.Count; r++)
            {
                var row = rows[r];
                if (row.Count < 2) continue;

                var table = row[0];
                var key = row[1].Trim();
                if (key.Length == 0) continue;

                for (var c = 2; c < row.Count && c < header.Count; c++)
                {
                    var locale = header[c].Trim();
                    if (locale.Length == 0 || row[c].Length == 0) continue;

                    Add(locale, table, key, row[c]);
                    added++;
                }
            }

            return added;
        }

        private static List<List<string>> ParseCsv(string text)
        {
            var rows = new List<List<string>>();
            var row = new List<string>();
            var field = new StringBuilder();
            var quoted = false;
            var i = 0;

            if (text.Length > 0 && text[0] == '﻿') i = 1;   // byte order mark

            for (; i < text.Length; i++)
            {
                var ch = text[i];

                if (quoted)
                {
                    if (ch == '"')
                    {
                        if (i + 1 < text.Length && text[i + 1] == '"') { field.Append('"'); i++; }
                        else quoted = false;
                    }
                    else
                    {
                        field.Append(ch);
                    }

                    continue;
                }

                switch (ch)
                {
                    case '"':
                        quoted = true;
                        break;
                    case ',':
                        row.Add(field.ToString());
                        field.Clear();
                        break;
                    case '\r':
                        break;
                    case '\n':
                        row.Add(field.ToString());
                        field.Clear();
                        if (!(row.Count == 1 && row[0].Length == 0)) rows.Add(row);
                        row = new List<string>();
                        break;
                    default:
                        field.Append(ch);
                        break;
                }
            }

            if (field.Length > 0 || row.Count > 0)
            {
                row.Add(field.ToString());
                rows.Add(row);
            }

            return rows;
        }
    }
}
