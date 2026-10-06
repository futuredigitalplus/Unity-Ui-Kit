using System.Collections.Generic;
using UnityEngine.UIElements;

namespace FutureDigitalPlus.UIKit.Hud
{
    /// <summary>
    /// A short-lived message feed (kill feed, loot log, system notices): a <see cref="TimedFeedModel"/> shown as a
    /// column of labels. Identical messages merge into one line with a counter (<c>+10 gold x3</c>), old lines fade
    /// and disappear, and the element scheduler runs only while lines are visible. Style a line through the
    /// <c>style</c> argument of <see cref="Push"/>, which adds the class <c>fdp-timed-feed__item--{style}</c>.
    /// </summary>
    [UxmlElement]
    public partial class TimedFeed : VisualElement
    {
        /// <summary>USS class of the element.</summary>
        public const string UssClassName = "fdp-timed-feed";

        private readonly TimedFeedModel _model = new TimedFeedModel();
        private readonly Dictionary<FeedEntry, Label> _labels = new Dictionary<FeedEntry, Label>();
        private readonly TickDriver _driver;

        /// <summary>Creates an empty feed.</summary>
        public TimedFeed()
        {
            AddToClassList(UssClassName);
            pickingMode = PickingMode.Ignore;
            _driver = new TickDriver(this, Step, 100);
            _model.EntryAdded += OnAdded;
            _model.EntryUpdated += OnUpdated;
            _model.EntryRemoved += OnRemoved;
        }

        /// <summary>The feed rules (maximum lines, time to live, merging, order). Change them before pushing.</summary>
        public TimedFeedModel Model => _model;

        /// <summary>Most lines shown at once (default 5).</summary>
        [UxmlAttribute]
        public int MaxEntries
        {
            get => _model.MaxEntries;
            set => _model.MaxEntries = value;
        }

        /// <summary>Seconds a line stays (default 4).</summary>
        [UxmlAttribute]
        public float TimeToLive
        {
            get => _model.TimeToLive;
            set => _model.TimeToLive = value;
        }

        /// <summary>True to show the newest line first (default: newest at the bottom).</summary>
        [UxmlAttribute]
        public bool NewestOnTop
        {
            get => _model.NewestOnTop;
            set => _model.NewestOnTop = value;
        }

        /// <summary>Adds a line, or merges it into a live line with the same <paramref name="key"/> (a counter is appended).</summary>
        public FeedEntry Push(string text, string key = null, string style = null)
        {
            return _model.Add(text, key, style);
        }

        /// <summary>Removes every line.</summary>
        public void ClearEntries()
        {
            _model.Clear();
        }

        internal bool Step(float deltaSeconds)
        {
            _model.Tick(deltaSeconds);
            return _model.Count > 0;
        }

        private void OnAdded(FeedEntry entry)
        {
            var label = new Label(entry.Text) { pickingMode = PickingMode.Ignore };
            label.AddToClassList("fdp-timed-feed__item");
            if (!string.IsNullOrEmpty(entry.Style)) label.AddToClassList("fdp-timed-feed__item--" + entry.Style);
            _labels[entry] = label;
            if (_model.NewestOnTop) Insert(0, label);
            else Add(label);
            _driver.Request();
        }

        private void OnUpdated(FeedEntry entry)
        {
            if (!_labels.TryGetValue(entry, out var label)) return;
            label.text = entry.Count > 1 ? entry.Text + " x" + entry.Count : entry.Text;
            label.EnableInClassList("fdp-timed-feed__item--fading", entry.IsFading);
        }

        private void OnRemoved(FeedEntry entry, FeedRemoveReason reason)
        {
            if (!_labels.TryGetValue(entry, out var label)) return;
            _labels.Remove(entry);
            label.RemoveFromHierarchy();
        }
    }
}
