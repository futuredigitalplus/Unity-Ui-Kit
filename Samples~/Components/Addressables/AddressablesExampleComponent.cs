// FutureDigitalPlus: renamed by Tools~/rename/rename.py (namespaces, resource paths); see SOURCE.md "Rename record".
using FutureDigitalPlus.UIKit.Components.Addressables;
using UnityEngine;
using UnityEngine.UIElements;

namespace FutureDigitalPlus.UIKit.Components.Samples.Addressables
{
    [AssetRoot("Assets/Samples/Addressables/Data/")]
    [Layout("AddressablesExampleComponent.uxml")]
    [Stylesheet("AddressablesExampleComponent.uss")]
    [Stylesheet("Box.uss")]
    [Dependency(typeof(IAssetSource), provide: typeof(AddressableAssetSource))]
    public partial class AddressablesExampleComponent : UIComponent
    {
        private readonly Label _loadingLabel;
        
        public AddressablesExampleComponent()
        {
            _loadingLabel = new Label("Loading...");
            _loadingLabel.style.alignSelf = Align.Center;
            _loadingLabel.style.unityFontStyleAndWeight = FontStyle.Bold;
            _loadingLabel.style.fontSize = 50;
            Add(_loadingLabel);
        }

        public override void OnInit()
        {
            _loadingLabel.RemoveFromHierarchy();
        }
    }
}
