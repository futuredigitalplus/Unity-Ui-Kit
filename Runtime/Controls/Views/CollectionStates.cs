using UnityEngine.UIElements;

namespace FutureDigitalPlus.UIKit.Controls
{
    /// <summary>Ready-made "no items" content for <see cref="ListViewBinder{T}"/> and <see cref="VirtualGrid{T}"/>.</summary>
    public static class EmptyStates
    {
        /// <summary>Creates a design system empty state (<c>ds-empty</c>) with a title and a message.</summary>
        public static VisualElement Create(string title, string message = null)
        {
            var root = new VisualElement { name = "empty-state" };
            root.AddToClassList("ds-empty");
            var titleLabel = new Label(title) { pickingMode = PickingMode.Ignore };
            titleLabel.AddToClassList("ds-empty__title");
            root.Add(titleLabel);
            if (string.IsNullOrEmpty(message)) return root;
            var messageLabel = new Label(message) { pickingMode = PickingMode.Ignore };
            messageLabel.AddToClassList("ds-empty__message");
            root.Add(messageLabel);
            return root;
        }
    }

    /// <summary>The three mutually exclusive states of a data view: showing items, empty, or loading (skeleton rows).</summary>
    internal sealed class CollectionStates
    {
        private readonly VisualElement _content;
        private readonly VisualElement _emptyHost = new VisualElement { name = "empty-host" };
        private readonly VisualElement _skeleton = new VisualElement { name = "skeleton", pickingMode = PickingMode.Ignore };
        private int _skeletonRows;
        private float _skeletonRowHeight;
        private VisualElement _empty;

        public CollectionStates(VisualElement host, VisualElement content, int skeletonRows, float skeletonRowHeight)
        {
            _content = content;
            _skeletonRows = skeletonRows;
            _skeletonRowHeight = skeletonRowHeight;
            _emptyHost.AddToClassList("fdp-collection__empty");
            _skeleton.AddToClassList("fdp-collection__skeleton");
            _emptyHost.style.display = DisplayStyle.None;
            _skeleton.style.display = DisplayStyle.None;
            host.Add(_emptyHost);
            host.Add(_skeleton);
        }

        public bool IsLoading { get; private set; }

        public VisualElement EmptyState
        {
            get => _empty;
            set
            {
                _empty?.RemoveFromHierarchy();
                _empty = value;
                if (value != null) _emptyHost.Add(value);
            }
        }

        public void SetSkeleton(int rows, float rowHeight)
        {
            _skeletonRows = rows;
            _skeletonRowHeight = rowHeight;
            if (_skeleton.childCount > 0) BuildSkeleton();
        }

        public void Update(int itemCount, bool loading)
        {
            if (loading && !IsLoading) BuildSkeleton();
            IsLoading = loading;
            _skeleton.style.display = loading ? DisplayStyle.Flex : DisplayStyle.None;
            _emptyHost.style.display = !loading && itemCount == 0 && _empty != null ? DisplayStyle.Flex : DisplayStyle.None;
            _content.style.display = loading || itemCount == 0 && _empty != null ? DisplayStyle.None : DisplayStyle.Flex;
        }

        private void BuildSkeleton()
        {
            _skeleton.Clear();
            for (var i = 0; i < _skeletonRows; i++)
            {
                var row = new VisualElement { pickingMode = PickingMode.Ignore };
                row.AddToClassList("ds-skeleton");
                row.AddToClassList("fdp-collection__skeleton-row");
                row.style.height = _skeletonRowHeight;
                _skeleton.Add(row);
            }
        }
    }
}
