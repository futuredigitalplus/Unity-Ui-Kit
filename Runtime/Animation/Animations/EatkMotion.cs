using System;
using UnityEngine;

namespace FutureDigitalPlus.UIKit.Animation
{
    /// <summary>
    /// The one hook through which the animation module honours a "reduce motion" or "animation speed" setting. Every duration that the
    /// module hands to UI Toolkit's <c>experimental.animation</c> passes through <see cref="Scale"/>. The module has no reference to any other
    /// module of the kit, so the setting arrives as a delegate: the facade (or a game) assigns <see cref="Scaler"/>, for example
    /// <c>EatkMotion.Scaler = accessibilityApplier.ScaleMotion;</c>.
    /// </summary>
    public static class EatkMotion
    {
        /// <summary>Maps a duration in milliseconds to the duration to use. Null (the default) leaves durations unchanged.</summary>
        public static Func<int, int> Scaler { get; set; }

        /// <summary>
        /// The duration to hand to the animation system for a requested <paramref name="milliseconds"/>. Durations of 0 or less pass through.
        /// A positive request never becomes 0: a zero-length value animation would divide by its duration, so it shrinks to 1 ms, which finishes on the next tick.
        /// </summary>
        public static int Scale(int milliseconds)
        {
            if (milliseconds <= 0) return milliseconds;
            var scaler = Scaler;
            if (scaler == null) return milliseconds;
            var scaled = scaler(milliseconds);
            return scaled < 1 ? 1 : scaled;
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetOnLoad() => Scaler = null;
    }
}
