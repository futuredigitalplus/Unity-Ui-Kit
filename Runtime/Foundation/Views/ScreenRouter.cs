using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace FutureDigitalPlus.UIKit.Foundation
{
    /// <summary>
    /// Executes <see cref="ScreenStack"/> operations: builds screens from their factories, shows and hides them, plays the USS transitions, blocks input while a
    /// transition runs, and captures or restores the whole stack. The rules (what push, replace, pop and reset do, the depth cap) live in the pure
    /// <see cref="ScreenStack"/>. A factory or <c>Build</c> that throws leaves the stack unchanged and logs the error. A push of the id that is already on
    /// top does nothing, and input is blocked during a transition, so a double tap cannot stack two copies of a page.
    /// Transitions are USS classes (<c>fdp-screen--enter</c>, <c>--enter-back</c>, <c>--exit</c>, <c>--behind</c>) removed on the next scheduler tick so the
    /// <c>transition-*</c> rules in Foundation.uss run; <see cref="TransitionMilliseconds"/> must match their duration. No transition runs when
    /// <see cref="TransitionsEnabled"/> is false or when <see cref="UiMotion"/> scales motion to zero (reduce motion).
    /// </summary>
    public sealed class ScreenRouter : IScreenRouter, IDisposable
    {
        /// <summary>Length of a screen transition; equals the <c>transition-duration</c> of <c>.fdp-screen</c> in Foundation.uss (220 ms).</summary>
        public const long TransitionMilliseconds = 220;

        readonly UiContext _ui;
        readonly VisualElement _outlet;
        readonly ScreenStack _stack;
        readonly Dictionary<ScreenEntry, UiScreen> _screens = new Dictionary<ScreenEntry, UiScreen>();
        readonly Dictionary<string, Func<UiScreen>> _factories = new Dictionary<string, Func<UiScreen>>(StringComparer.Ordinal);
        readonly List<Action> _pending = new List<Action>();
        int _transitionBlock;
        int _transitionGeneration;
        bool _disposed;

        /// <summary>False plays no transitions (tests, games that never animate pages). Reduce motion also switches transitions off.</summary>
        public bool TransitionsEnabled { get; set; } = true;

        /// <inheritdoc />
        public event Action<string, string> Changed;

        /// <summary>Creates a router that builds screens under <paramref name="outlet"/>. <paramref name="maxDepth"/> caps the stack (default 12).</summary>
        public ScreenRouter(UiContext ui, VisualElement outlet, int maxDepth = ScreenStack.DefaultMaxDepth)
        {
            if (ui == null)
                throw new ArgumentNullException(nameof(ui));
            if (outlet == null)
                throw new ArgumentNullException(nameof(outlet));
            _ui = ui;
            _outlet = outlet;
            _stack = new ScreenStack(maxDepth);
        }

        /// <inheritdoc />
        public string Current
        {
            get { return _stack.CurrentId; }
        }

        /// <inheritdoc />
        public bool CanPop
        {
            get { return _stack.CanPop; }
        }

        /// <summary>Number of screens on the stack.</summary>
        public int Depth
        {
            get { return _stack.Depth; }
        }

        /// <summary>The screen object on top, or null.</summary>
        public UiScreen CurrentScreen
        {
            get { return Lookup(_stack.Current); }
        }

        /// <summary>The screen object at <paramref name="index"/> of the stack (0 is the root), or null.</summary>
        public UiScreen ScreenAt(int index)
        {
            return index < 0 || index >= _stack.Depth ? null : Lookup(_stack[index]);
        }

        /// <summary>Registers or replaces the factory of <paramref name="id"/> for this router only.</summary>
        public void Register(string id, Func<UiScreen> factory)
        {
            if (string.IsNullOrEmpty(id))
                throw new ArgumentException("A screen id must not be empty.", nameof(id));
            if (factory == null)
                throw new ArgumentNullException(nameof(factory));
            _factories[id] = factory;
        }

        /// <summary>Takes every factory of <paramref name="catalog"/> that this router does not have yet.</summary>
        public void ApplyCatalog(ScreenCatalog catalog)
        {
            if (catalog == null)
                return;
            foreach (string id in catalog.Ids)
            {
                Func<UiScreen> factory;
                if (!_factories.ContainsKey(id) && catalog.TryGet(id, out factory))
                    _factories[id] = factory;
            }
        }

        /// <summary>True when a factory exists for <paramref name="id"/>.</summary>
        public bool IsRegistered(string id)
        {
            return id != null && _factories.ContainsKey(id);
        }

        /// <inheritdoc />
        public void Push(string screenId, object args = null)
        {
            if (_disposed || !CanOpen(screenId) || string.Equals(screenId, _stack.CurrentId, StringComparison.Ordinal))
                return;
            FlushPending();
            UiScreen next = BuildScreen(screenId);
            if (next == null)
                return;
            Execute(_stack.Push(screenId, args), next, args);
        }

        /// <inheritdoc />
        public void Replace(string screenId, object args = null)
        {
            if (_disposed || !CanOpen(screenId))
                return;
            FlushPending();
            UiScreen next = BuildScreen(screenId);
            if (next == null)
                return;
            Execute(_stack.Replace(screenId, args), next, args);
        }

        /// <inheritdoc />
        public bool Pop()
        {
            if (_disposed || !_stack.CanPop)
                return false;
            FlushPending();
            Execute(_stack.Pop(), null, null);
            return true;
        }

        /// <inheritdoc />
        public bool PopTo(string screenId)
        {
            if (_disposed || _stack.IndexOf(screenId) < 0 || string.Equals(screenId, _stack.CurrentId, StringComparison.Ordinal))
                return false;
            FlushPending();
            Execute(_stack.PopTo(screenId), null, null);
            return true;
        }

        /// <inheritdoc />
        public void ResetTo(string screenId, object args = null)
        {
            if (_disposed || !CanOpen(screenId))
                return;
            FlushPending();
            UiScreen next = BuildScreen(screenId);
            if (next == null)
                return;
            Execute(_stack.ResetTo(screenId, args), next, args);
        }

        /// <summary>The top screen's own back handling. True when it handled the press.</summary>
        public bool HandleScreenBack()
        {
            UiScreen top = CurrentScreen;
            if (top == null)
                return false;
            try
            {
                return top.OnBack();
            }
            catch (Exception ex)
            {
                Debug.LogException(ex);
                return false;
            }
        }

        /// <inheritdoc />
        public bool Back()
        {
            return HandleScreenBack() || Pop();
        }

        /// <summary>Serialises the stack with each screen's <see cref="UiScreen.SaveState"/> for deep state restore.</summary>
        public string CaptureState()
        {
            for (int i = 0; i < _stack.Depth; i++)
            {
                ScreenEntry entry = _stack[i];
                UiScreen screen = Lookup(entry);
                try
                {
                    entry.State = screen == null ? null : screen.SaveState();
                }
                catch (Exception ex)
                {
                    Debug.LogWarning("FutureDigitalPlus.UIKit.Foundation: SaveState of '" + entry.Id + "' threw: " + ex.Message);
                    entry.State = null;
                }
            }
            return _stack.Capture();
        }

        /// <summary>
        /// Rebuilds the stack from <see cref="CaptureState"/> output without transitions. Ids that are not registered any more are dropped; when the root is gone or
        /// anything fails to build, nothing changes and false is returned so the caller starts the normal start screen.
        /// </summary>
        public bool TryRestoreState(string captured)
        {
            if (_disposed)
                return false;
            List<ScreenEntry> entries;
            if (!ScreenStack.TryParse(captured, IsRegistered, out entries))
                return false;

            List<UiScreen> built = new List<UiScreen>();
            for (int i = 0; i < entries.Count; i++)
            {
                UiScreen screen = BuildScreen(entries[i].Id);
                if (screen == null)
                {
                    for (int j = 0; j < built.Count; j++)
                        built[j].Dispose();
                    return false;
                }
                built.Add(screen);
            }

            FlushPending();
            DisposeAll();
            _stack.Load(entries);
            for (int i = 0; i < entries.Count; i++)
            {
                _screens[entries[i]] = built[i];
                try
                {
                    built[i].RestoreState(entries[i].State);
                }
                catch (Exception ex)
                {
                    Debug.LogWarning("FutureDigitalPlus.UIKit.Foundation: RestoreState of '" + entries[i].Id + "' threw: " + ex.Message);
                }
            }

            ScreenEntry top = _stack.Current;
            UiScreen topScreen = built[built.Count - 1];
            topScreen.Root.style.display = DisplayStyle.Flex;
            topScreen.Show(null);
            RaiseChanged(null, top.Id);
            return true;
        }

        /// <summary>Disposes every screen and releases the transition input block. The router cannot be used afterwards: navigation calls do nothing.</summary>
        public void Dispose()
        {
            _disposed = true;
            FlushPending();
            DisposeAll();
            _transitionGeneration++;
            if (_transitionBlock != 0)
            {
                _ui.Input.Blocks.Release(_transitionBlock);
                _transitionBlock = 0;
            }
        }

        bool CanOpen(string id)
        {
            if (string.IsNullOrEmpty(id))
            {
                Debug.LogError("FutureDigitalPlus.UIKit.Foundation: screen id is empty.");
                return false;
            }
            if (!IsRegistered(id))
            {
                Debug.LogError("FutureDigitalPlus.UIKit.Foundation: screen '" + id + "' is not registered. Register it in the shell's ScreenCatalog (or ScreenCatalog.Default) before Boot.");
                return false;
            }
            return true;
        }

        UiScreen BuildScreen(string id)
        {
            UiScreen screen = null;
            try
            {
                screen = _factories[id]();
                if (screen == null)
                {
                    Debug.LogError("FutureDigitalPlus.UIKit.Foundation: the factory of screen '" + id + "' returned null.");
                    return null;
                }
                screen.Build(_ui, _outlet);
                return screen;
            }
            catch (Exception ex)
            {
                Debug.LogException(ex);
                // Build adds the root to the outlet before OnBuild runs; a screen that failed half way must not leave its tree behind.
                if (screen != null)
                    screen.Dispose();
                return null;
            }
        }

        UiScreen Lookup(ScreenEntry entry)
        {
            UiScreen screen;
            return entry != null && _screens.TryGetValue(entry, out screen) ? screen : null;
        }

        bool MotionOn
        {
            get { return TransitionsEnabled && UiMotion.Scale((int)TransitionMilliseconds) > 0; }
        }

        void Execute(ScreenChange change, UiScreen next, object args)
        {
            if (change.IsNone)
            {
                if (next != null)
                    next.Dispose();
                return;
            }

            UiScreen from = Lookup(change.From);
            UiScreen to;
            if (change.ToIsExisting)
            {
                to = Lookup(change.To);
            }
            else
            {
                to = next;
                _screens[change.To] = to;
            }

            bool animate = MotionOn && from != null && to != null;
            if (from != null)
                from.Hide();

            for (int i = 0; i < change.Disposed.Count; i++)
            {
                ScreenEntry gone = change.Disposed[i];
                UiScreen goneScreen = Lookup(gone);
                _screens.Remove(gone);
                if (goneScreen == null)
                    continue;
                if (ReferenceEquals(goneScreen, from) && animate)
                    RetireAnimated(goneScreen, UiNames.ClassScreenExit, true);
                else
                    goneScreen.Dispose();
            }

            if (from != null && change.FromStaysAlive)
            {
                if (animate)
                    RetireAnimated(from, UiNames.ClassScreenBehind, false);
                else
                    from.Root.style.display = DisplayStyle.None;
            }

            if (to != null)
            {
                string enter = change.Kind == ScreenChangeKind.Pop ? UiNames.ClassScreenEnterBack : UiNames.ClassScreenEnter;
                PresentAnimated(to, animate ? enter : null, args);
            }

            if (animate)
                BlockInputForTransition();
            RaiseChanged(change.From == null ? null : change.From.Id, change.To == null ? null : change.To.Id);
        }

        void PresentAnimated(UiScreen screen, string enterClass, object args)
        {
            VisualElement root = screen.Root;
            if (enterClass != null)
                root.AddToClassList(enterClass);
            root.RemoveFromClassList(UiNames.ClassScreenBehind);
            root.style.display = DisplayStyle.Flex;
            screen.Show(args);
            if (enterClass != null)
                root.schedule.Execute(() => root.RemoveFromClassList(enterClass)).StartingIn(16);
        }

        // Plays the leaving class; then either disposes the screen or just hides it (kept alive underneath).
        void RetireAnimated(UiScreen screen, string leaveClass, bool dispose)
        {
            VisualElement root = screen.Root;
            root.AddToClassList(leaveClass);
            root.pickingMode = PickingMode.Ignore;
            Action finish = () =>
            {
                if (dispose)
                {
                    screen.Dispose();
                    return;
                }
                root.style.display = DisplayStyle.None;
                root.RemoveFromClassList(leaveClass);
                root.pickingMode = screen.RootPicking;
            };
            _pending.Add(finish);
            root.schedule.Execute(() =>
            {
                if (_pending.Remove(finish))
                    finish();
            }).StartingIn(TransitionMilliseconds + 16);
        }

        void BlockInputForTransition()
        {
            if (_transitionBlock == 0)
                _transitionBlock = _ui.Input.Blocks.Acquire("screen transition");
            // A second navigation during the first transition extends the block: only the release scheduled last may let go.
            int generation = ++_transitionGeneration;
            _outlet.schedule.Execute(() =>
            {
                if (generation == _transitionGeneration && _transitionBlock != 0)
                {
                    _ui.Input.Blocks.Release(_transitionBlock);
                    _transitionBlock = 0;
                }
            }).StartingIn(TransitionMilliseconds + 32);
        }

        // A new navigation request during a transition completes the pending clean-up first.
        void FlushPending()
        {
            if (_pending.Count == 0)
                return;
            Action[] snapshot = _pending.ToArray();
            _pending.Clear();
            for (int i = 0; i < snapshot.Length; i++)
                snapshot[i]();
        }

        void DisposeAll()
        {
            UiScreen[] all = new UiScreen[_screens.Count];
            _screens.Values.CopyTo(all, 0);
            _screens.Clear();
            for (int i = all.Length - 1; i >= 0; i--)
                all[i].Dispose();
            _stack.Load(null);
        }

        void RaiseChanged(string from, string to)
        {
            Action<string, string> handler = Changed;
            if (handler != null)
                handler(from, to);
        }
    }
}
