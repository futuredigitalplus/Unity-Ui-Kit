using System;
using UnityEngine;
using UnityEngine.UIElements;

namespace FutureDigitalPlus.UIKit.Foundation
{
    /// <summary>
    /// The UI plumbing of one panel in plain C#: ordered layers (screens, hud, overlays, dialogs, toasts, loading), the services that live in them (screen router,
    /// dialogs, toasts, loading overlay), the input gate, the back dispatcher, the profile classes and the safe area. Create one per UIDocument; nothing is a
    /// singleton, so two shells in one scene or one test do not see each other.
    /// <code>
    /// var shell = new UiShell(uiDocument.rootVisualElement, new UiShellOptions { Assets = new ResourcesAssetSource("Screens") });
    /// shell.Catalog.Register("Home", () =&gt; new HomeScreen());
    /// shell.Boot("Home");
    /// // every frame: shell.Tick();   when the app loses focus: shell.NotifyApplicationPause(true);
    /// </code>
    /// A MonoBehaviour host (the facade's UIShellHost) only forwards <see cref="Tick"/> and the application events. The shell itself keeps working without
    /// a host: safe area and back through navigation-cancel events run from the UI Toolkit scheduler and event system.
    /// </summary>
    public sealed class UiShell : IDisposable
    {
        const long FallbackPollMilliseconds = 250;

        readonly UiShellOptions _options;
        readonly DialogService _dialogs;
        readonly ToastService _toasts;
        readonly LoadingOverlay _loading;
        readonly IBackKeySource _backKeySource;
        readonly IVisualElementScheduledItem _poll;
        int _lastBackFrame = -1;

        /// <summary>The shell root element (class <c>fdp-shell</c>), a child of the host element given to the constructor.</summary>
        public VisualElement Root { get; }

        /// <summary>Everything screens use.</summary>
        public UiContext Context { get; }

        /// <summary>The shell's own screen catalog.</summary>
        public ScreenCatalog Catalog { get; }

        /// <summary>The screen router (the concrete type behind <c>Context.Router</c>).</summary>
        public ScreenRouter Router { get; }

        /// <summary>True after <see cref="Boot"/>.</summary>
        public bool IsBooted { get; private set; }

        /// <summary>True after <see cref="Dispose"/>.</summary>
        public bool IsDisposed { get; private set; }

        /// <summary>Raised once the context exists and before the first screen shows: register more screens here.</summary>
        public event Action<UiContext> Booted;

        /// <summary>Builds the layers and services under <paramref name="host"/>. <paramref name="options"/> null uses the defaults.</summary>
        public UiShell(VisualElement host, UiShellOptions options = null)
        {
            if (host == null)
                throw new ArgumentNullException(nameof(host));
            FoundationRuntime.EnsureLogSink();
            _options = options ?? new UiShellOptions();
            _backKeySource = _options.BackKeySource;
            Catalog = _options.Catalog ?? new ScreenCatalog();

            Root = new VisualElement { name = UiNames.Root, pickingMode = PickingMode.Ignore };
            Root.AddToClassList(UiNames.ClassShell);
            if (_options.ApplyDesignSystemRootClasses)
            {
                Root.AddToClassList("ds-root");
                Root.AddToClassList("ds-root--hud");
            }
            FoundationLayout.Fill(Root);
            host.Add(Root);
            if (_options.InstallStyles)
                FoundationStyles.Install(Root);

            IUiAssetSource assets = _options.Assets ?? new CompositeAssetSource();
            Context = new UiContext(Root, assets, _options.Text);
            Context.State = _options.StateStore;

            SafeAreaProvider safeArea = _options.SafeArea;
            if (safeArea == null)
            {
                safeArea = new SafeAreaProvider { Mode = _options.SafeAreaMode, MinimumMarginPercent = _options.MinimumMarginPercent };
            }
            safeArea.Attach(Root);
            Context.SafeArea = safeArea;

            VisualElement screens = AddLayer(UiLayer.Screens, UiNames.LayerScreens);
            AddLayer(UiLayer.Hud, UiNames.LayerHud);
            AddLayer(UiLayer.Overlays, UiNames.LayerOverlays);
            VisualElement dialogs = AddLayer(UiLayer.Dialogs, UiNames.LayerDialogs);
            VisualElement toasts = AddLayer(UiLayer.Toasts, UiNames.LayerToasts);
            VisualElement loading = AddLayer(UiLayer.Loading, UiNames.LayerLoading);

            SafeAreaPanel toastSafe = new SafeAreaPanel { name = "toasts-safe" };
            toastSafe.AddToClassList("fdp-toasts");
            if (_options.ToastPlacement == ToastPlacement.Top)
                toastSafe.AddToClassList(UiNames.ClassToastsTop);
            FoundationLayout.Fill(toastSafe);
            toasts.Add(toastSafe);

            Context.Profile = new UiProfileWatcher(Root, _options.InputModeSource);
            _dialogs = new DialogService(dialogs, assets, Context.Input.Blocks, Context.Text);
            _toasts = new ToastService(toastSafe, assets, Context.Text);
            _loading = new LoadingOverlay(loading, assets, Context.Input.Blocks, Context.Text);
            Router = new ScreenRouter(Context, screens, _options.MaxDepth) { TransitionsEnabled = _options.TransitionsEnabled };
            Context.Dialogs = _dialogs;
            Context.Toasts = _toasts;
            Context.Loading = _loading;
            Context.Router = Router;

            Context.Back.Register(BackPriority.Loading, _loading.HandleBack);
            Context.Back.Register(BackPriority.Dialog, _dialogs.HandleBack);
            Context.Back.Register(BackPriority.Screen, Router.HandleScreenBack);
            Context.Back.Register(BackPriority.Router, Router.Pop);

            if (_options.HandleNavigationCancel)
                Root.RegisterCallback<NavigationCancelEvent>(OnNavigationCancel);
            Root.RegisterCallback<GeometryChangedEvent>(OnGeometryChanged);
            Router.Changed += OnRouterChanged;
            _poll = Root.schedule.Execute(() => Context.SafeArea.Poll(Root)).Every(FallbackPollMilliseconds);
        }

        /// <summary>
        /// Takes the registered screens, raises <see cref="Booted"/>, then restores the saved stack or resets to <paramref name="initialScreen"/>.
        /// A second call does nothing.
        /// </summary>
        public void Boot(string initialScreen)
        {
            if (IsDisposed)
                throw new ObjectDisposedException(nameof(UiShell));
            if (IsBooted)
                return;
            IsBooted = true;
            Context.SafeArea.Invalidate();
            Context.SafeArea.Poll(Root);
            ApplyCatalogs();
            Action<UiContext> booted = Booted;
            if (booted != null)
                booted(Context);
            ApplyCatalogs();

            if (_options.RestoreNavigation && _options.StateStore != null)
            {
                string saved = _options.StateStore.Load(_options.NavigationStateKey);
                if (!string.IsNullOrEmpty(saved) && Router.TryRestoreState(saved))
                    return;
            }
            if (Router.IsRegistered(initialScreen))
                Router.ResetTo(initialScreen);
            else
                Debug.LogWarning("FutureDigitalPlus.UIKit.Foundation: the start screen '" + initialScreen + "' is not registered; the UI starts empty.");
        }

        /// <summary>
        /// One back press: the loading overlay, then the open dialog, then the top screen's <see cref="UiScreen.OnBack"/>, then pop. Returns true when something
        /// consumed it; false at the root screen, where the game decides (a quit confirmation). Not de-duplicated: use <see cref="HandleBackPress"/> for key sources.
        /// </summary>
        public bool RequestBack()
        {
            return !IsDisposed && Context.Back.Dispatch();
        }

        /// <summary>
        /// Like <see cref="RequestBack"/> but at most one press per frame: several sources (a key poll and a navigation-cancel event) can report the same physical
        /// press in one frame, and only the first counts. Returns true when the press was consumed or was a duplicate.
        /// </summary>
        public bool HandleBackPress()
        {
            if (IsDisposed)
                return false;
            int frame = Time.frameCount;
            if (frame == _lastBackFrame)
                return true;
            _lastBackFrame = frame;
            return Context.Back.Dispatch();
        }

        /// <summary>Per-frame work for a host: polls the safe area and the back key source. Cheap; safe to call every frame.</summary>
        public void Tick()
        {
            if (IsDisposed)
                return;
            Context.SafeArea.Poll(Root);
            if (_backKeySource != null && _backKeySource.WasPressedThisFrame())
                HandleBackPress();
        }

        /// <summary>Forward <c>OnApplicationFocus</c>: on regaining focus the safe area is recomputed and dialogs that asked for it close.</summary>
        public void NotifyApplicationFocus(bool focus)
        {
            if (IsDisposed || !focus)
                return;
            Context.SafeArea.Invalidate();
            _dialogs.OnAppResumed();
        }

        /// <summary>Forward <c>OnApplicationPause</c>: pausing saves the navigation stack, resuming recomputes the safe area.</summary>
        public void NotifyApplicationPause(bool paused)
        {
            if (IsDisposed)
                return;
            if (paused)
                SaveNavigationState();
            else
                Context.SafeArea.Invalidate();
        }

        /// <summary>Writes the navigation stack to the state store (done automatically on every change and on pause).</summary>
        public void SaveNavigationState()
        {
            if (!_options.RestoreNavigation || _options.StateStore == null || !IsBooted)
                return;
            try
            {
                _options.StateStore.Save(_options.NavigationStateKey, Router.CaptureState());
            }
            catch (Exception ex)
            {
                Debug.LogWarning("FutureDigitalPlus.UIKit.Foundation: saving the navigation state failed: " + ex.Message);
            }
        }

        /// <summary>Closes dialogs, clears toasts, disposes the screens and removes the shell from the panel. Safe to call twice.</summary>
        public void Dispose()
        {
            if (IsDisposed)
                return;
            SaveNavigationState();
            IsDisposed = true;
            _poll.Pause();
            Router.Changed -= OnRouterChanged;
            Root.UnregisterCallback<NavigationCancelEvent>(OnNavigationCancel);
            Root.UnregisterCallback<GeometryChangedEvent>(OnGeometryChanged);
            _dialogs.Dispose();
            _toasts.Clear();
            _loading.Dispose();
            Router.Dispose();
            Context.Profile.Dispose();
            Context.SafeArea.Detach(Root);
            Context.Input.Blocks.Clear();
            Root.RemoveFromHierarchy();
        }

        void ApplyCatalogs()
        {
            Router.ApplyCatalog(Catalog);
            if (_options.UseDefaultCatalog)
                Router.ApplyCatalog(ScreenCatalog.Default);
        }

        VisualElement AddLayer(UiLayer layer, string name)
        {
            VisualElement element = new VisualElement { name = name, pickingMode = PickingMode.Ignore };
            element.AddToClassList(UiNames.ClassLayer);
            element.AddToClassList(UiNames.ClassLayer + "--" + layer.ToString().ToLowerInvariant());
            FoundationLayout.Fill(element);
            Root.Add(element);
            Context.SetLayer(layer, element);
            return element;
        }

        void OnGeometryChanged(GeometryChangedEvent evt)
        {
            Context.SafeArea.Poll(Root);
        }

        void OnNavigationCancel(NavigationCancelEvent evt)
        {
            HandleBackPress();
            evt.StopPropagation();
        }

        void OnRouterChanged(string from, string to)
        {
            SaveNavigationState();
        }
    }
}
