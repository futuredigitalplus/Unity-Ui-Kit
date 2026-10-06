// FutureDigitalPlus: renamed by Tools~/rename/rename.py (namespaces, resource paths); see SOURCE.md "Rename record".
namespace FutureDigitalPlus.UIKit.Components.Samples.Counter
{
    public class CounterService : ICounterService
    {
        private int _count;
    
        public void IncrementCount() => _count++;
        public int GetCount() => _count;
    }
}