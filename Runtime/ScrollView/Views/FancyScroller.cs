/*
 * FancyScrollView (https://github.com/setchi/FancyScrollView)
 * Copyright (c) 2020 setchi
 * Licensed under MIT (https://github.com/setchi/FancyScrollView/blob/master/LICENSE)
 */

// FutureDigitalPlus: the UI Toolkit half of Sources/Runtime/Scroller/Scroller.cs (its logic is ScrollerModel in Core). Replaces the uGUI
// EventSystem handlers with pointer and wheel callbacks on the viewport element, Update with a scheduled tick, and the uGUI Scrollbar
// with an optional UI Toolkit Scroller. New: a drag starts after DragThreshold (the EventSystem's drag threshold did that upstream) and
// captures the pointer, so a tap still clicks a button inside a cell; one wheel unit moves WheelStep panel units (upstream moved one
// screen point per wheel unit, which hardly moved the content); a cancelled pointer ends the drag like a release. While a child (a
// Button) holds the pointer, UI Toolkit sends the pointer events to that child only, so the scroller listens on the child for as long
// as it holds the capture (the technique of UI Toolkit's own ScrollView), and on the panel root for a release outside the viewport; a
// move with no button pressed ends a press whose release was lost. A release snaps only when nothing started an animation during the
// press, so a click on a cell that calls ScrollTo is not overridden by the snap (upstream: uGUI sends the click after the scroller's
// pointer-up). The viewport is the element's padding box, where the cells are. The scroll direction is fixed at construction, as
// upstream. If the view is resized during a drag, the drag continues from where the pointer is.

using System;
using UnityEngine;
using UnityEngine.UIElements;

namespace FutureDigitalPlus.UIKit.ScrollView
{
    /// <summary>
    /// Scrolls a fancy view by pointer drag, mouse wheel, trackpad and an optional scrollbar, with inertia, a rubber band at the ends and
    /// snapping to the nearest item (all in <see cref="Model"/>). Attach it to the viewport element; it ticks while the viewport is in a
    /// panel. Subscribe the view to <see cref="ValueChanged"/>; <see cref="FancyScrollRect{TItemData, TContext}"/> has its own.
    /// </summary>
    public sealed class FancyScroller
    {
        private readonly IVisualElementScheduledItem _tick;
        private Scroller _scrollbar;
        private int _pointerId = PointerId.invalidPointerId;
        private Vector2 _pressPosition;
        private Vector2 _dragStartPosition;
        private bool _dragging;
        private VisualElement _capturedChild;
        private VisualElement _panelRoot;
        private bool _skipTick = true;

        /// <summary>Attaches a scroller to <paramref name="viewport"/>: pointer and wheel input on it (and on its children) scroll.</summary>
        /// <param name="viewport">The element whose size is the viewport and which takes the input, usually the view itself.</param>
        /// <param name="direction">The scroll axis.</param>
        public FancyScroller(VisualElement viewport, ScrollDirection direction = ScrollDirection.Vertical)
        {
            Viewport = viewport ?? throw new ArgumentNullException(nameof(viewport));
            Model = new ScrollerModel { ScrollDirection = direction };
            Model.ValueChanged += OnModelValueChanged;

            Viewport.RegisterCallback<PointerDownEvent>(OnPointerDown, TrickleDown.TrickleDown);
            Viewport.RegisterCallback<PointerMoveEvent>(OnPointerMove, TrickleDown.TrickleDown);
            Viewport.RegisterCallback<PointerUpEvent>(OnPointerUp, TrickleDown.TrickleDown);
            Viewport.RegisterCallback<PointerCancelEvent>(OnPointerCancel, TrickleDown.TrickleDown);
            Viewport.RegisterCallback<PointerCaptureEvent>(OnPointerCapture);
            Viewport.RegisterCallback<PointerCaptureOutEvent>(OnPointerCaptureOut);
            Viewport.RegisterCallback<WheelEvent>(OnWheel);
            Viewport.RegisterCallback<GeometryChangedEvent>(OnGeometryChanged);
            Viewport.RegisterCallback<AttachToPanelEvent>(OnAttachToPanel);
            Viewport.RegisterCallback<DetachFromPanelEvent>(OnDetachFromPanel);
            if (Viewport.panel != null) WatchPanelRoot(Viewport.panel.visualTree);
            _tick = Viewport.schedule.Execute(OnTick).Every(0);
        }

        /// <summary>The element that gives the viewport size and takes the input.</summary>
        public VisualElement Viewport { get; }

        /// <summary>The engine-free scroll state; everything below forwards to it.</summary>
        public ScrollerModel Model { get; }

        /// <summary>
        /// Viewport length along the scroll axis in panel units: the viewport's padding box, where the cells are (the element's size less its
        /// borders; its padding does not inset the cells). 0 before the first layout.
        /// </summary>
        public float ViewportSize
        {
            get
            {
                var size = PaddingBoxSize(Viewport);
                return ScrollDirection == ScrollDirection.Horizontal ? size.x : size.y;
            }
        }

        /// <summary>The scroll axis, set at construction.</summary>
        public ScrollDirection ScrollDirection => Model.ScrollDirection;

        /// <summary>Distance in panel units the pointer moves before a press becomes a drag (default 8). Below it, a press is a tap.</summary>
        public float DragThreshold { get; set; } = 8f;

        /// <summary>Panel units one wheel unit scrolls (default 18, like a UI Toolkit ScrollView).</summary>
        public float WheelStep { get; set; } = 18f;

        /// <inheritdoc cref="ScrollerModel.MovementType"/>
        public MovementType MovementType { get => Model.MovementType; set => Model.MovementType = value; }

        /// <inheritdoc cref="ScrollerModel.Elasticity"/>
        public float Elasticity { get => Model.Elasticity; set => Model.Elasticity = value; }

        /// <inheritdoc cref="ScrollerModel.ScrollSensitivity"/>
        public float ScrollSensitivity { get => Model.ScrollSensitivity; set => Model.ScrollSensitivity = value; }

        /// <inheritdoc cref="ScrollerModel.Inertia"/>
        public bool Inertia { get => Model.Inertia; set => Model.Inertia = value; }

        /// <inheritdoc cref="ScrollerModel.DecelerationRate"/>
        public float DecelerationRate { get => Model.DecelerationRate; set => Model.DecelerationRate = value; }

        /// <inheritdoc cref="ScrollerModel.SnapEnabled"/>
        public bool SnapEnabled { get => Model.SnapEnabled; set => Model.SnapEnabled = value; }

        /// <inheritdoc cref="ScrollerModel.SnapVelocityThreshold"/>
        public float SnapVelocityThreshold { get => Model.SnapVelocityThreshold; set => Model.SnapVelocityThreshold = value; }

        /// <inheritdoc cref="ScrollerModel.SnapDuration"/>
        public float SnapDuration { get => Model.SnapDuration; set => Model.SnapDuration = value; }

        /// <inheritdoc cref="ScrollerModel.SnapEase"/>
        public Ease SnapEase { get => Model.SnapEase; set => Model.SnapEase = value; }

        /// <inheritdoc cref="ScrollerModel.Draggable"/>
        public bool Draggable { get => Model.Draggable; set => Model.Draggable = value; }

        /// <inheritdoc cref="ScrollerModel.Position"/>
        public float Position { get => Model.Position; set => Model.Position = value; }

        /// <inheritdoc cref="ScrollerModel.TotalCount"/>
        public int TotalCount => Model.TotalCount;

        /// <inheritdoc cref="ScrollerModel.ValueChanged"/>
        public event Action<float> ValueChanged
        {
            add => Model.ValueChanged += value;
            remove => Model.ValueChanged -= value;
        }

        /// <inheritdoc cref="ScrollerModel.SelectionChanged"/>
        public event Action<int> SelectionChanged
        {
            add => Model.SelectionChanged += value;
            remove => Model.SelectionChanged -= value;
        }

        /// <summary>
        /// An optional UI Toolkit scrollbar (range 0..1 is set for you) that follows the position and scrolls when dragged. Add it to the
        /// layout yourself, for example as a child of the viewport; presses on it do not drag the content. Null removes it.
        /// </summary>
        public Scroller Scrollbar
        {
            get => _scrollbar;
            set
            {
                if (_scrollbar != null) _scrollbar.valueChanged -= OnScrollbarValueChanged;
                _scrollbar = value;
                if (_scrollbar == null) return;
                _scrollbar.lowValue = 0f;
                _scrollbar.highValue = 1f;
                _scrollbar.slider.SetValueWithoutNotify(Model.NormalizedPosition);
                _scrollbar.valueChanged += OnScrollbarValueChanged;
            }
        }

        /// <inheritdoc cref="ScrollerModel.SetTotalCount"/>
        public void SetTotalCount(int totalCount) => Model.SetTotalCount(totalCount);

        /// <inheritdoc cref="ScrollerModel.ScrollTo(float, float, Action)"/>
        public void ScrollTo(float position, float duration, Action onComplete = null) => Model.ScrollTo(position, duration, onComplete);

        /// <inheritdoc cref="ScrollerModel.ScrollTo(float, float, Ease, Action)"/>
        public void ScrollTo(float position, float duration, Ease ease, Action onComplete = null) => Model.ScrollTo(position, duration, ease, onComplete);

        /// <inheritdoc cref="ScrollerModel.ScrollTo(float, float, EaseFunction, Action)"/>
        public void ScrollTo(float position, float duration, EaseFunction easeFunction, Action onComplete = null) =>
            Model.ScrollTo(position, duration, easeFunction, onComplete);

        /// <inheritdoc cref="ScrollerModel.JumpTo"/>
        public void JumpTo(int index) => Model.JumpTo(index);

        /// <inheritdoc cref="ScrollerModel.GetMovementDirection"/>
        public MovementDirection GetMovementDirection(int sourceIndex, int destIndex) => Model.GetMovementDirection(sourceIndex, destIndex);

        /// <summary>Advances the scroller by <paramref name="deltaTime"/> seconds. It ticks by itself while in a panel; tests call this.</summary>
        public void Tick(float deltaTime)
        {
            SyncViewportSize();
            Model.Tick(deltaTime);
        }

        /// <summary>Ends a press in progress, stops ticking and listening (the viewport keeps working as a plain element). Not reversible.</summary>
        public void Dispose()
        {
            if (_pointerId != PointerId.invalidPointerId) Release();
            _tick.Pause();
            Scrollbar = null;
            Model.ValueChanged -= OnModelValueChanged;
            Viewport.UnregisterCallback<PointerDownEvent>(OnPointerDown, TrickleDown.TrickleDown);
            Viewport.UnregisterCallback<PointerMoveEvent>(OnPointerMove, TrickleDown.TrickleDown);
            Viewport.UnregisterCallback<PointerUpEvent>(OnPointerUp, TrickleDown.TrickleDown);
            Viewport.UnregisterCallback<PointerCancelEvent>(OnPointerCancel, TrickleDown.TrickleDown);
            Viewport.UnregisterCallback<PointerCaptureEvent>(OnPointerCapture);
            Viewport.UnregisterCallback<PointerCaptureOutEvent>(OnPointerCaptureOut);
            Viewport.UnregisterCallback<WheelEvent>(OnWheel);
            Viewport.UnregisterCallback<GeometryChangedEvent>(OnGeometryChanged);
            Viewport.UnregisterCallback<AttachToPanelEvent>(OnAttachToPanel);
            Viewport.UnregisterCallback<DetachFromPanelEvent>(OnDetachFromPanel);
            WatchPanelRoot(null);
            UnwatchCapturedChild();
        }

        private void OnTick(TimerState timer)
        {
            // Real time like upstream's Time.unscaledDeltaTime. The first step after attaching measures the time spent detached: skipped.
            if (_skipTick)
            {
                _skipTick = false;
                return;
            }

            Tick(timer.deltaTime / 1000f);
        }

        private void SyncViewportSize() => Model.ViewportSize = ViewportSize;

        private void OnGeometryChanged(GeometryChangedEvent evt) => SyncViewportSize();

        /// <summary>The size of <paramref name="element"/>'s padding box (its layout size less its borders); zero before the first layout.</summary>
        internal static Vector2 PaddingBoxSize(VisualElement element)
        {
            var rect = element.layout;
            var style = element.resolvedStyle;
            var width = rect.width - style.borderLeftWidth - style.borderRightWidth;
            var height = rect.height - style.borderTopWidth - style.borderBottomWidth;
            return new Vector2(float.IsNaN(width) ? 0f : Mathf.Max(0f, width), float.IsNaN(height) ? 0f : Mathf.Max(0f, height));
        }

        private bool IsOnScrollbar(IEventHandler target) =>
            _scrollbar != null && target is VisualElement element && (element == _scrollbar || _scrollbar.Contains(element));

        private void OnPointerDown(PointerDownEvent evt)
        {
            if (evt.pointerId == _pointerId) Release(); // the release of the last press never reached us
            if (evt.button != 0 || _pointerId != PointerId.invalidPointerId || IsOnScrollbar(evt.target)) return;

            SyncViewportSize();
            _pointerId = evt.pointerId;
            _pressPosition = evt.position;
            _dragging = false;
            Model.PointerDown();
        }

        private void OnPointerMove(PointerMoveEvent evt)
        {
            if (evt.pointerId != _pointerId) return;

            if (evt.pressedButtons == 0)
            {
                Release(); // the release was lost (a native overlay, outside the window): the press is over
                return;
            }

            var position = (Vector2)evt.position;
            if (_dragging && !Model.IsDragging)
            {
                // Something set the position during the drag (a relayout after a resize): continue from here.
                _dragStartPosition = position;
                Model.BeginDrag();
            }
            else if (!_dragging)
            {
                if ((position - _pressPosition).sqrMagnitude < DragThreshold * DragThreshold || !Model.Draggable) return;

                _dragging = true;
                _dragStartPosition = position;
                Viewport.CapturePointer(_pointerId);
                Model.BeginDrag();
            }

            Model.Drag(DragDistance(position));
            evt.StopPropagation();
        }

        private void OnPointerUp(PointerUpEvent evt)
        {
            if (evt.pointerId != _pointerId || evt.button != 0) return;

            var wasDragging = _dragging;
            Release();
            if (wasDragging) evt.StopPropagation();
        }

        private void OnPointerCancel(PointerCancelEvent evt)
        {
            if (evt.pointerId == _pointerId) Release();
        }

        // A child of the viewport (a Button in a cell) took the pointer: from now on the events of that pointer go to it alone.
        private void OnPointerCapture(PointerCaptureEvent evt)
        {
            if (evt.pointerId != _pointerId || !(evt.target is VisualElement child) || child == Viewport) return;

            UnwatchCapturedChild();
            _capturedChild = child;
            _capturedChild.RegisterCallback<PointerMoveEvent>(OnPointerMove);
            _capturedChild.RegisterCallback<PointerUpEvent>(OnPointerUp);
            _capturedChild.RegisterCallback<PointerCancelEvent>(OnPointerCancel);
        }

        private void OnPointerCaptureOut(PointerCaptureOutEvent evt)
        {
            if (evt.target == _capturedChild)
            {
                UnwatchCapturedChild(); // released by the child, or taken by the viewport when the drag began
            }
            else if (evt.target == Viewport && evt.pointerId == _pointerId && _dragging)
            {
                Release(); // something else took the pointer from the drag
            }
        }

        private void UnwatchCapturedChild()
        {
            if (_capturedChild == null) return;
            _capturedChild.UnregisterCallback<PointerMoveEvent>(OnPointerMove);
            _capturedChild.UnregisterCallback<PointerUpEvent>(OnPointerUp);
            _capturedChild.UnregisterCallback<PointerCancelEvent>(OnPointerCancel);
            _capturedChild = null;
        }

        private void OnAttachToPanel(AttachToPanelEvent evt)
        {
            _skipTick = true;
            WatchPanelRoot(evt.destinationPanel.visualTree);
        }

        private void OnDetachFromPanel(DetachFromPanelEvent evt)
        {
            WatchPanelRoot(null);
            if (_pointerId != PointerId.invalidPointerId) Release();
        }

        private void WatchPanelRoot(VisualElement root)
        {
            _panelRoot?.UnregisterCallback<PointerUpEvent>(OnRootPointerUp, TrickleDown.TrickleDown);
            _panelRoot = root;
            _panelRoot?.RegisterCallback<PointerUpEvent>(OnRootPointerUp, TrickleDown.TrickleDown);
        }

        // A press that began in the viewport and is released outside it (nobody captured the pointer).
        private void OnRootPointerUp(PointerUpEvent evt)
        {
            if (evt.pointerId != _pointerId || evt.button != 0 || (evt.target is VisualElement target && (target == Viewport || Viewport.Contains(target)))) return;
            Release();
        }

        private void Release()
        {
            var id = _pointerId;
            var wasDragging = _dragging;
            _pointerId = PointerId.invalidPointerId;
            _dragging = false;

            // An animation started during the press (a click on a cell that calls ScrollTo) wins over the snap of the release.
            if (Model.IsAutoScrolling) Model.CancelPress();
            else Model.PointerUp();
            if (wasDragging) Model.EndDrag();
            if (Viewport.HasPointerCapture(id)) Viewport.ReleasePointer(id);
        }

        private void OnWheel(WheelEvent evt)
        {
            if (!Model.Draggable) return;

            SyncViewportSize();
            var dx = evt.delta.x;
            var dy = evt.delta.y;
            var delta = ScrollDirection == ScrollDirection.Horizontal
                ? Mathf.Abs(dy) > Mathf.Abs(dx) ? dy : dx
                : Mathf.Abs(dx) > Mathf.Abs(dy) ? dx : dy;
            Model.Scroll(delta * WheelStep);
            evt.StopPropagation();
        }

        // Upstream's sign: dragging up (vertical) or left (horizontal) moves to later items. Panel y grows downwards.
        private float DragDistance(Vector2 pointer)
        {
            var delta = pointer - _dragStartPosition;
            return ScrollDirection == ScrollDirection.Horizontal ? -delta.x : -delta.y;
        }

        private void OnScrollbarValueChanged(float value) => Model.SetNormalizedPosition(value);

        private void OnModelValueChanged(float _)
        {
            _scrollbar?.slider.SetValueWithoutNotify(Model.NormalizedPosition);
        }
    }
}
