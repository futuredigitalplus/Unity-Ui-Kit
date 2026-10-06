// FutureDigitalPlus: renamed by Tools~/rename/rename.py (namespaces, resource paths); see SOURCE.md "Rename record".
using JetBrains.Annotations;
using FutureDigitalPlus.UIKit.Components.InterfaceModifiers;
using UnityEngine.UIElements;

namespace FutureDigitalPlus.UIKit.Components
{
    /// <summary>
    /// When implemented by a <see cref="UIComponent"/>,
    /// a callback for <see cref="GeometryChangedEvent"/> is
    /// automatically registered in the UIComponent constructor.
    /// </summary>
    [RegistersEventCallback(typeof(GeometryChangedEvent))]
    public interface IOnGeometryChanged
    {
        void OnGeometryChanged([NotNull] GeometryChangedEvent evt);
    }
}
