// FutureDigitalPlus: renamed by Tools~/rename/rename.py (namespaces, resource paths); see SOURCE.md "Rename record".
using System;

namespace FutureDigitalPlus.UIKit.Components.DependencyInjection
{
    public interface IDependency
    {
        Type GetDependencyType();
        Type GetImplementationType();
        Scope GetScope();
        object CreateInstance();
    }
}