using System;
using System.Collections.Generic;

namespace FutureDigitalPlus.UIKit.Hud
{
    /// <summary>One line of a <see cref="TimedFeedModel"/> (a kill message, a loot pickup, a system notice).</summary>
    public sealed class FeedEntry
    {
        internal FeedEntry(int id, string key, string text, string style, object tag)
        {
            Id = id;
            Key = key;
            Text = text;
            Style = style;
            Tag = tag;
            Count = 1;
        }

        /// <summary>Unique number of this entry within its model.</summary>
        public int Id { get; }

        /// <summary>Merge key; entries added with the same key are merged into one. May be null.</summary>
        public string Key { get; }

        /// <summary>The message text.</summary>
        public string Text { get; }

        /// <summary>Free style name (a USS class suffix) chosen by the caller. May be null.</summary>
        public string Style { get; }

        /// <summary>Free payload for the caller.</summary>
        public object Tag { get; }

        /// <summary>How many identical messages were merged into this entry (1 or more).</summary>
        public int Count { get; internal set; }

        /// <summary>Seconds since the entry was added or last merged into.</summary>
        public float Age { get; internal set; }

        /// <summary>True during the last <see cref="TimedFeedModel.FadeSeconds"/> of the entry's life.</summary>
        public bool IsFading { get; internal set; }
    }

    /// <summary>Why an entry left a <see cref="TimedFeedModel"/>.</summary>
    public enum FeedRemoveReason
    {
        /// <summary>Its time to live ran out.</summary>
        Expired,

        /// <summary>The feed was full and this was the oldest entry.</summary>
        Evicted,

        /// <summary>Removed by <see cref="TimedFeedModel.Clear"/>.</summary>
        Cleared,
    }

    /// <summary>
    /// The logic of a short-lived message feed: at most N entries, each living for a time to live, identical messages
    /// merged into one with a counter, newest at the top or at the bottom. No time source: the caller feeds
    /// <see cref="Tick"/>.
    /// </summary>
    public sealed class TimedFeedModel
    {
        private readonly List<FeedEntry> _entries = new List<FeedEntry>(8); // oldest first
        private int _nextId = 1;

        /// <summary>Most entries kept at once (default 5). A new entry past the limit pushes the oldest out.</summary>
        public int MaxEntries { get; set; } = 5;

        /// <summary>Seconds an entry lives after it was added or merged into (default 4).</summary>
        public float TimeToLive { get; set; } = 4f;

        /// <summary>Length of the fade-out at the end of an entry's life (default 0.4).</summary>
        public float FadeSeconds { get; set; } = 0.4f;

        /// <summary>When true (default) an entry with the same non-null key as a live one is merged into it.</summary>
        public bool MergeIdentical { get; set; } = true;

        /// <summary>When true the newest entry is displayed first (index 0); by default it is displayed last.</summary>
        public bool NewestOnTop { get; set; }

        /// <summary>Number of live entries.</summary>
        public int Count => _entries.Count;

        /// <summary>The entry at a display position, 0 being the top row.</summary>
        public FeedEntry this[int displayIndex] => _entries[NewestOnTop ? _entries.Count - 1 - displayIndex : displayIndex];

        /// <summary>Raised after an entry was added.</summary>
        public event Action<FeedEntry> EntryAdded;

        /// <summary>Raised after an entry changed (merged into, or started fading).</summary>
        public event Action<FeedEntry> EntryUpdated;

        /// <summary>Raised after an entry was removed.</summary>
        public event Action<FeedEntry, FeedRemoveReason> EntryRemoved;

        /// <summary>The display position of <paramref name="entry"/>, or -1 when it is not in the feed.</summary>
        public int DisplayIndexOf(FeedEntry entry)
        {
            var i = _entries.IndexOf(entry);
            if (i < 0) return -1;
            return NewestOnTop ? _entries.Count - 1 - i : i;
        }

        /// <summary>Adds a message, or merges it into a live entry with the same <paramref name="key"/>.</summary>
        public FeedEntry Add(string text, string key = null, string style = null, object tag = null)
        {
            if (MergeIdentical && key != null)
            {
                for (var i = _entries.Count - 1; i >= 0; i--)
                {
                    var existing = _entries[i];
                    if (!string.Equals(existing.Key, key, StringComparison.Ordinal)) continue;
                    existing.Count++;
                    existing.Age = 0f;
                    existing.IsFading = false;
                    EntryUpdated?.Invoke(existing);
                    return existing;
                }
            }

            var entry = new FeedEntry(_nextId++, key, text, style, tag);
            _entries.Add(entry);
            EntryAdded?.Invoke(entry);

            var max = Math.Max(1, MaxEntries);
            while (_entries.Count > max) RemoveAt(0, FeedRemoveReason.Evicted);
            return entry;
        }

        /// <summary>Advances time: ages entries, starts fades and removes entries whose time to live ran out.</summary>
        public void Tick(float deltaSeconds)
        {
            if (!(deltaSeconds > 0f)) deltaSeconds = 0f;   // also NaN: an age that is NaN never reaches the time to live
            for (var i = _entries.Count - 1; i >= 0; i--)
            {
                var entry = _entries[i];
                entry.Age += deltaSeconds;
                if (entry.Age >= TimeToLive)
                {
                    RemoveAt(i, FeedRemoveReason.Expired);
                    continue;
                }

                var fading = FadeSeconds > 0f && TimeToLive - entry.Age < FadeSeconds;
                if (fading == entry.IsFading) continue;
                entry.IsFading = fading;
                EntryUpdated?.Invoke(entry);
            }
        }

        /// <summary>Removes every entry.</summary>
        public void Clear()
        {
            for (var i = _entries.Count - 1; i >= 0; i--) RemoveAt(i, FeedRemoveReason.Cleared);
        }

        private void RemoveAt(int index, FeedRemoveReason reason)
        {
            var entry = _entries[index];
            _entries.RemoveAt(index);
            EntryRemoved?.Invoke(entry, reason);
        }
    }
}
