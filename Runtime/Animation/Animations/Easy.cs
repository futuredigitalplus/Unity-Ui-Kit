// FutureDigitalPlus: renamed by Tools~/rename/rename.py (namespaces, resource paths); see SOURCE.md "Rename record".
// FutureDigitalPlus: rewritten. Upstream's copy of the standard curves was decompiled code of Unity's own Easing class
// (header "From com.unity.ui"), which an MIT notice cannot cover. The curves now call Unity's public
// UnityEngine.UIElements.Experimental.Easing; only OutExpo and EaseInOutQuint (the upstream author's own) are written here.
// See SOURCE.md "Provenance notes".
// ----------------------------------------------------------------------------
// -- Project : https://github.com/instance-id/ElementAnimationToolkit       --
// -- instance.id 2020 | http://github.com/instance-id | http://instance.id  --
// ----------------------------------------------------------------------------

using System;
using UnityEngine;
using UnityEasing = UnityEngine.UIElements.Experimental.Easing;

namespace FutureDigitalPlus.UIKit.Animation
{
    /// <summary>
    ///   <para>A collection of easing curves to be used with ValueAnimations. Each maps a progress t in [0, 1] to an eased progress.</para>
    /// </summary>
    public static class Easy
    {
        /// <summary>Jumps from 0 to 1 at t = 0.5.</summary>
        public static float Step(float t) => UnityEasing.Step(t);

        /// <summary>No easing.</summary>
        public static float Linear(float t) => UnityEasing.Linear(t);

        /// <summary>Sine curve, slow start.</summary>
        public static float InSine(float t) => UnityEasing.InSine(t);

        /// <summary>Sine curve, slow end.</summary>
        public static float OutSine(float t) => UnityEasing.OutSine(t);

        /// <summary>Sine curve, slow start and end.</summary>
        public static float InOutSine(float t) => UnityEasing.InOutSine(t);

        /// <summary>Quadratic, slow start.</summary>
        public static float InQuad(float t) => UnityEasing.InQuad(t);

        /// <summary>Quadratic, slow end.</summary>
        public static float OutQuad(float t) => UnityEasing.OutQuad(t);

        /// <summary>Exponential, slow end. Reaches exactly 1 at t = 1.</summary>
        public static float OutExpo(float t) => t.Equals(1f) ? 1 : 1 - Mathf.Pow(2, -10 * t);

        /// <summary>Quadratic, slow start and end.</summary>
        public static float InOutQuad(float t) => UnityEasing.InOutQuad(t);

        /// <summary>Cubic, slow start.</summary>
        public static float InCubic(float t) => UnityEasing.InCubic(t);

        /// <summary>Cubic, slow end.</summary>
        public static float OutCubic(float t) => UnityEasing.OutCubic(t);

        /// <summary>Cubic, slow start and end.</summary>
        public static float InOutCubic(float t) => UnityEasing.InOutCubic(t);

        /// <summary>Power curve of the given exponent, slow start.</summary>
        public static float InPower(float t, int power) => UnityEasing.InPower(t, power);

        /// <summary>Power curve of the given exponent, slow end.</summary>
        public static float OutPower(float t, int power) => UnityEasing.OutPower(t, power);

        /// <summary>Power curve of the given exponent, slow start and end.</summary>
        public static float InOutPower(float t, int power) => UnityEasing.InOutPower(t, power);

        /// <summary>Bounce at the start.</summary>
        public static float InBounce(float t) => UnityEasing.InBounce(t);

        /// <summary>Bounce at the end.</summary>
        public static float OutBounce(float t) => UnityEasing.OutBounce(t);

        /// <summary>Bounce at the start and the end.</summary>
        public static float InOutBounce(float t) => UnityEasing.InOutBounce(t);

        /// <summary>Elastic wobble at the start.</summary>
        public static float InElastic(float t) => UnityEasing.InElastic(t);

        /// <summary>Elastic wobble at the end.</summary>
        public static float OutElastic(float t) => UnityEasing.OutElastic(t);

        /// <summary>Elastic wobble at the start and the end.</summary>
        public static float InOutElastic(float t) => UnityEasing.InOutElastic(t);

        /// <summary>Pulls back before the start.</summary>
        public static float InBack(float t) => UnityEasing.InBack(t);

        /// <summary>Overshoots before the end.</summary>
        public static float OutBack(float t) => UnityEasing.OutBack(t);

        /// <summary>Pulls back at the start and overshoots at the end.</summary>
        public static float InOutBack(float t) => UnityEasing.InOutBack(t);

        /// <summary>Like <see cref="InBack(float)"/> with an explicit overshoot amount <paramref name="s"/>.</summary>
        public static float InBack(float t, float s) => UnityEasing.InBack(t, s);

        /// <summary>Like <see cref="OutBack(float)"/> with an explicit overshoot amount <paramref name="s"/>.</summary>
        public static float OutBack(float t, float s) => UnityEasing.OutBack(t, s);

        /// <summary>Like <see cref="InOutBack(float)"/> with an explicit overshoot amount <paramref name="s"/>.</summary>
        public static float InOutBack(float t, float s) => UnityEasing.InOutBack(t, s);

        /// <summary>Circular, slow start.</summary>
        public static float InCirc(float t) => UnityEasing.InCirc(t);

        /// <summary>Circular, slow end.</summary>
        public static float OutCirc(float t) => UnityEasing.OutCirc(t);

        /// <summary>Circular, slow start and end.</summary>
        public static float InOutCirc(float t) => UnityEasing.InOutCirc(t);

        /// <summary>Quintic, slow start and end.</summary>
        public static float EaseInOutQuint(float t)
        {
            return (float) (t < 0.5f ? 16f * t * t * t * t * t : 1 - Math.Pow(-2 * t + 2, 5) / 2);
        }
    }
}
