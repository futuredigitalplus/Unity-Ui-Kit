using System;

namespace FutureDigitalPlus.UIKit.Hud
{
    /// <summary>Look of a floating text (damage number, heal, pickup).</summary>
    public enum FloatingTextKind
    {
        /// <summary>A regular number.</summary>
        Normal,

        /// <summary>A critical hit: pops in larger and lives longer.</summary>
        Critical,

        /// <summary>Healing or a gain.</summary>
        Heal,

        /// <summary>A neutral note ("Miss", "Level up").</summary>
        Info,
    }

    /// <summary>The state of one live floating text in a <see cref="FloatingTextModel"/> slot.</summary>
    public struct FloatingTextItem
    {
        /// <summary>True while the slot holds a live text.</summary>
        public bool Active;

        /// <summary>Horizontal position (panel space).</summary>
        public float X;

        /// <summary>Current vertical position (panel space, grows downwards).</summary>
        public float Y;

        /// <summary>Seconds since it spawned.</summary>
        public float Age;

        /// <summary>Total life in seconds.</summary>
        public float Life;

        /// <summary>Opacity 0 to 1.</summary>
        public float Alpha;

        /// <summary>Scale, 1 at rest; above 1 during the pop of a critical.</summary>
        public float Scale;

        /// <summary>The text.</summary>
        public string Text;

        /// <summary>The look.</summary>
        public FloatingTextKind Kind;

        internal float SpawnY;
    }

    /// <summary>
    /// The logic of floating combat text: a fixed pool of slots, rise and fade curves, a pop for criticals, pushing a
    /// new text above a recent one at the same spot, and a limit of spawns per frame. The pool is a plan: the view owns
    /// one element per slot and never allocates after warm-up. Positions are in panel space (y grows downwards).
    /// </summary>
    public sealed class FloatingTextModel
    {
        private const float PopSeconds = 0.2f;
        private const int MaxDeOverlapSteps = 4;

        private readonly FloatingTextItem[] _items;
        private int _spawnedThisFrame;

        /// <summary>Creates a pool of <paramref name="capacity"/> slots (at least 1).</summary>
        public FloatingTextModel(int capacity = 24)
        {
            _items = new FloatingTextItem[Math.Max(1, capacity)];
        }

        /// <summary>Number of slots.</summary>
        public int Capacity => _items.Length;

        /// <summary>Number of live texts.</summary>
        public int ActiveCount { get; private set; }

        /// <summary>Total distance a text rises over its life, in panel units (default 70).</summary>
        public float RiseDistance { get; set; } = 70f;

        /// <summary>Life of a regular text in seconds (default 0.9).</summary>
        public float Lifetime { get; set; } = 0.9f;

        /// <summary>Critical texts live this many times longer (default 1.3).</summary>
        public float CriticalLifetimeScale { get; set; } = 1.3f;

        /// <summary>Fraction of the life after which the fade begins (default 0.5).</summary>
        public float FadeStart { get; set; } = 0.5f;

        /// <summary>Scale at the start of a critical's pop (default 1.6); it settles to 1 within a fifth of a second.</summary>
        public float PopScale { get; set; } = 1.6f;

        /// <summary>Texts closer than this horizontally count as being at the same spot (default 40).</summary>
        public float OverlapWidth { get; set; } = 40f;

        /// <summary>Vertical gap a new text is pushed up by when it would overlap another (default 24).</summary>
        public float OverlapStep { get; set; } = 24f;

        /// <summary>Most spawns accepted between two <see cref="BeginFrame"/> calls (default 8); more are dropped.</summary>
        public int MaxSpawnsPerFrame { get; set; } = 8;

        /// <summary>The state of a slot. <paramref name="slot"/> must be 0 .. <see cref="Capacity"/> - 1.</summary>
        public ref readonly FloatingTextItem Get(int slot) => ref _items[slot];

        /// <summary>Resets the per-frame spawn counter. Call once per frame before spawning.</summary>
        public void BeginFrame()
        {
            _spawnedThisFrame = 0;
        }

        /// <summary>
        /// Spawns a text. Returns the slot used, or -1 when the per-frame limit dropped it. When the pool is full the
        /// oldest text is replaced.
        /// </summary>
        public int Spawn(float x, float y, string text, FloatingTextKind kind = FloatingTextKind.Normal)
        {
            if (_spawnedThisFrame >= MaxSpawnsPerFrame) return -1;
            _spawnedThisFrame++;

            for (var step = 0; step < MaxDeOverlapSteps && Overlaps(x, y); step++) y -= OverlapStep;

            var slot = FindFreeSlot();
            if (!_items[slot].Active) ActiveCount++;
            ref var item = ref _items[slot];
            item.Active = true;
            item.X = x;
            item.Y = y;
            item.SpawnY = y;
            item.Age = 0f;
            item.Life = Lifetime * (kind == FloatingTextKind.Critical ? CriticalLifetimeScale : 1f);
            item.Alpha = 1f;
            item.Scale = kind == FloatingTextKind.Critical ? PopScale : 1f;
            item.Text = text;
            item.Kind = kind;
            return slot;
        }

        /// <summary>Advances every live text. Returns true while any text is alive afterwards.</summary>
        public bool Tick(float deltaSeconds)
        {
            if (ActiveCount == 0) return false;
            if (!(deltaSeconds > 0f)) deltaSeconds = 0f;   // also NaN: an age that is NaN never reaches the lifetime
            for (var i = 0; i < _items.Length; i++)
            {
                ref var item = ref _items[i];
                if (!item.Active) continue;
                item.Age += deltaSeconds;
                if (item.Age >= item.Life)
                {
                    item.Active = false;
                    item.Text = null;
                    ActiveCount--;
                    continue;
                }

                var t = item.Age / item.Life;
                item.Y = item.SpawnY - RiseDistance * CountUpModel.Ease(t, CountEasing.EaseOutQuad);
                item.Alpha = t <= FadeStart ? 1f : 1f - (t - FadeStart) / (1f - FadeStart);
                item.Scale = item.Kind == FloatingTextKind.Critical
                    ? 1f + (PopScale - 1f) * Math.Max(0f, 1f - item.Age / PopSeconds)
                    : 1f;
            }

            return ActiveCount > 0;
        }

        /// <summary>Removes every text.</summary>
        public void Clear()
        {
            for (var i = 0; i < _items.Length; i++)
            {
                _items[i].Active = false;
                _items[i].Text = null;
            }

            ActiveCount = 0;
        }

        private bool Overlaps(float x, float y)
        {
            for (var i = 0; i < _items.Length; i++)
            {
                ref readonly var item = ref _items[i];
                if (!item.Active) continue;
                if (Math.Abs(item.X - x) < OverlapWidth && Math.Abs(item.Y - y) < OverlapStep) return true;
            }

            return false;
        }

        private int FindFreeSlot()
        {
            var oldest = 0;
            var oldestAge = -1f;
            for (var i = 0; i < _items.Length; i++)
            {
                if (!_items[i].Active) return i;
                if (_items[i].Age <= oldestAge) continue;
                oldestAge = _items[i].Age;
                oldest = i;
            }

            return oldest;
        }
    }
}
