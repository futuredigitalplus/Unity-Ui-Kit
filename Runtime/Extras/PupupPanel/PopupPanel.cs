// FutureDigitalPlus: renamed by Tools~/rename/rename.py (namespaces, resource paths); see SOURCE.md "Rename record".
// FutureDigitalPlus: the popup keeps keyboard and gamepad focus inside itself while open, returns focus when it closes, closes on Cancel (Escape, gamepad B)
// and can register with a back-key dispatcher through BackRegistrar; see SOURCE.md.
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace FutureDigitalPlus.UIKit.Extras
{
	public class PopupPanel : VisualElement
	{
#pragma warning disable CS0618 // FutureDigitalPlus: UxmlFactory/UxmlTraits are deprecated in Unity 6 but still work; kept so upstream code stays diffable (see SOURCE.md)
		[UnityEngine.Scripting.Preserve]
		public new class UxmlFactory : UxmlFactory<PopupPanel, UxmlTraits> { }

		[UnityEngine.Scripting.Preserve]
		public new class UxmlTraits : VisualElement.UxmlTraits
		{
			private readonly UxmlBoolAttributeDescription startVisible = new UxmlBoolAttributeDescription { name = "start-visible", defaultValue = false };
			private readonly UxmlIntAttributeDescription fadeTime = new UxmlIntAttributeDescription { name = "fade-time", defaultValue = 30 };

			public override void Init(VisualElement ve, IUxmlAttributes bag, CreationContext cc)
			{
				base.Init(ve, bag, cc);

				var item = ve as PopupPanel;
				var vis = startVisible.GetValueFromBag(bag, cc);
				item.FadeTime = fadeTime.GetValueFromBag(bag, cc);

				item.SetStartVisibility(vis);
			}
		}
#pragma warning restore CS0618

		public int FadeTime { get; private set; } = 30;

		/// <summary>
		/// Registers a close action with a back-key dispatcher (Android back, Escape, gamepad B on a screen where nothing has focus). Receives the action to
		/// run when back is pressed and returns the registration to dispose when the popup closes. Null (the default) means the popup only closes on Cancel while it
		/// holds focus. The facade assigns it; the Extras module itself references no dispatcher.
		/// </summary>
		public static Func<Action, IDisposable> BackRegistrar { get; set; }

		[RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
		private static void ResetOnLoad() => BackRegistrar = null;

		public override VisualElement contentContainer => mainPanel;

		protected BlurPanel mainPanel;
		protected IVisualElementScheduledItem task;

		private VisualElement focusBeforeShow;
		private IDisposable backRegistration;
		private bool isOpen;

		private const string stylesResource = "FutureDigitalPlus/UIKit/Extras/Styles/PopupPanelStyleSheet";
		private const string ussClassName = "fdp-popup-panel";
		private const string ussContainer = ussClassName + "__container";

		// ------------------------------------------------------------------------------------------------------------

		public PopupPanel()
		{
			styleSheets.Add(Resources.Load<StyleSheet>(stylesResource));
			AddToClassList(ussContainer);
			pickingMode = PickingMode.Position;

			// panel
			mainPanel = new BlurPanel();
			mainPanel.pickingMode = PickingMode.Position;
			mainPanel.focusable = true;
			mainPanel.AddToClassList(ussClassName);
			mainPanel.RegisterCallback<NavigationCancelEvent>(OnCancel);
			mainPanel.RegisterCallback<FocusOutEvent>(OnPanelFocusOut);
			hierarchy.Add(mainPanel);

			SetStartVisibility(false);
		}

		// ------------------------------------------------------------------------------------------------------------

		public void SetBlurSource(Texture texture)
		{
			mainPanel.SetImage(texture);
		}

		public virtual void Show()
		{
			task?.Pause();
			task = null;

			if (FadeTime > 0.0f)
			{
				style.visibility = Visibility.Visible;
				style.opacity = 0f;
				task = schedule
					.Execute(() => style.opacity = Mathf.Clamp01(resolvedStyle.opacity + 0.1f))
					.Every(FadeTime) // ms	
					.Until(() => resolvedStyle.opacity >= 1.0f);
			}
			else
			{
				style.visibility = Visibility.Visible;
				style.opacity = 1f;
			}

			if (!isOpen)
			{
				isOpen = true;
				focusBeforeShow = panel?.focusController.focusedElement as VisualElement;
				backRegistration = BackRegistrar?.Invoke(Hide);
			}

			mainPanel.Focus();

			// FutureDigitalPlus: a popup that was hidden a moment ago has not resolved its inherited visibility yet, and an element cannot take
			// focus until it has (the call above then drops focus instead). Try again until it can, for a few frames at most.
			if (!HasFocusInside())
			{
				var attempts = 0;
				mainPanel.schedule
					.Execute(() =>
					{
						attempts++;
						if (isOpen && mainPanel.canGrabFocus) mainPanel.Focus();
					})
					.Every(16)
					.Until(() => !isOpen || mainPanel.panel == null || HasFocusInside() || attempts >= 20);
			}
		}

		private bool HasFocusInside()
		{
			var focused = mainPanel.panel?.focusController.focusedElement as VisualElement;
			return focused != null && (focused == mainPanel || mainPanel.Contains(focused));
		}

		public virtual void Hide()
		{
			task?.Pause();
			task = null;

			if (isOpen)
			{
				isOpen = false;
				backRegistration?.Dispose();
				backRegistration = null;
			}
			
			mainPanel.Blur();
			var previous = focusBeforeShow;
			focusBeforeShow = null;
			if (previous != null && previous.panel != null && previous.canGrabFocus) previous.Focus();

			if (FadeTime > 0.0f)
			{
				task = schedule
					.Execute(() =>
					{
						var o = Mathf.Clamp01(resolvedStyle.opacity - 0.1f);
						style.opacity = o;
						if (o <= 0.0f) style.visibility = Visibility.Hidden;
					})
					.Every(FadeTime) // ms	
					.Until(() => resolvedStyle.opacity <= 0.0f);
			}
			else
			{
				style.visibility = Visibility.Hidden;
				style.opacity = 0f;
			}
		}

		private void OnCancel(NavigationCancelEvent evt)
		{
			if (!isOpen) return;
			Hide();
			evt.StopPropagation();
		}

		// While open, focus cannot leave the popup: UI Toolkit's focus ring spans the whole panel, so Tab or the D-pad would otherwise walk onto
		// the page behind it.
		private void OnPanelFocusOut(FocusOutEvent evt)
		{
			if (!isOpen) return;
			if (evt.relatedTarget is VisualElement next && mainPanel.Contains(next)) return;
			mainPanel.schedule.Execute(() =>
			{
				if (!isOpen || mainPanel.panel == null) return;
				var focused = mainPanel.panel.focusController.focusedElement as VisualElement;
				if (focused == null || !mainPanel.Contains(focused)) mainPanel.Focus();
			});
		}

		protected void SetStartVisibility(bool isVisible)
		{
			if (isVisible)
			{
				style.visibility = Visibility.Visible;
				style.opacity = 1f;
			}
			else
			{
				style.visibility = Visibility.Hidden;
				style.opacity = 0f;
			}
		}

		// ============================================================================================================
	}
}