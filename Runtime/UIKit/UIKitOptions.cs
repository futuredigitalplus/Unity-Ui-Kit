using System;
using FutureDigitalPlus.UIKit.DesignSystem.Theme.Data;
using UnityEngine;

namespace FutureDigitalPlus.UIKit
{
    /// <summary>How the <c>fdp-input-touch</c> class (the design system's touch-size tier) is decided.</summary>
    public enum MobileMode
    {
        /// <summary>On when running on a mobile platform or when the panel is narrower than
        /// <see cref="UIKitOptions.MobileBreakpoint"/>; re-evaluated whenever the panel is resized.</summary>
        Auto,
        /// <summary>Never add the class.</summary>
        Off,
        /// <summary>Always add the class.</summary>
        On,
    }

    /// <summary>
    /// What <see cref="UIKitInstaller.Install"/> / <see cref="UIKitHost"/> set up on a UIDocument root.
    /// Serializable, so a game can edit it in the inspector of a <see cref="UIKitHost"/>.
    /// </summary>
    [Serializable]
    public class UIKitOptions
    {
        /// <summary>Resources path of the design system's master stylesheet (it @imports the others).</summary>
        public const string BaseStylesResource = "FutureDigitalPlus/UIKit/DesignSystem/Styles/DesignSystem";

        /// <summary>The theme to wear; null means the base tokens (the dark palette).</summary>
        [Tooltip("Theme to wear. Leave empty for the base tokens (the dark palette).")]
        public ThemeData Theme;

        /// <summary>Adds the design system's master stylesheet to the panel root when it is not there yet.</summary>
        [Tooltip("Adds the design system's master stylesheet to the root if it is not there yet. " +
                 "Turn off when your UXML already references it.")]
        public bool InstallBaseStyles = true;

        /// <summary>How the <c>fdp-input-touch</c> class is decided.</summary>
        public MobileMode Mobile = MobileMode.Auto;

        /// <summary>Panel width in panel units below which <see cref="MobileMode.Auto"/> turns the class on.</summary>
        [Tooltip("Auto mobile mode: panel widths below this (in panel units) get the mobile class.")]
        public float MobileBreakpoint = 768f;

        /// <summary>Pads the root by the device safe area; turn it off when a shell or a <c>SafeAreaPanel</c> does it.</summary>
        [Tooltip("Pads the root by the device safe area (notches, rounded corners, home indicator).")]
        public bool ApplySafeArea = true;

        /// <summary>Runs the design system's runtime behaviours over the tree at install and then every 250 ms (idempotent), so elements added later are covered too.</summary>
        [Tooltip("Runs the design system's runtime behaviours over the tree (toggle knobs, tabs, " +
                 "dropdown popups, ...) at install and then every 250 ms, so elements added later are covered too. " +
                 "Idempotent.")]
        public bool EnsureBehaviours = true;
    }
}
