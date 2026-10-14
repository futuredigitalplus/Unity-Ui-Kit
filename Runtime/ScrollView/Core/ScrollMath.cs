/*
 * FancyScrollView (https://github.com/setchi/FancyScrollView)
 * Copyright (c) 2020 setchi
 * Licensed under MIT (https://github.com/setchi/FancyScrollView/blob/master/LICENSE)
 */

// FutureDigitalPlus: new file. CircularIndex, CircularPosition and RubberDelta are the private helpers of upstream FancyScrollView.cs and
// Scroller.cs, made public and shared. The rest are engine-free copies of the UnityEngine.Mathf methods upstream calls, with the same
// results (Unity's documented SmoothDamp and Approximately, banker's rounding in RoundToInt), so Core needs no engine reference.
// Approximately always uses the smallest normal float as Unity's Epsilon, so it also holds where denormals are flushed to zero.

using System;

namespace FutureDigitalPlus.UIKit.ScrollView
{
    /// <summary>The small math the scroller and the layouts share. Engine-free, same results as the <c>Mathf</c> calls they replace.</summary>
    public static class ScrollMath
    {
        /// <summary>Wraps <paramref name="i"/> into 0..size-1 (also for negatives). 0 when <paramref name="size"/> is below 1.</summary>
        public static int CircularIndex(int i, int size) => size < 1 ? 0 : i < 0 ? size - 1 + (i + 1) % size : i % size;

        /// <summary>Wraps a scroll position into 0..size (also for negatives). 0 when <paramref name="size"/> is below 1.</summary>
        public static float CircularPosition(float p, int size) => size < 1 ? 0 : p < 0 ? size - 1 + (p + 1) % size : p % size;

        /// <summary>How far content pulled <paramref name="overStretching"/> past an end gives way (the rubber band of an elastic scroller).</summary>
        public static float RubberDelta(float overStretching, float viewSize) =>
            (1 - 1 / (Math.Abs(overStretching) * 0.55f / viewSize + 1)) * viewSize * Sign(overStretching);

        // Mathf.Epsilon where the CPU flushes denormals to zero (ARM phones): the smallest normal float. float.Epsilon is a denormal
        // there, 8 times it reads as 0, and Approximately(0, 0) would be false.
        private const float FloatMinNormal = 1.17549435E-38f;

        /// <summary>Same as <c>Mathf.Approximately</c> on a CPU that flushes denormals to zero; on others it differs only below 1e-37.</summary>
        public static bool Approximately(float a, float b) =>
            Math.Abs(b - a) < Math.Max(1E-06f * Math.Max(Math.Abs(a), Math.Abs(b)), FloatMinNormal * 8f);

        /// <summary>Same as <c>Mathf.Sign</c>: 1 for zero and positive values, -1 for negative ones.</summary>
        public static float Sign(float f) => f >= 0f ? 1f : -1f;

        /// <summary>Same as <c>Mathf.Clamp</c> (when <paramref name="min"/> is above <paramref name="max"/>, a value above max gives max).</summary>
        public static float Clamp(float value, float min, float max)
        {
            if (value < min) value = min;
            else if (value > max) value = max;
            return value;
        }

        /// <summary>Same as <c>Mathf.Clamp</c> for integers.</summary>
        public static int Clamp(int value, int min, int max)
        {
            if (value < min) value = min;
            else if (value > max) value = max;
            return value;
        }

        /// <summary>Same as <c>Mathf.Clamp01</c>.</summary>
        public static float Clamp01(float value) => value < 0f ? 0f : value > 1f ? 1f : value;

        /// <summary>Same as <c>Mathf.Lerp</c> (t clamped to 0..1).</summary>
        public static float Lerp(float a, float b, float t) => a + (b - a) * Clamp01(t);

        /// <summary>Same as <c>Mathf.LerpUnclamped</c>.</summary>
        public static float LerpUnclamped(float a, float b, float t) => a + (b - a) * t;

        /// <summary>Same as <c>Mathf.RoundToInt</c> (halves round to even).</summary>
        public static int RoundToInt(float f) => (int)Math.Round(f);

        /// <summary>Same as <c>Mathf.CeilToInt</c>.</summary>
        public static int CeilToInt(float f) => (int)Math.Ceiling(f);

        /// <summary>Same as <c>Mathf.SmoothDamp</c>: moves <paramref name="current"/> towards <paramref name="target"/> like a critically damped spring.</summary>
        public static float SmoothDamp(float current, float target, ref float currentVelocity, float smoothTime, float maxSpeed, float deltaTime)
        {
            smoothTime = Math.Max(0.0001f, smoothTime);
            var omega = 2f / smoothTime;
            var x = omega * deltaTime;
            var exp = 1f / (1f + x + 0.48f * x * x + 0.235f * x * x * x);
            var change = current - target;
            var originalTo = target;
            var maxChange = maxSpeed * smoothTime;
            change = Clamp(change, -maxChange, maxChange);
            target = current - change;
            var temp = (currentVelocity + omega * change) * deltaTime;
            currentVelocity = (currentVelocity - omega * temp) * exp;
            var output = target + (change + temp) * exp;
            if (originalTo - current > 0f == output > originalTo)
            {
                output = originalTo;
                currentVelocity = (output - originalTo) / deltaTime;
            }

            return output;
        }
    }
}
