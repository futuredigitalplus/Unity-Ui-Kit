using System;
using UnityEngine;

namespace FutureDigitalPlus.UIKit.Language
{
    /// <summary>JSON loading for <see cref="DictionaryTextSource"/>.</summary>
    public static class DictionaryTextSourceJson
    {
#pragma warning disable CS0649 // filled by JsonUtility
        [Serializable]
        private sealed class Entry
        {
            public string locale;
            public string table;
            public string key;
            public string value;
        }

        [Serializable]
        private sealed class Document
        {
            public Entry[] entries;
        }
#pragma warning restore CS0649

        /// <summary>
        /// Loads entries from JSON of the form
        /// <c>{"entries":[{"locale":"en","table":"UI","key":"HOME","value":"Home"}]}</c>. A missing table means the default table.
        /// Does not raise <see cref="DictionaryTextSource.Changed"/>.
        /// </summary>
        /// <param name="source">The source to fill.</param>
        /// <param name="json">The JSON text.</param>
        /// <returns>How many entries were added; 0 for text that is not valid JSON of this form.</returns>
        public static int LoadJson(this DictionaryTextSource source, string json)
        {
            if (source == null || string.IsNullOrWhiteSpace(json)) return 0;

            Document document;
            try
            {
                document = JsonUtility.FromJson<Document>(json);
            }
            catch (ArgumentException)
            {
                return 0;
            }

            if (document?.entries == null) return 0;

            var added = 0;
            foreach (var entry in document.entries)
            {
                if (entry == null || string.IsNullOrEmpty(entry.key) || entry.value == null) continue;

                source.Add(entry.locale, entry.table, entry.key, entry.value);
                added++;
            }

            return added;
        }
    }
}
