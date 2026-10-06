// FutureDigitalPlus: renamed by Tools~/rename/rename.py (namespaces, resource paths); see SOURCE.md "Rename record".
using System.Collections.Generic;

namespace FutureDigitalPlus.UIKit.Components.DependencyInjection
{
    public interface IDependencyConsumer
    {
        IEnumerable<IDependency> GetDependencies();
    }
}