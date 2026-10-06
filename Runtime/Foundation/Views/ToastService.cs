using UnityEngine.UIElements;

namespace FutureDigitalPlus.UIKit.Foundation
{
    /// <summary>
    /// UI Toolkit view of the <see cref="ToastQueue"/>: one toast at a time (up to three wait, identical repeats are coalesced), built in the toasts layer on the
    /// design system's <c>ds-toast</c> classes (<c>ds-toast--success</c>, <c>--info</c>, <c>--warning</c>, <c>--danger</c>). A UXML template named <c>Toast</c> in the
    /// asset source replaces it. The toast never takes pointer input or focus. Timing uses the element scheduler (real time), so toasts still expire while
    /// the game is paused with <c>Time.timeScale = 0</c>.
    /// </summary>
    public sealed class ToastService : IToastService
    {
        const long FadeMilliseconds = 220;

        readonly VisualElement _host;
        readonly IUiAssetSource _assets;
        readonly UiTextSource _text;
        readonly ToastQueue _queue = new ToastQueue();
        VisualElement _view;
        IVisualElementScheduledItem _timer;
        IVisualElementScheduledItem _fadeTimer;

        /// <summary>Creates the service. <paramref name="host"/> is the element toasts are added to (the shell gives it the safe-area panel of the toasts layer).</summary>
        public ToastService(VisualElement host, IUiAssetSource assets = null, UiTextSource text = null)
        {
            if (host == null)
                throw new System.ArgumentNullException(nameof(host));
            _host = host;
            _assets = assets;
            _text = text ?? new UiTextSource();
            _queue.Showing += Present;
            _queue.Refreshed += Refresh;
        }

        /// <inheritdoc />
        public void Show(UiText text, ToastKind kind = ToastKind.Info, float seconds = ToastItem.DefaultSeconds)
        {
            _queue.Show(new ToastItem(text, kind, seconds));
        }

        /// <inheritdoc />
        public void ShowRaw(string text, ToastKind kind = ToastKind.Info, float seconds = ToastItem.DefaultSeconds)
        {
            _queue.Show(new ToastItem(UiText.FromLiteral(text), kind, seconds));
        }

        /// <inheritdoc />
        public void Clear()
        {
            if (_timer != null)
                _timer.Pause();
            _timer = null;
            if (_fadeTimer != null)
                _fadeTimer.Pause();
            _fadeTimer = null;
            _queue.Clear();
            if (_view != null)
            {
                _view.RemoveFromHierarchy();
                _view = null;
            }
        }

        /// <summary>The class a toast of <paramref name="kind"/> carries (<c>ds-toast--info</c> and so on).</summary>
        public static string KindClass(ToastKind kind)
        {
            switch (kind)
            {
                case ToastKind.Success:
                    return UiNames.ClassToastKindPrefix + "success";
                case ToastKind.Warning:
                    return UiNames.ClassToastKindPrefix + "warning";
                case ToastKind.Danger:
                    return UiNames.ClassToastKindPrefix + "danger";
                default:
                    return UiNames.ClassToastKindPrefix + "info";
            }
        }

        void Present(ToastItem item)
        {
            VisualElement view = null;
            VisualTreeAsset tree = _assets == null ? null : _assets.LoadUxml(UiNames.TemplateToast);
            if (tree != null)
            {
                TemplateContainer container = tree.Instantiate();
                view = container.Q<VisualElement>(UiNames.ToastRoot);
                if (view != null)
                {
                    view.RemoveFromHierarchy();
                    _host.Add(view);
                }
            }
            if (view == null)
            {
                view = BuildToast();
                _host.Add(view);
            }

            view.pickingMode = PickingMode.Ignore;
            view.focusable = false;
            _view = view;
            Label label = view.Q<Label>(UiNames.ToastLabel);
            if (label != null)
                _text.Bind(label, item.Text);
            SetKind(view, item.Kind);
            KeepInsideHost(view);

            if (UiMotion.Scale(1) > 0)
                view.schedule.Execute(() => view.AddToClassList(UiNames.ClassVisible)).StartingIn(16);
            else
                view.AddToClassList(UiNames.ClassVisible);
            StartTimer(item);
        }

        static VisualElement BuildToast()
        {
            VisualElement toast = new VisualElement { name = UiNames.ToastRoot };
            toast.AddToClassList("ds-toast");
            toast.AddToClassList(UiNames.ClassToast);
            Label label = new Label { name = UiNames.ToastLabel, pickingMode = PickingMode.Ignore };
            label.AddToClassList("ds-toast__message");
            toast.Add(label);
            return toast;
        }

        /// <summary>
        /// The toast is as wide as its text up to the width in USS (<c>max-width</c>). A host narrower than that plus the toast margins (a very small phone, a split
        /// view) would push it out of the safe area, so the width is narrowed to the host inline. The inline value only ever applies when the toast is wider than
        /// the room, which means the room is smaller than the limit.
        /// </summary>
        void KeepInsideHost(VisualElement view)
        {
            view.RegisterCallback<GeometryChangedEvent>(evt =>
            {
                float room = _host.contentRect.width - view.resolvedStyle.marginLeft - view.resolvedStyle.marginRight;
                if (float.IsNaN(room) || room <= 0f)
                    return;
                if (view.layout.width > room + 0.5f)
                    view.style.maxWidth = room;
            });
        }

        void Refresh(ToastItem item)
        {
            StartTimer(item);
        }

        void StartTimer(ToastItem item)
        {
            if (_timer != null)
                _timer.Pause();
            VisualElement view = _view;
            // A repeat that arrives while the toast is already fading out brings it back instead of letting it vanish.
            if (_fadeTimer != null)
            {
                _fadeTimer.Pause();
                _fadeTimer = null;
                view.AddToClassList(UiNames.ClassVisible);
            }
            _timer = view.schedule.Execute(() => Dismiss(view)).StartingIn((long)(item.Seconds * 1000f));
        }

        void Dismiss(VisualElement view)
        {
            view.RemoveFromClassList(UiNames.ClassVisible);
            _fadeTimer = view.schedule.Execute(() =>
            {
                _fadeTimer = null;
                view.RemoveFromHierarchy();
                if (ReferenceEquals(_view, view))
                {
                    _view = null;
                    _timer = null;
                    _queue.Hidden();
                }
            });
            _fadeTimer.StartingIn(FadeMilliseconds);
        }

        void SetKind(VisualElement view, ToastKind kind)
        {
            view.RemoveFromClassList(KindClass(ToastKind.Info));
            view.RemoveFromClassList(KindClass(ToastKind.Success));
            view.RemoveFromClassList(KindClass(ToastKind.Warning));
            view.RemoveFromClassList(KindClass(ToastKind.Danger));
            view.AddToClassList(KindClass(kind));
        }
    }
}
