// FutureDigitalPlus: renamed by Tools~/rename/rename.py (namespaces, resource paths); see SOURCE.md "Rename record".
using FutureDigitalPlus.UIKit.Components.InterfaceModifiers;
using UnityEngine.UIElements;

namespace FutureDigitalPlus.UIKit.Components
{
    /// <summary>
    /// When implemented by a <see cref="UIComponent"/>,
    /// a callback for <see cref="ClickEvent"/> is
    /// automatically registered in the UIComponent constructor.
    /// </summary>
    [RegistersEventCallback(typeof(ClickEvent))]
    public interface IOnClick
    {
        void OnClick(ClickEvent evt);
    }
}
