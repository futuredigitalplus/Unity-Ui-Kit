using System;
using System.Collections.Generic;

namespace FutureDigitalPlus.UIKit.Controls
{
    /// <summary>
    /// The timing of a reward reveal: rewards appear one after another after an initial delay, and a skip reveals the
    /// rest at once. No time source: the caller feeds <see cref="Tick"/>.
    /// </summary>
    public sealed class RewardSequence
    {
        private float _clock;

        /// <summary>Creates a sequence of <paramref name="count"/> rewards.</summary>
        /// <param name="count">Number of rewards (0 or more).</param>
        /// <param name="initialDelaySeconds">Wait before the first reward (default 0.25).</param>
        /// <param name="intervalSeconds">Wait between rewards (default 0.35).</param>
        public RewardSequence(int count, float initialDelaySeconds = 0.25f, float intervalSeconds = 0.35f)
        {
            Count = Math.Max(0, count);
            InitialDelaySeconds = Math.Max(0f, initialDelaySeconds);
            IntervalSeconds = Math.Max(0f, intervalSeconds);
        }

        /// <summary>Total number of rewards.</summary>
        public int Count { get; }

        /// <summary>Seconds before the first reward appears.</summary>
        public float InitialDelaySeconds { get; }

        /// <summary>Seconds between two rewards.</summary>
        public float IntervalSeconds { get; }

        /// <summary>How many rewards are visible; they are the first <see cref="RevealedCount"/> by index.</summary>
        public int RevealedCount { get; private set; }

        /// <summary>True when every reward is visible.</summary>
        public bool IsComplete => RevealedCount >= Count;

        /// <summary>True when the player skipped the animation.</summary>
        public bool WasSkipped { get; private set; }

        /// <summary>Raised with the index of each reward as it is revealed.</summary>
        public event Action<int> Revealed;

        /// <summary>True when the reward at <paramref name="index"/> is visible.</summary>
        public bool IsRevealed(int index) => index >= 0 && index < RevealedCount;

        /// <summary>Advances time. Returns true when at least one reward was revealed during this call.</summary>
        public bool Tick(float deltaSeconds)
        {
            if (IsComplete) return false;
            if (deltaSeconds > 0f) _clock += deltaSeconds;   // negative and NaN steps change nothing
            var changed = false;
            while (RevealedCount < Count && _clock >= NextRevealAt(RevealedCount))
            {
                RevealOne();
                changed = true;
            }

            return changed;
        }

        /// <summary>Reveals every remaining reward at once.</summary>
        public void Skip()
        {
            if (IsComplete) return;
            WasSkipped = true;
            while (RevealedCount < Count) RevealOne();
        }

        private float NextRevealAt(int index) => InitialDelaySeconds + IntervalSeconds * index;

        private void RevealOne()
        {
            var index = RevealedCount++;
            Revealed?.Invoke(index);
        }
    }

    /// <summary>
    /// A first-in first-out queue of things to present one at a time (reward sets, announcements). The first item
    /// becomes current at once; the next becomes current when the caller reports the current one finished.
    /// </summary>
    /// <typeparam name="T">The type of item.</typeparam>
    public sealed class PresentationQueue<T>
    {
        private readonly Queue<T> _waiting = new Queue<T>();

        /// <summary>True when an item is being presented.</summary>
        public bool HasCurrent { get; private set; }

        /// <summary>The item being presented (default when none).</summary>
        public T Current { get; private set; }

        /// <summary>Number of items waiting behind the current one.</summary>
        public int WaitingCount => _waiting.Count;

        /// <summary>Raised when an item becomes current (including the first).</summary>
        public event Action<T> CurrentChanged;

        /// <summary>Raised when the last item has finished and nothing is current any more.</summary>
        public event Action Drained;

        /// <summary>Adds an item. It becomes current immediately when nothing is being presented.</summary>
        public void Enqueue(T item)
        {
            if (HasCurrent)
            {
                _waiting.Enqueue(item);
                return;
            }

            MakeCurrent(item);
        }

        /// <summary>Reports the current item as finished and moves on to the next one, if any.</summary>
        public void Complete()
        {
            if (!HasCurrent) return;
            if (_waiting.Count > 0)
            {
                MakeCurrent(_waiting.Dequeue());
                return;
            }

            HasCurrent = false;
            Current = default;
            Drained?.Invoke();
        }

        /// <summary>Drops the waiting items and finishes the current one without raising <see cref="Drained"/>.</summary>
        public void Clear()
        {
            _waiting.Clear();
            HasCurrent = false;
            Current = default;
        }

        private void MakeCurrent(T item)
        {
            HasCurrent = true;
            Current = item;
            CurrentChanged?.Invoke(item);
        }
    }
}
