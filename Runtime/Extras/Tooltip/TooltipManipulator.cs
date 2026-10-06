// FutureDigitalPlus: renamed by Tools~/rename/rename.py (namespaces, resource paths); see SOURCE.md "Rename record".
// FutureDigitalPlus: tooltips also show for keyboard and gamepad focus (in navigation mode only) and for a long press on touch; see SOURCE.md.
using UnityEngine;
using UnityEngine.UIElements;

namespace FutureDigitalPlus.UIKit.Extras
{
	/// <summary>
	/// Shows a <see cref="Tooltip"/> for its target: on mouse hover (upstream behaviour), while the target has focus when the last input device was a
	/// keyboard or a gamepad, and after a long press with a finger. The navigation mode is read from the root class <see cref="NavigationClass"/>, which
	/// the Interaction module's <c>InputProfile</c> writes; without that class (a game that does not use the module) focus never shows a tooltip.
	/// </summary>
	public class TooltipManipulator : Manipulator
	{
		/// <summary>The root class that marks keyboard or gamepad use. Written by <c>InputProfile</c>; any game code may set it.</summary>
		public const string NavigationClass = "fdp-input-nav";

		/// <summary>How long a finger must stay down before the tooltip shows, in milliseconds.</summary>
		public int LongPressDelay { get; set; } = 500;

		/// <summary>How long a tooltip that a long press opened stays after the finger lifted, in milliseconds.</summary>
		public int LongPressLinger { get; set; } = 1500;

		private const float LongPressSlop = 10f;

		private Tooltip tooltip;
		private IVisualElementScheduledItem pressTask;
		private IVisualElementScheduledItem lingerTask;
		private Vector2 pressStart;
		private bool shownByPress;
		private bool showing;   // FutureDigitalPlus: this manipulator asked the shared tooltip to show, so it must also be the one to close it when its target goes away

		public TooltipManipulator(Tooltip tooltip)
		{
			this.tooltip = tooltip;
		}

		protected override void RegisterCallbacksOnTarget()
		{
			// FutureDigitalPlus: the pointer callbacks run in the trickle-down phase. A Button's own Clickable stops the propagation of a
			// PointerDown at the target, so a callback registered for the bubble phase afterwards never saw a press on a button.
			target.RegisterCallback<PointerEnterEvent>(MouseIn);
			target.RegisterCallback<PointerOutEvent>(MouseOut);
			target.RegisterCallback<FocusInEvent>(FocusIn);
			target.RegisterCallback<FocusOutEvent>(FocusOut);
			target.RegisterCallback<PointerDownEvent>(PointerDown, TrickleDown.TrickleDown);
			target.RegisterCallback<PointerMoveEvent>(PointerMove, TrickleDown.TrickleDown);
			target.RegisterCallback<PointerUpEvent>(PointerUp, TrickleDown.TrickleDown);
			target.RegisterCallback<PointerCancelEvent>(PointerCancel, TrickleDown.TrickleDown);
			target.RegisterCallback<DetachFromPanelEvent>(Detach);
		}

		protected override void UnregisterCallbacksFromTarget()
		{
			target.UnregisterCallback<PointerEnterEvent>(MouseIn);
			target.UnregisterCallback<PointerOutEvent>(MouseOut);
			target.UnregisterCallback<FocusInEvent>(FocusIn);
			target.UnregisterCallback<FocusOutEvent>(FocusOut);
			target.UnregisterCallback<PointerDownEvent>(PointerDown, TrickleDown.TrickleDown);
			target.UnregisterCallback<PointerMoveEvent>(PointerMove, TrickleDown.TrickleDown);
			target.UnregisterCallback<PointerUpEvent>(PointerUp, TrickleDown.TrickleDown);
			target.UnregisterCallback<PointerCancelEvent>(PointerCancel, TrickleDown.TrickleDown);
			target.UnregisterCallback<DetachFromPanelEvent>(Detach);
			CancelPress();
			lingerTask?.Pause();
			lingerTask = null;
		}

		// FutureDigitalPlus: hover is a mouse thing. UI Toolkit also reports the compatibility MouseEnterEvent and MouseOutEvent for a finger that
		// touches the target, which showed the tooltip at once on every tap and hid it again when the finger lifted, before the long press
		// below could do its job. The pointer events say which kind of pointer it is.
		private void MouseIn(PointerEnterEvent e)
		{
			if (e.pointerType != UnityEngine.UIElements.PointerType.mouse) return;
			Show();
		}

		private void MouseOut(PointerOutEvent e)
		{
			if (e.pointerType != UnityEngine.UIElements.PointerType.mouse) return;
			Hide();
		}

		private void FocusIn(FocusInEvent e)
		{
			if (string.IsNullOrEmpty(target.tooltip) || !InNavigationMode()) return;
			Show();
		}

		private void FocusOut(FocusOutEvent e)
		{
			Hide();
		}

		private void PointerDown(PointerDownEvent e)
		{
			if (e.pointerType != UnityEngine.UIElements.PointerType.touch || string.IsNullOrEmpty(target.tooltip)) return;
			CancelPress();
			lingerTask?.Pause();
			pressStart = (Vector2)e.position;
			pressTask = target.schedule.Execute(() =>
			{
				pressTask = null;
				shownByPress = true;
				Show();
			}).StartingIn(LongPressDelay);
		}

		private void PointerMove(PointerMoveEvent e)
		{
			if (pressTask != null && ((Vector2)e.position - pressStart).sqrMagnitude > LongPressSlop * LongPressSlop) CancelPress();
		}

		private void PointerUp(PointerUpEvent e)
		{
			CancelPress();
			if (!shownByPress) return;
			shownByPress = false;
			lingerTask?.Pause();
			lingerTask = target.schedule.Execute(Hide).StartingIn(LongPressLinger);
		}

		private void PointerCancel(PointerCancelEvent e)
		{
			CancelPress();
			if (!shownByPress) return;
			shownByPress = false;
			Hide();
		}

		// FutureDigitalPlus: a target that leaves the panel while its tooltip is up (a screen change, a destroyed widget) never gets its PointerOut or FocusOut.
		private void Detach(DetachFromPanelEvent e)
		{
			CancelPress();
			lingerTask?.Pause();
			lingerTask = null;
			shownByPress = false;
			if (showing) Hide();   // only when this manipulator's target had the tooltip up: the tooltip is shared
		}

		private void Show()
		{
			showing = true;
			tooltip.Show(target);
		}

		private void Hide()
		{
			showing = false;
			tooltip.Close();
		}

		private void CancelPress()
		{
			pressTask?.Pause();
			pressTask = null;
		}

		private bool InNavigationMode()
		{
			for (var element = target; element != null; element = element.parent)
				if (element.ClassListContains(NavigationClass)) return true;
			return false;
		}

		// ============================================================================================================
	}
}