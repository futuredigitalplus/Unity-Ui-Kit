using System;
using System.Collections.Generic;
using UnityEngine.UIElements;

namespace FutureDigitalPlus.UIKit.Foundation
{
    /// <summary>
    /// Carries the result of a <see cref="PanelScaleDriver"/> into the visual tree: the form-factor class on the root (<c>fdp-ff-phone</c>, <c>fdp-ff-pad</c>,
    /// <c>fdp-ff-flip-cover</c>, <c>fdp-ff-fold-cover</c>, <c>fdp-ff-fold-inner</c>), the posture classes (<c>fdp-posture-half-opened</c> with <c>fdp-hinge-horizontal</c>
    /// or <c>fdp-hinge-vertical</c>) and the rule's <see cref="ResponsiveRule.MaxContentWidth"/> as <c>max-width</c> on every element with <see cref="ColumnClass"/>.
    /// Screens then adapt with plain descendant selectors (<c>.fdp-ff-flip-cover .my-row { ... }</c>). Re-applies when the driver applies a new scale; while a width
    /// limit is set it also checks four times a second for columns of screens shown since (a new screen does not change the root's geometry). Dispose to stop; the
    /// classes stay.
    /// <code>
    /// var binder = new ResponsiveRootBinder(shell.Root, driver) { ColumnClass = "my-page-column" };
    /// </code>
    /// </summary>
    public sealed class ResponsiveRootBinder : IDisposable
    {
        /// <summary>The default class of the content columns whose width the rules limit.</summary>
        public const string DefaultColumnClass = "fdp-responsive-column";

        static readonly List<ResponsiveRootBinder> s_active = new List<ResponsiveRootBinder>();

        readonly VisualElement _root;
        readonly PanelScaleDriver _driver;
        DevicePosture _posture;
        HingeAxis _hinge;
        string _columnClass = DefaultColumnClass;
        float _appliedWidth = -1f;
        IVisualElementScheduledItem _columnWatch;
        bool _disposed;

        /// <summary>How often, in milliseconds, new columns are looked for while a width limit is set.</summary>
        public const long ColumnCheckMilliseconds = 250;

        /// <summary>Starts binding <paramref name="driver"/> to <paramref name="root"/> (usually the shell root) and applies once.</summary>
        public ResponsiveRootBinder(VisualElement root, PanelScaleDriver driver)
        {
            _root = root ?? throw new ArgumentNullException(nameof(root));
            _driver = driver ?? throw new ArgumentNullException(nameof(driver));
            _driver.Applied += OnApplied;
            s_active.Add(this);
            Refresh();
        }

        /// <summary>The binders that exist and are not disposed, oldest first. Tools (the Responsive Manager) find the binder of a driver here to preview a posture.</summary>
        public static IReadOnlyList<ResponsiveRootBinder> Active
        {
            get { return s_active; }
        }

        /// <summary>The driver whose result is bound.</summary>
        public PanelScaleDriver Driver
        {
            get { return _driver; }
        }

        /// <summary>The element that gets the classes.</summary>
        public VisualElement Root
        {
            get { return _root; }
        }

        /// <summary>The form factor that is applied now.</summary>
        public DeviceFormFactor FormFactor { get; private set; }

        /// <summary>
        /// How the device is held. The size of the screen cannot tell it: a host with a posture source (a foldable plugin) or a preview sets it. Setting it re-applies at once.
        /// </summary>
        public DevicePosture Posture
        {
            get { return _posture; }
            set
            {
                _posture = value;
                Refresh();
            }
        }

        /// <summary>The direction of the hinge while <see cref="Posture"/> is half opened. Setting it re-applies at once.</summary>
        public HingeAxis Hinge
        {
            get { return _hinge; }
            set
            {
                _hinge = value;
                Refresh();
            }
        }

        /// <summary>The class of the elements whose width <see cref="ResponsiveRule.MaxContentWidth"/> limits (default <see cref="DefaultColumnClass"/>).</summary>
        public string ColumnClass
        {
            get { return _columnClass; }
            set
            {
                _columnClass = string.IsNullOrEmpty(value) ? DefaultColumnClass : value;
                _appliedWidth = -1f;
                Refresh();
            }
        }

        /// <summary>Applies the classes and the column width now.</summary>
        public void Refresh()
        {
            if (_disposed)
                return;
            FormFactor = _driver.FormFactor;
            for (int i = 0; i < DeviceFormFactors.All.Length; i++)
                _root.EnableInClassList(DeviceFormFactors.ClassName(DeviceFormFactors.All[i]), DeviceFormFactors.All[i] == FormFactor);
            bool halfOpened = _posture == DevicePosture.HalfOpened;
            _root.EnableInClassList(DeviceFormFactors.ClassHalfOpened, halfOpened);
            _root.EnableInClassList(DeviceFormFactors.ClassHingeHorizontal, halfOpened && _hinge == HingeAxis.Horizontal);
            _root.EnableInClassList(DeviceFormFactors.ClassHingeVertical, halfOpened && _hinge == HingeAxis.Vertical);
            ApplyColumnWidth(_driver.Rule.MaxContentWidth, false);
        }

        /// <summary>Stops binding. The classes and widths stay as they are.</summary>
        public void Dispose()
        {
            if (_disposed)
                return;
            _disposed = true;
            s_active.Remove(this);
            _driver.Applied -= OnApplied;
            if (_columnWatch != null)
                _columnWatch.Pause();
        }

        void OnApplied(PanelScale scale)
        {
            Refresh();
        }

        void ApplyColumnWidth(float width, bool always)
        {
            UpdateColumnWatch(width > 0f);
            if (!always && Math.Abs(width - _appliedWidth) < 0.01f)
                return;
            _appliedWidth = width;
            StyleLength value = width > 0f ? new StyleLength(width) : new StyleLength(StyleKeyword.Null);
            _root.Query(className: _columnClass).ForEach(column =>
            {
                if (column.style.maxWidth != value)
                    column.style.maxWidth = value;
            });
        }

        // a game without width rules pays nothing: the watch runs only while a limit is set
        void UpdateColumnWatch(bool needed)
        {
            if (needed)
            {
                if (_columnWatch == null)
                    _columnWatch = _root.schedule.Execute(() => ApplyColumnWidth(_driver.Rule.MaxContentWidth, true)).Every(ColumnCheckMilliseconds);
                else
                    _columnWatch.Resume();
            }
            else if (_columnWatch != null)
            {
                _columnWatch.Pause();
            }
        }
    }
}
