using System.Globalization;
using UnityEngine;
using UnityEngine.UIElements;

namespace FutureDigitalPlus.UIKit.Gameplay
{
    /// <summary>
    /// One inventory slot: a square with an icon, a rarity coloured border, a stack count, a cooldown shade, a lock mark,
    /// a selected state and an "equipped" chip. It is a plain view: a <see cref="SlotGrid"/> fills it from an
    /// <see cref="InventoryModel"/>, and a game can also use it alone (a hotbar, an equipment slot) through
    /// <see cref="SetItem"/>. Slots take keyboard and gamepad focus.
    /// </summary>
    [UxmlElement]
    public partial class ItemSlot : VisualElement
    {
        /// <summary>USS class of the element.</summary>
        public const string UssClassName = "fdp-item-slot";

        private readonly VisualElement _icon = new VisualElement { name = "icon", pickingMode = PickingMode.Ignore };
        private readonly VisualElement _cooldown = new VisualElement { name = "cooldown", pickingMode = PickingMode.Ignore };
        private readonly VisualElement _lock = new VisualElement { name = "lock", pickingMode = PickingMode.Ignore };
        private readonly Label _count = new Label { name = "count", pickingMode = PickingMode.Ignore };
        private readonly Label _equipped = new Label("E") { name = "equipped", pickingMode = PickingMode.Ignore };
        private string _rarity;
        private int _countValue;
        private float _cooldownFraction;
        private bool _hasItem;

        /// <summary>Creates an empty slot.</summary>
        public ItemSlot()
        {
            AddToClassList(UssClassName);
            focusable = true;
            tabIndex = 0;
            _icon.AddToClassList(UssClassName + "__icon");
            _cooldown.AddToClassList(UssClassName + "__cooldown");
            _lock.AddToClassList(UssClassName + "__lock");
            _count.AddToClassList(UssClassName + "__count");
            _equipped.AddToClassList(UssClassName + "__equipped");
            _equipped.AddToClassList("ds-badge");
            _cooldown.style.display = DisplayStyle.None;
            _lock.style.display = DisplayStyle.None;
            _count.style.display = DisplayStyle.None;
            _equipped.style.display = DisplayStyle.None;
            Add(_icon);
            Add(_cooldown);
            Add(_lock);
            Add(_count);
            Add(_equipped);
            EnableInClassList(UssClassName + "--empty", true);
        }

        /// <summary>The grid this slot belongs to, or null for a slot used alone.</summary>
        public SlotGrid Grid { get; internal set; }

        /// <summary>The slot number in the grid's inventory (-1 for a slot used alone).</summary>
        public int Index { get; internal set; } = -1;

        /// <summary>True when an item is shown.</summary>
        public bool HasItem => _hasItem;

        /// <summary>The number of items shown; the count label appears from 2 up.</summary>
        [UxmlAttribute]
        public int Count
        {
            get => _countValue;
            set
            {
                value = Mathf.Max(0, value);
                if (value == _countValue) return;
                _countValue = value;
                _count.style.display = value > 1 ? DisplayStyle.Flex : DisplayStyle.None;
                if (value > 1) _count.text = value.ToString(CultureInfo.InvariantCulture);
            }
        }

        /// <summary>The rarity name (<c>common</c>, <c>rare</c>, <c>epic</c>, <c>legendary</c>) that colours the border; null or empty for none.</summary>
        [UxmlAttribute]
        public string Rarity
        {
            get => _rarity;
            set
            {
                if (string.IsNullOrEmpty(value)) value = null;
                if (string.Equals(value, _rarity, System.StringComparison.Ordinal)) return;
                if (_rarity != null) RemoveFromClassList(UssClassName + "--" + _rarity);
                _rarity = value?.ToLowerInvariant();
                if (_rarity != null) AddToClassList(UssClassName + "--" + _rarity);
            }
        }

        /// <summary>How much of the cooldown shade covers the icon, from 0 (none) to 1 (all); the shade shrinks from the top as the value falls.</summary>
        [UxmlAttribute]
        public float Cooldown
        {
            get => _cooldownFraction;
            set
            {
                value = Mathf.Clamp01(value);
                if (Mathf.Approximately(value, _cooldownFraction)) return;
                _cooldownFraction = value;
                _cooldown.style.display = value > 0f ? DisplayStyle.Flex : DisplayStyle.None;
                _cooldown.style.height = Length.Percent(value * 100f);
            }
        }

        /// <summary>True when the slot is locked (a lock mark covers it and the inventory refuses moves).</summary>
        [UxmlAttribute]
        public bool IsLocked
        {
            get => ClassListContains(UssClassName + "--locked");
            set
            {
                EnableInClassList(UssClassName + "--locked", value);
                _lock.style.display = value ? DisplayStyle.Flex : DisplayStyle.None;
            }
        }

        /// <summary>True when the slot is shown as selected.</summary>
        [UxmlAttribute]
        public bool IsSelected
        {
            get => ClassListContains(UssClassName + "--selected");
            set => EnableInClassList(UssClassName + "--selected", value);
        }

        /// <summary>True when the chip that marks an equipped item is shown.</summary>
        [UxmlAttribute]
        public bool IsEquipped
        {
            get => _equipped.style.display == DisplayStyle.Flex;
            set
            {
                _equipped.style.display = value ? DisplayStyle.Flex : DisplayStyle.None;
                EnableInClassList(UssClassName + "--equipped", value);
            }
        }

        /// <summary>The text of the equipped chip (default <c>E</c>); give it a localised word if there is room.</summary>
        public string EquippedText
        {
            get => _equipped.text;
            set => _equipped.text = value ?? string.Empty;
        }

        /// <summary>The icon element, for a game that wants to tint or animate it.</summary>
        public VisualElement IconElement => _icon;

        /// <summary>Shows an item.</summary>
        public void SetItem(ItemVisual visual, int count)
        {
            _hasItem = true;
            EnableInClassList(UssClassName + "--empty", false);
            _icon.style.backgroundImage = new StyleBackground(visual.Icon);
            Rarity = visual.Rarity;
            Count = count;
        }

        /// <summary>Shows no item (cooldown, locked and selected states stay as they are).</summary>
        public void SetEmpty()
        {
            _hasItem = false;
            EnableInClassList(UssClassName + "--empty", true);
            _icon.style.backgroundImage = StyleKeyword.None;
            Rarity = null;
            Count = 0;
        }
    }
}
