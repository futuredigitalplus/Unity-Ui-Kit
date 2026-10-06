using System;

namespace FutureDigitalPlus.UIKit.Foundation
{
    /// <summary>
    /// The player's current accessibility preferences, as one shared value every module reads. Whoever knows the preferences (the Interaction module reads the operating
    /// system, a settings screen reads the player) calls <see cref="Set"/>; animations and transitions read <see cref="UiMotion"/>. Static, because the preferences belong
    /// to the player, not to a panel. Not thread safe: use it from the main thread.
    /// </summary>
    public static class UiAccessibility
    {
        static UiAccessibilitySettings _current = UiAccessibilitySettings.Default;

        /// <summary>The preferences in force.</summary>
        public static UiAccessibilitySettings Current
        {
            get { return _current; }
        }

        /// <summary>Shortcut for <c>Current.ReduceMotion</c>.</summary>
        public static bool ReduceMotion
        {
            get { return _current.ReduceMotion; }
        }

        /// <summary>Raised after <see cref="Current"/> changed, with the new value. Not raised when a call sets an equal value.</summary>
        public static event Action<UiAccessibilitySettings> Changed;

        /// <summary>Replaces the preferences and raises <see cref="Changed"/> when they differ.</summary>
        public static void Set(UiAccessibilitySettings settings)
        {
            if (settings.Equals(_current))
                return;
            _current = settings;
            Action<UiAccessibilitySettings> handler = Changed;
            if (handler != null)
                handler(settings);
        }

        /// <summary>Back to the defaults and no listeners. Called when the domain reloads and by tests.</summary>
        public static void Reset()
        {
            _current = UiAccessibilitySettings.Default;
            Changed = null;
        }
    }

    /// <summary>Scales durations by the player's motion preference. Every animation of the kit asks it, so reduce motion works everywhere with one switch.</summary>
    public static class UiMotion
    {
        /// <summary>The duration to use: <paramref name="milliseconds"/>, or 0 when the player asked for reduced motion.</summary>
        public static int Scale(int milliseconds)
        {
            return UiAccessibility.ReduceMotion ? 0 : milliseconds;
        }

        /// <summary>The duration to use in seconds: <paramref name="seconds"/>, or 0 when the player asked for reduced motion.</summary>
        public static float Scale(float seconds)
        {
            return UiAccessibility.ReduceMotion ? 0f : seconds;
        }
    }
}
