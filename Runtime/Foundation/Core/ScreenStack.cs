using System;
using System.Collections.Generic;
using System.Text;

namespace FutureDigitalPlus.UIKit.Foundation
{
    /// <summary>
    /// The navigation rules of the app, with no Unity types: push keeps the current screen alive underneath, replace removes it, pop returns,
    /// reset clears everything but the new root. The router (view layer) executes the returned <see cref="ScreenChange"/>.
    /// Rules worth knowing:
    ///  - pushing the id that is already on top is ignored (double taps cannot stack two Settings screens);
    ///  - pop at the root is ignored and reports <see cref="ScreenChangeKind.None"/> so the caller can let the platform back button act;
    ///  - when the depth would exceed <see cref="MaxDepth"/> the entry just above the root is disposed (the root and the top never are).
    /// </summary>
    public sealed class ScreenStack
    {
        /// <summary>Default depth cap.</summary>
        public const int DefaultMaxDepth = 12;
        const string FormatTag = "fdp-nav-v1";

        readonly List<ScreenEntry> _entries = new List<ScreenEntry>();

        /// <summary>Entries beyond this many drop the oldest one above the root.</summary>
        public int MaxDepth { get; }

        /// <summary>Creates an empty stack.</summary>
        public ScreenStack(int maxDepth = DefaultMaxDepth)
        {
            MaxDepth = maxDepth < 2 ? 2 : maxDepth;
        }

        /// <summary>Number of screens on the stack.</summary>
        public int Depth
        {
            get { return _entries.Count; }
        }

        /// <summary>True when a pop would remove a screen (there is more than the root).</summary>
        public bool CanPop
        {
            get { return _entries.Count > 1; }
        }

        /// <summary>The screen on top, or null.</summary>
        public ScreenEntry Current
        {
            get { return _entries.Count == 0 ? null : _entries[_entries.Count - 1]; }
        }

        /// <summary>Id of the screen on top, or null.</summary>
        public string CurrentId
        {
            get { return _entries.Count == 0 ? null : _entries[_entries.Count - 1].Id; }
        }

        /// <summary>Entry at <paramref name="index"/> counted from the bottom (0 is the root).</summary>
        public ScreenEntry this[int index]
        {
            get { return _entries[index]; }
        }

        /// <summary>True when a screen with this id is on the stack.</summary>
        public bool Contains(string id)
        {
            return IndexOf(id) >= 0;
        }

        /// <summary>Index of the topmost entry with <paramref name="id"/>, or -1.</summary>
        public int IndexOf(string id)
        {
            for (int i = _entries.Count - 1; i >= 0; i--)
            {
                if (string.Equals(_entries[i].Id, id, StringComparison.Ordinal))
                    return i;
            }
            return -1;
        }

        /// <summary>Puts a screen on top. Pushing the id that is already on top does nothing (a double tap cannot stack two copies).</summary>
        public ScreenChange Push(string id, object args = null)
        {
            ValidateId(id);
            ScreenEntry from = Current;
            if (from != null && string.Equals(from.Id, id, StringComparison.Ordinal))
                return ScreenChange.None(from);

            ScreenEntry entry = new ScreenEntry(id, args, null);
            _entries.Add(entry);

            List<ScreenEntry> disposed = null;
            if (_entries.Count > MaxDepth)
            {
                disposed = new List<ScreenEntry>();
                while (_entries.Count > MaxDepth)
                {
                    disposed.Add(_entries[1]);
                    _entries.RemoveAt(1);
                }
            }
            // With a tiny MaxDepth the screen that was on top can be the one trimmed away; it then does not stay alive underneath.
            bool fromStaysAlive = from != null && (disposed == null || !disposed.Contains(from));
            return new ScreenChange(ScreenChangeKind.Push, from, entry, fromStaysAlive, false, disposed);
        }

        /// <summary>Swaps the top screen for a new one. On an empty stack this is a push.</summary>
        public ScreenChange Replace(string id, object args = null)
        {
            ValidateId(id);
            ScreenEntry from = Current;
            if (from == null)
                return Push(id, args);

            _entries.RemoveAt(_entries.Count - 1);
            ScreenEntry entry = new ScreenEntry(id, args, null);
            _entries.Add(entry);
            return new ScreenChange(ScreenChangeKind.Replace, from, entry, false, false, new[] { from });
        }

        /// <summary>Removes the top screen. Does nothing at the root.</summary>
        public ScreenChange Pop()
        {
            if (_entries.Count <= 1)
                return ScreenChange.None(Current);
            ScreenEntry from = _entries[_entries.Count - 1];
            _entries.RemoveAt(_entries.Count - 1);
            return new ScreenChange(ScreenChangeKind.Pop, from, Current, false, true, new[] { from });
        }

        /// <summary>Pops until <paramref name="id"/> is on top. Unknown id, or already on top: nothing happens.</summary>
        public ScreenChange PopTo(string id)
        {
            int index = IndexOf(id);
            if (index < 0 || index == _entries.Count - 1)
                return ScreenChange.None(Current);
            ScreenEntry from = Current;
            List<ScreenEntry> disposed = new List<ScreenEntry>();
            for (int i = index + 1; i < _entries.Count; i++)
                disposed.Add(_entries[i]);
            _entries.RemoveRange(index + 1, _entries.Count - index - 1);
            return new ScreenChange(ScreenChangeKind.Pop, from, Current, false, true, disposed);
        }

        /// <summary>Clears the stack and makes <paramref name="id"/> the only screen (for example back to Home).</summary>
        public ScreenChange ResetTo(string id, object args = null)
        {
            ValidateId(id);
            ScreenEntry from = Current;
            List<ScreenEntry> disposed = new List<ScreenEntry>(_entries);
            _entries.Clear();
            ScreenEntry entry = new ScreenEntry(id, args, null);
            _entries.Add(entry);
            return new ScreenChange(ScreenChangeKind.Reset, from, entry, false, false, disposed);
        }

        /// <summary>
        /// Serialises the ids and state strings (not the args) so the stack can be rebuilt after a restart. Plain text, one line per entry:
        /// the id, a tab, the state with backslash, tab and line breaks escaped.
        /// </summary>
        public string Capture()
        {
            StringBuilder builder = new StringBuilder();
            builder.Append(FormatTag);
            for (int i = 0; i < _entries.Count; i++)
            {
                builder.Append('\n');
                builder.Append(_entries[i].Id);
                builder.Append('\t');
                builder.Append(Escape(_entries[i].State));
            }
            return builder.ToString();
        }

        /// <summary>
        /// Parses text produced by <see cref="Capture"/>. Returns false (and an empty list) for anything else; the caller then boots the
        /// normal start screen. <paramref name="isKnown"/> lets the caller drop ids that no longer exist (renamed screens after an update):
        /// such entries are skipped, and the restore fails when the root or nothing is left.
        /// </summary>
        public static bool TryParse(string captured, Func<string, bool> isKnown, out List<ScreenEntry> entries)
        {
            entries = new List<ScreenEntry>();
            if (string.IsNullOrEmpty(captured))
                return false;
            string[] lines = captured.Split('\n');
            if (lines.Length < 2 || lines[0] != FormatTag)
                return false;
            for (int i = 1; i < lines.Length; i++)
            {
                string line = lines[i];
                int tab = line.IndexOf('\t');
                string id = tab < 0 ? line : line.Substring(0, tab);
                string state = tab < 0 ? null : Unescape(line.Substring(tab + 1));
                if (string.IsNullOrEmpty(id))
                    continue;
                if (isKnown != null && !isKnown(id))
                {
                    if (i == 1)
                    {
                        entries.Clear();
                        return false;
                    }
                    continue;
                }
                entries.Add(new ScreenEntry(id, null, state));
            }
            if (entries.Count == 0)
                return false;
            return true;
        }

        /// <summary>Replaces the stack content with restored entries (bottom first). The router builds the screens afterwards.</summary>
        public void Load(IReadOnlyList<ScreenEntry> entries)
        {
            _entries.Clear();
            if (entries == null)
                return;
            for (int i = 0; i < entries.Count; i++)
                _entries.Add(entries[i]);
        }

        static void ValidateId(string id)
        {
            if (string.IsNullOrEmpty(id))
                throw new ArgumentException("A screen id must not be empty.", nameof(id));
            if (id.IndexOf('\t') >= 0 || id.IndexOf('\n') >= 0)
                throw new ArgumentException("A screen id must not contain tab or line break characters.", nameof(id));
        }

        static string Escape(string value)
        {
            if (string.IsNullOrEmpty(value))
                return string.Empty;
            return value.Replace("\\", "\\\\").Replace("\t", "\\t").Replace("\n", "\\n").Replace("\r", "\\r");
        }

        static string Unescape(string value)
        {
            if (string.IsNullOrEmpty(value) || value.IndexOf('\\') < 0)
                return value;
            StringBuilder builder = new StringBuilder(value.Length);
            for (int i = 0; i < value.Length; i++)
            {
                char c = value[i];
                if (c != '\\' || i == value.Length - 1)
                {
                    builder.Append(c);
                    continue;
                }
                i++;
                char next = value[i];
                switch (next)
                {
                    case 't':
                        builder.Append('\t');
                        break;
                    case 'n':
                        builder.Append('\n');
                        break;
                    case 'r':
                        builder.Append('\r');
                        break;
                    default:
                        builder.Append(next);
                        break;
                }
            }
            return builder.ToString();
        }
    }
}
