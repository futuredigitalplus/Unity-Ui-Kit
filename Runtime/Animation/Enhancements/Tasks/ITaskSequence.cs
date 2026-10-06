// FutureDigitalPlus: renamed by Tools~/rename/rename.py (namespaces, resource paths); see SOURCE.md "Rename record".
using System;
using System.Collections;

namespace FutureDigitalPlus.UIKit.Animation
{
    public interface ITaskSequence : IEnumerable
    {
        // --| Add a task to the sequence -----------------
        void Add(string text, Action<Action> task);

        // --| Run the task sequence ----------------------
        void Run(string text, Action onCompleted);
    }


    // --| A class that manages type extension methods
    public static class ITaskSequenceExtended
    {
        // --| Add a task to the sequence -----------------
        public static void Add(this ITaskSequence self, Action<Action> task) => self.Add(string.Empty, task);
        
        // --| Run the task sequence ----------------------
        public static void Run(this ITaskSequence self) => self.Run(onCompleted: null);
        
        // --| Run the task sequence ----------------------
        public static void Run(this ITaskSequence self, string text) => self.Run(text, null);
        
        // --| Run the task sequence ----------------------
        public static void Run(this ITaskSequence self, Action onCompleted) => self.Run(string.Empty, onCompleted);
        
    }
}
