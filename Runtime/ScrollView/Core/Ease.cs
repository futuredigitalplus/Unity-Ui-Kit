/*
 * EasingCore (https://github.com/setchi/EasingCore)
 * Copyright (c) 2020 setchi
 * Licensed under MIT (https://github.com/setchi/EasingCore/blob/master/LICENSE)
 */

// FutureDigitalPlus: from FancyScrollView Sources/Runtime/Scroller/EasingCore.cs. Namespace FutureDigitalPlus.UIKit.ScrollView (was EasingCore);
// UnityEngine.Mathf replaced by System.MathF so the file builds without the engine; `Easing` renamed EaseFunctions and `EasingFunction`
// renamed EaseFunction (both are UI Toolkit type names); the functions are cached instead of allocated on every Get.

using System;

namespace FutureDigitalPlus.UIKit.ScrollView
{
    /// <summary>The easing curves of <see cref="EaseFunctions"/>.</summary>
    public enum Ease
    {
        /// <summary>No easing.</summary>
        Linear,
        /// <summary>Back, accelerating.</summary>
        InBack,
        /// <summary>Bounce, accelerating.</summary>
        InBounce,
        /// <summary>Circular, accelerating.</summary>
        InCirc,
        /// <summary>Cubic, accelerating.</summary>
        InCubic,
        /// <summary>Elastic, accelerating.</summary>
        InElastic,
        /// <summary>Exponential, accelerating.</summary>
        InExpo,
        /// <summary>Quadratic, accelerating.</summary>
        InQuad,
        /// <summary>Quartic, accelerating.</summary>
        InQuart,
        /// <summary>Quintic, accelerating.</summary>
        InQuint,
        /// <summary>Sine, accelerating.</summary>
        InSine,
        /// <summary>Back, decelerating.</summary>
        OutBack,
        /// <summary>Bounce, decelerating.</summary>
        OutBounce,
        /// <summary>Circular, decelerating.</summary>
        OutCirc,
        /// <summary>Cubic, decelerating.</summary>
        OutCubic,
        /// <summary>Elastic, decelerating.</summary>
        OutElastic,
        /// <summary>Exponential, decelerating.</summary>
        OutExpo,
        /// <summary>Quadratic, decelerating.</summary>
        OutQuad,
        /// <summary>Quartic, decelerating.</summary>
        OutQuart,
        /// <summary>Quintic, decelerating.</summary>
        OutQuint,
        /// <summary>Sine, decelerating.</summary>
        OutSine,
        /// <summary>Back, accelerating then decelerating.</summary>
        InOutBack,
        /// <summary>Bounce, accelerating then decelerating.</summary>
        InOutBounce,
        /// <summary>Circular, accelerating then decelerating.</summary>
        InOutCirc,
        /// <summary>Cubic, accelerating then decelerating.</summary>
        InOutCubic,
        /// <summary>Elastic, accelerating then decelerating.</summary>
        InOutElastic,
        /// <summary>Exponential, accelerating then decelerating.</summary>
        InOutExpo,
        /// <summary>Quadratic, accelerating then decelerating.</summary>
        InOutQuad,
        /// <summary>Quartic, accelerating then decelerating.</summary>
        InOutQuart,
        /// <summary>Quintic, accelerating then decelerating.</summary>
        InOutQuint,
        /// <summary>Sine, accelerating then decelerating.</summary>
        InOutSine,
    }

    /// <summary>Maps progress 0..1 to eased progress (0 at 0, 1 at 1; back and elastic curves overshoot in between).</summary>
    public delegate float EaseFunction(float t);

    /// <summary>The easing functions used by <see cref="ScrollerModel.ScrollTo(float, float, Ease, Action)"/> and snapping.</summary>
    public static class EaseFunctions
    {
        private const float PI = MathF.PI;

        private static readonly EaseFunction[] Cache =
        {
            Linear, InBack, InBounce, InCirc, InCubic, InElastic, InExpo, InQuad, InQuart, InQuint, InSine,
            OutBack, OutBounce, OutCirc, OutCubic, OutElastic, OutExpo, OutQuad, OutQuart, OutQuint, OutSine,
            InOutBack, InOutBounce, InOutCirc, InOutCubic, InOutElastic, InOutExpo, InOutQuad, InOutQuart, InOutQuint, InOutSine,
        };

        /// <summary>The function for <paramref name="type"/>; an undefined value gives <see cref="Ease.Linear"/>.</summary>
        public static EaseFunction Get(Ease type)
        {
            var i = (int)type;
            return i >= 0 && i < Cache.Length ? Cache[i] : Cache[0];
        }

        private static float Linear(float t) => t;

        private static float InBack(float t) => t * t * t - t * MathF.Sin(t * PI);

        private static float OutBack(float t) => 1f - InBack(1f - t);

        private static float InOutBack(float t) =>
            t < 0.5f
                ? 0.5f * InBack(2f * t)
                : 0.5f * OutBack(2f * t - 1f) + 0.5f;

        private static float InBounce(float t) => 1f - OutBounce(1f - t);

        private static float OutBounce(float t) =>
            t < 4f / 11.0f ?
                (121f * t * t) / 16.0f :
            t < 8f / 11.0f ?
                (363f / 40.0f * t * t) - (99f / 10.0f * t) + 17f / 5.0f :
            t < 9f / 10.0f ?
                (4356f / 361.0f * t * t) - (35442f / 1805.0f * t) + 16061f / 1805.0f :
                (54f / 5.0f * t * t) - (513f / 25.0f * t) + 268f / 25.0f;

        private static float InOutBounce(float t) =>
            t < 0.5f
                ? 0.5f * InBounce(2f * t)
                : 0.5f * OutBounce(2f * t - 1f) + 0.5f;

        private static float InCirc(float t) => 1f - MathF.Sqrt(1f - (t * t));

        private static float OutCirc(float t) => MathF.Sqrt((2f - t) * t);

        private static float InOutCirc(float t) =>
            t < 0.5f
                ? 0.5f * (1 - MathF.Sqrt(1f - 4f * (t * t)))
                : 0.5f * (MathF.Sqrt(-((2f * t) - 3f) * ((2f * t) - 1f)) + 1f);

        private static float InCubic(float t) => t * t * t;

        private static float OutCubic(float t) => InCubic(t - 1f) + 1f;

        private static float InOutCubic(float t) =>
            t < 0.5f
                ? 4f * t * t * t
                : 0.5f * InCubic(2f * t - 2f) + 1f;

        private static float InElastic(float t) => MathF.Sin(13f * (PI * 0.5f) * t) * MathF.Pow(2f, 10f * (t - 1f));

        private static float OutElastic(float t) => MathF.Sin(-13f * (PI * 0.5f) * (t + 1)) * MathF.Pow(2f, -10f * t) + 1f;

        private static float InOutElastic(float t) =>
            t < 0.5f
                ? 0.5f * MathF.Sin(13f * (PI * 0.5f) * (2f * t)) * MathF.Pow(2f, 10f * ((2f * t) - 1f))
                : 0.5f * (MathF.Sin(-13f * (PI * 0.5f) * ((2f * t - 1f) + 1f)) * MathF.Pow(2f, -10f * (2f * t - 1f)) + 2f);

        private static float InExpo(float t) => ScrollMath.Approximately(0.0f, t) ? t : MathF.Pow(2f, 10f * (t - 1f));

        private static float OutExpo(float t) => ScrollMath.Approximately(1.0f, t) ? t : 1f - MathF.Pow(2f, -10f * t);

        private static float InOutExpo(float v) =>
            ScrollMath.Approximately(0.0f, v) || ScrollMath.Approximately(1.0f, v)
                ? v
                : v < 0.5f
                    ? 0.5f * MathF.Pow(2f, (20f * v) - 10f)
                    : -0.5f * MathF.Pow(2f, (-20f * v) + 10f) + 1f;

        private static float InQuad(float t) => t * t;

        private static float OutQuad(float t) => -t * (t - 2f);

        private static float InOutQuad(float t) =>
            t < 0.5f
                ? 2f * t * t
                : -2f * t * t + 4f * t - 1f;

        private static float InQuart(float t) => t * t * t * t;

        private static float OutQuart(float t)
        {
            var u = t - 1f;
            return u * u * u * (1f - t) + 1f;
        }

        private static float InOutQuart(float t) =>
            t < 0.5f
                ? 8f * InQuart(t)
                : -8f * InQuart(t - 1f) + 1f;

        private static float InQuint(float t) => t * t * t * t * t;

        private static float OutQuint(float t) => InQuint(t - 1f) + 1f;

        private static float InOutQuint(float t) =>
            t < 0.5f
                ? 16f * InQuint(t)
                : 0.5f * InQuint(2f * t - 2f) + 1f;

        private static float InSine(float t) => MathF.Sin((t - 1f) * (PI * 0.5f)) + 1f;

        private static float OutSine(float t) => MathF.Sin(t * (PI * 0.5f));

        private static float InOutSine(float t) => 0.5f * (1f - MathF.Cos(t * PI));
    }
}
