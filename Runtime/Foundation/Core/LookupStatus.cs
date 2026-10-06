namespace FutureDigitalPlus.UIKit.Foundation
{
    /// <summary>Outcome of one string table lookup (see <see cref="TableLookup"/>).</summary>
    public enum LookupStatus
    {
        /// <summary>The entry exists; the text was produced.</summary>
        Found = 0,

        /// <summary>The table is there but has no such entry (or it is empty for the active locale).</summary>
        EntryMissing = 1,

        /// <summary>The table does not exist, is not loaded, or localization is not initialised. The resolver stops asking that table until <see cref="LocalizationKeyResolver.ResetMissing"/>.</summary>
        TableMissing = 2
    }
}
