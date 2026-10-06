// FutureDigitalPlus: renamed by Tools~/rename/rename.py (namespaces, resource paths); see SOURCE.md "Rename record".
using System;
using JetBrains.Annotations;
using UnityEngine.TestTools;

namespace FutureDigitalPlus.UIKit.Components
{
    /// <summary>
    /// Specifies the path to the .uxml layout file used by a UIComponent.
    /// When used without a path, convention-based resolution is used:
    /// the component's class name becomes the asset name.
    /// </summary>
    /// <seealso cref="AssetPrefixAttribute"/>
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = true)]
    [BaseTypeRequired(typeof(UIComponent))]
    [ExcludeFromCoverage]
    public sealed class LayoutAttribute : Attribute
    {
        public readonly string Path;

        public LayoutAttribute(string path)
        {
            Path = path;
        }

        public LayoutAttribute() : this(null) {}
    }
}
