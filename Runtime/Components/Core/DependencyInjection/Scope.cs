// FutureDigitalPlus: renamed by Tools~/rename/rename.py (namespaces, resource paths); see SOURCE.md "Rename record".
namespace FutureDigitalPlus.UIKit.Components
{
    /// <summary>
    /// An enum for dependency scopes.
    /// </summary>
    public enum Scope
    {
        /// <summary>
        /// A singleton dependency, which is cached. All consumers receive
        /// the same instance.
        /// </summary>
        Singleton,
        /// <summary>
        /// A dependency which is not cached. All consumers receive their
        /// own instance.
        /// </summary>
        Transient
    }
}