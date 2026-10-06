using System;
using System.Collections.Generic;
using FutureDigitalPlus.UIKit.Foundation;
using UnityEngine;
using UnityEngine.UIElements;

namespace FutureDigitalPlus.UIKit.Controls
{
    /// <summary>
    /// A bottom sheet on the design system sheet skin (<c>ds-sheet</c>): it slides up over a scrim, can rest at several
    /// heights (snap points), is dragged by its handle with the pointer or moved with the up and down arrows, closes
    /// on a downward flick, on Cancel or back, or on a tap on the scrim, keeps keyboard and gamepad focus inside while open
    /// and gives focus back when it closes. Put the content in the sheet with <c>sheet.Add(...)</c>, then call
    /// <see cref="Open"/> with any element of the panel. Positions are fractions of the sheet's own height (1 fully open).
    /// </summary>
    public sealed class BottomSheet : VisualElement
    {
        /// <summary>USS class of the sheet element.</summary>
        public const string UssClassName = "fdp-bottom-sheet";

        private const string InstantClass = UssClassName + "--instant";
        private const int CloseMilliseconds = 220;

        private readonly VisualElement _content = new VisualElement { name = "content" };
        private readonly VisualElement _grab = new VisualElement { name = "grab", focusable = true };
        private readonly VisualElement _scrim = new VisualElement { name = "scrim", pickingMode = PickingMode.Position };
        private readonly List<float> _snapPoints = new List<float> { 1f };
        private VisualElement _layer;
        private PopupFocus _focus;
        private IVisualElementScheduledItem _closeTimer;
        private int _snapIndex;
        private float _visible;
        private int _lastCancelFrame = -1;
        private bool _closing;
        private bool _slid;

        // drag state
        private int _dragPointer = -1;
        private float _dragStartY;
        private float _dragStartVisible;
        private float _lastY;
        private double _lastTime;
        private float _velocity;

        /// <summary>Creates a closed sheet with one snap point (fully open).</summary>
        public BottomSheet()
        {
            AddToClassList("ds-sheet");
            AddToClassList(UssClassName);
            AddToClassList(InstantClass);
            style.translate = new StyleTranslate(new Translate(0f, Length.Percent(100f)));

            _grab.AddToClassList(UssClassName + "__grab");
            var handle = new VisualElement { pickingMode = PickingMode.Ignore };
            handle.AddToClassList("ds-sheet__handle");
            _grab.Add(handle);
            hierarchy.Add(_grab);
            _content.AddToClassList(UssClassName + "__content");
            hierarchy.Add(_content);

            _scrim.AddToClassList(UssClassName + "__scrim");
            _scrim.RegisterCallback<PointerDownEvent>(OnScrimPointerDown);

            _grab.RegisterCallback<PointerDownEvent>(OnGrabDown);
            _grab.RegisterCallback<PointerMoveEvent>(OnGrabMove);
            _grab.RegisterCallback<PointerUpEvent>(OnGrabUp);
            _grab.RegisterCallback<PointerCaptureOutEvent>(OnGrabCaptureOut);
            _grab.RegisterCallback<NavigationMoveEvent>(OnGrabMoveKey);

            RegisterCallback<NavigationCancelEvent>(OnCancel);
            RegisterCallback<KeyDownEvent>(OnKeyDown);
        }

        /// <inheritdoc />
        public override VisualElement contentContainer => _content;

        /// <summary>Raised after the sheet closed and left the panel.</summary>
        public event Action Closed;

        /// <summary>Raised when the sheet settles at another snap point (with the new index).</summary>
        public event Action<int> SnapChanged;

        /// <summary>True while the sheet is on a panel and not closing.</summary>
        public bool IsOpen => _layer != null && !_closing;

        /// <summary>True (default) when the sheet may be closed by the user (scrim tap, flick, Cancel).</summary>
        public bool Dismissable { get; set; } = true;

        /// <summary>The index of the snap point the sheet rests at.</summary>
        public int SnapIndex => _snapIndex;

        /// <summary>The visible fraction of the sheet right now (changes while dragging).</summary>
        public float VisibleFraction => _visible;

        /// <summary>The snap points, ascending.</summary>
        public IReadOnlyList<float> SnapPoints => _snapPoints;

        /// <summary>Replaces the snap points: ascending fractions above 0 and at most 1.</summary>
        public void SetSnapPoints(params float[] points)
        {
            if (points == null || points.Length == 0) throw new ArgumentException("At least one snap point is required.", nameof(points));
            for (var i = 0; i < points.Length; i++)
            {
                if (points[i] <= 0f || points[i] > 1f) throw new ArgumentException("Snap points must be above 0 and at most 1.", nameof(points));
                if (i > 0 && points[i] <= points[i - 1]) throw new ArgumentException("Snap points must be ascending.", nameof(points));
            }

            _snapPoints.Clear();
            _snapPoints.AddRange(points);
            _snapIndex = Math.Min(_snapIndex, _snapPoints.Count - 1);
        }

        /// <summary>Opens the sheet on the top element of the panel that contains <paramref name="anchor"/>, at the snap point <paramref name="snapIndex"/>.</summary>
        /// <returns>False when the anchor is not on a panel or the sheet is already open.</returns>
        public bool Open(VisualElement anchor, int snapIndex = 0)
        {
            if (_layer != null) return false;
            var host = PopupHost.TopLevel(anchor);
            if (host == null) return false;

            _closing = false;
            _snapIndex = Math.Max(0, Math.Min(_snapPoints.Count - 1, snapIndex));
            _layer = PopupHost.CreateLayer(host, "bottom-sheet");
            _layer.AddToClassList(UssClassName + "-layer");
            _layer.Add(_scrim);
            _layer.Add(this);

            AddToClassList(InstantClass);
            SetVisible(0f);
            _scrim.RemoveFromClassList(UssClassName + "__scrim--shown");

            _focus = new PopupFocus(_layer);
            _focus.Begin();

            // Slide in once the sheet has been laid out at its hidden position (a transition needs a resolved start value);
            // the timer is the fallback for a sheet that never gets a geometry event.
            _slid = false;
            RegisterCallback<GeometryChangedEvent>(OnFirstGeometry);
            schedule.Execute(SlideIn).StartingIn(250);
            return true;
        }

        /// <summary>Moves the sheet to another snap point.</summary>
        public void SnapTo(int index)
        {
            if (!IsOpen) return;
            index = Math.Max(0, Math.Min(_snapPoints.Count - 1, index));
            var changed = index != _snapIndex;
            _snapIndex = index;
            RemoveFromClassList(InstantClass);
            SetVisible(_snapPoints[index]);
            if (changed) SnapChanged?.Invoke(index);
        }

        /// <summary>Closes the sheet with a slide down (at once under reduced motion).</summary>
        public void Close()
        {
            if (_layer == null || _closing) return;
            _closing = true;
            _scrim.RemoveFromClassList(UssClassName + "__scrim--shown");
            var animate = !MotionGate.IsReduced(this);
            if (animate)
            {
                RemoveFromClassList(InstantClass);
                SetVisible(0f);
                _closeTimer = schedule.Execute(Finish).StartingIn(CloseMilliseconds);
            }
            else
            {
                Finish();
            }
        }

        /// <summary>Handles a back request (the Android back button, a back dispatcher): closes the sheet when it is dismissable. Returns true when it was consumed.</summary>
        public bool HandleBack()
        {
            if (!IsOpen) return false;
            if (Dismissable) Close();
            return true;
        }

        private void OnFirstGeometry(GeometryChangedEvent evt)
        {
            UnregisterCallback<GeometryChangedEvent>(OnFirstGeometry);
            schedule.Execute(SlideIn);
        }

        private void SlideIn()
        {
            if (_layer == null || _closing || _slid) return;
            _slid = true;
            _scrim.AddToClassList(UssClassName + "__scrim--shown");
            if (MotionGate.IsReduced(this)) AddToClassList(InstantClass);
            else RemoveFromClassList(InstantClass);
            SetVisible(_snapPoints[_snapIndex]);
            _focus.Focus(UiFocus.FirstFocusable(_content) ?? _grab);
        }

        private void Finish()
        {
            if (_layer == null) return;
            _closeTimer?.Pause();
            _closeTimer = null;
            _focus.End();
            _focus = null;
            _layer.RemoveFromHierarchy();
            _layer = null;
            _scrim.RemoveFromHierarchy();
            RemoveFromHierarchy();
            _closing = false;
            Closed?.Invoke();
        }

        private void SetVisible(float fraction)
        {
            _visible = fraction;
            style.translate = new StyleTranslate(new Translate(0f, Length.Percent((1f - fraction) * 100f)));
        }

        // ---- scrim, back, keys ----

        private void OnScrimPointerDown(PointerDownEvent evt)
        {
            if (Dismissable) Close();
            evt.StopPropagation();
        }

        private void OnCancel(NavigationCancelEvent evt)
        {
            var panel = this.panel;
            CancelOncePerFrame();
            evt.StopPropagation();
            panel?.focusController.IgnoreEvent(evt);
        }

        private void OnKeyDown(KeyDownEvent evt)
        {
            if (evt.keyCode != KeyCode.Escape) return;
            CancelOncePerFrame();
            evt.StopPropagation();
        }

        private void CancelOncePerFrame()
        {
            if (_lastCancelFrame == Time.frameCount) return;
            _lastCancelFrame = Time.frameCount;
            HandleBack();
        }

        private void OnGrabMoveKey(NavigationMoveEvent evt)
        {
            if (evt.direction != NavigationMoveEvent.Direction.Up && evt.direction != NavigationMoveEvent.Direction.Down) return;
            evt.StopPropagation();
            panel?.focusController.IgnoreEvent(evt);
            if (evt.direction == NavigationMoveEvent.Direction.Up)
            {
                SnapTo(_snapIndex + 1);
            }
            else if (_snapIndex > 0)
            {
                SnapTo(_snapIndex - 1);
            }
            else if (Dismissable)
            {
                Close();
            }
        }

        // ---- dragging ----

        private void OnGrabDown(PointerDownEvent evt)
        {
            if (!IsOpen || _dragPointer >= 0 || evt.button != 0) return;
            _dragPointer = evt.pointerId;
            _grab.CapturePointer(_dragPointer);
            _dragStartY = _lastY = evt.position.y;
            _dragStartVisible = _visible;
            _lastTime = PopupHost.Now;
            _velocity = 0f;
            AddToClassList(InstantClass);
            evt.StopPropagation();
        }

        private void OnGrabMove(PointerMoveEvent evt)
        {
            if (evt.pointerId != _dragPointer) return;
            var height = resolvedStyle.height;
            if (height <= 0f || float.IsNaN(height)) return;

            var y = evt.position.y;
            var now = PopupHost.Now;
            var fraction = Mathf.Clamp(_dragStartVisible - (y - _dragStartY) / height, 0f, _snapPoints[_snapPoints.Count - 1]);
            var dt = (float)(now - _lastTime);
            if (dt > 0.0005f)
            {
                var instant = -(y - _lastY) / height / dt;
                _velocity = _velocity * 0.6f + instant * 0.4f;
                _lastY = y;
                _lastTime = now;
            }

            SetVisible(fraction);
            evt.StopPropagation();
        }

        private void OnGrabUp(PointerUpEvent evt)
        {
            if (evt.pointerId != _dragPointer) return;
            _grab.ReleasePointer(evt.pointerId); // raises the capture-out event, which ends the drag
            EndDrag();
        }

        private void OnGrabCaptureOut(PointerCaptureOutEvent evt)
        {
            if (evt.pointerId == _dragPointer) EndDrag();
        }

        private void EndDrag()
        {
            if (_dragPointer < 0) return;
            _dragPointer = -1;
            if (!IsOpen) return;
            var result = SheetSnap.Resolve(_visible, _velocity, _snapPoints, Dismissable);
            if (result.Dismiss) Close();
            else SnapTo(result.Index);
        }
    }
}
