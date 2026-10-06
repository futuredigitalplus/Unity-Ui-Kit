using System;
#if FDP_UIKIT_ACCESSIBILITY
using UnityEngine.Accessibility;
#endif

namespace FutureDigitalPlus.UIKit.Interaction
{
    /// <summary>
    /// Reads the OS font scale and bold text setting through <c>UnityEngine.Accessibility.AccessibilitySettings</c> when the built-in accessibility module
    /// is present (define <c>FDP_UIKIT_ACCESSIBILITY</c>); without the module it reports <see cref="SystemAccessibility.Default"/> and never changes.
    /// The values are live on iOS and Android only; on desktop the font scale is 1.
    /// </summary>
    public sealed class UnitySystemAccessibility : ISystemAccessibility, IDisposable
    {
        /// <inheritdoc />
#if FDP_UIKIT_ACCESSIBILITY
        public event Action Changed;
#else
        public event Action Changed { add { } remove { } }
#endif

        /// <summary>Starts listening to the OS change events.</summary>
        public UnitySystemAccessibility()
        {
#if FDP_UIKIT_ACCESSIBILITY
            AccessibilitySettings.fontScaleChanged += OnFontScale;
            AccessibilitySettings.boldTextStatusChanged += OnBold;
#endif
        }

        /// <inheritdoc />
        public SystemAccessibility Current
        {
            get
            {
#if FDP_UIKIT_ACCESSIBILITY
                var scale = AccessibilitySettings.fontScale;
                return new SystemAccessibility(scale > 0.1f ? scale : 1f, AccessibilitySettings.isBoldTextEnabled, false, false);
#else
                return SystemAccessibility.Default;
#endif
            }
        }

        /// <summary>Stops listening to the OS change events.</summary>
        public void Dispose()
        {
#if FDP_UIKIT_ACCESSIBILITY
            AccessibilitySettings.fontScaleChanged -= OnFontScale;
            AccessibilitySettings.boldTextStatusChanged -= OnBold;
#endif
        }

#if FDP_UIKIT_ACCESSIBILITY
        private void OnFontScale(float scale) => Changed?.Invoke();

        private void OnBold(bool enabled) => Changed?.Invoke();
#endif
    }
}
