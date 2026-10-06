namespace FutureDigitalPlus.UIKit.Foundation
{
    /// <summary>One screen on the router stack: its id, the arguments it was opened with and its persisted state string.</summary>
    public sealed class ScreenEntry
    {
        /// <summary>The screen id it was registered with.</summary>
        public string Id { get; }

        /// <summary>Arguments of the push (any object). Not persisted; a restored entry has null here and gets <see cref="State"/> instead.</summary>
        public object Args { get; }

        /// <summary>
        /// Free-form string the screen wants back after a restart (selected tab, scroll position, search text). Written by the router from
        /// <c>UiScreen.SaveState()</c> before the stack is captured. Must not contain secrets; it is stored in plain text.
        /// </summary>
        public string State { get; set; }

        /// <summary>Creates an entry.</summary>
        public ScreenEntry(string id, object args, string state)
        {
            Id = id;
            Args = args;
            State = state;
        }

        /// <summary>The screen id.</summary>
        public override string ToString()
        {
            return Id;
        }
    }
}
