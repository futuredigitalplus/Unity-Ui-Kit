using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace FutureDigitalPlus.UIKit.Controls
{
    /// <summary>
    /// What one reward looks like in a <see cref="RewardPopup"/>: plain presentation data. The module knows nothing
    /// about where a reward came from (a chest, a level, a store), so there is no price, product or analytics field.
    /// </summary>
    public readonly struct RewardItem
    {
        /// <summary>Creates a reward entry.</summary>
        /// <param name="id">A stable id the game can use to recognise the entry (not shown).</param>
        /// <param name="label">The name shown under the icon.</param>
        /// <param name="amount">The amount text shown on the entry, for example <c>x25</c>; null shows none.</param>
        /// <param name="icon">The picture; null shows a plain tile.</param>
        /// <param name="rarity">One of <c>common</c>, <c>rare</c>, <c>epic</c>, <c>legendary</c> to tint the border with the design system rarity colour; null for none.</param>
        public RewardItem(string id, string label, string amount = null, Sprite icon = null, string rarity = null)
        {
            Id = id;
            Label = label;
            Amount = amount;
            Icon = icon;
            Rarity = rarity;
        }

        /// <summary>The id the game gave the entry.</summary>
        public string Id { get; }

        /// <summary>The name shown under the icon.</summary>
        public string Label { get; }

        /// <summary>The amount text, or null.</summary>
        public string Amount { get; }

        /// <summary>The picture, or null.</summary>
        public Sprite Icon { get; }

        /// <summary>The rarity name that picks the border colour, or null.</summary>
        public string Rarity { get; }
    }

    /// <summary>
    /// A modal "you got these" popup. The rewards appear one after another (driven by a <see cref="RewardSequence"/>);
    /// the one button first skips the reveal and then claims, a tap on the card does the same, and Cancel or back
    /// behaves like the button. It takes view models only. Under reduced motion (the class <c>fdp-reduce-motion</c> on an
    /// ancestor) every reward shows at once. Several sets can be presented in a row through <see cref="RewardPopupQueue"/>.
    /// </summary>
    public sealed class RewardPopup
    {
        private const int TickMilliseconds = 30;

        private readonly VisualElement _anchor;
        private readonly Button _button;
        private readonly string _skipText;
        private readonly string _claimText;
        private readonly List<VisualElement> _tiles = new List<VisualElement>();
        private readonly RewardItem[] _rewards;
        private VisualElement _layer;
        private PopupFocus _focus;
        private IVisualElementScheduledItem _ticker;
        private double _lastTick;
        private int _lastCancelFrame = -1;
        private bool _closed;

        private RewardPopup(VisualElement anchor, string title, IReadOnlyList<RewardItem> rewards, string claimText, string skipText,
            float initialDelaySeconds, float intervalSeconds)
        {
            _anchor = anchor;
            _claimText = claimText;
            _skipText = skipText;
            _rewards = new RewardItem[rewards.Count];
            for (var i = 0; i < rewards.Count; i++) _rewards[i] = rewards[i];

            var reduced = MotionGate.IsReduced(anchor);
            Sequence = reduced ? new RewardSequence(_rewards.Length, 0f, 0f) : new RewardSequence(_rewards.Length, initialDelaySeconds, intervalSeconds);
            Sequence.Revealed += OnRevealed;

            var host = PopupHost.TopLevel(anchor);
            _layer = PopupHost.CreateLayer(host, "reward-popup");
            _layer.AddToClassList("fdp-reward-layer");
            _layer.RegisterCallback<NavigationCancelEvent>(OnCancel);
            _layer.RegisterCallback<KeyDownEvent>(OnKeyDown);

            Card = new VisualElement { name = "card", pickingMode = PickingMode.Position };
            Card.AddToClassList("ds-modal");
            Card.AddToClassList("fdp-reward");
            Card.RegisterCallback<ClickEvent>(OnCardClick);
            _layer.Add(Card);

            var titleLabel = new Label(title ?? string.Empty) { pickingMode = PickingMode.Ignore };
            titleLabel.AddToClassList("ds-modal__title");
            titleLabel.AddToClassList("fdp-reward__title");
            Card.Add(titleLabel);

            var row = new VisualElement { name = "rewards", pickingMode = PickingMode.Ignore };
            row.AddToClassList("fdp-reward__row");
            Card.Add(row);
            foreach (var reward in _rewards)
            {
                var tile = BuildTile(reward);
                _tiles.Add(tile);
                row.Add(tile);
            }

            _button = new Button { name = "action", focusable = true };
            _button.AddToClassList("ds-btn");
            _button.AddToClassList("ds-btn--primary");
            _button.AddToClassList("fdp-reward__action");
            _button.clicked += Advance;
            Card.Add(_button);
            RefreshButton();
            if (reduced) Sequence.Tick(0f); // reduced motion: everything shows at once

            _focus = new PopupFocus(_layer);
            _focus.Begin();
            _focus.Focus(_button);

            _lastTick = PopupHost.Now;
            _ticker = _layer.schedule.Execute(Tick).Every(TickMilliseconds);
        }

        /// <summary>The reveal timing and state of this popup.</summary>
        public RewardSequence Sequence { get; }

        /// <summary>The card element (a <c>ds-modal</c>), for styling or tests.</summary>
        public VisualElement Card { get; }

        /// <summary>True while the popup is on the panel.</summary>
        public bool IsOpen => !_closed;

        /// <summary>The rewards shown, in order.</summary>
        public IReadOnlyList<RewardItem> Rewards => _rewards;

        /// <summary>Raised when the player claimed (the popup closes right after).</summary>
        public event Action Claimed;

        /// <summary>Raised after the popup left the panel, however it was closed.</summary>
        public event Action Closed;

        /// <summary>
        /// Shows a popup on the top element of the panel that contains <paramref name="anchor"/>. Returns null when the
        /// anchor is not on a panel or <paramref name="rewards"/> is null.
        /// </summary>
        /// <param name="anchor">Any element of the panel.</param>
        /// <param name="title">The heading.</param>
        /// <param name="rewards">The entries, shown in order.</param>
        /// <param name="claimText">Text of the button once everything is revealed.</param>
        /// <param name="skipText">Text of the button while rewards are still appearing.</param>
        /// <param name="initialDelaySeconds">Wait before the first reward appears.</param>
        /// <param name="intervalSeconds">Wait between rewards.</param>
        public static RewardPopup Show(VisualElement anchor, string title, IReadOnlyList<RewardItem> rewards, string claimText = "Claim",
            string skipText = "Skip", float initialDelaySeconds = 0.25f, float intervalSeconds = 0.35f)
        {
            if (rewards == null || anchor?.panel == null) return null;
            return new RewardPopup(anchor, title, rewards, claimText, skipText, initialDelaySeconds, intervalSeconds);
        }

        /// <summary>Reveals every reward that is still hidden.</summary>
        public void Skip()
        {
            if (_closed) return;
            Sequence.Skip();
            RefreshButton();
        }

        /// <summary>Claims and closes. Returns false when the rewards are not all revealed yet (nothing happens then).</summary>
        public bool Claim()
        {
            if (_closed || !Sequence.IsComplete) return false;
            Claimed?.Invoke();
            Close();
            return true;
        }

        /// <summary>What the button does: skips while rewards are hidden, claims once all are shown.</summary>
        public void Advance()
        {
            if (_closed) return;
            if (!Sequence.IsComplete) Skip();
            else Claim();
        }

        /// <summary>Closes without claiming (for a game that cancels the popup, for example on scene change).</summary>
        public void Close()
        {
            if (_closed) return;
            _closed = true;
            _ticker?.Pause();
            _ticker = null;
            _focus.End();
            _layer.RemoveFromHierarchy();
            _layer = null;
            Closed?.Invoke();
        }

        /// <summary>Handles a back request: like the button (skip first, claim next). Returns true when the popup consumed it.</summary>
        public bool HandleBack()
        {
            if (_closed) return false;
            Advance();
            return true;
        }

        private static VisualElement BuildTile(RewardItem reward)
        {
            var tile = new VisualElement { pickingMode = PickingMode.Ignore };
            tile.AddToClassList("fdp-reward__item");
            tile.AddToClassList("fdp-reward__item--hidden");
            if (!string.IsNullOrEmpty(reward.Rarity)) tile.AddToClassList("fdp-reward__item--" + reward.Rarity.ToLowerInvariant());

            var icon = new VisualElement { name = "icon", pickingMode = PickingMode.Ignore };
            icon.AddToClassList("fdp-reward__icon");
            if (reward.Icon != null) icon.style.backgroundImage = new StyleBackground(reward.Icon);
            tile.Add(icon);

            if (!string.IsNullOrEmpty(reward.Amount))
            {
                var amount = new Label(reward.Amount) { name = "amount", pickingMode = PickingMode.Ignore };
                amount.AddToClassList("fdp-reward__amount");
                tile.Add(amount);
            }

            if (!string.IsNullOrEmpty(reward.Label))
            {
                var label = new Label(reward.Label) { name = "label", pickingMode = PickingMode.Ignore };
                label.AddToClassList("fdp-reward__label");
                tile.Add(label);
            }

            return tile;
        }

        private void OnRevealed(int index)
        {
            var tile = _tiles[index];
            tile.RemoveFromClassList("fdp-reward__item--hidden");
            tile.AddToClassList("fdp-reward__item--shown");
            if (Sequence.IsComplete) RefreshButton();
        }

        private void Tick()
        {
            if (_closed) return;
            var now = PopupHost.Now;
            var delta = (float)(now - _lastTick);
            _lastTick = now;
            if (Sequence.Tick(delta)) RefreshButton();
            if (Sequence.IsComplete) _ticker?.Pause();
        }

        private void RefreshButton()
        {
            _button.text = Sequence.IsComplete ? _claimText : _skipText;
            _button.EnableInClassList("fdp-reward__action--skip", !Sequence.IsComplete);
        }

        private void OnCardClick(ClickEvent evt)
        {
            if (evt.target == _button) return; // the button has its own handler
            if (!Sequence.IsComplete) Skip();
        }

        private void OnCancel(NavigationCancelEvent evt)
        {
            var focusController = _layer?.panel?.focusController;
            CancelOncePerFrame();
            evt.StopPropagation();
            focusController?.IgnoreEvent(evt);
        }

        private void OnKeyDown(KeyDownEvent evt)
        {
            if (evt.keyCode != KeyCode.Escape) return;
            CancelOncePerFrame();
            evt.StopPropagation();
        }

        private void CancelOncePerFrame()
        {
            if (_lastCancelFrame == Time.frameCount) return;
            _lastCancelFrame = Time.frameCount;
            HandleBack();
        }
    }

    /// <summary>
    /// Presents reward sets one at a time: a set that arrives while a popup is open waits and is shown when the player
    /// has claimed the one before. Bind one queue per panel and feed it from the game.
    /// </summary>
    public sealed class RewardPopupQueue
    {
        private sealed class Entry
        {
            public VisualElement Anchor;
            public string Title;
            public IReadOnlyList<RewardItem> Rewards;
            public Action OnClaimed;
        }

        private readonly PresentationQueue<Entry> _queue = new PresentationQueue<Entry>();
        private readonly string _claimText;
        private readonly string _skipText;
        private RewardPopup _current;

        /// <summary>Creates a queue whose popups use the given button texts.</summary>
        public RewardPopupQueue(string claimText = "Claim", string skipText = "Skip")
        {
            _claimText = claimText;
            _skipText = skipText;
            _queue.CurrentChanged += Present;
        }

        /// <summary>The popup on screen now, or null.</summary>
        public RewardPopup Current => _current;

        /// <summary>Number of sets waiting behind the one on screen.</summary>
        public int WaitingCount => _queue.WaitingCount;

        /// <summary>Raised when the last set was closed and nothing is left to show.</summary>
        public event Action Drained
        {
            add => _queue.Drained += value;
            remove => _queue.Drained -= value;
        }

        /// <summary>Adds a reward set; it is shown at once when nothing is on screen.</summary>
        /// <param name="anchor">Any element of the panel to show it on.</param>
        /// <param name="title">The heading.</param>
        /// <param name="rewards">The entries.</param>
        /// <param name="onClaimed">Called when the player claims this set.</param>
        public void Enqueue(VisualElement anchor, string title, IReadOnlyList<RewardItem> rewards, Action onClaimed = null)
        {
            if (anchor == null) throw new ArgumentNullException(nameof(anchor));
            if (rewards == null) throw new ArgumentNullException(nameof(rewards));
            _queue.Enqueue(new Entry { Anchor = anchor, Title = title, Rewards = rewards, OnClaimed = onClaimed });
        }

        /// <summary>Closes the popup on screen and forgets everything waiting.</summary>
        public void Clear()
        {
            _queue.Clear();
            var popup = _current;
            _current = null;
            popup?.Close();
        }

        private void Present(Entry entry)
        {
            var popup = RewardPopup.Show(entry.Anchor, entry.Title, entry.Rewards, _claimText, _skipText);
            if (popup == null)
            {
                // The anchor left the panel while the set waited: drop it and go on with the next.
                _queue.Complete();
                return;
            }

            _current = popup;
            popup.Claimed += entry.OnClaimed;
            popup.Closed += () =>
            {
                if (!ReferenceEquals(_current, popup)) return;
                _current = null;
                _queue.Complete();
            };
        }
    }
}
