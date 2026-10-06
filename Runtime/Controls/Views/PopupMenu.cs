using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace FutureDigitalPlus.UIKit.Controls
{
    /// <summary>
    /// A popup menu with submenus, built on <see cref="MenuModel"/>. It opens at a pointer position or below an element,
    /// flips to stay inside the screen, takes focus (rows are focusable, the previous focus returns on close), is
    /// operated with arrows, Submit, Cancel, Home, End and type-ahead as well as the pointer, and closes on Cancel, on a
    /// click outside, or when an item is invoked. It lives on a layer on top of the panel's top element, so it is
    /// not clipped by scroll views. Use <see cref="Attach"/> to open it from a right click, a long press or a key.
    /// </summary>
    public sealed class PopupMenu
    {
        private readonly VisualElement _anchor;
        private readonly MenuModel _model;
        private readonly Action<MenuEntry> _onInvoke;
        private readonly List<VisualElement> _panels = new List<VisualElement>(3);
        private readonly List<IReadOnlyList<MenuEntry>> _sources = new List<IReadOnlyList<MenuEntry>>(3);
        private readonly Vector2? _point;
        private readonly bool _matchAnchorWidth;
        private VisualElement _layer;
        private PopupFocus _focus;
        private int _lastCancelFrame = -1;
        private bool _closed;

        private PopupMenu(VisualElement anchor, MenuModel model, Vector2? point, bool matchAnchorWidth, Action<MenuEntry> onInvoke)
        {
            _anchor = anchor;
            _model = model;
            _point = point;
            _matchAnchorWidth = matchAnchorWidth;
            _onInvoke = onInvoke;
        }

        /// <summary>Raised after an item was invoked and the menu closed.</summary>
        public event Action<MenuEntry> ItemInvoked;

        /// <summary>Raised when the menu closed, for any reason.</summary>
        public event Action Closed;

        /// <summary>True while the menu is on screen.</summary>
        public bool IsOpen => _layer != null && !_closed;

        /// <summary>The model that drives the menu (focus, open submenus).</summary>
        public MenuModel Model => _model;

        /// <summary>The root element of the menu layer (for tests and styling hooks); null once closed.</summary>
        public VisualElement Layer => _layer;

        /// <summary>Opens a menu with its corner at <paramref name="panelPosition"/> (panel coordinates, for example a pointer event's <c>position</c>).</summary>
        /// <param name="anchor">Any element of the panel the menu opens in; it gets focus back when the menu closes.</param>
        /// <param name="items">The root items.</param>
        /// <param name="panelPosition">Where the menu's top left corner goes, before it is moved to stay on screen.</param>
        /// <param name="onInvoke">Called with the invoked item, after the menu closed.</param>
        public static PopupMenu Show(VisualElement anchor, IReadOnlyList<MenuEntry> items, Vector2 panelPosition, Action<MenuEntry> onInvoke = null)
        {
            return Show(anchor, new MenuModel(items), panelPosition, onInvoke);
        }

        /// <summary>Opens a menu driven by <paramref name="model"/> (for example with a preset focus) at a pointer position.</summary>
        public static PopupMenu Show(VisualElement anchor, MenuModel model, Vector2 panelPosition, Action<MenuEntry> onInvoke = null)
        {
            var menu = new PopupMenu(anchor, model, panelPosition, false, onInvoke);
            return menu.Open() ? menu : null;
        }

        /// <summary>Opens a menu below <paramref name="anchor"/> (or above when there is no room), as wide as the anchor when <paramref name="matchAnchorWidth"/> is set.</summary>
        public static PopupMenu ShowBelow(VisualElement anchor, IReadOnlyList<MenuEntry> items, Action<MenuEntry> onInvoke = null,
            bool matchAnchorWidth = false)
        {
            return ShowBelow(anchor, new MenuModel(items), onInvoke, matchAnchorWidth);
        }

        /// <summary>Opens a menu driven by <paramref name="model"/> below <paramref name="anchor"/>.</summary>
        public static PopupMenu ShowBelow(VisualElement anchor, MenuModel model, Action<MenuEntry> onInvoke = null, bool matchAnchorWidth = false)
        {
            var menu = new PopupMenu(anchor, model, null, matchAnchorWidth, onInvoke);
            return menu.Open() ? menu : null;
        }

        /// <summary>
        /// Opens <paramref name="target"/>'s menu from a right click, a long press on touch, the keyboard menu key (or Shift+F10)
        /// and a gamepad button, according to <paramref name="triggers"/>. <paramref name="items"/> is called each time the menu opens.
        /// Dispose the result to stop listening.
        /// </summary>
        public static PopupMenuBinding Attach(VisualElement target, Func<IReadOnlyList<MenuEntry>> items, Action<MenuEntry> onInvoke,
            PopupMenuTriggers triggers = PopupMenuTriggers.Default)
        {
            return new PopupMenuBinding(target, items, onInvoke, triggers);
        }

        /// <summary>Closes the menu. Does nothing when it is already closed.</summary>
        public void Close()
        {
            CloseInternal(null);
        }

        /// <summary>Handles a back request (the Android back button, a back dispatcher): closes the open submenu, or the menu. Returns true when it did something.</summary>
        public bool HandleBack()
        {
            if (!IsOpen) return false;
            Apply(_model.Cancel());
            return true;
        }

        private bool Open()
        {
            var host = PopupHost.TopLevel(_anchor);
            if (host == null) return false;

            _layer = PopupHost.CreateLayer(host, "context-menu");
            _layer.AddToClassList("fdp-menu-layer");
            _layer.RegisterCallback<PointerDownEvent>(OnLayerPointerDown);
            _layer.RegisterCallback<NavigationMoveEvent>(OnMove);
            _layer.RegisterCallback<NavigationSubmitEvent>(OnSubmit);
            _layer.RegisterCallback<NavigationCancelEvent>(OnCancel);
            _layer.RegisterCallback<KeyDownEvent>(OnKeyDown);

            _focus = new PopupFocus(_layer);
            _focus.Begin();
            Sync();
            return true;
        }

        private void CloseInternal(MenuEntry invoked)
        {
            if (_closed || _layer == null) return;
            _closed = true;
            _focus.End();
            _layer.RemoveFromHierarchy();
            _layer = null;
            _panels.Clear();
            _sources.Clear();
            Closed?.Invoke();
            if (invoked == null) return;
            ItemInvoked?.Invoke(invoked);
            _onInvoke?.Invoke(invoked);
        }

        private void Apply(MenuStep step)
        {
            switch (step.Action)
            {
                case MenuAction.Invoked:
                    CloseInternal(step.Item);
                    break;
                case MenuAction.Closed:
                    CloseInternal(null);
                    break;
                case MenuAction.OpenedSubmenu:
                case MenuAction.ClosedSubmenu:
                    Sync();
                    break;
            }
        }

        // ---- events ----

        private void OnLayerPointerDown(PointerDownEvent evt)
        {
            if (evt.target == _layer) CloseInternal(null);
            evt.StopPropagation();
        }

        private void OnMove(NavigationMoveEvent evt)
        {
            if (_layer == null) return;
            var panel = _layer.panel;
            var rtl = PopupHost.IsRightToLeft(_layer);
            switch (evt.direction)
            {
                case NavigationMoveEvent.Direction.Up:
                case NavigationMoveEvent.Direction.Previous:
                    if (_model.MovePrevious()) Sync();
                    break;
                case NavigationMoveEvent.Direction.Down:
                case NavigationMoveEvent.Direction.Next:
                    if (_model.MoveNext()) Sync();
                    break;
                case NavigationMoveEvent.Direction.Right:
                    Apply(rtl ? _model.Back() : _model.OpenSubmenu());
                    break;
                case NavigationMoveEvent.Direction.Left:
                    Apply(rtl ? _model.OpenSubmenu() : _model.Back());
                    break;
            }

            Consume(evt, panel);
        }

        private void OnSubmit(NavigationSubmitEvent evt)
        {
            if (_layer == null) return;
            var panel = _layer.panel;
            Apply(_model.Activate());
            Consume(evt, panel);
        }

        private void OnCancel(NavigationCancelEvent evt)
        {
            if (_layer == null) return;
            var panel = _layer.panel;
            CancelOncePerFrame();
            Consume(evt, panel);
        }

        private void OnKeyDown(KeyDownEvent evt)
        {
            switch (evt.keyCode)
            {
                case KeyCode.Escape:
                    CancelOncePerFrame();
                    break;
                case KeyCode.Home:
                    if (_model.MoveFirst()) Sync();
                    break;
                case KeyCode.End:
                    if (_model.MoveLast()) Sync();
                    break;
                default:
                    if (evt.character != 0 && !evt.ctrlKey && !evt.commandKey && !evt.altKey
                        && _model.TypeAhead(evt.character, PopupHost.Now)) Sync();
                    return;
            }

            evt.StopPropagation();
        }

        // Stops the event here and keeps the panel's focus navigation from also acting on it.
        private static void Consume(EventBase evt, IPanel panel)
        {
            evt.StopPropagation();
            panel?.focusController.IgnoreEvent(evt);
        }

        // Escape raises both a KeyDown and (with the default input mapping) a NavigationCancel in the same frame.
        private void CancelOncePerFrame()
        {
            if (_lastCancelFrame == Time.frameCount) return;
            _lastCancelFrame = Time.frameCount;
            HandleBack();
        }

        // ---- view ----

        // Brings the panels in line with the model: one panel per open level, the focused row marked and focused.
        private void Sync()
        {
            if (!IsOpen) return;

            // drop panels of closed levels, and of levels that now show a different submenu
            var keep = 0;
            while (keep < _panels.Count && keep <= _model.Depth && ReferenceEquals(_sources[keep], _model.ItemsAt(keep))) keep++;
            for (var i = _panels.Count - 1; i >= keep; i--) RemovePanel(i);
            for (var depth = keep; depth <= _model.Depth; depth++)
            {
                _panels.Add(BuildPanel(depth));
                _sources.Add(_model.ItemsAt(depth));
            }

            VisualElement toFocus = null;
            for (var depth = 0; depth < _panels.Count; depth++)
            {
                var rows = (List<VisualElement>)_panels[depth].userData;
                var focusIndex = _model.FocusIndexAt(depth);
                var deepest = depth == _model.Depth;
                for (var i = 0; i < rows.Count; i++)
                {
                    var row = rows[i];
                    if (row == null) continue;
                    row.EnableInClassList("fdp-menu__item--active", i == focusIndex && deepest);
                    row.EnableInClassList("fdp-menu__item--open", i == focusIndex && !deepest);
                    if (i == focusIndex && deepest) toFocus = row;
                }
            }

            toFocus?.Focus();
        }

        private void RemovePanel(int index)
        {
            _panels[index].RemoveFromHierarchy();
            _panels.RemoveAt(index);
            _sources.RemoveAt(index);
        }

        private VisualElement BuildPanel(int depth)
        {
            var items = _model.ItemsAt(depth);
            var panel = new VisualElement { name = "menu-" + depth, pickingMode = PickingMode.Position };
            panel.AddToClassList("fdp-menu");
            panel.style.position = Position.Absolute;
            panel.style.visibility = Visibility.Hidden;
            var rows = new List<VisualElement>(items.Count);
            panel.userData = rows;

            for (var i = 0; i < items.Count; i++)
            {
                var item = items[i];
                if (item.IsSeparator)
                {
                    var separator = new VisualElement { pickingMode = PickingMode.Ignore };
                    separator.AddToClassList("fdp-menu__separator");
                    panel.Add(separator);
                    rows.Add(null);
                    continue;
                }

                var row = BuildRow(item, depth, i);
                panel.Add(row);
                rows.Add(row);
            }

            _layer.Add(panel);
            panel.RegisterCallback<GeometryChangedEvent>(OnPanelGeometry);
            return panel;
        }

        private VisualElement BuildRow(MenuEntry item, int depth, int index)
        {
            var row = new VisualElement { name = "item", focusable = item.IsFocusable };
            row.AddToClassList("fdp-menu__item");
            row.EnableInClassList("fdp-menu__item--disabled", !item.Enabled);

            var check = new VisualElement { pickingMode = PickingMode.Ignore };
            check.AddToClassList("fdp-menu__check");
            check.EnableInClassList("fdp-menu__check--on", item.Checked);
            row.Add(check);

            var label = new Label(item.Label) { pickingMode = PickingMode.Ignore };
            label.AddToClassList("fdp-menu__label");
            row.Add(label);

            if (!string.IsNullOrEmpty(item.Hint))
            {
                var hint = new Label(item.Hint) { pickingMode = PickingMode.Ignore };
                hint.AddToClassList("fdp-menu__hint");
                row.Add(hint);
            }

            if (item.HasSubmenu)
            {
                var arrow = new Label(">") { pickingMode = PickingMode.Ignore };
                arrow.AddToClassList("fdp-menu__arrow");
                row.Add(arrow);
            }

            if (!item.IsFocusable) return row;
            row.RegisterCallback<PointerEnterEvent>(_ => OnRowHover(depth, index));
            row.RegisterCallback<ClickEvent>(_ => OnRowClick(depth, index));
            return row;
        }

        private void OnRowHover(int depth, int index)
        {
            if (!IsOpen) return;
            var changed = false;
            while (_model.Depth > depth)
            {
                _model.Back();
                changed = true;
            }

            if (_model.FocusItem(index)) changed = true;
            if (_model.Focused != null && _model.Focused.HasSubmenu && _model.Depth == depth)
            {
                _model.OpenSubmenu();
                changed = true;
            }

            if (changed) Sync();
        }

        private void OnRowClick(int depth, int index)
        {
            if (!IsOpen) return;
            while (_model.Depth > depth) _model.Back();
            _model.FocusItem(index);
            Apply(_model.Activate());
        }

        private void OnPanelGeometry(GeometryChangedEvent evt)
        {
            var panel = (VisualElement)evt.target;
            if (_layer == null || evt.newRect.width <= 0f || evt.newRect.height <= 0f) return;
            var container = _layer.layout.size;
            if (container.x <= 0f || container.y <= 0f) return;

            var depth = _panels.IndexOf(panel);
            if (depth < 0) return;
            panel.UnregisterCallback<GeometryChangedEvent>(OnPanelGeometry);

            var size = evt.newRect.size;
            PopupPlacementResult placed;
            if (depth > 0)
            {
                var parentRows = (List<VisualElement>)_panels[depth - 1].userData;
                var parentRow = parentRows[_model.FocusIndexAt(depth - 1)];
                var rect = _layer.WorldToLocal(parentRow.worldBound);
                placed = PopupPlacement.BesideRect(rect.x, rect.y, rect.width, rect.height, size.x, size.y, container.x,
                    container.y, PopupSide.Right);
            }
            else if (_point.HasValue)
            {
                var p = _layer.WorldToLocal(_point.Value);
                placed = PopupPlacement.AtPoint(p.x, p.y, size.x, size.y, container.x, container.y);
            }
            else
            {
                var rect = _layer.WorldToLocal(_anchor.worldBound);
                if (_matchAnchorWidth && Mathf.Abs(panel.resolvedStyle.minWidth.value - rect.width) > 0.5f)
                {
                    // widen to the anchor, then place on the next layout pass
                    panel.style.minWidth = rect.width;
                    panel.RegisterCallback<GeometryChangedEvent>(OnPanelGeometry);
                    return;
                }

                placed = PopupPlacement.BesideRect(rect.x, rect.y, rect.width, rect.height, size.x, size.y, container.x,
                    container.y, PopupSide.Below);
            }

            panel.style.left = placed.X;
            panel.style.top = placed.Y;
            panel.style.visibility = Visibility.Visible;
        }
    }
}
