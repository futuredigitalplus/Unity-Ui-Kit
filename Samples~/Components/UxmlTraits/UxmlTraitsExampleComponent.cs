// FutureDigitalPlus: renamed by Tools~/rename/rename.py (namespaces, resource paths); see SOURCE.md "Rename record".
using UnityEngine;
using UnityEngine.UIElements;

namespace FutureDigitalPlus.UIKit.Components.Samples.UxmlTraits
{
    /// <summary>
    /// An example component which uses UxmlName and UxmlTrait.
    /// The component is instantiated with the name "UxmlTraitsExample" in UXML,
    /// and the UxmlTrait attributes generate the necessary code for
    /// customizing this component in UXML.
    /// </summary>
    [UxmlName("UxmlTraitsExample")]
    [Layout("FutureDigitalPlus/UIKit/Samples/Components/UxmlTraits/UxmlTraitsExampleComponent")]
    [Stylesheet("FutureDigitalPlus/UIKit/Samples/Components/UxmlTraits/UxmlTraitsExampleComponent.style")]
    public partial class UxmlTraitsExampleComponent : UIComponent
    {
        [UxmlTrait]
        public string DescriptionText
        {
            set
            {
                _descriptionText = value;
                if (_descriptionLabel != null)
                    _descriptionLabel.text = value;
            }
        }
        private string _descriptionText;

        [UxmlTrait]
        private Color _buttonColor;
        
        [Query("uxml-traits-button")]
        private Button _button;

        [Query("uxml-traits-description-label")]
        private Label _descriptionLabel;

        public override void OnInit()
        {
            if (!string.IsNullOrEmpty(_descriptionText))
                _descriptionLabel.text = _descriptionText;

            _button.style.backgroundColor = _buttonColor;
        }
    }
}
