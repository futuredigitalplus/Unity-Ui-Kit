using System;
using System.Runtime.CompilerServices;
using FutureDigitalPlus.UIKit.Animation;
using UnityEngine.UIElements;
using UnityEngine.UIElements.Experimental;

namespace FutureDigitalPlus.UIKit
{
    /// <summary>
    /// A tiny convenience layer over the Animation module for the two motions nearly every screen needs.
    /// Everything else stays in the Animation module's own extension methods (<c>AnimateOpacity</c>,
    /// <c>AnimateWidth</c>, <c>Hover*</c>, ...), which can be called directly.
    /// A fade that starts while another fade of the same element is still running replaces it: the older one stops where it is, its
    /// <c>onDone</c> never runs, and the new one starts from the opacity the element has at that moment. So a quick FadeOut then FadeIn
    /// (a panel toggled twice) ends visible, not hidden by the late completion of the first fade.
    /// </summary>
    public static class UIKitMotion
    {
        /// <summary>The duration, in milliseconds, used when a fade is called without one.</summary>
        public const int DefaultDurationMs = 200;

        private sealed class RunningFade
        {
            public int Version;
            public ValueAnimation<StyleValues> Animation;
        }

        private static readonly ConditionalWeakTable<VisualElement, RunningFade> Fades = new();

        /// <summary>Shows the element and fades it from transparent to opaque. <paramref name="onDone"/> runs at the end.</summary>
        public static void FadeIn(this VisualElement element, int durationMs = DefaultDurationMs, Action onDone = null)
        {
            if (element == null) return;
            element.style.display = DisplayStyle.Flex;
            Fade(element, 0f, 1f, durationMs, onDone);
        }

        /// <summary>Fades the element out; with <paramref name="hide"/> it is then removed from layout (display none).</summary>
        public static void FadeOut(this VisualElement element, int durationMs = DefaultDurationMs, bool hide = true,
            Action onDone = null)
        {
            if (element == null) return;
            Fade(element, 1f, 0f, durationMs, () =>
            {
                if (hide) element.style.display = DisplayStyle.None;
                onDone?.Invoke();
            });
        }

        private static void Fade(VisualElement element, float from, float to, int durationMs, Action finished)
        {
            var running = Fades.GetOrCreateValue(element);
            var replaced = running.Animation;
            var version = ++running.Version;
            if (replaced != null)
            {
                from = element.resolvedStyle.opacity;
                running.Animation = null;
                if (replaced.isRunning) replaced.Stop();   // its completion, if Stop raises it, is ignored: the version no longer matches
            }

            running.Animation = element.AnimateOpacity(from, to, durationMs, () =>
            {
                if (running.Version != version) return;
                running.Animation = null;
                finished?.Invoke();
            }, Easy.OutCubic, reanimate: true);
        }
    }
}
