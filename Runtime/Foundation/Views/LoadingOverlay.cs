using System;
using UnityEngine;
using UnityEngine.UIElements;

namespace FutureDigitalPlus.UIKit.Foundation
{
    /// <summary>
    /// UI Toolkit view of <see cref="LoadingTokens"/>: reference-counted loading periods with anti-flicker timing (shown after 250 ms, kept at least 400 ms) and
    /// optional input blocking. The backdrop starts absorbing input the moment a blocking token begins; the graphic fades in only after the show delay.
    /// The spinner (the design system's <c>ds-spinner</c>) is turned by a scheduler loop that exists only while the overlay is visible (USS has no keyframe
    /// animation). A UXML template named <c>LoadingOverlay</c> in the asset source replaces the code-built view. Time is real time, so it works with
    /// <c>Time.timeScale = 0</c>.
    /// <code>using (ui.Loading.Begin(UiText.FromLiteral("Loading"))) { await LoadAsync(); }</code>
    /// </summary>
    public sealed class LoadingOverlay : ILoadingOverlay
    {
        const long SpinnerIntervalMilliseconds = 16;
        const float DegreesPerStep = 6f;

        readonly LoadingTokens _tokens;
        readonly InputBlockCounter _blocks;
        readonly UiTextSource _text;
        readonly VisualElement _backdrop;
        readonly VisualElement _spinner;
        readonly Label _message;
        readonly VisualElement _progress;
        readonly VisualElement _fill;
        IVisualElementScheduledItem _spinnerLoop;
        IVisualElementScheduledItem _dueTimer;
        float _angle;
        int _blockToken;
        bool _disposed;

        /// <summary>Creates the overlay inside <paramref name="host"/> (the loading layer). <paramref name="now"/> is the clock in seconds; null uses real time.</summary>
        public LoadingOverlay(VisualElement host, IUiAssetSource assets, InputBlockCounter blocks, UiTextSource text = null, Func<double> now = null)
        {
            if (host == null)
                throw new ArgumentNullException(nameof(host));
            if (blocks == null)
                throw new ArgumentNullException(nameof(blocks));
            _blocks = blocks;
            _text = text ?? new UiTextSource();
            _tokens = new LoadingTokens(now ?? (() => Time.realtimeSinceStartupAsDouble));

            VisualElement backdrop = null;
            VisualTreeAsset tree = assets == null ? null : assets.LoadUxml(UiNames.TemplateLoadingOverlay);
            if (tree != null)
            {
                TemplateContainer container = tree.Instantiate();
                backdrop = container.Q<VisualElement>(UiNames.LoadingBackdrop);
                if (backdrop != null)
                    backdrop.RemoveFromHierarchy();
            }
            _backdrop = backdrop ?? BuildBackdrop();
            FoundationLayout.Fill(_backdrop);
            FoundationLayout.AbsorbPointer(_backdrop);
            _backdrop.pickingMode = PickingMode.Ignore;
            IgnorePointerBelow(_backdrop);
            host.Add(_backdrop);

            _spinner = _backdrop.Q<VisualElement>(UiNames.LoadingSpinner);
            _message = _backdrop.Q<Label>(UiNames.LoadingMessage);
            _progress = _backdrop.Q<VisualElement>(UiNames.LoadingProgress);
            _fill = _backdrop.Q<VisualElement>(UiNames.LoadingProgressFill);
            Render();
            _tokens.Changed += OnChanged;
        }

        /// <inheritdoc />
        public bool BlocksInput
        {
            get { return _tokens.BlocksInput; }
        }

        /// <summary>True while the overlay graphic is shown (after the delay, until the minimum time has passed).</summary>
        public bool IsVisible
        {
            get { return _tokens.IsVisible; }
        }

        /// <summary>Number of loading periods that are alive.</summary>
        public int ActiveCount
        {
            get { return _tokens.ActiveCount; }
        }

        /// <inheritdoc />
        public LoadingToken Begin(UiText message = null, bool blockInput = true)
        {
            int id = _tokens.Begin(message, blockInput);
            return new LoadingToken(this, id);
        }

        /// <inheritdoc />
        public void End(LoadingToken token)
        {
            if (token.IsIssuedBy(this))
                _tokens.End(token.Id);
        }

        /// <inheritdoc />
        public void Update(LoadingToken token, UiText message, bool showSpinner)
        {
            if (token.IsIssuedBy(this))
                _tokens.Update(token.Id, message, showSpinner);
        }

        /// <inheritdoc />
        public void SetProgress(LoadingToken token, float normalized)
        {
            if (token.IsIssuedBy(this))
                _tokens.SetProgress(token.Id, normalized);
        }

        /// <inheritdoc />
        public bool HandleBack()
        {
            return _tokens.BlocksInput;
        }

        /// <summary>Stops the timers and releases the input block. The shell calls it on dispose.</summary>
        public void Dispose()
        {
            if (_disposed)
                return;
            _disposed = true;
            _tokens.Changed -= OnChanged;
            StopSpinner();
            if (_dueTimer != null)
            {
                _dueTimer.Pause();
                _dueTimer = null;
            }
            if (_blockToken != 0)
            {
                _blocks.Release(_blockToken);
                _blockToken = 0;
            }
            _backdrop.RemoveFromHierarchy();
        }

        static VisualElement BuildBackdrop()
        {
            VisualElement backdrop = new VisualElement { name = UiNames.LoadingBackdrop };
            backdrop.AddToClassList("fdp-loading");
            VisualElement card = new VisualElement();
            card.AddToClassList("fdp-loading__card");
            VisualElement spinner = new VisualElement { name = UiNames.LoadingSpinner };
            spinner.AddToClassList("ds-spinner");
            spinner.AddToClassList("fdp-loading__spinner");
            Label message = new Label { name = UiNames.LoadingMessage };
            message.AddToClassList("fdp-loading__message");
            VisualElement progress = new VisualElement { name = UiNames.LoadingProgress };
            progress.AddToClassList("fdp-progress");
            VisualElement fill = new VisualElement { name = UiNames.LoadingProgressFill };
            fill.AddToClassList("fdp-progress__fill");
            progress.Add(fill);
            card.Add(spinner);
            card.Add(message);
            card.Add(progress);
            backdrop.Add(card);
            return backdrop;
        }

        // Only the backdrop takes pointer input (while a blocking token is alive). Its card is invisible most of the time and must never catch a tap meant for the screen below.
        static void IgnorePointerBelow(VisualElement element)
        {
            foreach (VisualElement child in element.Children())
            {
                child.pickingMode = PickingMode.Ignore;
                IgnorePointerBelow(child);
            }
        }

        void OnChanged()
        {
            Render();
        }

        void Render()
        {
            bool blocks = _tokens.BlocksInput;
            _backdrop.pickingMode = blocks ? PickingMode.Position : PickingMode.Ignore;
            if (blocks && _blockToken == 0)
            {
                _blockToken = _blocks.Acquire("loading");
            }
            else if (!blocks && _blockToken != 0)
            {
                _blocks.Release(_blockToken);
                _blockToken = 0;
            }

            bool visible = _tokens.IsVisible;
            _backdrop.EnableInClassList(UiNames.ClassVisible, visible);
            if (_message != null)
            {
                UiText text = _tokens.Message;
                _message.style.display = text.IsEmpty ? DisplayStyle.None : DisplayStyle.Flex;
                if (!text.IsEmpty)
                    _text.Bind(_message, text);
            }
            if (_spinner != null)
                _spinner.style.display = _tokens.ShowSpinner ? DisplayStyle.Flex : DisplayStyle.None;
            if (_progress != null)
            {
                float progress = _tokens.Progress;
                _progress.style.display = progress < 0f ? DisplayStyle.None : DisplayStyle.Flex;
                if (_fill != null && progress >= 0f)
                    _fill.style.width = Length.Percent(progress * 100f);
            }

            if (visible && _tokens.ShowSpinner)
                StartSpinner();
            else
                StopSpinner();

            ScheduleDue();
        }

        void StartSpinner()
        {
            if (_spinner == null || _spinnerLoop != null)
                return;
            _spinnerLoop = _spinner.schedule.Execute(() =>
            {
                _angle = (_angle + DegreesPerStep) % 360f;
                _spinner.style.rotate = new Rotate(new Angle(_angle, AngleUnit.Degree));
            }).Every(SpinnerIntervalMilliseconds);
        }

        void StopSpinner()
        {
            if (_spinnerLoop == null)
                return;
            _spinnerLoop.Pause();
            _spinnerLoop = null;
        }

        // Visibility changes by itself when the show delay or the minimum time ends; one timer wakes the tokens up exactly then.
        void ScheduleDue()
        {
            if (_dueTimer != null)
            {
                _dueTimer.Pause();
                _dueTimer = null;
            }
            if (_disposed)
                return;
            double due = _tokens.NextDueSeconds;
            if (due < 0)
                return;
            _dueTimer = _backdrop.schedule.Execute(() =>
            {
                _dueTimer = null;
                _tokens.Tick();
                ScheduleDue();
            }).StartingIn((long)(due * 1000.0) + 1);
        }
    }
}
