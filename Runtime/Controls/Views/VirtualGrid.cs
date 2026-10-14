using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace FutureDigitalPlus.UIKit.Controls
{
    /// <summary>
    /// A scrolling grid of equal cells that builds only the cells in view. The column count follows the width
    /// (<see cref="MinCellWidth"/>, spread evenly), cell height is fixed or follows the width through
    /// <see cref="CellAspect"/>, and cells are pooled and recycled while scrolling. Like <see cref="ListViewBinder{T}"/>
    /// it is fed with whole lists by stable key: the scroll offset stays, an identical list does nothing, and it has an
    /// empty state and skeleton cells for loading. Cells are created with <c>makeCell</c> and filled by <c>bindCell</c>.
    /// Make the cells focusable (<c>focusable = true</c>) for keyboard and gamepad use: the arrow keys and the d-pad then
    /// move focus by cell, scroll the target into view even when it is not built yet, and leave the grid at its edges.
    /// </summary>
    /// <typeparam name="T">The cell data type.</typeparam>
    public sealed class VirtualGrid<T> : VisualElement
    {
        private readonly List<T> _items = new List<T>();
        private readonly List<VisualElement> _cells = new List<VisualElement>();
        private readonly List<int> _bound = new List<int>();
        private readonly Func<VisualElement> _makeCell;
        private readonly Action<VisualElement, T, int> _bindCell;
        private readonly Func<T, string> _keyOf;
        private readonly Func<T, T, bool> _sameContent;
        private readonly CollectionStates _states;
        private List<string> _keys = new List<string>();
        private int _columns = 1;
        private float _cellWidth;
        private float _cellHeight;
        private bool _loading;
        private float _minCellWidth = 96f;
        private float _cellAspect = 1f;
        private float _fixedCellHeight;
        private float _spacing = 8f;

        /// <summary>Creates the grid.</summary>
        /// <param name="makeCell">Creates one empty cell element.</param>
        /// <param name="bindCell">Fills a cell element with the item at an index.</param>
        /// <param name="keyOf">The stable key of an item; null compares items by index only (every change rebinds).</param>
        /// <param name="sameContent">Tells whether an item changed; the default is <c>EqualityComparer&lt;T&gt;.Default</c>.</param>
        public VirtualGrid(Func<VisualElement> makeCell, Action<VisualElement, T, int> bindCell, Func<T, string> keyOf = null,
            Func<T, T, bool> sameContent = null)
        {
            _makeCell = makeCell ?? throw new ArgumentNullException(nameof(makeCell));
            _bindCell = bindCell ?? throw new ArgumentNullException(nameof(bindCell));
            _keyOf = keyOf;
            _sameContent = sameContent ?? EqualityComparer<T>.Default.Equals;

            AddToClassList("fdp-virtual-grid");
            ScrollView = new UnityEngine.UIElements.ScrollView(ScrollViewMode.Vertical);
            ScrollView.AddToClassList("fdp-virtual-grid__scroll");
            ScrollView.contentContainer.AddToClassList("fdp-virtual-grid__content");
            Add(ScrollView);
            _states = new CollectionStates(this, ScrollView, 8, 96f);

            ScrollView.RegisterCallback<GeometryChangedEvent>(_ => Relayout());
            ScrollView.verticalScroller.valueChanged += _ => UpdateVisible();
            RegisterCallback<NavigationMoveEvent>(OnNavigationMove);
        }

        /// <summary>The inner scroll view.</summary>
        public UnityEngine.UIElements.ScrollView ScrollView { get; }

        /// <summary>Number of items.</summary>
        public int Count => _items.Count;

        /// <summary>The current number of columns (follows the width).</summary>
        public int Columns => _columns;

        /// <summary>Height of one cell in the current layout.</summary>
        public float CellHeight => _cellHeight;

        /// <summary>Narrowest a cell may get; more columns are used as the grid widens (default 96).</summary>
        public float MinCellWidth
        {
            get => _minCellWidth;
            set
            {
                _minCellWidth = Mathf.Max(1f, value);
                Relayout();
            }
        }

        /// <summary>Cell height as a multiple of the cell width when <see cref="FixedCellHeight"/> is 0 (default 1, square cells).</summary>
        public float CellAspect
        {
            get => _cellAspect;
            set
            {
                _cellAspect = Mathf.Max(0.01f, value);
                Relayout();
            }
        }

        /// <summary>A cell height in panel units that does not depend on the width; 0 (default) uses <see cref="CellAspect"/>.</summary>
        public float FixedCellHeight
        {
            get => _fixedCellHeight;
            set
            {
                _fixedCellHeight = Mathf.Max(0f, value);
                Relayout();
            }
        }

        /// <summary>Gap between cells in both directions (default 8).</summary>
        public float Spacing
        {
            get => _spacing;
            set
            {
                _spacing = Mathf.Max(0f, value);
                Relayout();
            }
        }

        /// <summary>Content shown instead of the grid when there are no items and nothing is loading.</summary>
        public VisualElement EmptyState
        {
            get => _states.EmptyState;
            set
            {
                _states.EmptyState = value;
                _states.Update(_items.Count, _loading);
            }
        }

        /// <summary>True while data is being fetched: skeleton blocks replace the grid.</summary>
        public bool IsLoading
        {
            get => _loading;
            set
            {
                _loading = value;
                _states.Update(_items.Count, _loading);
            }
        }

        /// <summary>The cell currently showing the item at <paramref name="index"/>, or null when that item is not in view.</summary>
        public VisualElement CellAt(int index)
        {
            for (var slot = 0; slot < _cells.Count; slot++)
                if (_bound[slot] == index) return _cells[slot];
            return null;
        }

        /// <summary>Scrolls the minimum distance that brings the cell at <paramref name="index"/> into view and builds it.</summary>
        public void ScrollToIndex(int index)
        {
            if (index < 0 || index >= _items.Count || _columns <= 0 || _cellHeight <= 0f) return;
            var top = index / _columns * (_cellHeight + _spacing);
            var bottom = top + _cellHeight;
            var viewport = ScrollView.contentViewport.layout.height;
            var offset = ScrollView.scrollOffset.y;
            if (top < offset) offset = top;
            else if (bottom > offset + viewport) offset = bottom - viewport;
            else return;

            ScrollView.scrollOffset = new Vector2(0f, Mathf.Max(0f, offset));
            UpdateVisible();
        }

        /// <summary>Scrolls the cell at <paramref name="index"/> into view and gives it keyboard focus (when the cell is focusable).</summary>
        /// <returns>False when the index is out of range.</returns>
        public bool FocusIndex(int index)
        {
            if (index < 0 || index >= _items.Count) return false;
            ScrollToIndex(index);
            CellAt(index)?.Focus();
            return true;
        }

        /// <summary>The index of the item whose cell contains <paramref name="element"/>, or -1.</summary>
        public int ItemIndexOf(VisualElement element)
        {
            for (var e = element; e != null && e != this; e = e.hierarchy.parent)
                for (var slot = 0; slot < _cells.Count; slot++)
                    if (_cells[slot] == e) return _bound[slot];
            return -1;
        }

        /// <summary>Replaces the data. Keeps the scroll offset and skips the work when nothing changed.</summary>
        public void SetItems(IReadOnlyList<T> items)
        {
            items = items ?? Array.Empty<T>();
            var changed = true;
            List<string> newKeys = null;
            if (_keyOf != null)
            {
                newKeys = new List<string>(items.Count);
                for (var i = 0; i < items.Count; i++) newKeys.Add(_keyOf(items[i]));
                changed = !ListDiff.Compute(_keys, newKeys, StringComparer.Ordinal).IsIdentical;
            }
            else
            {
                changed = items.Count != _items.Count;
            }

            for (var i = 0; !changed && i < items.Count; i++)
                if (!_sameContent(_items[i], items[i])) changed = true;
            if (!changed)
            {
                _states.Update(_items.Count, _loading);
                return;
            }

            _items.Clear();
            for (var i = 0; i < items.Count; i++) _items.Add(items[i]);
            if (newKeys != null) _keys = newKeys;
            Relayout();
            _states.Update(_items.Count, _loading);
        }

        /// <summary>Rebinds every visible cell (after data that the items reference changed).</summary>
        public void Refresh()
        {
            ResetBindings();
            UpdateVisible();
        }

        private void OnNavigationMove(NavigationMoveEvent evt)
        {
            GridDirection direction;
            switch (evt.direction)
            {
                case NavigationMoveEvent.Direction.Left: direction = GridDirection.Left; break;
                case NavigationMoveEvent.Direction.Right: direction = GridDirection.Right; break;
                case NavigationMoveEvent.Direction.Up: direction = GridDirection.Up; break;
                case NavigationMoveEvent.Direction.Down: direction = GridDirection.Down; break;
                default: return;
            }

            var index = ItemIndexOf(evt.target as VisualElement);
            if (index < 0) return;
            var next = GridLayoutMath.Move(index, direction, _columns, _items.Count);
            if (next == index || next < 0) return; // at an edge: let the default navigation leave the grid
            evt.StopPropagation();
            panel?.focusController.IgnoreEvent(evt);
            FocusIndex(next);
        }

        private void Relayout()
        {
            var width = ScrollView.contentViewport.layout.width;
            if (float.IsNaN(width) || width <= 0f) return;

            _columns = GridLayoutMath.ColumnCount(width, _minCellWidth, _spacing);
            _cellWidth = GridLayoutMath.CellWidth(width, _columns, _spacing);
            _cellHeight = _fixedCellHeight > 0f ? _fixedCellHeight : _cellWidth * _cellAspect;
            var rows = GridLayoutMath.RowCount(_items.Count, _columns);
            ScrollView.contentContainer.style.height = GridLayoutMath.ContentHeight(rows, _cellHeight, _spacing);
            ResetBindings();
            UpdateVisible();
        }

        private void ResetBindings()
        {
            for (var slot = 0; slot < _cells.Count; slot++)
            {
                _bound[slot] = -1;
                _cells[slot].style.display = DisplayStyle.None;
            }
        }

        private void UpdateVisible()
        {
            if (_columns <= 0 || _cellHeight <= 0f) return;
            var rows = GridLayoutMath.RowCount(_items.Count, _columns);
            GridLayoutMath.VisibleRows(ScrollView.scrollOffset.y, ScrollView.contentViewport.layout.height, _cellHeight, _spacing,
                rows, 1, out var firstRow, out var lastRow);
            var first = firstRow * _columns;
            var last = Math.Min(_items.Count, lastRow * _columns);
            var needed = Math.Max(0, last - first);
            EnsurePool(needed);

            for (var slot = 0; slot < _cells.Count; slot++)
            {
                var index = _bound[slot];
                if (index < 0 || (index >= first && index < last)) continue;
                _bound[slot] = -1;
                _cells[slot].style.display = DisplayStyle.None;
            }

            for (var index = first; index < last; index++)
            {
                var slot = index % _cells.Count;
                if (_bound[slot] == index) continue;
                var cell = _cells[slot];
                _bound[slot] = index;
                var row = index / _columns;
                var column = index % _columns;
                cell.style.left = column * (_cellWidth + _spacing);
                cell.style.top = row * (_cellHeight + _spacing);
                cell.style.width = _cellWidth;
                cell.style.height = _cellHeight;
                cell.style.display = DisplayStyle.Flex;
                _bindCell(cell, _items[index], index);
            }
        }

        // A bigger pool changes which slot an index maps to, so every binding is dropped and the visible cells are rebound.
        private void EnsurePool(int needed)
        {
            if (_cells.Count >= needed || needed == 0) return;
            var target = needed + _columns;
            while (_cells.Count < target)
            {
                var cell = _makeCell();
                cell.style.position = Position.Absolute;
                cell.style.display = DisplayStyle.None;
                ScrollView.contentContainer.Add(cell);
                _cells.Add(cell);
                _bound.Add(-1);
            }

            ResetBindings();
        }
    }
}
