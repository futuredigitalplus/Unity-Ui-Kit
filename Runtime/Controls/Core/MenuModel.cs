using System;
using System.Collections.Generic;

namespace FutureDigitalPlus.UIKit.Controls
{
    /// <summary>One entry of a menu: a command, a separator, or a command that opens a submenu.</summary>
    public sealed class MenuEntry
    {
        private List<MenuEntry> _children;

        /// <summary>Creates a command item.</summary>
        public MenuEntry(string id, string label)
        {
            Id = id;
            Label = label;
        }

        private MenuEntry()
        {
            IsSeparator = true;
        }

        /// <summary>Creates a separator line.</summary>
        public static MenuEntry Separator() => new MenuEntry();

        /// <summary>Identifier given back when the item is invoked.</summary>
        public string Id { get; }

        /// <summary>Text shown for the item.</summary>
        public string Label { get; set; }

        /// <summary>Optional text shown at the end of the row (a shortcut or a value).</summary>
        public string Hint { get; set; }

        /// <summary>False for an item that is shown dimmed and cannot be focused or invoked.</summary>
        public bool Enabled { get; set; } = true;

        /// <summary>True for an item that shows a check mark (a toggled option or the current choice).</summary>
        public bool Checked { get; set; }

        /// <summary>Free payload for the caller.</summary>
        public object Tag { get; set; }

        /// <summary>True for a separator line.</summary>
        public bool IsSeparator { get; }

        /// <summary>The items of the submenu this item opens; empty for a command.</summary>
        public IReadOnlyList<MenuEntry> Children => _children ?? (IReadOnlyList<MenuEntry>)Array.Empty<MenuEntry>();

        /// <summary>True when the item opens a submenu.</summary>
        public bool HasSubmenu => _children != null && _children.Count > 0;

        /// <summary>True when the item can take focus: not a separator and enabled.</summary>
        public bool IsFocusable => !IsSeparator && Enabled;

        /// <summary>Adds an item to this item's submenu and returns this item, for chaining.</summary>
        public MenuEntry Add(MenuEntry child)
        {
            if (child == null) throw new ArgumentNullException(nameof(child));
            if (IsSeparator) throw new InvalidOperationException("A separator cannot have a submenu.");
            (_children ?? (_children = new List<MenuEntry>())).Add(child);
            return this;
        }
    }

    /// <summary>What a menu key press did.</summary>
    public enum MenuAction
    {
        /// <summary>Nothing happened (no focused item, disabled item, already at the root).</summary>
        None,

        /// <summary>A command was invoked; see <see cref="MenuStep.Item"/>.</summary>
        Invoked,

        /// <summary>A submenu opened.</summary>
        OpenedSubmenu,

        /// <summary>A submenu closed and the parent level is current again.</summary>
        ClosedSubmenu,

        /// <summary>The whole menu should close.</summary>
        Closed,
    }

    /// <summary>The outcome of <see cref="MenuModel.Activate"/>, <see cref="MenuModel.Back"/> or <see cref="MenuModel.Cancel"/>.</summary>
    public readonly struct MenuStep
    {
        internal MenuStep(MenuAction action, MenuEntry item)
        {
            Action = action;
            Item = item;
        }

        /// <summary>What happened.</summary>
        public MenuAction Action { get; }

        /// <summary>The invoked item (<see cref="MenuAction.Invoked"/>) or the item whose submenu opened or closed; otherwise null.</summary>
        public MenuEntry Item { get; }
    }

    /// <summary>
    /// The keyboard and gamepad logic of a menu with submenus: which row has focus (skipping separators and disabled
    /// rows), moving with wrap-around, type-ahead search, and opening or closing submenus. It has no view and no
    /// clock of its own: type-ahead takes the current time from the caller.
    /// </summary>
    public sealed class MenuModel
    {
        private sealed class Level
        {
            public IReadOnlyList<MenuEntry> Items;
            public int Focus;
            public MenuEntry Parent;
        }

        private readonly List<Level> _levels = new List<Level>(3);
        private readonly System.Text.StringBuilder _typed = new System.Text.StringBuilder();
        private double _lastTypedAt = double.NegativeInfinity;

        /// <summary>Creates a model over the root items. The first focusable item has focus.</summary>
        public MenuModel(IReadOnlyList<MenuEntry> items)
        {
            if (items == null) throw new ArgumentNullException(nameof(items));
            _levels.Add(NewLevel(items, null));
        }

        /// <summary>True (default) when moving past the last item continues at the first, and the other way round.</summary>
        public bool Wrap { get; set; } = true;

        /// <summary>Seconds of silence after which the type-ahead text starts over (default 1).</summary>
        public double TypeAheadTimeoutSeconds { get; set; } = 1d;

        /// <summary>How many submenus are open (0 for the root level).</summary>
        public int Depth => _levels.Count - 1;

        /// <summary>The items of the level that currently has focus.</summary>
        public IReadOnlyList<MenuEntry> CurrentItems => Top.Items;

        /// <summary>The index of the focused item in <see cref="CurrentItems"/>, or -1 when no item can take focus.</summary>
        public int FocusIndex => Top.Focus;

        /// <summary>The focused item, or null.</summary>
        public MenuEntry Focused => Top.Focus >= 0 ? Top.Items[Top.Focus] : null;

        /// <summary>The item whose submenu is the current level, or null at the root.</summary>
        public MenuEntry CurrentParent => Top.Parent;

        /// <summary>The items of the level at <paramref name="depth"/> (0 is the root).</summary>
        public IReadOnlyList<MenuEntry> ItemsAt(int depth) => _levels[depth].Items;

        /// <summary>The focused index of the level at <paramref name="depth"/>.</summary>
        public int FocusIndexAt(int depth) => _levels[depth].Focus;

        private Level Top => _levels[_levels.Count - 1];

        /// <summary>Focuses the item at <paramref name="index"/> of the current level. Returns false when it cannot take focus or is already focused.</summary>
        public bool FocusItem(int index)
        {
            var level = Top;
            if (index < 0 || index >= level.Items.Count || !level.Items[index].IsFocusable || index == level.Focus) return false;
            level.Focus = index;
            return true;
        }

        /// <summary>Moves focus to the next focusable item. Returns true when focus changed.</summary>
        public bool MoveNext() => Move(1);

        /// <summary>Moves focus to the previous focusable item. Returns true when focus changed.</summary>
        public bool MovePrevious() => Move(-1);

        /// <summary>Moves focus to the first focusable item.</summary>
        public bool MoveFirst()
        {
            var level = Top;
            for (var i = 0; i < level.Items.Count; i++)
                if (level.Items[i].IsFocusable) return SetFocus(level, i);
            return false;
        }

        /// <summary>Moves focus to the last focusable item.</summary>
        public bool MoveLast()
        {
            var level = Top;
            for (var i = level.Items.Count - 1; i >= 0; i--)
                if (level.Items[i].IsFocusable) return SetFocus(level, i);
            return false;
        }

        /// <summary>
        /// Type-ahead: focuses the next focusable item whose label starts with the letters typed within the timeout.
        /// Typing the same letter repeatedly cycles through the items that start with it. Returns true when focus changed.
        /// </summary>
        /// <param name="character">The typed character; control characters and white space are ignored.</param>
        /// <param name="nowSeconds">The current time in seconds, from any monotonic clock.</param>
        public bool TypeAhead(char character, double nowSeconds)
        {
            if (char.IsControl(character) || char.IsWhiteSpace(character)) return false;
            if (nowSeconds - _lastTypedAt > TypeAheadTimeoutSeconds) _typed.Length = 0;
            _lastTypedAt = nowSeconds;
            _typed.Append(character);

            var text = _typed.ToString();
            var repeated = true;
            for (var i = 1; i < text.Length; i++)
                if (char.ToUpperInvariant(text[i]) != char.ToUpperInvariant(text[0])) { repeated = false; break; }

            var level = Top;
            var count = level.Items.Count;
            // a repeated letter searches for that single letter, starting after the focused item; a longer text searches from the focused item
            var needle = repeated ? text.Substring(0, 1) : text;
            var start = repeated || level.Focus < 0 ? level.Focus + 1 : level.Focus;
            for (var step = 0; step < count; step++)
            {
                var index = (start + step + count) % count;
                var item = level.Items[index];
                if (!item.IsFocusable || item.Label == null) continue;
                if (!item.Label.StartsWith(needle, StringComparison.OrdinalIgnoreCase)) continue;
                return SetFocus(level, index);
            }

            return false;
        }

        /// <summary>Enter, Submit or the gamepad confirm button: invokes the focused command or opens its submenu.</summary>
        public MenuStep Activate()
        {
            var item = Focused;
            if (item == null || !item.IsFocusable) return new MenuStep(MenuAction.None, null);
            return item.HasSubmenu ? OpenSubmenu() : new MenuStep(MenuAction.Invoked, item);
        }

        /// <summary>Right arrow: opens the focused item's submenu, if it has one.</summary>
        public MenuStep OpenSubmenu()
        {
            var item = Focused;
            if (item == null || !item.IsFocusable || !item.HasSubmenu) return new MenuStep(MenuAction.None, null);
            _levels.Add(NewLevel(item.Children, item));
            return new MenuStep(MenuAction.OpenedSubmenu, item);
        }

        /// <summary>Left arrow: closes the current submenu. Does nothing at the root.</summary>
        public MenuStep Back()
        {
            if (_levels.Count <= 1) return new MenuStep(MenuAction.None, null);
            var closed = Top.Parent;
            _levels.RemoveAt(_levels.Count - 1);
            return new MenuStep(MenuAction.ClosedSubmenu, closed);
        }

        /// <summary>Cancel or Escape: closes the current submenu, or the whole menu at the root.</summary>
        public MenuStep Cancel()
        {
            var step = Back();
            return step.Action == MenuAction.None ? new MenuStep(MenuAction.Closed, null) : step;
        }

        private static Level NewLevel(IReadOnlyList<MenuEntry> items, MenuEntry parent)
        {
            var level = new Level { Items = items, Focus = -1, Parent = parent };
            for (var i = 0; i < items.Count; i++)
            {
                if (!items[i].IsFocusable) continue;
                level.Focus = i;
                break;
            }

            return level;
        }

        private bool Move(int direction)
        {
            var level = Top;
            var count = level.Items.Count;
            if (count == 0) return false;
            var index = level.Focus;
            for (var step = 0; step < count; step++)
            {
                index += direction;
                if (index < 0 || index >= count)
                {
                    if (!Wrap) return false;
                    index = (index + count) % count;
                }

                if (level.Items[index].IsFocusable) return SetFocus(level, index);
            }

            return false;
        }

        private static bool SetFocus(Level level, int index)
        {
            if (level.Focus == index) return false;
            level.Focus = index;
            return true;
        }
    }
}
