using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace FutureDigitalPlus.UIKit.Gameplay
{
    /// <summary>What a drop or a keyboard placement tried to do: where from, where to, and how it went.</summary>
    public readonly struct SlotMove
    {
        /// <summary>Creates a description of a move.</summary>
        public SlotMove(InventoryModel source, int sourceSlot, InventoryModel target, int targetSlot, MoveResult result)
        {
            Source = source;
            SourceSlot = sourceSlot;
            Target = target;
            TargetSlot = targetSlot;
            Result = result;
        }

        /// <summary>The inventory the items came from.</summary>
        public InventoryModel Source { get; }

        /// <summary>The slot the items came from.</summary>
        public int SourceSlot { get; }

        /// <summary>The inventory the items were put into.</summary>
        public InventoryModel Target { get; }

        /// <summary>The slot the items were put into.</summary>
        public int TargetSlot { get; }

        /// <summary>The outcome, with the reason when it failed.</summary>
        public MoveResult Result { get; }
    }

    /// <summary>
    /// A grid of <see cref="ItemSlot"/>s that shows an <see cref="InventoryModel"/> and lets the player rearrange it.
    /// With a pointer, an item is dragged by its slot: a ghost follows the pointer, the slot under it lights up, and
    /// letting go puts the items there (or does nothing when the drop is refused or lands outside any slot). With a
    /// keyboard or gamepad, the arrow keys move focus, Submit picks up the stack in the focused slot and Submit on
    /// another slot puts it down, Cancel puts it back. Holding Shift while picking up takes half the stack, Ctrl or
    /// Command takes one item. Several grids can be linked with <see cref="Link"/> so that items move between them.
    /// The grid reports what happens through events and does not know about tooltips, menus or what an item does;
    /// a game connects those to <see cref="SlotHovered"/>, <see cref="SlotFocused"/>, <see cref="SlotActivated"/> and
    /// <see cref="SlotContext"/>. The slots wrap by themselves; <see cref="Columns"/> fixes the number of columns.
    /// </summary>
    public sealed class SlotGrid : VisualElement
    {
        /// <summary>USS class of the element.</summary>
        public const string UssClassName = "fdp-slot-grid";

        private const string HeldClass = ItemSlot.UssClassName + "--held";
        private const string DropClass = ItemSlot.UssClassName + "--drop";

        private readonly List<ItemSlot> _slots = new List<ItemSlot>();
        private readonly Action _onLongPress;
        private InventoryModel _model;
        private Func<string, ItemVisual> _visualOf;
        private PickupMode _pickup = new PickupMode();
        private bool _subscribed;
        private float _slotSize = 56f;
        private float _spacing = 4f;
        private int _columns;
        private bool _widthFromColumns;
        private float _dragThreshold = 6f;
        private float _longPressSeconds = 0.5f;

        // press and drag state
        private ItemSlot _pressSlot;
        private int _pressPointer = -1;
        private Vector2 _pressPosition;
        private int _pressCount;
        private bool _dragging;
        private ItemSlot _dropSlot;
        private IVisualElementScheduledItem _longPress;
        private VisualElement _ghost;
        private VisualElement _ghostIcon;
        private Label _ghostCount;
        private VisualElement _ghostHost;
        private Vector2 _ghostHalfSize;

        /// <summary>Creates a grid with no inventory. Call <see cref="Bind"/>.</summary>
        public SlotGrid()
        {
            AddToClassList(UssClassName);
            _onLongPress = OnLongPress;
            RegisterCallback<NavigationMoveEvent>(OnNavigationMove);
            RegisterCallback<NavigationCancelEvent>(OnNavigationCancel);
            RegisterCallback<KeyDownEvent>(OnKeyDown);
            RegisterCallback<AttachToPanelEvent>(OnAttach);
            RegisterCallback<DetachFromPanelEvent>(OnDetach);
        }

        /// <summary>Raised when the pointer enters a slot.</summary>
        public event Action<ItemSlot> SlotHovered;

        /// <summary>Raised when a slot gets keyboard or gamepad focus.</summary>
        public event Action<ItemSlot> SlotFocused;

        /// <summary>
        /// Raised when a slot is activated: a pointer click that did not drag, or Submit on a slot that has no item to pick up
        /// (always, when <see cref="SubmitPicksUp"/> is false).
        /// </summary>
        public event Action<ItemSlot> SlotActivated;

        /// <summary>Raised when the player asks for the context of a slot (right click, long press, the gamepad Y button or the menu key) with the panel position to open a menu at.</summary>
        public event Action<ItemSlot, Vector2> SlotContext;

        /// <summary>Raised on the grid whose slot received the items after a successful move, by drag or by keyboard.</summary>
        public event Action<SlotMove> Moved;

        /// <summary>Raised on the grid whose slot was the target of a refused move (the reason is in the result).</summary>
        public event Action<SlotMove> MoveRejected;

        /// <summary>The inventory shown, or null before <see cref="Bind"/>.</summary>
        public InventoryModel Model => _model;

        /// <summary>The slots, by inventory slot number.</summary>
        public IReadOnlyList<ItemSlot> Slots => _slots;

        /// <summary>The state of picking up and carrying. Share one instance between grids (see <see cref="Link"/>) to carry items from one to the other.</summary>
        public PickupMode Pickup
        {
            get => _pickup;
            set
            {
                value = value ?? new PickupMode();
                if (ReferenceEquals(value, _pickup)) return;
                if (_subscribed) _pickup.Changed -= UpdateHeld;
                _pickup = value;
                if (_subscribed) _pickup.Changed += UpdateHeld;
                UpdateHeld();
            }
        }

        /// <summary>True (default) when Submit on a slot with an item picks it up. Set false when Submit should only raise <see cref="SlotActivated"/> (use the item) and picking up is done by dragging.</summary>
        public bool SubmitPicksUp { get; set; } = true;

        /// <summary>The key that raises <see cref="SlotContext"/> for the focused slot (default the gamepad Y button); the menu key always does.</summary>
        public KeyCode ContextKey { get; set; } = KeyCode.JoystickButton3;

        /// <summary>The side of a slot in panel units (default 56).</summary>
        public float SlotSize
        {
            get => _slotSize;
            set
            {
                _slotSize = Mathf.Max(1f, value);
                ApplySizing();
            }
        }

        /// <summary>The gap between slots (default 4).</summary>
        public float Spacing
        {
            get => _spacing;
            set
            {
                _spacing = Mathf.Max(0f, value);
                ApplySizing();
            }
        }

        /// <summary>The number of columns; 0 (default) lets the slots wrap to the width of the grid.</summary>
        public int Columns
        {
            get => _columns;
            set
            {
                _columns = Mathf.Max(0, value);
                ApplySizing();
            }
        }

        /// <summary>How far the pointer must move after pressing before a drag starts (default 6 panel units).</summary>
        public float DragThreshold
        {
            get => _dragThreshold;
            set => _dragThreshold = Mathf.Max(0f, value);
        }

        /// <summary>How long a touch or pen press without movement takes to raise <see cref="SlotContext"/> (default 0.5 s); 0 turns the long press off. A mouse press never counts as a long press.</summary>
        public float LongPressSeconds
        {
            get => _longPressSeconds;
            set => _longPressSeconds = Mathf.Max(0f, value);
        }

        /// <summary>The number of columns the slots are laid out in right now (the fixed <see cref="Columns"/> or the number measured from the layout).</summary>
        public int ColumnCount
        {
            get
            {
                if (_columns > 0) return _columns;
                if (_slots.Count == 0) return 1;
                var y = _slots[0].layout.y;
                var n = 0;
                while (n < _slots.Count && Mathf.Abs(_slots[n].layout.y - y) < 0.5f) n++;
                return Mathf.Max(1, n);
            }
        }

        /// <summary>Makes the grids share one <see cref="PickupMode"/> so an item picked up in one can be put down in another.</summary>
        public static void Link(params SlotGrid[] grids)
        {
            if (grids == null || grids.Length == 0) return;
            var shared = grids[0]._pickup;
            for (var i = 1; i < grids.Length; i++) grids[i].Pickup = shared;
        }

        /// <summary>Shows an inventory: one slot per inventory slot, kept up to date while the grid is on a panel.</summary>
        /// <param name="model">The inventory.</param>
        /// <param name="visualOf">Gives the look of an item id (icon, rarity). Null shows slots without icons.</param>
        public void Bind(InventoryModel model, Func<string, ItemVisual> visualOf)
        {
            if (model == null) throw new ArgumentNullException(nameof(model));
            Unbind();
            _model = model;
            _visualOf = visualOf;
            for (var i = 0; i < model.Capacity; i++)
            {
                var slot = CreateSlot(i);
                _slots.Add(slot);
                Add(slot);
            }

            ApplySizing();
            if (panel != null) Subscribe();
            RefreshAll();
        }

        /// <summary>Removes the slots and stops listening to the inventory.</summary>
        public void Unbind()
        {
            AbortPress(cancelPickup: true);
            Unsubscribe();
            foreach (var slot in _slots)
            {
                UnregisterSlot(slot);
                slot.RemoveFromHierarchy();
            }

            _slots.Clear();
            _model = null;
        }

        /// <summary>Redraws every slot from the inventory (the grid does this by itself when the inventory raises changes).</summary>
        public void RefreshAll()
        {
            if (_model == null) return;
            for (var i = 0; i < _slots.Count; i++) RefreshSlot(i);
        }

        /// <summary>Gives keyboard focus to a slot. Returns false when the number is outside the grid.</summary>
        public bool FocusSlot(int index)
        {
            if (index < 0 || index >= _slots.Count) return false;
            _slots[index].Focus();
            return true;
        }

        // ---- slots ----

        private ItemSlot CreateSlot(int index)
        {
            var slot = new ItemSlot { name = "slot-" + index, Grid = this, Index = index };
            slot.RegisterCallback<PointerDownEvent>(OnPointerDown);
            slot.RegisterCallback<PointerMoveEvent>(OnPointerMove);
            slot.RegisterCallback<PointerUpEvent>(OnPointerUp);
            slot.RegisterCallback<PointerCaptureOutEvent>(OnPointerCaptureOut);
            slot.RegisterCallback<PointerEnterEvent>(OnPointerEnter);
            slot.RegisterCallback<FocusInEvent>(OnFocusIn);
            slot.RegisterCallback<NavigationSubmitEvent>(OnSubmit);
            return slot;
        }

        private void UnregisterSlot(ItemSlot slot)
        {
            slot.UnregisterCallback<PointerDownEvent>(OnPointerDown);
            slot.UnregisterCallback<PointerMoveEvent>(OnPointerMove);
            slot.UnregisterCallback<PointerUpEvent>(OnPointerUp);
            slot.UnregisterCallback<PointerCaptureOutEvent>(OnPointerCaptureOut);
            slot.UnregisterCallback<PointerEnterEvent>(OnPointerEnter);
            slot.UnregisterCallback<FocusInEvent>(OnFocusIn);
            slot.UnregisterCallback<NavigationSubmitEvent>(OnSubmit);
            slot.Grid = null;
            slot.Index = -1;
        }

        private void ApplySizing()
        {
            foreach (var slot in _slots)
            {
                slot.style.width = _slotSize;
                slot.style.height = _slotSize;
                slot.style.marginRight = _spacing;
                slot.style.marginBottom = _spacing;
            }

            if (_columns > 0)
            {
                style.width = _columns * (_slotSize + _spacing);
                _widthFromColumns = true;
            }
            else if (_widthFromColumns)
            {
                style.width = StyleKeyword.Null; // give the width back to the game's own styling
                _widthFromColumns = false;
            }
        }

        private void RefreshSlot(int index)
        {
            var slot = _slots[index];
            var stack = _model[index];
            if (stack.IsEmpty) slot.SetEmpty();
            else slot.SetItem(_visualOf != null ? _visualOf(stack.ItemId) : default, stack.Count);
            slot.IsLocked = _model.IsLocked(index);
            slot.EnableInClassList(HeldClass, IsHeld(index));
        }

        private bool IsHeld(int index) => _pickup.IsHolding && ReferenceEquals(_pickup.Source, _model) && _pickup.SourceSlot == index;

        private void UpdateHeld()
        {
            for (var i = 0; i < _slots.Count; i++) _slots[i].EnableInClassList(HeldClass, IsHeld(i));
        }

        private void OnSlotChanged(int index)
        {
            if (index >= 0 && index < _slots.Count) RefreshSlot(index);
        }

        private void Subscribe()
        {
            if (_subscribed || _model == null) return;
            _subscribed = true;
            _model.SlotChanged += OnSlotChanged;
            _pickup.Changed += UpdateHeld;
        }

        private void Unsubscribe()
        {
            if (!_subscribed) return;
            _subscribed = false;
            if (_model != null) _model.SlotChanged -= OnSlotChanged;
            _pickup.Changed -= UpdateHeld;
        }

        private void OnAttach(AttachToPanelEvent evt)
        {
            Subscribe();
            RefreshAll();
        }

        private void OnDetach(DetachFromPanelEvent evt)
        {
            AbortPress(cancelPickup: true);
            Unsubscribe();
        }

        // ---- pointer ----

        private void OnPointerEnter(PointerEnterEvent evt) => SlotHovered?.Invoke((ItemSlot)evt.currentTarget);

        private void OnFocusIn(FocusInEvent evt) => SlotFocused?.Invoke((ItemSlot)evt.currentTarget);

        private void OnPointerDown(PointerDownEvent evt)
        {
            var slot = (ItemSlot)evt.currentTarget;
            if (evt.button == 1)
            {
                SlotContext?.Invoke(slot, evt.position);
                evt.StopPropagation();
                return;
            }

            if (evt.button != 0 || _pressPointer >= 0) return;
            _pressSlot = slot;
            _pressPointer = evt.pointerId;
            _pressPosition = evt.position;
            _pressCount = CountFor(evt.shiftKey, evt.ctrlKey || evt.commandKey);
            _dragging = false;
            slot.CapturePointer(evt.pointerId);
            if (slot.HasItem && _longPressSeconds > 0f && evt.pointerType != UnityEngine.UIElements.PointerType.mouse)
                _longPress = slot.schedule.Execute(_onLongPress).StartingIn((long)(_longPressSeconds * 1000f));
        }

        private void OnPointerMove(PointerMoveEvent evt)
        {
            if (evt.pointerId != _pressPointer) return;
            var position = (Vector2)evt.position;
            if (!_dragging)
            {
                if ((position - _pressPosition).sqrMagnitude < _dragThreshold * _dragThreshold) return;
                if (!BeginDrag())
                {
                    AbortPress(cancelPickup: false);
                    return;
                }
            }

            UpdateDrag(position);
            evt.StopPropagation();
        }

        // What every pointer move of a drag does: the ghost follows, the slot under the pointer lights up. No allocation while the pointer stays in one slot (entering another changes a USS class, which UI Toolkit allocates for).
        internal void UpdateDrag(Vector2 panelPosition)
        {
            MoveGhost(panelPosition);
            SetDropSlot(SlotAt(panelPosition));
        }

        private void OnPointerUp(PointerUpEvent evt)
        {
            if (evt.pointerId != _pressPointer) return;
            var slot = _pressSlot;
            var wasDragging = _dragging;
            var position = (Vector2)evt.position;
            EndPress();
            if (wasDragging) Drop(position);
            else if (evt.button == 0 && slot != null) ClickSlot(slot);
        }

        private void OnPointerCaptureOut(PointerCaptureOutEvent evt)
        {
            if (evt.pointerId != _pressPointer) return;
            AbortPress(cancelPickup: true); // capture was taken away: nothing was dropped, so nothing moves
        }

        private void OnLongPress()
        {
            var slot = _pressSlot;
            if (slot == null || _dragging) return;
            var position = _pressPosition;
            EndPress();
            SlotContext?.Invoke(slot, position);
        }

        private bool BeginDrag()
        {
            var slot = _pressSlot;
            if (slot == null || !slot.HasItem || _pickup.IsHolding) return false;
            var picked = _pressCount < 0 ? _pickup.PickHalf(_model, slot.Index) : _pickup.Pick(_model, slot.Index, _pressCount);
            if (!picked) return false;
            _dragging = true;
            ShowGhost(slot);
            return true;
        }

        private void Drop(Vector2 position)
        {
            var target = SlotAt(position);
            if (target == null || target.Grid == null)
            {
                _pickup.Cancel();
                return;
            }

            var move = Place(target);
            if (move.HasValue && !move.Value.Result.Success) _pickup.Cancel(); // a drag never keeps carrying
        }

        private void ClickSlot(ItemSlot slot)
        {
            if (_pickup.IsHolding) Place(slot);
            else SlotActivated?.Invoke(slot);
        }

        // Puts the carried items into a slot and reports it; null when the placement was a cancel or nothing was carried.
        private SlotMove? Place(ItemSlot target)
        {
            var source = _pickup.Source;
            var from = _pickup.SourceSlot;
            var result = _pickup.Place(target.Grid._model, target.Index);
            if (result.Reason == MoveReason.SameSlot || result.Reason == MoveReason.NotHolding) return null;
            var move = new SlotMove(source, from, target.Grid._model, target.Index, result);
            if (result.Success) target.Grid.Moved?.Invoke(move);
            else target.Grid.MoveRejected?.Invoke(move);
            return move;
        }

        private void EndPress()
        {
            _longPress?.Pause();
            _longPress = null;
            var slot = _pressSlot;
            var pointer = _pressPointer;
            _pressSlot = null;
            _pressPointer = -1;
            if (_dragging)
            {
                _dragging = false;
                HideGhost();
                SetDropSlot(null);
            }

            if (slot != null && pointer >= 0 && slot.HasPointerCapture(pointer)) slot.ReleasePointer(pointer);
        }

        private void AbortPress(bool cancelPickup)
        {
            var wasDragging = _dragging;
            EndPress();
            if (wasDragging && cancelPickup) _pickup.Cancel();
        }

        private static int CountFor(bool half, bool one) => half ? -1 : one ? 1 : 0;

        // ---- drop target and ghost ----

        private ItemSlot SlotAt(Vector2 panelPosition)
        {
            var picked = panel?.Pick(panelPosition);
            while (picked != null && !(picked is ItemSlot)) picked = picked.hierarchy.parent;
            return picked as ItemSlot;
        }

        private void SetDropSlot(ItemSlot slot)
        {
            if (ReferenceEquals(slot, _dropSlot)) return;
            _dropSlot?.RemoveFromClassList(DropClass);
            _dropSlot = slot;
            _dropSlot?.AddToClassList(DropClass);
        }

        private void ShowGhost(ItemSlot source)
        {
            if (_ghost == null)
            {
                _ghost = new VisualElement { name = "ghost", pickingMode = PickingMode.Ignore };
                _ghost.AddToClassList("ds-drag-ghost");
                _ghost.AddToClassList(ItemSlot.UssClassName + "-ghost");
                _ghost.style.position = Position.Absolute;
                _ghostIcon = new VisualElement { pickingMode = PickingMode.Ignore };
                _ghostIcon.AddToClassList(ItemSlot.UssClassName + "-ghost__icon");
                _ghostCount = new Label { pickingMode = PickingMode.Ignore };
                _ghostCount.AddToClassList(ItemSlot.UssClassName + "__count");
                _ghost.Add(_ghostIcon);
                _ghost.Add(_ghostCount);
            }

            var held = _pickup.Held;
            _ghostIcon.style.backgroundImage = new StyleBackground(_visualOf != null ? _visualOf(held.ItemId).Icon : default);
            _ghostCount.text = held.Count.ToString(System.Globalization.CultureInfo.InvariantCulture);
            _ghostCount.style.display = held.Count > 1 ? DisplayStyle.Flex : DisplayStyle.None;

            _ghostHost = TopLevel(this);
            var size = source.layout.size;
            _ghost.style.width = size.x;
            _ghost.style.height = size.y;
            _ghostHalfSize = size * 0.5f;
            _ghostHost.Add(_ghost);
            MoveGhost(_pressPosition);
        }

        private void MoveGhost(Vector2 panelPosition)
        {
            if (_ghost == null || _ghostHost == null) return;
            var local = _ghostHost.WorldToLocal(panelPosition);
            _ghost.style.left = local.x - _ghostHalfSize.x;
            _ghost.style.top = local.y - _ghostHalfSize.y;
        }

        private void HideGhost()
        {
            _ghost?.RemoveFromHierarchy();
            _ghostHost = null;
        }

        private static VisualElement TopLevel(VisualElement element)
        {
            var visualTree = element.panel.visualTree;
            var e = element;
            while (e.hierarchy.parent != null && e.hierarchy.parent != visualTree) e = e.hierarchy.parent;
            return e;
        }

        // ---- keyboard and gamepad ----

        private void OnSubmit(NavigationSubmitEvent evt)
        {
            var slot = (ItemSlot)evt.currentTarget;
            evt.StopPropagation();
            if (_pickup.IsHolding)
            {
                Place(slot);
                return;
            }

            if (SubmitPicksUp && slot.HasItem && !_model.IsLocked(slot.Index))
            {
                var count = CountFor(evt.shiftKey, evt.ctrlKey || evt.commandKey);
                if (count < 0) _pickup.PickHalf(_model, slot.Index);
                else _pickup.Pick(_model, slot.Index, count);
                return;
            }

            SlotActivated?.Invoke(slot);
        }

        private void OnNavigationCancel(NavigationCancelEvent evt)
        {
            if (!_pickup.IsHolding) return;
            _pickup.Cancel();
            evt.StopPropagation();
        }

        private void OnKeyDown(KeyDownEvent evt)
        {
            if (evt.keyCode == KeyCode.Escape)
            {
                if (!_pickup.IsHolding) return;
                _pickup.Cancel();
                evt.StopPropagation();
                return;
            }

            if (evt.keyCode != ContextKey && evt.keyCode != KeyCode.Menu) return;
            var slot = FindSlot(evt.target as VisualElement);
            if (slot == null) return;
            SlotContext?.Invoke(slot, slot.worldBound.center);
            evt.StopPropagation();
        }

        private void OnNavigationMove(NavigationMoveEvent evt)
        {
            var slot = FindSlot(evt.target as VisualElement);
            if (slot == null) return;
            SlotDirection direction;
            switch (evt.direction)
            {
                case NavigationMoveEvent.Direction.Left: direction = SlotDirection.Left; break;
                case NavigationMoveEvent.Direction.Right: direction = SlotDirection.Right; break;
                case NavigationMoveEvent.Direction.Up: direction = SlotDirection.Up; break;
                case NavigationMoveEvent.Direction.Down: direction = SlotDirection.Down; break;
                default: return;
            }

            var next = SlotGridMath.Move(slot.Index, direction, ColumnCount, _slots.Count);
            if (next == slot.Index || next < 0) return; // at an edge: let the default navigation leave the grid
            evt.StopPropagation();
            panel?.focusController.IgnoreEvent(evt);
            _slots[next].Focus();
        }

        private ItemSlot FindSlot(VisualElement element)
        {
            for (var e = element; e != null && e != this; e = e.hierarchy.parent)
                if (e is ItemSlot slot && slot.Grid == this) return slot;
            return null;
        }
    }
}
