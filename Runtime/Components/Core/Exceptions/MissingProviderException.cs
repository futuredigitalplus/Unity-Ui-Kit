// FutureDigitalPlus: renamed by Tools~/rename/rename.py (namespaces, resource paths); see SOURCE.md "Rename record".
using System;

namespace FutureDigitalPlus.UIKit.Components
{
    /// <summary>
    /// Thrown when a dependency does not have a provider.
    /// </summary>
    public sealed class MissingProviderException : Exception
    {
        /// <summary>
        /// The dependency type.
        /// </summary>
        public readonly Type DependencyType;
        
        public MissingProviderException(Type dependencyType)
        {
            DependencyType = dependencyType;
            Message = $"No provider found for {dependencyType.Name}";
        }

        public override string Message { get; }
    }
}
