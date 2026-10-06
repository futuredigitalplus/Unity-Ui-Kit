using System;
using System.Collections.Generic;
using UnityEngine.UIElements;

namespace FutureDigitalPlus.UIKit.Controls
{
    /// <summary>
    /// A <see cref="ListView"/> that is fed with whole lists and keeps its place. Rows are identified by a stable key:
    /// when the data is replaced the same ListView keeps its scroll offset and its selection (restored by key), rows
    /// whose content did not change are not rebound, and an identical list does nothing at all. It also shows an
    /// empty state when there are no rows and skeleton rows while loading. Reach the inner list through
    /// <see cref="ListView"/> for selection mode and events.
    /// </summary>
    /// <typeparam name="T">The row data type.</typeparam>
    public sealed class ListViewBinder<T> : VisualElement
    {
        private readonly List<T> _items = new List<T>();
        private readonly Func<T, string> _keyOf;
        private readonly Func<T, T, bool> _sameContent;
        private readonly Action<VisualElement, T> _bindItem;
        private readonly CollectionStates _states;
        private List<string> _keys = new List<string>();
        private bool _loading;

        /// <summary>Creates the binder.</summary>
        /// <param name="makeItem">Creates one empty row element.</param>
        /// <param name="bindItem">Fills a row element with a data item.</param>
        /// <param name="keyOf">The stable key of an item (an id); keys must be unique within a list.</param>
        /// <param name="itemHeight">Fixed row height; 0 or less lets rows have their own height (dynamic height).</param>
        /// <param name="sameContent">Tells whether an item changed; the default is <c>EqualityComparer&lt;T&gt;.Default</c>.</param>
        public ListViewBinder(Func<VisualElement> makeItem, Action<VisualElement, T> bindItem, Func<T, string> keyOf,
            float itemHeight = 0f, Func<T, T, bool> sameContent = null)
        {
            _keyOf = keyOf ?? throw new ArgumentNullException(nameof(keyOf));
            _bindItem = bindItem ?? throw new ArgumentNullException(nameof(bindItem));
            _sameContent = sameContent ?? EqualityComparer<T>.Default.Equals;
            if (makeItem == null) throw new ArgumentNullException(nameof(makeItem));

            AddToClassList("fdp-list-view-binder");
            ListView = new ListView(_items, itemHeight > 0f ? itemHeight : -1f, makeItem, Bind);
            ListView.AddToClassList("fdp-list-view-binder__list");
            if (itemHeight <= 0f) ListView.virtualizationMethod = CollectionVirtualizationMethod.DynamicHeight;
            Add(ListView);
            _states = new CollectionStates(this, ListView, 6, itemHeight > 0f ? itemHeight : 56f);
        }

        /// <summary>The inner list view.</summary>
        public ListView ListView { get; }

        /// <summary>The items currently shown.</summary>
        public IReadOnlyList<T> Items => _items;

        /// <summary>Content shown instead of the list when there are no items and nothing is loading; null shows an empty list.</summary>
        public VisualElement EmptyState
        {
            get => _states.EmptyState;
            set
            {
                _states.EmptyState = value;
                _states.Update(_items.Count, _loading);
            }
        }

        /// <summary>True while data is being fetched: skeleton rows replace the list.</summary>
        public bool IsLoading
        {
            get => _loading;
            set
            {
                _loading = value;
                _states.Update(_items.Count, _loading);
            }
        }

        /// <summary>Number of skeleton rows shown while loading (default 6).</summary>
        public void SetSkeleton(int rows, float rowHeight) => _states.SetSkeleton(rows, rowHeight);

        /// <summary>The key of the first selected item, or null.</summary>
        public string SelectedKey => ListView.selectedIndex >= 0 && ListView.selectedIndex < _items.Count ? _keyOf(_items[ListView.selectedIndex]) : null;

        /// <summary>Replaces the data. Keeps scroll and selection, skips the work when nothing changed.</summary>
        public void SetItems(IReadOnlyList<T> items)
        {
            items = items ?? Array.Empty<T>();
            var newKeys = new List<string>(items.Count);
            for (var i = 0; i < items.Count; i++) newKeys.Add(_keyOf(items[i]));

            var diff = ListDiff.Compute(_keys, newKeys, StringComparer.Ordinal);
            var unchanged = diff.IsIdentical;
            for (var i = 0; unchanged && i < items.Count; i++)
                if (!_sameContent(_items[i], items[i])) unchanged = false;
            if (unchanged)
            {
                _states.Update(_items.Count, _loading);
                return;
            }

            var selectedKeys = new List<string>();
            foreach (var index in ListView.selectedIndices)
                if (index >= 0 && index < _keys.Count) selectedKeys.Add(_keys[index]);

            _items.Clear();
            for (var i = 0; i < items.Count; i++) _items.Add(items[i]);
            _keys = newKeys;
            ListView.RefreshItems();

            if (selectedKeys.Count > 0)
            {
                var restored = new List<int>(selectedKeys.Count);
                foreach (var key in selectedKeys)
                {
                    var at = _keys.IndexOf(key);
                    if (at >= 0) restored.Add(at);
                }

                ListView.SetSelectionWithoutNotify(restored);
            }

            _states.Update(_items.Count, _loading);
        }

        /// <summary>Selects the item with the given key (without raising selection events). Returns false when there is none.</summary>
        public bool SelectKey(string key)
        {
            var at = _keys.IndexOf(key);
            if (at < 0) return false;
            ListView.SetSelectionWithoutNotify(new[] { at });
            return true;
        }

        private void Bind(VisualElement element, int index)
        {
            if (index >= 0 && index < _items.Count) _bindItem(element, _items[index]);
        }
    }
}
