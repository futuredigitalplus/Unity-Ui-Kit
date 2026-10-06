// FutureDigitalPlus: renamed by Tools~/rename/rename.py (namespaces, resource paths); see SOURCE.md "Rename record".
using System.Collections.Generic;
using UnityEngine.UIElements;

namespace FutureDigitalPlus.UIKit.Components.Samples.EventInterfaces
{
    public interface IEventLogService
    {
        public delegate void OnMessageAddDelegate(string message);

        public delegate void OnMessageChangeDelegate(int index, string newMessage);
        
        public event OnMessageAddDelegate OnMessageAdd;

        public event OnMessageChangeDelegate OnMessageChange;
        
        public delegate void OnClearDelegate();

        public event OnClearDelegate OnClear;
        
        void Log(EventBase evt);

        void Clear();

        IReadOnlyList<string> GetMessages();
    }
}
