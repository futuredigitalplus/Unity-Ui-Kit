using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace FutureDigitalPlus.UIKit.Controls
{
    /// <summary>
    /// A select field that works with a gamepad, a keyboard and a pointer alike. Submit (or a click) opens a list
    /// popup (a <see cref="PopupMenu"/> with the current choice marked and focused); the left and right arrows change
    /// the choice without opening the list. Unlike the stock dropdown, the list never depends on d-pad item selection
    /// inside a popup, which does not work with the UI Toolkit navigation in Unity 6000.3. The value is the index of the
    /// chosen entry in <see cref="Choices"/>, and a change raises a <c>ChangeEvent&lt;int&gt;</c>.
    /// </summary>
    [UxmlElement]
    public partial class SelectField : VisualElement, INotifyValueChanged<int>
    {
        /// <summary>USS class of the element.</summary>
        public const string UssClassName = "fdp-select-field";

        private readonly List<string> _choices = new List<string>();
        private readonly Label _label = new Label { name = "label", pickingMode = PickingMode.Ignore };
        private readonly Label _arrow = new Label("v") { name = "arrow", pickingMode = PickingMode.Ignore };
        private int _index = -1;
        private string _placeholder = string.Empty;
        private PopupMenu _open;

        /// <summary>Creates an empty field.</summary>
        public SelectField()
        {
            AddToClassList(UssClassName);
            focusable = true;
            tabIndex = 0;
            _label.AddToClassList(UssClassName + "__label");
            _arrow.AddToClassList(UssClassName + "__arrow");
            Add(_label);
            Add(_arrow);
            RegisterCallback<ClickEvent>(_ => OpenList());
            RegisterCallback<NavigationSubmitEvent>(OnSubmit);
            RegisterCallback<NavigationMoveEvent>(OnMove);
            RefreshText();
        }

        /// <summary>Text shown while no entry is chosen.</summary>
        [UxmlAttribute]
        public string Placeholder
        {
            get => _placeholder;
            set
            {
                _placeholder = value ?? string.Empty;
                RefreshText();
            }
        }

        /// <summary>
        /// The entries as one string for UXML: <c>One,Two,Three</c> (an entry may not contain a comma). Setting it replaces all
        /// entries; code can assign <see cref="Choices"/> instead.
        /// </summary>
        [UxmlAttribute]
        public string Entries
        {
            get => string.Join(",", _choices);
            set
            {
                var parts = new List<string>();
                foreach (var part in (value ?? string.Empty).Split(','))
                {
                    var entry = part.Trim();
                    if (entry.Length > 0) parts.Add(entry);   // blank entries (", ,") are not choices
                }

                Choices = parts;
            }
        }

        /// <summary>The entries of the list. Assigning replaces them and keeps the chosen index when it is still valid.</summary>
        public IReadOnlyList<string> Choices
        {
            get => _choices;
            set
            {
                _choices.Clear();
                if (value != null)
                    foreach (var choice in value) _choices.Add(choice);
                if (_index >= _choices.Count) _index = _choices.Count - 1;
                RefreshText();
            }
        }

        /// <summary>The index of the chosen entry, or -1. Setting it raises a change event.</summary>
        [UxmlAttribute]
        // ReSharper disable once InconsistentNaming
        public int value
        {
            get => _index;
            set
            {
                value = Math.Max(-1, Math.Min(_choices.Count - 1, value));
                if (value == _index) return;
                var previous = _index;
                if (panel != null)
                {
                    using (var evt = ChangeEvent<int>.GetPooled(previous, value))
                    {
                        evt.target = this;
                        SetValueWithoutNotify(value);
                        SendEvent(evt);
                    }
                }
                else
                {
                    SetValueWithoutNotify(value);
                }
            }
        }

        /// <summary>The chosen entry's text, or null.</summary>
        public string SelectedText => _index >= 0 && _index < _choices.Count ? _choices[_index] : null;

        /// <summary>True while the list is open.</summary>
        public bool IsOpen => _open != null && _open.IsOpen;

        /// <summary>Chooses an entry without raising a change event.</summary>
        public void SetValueWithoutNotify(int newValue)
        {
            _index = Math.Max(-1, Math.Min(_choices.Count - 1, newValue));
            RefreshText();
        }

        /// <summary>Opens the list below the field and returns it. Does nothing (and returns the open list or null) when it is open already, empty, or the field is disabled or not on a panel.</summary>
        public PopupMenu OpenList()
        {
            if (IsOpen || _choices.Count == 0 || !enabledInHierarchy || panel == null) return _open;
            var items = new List<MenuEntry>(_choices.Count);
            for (var i = 0; i < _choices.Count; i++)
                items.Add(new MenuEntry(i.ToString(System.Globalization.CultureInfo.InvariantCulture), _choices[i]) { Checked = i == _index });
            var model = new MenuModel(items);
            if (_index >= 0) model.FocusItem(_index);
            _open = PopupMenu.ShowBelow(this, model, OnPicked, matchAnchorWidth: true);
            return _open;
        }

        private void OnPicked(MenuEntry item)
        {
            if (int.TryParse(item.Id, out var index)) value = index;
        }

        private void OnSubmit(NavigationSubmitEvent evt)
        {
            OpenList();
            evt.StopPropagation();
        }

        private void OnMove(NavigationMoveEvent evt)
        {
            int step;
            var rtl = PopupHost.IsRightToLeft(this);
            switch (evt.direction)
            {
                case NavigationMoveEvent.Direction.Left: step = rtl ? 1 : -1; break;
                case NavigationMoveEvent.Direction.Right: step = rtl ? -1 : 1; break;
                default: return;
            }

            if (_choices.Count == 0) return;
            evt.StopPropagation();
            panel?.focusController.IgnoreEvent(evt);
            var next = _index < 0 ? (step > 0 ? 0 : _choices.Count - 1) : Math.Max(0, Math.Min(_choices.Count - 1, _index + step));
            value = next;
        }

        private void RefreshText()
        {
            var text = SelectedText;
            _label.text = text ?? _placeholder;
            _label.EnableInClassList(UssClassName + "__label--placeholder", text == null);
        }
    }
}
