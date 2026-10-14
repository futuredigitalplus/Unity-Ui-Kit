/*
 * FancyScrollView (https://github.com/setchi/FancyScrollView)
 * Copyright (c) 2020 setchi
 * Licensed under MIT (https://github.com/setchi/FancyScrollView/blob/master/LICENSE)
 */

// FutureDigitalPlus: the logic of Sources/Runtime/Scroller/Scroller.cs without uGUI. The MonoBehaviour, its EventSystem handlers and the
// uGUI Scrollbar are gone; the view (FancyScroller) feeds pointer, wheel and scrollbar input through PointerDown/PointerUp/BeginDrag/
// Drag/EndDrag/Scroll/SetNormalizedPosition, and time through Tick (replacing Update and Time.unscaledTime). The single-callback
// setters OnValueChanged/OnSelectionChanged became the events ValueChanged/SelectionChanged. Drag takes the pointer distance along the
// axis instead of screen points, Scroll takes a wheel distance in panel units. Tick ignores a zero or negative time step (upstream
// divided by it), Drag and Scroll ignore input while the viewport has no size (before the first layout). New: CancelPress, for a release
// that must not snap because a click started an animation during the press. EndDrag snaps when the drag left no velocity at all
// (upstream only snapped from inertia, so such a release, or any drag with Inertia off, left the content between two items). Every branch of Update, OnPointerUp, OnScroll, OnDrag and the ScrollTo/JumpTo family is otherwise unchanged.

using System;

namespace FutureDigitalPlus.UIKit.ScrollView
{
    /// <summary>
    /// The scroll position of a fancy scroll view and how it moves: drag with a rubber band, inertia, snapping to the nearest item,
    /// animated <see cref="ScrollTo(float, float, Action)"/>, and the ends (<see cref="MovementType"/>). Positions are in items:
    /// 0 is the first item, <c>TotalCount - 1</c> the last. Engine-free; a view feeds it input and time.
    /// </summary>
    public sealed class ScrollerModel
    {
        private static readonly EaseFunction DefaultEaseFunction = EaseFunctions.Get(Ease.OutCubic);

        private readonly AutoScrollState _autoScroll = new AutoScrollState();

        private float _scrollStartPosition;
        private float _prevPosition;
        private float _currentPosition;
        private float _time;
        private bool _hold;
        private bool _scrolling;
        private bool _dragging;
        private float _velocity;

        /// <summary>Length of the viewport along the scroll axis, in the units of the distances passed to <see cref="Drag"/> and <see cref="Scroll"/>.</summary>
        public float ViewportSize { get; set; }

        /// <summary>The scroll axis (only <see cref="GetMovementDirection"/> reads it).</summary>
        public ScrollDirection ScrollDirection { get; set; } = ScrollDirection.Vertical;

        /// <summary>What happens past either end (default <see cref="ScrollView.MovementType.Elastic"/>).</summary>
        public MovementType MovementType { get; set; } = MovementType.Elastic;

        /// <summary>Spring time in seconds when an elastic scroller returns from past an end (default 0.1).</summary>
        public float Elasticity { get; set; } = 0.1f;

        /// <summary>How far the position moves when the pointer is dragged across the whole viewport (default 1 item).</summary>
        public float ScrollSensitivity { get; set; } = 1f;

        /// <summary>True (default): the content keeps moving after a drag and slows down.</summary>
        public bool Inertia { get; set; } = true;

        /// <summary>Velocity left after one second of inertia, as a fraction (default 0.03). Used only when <see cref="Inertia"/> is on.</summary>
        public float DecelerationRate { get; set; } = 0.03f;

        /// <summary>True (default): when inertia slows below <see cref="SnapVelocityThreshold"/>, or on a tap, the content moves to the nearest item.</summary>
        public bool SnapEnabled { get; set; } = true;

        /// <summary>Speed in items per second under which inertia gives way to the snap (default 0.5).</summary>
        public float SnapVelocityThreshold { get; set; } = 0.5f;

        /// <summary>Seconds the snap takes (default 0.3).</summary>
        public float SnapDuration { get; set; } = 0.3f;

        /// <summary>Easing of the snap (default <see cref="Ease.InOutCubic"/>).</summary>
        public Ease SnapEase { get; set; } = Ease.InOutCubic;

        /// <summary>False: pointer and wheel input are ignored (code can still move the position).</summary>
        public bool Draggable { get; set; } = true;

        /// <summary>The item count; the position runs from 0 to <c>TotalCount - 1</c>.</summary>
        public int TotalCount { get; private set; }

        /// <summary>The current velocity in items per second.</summary>
        public float Velocity => _velocity;

        /// <summary>True between <see cref="BeginDrag"/> and <see cref="EndDrag"/>.</summary>
        public bool IsDragging => _dragging;

        /// <summary>True while a <see cref="ScrollTo(float, float, Action)"/>, a snap or an elastic return runs.</summary>
        public bool IsAutoScrolling => _autoScroll.Enable;

        /// <summary>
        /// The current position. Setting it stops any animation, inertia and drag, and raises <see cref="ValueChanged"/>.
        /// </summary>
        public float Position
        {
            get => _currentPosition;
            set
            {
                _autoScroll.Reset();
                _velocity = 0f;
                _dragging = false;

                UpdatePosition(value);
            }
        }

        /// <summary>Raised with the new position every time it changes (also when set to the same value).</summary>
        public event Action<float> ValueChanged;

        /// <summary>Raised with the item the scroller is heading for: on a snap, at the start of a <c>ScrollTo</c>, at an end.</summary>
        public event Action<int> SelectionChanged;

        private sealed class AutoScrollState
        {
            public bool Enable;
            public bool Elastic;
            public float Duration;
            public EaseFunction EaseFunction;
            public float StartTime;
            public float EndPosition;

            public Action OnComplete;

            public void Reset()
            {
                Enable = false;
                Elastic = false;
                Duration = 0f;
                StartTime = 0f;
                EaseFunction = DefaultEaseFunction;
                EndPosition = 0f;
                OnComplete = null;
            }

            public void Complete()
            {
                OnComplete?.Invoke();
                Reset();
            }
        }

        /// <summary>Sets the item count, which gives the largest position (<c>totalCount - 1</c>).</summary>
        public void SetTotalCount(int totalCount) => TotalCount = totalCount;

        /// <summary>Moves to <paramref name="position"/> in <paramref name="duration"/> seconds with <see cref="Ease.OutCubic"/>.</summary>
        /// <param name="position">Target position, 0 to <c>TotalCount - 1</c>.</param>
        /// <param name="duration">Seconds; 0 or less jumps.</param>
        /// <param name="onComplete">Called when the move ends (not when it is interrupted).</param>
        public void ScrollTo(float position, float duration, Action onComplete = null) => ScrollTo(position, duration, Ease.OutCubic, onComplete);

        /// <summary>Moves to <paramref name="position"/> in <paramref name="duration"/> seconds with <paramref name="ease"/>.</summary>
        /// <param name="position">Target position, 0 to <c>TotalCount - 1</c>.</param>
        /// <param name="duration">Seconds; 0 or less jumps.</param>
        /// <param name="ease">The easing curve.</param>
        /// <param name="onComplete">Called when the move ends (not when it is interrupted).</param>
        public void ScrollTo(float position, float duration, Ease ease, Action onComplete = null) => ScrollTo(position, duration, EaseFunctions.Get(ease), onComplete);

        /// <summary>Moves to <paramref name="position"/> in <paramref name="duration"/> seconds with a custom easing function.</summary>
        /// <param name="position">Target position, 0 to <c>TotalCount - 1</c>.</param>
        /// <param name="duration">Seconds; 0 or less jumps.</param>
        /// <param name="easeFunction">The easing function; null uses <see cref="Ease.OutCubic"/>.</param>
        /// <param name="onComplete">Called when the move ends (not when it is interrupted).</param>
        public void ScrollTo(float position, float duration, EaseFunction easeFunction, Action onComplete = null)
        {
            if (duration <= 0f)
            {
                Position = ScrollMath.CircularPosition(position, TotalCount);
                onComplete?.Invoke();
                return;
            }

            _autoScroll.Reset();
            _autoScroll.Enable = true;
            _autoScroll.Duration = duration;
            _autoScroll.EaseFunction = easeFunction ?? DefaultEaseFunction;
            _autoScroll.StartTime = _time;
            _autoScroll.EndPosition = _currentPosition + CalculateMovementAmount(_currentPosition, position);
            _autoScroll.OnComplete = onComplete;

            _velocity = 0f;
            _scrollStartPosition = _currentPosition;

            UpdateSelection(ScrollMath.RoundToInt(ScrollMath.CircularPosition(_autoScroll.EndPosition, TotalCount)));
        }

        /// <summary>Jumps to the item at <paramref name="index"/> and raises <see cref="SelectionChanged"/>.</summary>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="index"/> is outside 0..TotalCount-1.</exception>
        public void JumpTo(int index)
        {
            if (index < 0 || index > TotalCount - 1)
            {
                throw new ArgumentOutOfRangeException(nameof(index));
            }

            UpdateSelection(index);
            Position = index;
        }

        /// <summary>
        /// The way the content moves on screen to go from <paramref name="sourceIndex"/> to <paramref name="destIndex"/>;
        /// with <see cref="ScrollView.MovementType.Unrestricted"/>, the way of the shorter trip around.
        /// </summary>
        public MovementDirection GetMovementDirection(int sourceIndex, int destIndex)
        {
            var movementAmount = CalculateMovementAmount(sourceIndex, destIndex);
            return ScrollDirection == ScrollDirection.Horizontal
                ? movementAmount > 0
                    ? MovementDirection.Left
                    : MovementDirection.Right
                : movementAmount > 0
                    ? MovementDirection.Up
                    : MovementDirection.Down;
        }

        /// <summary>A primary pointer went down on the viewport: stops inertia and animation.</summary>
        public void PointerDown()
        {
            if (!Draggable)
            {
                return;
            }

            _hold = true;
            _velocity = 0f;
            _autoScroll.Reset();
        }

        /// <summary>The primary pointer went up. After a press without a drag, snaps to the nearest item. Call before <see cref="EndDrag"/>.</summary>
        public void PointerUp()
        {
            if (!Draggable)
            {
                return;
            }

            if (_hold && SnapEnabled)
            {
                UpdateSelection(ScrollMath.RoundToInt(ScrollMath.CircularPosition(_currentPosition, TotalCount)));
                ScrollTo(ScrollMath.RoundToInt(_currentPosition), SnapDuration, SnapEase);
            }

            _hold = false;
        }

        /// <summary>
        /// The press ended without a snap: something started an animation during it (a click on a cell that calls <c>ScrollTo</c>),
        /// and that animation wins. Call instead of <see cref="PointerUp"/>.
        /// </summary>
        public void CancelPress() => _hold = false;

        /// <summary>
        /// A wheel or trackpad moved the content by <paramref name="scrollDelta"/> along the axis, in the units of <see cref="ViewportSize"/>.
        /// Positive moves towards the end (later items).
        /// </summary>
        public void Scroll(float scrollDelta)
        {
            if (!Draggable || !(ViewportSize > 0f))
            {
                return;
            }

            _scrolling = true;

            var position = _currentPosition + scrollDelta / ViewportSize * ScrollSensitivity;
            if (MovementType == MovementType.Clamped)
            {
                position += CalculateOffset(position);
            }

            if (_autoScroll.Enable)
            {
                _autoScroll.Reset();
            }

            UpdatePosition(position);
        }

        /// <summary>The pointer moved far enough from where it went down to start a drag.</summary>
        public void BeginDrag()
        {
            if (!Draggable)
            {
                return;
            }

            _hold = false;
            _scrollStartPosition = _currentPosition;
            _dragging = true;
            _autoScroll.Reset();
        }

        /// <summary>
        /// The pointer is <paramref name="distance"/> away from where the drag began, along the axis, in the units of <see cref="ViewportSize"/>.
        /// Positive moves towards the end (pointer moving up or left).
        /// </summary>
        public void Drag(float distance)
        {
            if (!Draggable || !_dragging || !(ViewportSize > 0f))
            {
                return;
            }

            var position = distance / ViewportSize * ScrollSensitivity + _scrollStartPosition;

            var offset = CalculateOffset(position);
            position += offset;

            if (MovementType == MovementType.Elastic)
            {
                if (offset != 0f)
                {
                    position -= ScrollMath.RubberDelta(offset, ScrollSensitivity);
                }
            }

            UpdatePosition(position);
        }

        /// <summary>
        /// The drag ended. Inertia, the elastic return and the snap continue in <see cref="Tick"/>. A drag that left no velocity at all
        /// (released in the frame it moved, after holding still for seconds, or with <see cref="Inertia"/> off) snaps at once.
        /// </summary>
        public void EndDrag()
        {
            if (!Draggable)
            {
                return;
            }

            _dragging = false;

            if (SnapEnabled && _velocity == 0f && !_autoScroll.Enable)
            {
                ScrollTo(ScrollMath.RoundToInt(_currentPosition), SnapDuration, SnapEase);
            }
        }

        /// <summary>A scrollbar moved to <paramref name="normalized"/> (0 first item, 1 last item). Keeps inertia and animation running.</summary>
        public void SetNormalizedPosition(float normalized) => UpdatePosition(normalized * (TotalCount - 1f));

        /// <summary>The scrollbar value for the current position: 0 at the first item, 1 at the last.</summary>
        public float NormalizedPosition => ScrollMath.Clamp01(_currentPosition / Math.Max(TotalCount - 1f, 1e-4f));

        /// <summary>Advances animation, inertia, the elastic return and the snap by <paramref name="deltaTime"/> seconds (unscaled). Call once per frame.</summary>
        public void Tick(float deltaTime)
        {
            if (!(deltaTime > 0f))
            {
                return;
            }

            _time += deltaTime;
            var offset = CalculateOffset(_currentPosition);

            if (_autoScroll.Enable)
            {
                var position = 0f;

                if (_autoScroll.Elastic)
                {
                    position = ScrollMath.SmoothDamp(_currentPosition, _currentPosition + offset, ref _velocity,
                        Elasticity, float.PositiveInfinity, deltaTime);

                    if (Math.Abs(_velocity) < 0.01f)
                    {
                        position = ScrollMath.Clamp(ScrollMath.RoundToInt(position), 0, TotalCount - 1);
                        _velocity = 0f;
                        _autoScroll.Complete();
                    }
                }
                else
                {
                    var alpha = ScrollMath.Clamp01((_time - _autoScroll.StartTime) /
                                                   Math.Max(_autoScroll.Duration, float.Epsilon));
                    position = ScrollMath.LerpUnclamped(_scrollStartPosition, _autoScroll.EndPosition,
                        _autoScroll.EaseFunction(alpha));

                    if (ScrollMath.Approximately(alpha, 1f))
                    {
                        _autoScroll.Complete();
                    }
                }

                UpdatePosition(position);
            }
            else if (!(_dragging || _scrolling) && (!ScrollMath.Approximately(offset, 0f) || !ScrollMath.Approximately(_velocity, 0f)))
            {
                var position = _currentPosition;

                if (MovementType == MovementType.Elastic && !ScrollMath.Approximately(offset, 0f))
                {
                    _autoScroll.Reset();
                    _autoScroll.Enable = true;
                    _autoScroll.Elastic = true;

                    UpdateSelection(ScrollMath.Clamp(ScrollMath.RoundToInt(position), 0, TotalCount - 1));
                }
                else if (Inertia)
                {
                    _velocity *= MathF.Pow(DecelerationRate, deltaTime);

                    if (Math.Abs(_velocity) < 0.001f)
                    {
                        _velocity = 0f;
                    }

                    position += _velocity * deltaTime;

                    if (SnapEnabled && Math.Abs(_velocity) < SnapVelocityThreshold)
                    {
                        ScrollTo(ScrollMath.RoundToInt(_currentPosition), SnapDuration, SnapEase);
                    }
                }
                else
                {
                    _velocity = 0f;
                }

                if (!ScrollMath.Approximately(_velocity, 0f))
                {
                    if (MovementType == MovementType.Clamped)
                    {
                        offset = CalculateOffset(position);
                        position += offset;

                        if (ScrollMath.Approximately(position, 0f) || ScrollMath.Approximately(position, TotalCount - 1f))
                        {
                            _velocity = 0f;
                            UpdateSelection(ScrollMath.RoundToInt(position));
                        }
                    }

                    UpdatePosition(position);
                }
            }

            if (!_autoScroll.Enable && (_dragging || _scrolling) && Inertia)
            {
                var newVelocity = (_currentPosition - _prevPosition) / deltaTime;
                _velocity = ScrollMath.Lerp(_velocity, newVelocity, deltaTime * 10f);
            }

            _prevPosition = _currentPosition;
            _scrolling = false;
        }

        private float CalculateOffset(float position)
        {
            if (MovementType == MovementType.Unrestricted)
            {
                return 0f;
            }

            if (position < 0f)
            {
                return -position;
            }

            if (position > TotalCount - 1)
            {
                return TotalCount - 1 - position;
            }

            return 0f;
        }

        private void UpdatePosition(float position)
        {
            _currentPosition = position;
            ValueChanged?.Invoke(position);
        }

        private void UpdateSelection(int index) => SelectionChanged?.Invoke(index);

        private float CalculateMovementAmount(float sourcePosition, float destPosition)
        {
            if (MovementType != MovementType.Unrestricted)
            {
                return ScrollMath.Clamp(destPosition, 0, TotalCount - 1) - sourcePosition;
            }

            var amount = ScrollMath.CircularPosition(destPosition, TotalCount) - ScrollMath.CircularPosition(sourcePosition, TotalCount);

            if (Math.Abs(amount) > TotalCount * 0.5f)
            {
                amount = ScrollMath.Sign(-amount) * (TotalCount - Math.Abs(amount));
            }

            return amount;
        }
    }
}
