// FutureDigitalPlus: renamed by Tools~/rename/rename.py (namespaces, resource paths); see SOURCE.md "Rename record".
// FutureDigitalPlus: rewritten as a main-thread, frame-paced loop. Upstream ran the condition on a thread-pool thread
// (Task.Run + Task.Delay): UnityEngine calls are illegal there, and on WebGL (no threads, no timers) the task never
// completed. The signatures are unchanged. See SOURCE.md.
using System;
using System.Threading.Tasks;
using UnityEngine;

namespace FutureDigitalPlus.UIKit.Animation
{
    /// <summary>
    /// Awaitable polling helpers. The condition always runs on the Unity main thread, one check per frame at most, so it may
    /// read any UnityEngine state. Call them from the main thread only (they read <c>Time.realtimeSinceStartupAsDouble</c> and
    /// await <c>Awaitable.NextFrameAsync</c>). Time is real time, so a paused game (<c>Time.timeScale = 0</c>) still times out.
    /// The wait advances only while Unity runs frames: in the Editor outside Play Mode that means editor updates.
    /// </summary>
    public static class Wait
    {
        /// <summary>
        /// Waits while <paramref name="condition"/> is true. Completes on the first frame where it is false.
        /// </summary>
        /// <param name="condition">The condition that will perpetuate the wait. Evaluated on the main thread.</param>
        /// <param name="frequency">Minimum gap between two evaluations, in milliseconds (checks happen on frame boundaries, so
        /// the real gap is rounded up to a frame). 0 checks every frame.</param>
        /// <param name="timeout">Timeout in milliseconds of real time; a negative value waits forever.</param>
        /// <exception cref="TimeoutException">The condition was still true when the timeout elapsed.</exception>
        /// <exception cref="ArgumentNullException"><paramref name="condition"/> is null.</exception>
        public static Task While(Func<bool> condition, int frequency = 25, int timeout = -1)
        {
            if (condition == null) throw new ArgumentNullException(nameof(condition));
            return Poll(() => !condition(), frequency, timeout);
        }

        /// <summary>
        /// Waits until <paramref name="condition"/> is true. Completes on the first frame where it is true.
        /// </summary>
        /// <param name="condition">The break condition. Evaluated on the main thread.</param>
        /// <param name="frequency">Minimum gap between two evaluations, in milliseconds (rounded up to a frame). 0 checks every frame.</param>
        /// <param name="timeout">Timeout in milliseconds of real time; a negative value waits forever.</param>
        /// <exception cref="TimeoutException">The condition was still false when the timeout elapsed.</exception>
        /// <exception cref="ArgumentNullException"><paramref name="condition"/> is null.</exception>
        public static Task Until(Func<bool> condition, int frequency = 25, int timeout = -1)
        {
            if (condition == null) throw new ArgumentNullException(nameof(condition));
            return Poll(condition, frequency, timeout);
        }

        private static async Task Poll(Func<bool> done, int frequency, int timeoutMs)
        {
            double start = Time.realtimeSinceStartupAsDouble;
            double nextCheck = start;
            double gap = Math.Max(0, frequency) / 1000.0;

            while (true)
            {
                double now = Time.realtimeSinceStartupAsDouble;

                if (now >= nextCheck)
                {
                    if (done()) return;
                    nextCheck = now + gap;
                }

                // The condition is checked before the timeout, so a condition that became true in time wins.
                if (timeoutMs >= 0 && (now - start) * 1000.0 >= timeoutMs) throw new TimeoutException();

                await Awaitable.NextFrameAsync();
            }
        }
    }
}
