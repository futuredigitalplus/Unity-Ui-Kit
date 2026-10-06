using System;
using System.Collections.Generic;

namespace FutureDigitalPlus.UIKit.Controls
{
    /// <summary>What a badge shows: a count, a plain dot, or nothing.</summary>
    public readonly struct BadgeValue : IEquatable<BadgeValue>
    {
        /// <summary>Creates a badge value.</summary>
        public BadgeValue(int count, bool dot)
        {
            Count = count;
            Dot = dot;
        }

        /// <summary>The number to show; 0 or less means no number.</summary>
        public int Count { get; }

        /// <summary>True when a plain dot should show (used when there is no number).</summary>
        public bool Dot { get; }

        /// <summary>True when there is anything to show.</summary>
        public bool IsVisible => Count > 0 || Dot;

        /// <inheritdoc />
        public bool Equals(BadgeValue other) => Count == other.Count && Dot == other.Dot;

        /// <inheritdoc />
        public override bool Equals(object obj) => obj is BadgeValue other && Equals(other);

        /// <inheritdoc />
        public override int GetHashCode() => Count * 397 ^ (Dot ? 1 : 0);
    }

    /// <summary>
    /// Notification badges for a hierarchy of places (a tab, a list inside it, one row of the list). A parent shows
    /// the sum of its children plus its own count, so the tab bar can show "3" for "shop" while "shop/offers" holds 2
    /// and "shop/gems" holds 1. Visiting a place clears it and everything below it. Paths use '/' between levels.
    /// </summary>
    public sealed class BadgeTree
    {
        /// <summary>The character that separates levels of a path.</summary>
        public const char Separator = '/';

        private sealed class Node
        {
            public int Count;
            public bool Dot;
        }

        private readonly Dictionary<string, Node> _nodes = new Dictionary<string, Node>(StringComparer.Ordinal);
        private readonly List<string> _scratch = new List<string>(8);

        /// <summary>Raised with a path whose displayed value changed (the changed place and each ancestor whose total changed).</summary>
        public event Action<string> Changed;

        /// <summary>Sets the count of a place itself (not counting its children).</summary>
        public void SetCount(string path, int count)
        {
            Mutate(path, node => node.Count = Math.Max(0, count));
        }

        /// <summary>Adds <paramref name="delta"/> to the count of a place (the result never goes below 0).</summary>
        public void Add(string path, int delta = 1)
        {
            Mutate(path, node => node.Count = Math.Max(0, node.Count + delta));
        }

        /// <summary>Sets or clears the dot of a place itself.</summary>
        public void SetDot(string path, bool dot)
        {
            Mutate(path, node => node.Dot = dot);
        }

        /// <summary>The value to show for a place: its own count plus all descendants; a dot when any of them has one and the total is 0.</summary>
        public BadgeValue Get(string path)
        {
            path = Normalize(path);
            var count = 0;
            var dot = false;
            foreach (var pair in _nodes)
            {
                if (!IsSameOrDescendant(pair.Key, path)) continue;
                count += pair.Value.Count;
                dot |= pair.Value.Dot;
            }

            return new BadgeValue(count, dot);
        }

        /// <summary>The count of a place itself, without its descendants.</summary>
        public int OwnCount(string path) => _nodes.TryGetValue(Normalize(path), out var node) ? node.Count : 0;

        /// <summary>The user visited a place: clears its count and dot and those of everything below it.</summary>
        public void Visit(string path)
        {
            path = Normalize(path);
            _scratch.Clear();
            foreach (var pair in _nodes)
                if (IsSameOrDescendant(pair.Key, path) && (pair.Value.Count > 0 || pair.Value.Dot)) _scratch.Add(pair.Key);
            if (_scratch.Count == 0) return;
            Change(_scratch, () =>
            {
                foreach (var key in _scratch)
                {
                    _nodes[key].Count = 0;
                    _nodes[key].Dot = false;
                }
            });
        }

        /// <summary>Clears every badge.</summary>
        public void Clear()
        {
            if (_nodes.Count == 0) return;
            var keys = new List<string>(_nodes.Keys);
            Change(keys, () => _nodes.Clear());
        }

        private void Mutate(string path, Action<Node> change)
        {
            path = Normalize(path);
            Change(new[] { path }, () =>
            {
                if (!_nodes.TryGetValue(path, out var node)) _nodes[path] = node = new Node();
                change(node);
            });
        }

        // Runs a change and raises Changed for every place (the changed ones and all their ancestors) whose value differs afterwards.
        private void Change(IEnumerable<string> keys, Action apply)
        {
            var affected = new HashSet<string>(StringComparer.Ordinal);
            foreach (var key in keys)
            {
                var current = key;
                while (affected.Add(current) && current.Length > 0)
                {
                    var cut = current.LastIndexOf(Separator);
                    current = cut < 0 ? string.Empty : current.Substring(0, cut);
                }
            }

            var before = new Dictionary<string, BadgeValue>(affected.Count, StringComparer.Ordinal);
            foreach (var path in affected) before[path] = Get(path);
            apply();
            foreach (var path in affected)
                if (!Get(path).Equals(before[path])) Changed?.Invoke(path);
        }

        private static bool IsSameOrDescendant(string key, string path)
        {
            if (path.Length == 0) return true;
            if (!key.StartsWith(path, StringComparison.Ordinal)) return false;
            return key.Length == path.Length || key[path.Length] == Separator;
        }

        private static string Normalize(string path) => path == null ? string.Empty : path.Trim(Separator);
    }
}
