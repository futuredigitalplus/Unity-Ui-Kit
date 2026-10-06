// FutureDigitalPlus: renamed by Tools~/rename/rename.py (namespaces, resource paths); see SOURCE.md "Rename record".
using UnityEngine;
using UnityEngine.UIElements;

namespace FutureDigitalPlus.UIKit.Extras
{
	public class Tooltip : VisualElement
	{
#pragma warning disable CS0618 // FutureDigitalPlus: UxmlFactory/UxmlTraits are deprecated in Unity 6 but still work; kept so upstream code stays diffable (see SOURCE.md)
		[UnityEngine.Scripting.Preserve]
		public new class UxmlFactory : UxmlFactory<Tooltip, UxmlTraits> { }

		[UnityEngine.Scripting.Preserve]
		public new class UxmlTraits : VisualElement.UxmlTraits
		{
			private readonly UxmlIntAttributeDescription delay = new UxmlIntAttributeDescription { name = "delay", defaultValue = 500 };
			private readonly UxmlIntAttributeDescription fadeTime = new UxmlIntAttributeDescription { name = "fade-time", defaultValue = 15 };

			public override void Init(VisualElement ve, IUxmlAttributes bag, CreationContext cc)
			{
				base.Init(ve, bag, cc);
				var item = ve as Tooltip;

				item.Delay = delay.GetValueFromBag(bag, cc);
				item.FadeTime = fadeTime.GetValueFromBag(bag, cc);
			}
		}
#pragma warning restore CS0618

		public int Delay { get; private set; } = 500;	// how long to wait before appearing
		public int FadeTime { get; private set; } = 15; // speed at which it fades in and out (lower = faster)

		private Label label;
		private IVisualElementScheduledItem task;

		private const string stylesResource = "FutureDigitalPlus/UIKit/Extras/Styles/TooltipStyleSheet";
		private const string ussClassName = "fdp-tooltip";
		private const string ussLabel = ussClassName + "__label";

		// ------------------------------------------------------------------------------------------------------------

		public Tooltip()
		{
			styleSheets.Add(Resources.Load<StyleSheet>(stylesResource));
			AddToClassList(ussClassName);
			pickingMode = PickingMode.Ignore;

			// label
			label = new Label { pickingMode = PickingMode.Ignore };
			label.AddToClassList(ussLabel);
			Add(label);

			style.position = Position.Absolute;
			style.visibility = Visibility.Hidden;
			style.opacity = 0f;
		}

		public virtual void Show(VisualElement target)
		{
			// FutureDigitalPlus: a target without text (its tooltip was cleared, or it never had one) shows nothing; upstream showed an empty bubble.
			if (target == null || string.IsNullOrEmpty(target.tooltip))
			{
				Close();
				return;
			}

			if (FadeTime > 0.0f)
			{
				task?.Pause();
				style.visibility = Visibility.Visible;
				style.opacity = 0f;
				task = schedule
					.Execute(() => style.opacity = Mathf.Clamp01(resolvedStyle.opacity + 0.1f))
					.Every(FadeTime) // ms	
					.Until(() => resolvedStyle.opacity >= 1.0f)
					.StartingIn(Delay);
			}
			else
			{
				style.visibility = Visibility.Visible;
				style.opacity = 1f;
			}


			// check if there is position hint in tooltip
			char hint = 'B';
			var msg = target.tooltip;
			if (msg.Length > 2 && msg[1] == ':')
			{
				hint = target.tooltip[0] switch
				{
					'B' => 'B',
					'b' => 'B',
					'T' => 'T',
					't' => 'T',
					'L' => 'L',
					'l' => 'L',
					'R' => 'R',
					'r' => 'R',
					_ => 'B'
				};

				msg = msg.Substring(2);
			}

			label.text = msg;

			// FutureDigitalPlus: placement is computed in the local space of the tooltip's parent (WorldToLocal and layout sizes) instead of worldBound.
			// In a world-space panel worldBound is in world units, not panel pixels, so the upstream numbers do not fit style.left and style.top there; on a screen
			// panel whose parent sits at the origin the results are identical.
			if (parent == null) return;
			var targetRect = parent.WorldToLocal(target.worldBound);
			var width = float.IsNaN(layout.width) ? 0f : layout.width;
			var height = float.IsNaN(layout.height) ? 0f : layout.height;

			float top=0f, left=0f;
			switch (hint)
			{
				case 'L': // left
					left = targetRect.xMin - width - 5;
					top = targetRect.center.y - (height * 0.5f);
					break;
				case 'R': // right
					left = targetRect.xMax + 5;
					top = targetRect.center.y - (height * 0.5f);
					break;
				case 'T': //top
					left = targetRect.center.x - (width * 0.5f);
					top = targetRect.yMin - height - 5;
					break;
				default: // bottom
					left = targetRect.center.x - (width * 0.5f);
					top = targetRect.yMax + 5;
					break;
			}

			var parentWidth = parent.layout.width;
			var parentHeight = parent.layout.height;
			if (!float.IsNaN(parentWidth) && !float.IsNaN(parentHeight))
			{
				if (left < 0f) left = 0f;
				if (left + width > parentWidth) left = parentWidth - width;
				if (top < 0f) top = 0f;
				if (top + height > parentHeight) top = parentHeight - height;
			}

			style.left = left;
			style.top = top;
		}

		public virtual void Close()
		{
			if (FadeTime > 0.0f && resolvedStyle.visibility != Visibility.Hidden)
			{
				task?.Pause();
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

		// ============================================================================================================
	}
}