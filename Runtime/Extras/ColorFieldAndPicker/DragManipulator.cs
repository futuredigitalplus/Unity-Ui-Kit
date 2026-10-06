// FutureDigitalPlus: renamed by Tools~/rename/rename.py (namespaces, resource paths); see SOURCE.md "Rename record".
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace FutureDigitalPlus.UIKit.Extras
{
	public class DragManipulator : Clickable
	{
        public bool FreeMoving { get; set; }
        public Vector2 StartMousePosition { get; set; }
        public Vector2 Delta => (lastMousePosition - StartMousePosition);

        private event System.Action onDragging;

        public DragManipulator(System.Action clickHandler, System.Action dragHandler)
            : base(clickHandler, 250, 30)
        {
            FreeMoving = false;
            onDragging += dragHandler;
        }

        // FutureDigitalPlus: upstream re-registered its own copies of Clickable's pointer handlers here
        // (RegisterCallbacksOnTarget / UnregisterCallbacksFromTarget, OnPointerDown / OnPointerMove / OnPointerUp /
        // OnPointerCancel and IsNotMouseEvent). Unity 6's Clickable already has identical protected handlers, so the
        // copies hid the base members (warning CS0108) and ran twice for touch input. They were removed; the
        // ProcessDownEvent / ProcessMoveEvent overrides below are still called by Clickable's own handlers.

        protected override void ProcessDownEvent(EventBase evt, Vector2 localPosition, int pointerId)
        {
            FreeMoving = false;
            StartMousePosition = localPosition;
            base.ProcessDownEvent(evt, localPosition, pointerId);
        }

        protected override void ProcessMoveEvent(EventBase evt, Vector2 localPosition)
        {
            FreeMoving = true;

            base.ProcessMoveEvent(evt, localPosition);

            if (evt.eventTypeId == PointerMoveEvent.TypeId())
            {
#pragma warning disable CS0618 // FutureDigitalPlus: PreventDefault is obsolete in Unity 6; kept to preserve the upstream behaviour
                evt.PreventDefault();
#pragma warning restore CS0618
            }

            onDragging?.Invoke();
        }

        // ============================================================================================================
    }
}