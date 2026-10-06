using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace FutureDigitalPlus.UIKit.Foundation
{
    /// <summary>
    /// Base class of every screen: the contract is <see cref="Build"/>, <see cref="Show"/>, <see cref="Hide"/>, <see cref="Dispose"/>.
    /// A screen is C# behaviour for one UXML file, or for a tree it builds in <see cref="OnBuild"/> when it has no UXML.
    /// <see cref="UxmlName"/> names the template (<c>Home</c> for <c>Home.uxml</c>); <see cref="Definition"/> gives the assets directly instead.
    /// <see cref="Build"/> loads the UXML, adds the style sheet of the same name, clones the tree under <see cref="Root"/> and calls <see cref="OnBuild"/>,
    /// where the subclass queries its elements once. The router decides when the screen is shown, hidden (kept alive underneath) or disposed.
    /// A missing UXML does not throw: <see cref="Root"/> then holds an error label, so a broken asset shows up on screen instead of crashing navigation.
    /// Exceptions of the overrides are logged, never thrown into navigation.
    /// </summary>
    public abstract class UiScreen : IDisposable
    {
        readonly List<Action> _cleanups = new List<Action>();

        /// <summary>
        /// File name of the UXML (and USS) without extension, looked up in <see cref="UiContext.Assets"/>. Return null or empty for a screen that builds its tree
        /// in code or uses <see cref="Definition"/>.
        /// </summary>
        public virtual string UxmlName
        {
            get { return null; }
        }

        /// <summary>The screen's assets as a <see cref="ScreenDefinition"/> (direct references). When non-null it is used instead of the asset source.</summary>
        public virtual ScreenDefinition Definition
        {
            get { return null; }
        }

        /// <summary>The element that holds the screen's tree. Set by <see cref="Build"/>.</summary>
        public VisualElement Root { get; private set; }

        /// <summary>The context this screen was built with.</summary>
        protected UiContext Ui { get; private set; }

        /// <summary>True after <see cref="Build"/>.</summary>
        public bool IsBuilt { get; private set; }

        /// <summary>True between <see cref="Show"/> and <see cref="Hide"/>.</summary>
        public bool IsShown { get; private set; }

        /// <summary>True after <see cref="Dispose"/>.</summary>
        public bool IsDisposed { get; private set; }

        /// <summary>How many times <see cref="Show"/> ran. 1 inside the first <see cref="OnShow"/>; larger after returning to the screen with a pop.</summary>
        protected int ShowCount { get; private set; }

        /// <summary>
        /// Whether empty areas of the screen take pointer input. True (default) for full-screen menus; a screen that sits on top of the game world (a HUD page)
        /// returns <see cref="PickingMode.Ignore"/> so touches outside its controls reach the world.
        /// </summary>
        public virtual PickingMode RootPicking
        {
            get { return PickingMode.Position; }
        }

        /// <summary>Extra style sheet names to add besides the one named like the UXML.</summary>
        protected virtual IEnumerable<string> ExtraStyleSheets
        {
            get { return null; }
        }

        /// <summary>Loads the UXML and builds the tree under a new <see cref="Root"/> that is added to <paramref name="outlet"/> (hidden until the router shows it).</summary>
        public void Build(UiContext ui, VisualElement outlet)
        {
            if (ui == null)
                throw new ArgumentNullException(nameof(ui));
            if (outlet == null)
                throw new ArgumentNullException(nameof(outlet));
            if (IsBuilt)
                throw new InvalidOperationException("The screen " + (UxmlName ?? GetType().Name) + " is already built.");
            Ui = ui;
            string label = string.IsNullOrEmpty(UxmlName) ? GetType().Name : UxmlName;
            Root = new VisualElement { name = "screen-" + label };
            Root.AddToClassList(UiNames.ClassScreen);
            Root.AddToClassList(UiNames.ClassScreen + "--" + label.ToLowerInvariant());
            Root.style.display = DisplayStyle.None;
            Root.pickingMode = RootPicking;

            ScreenDefinition definition = Definition;
            VisualTreeAsset tree = definition != null ? definition.Layout : (ui.Assets == null || string.IsNullOrEmpty(UxmlName) ? null : ui.Assets.LoadUxml(UxmlName));
            if (tree != null)
            {
                TemplateContainer content = tree.Instantiate();
                FoundationLayout.Fill(content);
                Root.Add(content);
            }
            else if (definition != null || !string.IsNullOrEmpty(UxmlName))
            {
                Debug.LogError("FutureDigitalPlus.UIKit.Foundation: no UXML for screen '" + label + "'. Give the screen a ScreenDefinition or put " + label + ".uxml where the asset source looks.");
                Label error = new Label("Missing UXML: " + label);
                error.AddToClassList("fdp-screen__error");
                Root.Add(error);
            }

            if (definition != null)
            {
                AddStyle(definition.Style);
                for (int i = 0; i < definition.ExtraStyles.Count; i++)
                    AddStyle(definition.ExtraStyles[i]);
            }
            else if (!string.IsNullOrEmpty(UxmlName))
            {
                AddStyle(ui.Assets == null ? null : ui.Assets.LoadStyle(UxmlName));
            }
            IEnumerable<string> extra = ExtraStyleSheets;
            if (extra != null && ui.Assets != null)
            {
                foreach (string sheet in extra)
                    AddStyle(string.IsNullOrEmpty(sheet) ? null : ui.Assets.LoadStyle(sheet));
            }

            outlet.Add(Root);
            IsBuilt = true;
            OnBuild();
        }

        /// <summary>The screen became the top screen. <paramref name="args"/> is what the pusher passed (null when returning with a pop and after a state restore).</summary>
        public void Show(object args)
        {
            if (IsDisposed || !IsBuilt)
                return;
            IsShown = true;
            ShowCount++;
            try
            {
                OnShow(args);
            }
            catch (Exception ex)
            {
                // A screen that fails in OnShow must not break navigation (the stack has already changed); the error is logged.
                Debug.LogException(ex);
            }
        }

        /// <summary>The screen is no longer on top (another screen was pushed over it, or it is leaving). Its tree stays alive until <see cref="Dispose"/>.</summary>
        public void Hide()
        {
            if (IsDisposed || !IsShown)
                return;
            IsShown = false;
            try
            {
                OnHide();
            }
            catch (Exception ex)
            {
                Debug.LogException(ex);
            }
        }

        /// <summary>Hides, runs <see cref="OnDispose"/>, unregisters every <see cref="Listen"/> callback and removes the tree. Safe to call twice; exceptions of the overrides are logged, not thrown.</summary>
        public void Dispose()
        {
            if (IsDisposed)
                return;
            Hide();
            IsDisposed = true;
            try
            {
                OnDispose();
            }
            catch (Exception ex)
            {
                Debug.LogException(ex);
            }
            // One failing cleanup must not leave the others registered or the tree in the panel.
            for (int i = _cleanups.Count - 1; i >= 0; i--)
            {
                try
                {
                    _cleanups[i]();
                }
                catch (Exception ex)
                {
                    Debug.LogException(ex);
                }
            }
            _cleanups.Clear();
            if (Root != null)
                Root.RemoveFromHierarchy();
        }

        /// <summary>Back press while this screen is on top and no dialog or overlay took it. Return true when handled (closed a sheet, cleared a selection); false lets the router pop.</summary>
        public virtual bool OnBack()
        {
            return false;
        }

        /// <summary>A string to persist for deep state restore (selected tab, search text); null for none. Plain text, no secrets.</summary>
        public virtual string SaveState()
        {
            return null;
        }

        /// <summary>Called after <see cref="OnBuild"/> and before <see cref="OnShow"/> when the stack is rebuilt after an app restart.</summary>
        public virtual void RestoreState(string state)
        {
        }

        /// <summary>The tree exists: query elements, register listeners.</summary>
        protected virtual void OnBuild()
        {
        }

        /// <summary>The screen is on top. <paramref name="args"/> is what the pusher passed.</summary>
        protected virtual void OnShow(object args)
        {
        }

        /// <summary>The screen is no longer on top.</summary>
        protected virtual void OnHide()
        {
        }

        /// <summary>The screen is going away; release what is not covered by <see cref="Listen"/>, <see cref="OnClick"/> and <see cref="OnDisposeDo"/>.</summary>
        protected virtual void OnDispose()
        {
        }

        /// <summary>The first element called <paramref name="name"/> under <see cref="Root"/>, or null.</summary>
        protected T Q<T>(string name) where T : VisualElement
        {
            return Root == null ? null : Root.Q<T>(name);
        }

        /// <summary>Registers an event callback that is removed automatically on <see cref="Dispose"/>.</summary>
        protected void Listen<TEvent>(VisualElement element, EventCallback<TEvent> callback) where TEvent : EventBase<TEvent>, new()
        {
            if (element == null)
                return;
            element.RegisterCallback(callback);
            _cleanups.Add(() => element.UnregisterCallback(callback));
        }

        /// <summary>Registers a button click that is removed automatically on <see cref="Dispose"/>.</summary>
        protected void OnClick(Button button, Action handler)
        {
            if (button == null)
                return;
            button.clicked += handler;
            _cleanups.Add(() => button.clicked -= handler);
        }

        /// <summary>Registers any cleanup to run on <see cref="Dispose"/> (an event unsubscription, a service handler).</summary>
        protected void OnDisposeDo(Action cleanup)
        {
            if (cleanup != null)
                _cleanups.Add(cleanup);
        }

        void AddStyle(StyleSheet sheet)
        {
            if (sheet != null && !Root.styleSheets.Contains(sheet))
                Root.styleSheets.Add(sheet);
        }
    }
}
