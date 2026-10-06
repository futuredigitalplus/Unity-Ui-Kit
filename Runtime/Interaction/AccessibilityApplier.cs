using System;
using UnityEngine.UIElements;

namespace FutureDigitalPlus.UIKit.Interaction
{
    /// <summary>
    /// Combines the player's <see cref="AccessibilityOptions"/> with the OS settings and writes the result as classes on a root element:
    /// <c>fdp-reduce-motion</c>, <c>fdp-high-contrast</c>, <c>fdp-bold-text</c> and <c>fdp-text-scale-125</c> .. <c>fdp-text-scale-200</c>.
    /// A11y.uss reads them. Code that animates on its own reads <see cref="Current"/> or calls <see cref="ScaleMotion"/>.
    /// <code>
    /// var a11y = AccessibilityApplier.Bind(uiDocument.rootVisualElement, savedOptions);
    /// a11y.Options.TextScalePercent = 150;
    /// a11y.Refresh();
    /// </code>
    /// </summary>
    public sealed class AccessibilityApplier : IDisposable
    {
        private readonly VisualElement _root;
        private readonly ISystemAccessibility _system;
        private readonly bool _ownsSystem;
        private EffectiveAccessibility _current;
        private bool _disposed;

        private AccessibilityApplier(VisualElement root, AccessibilityOptions options, ISystemAccessibility system)
        {
            _root = root;
            Options = options ?? new AccessibilityOptions();
            _ownsSystem = system == null;
            _system = system ?? new UnitySystemAccessibility();
            _system.Changed += Refresh;
            Refresh();
        }

        /// <summary>
        /// Starts applying to <paramref name="root"/>. Returns null when <paramref name="root"/> is null. Dispose to stop and remove the classes.
        /// </summary>
        /// <param name="root">The element that receives the classes (the document root, so every screen below inherits them).</param>
        /// <param name="options">The player's options; null creates defaults. The applier keeps a reference: change fields, then call <see cref="Refresh"/>.</param>
        /// <param name="system">Where the OS values come from; null reads what Unity exposes.</param>
        public static AccessibilityApplier Bind(VisualElement root, AccessibilityOptions options = null, ISystemAccessibility system = null) =>
            root == null ? null : new AccessibilityApplier(root, options, system);

        /// <summary>The player's options. Change fields and call <see cref="Refresh"/>.</summary>
        public AccessibilityOptions Options { get; }

        /// <summary>The values in force after combining <see cref="Options"/> with the OS settings.</summary>
        public EffectiveAccessibility Current => _current;

        /// <summary>Raised on the main thread after <see cref="Current"/> changed and the classes were updated.</summary>
        public event Action<EffectiveAccessibility> Changed;

        /// <summary>Scales an animation duration: 0 under reduced motion, unchanged otherwise. Safe to hand out as a delegate (it always reads the latest value).</summary>
        public int ScaleMotion(int milliseconds) => _current.ScaleMotion(milliseconds);

        /// <summary>Re-reads <see cref="Options"/> and the OS values, rewrites the classes and raises <see cref="Changed"/> when something differs.</summary>
        public void Refresh()
        {
            if (_disposed) return;
            var next = Options.Resolve(_system.Current);
            var changed = next != _current;
            _current = next;
            Apply(next);
            if (changed) Changed?.Invoke(next);
        }

        /// <summary>Stops applying and removes the classes from the root.</summary>
        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;
            _system.Changed -= Refresh;
            if (_ownsSystem && _system is IDisposable disposable) disposable.Dispose();
            Apply(EffectiveAccessibility.Default);
        }

        private void Apply(EffectiveAccessibility state)
        {
            _root.EnableInClassList(AccessibilityClasses.ReduceMotion, state.ReduceMotion);
            _root.EnableInClassList(AccessibilityClasses.HighContrast, state.HighContrast);
            _root.EnableInClassList(AccessibilityClasses.BoldText, state.BoldText);
            var step = state.TextScaleStep;
            foreach (var s in TextScaleSteps.All)
            {
                var cls = AccessibilityClasses.TextScale(s);
                if (cls != null) _root.EnableInClassList(cls, s == step);
            }
        }
    }
}
