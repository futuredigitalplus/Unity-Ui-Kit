namespace FutureDigitalPlus.UIKit.Foundation
{
    /// <summary>The screen stack as screens see it: push, replace, pop, back.</summary>
    public interface IScreenRouter
    {
        /// <summary>Id of the screen on top, or null.</summary>
        string Current { get; }

        /// <summary>True when there is a screen below the one on top.</summary>
        bool CanPop { get; }

        /// <summary>Raised after the stack changed, with the id that was on top before and the id on top now (either may be null).</summary>
        event System.Action<string, string> Changed;

        /// <summary>Opens a screen on top; the current one stays alive underneath. Pushing the screen that is already on top does nothing.</summary>
        void Push(string screenId, object args = null);

        /// <summary>Replaces the screen on top.</summary>
        void Replace(string screenId, object args = null);

        /// <summary>Closes the screen on top. Returns false at the root.</summary>
        bool Pop();

        /// <summary>Closes screens until <paramref name="screenId"/> is on top. Returns false when it is not on the stack.</summary>
        bool PopTo(string screenId);

        /// <summary>Clears the stack and shows one screen.</summary>
        void ResetTo(string screenId, object args = null);

        /// <summary>A back press: asks the screen first, then pops. Returns false at the root when nothing consumed it.</summary>
        bool Back();
    }
}
