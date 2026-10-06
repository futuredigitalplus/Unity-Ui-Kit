using System.Collections.Generic;

namespace FutureDigitalPlus.UIKit.Foundation
{
    /// <summary>
    /// What the view layer must do after a stack operation. The stack has already changed when this is returned.
    /// The router shows <see cref="To"/>, hides <see cref="From"/> when <see cref="FromStaysAlive"/> (it keeps its state underneath),
    /// and disposes everything in <see cref="Disposed"/> (which may include <see cref="From"/>).
    /// </summary>
    public sealed class ScreenChange
    {
        static readonly ScreenEntry[] NoEntries = new ScreenEntry[0];

        /// <summary>The operation that produced this change.</summary>
        public ScreenChangeKind Kind { get; }

        /// <summary>The screen that was on top before the operation, or null.</summary>
        public ScreenEntry From { get; }

        /// <summary>The screen on top now, or null when the stack is empty.</summary>
        public ScreenEntry To { get; }

        /// <summary>True when <see cref="From"/> remains on the stack below <see cref="To"/> (push); false when it was removed.</summary>
        public bool FromStaysAlive { get; }

        /// <summary>Entries that left the stack and must be disposed, bottom first.</summary>
        public IReadOnlyList<ScreenEntry> Disposed { get; }

        /// <summary>True when <see cref="To"/> was already on the stack (pop, pop-to) and must be shown again instead of built.</summary>
        public bool ToIsExisting { get; }

        /// <summary>True when nothing happened.</summary>
        public bool IsNone
        {
            get { return Kind == ScreenChangeKind.None; }
        }

        /// <summary>Creates a change description.</summary>
        public ScreenChange(ScreenChangeKind kind, ScreenEntry from, ScreenEntry to, bool fromStaysAlive, bool toIsExisting, IReadOnlyList<ScreenEntry> disposed)
        {
            Kind = kind;
            From = from;
            To = to;
            FromStaysAlive = fromStaysAlive;
            ToIsExisting = toIsExisting;
            Disposed = disposed ?? NoEntries;
        }

        /// <summary>A change that did nothing; <paramref name="current"/> stays on top.</summary>
        public static ScreenChange None(ScreenEntry current)
        {
            return new ScreenChange(ScreenChangeKind.None, current, current, true, true, null);
        }
    }
}
