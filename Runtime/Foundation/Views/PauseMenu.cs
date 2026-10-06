using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace FutureDigitalPlus.UIKit.Foundation
{
    /// <summary>One entry of the <see cref="PauseMenu"/>: a caption, what it does, and how it looks.</summary>
    public sealed class PauseMenuItem
    {
        /// <summary>The button caption.</summary>
        public UiText Label { get; }

        /// <summary>What pressing the button does. It receives the context; the menu itself stays open unless the action navigates.</summary>
        public Action<UiContext> Action { get; }

        /// <summary>Danger styling for entries such as "Quit to menu".</summary>
        public bool Destructive { get; set; }

        /// <summary>Creates an entry.</summary>
        public PauseMenuItem(UiText label, Action<UiContext> action)
        {
            Label = label ?? UiText.Empty;
            Action = action;
        }
    }

    /// <summary>Settings of the <see cref="PauseMenu"/>.</summary>
    public sealed class PauseMenuOptions
    {
        /// <summary>The router id the menu is registered under. Default <c>Pause</c>.</summary>
        public string Id { get; set; } = PauseMenu.DefaultId;

        /// <summary>The heading. Default "Paused" (key <c>FDP-PAUSED</c>).</summary>
        public UiText Title { get; set; } = UiText.FromKey("FDP-PAUSED").WithFallback("Paused");

        /// <summary>The caption of the first button, which closes the menu. Default "Resume" (key <c>FDP-RESUME</c>).</summary>
        public UiText ResumeLabel { get; set; } = UiText.FromKey("FDP-RESUME").WithFallback("Resume");

        /// <summary>Further buttons below Resume (Settings, Restart, Quit to menu).</summary>
        public List<PauseMenuItem> Items { get; } = new List<PauseMenuItem>();

        /// <summary>When true (default) the time scale is 0 while the menu is on the stack, and the previous value comes back when it closes.</summary>
        public bool PauseTime { get; set; } = true;

        /// <summary>
        /// The hook that pauses and resumes the game: called with true when the menu opens and false when it closes. The default sets <c>Time.timeScale</c>
        /// to 0 and back to the value it had. Replace it for games that pause through their own clock or audio mixer. UI timers (transitions, toasts, loading)
        /// run in real time and keep working while the game is paused.
        /// </summary>
        public Action<bool> PauseHook { get; set; }

        /// <summary>Raised when the menu closes (Resume, back, or the screen being disposed).</summary>
        public Action Resumed { get; set; }
    }

    /// <summary>
    /// A ready pause menu: <see cref="Register"/> it on a shell, then <c>ui.Router.Push("Pause")</c> from your pause button or key. The menu is a screen built in
    /// code (title, Resume, your items) on the design system's buttons; back closes it, a screen pushed over it (Settings) keeps the game paused, and the
    /// time scale returns when the menu leaves the stack.
    /// <code>
    /// var options = new PauseMenuOptions();
    /// options.Items.Add(new PauseMenuItem(UiText.FromLiteral("Quit to menu"), ui =&gt; ui.Router.ResetTo("Home")) { Destructive = true });
    /// PauseMenu.Register(shell.Catalog, options);
    /// </code>
    /// </summary>
    public static class PauseMenu
    {
        /// <summary>The default router id.</summary>
        public const string DefaultId = "Pause";

        /// <summary>Registers the pause menu on <paramref name="catalog"/> under <see cref="PauseMenuOptions.Id"/>.</summary>
        public static void Register(ScreenCatalog catalog, PauseMenuOptions options = null)
        {
            if (catalog == null)
                throw new ArgumentNullException(nameof(catalog));
            PauseMenuOptions settings = options ?? new PauseMenuOptions();
            catalog.Register(settings.Id, () => new PauseScreen(settings));
        }

        sealed class PauseScreen : UiScreen
        {
            readonly PauseMenuOptions _options;
            float _previousTimeScale = 1f;
            bool _paused;

            public PauseScreen(PauseMenuOptions options)
            {
                _options = options;
            }

            protected override void OnBuild()
            {
                Root.AddToClassList("fdp-pause");
                VisualElement card = new VisualElement();
                card.AddToClassList("ds-modal");
                card.AddToClassList("fdp-pause__card");
                Label title = new Label();
                title.AddToClassList("ds-modal__title");
                title.AddToClassList("fdp-pause__title");
                Ui.SetText(title, _options.Title);
                card.Add(title);

                Button resume = AddButton(card, _options.ResumeLabel, false, true);
                OnClick(resume, Resume);
                for (int i = 0; i < _options.Items.Count; i++)
                {
                    PauseMenuItem item = _options.Items[i];
                    Button button = AddButton(card, item.Label, item.Destructive, false);
                    OnClick(button, () =>
                    {
                        if (item.Action != null)
                            item.Action(Ui);
                    });
                }
                Root.Add(card);
            }

            protected override void OnShow(object args)
            {
                if (!_options.PauseTime || _paused)
                    return;
                _paused = true;
                if (_options.PauseHook != null)
                {
                    _options.PauseHook(true);
                    return;
                }
                _previousTimeScale = Time.timeScale;
                Time.timeScale = 0f;
            }

            protected override void OnDispose()
            {
                if (_paused)
                {
                    _paused = false;
                    if (_options.PauseHook != null)
                        _options.PauseHook(false);
                    else
                        Time.timeScale = _previousTimeScale;
                }
                if (_options.Resumed != null)
                    _options.Resumed();
            }

            Button AddButton(VisualElement parent, UiText label, bool destructive, bool primary)
            {
                Button button = new Button();
                button.AddToClassList("ds-btn");
                button.AddToClassList(destructive ? "ds-btn--danger" : primary ? "ds-btn--primary" : "ds-btn--secondary");
                button.AddToClassList("fdp-pause__button");
                Ui.SetText(button, label);
                parent.Add(button);
                return button;
            }

            void Resume()
            {
                Ui.Router.Pop();
            }
        }
    }
}
