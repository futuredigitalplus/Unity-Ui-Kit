// FutureDigitalPlus: renamed by Tools~/rename/rename.py (namespaces, resource paths); see SOURCE.md "Rename record".
// FutureDigitalPlus: the colour swatch can take keyboard and gamepad focus and opens the picker on Submit; see SOURCE.md.
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace FutureDigitalPlus.UIKit.Extras
{
	public class ColorField : BaseField<Color>
	{
#pragma warning disable CS0618 // FutureDigitalPlus: UxmlFactory/UxmlTraits are deprecated in Unity 6 but still work; kept so upstream code stays diffable (see SOURCE.md)
		[UnityEngine.Scripting.Preserve]
		public new class UxmlFactory : UxmlFactory<ColorField, UxmlTraits> { }

		[UnityEngine.Scripting.Preserve]
		public new class UxmlTraits : BaseFieldTraits<Color, UxmlColorAttributeDescription>
		{
			private readonly UxmlStringAttributeDescription resetLabel = new UxmlStringAttributeDescription { name = "reset-label", defaultValue = null };

			public override void Init(VisualElement ve, IUxmlAttributes bag, CreationContext cc)
			{
				base.Init(ve, bag, cc);
				var item = ve as ColorField;
				item.resetLabel = resetLabel.GetValueFromBag(bag, cc);
				item.UpdateResetButton();
			}
		}
#pragma warning restore CS0618

		public event System.Action ResetButtonPressed;
		public ColorPopup ColorPopup { get; set; }

		private string resetLabel = null;
		private ColorFieldInput colorFieldInput;
		private Button resetButton;

		private const string stylesResource = "FutureDigitalPlus/UIKit/Extras/Styles/ColorFieldStyleSheet";
		private const string ussFieldName = "fdp-color-field";
		private const string ussFieldLabel = "fdp-color-field__label";
		private const string ussFieldResetButton = "fdp-color-field__reset-button";

		// ------------------------------------------------------------------------------------------------------------

		public ColorField()
			: this(null, null, new ColorFieldInput())
		{ }

		public ColorField(string label, string resetLabel = null)
			: this(label, resetLabel, new ColorFieldInput())
		{ }

		private ColorField(string label, string resetLabel, ColorFieldInput colorFieldInput)
			: base(label, colorFieldInput)
		{
			this.colorFieldInput = colorFieldInput;
			this.resetLabel = resetLabel;

			styleSheets.Add(Resources.Load<StyleSheet>(stylesResource));
			AddToClassList(ussFieldName);

			labelElement.AddToClassList(ussFieldLabel);

			colorFieldInput.RegisterCallback<ClickEvent>(OnClickEvent);
			// FutureDigitalPlus: Enter and the gamepad south button raise NavigationSubmitEvent, not ClickEvent, and a plain element is not focusable.
			colorFieldInput.focusable = true;
			colorFieldInput.RegisterCallback<NavigationSubmitEvent>(OnSubmitEvent);

			UpdateResetButton();

			RegisterCallback<GeometryChangedEvent>(OnGeometryChangedEvent);
		}

		public override void SetValueWithoutNotify(Color newValue)
		{
			base.SetValueWithoutNotify(newValue);
			colorFieldInput.SetColor(newValue);
		}

		private void UpdateResetButton()
		{
			if (resetLabel == null)
			{
				if (resetButton != null)
				{
					Remove(resetButton);
					resetButton = null;
				}
			}
			else
			{
				if (resetButton == null)
				{
					resetButton = new Button();
					resetButton.AddToClassList(ussFieldResetButton);
					resetButton.clicked += OnResetButton;
					Add(resetButton);
				}
				resetButton.text = resetLabel;
			}
		}

		private void OnGeometryChangedEvent(GeometryChangedEvent ev)
		{
			UnregisterCallback<GeometryChangedEvent>(OnGeometryChangedEvent);
			colorFieldInput.SetColor(value);
		}

		private void OnClickEvent(ClickEvent ev)
		{
			ColorPopup?.Show(value, c => value = c);
		}

		private void OnSubmitEvent(NavigationSubmitEvent ev)
		{
			ColorPopup?.Show(value, c => value = c);
			ev.StopPropagation();
		}

		private void OnResetButton()
		{
			ResetButtonPressed?.Invoke();
		}

		// ============================================================================================================

		private class ColorFieldInput : VisualElement
		{
			public VisualElement rgbField;
			public VisualElement alphaField;

			private const string ussFieldInput = "fdp-color-field__input";
			private const string ussFieldInputRGB = "fdp-color-field__input-rgb";
			private const string ussFieldInputAlpha = "fdp-color-field__input-alpha";
			private const string ussFieldInputAlphaContainer = "fdp-color-field__input-alpha-container";

			public ColorFieldInput()
			{
				AddToClassList(ussFieldInput);

				rgbField = new VisualElement();
				rgbField.AddToClassList(ussFieldInputRGB);
				Add(rgbField);

				var alphaFieldContainer = new VisualElement();
				alphaFieldContainer.AddToClassList(ussFieldInputAlphaContainer);
				Add(alphaFieldContainer);

				alphaField = new VisualElement();
				alphaField.AddToClassList(ussFieldInputAlpha);
				alphaFieldContainer.Add(alphaField);
			}

			public void SetColor(Color color)
			{
				rgbField.style.backgroundColor = new Color(color.r, color.g, color.b, 1f);
				alphaField.style.width = alphaField.parent.resolvedStyle.width * color.a;
			}
		}

		// ============================================================================================================
	}
}