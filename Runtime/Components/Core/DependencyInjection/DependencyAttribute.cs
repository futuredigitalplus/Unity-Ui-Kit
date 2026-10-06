// FutureDigitalPlus: renamed by Tools~/rename/rename.py (namespaces, resource paths); see SOURCE.md "Rename record".
using JetBrains.Annotations;
using System;
using FutureDigitalPlus.UIKit.Components.DependencyInjection;
using UnityEngine.TestTools;

namespace FutureDigitalPlus.UIKit.Components
{
    /// <summary>
    /// Used to define a dependency for FutureDigitalPlus.UIKit.Components and other dependency consumers.
    /// </summary>
    /// <seealso cref="UIComponent"/>
    /// <seealso cref="IDependencyConsumer"/>
    /// <seealso cref="DependencyConsumer"/>
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct | AttributeTargets.Assembly, AllowMultiple = true, Inherited = true)]
    [ExcludeFromCoverage]
    [BaseTypeRequired(typeof(IDependencyConsumer))]
    public sealed class DependencyAttribute : Attribute
    {
        public readonly Type DependencyType;

        public readonly Type ImplementationType;
        
        public readonly Scope Scope;

        public DependencyAttribute(Type dependency, Type provide = null, Scope scope = Scope.Singleton)
        {
            DependencyType = dependency;
            ImplementationType = provide ?? dependency;
            Scope = scope;
        }
    }
}
