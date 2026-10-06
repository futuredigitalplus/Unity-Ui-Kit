using System;
using System.Collections.Generic;

namespace FutureDigitalPlus.UIKit.Controls
{
    /// <summary>The plan produced by <see cref="ListDiff.Compute{TKey}"/>: which new rows reuse which old rows.</summary>
    public sealed class ListDiffResult
    {
        internal ListDiffResult(int[] newToOld, int[] removedOld, bool orderPreserved)
        {
            NewToOld = newToOld;
            RemovedOld = removedOld;
            OrderPreserved = orderPreserved;
        }

        /// <summary>For every index of the new list, the index of the old row with the same key, or -1 for a new row.</summary>
        public int[] NewToOld { get; }

        /// <summary>Indexes of old rows whose key is gone.</summary>
        public int[] RemovedOld { get; }

        /// <summary>True when the rows that exist in both lists have the same relative order (only inserts and removals happened).</summary>
        public bool OrderPreserved { get; }

        /// <summary>Number of new rows that have no old counterpart.</summary>
        public int InsertedCount
        {
            get
            {
                var n = 0;
                for (var i = 0; i < NewToOld.Length; i++)
                    if (NewToOld[i] < 0) n++;
                return n;
            }
        }

        /// <summary>True when both lists have the same keys in the same order.</summary>
        public bool IsIdentical
        {
            get
            {
                if (RemovedOld.Length != 0) return false;
                for (var i = 0; i < NewToOld.Length; i++)
                    if (NewToOld[i] != i) return false;
                return true;
            }
        }
    }

    /// <summary>
    /// Compares two lists by stable key. A list view uses the result to keep its scroll position and selection when
    /// the data is replaced: rows with the same key are the same row, whatever their index.
    /// </summary>
    public static class ListDiff
    {
        /// <summary>Matches the keys of a new list against an old one. Duplicate keys match in order of appearance.</summary>
        public static ListDiffResult Compute<TKey>(IReadOnlyList<TKey> oldKeys, IReadOnlyList<TKey> newKeys,
            IEqualityComparer<TKey> comparer = null)
        {
            if (oldKeys == null) throw new ArgumentNullException(nameof(oldKeys));
            if (newKeys == null) throw new ArgumentNullException(nameof(newKeys));
            comparer = comparer ?? EqualityComparer<TKey>.Default;

            var positions = new Dictionary<TKey, Queue<int>>(oldKeys.Count, comparer);
            var nullPositions = new Queue<int>();
            for (var i = 0; i < oldKeys.Count; i++)
            {
                var key = oldKeys[i];
                if (key == null)
                {
                    nullPositions.Enqueue(i);
                    continue;
                }

                if (!positions.TryGetValue(key, out var queue)) positions[key] = queue = new Queue<int>(1);
                queue.Enqueue(i);
            }

            var newToOld = new int[newKeys.Count];
            var matched = new bool[oldKeys.Count];
            var orderPreserved = true;
            var lastOld = -1;
            for (var i = 0; i < newKeys.Count; i++)
            {
                var key = newKeys[i];
                Queue<int> queue;
                var found = key == null ? (queue = nullPositions).Count > 0 : positions.TryGetValue(key, out queue) && queue.Count > 0;
                if (!found)
                {
                    newToOld[i] = -1;
                    continue;
                }

                var old = queue.Dequeue();
                newToOld[i] = old;
                matched[old] = true;
                if (old < lastOld) orderPreserved = false;
                lastOld = old;
            }

            var removed = new List<int>();
            for (var i = 0; i < matched.Length; i++)
                if (!matched[i]) removed.Add(i);
            return new ListDiffResult(newToOld, removed.ToArray(), orderPreserved);
        }
    }
}
