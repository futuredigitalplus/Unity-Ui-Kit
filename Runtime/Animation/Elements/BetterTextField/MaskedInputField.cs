// FutureDigitalPlus: renamed by Tools~/rename/rename.py (namespaces, resource paths); see SOURCE.md "Rename record".
using System;
using System.Collections.Generic;
using System.Linq;
// FutureDigitalPlus: removed 'using JetBrains.Annotations;' (IDE annotation only).
using UnityEngine;
using UnityEngine.UIElements;
using FutureDigitalPlus.UIKit.Animation.Extensions;

// FutureDigitalPlus: upstream wrapped this whole file in '#if UNITY_EDITOR' (editor-only toolkit). Guard removed so the code also works at runtime / in player builds.
namespace FutureDigitalPlus.UIKit.Animation
{
    // Hopefully these features will eventually be in the default TextField.
    // FutureDigitalPlus: now 'partial' + [UxmlElement] (legacy UxmlFactory/UxmlTraits are obsolete in Unity 6).
    [UxmlElement]
    public partial class MaskedInputField : TextField
    {
        /// <summary>
        /// USS class name of elements of this type.
        /// </summary>
        public const string UssClassName = "unity-masked-text-field";

        /// <summary>
        /// USS class name of placeholder elements of this type.
        /// </summary>
        public const string PlaceholderUssClassName = UssClassName + "__placeholder";

        StyleSheet k_StylePath;
        private Type editorType;

        readonly Label m_PlaceholderLabel;
        private VisualElement maskedInput;
        public TextField maskedText;
        private Font monoFont;

        /// <summary>
        /// FutureDigitalPlus: upstream looked up the 'SourceCodePro-Medium' font with AssetDatabase (editor only) and shipped the font files.
        /// The fonts are not part of this package. Assign a monospaced font here before creating fields to get the upstream look;
        /// when it is null the field uses the default panel font.
        /// </summary>
        public static Font MonoFont;

        public int prefixNum = 2;
        public int suffixNum = 9;
        public bool usePrivacyMask = false;

        /// <summary>
        /// Notify external subscribers that value of text property changed.
        /// </summary>
        public Action<string> OnValueChangedHandler;

        public MaskedInputField()
        {
            // FutureDigitalPlus: font now comes from the static MonoFont field (see above) instead of AssetDatabase.
            if (monoFont == null) monoFont = MonoFont;

            AddToClassList(UssClassName);
            editorType = GetType();
            styleSheets.Add(StylesheetSetup());
            if (monoFont != null) style.unityFont = monoFont;

            var textInputBase = this.Q("unity-text-input");
            VisualElementExtension.NameAsUSS(textInputBase, nameof(textInputBase));
            textInputBase.style.display = DisplayStyle.Flex;
            textInputBase.style.color = new StyleColor(StyleKeyword.Initial);
            VisualElementExtension.SetBorderWidth(textInputBase, 2);

            maskedText = new TextField
            {
                style = { display = DisplayStyle.Flex, position = Position.Absolute },
                value = "",
                pickingMode = PickingMode.Ignore,
            };
            if (monoFont != null) maskedText.style.unityFont = monoFont; // FutureDigitalPlus: only when a font is available
            maskedInput = maskedText.Q("unity-text-input");
            maskedInput.pickingMode = PickingMode.Ignore;
            VisualElementExtension.NameAsUSS(maskedInput, nameof(maskedInput));


            VisualElementExtension.NameAsUSS(maskedText, nameof(maskedText));
            maskedText.AddToClassList(UssClassName);
            Add(maskedText);

            // FutureDigitalPlus: PointerUpEvent instead of MouseUpEvent, so a finger or a pen places the caret too (pointer events are raised for every pointer device; mouse events need not be raised for touch).
            this.RegisterCallback<PointerUpEvent>(evt =>
            {
                VisualElementExtension.SelectRangeDelayed(this, maskedText.text.Length, maskedText.text.Length);
                evt.StopPropagation();
            });

            // Add and configure placeholder
            m_PlaceholderLabel = new Label { pickingMode = PickingMode.Ignore };
            m_PlaceholderLabel.AddToClassList(PlaceholderUssClassName);
            maskedText.Add(m_PlaceholderLabel);

            RegisterCallback<FocusInEvent>(e => HidePlaceholder());
            RegisterCallback<FocusOutEvent>(e =>
            {
                if (string.IsNullOrEmpty(text))
                {
                    ShowPlaceholder();
                }
            });

            this.RegisterCallback<GeometryChangedEvent>(DeferredExecution);
            this.RegisterValueChangedCallback(e => OnValueChangedHandler?.Invoke(e.newValue));
        }

        private void DeferredExecution(GeometryChangedEvent evt)
        {
            this.RegisterCallback<GeometryChangedEvent>(DeferredExecution);

            this.style.color = new StyleColor(StyleKeyword.Initial);
            maskedText.style.width = this.resolvedStyle.width;
            maskedText.style.height = this.resolvedStyle.height;
            maskedText.style.right = -4;
            maskedText.style.top = -3;
            maskedText.style.backgroundColor = new StyleColor(StyleKeyword.Initial);
            VisualElementExtension.SetBorderWidth(maskedText, 2);

            maskedInput.BringToFront();
        }

        private StyleSheet StylesheetSetup()
        {
            // FutureDigitalPlus: upstream used AssetDatabase (editor only, via GetStyleSheet). The stylesheet is now loaded from a Resources
            // folder: Elements/BetterTextField/Resources/FutureDigitalPlus/UIKit/Animation/Styles/MaskedInputField.uss -> Resources path "FutureDigitalPlus/UIKit/Animation/Styles/MaskedInputField".
            if (k_StylePath == null)
                k_StylePath = Resources.Load<StyleSheet>("FutureDigitalPlus/UIKit/Animation/Styles/MaskedInputField");
            if (k_StylePath is null) Debug.LogError($"{editorType.Name} Stylesheet not found");
            return k_StylePath;
        }

        void UpdatePlaceholderVisibility()
        {
            if (string.IsNullOrEmpty(value))
            {
                if (focusController?.focusedElement != this)
                {
                    ShowPlaceholder();
                }
            }
            else
            {
                HidePlaceholder();
            }
        }

        void HidePlaceholder()
        {
            m_PlaceholderLabel?.AddToClassList("hidden");
        }

        void ShowPlaceholder()
        {
            m_PlaceholderLabel?.RemoveFromClassList("hidden");
        }

        public override string value
        {
            get => base.value;
            set
            {
                base.value = value;
                if (value != null)
                {
                    var privacyMask = ApplyPrivacyMask(value);
                    maskedText.SetValueWithoutNotify(privacyMask.Item2);
                }

                UpdatePlaceholderVisibility();
            }
        }

        // FutureDigitalPlus: [UxmlAttribute("placeholder")] replaces the 'placeholder' attribute of the old UxmlTraits.
        [UxmlAttribute("placeholder")]
        public string Placeholder
        {
            get => m_PlaceholderLabel.text;
            set => m_PlaceholderLabel.text = value;
        }

        /// <summary>
        /// Creates a privacy string in which characters at either the beginning or end are shown as asterisks ( * )
        /// Ex. 0123456 -> **2345*
        /// </summary>
        /// <param name="exposedString">The string in which to apply the privacy mask</param>
        /// <returns>Returns a Tuple value in which Item1 is the original string, Item2 is the privacy masked string</returns>
        public (string, string) ApplyPrivacyMask(string exposedString)
        {
            if (exposedString.Length <= 0) return ("", "");
            List<string> tmpString = new List<string>();
            for (int i = 0; i < exposedString.Length; i++)
            {
                tmpString.Add(i < prefixNum || i > suffixNum ? exposedString[i].ToString() : "*");
            }

            return (exposedString, tmpString.Aggregate((i, j) => i + j));
        }

        public override void SetValueWithoutNotify(string newValue)
        {
            base.SetValueWithoutNotify(newValue);
            UpdatePlaceholderVisibility();
        }

        // FutureDigitalPlus: removed the obsolete UxmlFactory / UxmlTraits classes (see [UxmlElement] / [UxmlAttribute] above).
    }
}
