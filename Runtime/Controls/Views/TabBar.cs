using System;
using System.Collections.Generic;
using System.Globalization;
using UnityEngine;
using UnityEngine.UIElements;

namespace FutureDigitalPlus.UIKit.Controls
{
    /// <summary>The look of a <see cref="TabBar"/>.</summary>
    public enum TabBarSkin
    {
        /// <summary>The design system tab strip (<c>ds-tabs</c> / <c>ds-tab</c>).</summary>
        Tabs,

        /// <summary>A segmented control: one rounded track with a highlighted segment.</summary>
        Segmented,
    }

    /// <summary>
    /// One selection controller with two skins. Items have an id and a label, can be disabled and can carry a badge
    /// (a dot or a count, set directly or bound to a <see cref="BadgeTree"/>). Exactly one item is selected; the
    /// selection is the item id in <see cref="value"/>, and a change raises a <c>ChangeEvent&lt;string&gt;</c>. A pointer
    /// click, Submit, and the left and right arrows (swapped in right-to-left layouts; Home and End jump to the ends)
    /// all select, and focus follows the selection so the bar works with a keyboard or a gamepad alone.
    /// </summary>
    [UxmlElement]
    public partial class TabBar : VisualElement, INotifyValueChanged<string>
    {
        /// <summary>USS class of the element.</summary>
        public const string UssClassName = "fdp-tab-bar";

        private sealed class Tab
        {
            public string Id;
            public string Label;
            public bool Enabled = true;
            public Button Button;
            public Label BadgeLabel;
        }

        private readonly List<Tab> _tabs = new List<Tab>();
        private TabBarSkin _skin = TabBarSkin.Tabs;
        private string _value;
        private string _itemsSpec = string.Empty;
        private string _pendingValue;
        private BadgeTree _badges;
        private string _badgePrefix = string.Empty;

        /// <summary>Creates an empty tab bar in the <see cref="TabBarSkin.Tabs"/> skin.</summary>
        public TabBar()
        {
            AddToClassList(UssClassName);
            ApplySkin();
            RegisterCallback<AttachToPanelEvent>(OnAttach);
            RegisterCallback<DetachFromPanelEvent>(OnDetach);
        }

        /// <summary>The look of the bar.</summary>
        [UxmlAttribute]
        public TabBarSkin Skin
        {
            get => _skin;
            set
            {
                _skin = value;
                ApplySkin();
            }
        }

        /// <summary>
        /// The items as one string for UXML: <c>id:Label,id:Label</c> (a label may not contain a comma; an item without a colon uses
        /// the id as its label). Setting it replaces all items; code can use <see cref="AddTab"/> instead.
        /// </summary>
        [UxmlAttribute]
        public string Items
        {
            get => _itemsSpec;
            set
            {
                _itemsSpec = value ?? string.Empty;
                // A value assigned before the items (an object initializer, UXML in another order) waits for its item; replacing the items must not lose it.
                var pending = _pendingValue;
                ClearTabs();
                _pendingValue = pending;
                foreach (var part in _itemsSpec.Split(','))
                {
                    var spec = part.Trim();
                    if (spec.Length == 0) continue;
                    var colon = spec.IndexOf(':');
                    if (colon < 0) AddTab(spec, spec);
                    else AddTab(spec.Substring(0, colon).Trim(), spec.Substring(colon + 1).Trim());
                }
            }
        }

        /// <summary>Number of items.</summary>
        public int Count => _tabs.Count;

        /// <summary>
        /// The id of the selected item; null or empty when none is selected. Setting it raises a change event. An id that is not
        /// an item yet is remembered and selected when the item is added (so UXML attributes work in any order).
        /// </summary>
        [UxmlAttribute]
        // ReSharper disable once InconsistentNaming
        public string value
        {
            get => _value;
            set
            {
                if (string.Equals(_value, value, StringComparison.Ordinal)) return;
                if (!string.IsNullOrEmpty(value) && IndexOf(value) < 0)
                {
                    _pendingValue = value;
                    return;
                }

                var previous = _value;
                if (panel != null)
                {
                    using (var evt = ChangeEvent<string>.GetPooled(previous, value))
                    {
                        evt.target = this;
                        SetValueWithoutNotify(value);
                        SendEvent(evt);
                    }
                }
                else
                {
                    SetValueWithoutNotify(value);
                }
            }
        }

        /// <summary>Selects an item without raising a change event.</summary>
        public void SetValueWithoutNotify(string newValue)
        {
            _value = newValue;
            RefreshSelection();
        }

        /// <summary>The item ids in order.</summary>
        public IEnumerable<string> Ids
        {
            get
            {
                foreach (var tab in _tabs) yield return tab.Id;
            }
        }

        /// <summary>Adds an item. The first enabled item added becomes selected when nothing is selected.</summary>
        public void AddTab(string id, string label, bool enabled = true)
        {
            if (string.IsNullOrEmpty(id)) throw new ArgumentException("A tab needs an id.", nameof(id));
            if (IndexOf(id) >= 0) throw new ArgumentException($"A tab with the id '{id}' exists already.", nameof(id));

            var button = new Button { name = "tab-" + id, text = label, focusable = true };
            button.AddToClassList(UssClassName + "__item");
            var badge = new Label { pickingMode = PickingMode.Ignore };
            badge.AddToClassList(UssClassName + "__badge");
            badge.style.display = DisplayStyle.None;
            button.Add(badge);

            var tab = new Tab { Id = id, Label = label, Enabled = enabled, Button = button, BadgeLabel = badge };
            button.clicked += () => Select(tab);
            button.RegisterCallback<NavigationMoveEvent>(evt => OnMove(tab, evt));
            button.RegisterCallback<KeyDownEvent>(evt => OnKeyDown(tab, evt));
            _tabs.Add(tab);
            Add(button);
            ApplySkinTo(tab);
            button.SetEnabled(enabled);

            if (string.Equals(_pendingValue, id, StringComparison.Ordinal))
            {
                _pendingValue = null;
                SetValueWithoutNotify(id);
            }
            else if (string.IsNullOrEmpty(_value) && enabled) SetValueWithoutNotify(id);
            else RefreshSelection();
            RefreshBadge(tab);
        }

        /// <summary>Removes an item. When it was selected, the first enabled item becomes selected (with a change event).</summary>
        public bool RemoveTab(string id)
        {
            var index = IndexOf(id);
            if (index < 0) return false;
            var tab = _tabs[index];
            _tabs.RemoveAt(index);
            tab.Button.RemoveFromHierarchy();
            if (string.Equals(_value, id, StringComparison.Ordinal))
            {
                var next = FirstEnabled();
                if (next != null) value = next.Id;
                else SetValueWithoutNotify(null);
            }

            return true;
        }

        /// <summary>Removes every item and clears the selection (no change event).</summary>
        public void ClearTabs()
        {
            foreach (var tab in _tabs) tab.Button.RemoveFromHierarchy();
            _tabs.Clear();
            _value = null;
            _pendingValue = null;
        }

        /// <summary>Enables or disables an item. A disabled item cannot be selected or focused.</summary>
        public void SetTabEnabled(string id, bool enabled)
        {
            var tab = Find(id);
            if (tab == null) return;
            tab.Enabled = enabled;
            tab.Button.SetEnabled(enabled);
        }

        /// <summary>Changes the label of an item.</summary>
        public void SetTabLabel(string id, string label)
        {
            var tab = Find(id);
            if (tab == null) return;
            tab.Label = label;
            tab.Button.text = label;
        }

        /// <summary>Sets the badge of an item directly. Ignored while the bar is bound to a <see cref="BadgeTree"/>.</summary>
        public void SetBadge(string id, BadgeValue badge)
        {
            var tab = Find(id);
            if (tab != null) ShowBadge(tab, badge);
        }

        /// <summary>
        /// Shows the badges of a <see cref="BadgeTree"/>: the item with id <c>x</c> shows the tree's value for the path
        /// <c>prefix/x</c> (or just <c>x</c> without a prefix), including everything below it. Visiting a place in the tree clears its badge.
        /// </summary>
        public void BindBadges(BadgeTree tree, string pathPrefix = "")
        {
            Unsubscribe();
            _badges = tree;
            _badgePrefix = pathPrefix ?? string.Empty;
            if (panel != null) Subscribe();
            RefreshBadges();
        }

        /// <summary>Stops following the badge tree and hides all badges.</summary>
        public void UnbindBadges()
        {
            Unsubscribe();
            _badges = null;
            foreach (var tab in _tabs) ShowBadge(tab, default);
        }

        private int IndexOf(string id)
        {
            for (var i = 0; i < _tabs.Count; i++)
                if (string.Equals(_tabs[i].Id, id, StringComparison.Ordinal)) return i;
            return -1;
        }

        private Tab Find(string id)
        {
            var index = IndexOf(id);
            return index < 0 ? null : _tabs[index];
        }

        private Tab FirstEnabled()
        {
            foreach (var tab in _tabs)
                if (tab.Enabled) return tab;
            return null;
        }

        private void Select(Tab tab)
        {
            if (!tab.Enabled) return;
            value = tab.Id;
            tab.Button.Focus();
            if (_badges != null) _badges.Visit(BadgePath(tab));
        }

        private void OnMove(Tab from, NavigationMoveEvent evt)
        {
            int step;
            var rtl = PopupHost.IsRightToLeft(this);
            switch (evt.direction)
            {
                case NavigationMoveEvent.Direction.Left: step = rtl ? 1 : -1; break;
                case NavigationMoveEvent.Direction.Right: step = rtl ? -1 : 1; break;
                default: return;
            }

            var target = Neighbour(_tabs.IndexOf(from), step);
            if (target == null) return;
            evt.StopPropagation();
            panel?.focusController.IgnoreEvent(evt);
            Select(target);
        }

        private void OnKeyDown(Tab from, KeyDownEvent evt)
        {
            Tab target;
            switch (evt.keyCode)
            {
                case KeyCode.Home: target = Neighbour(-1, 1); break;
                case KeyCode.End: target = Neighbour(_tabs.Count, -1); break;
                default: return;
            }

            if (target == null) return;
            evt.StopPropagation();
            Select(target);
        }

        // The next enabled item from index in a direction, or null.
        private Tab Neighbour(int index, int step)
        {
            for (var i = index + step; i >= 0 && i < _tabs.Count; i += step)
                if (_tabs[i].Enabled) return _tabs[i];
            return null;
        }

        private void RefreshSelection()
        {
            foreach (var tab in _tabs)
            {
                var selected = string.Equals(tab.Id, _value, StringComparison.Ordinal);
                tab.Button.EnableInClassList("is-active", selected && _skin == TabBarSkin.Tabs);
                tab.Button.EnableInClassList(UssClassName + "__item--selected", selected);
            }
        }

        private void ApplySkin()
        {
            EnableInClassList("ds-tabs", _skin == TabBarSkin.Tabs);
            EnableInClassList(UssClassName + "--segmented", _skin == TabBarSkin.Segmented);
            foreach (var tab in _tabs) ApplySkinTo(tab);
            RefreshSelection();
        }

        private void ApplySkinTo(Tab tab)
        {
            tab.Button.EnableInClassList("ds-tab", _skin == TabBarSkin.Tabs);
            tab.Button.EnableInClassList(UssClassName + "__segment", _skin == TabBarSkin.Segmented);
        }

        // ---- badges ----

        private string BadgePath(Tab tab) => _badgePrefix.Length == 0 ? tab.Id : _badgePrefix.TrimEnd(BadgeTree.Separator) + BadgeTree.Separator + tab.Id;

        private void RefreshBadges()
        {
            foreach (var tab in _tabs) RefreshBadge(tab);
        }

        private void RefreshBadge(Tab tab)
        {
            if (_badges != null) ShowBadge(tab, _badges.Get(BadgePath(tab)));
        }

        private void ShowBadge(Tab tab, BadgeValue badge)
        {
            var label = tab.BadgeLabel;
            if (!badge.IsVisible)
            {
                label.style.display = DisplayStyle.None;
                return;
            }

            label.style.display = DisplayStyle.Flex;
            label.EnableInClassList(UssClassName + "__badge--dot", badge.Count <= 0);
            label.text = badge.Count <= 0 ? string.Empty : badge.Count > 99 ? "99+" : badge.Count.ToString(CultureInfo.InvariantCulture);
        }

        private void OnBadgeChanged(string path)
        {
            foreach (var tab in _tabs)
                if (string.Equals(BadgePath(tab), path, StringComparison.Ordinal)) RefreshBadge(tab);
        }

        private void Subscribe()
        {
            if (_badges != null) _badges.Changed += OnBadgeChanged;
        }

        private void Unsubscribe()
        {
            if (_badges != null) _badges.Changed -= OnBadgeChanged;
        }

        private void OnAttach(AttachToPanelEvent evt)
        {
            Unsubscribe();
            Subscribe();
            RefreshBadges();
        }

        private void OnDetach(DetachFromPanelEvent evt) => Unsubscribe();
    }

    /// <summary>
    /// A <see cref="TabBar"/> in the segmented skin, a drop-in name for code that thinks in segmented controls (a
    /// settings page with "Easy / Normal / Hard", a "Daily / Weekly / All time" switch).
    /// </summary>
    [UxmlElement]
    public partial class SegmentedControl : TabBar
    {
        /// <summary>Creates an empty segmented control.</summary>
        public SegmentedControl()
        {
            Skin = TabBarSkin.Segmented;
        }
    }
}
