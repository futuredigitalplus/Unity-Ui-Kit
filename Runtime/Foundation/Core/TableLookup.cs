namespace FutureDigitalPlus.UIKit.Foundation
{
    /// <summary>One string table lookup. Must not throw for a missing table; return <see cref="LookupStatus.TableMissing"/>.</summary>
    public delegate LookupStatus TableLookup(string table, string key, object[] args, out string text);
}
