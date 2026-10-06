using System;
using System.Collections.Generic;
using FutureDigitalPlus.UIKit.Controls;
using FutureDigitalPlus.UIKit.Extras;
using FutureDigitalPlus.UIKit.Gameplay;
using UnityEngine;
using UnityEngine.UIElements;

namespace FutureDigitalPlus.UIKit.Samples.RpgInventory
{
    /// <summary>
    /// An RPG inventory: a backpack and a chest, items you drag between them, keyboard and gamepad pick-up, a context menu, tooltips and a tab bar with badges. Put it
    /// on an empty GameObject in an empty scene and press Play.
    /// <para>
    /// With a mouse or a finger: drag an item from one grid to the other (a stack of the same item merges, a different item swaps), right click or long press for the
    /// context menu, click a potion to drink it. With a keyboard or gamepad: arrow keys or the D-pad move focus, Enter or A picks the stack up and puts it down on
    /// another slot, Escape or B puts it back, Shift while picking up takes half the stack, the menu key or gamepad Y opens the context menu. The last row of the
    /// backpack is locked, and the chest refuses keys, to show the two ways a move can be refused.
    /// </para>
    /// </summary>
    [AddComponentMenu("Future Digital Plus UI Kit/RPG Inventory Sample")]
    public sealed class RpgInventorySample : MonoBehaviour
    {
        private const string ThemeResource = "FutureDigitalPlus/UIKit/Samples/RpgInventory/UnityDefaultRuntimeTheme";
        private const string DesignSystemResource = "FutureDigitalPlus/UIKit/DesignSystem/Styles/DesignSystem";
        private const string IconFolder = "FutureDigitalPlus/UIKit/DesignSystem/Icons/";
        private const int Columns = 5;
        private const int Rows = 4;

        private sealed class ItemKind
        {
            public string Name;
            public string Icon;
            public string Rarity;
            public int MaxStack;
        }

        private static readonly Dictionary<string, ItemKind> Kinds = new Dictionary<string, ItemKind>
        {
            { "sword", new ItemKind { Name = "Iron sword", Icon = "sword", Rarity = "common", MaxStack = 1 } },
            { "shield", new ItemKind { Name = "Oak shield", Icon = "shield", Rarity = "common", MaxStack = 1 } },
            { "bow", new ItemKind { Name = "Hunter's bow", Icon = "bow", Rarity = "rare", MaxStack = 1 } },
            { "axe", new ItemKind { Name = "Frost axe", Icon = "axe", Rarity = "epic", MaxStack = 1 } },
            { "crown", new ItemKind { Name = "Crown of ash", Icon = "crown", Rarity = "legendary", MaxStack = 1 } },
            { "wand", new ItemKind { Name = "Spark wand", Icon = "wand", Rarity = "rare", MaxStack = 1 } },
            { "potion", new ItemKind { Name = "Health potion", Icon = "potion", Rarity = "common", MaxStack = 20 } },
            { "bomb", new ItemKind { Name = "Bomb", Icon = "bomb", Rarity = null, MaxStack = 10 } },
            { "gem", new ItemKind { Name = "Gem", Icon = "gem", Rarity = "rare", MaxStack = 99 } },
            { "key", new ItemKind { Name = "Cellar key", Icon = "key", Rarity = "epic", MaxStack = 1 } },
        };

        private readonly Dictionary<string, VectorImage> _icons = new Dictionary<string, VectorImage>();
        private PanelSettings _settings;
        private InventoryModel _backpack;
        private InventoryModel _chest;
        private SlotGrid _backpackGrid;
        private SlotGrid _chestGrid;
        private Label _status;

        private void Start()
        {
            _settings = ScriptableObject.CreateInstance<PanelSettings>();
            _settings.name = "RpgInventoryPanelSettings";
            _settings.themeStyleSheet = Resources.Load<ThemeStyleSheet>(ThemeResource);
            _settings.scaleMode = PanelScaleMode.ScaleWithScreenSize;
            _settings.referenceResolution = new Vector2Int(1280, 720);
            _settings.screenMatchMode = PanelScreenMatchMode.MatchWidthOrHeight;
            _settings.match = 0.5f;

            // Inactive while configured, so the UIDocument builds its tree once with everything set.
            var go = new GameObject("RPG inventory (UIDocument)");
            go.transform.SetParent(transform, false);
            go.SetActive(false);
            var document = go.AddComponent<UIDocument>();
            document.panelSettings = _settings;
            go.SetActive(true);

            var root = document.rootVisualElement;
            var designSystem = Resources.Load<StyleSheet>(DesignSystemResource);
            var holder = root.parent ?? root;
            if (designSystem != null && !holder.styleSheets.Contains(designSystem))
                holder.styleSheets.Add(designSystem);
            ControlsStyles.Install(root);
            GameplayStyles.Install(root);
            root.AddToClassList("ds-root");

            CreateInventories();
            Build(root);
        }

        private void OnDestroy()
        {
            if (_settings != null)
                Destroy(_settings);
        }

        // ---- data ------------------------------------------------------------------------------------------------

        private void CreateInventories()
        {
            // The model knows nothing about UI: stack limits and placement rules are functions the game passes in.
            _backpack = new InventoryModel(Columns * Rows, id => Kinds[id].MaxStack);
            _chest = new InventoryModel(Columns * Rows, id => Kinds[id].MaxStack, (slot, stack) => stack.ItemId != "key");

            // The last row of the backpack is locked until the player buys a bigger bag.
            for (var i = Columns * (Rows - 1); i < _backpack.Capacity; i++)
                _backpack.SetLocked(i, true);

            _backpack.Add(new ItemStack("sword", 1));
            _backpack.Add(new ItemStack("shield", 1));
            _backpack.Add(new ItemStack("potion", 5));
            _backpack.Add(new ItemStack("bomb", 3));
            _backpack.Add(new ItemStack("key", 1));
            _backpack.Add(new ItemStack("gem", 12));
            _chest.Add(new ItemStack("bow", 1));
            _chest.Add(new ItemStack("axe", 1));
            _chest.Add(new ItemStack("wand", 1));
            _chest.Add(new ItemStack("crown", 1));
            _chest.Add(new ItemStack("potion", 30)); // two stacks: the limit is 20
            _chest.Add(new ItemStack("gem", 40));
        }

        private ItemVisual VisualOf(string itemId)
        {
            var kind = Kinds[itemId];
            if (!_icons.TryGetValue(kind.Icon, out var icon))
                _icons[kind.Icon] = icon = Resources.Load<VectorImage>(IconFolder + kind.Icon);
            return new ItemVisual(Background.FromVectorImage(icon), kind.Rarity);
        }

        // ---- UI --------------------------------------------------------------------------------------------------

        private void Build(VisualElement root)
        {
            var column = new VisualElement();
            column.style.alignSelf = Align.Center;
            column.style.marginTop = 24;
            root.Add(column);

            // A tab bar with badges: only the first page has content in this sample, the others show the empty state a game starts with.
            var tabs = new TabBar();
            tabs.AddTab("items", "Items");
            tabs.AddTab("gear", "Gear");
            tabs.AddTab("quests", "Quests");
            tabs.SetBadge("gear", new BadgeValue(0, true));
            tabs.SetBadge("quests", new BadgeValue(2, false));
            column.Add(tabs);

            var itemsPage = new VisualElement();
            itemsPage.style.flexDirection = FlexDirection.Row;
            itemsPage.style.marginTop = 16;
            _backpackGrid = Grid("Backpack", _backpack, itemsPage);
            _chestGrid = Grid("Chest", _chest, itemsPage);
            // Linked grids share one pick-up state: an item picked up in one can be put down in the other.
            SlotGrid.Link(_backpackGrid, _chestGrid);
            column.Add(itemsPage);

            var emptyPage = EmptyStates.Create("Nothing here yet", "This tab has no content in the sample.");
            emptyPage.style.display = DisplayStyle.None;
            column.Add(emptyPage);

            tabs.RegisterValueChangedCallback(e =>
            {
                itemsPage.style.display = e.newValue == "items" ? DisplayStyle.Flex : DisplayStyle.None;
                emptyPage.style.display = e.newValue == "items" ? DisplayStyle.None : DisplayStyle.Flex;
                if (e.newValue == "quests")
                    tabs.SetBadge("quests", default);
            });
            tabs.value = "items";

            _status = new Label("Drag an item, or pick one up with Enter.");
            _status.AddToClassList("ds-body-2");
            _status.style.marginTop = 16;
            column.Add(_status);

            var hint = new Label("Enter / A: pick up and put down. Esc / B: put back. Shift: half the stack. Menu key / Y / right click: context menu.");
            hint.AddToClassList("ds-caption");
            column.Add(hint);

            // The tooltip goes last so it paints over everything. Each slot gets a manipulator; the text is the slot's own tooltip property.
            var tooltip = new Tooltip();
            root.Add(tooltip);
            foreach (var grid in new[] { _backpackGrid, _chestGrid })
                foreach (var slot in grid.Slots)
                    slot.AddManipulator(new TooltipManipulator(tooltip));
            UpdateTooltips(_backpackGrid);
            UpdateTooltips(_chestGrid);
        }

        private SlotGrid Grid(string title, InventoryModel model, VisualElement parent)
        {
            var group = new VisualElement();
            group.style.marginRight = 24;
            var heading = new Label(title);
            heading.AddToClassList("ds-h3");
            group.Add(heading);

            var grid = new SlotGrid { Columns = Columns, SlotSize = 56f, Spacing = 4f };
            grid.style.width = Columns * 60f;
            grid.Bind(model, VisualOf);
            group.Add(grid);
            parent.Add(group);

            grid.Moved += OnMoved;
            grid.MoveRejected += OnRejected;
            grid.SlotActivated += slot => UseItem(grid, slot);
            grid.SlotContext += (slot, position) => OpenMenu(grid, slot, position);
            model.SlotChanged += _ => UpdateTooltips(grid);
            return grid;
        }

        private void UpdateTooltips(SlotGrid grid)
        {
            for (var i = 0; i < grid.Slots.Count; i++)
            {
                var stack = grid.Model[i];
                grid.Slots[i].tooltip = stack.IsEmpty
                    ? string.Empty
                    : "T:" + Kinds[stack.ItemId].Name + (stack.Count > 1 ? " x" + stack.Count : string.Empty);
            }
        }

        // ---- what the grids report -------------------------------------------------------------------------------

        private void OnMoved(SlotMove move)
        {
            var stack = move.Target[move.TargetSlot];
            _status.text = move.Result.Reason + ": " + Kinds[stack.ItemId].Name + " to " + (move.Target == _chest ? "the chest" : "the backpack") + ".";
        }

        private void OnRejected(SlotMove move)
        {
            var held = move.Source[move.SourceSlot];
            _status.text = "Cannot put " + (held.IsEmpty ? "that" : Kinds[held.ItemId].Name) + " there: " + move.Result.Reason + ".";
        }

        private void UseItem(SlotGrid grid, ItemSlot slot)
        {
            var stack = grid.Model[slot.Index];
            if (stack.IsEmpty || stack.ItemId != "potion")
                return;
            grid.Model.Set(slot.Index, stack.WithCount(stack.Count - 1));
            _status.text = "You drink a potion and feel better.";
        }

        private void OpenMenu(SlotGrid grid, ItemSlot slot, Vector2 panelPosition)
        {
            var stack = grid.Model[slot.Index];
            if (stack.IsEmpty)
                return;
            var items = new List<MenuEntry>
            {
                new MenuEntry("use", "Use") { Enabled = stack.ItemId == "potion" },
                new MenuEntry("split", "Split") { Enabled = stack.Count > 1 },
                MenuEntry.Separator(),
                new MenuEntry("drop", "Drop"),
            };
            PopupMenu.Show(slot, items, panelPosition, item =>
            {
                switch (item.Id)
                {
                    case "use":
                        UseItem(grid, slot);
                        break;
                    case "split":
                        SplitStack(grid.Model, slot.Index);
                        break;
                    case "drop":
                        grid.Model.Set(slot.Index, ItemStack.Empty);
                        _status.text = "Dropped " + Kinds[stack.ItemId].Name + ".";
                        break;
                }
            });
        }

        private void SplitStack(InventoryModel model, int from)
        {
            for (var to = 0; to < model.Capacity; to++)
            {
                if (!model[to].IsEmpty || model.IsLocked(to))
                    continue;
                var result = model.MoveHalf(from, to);
                _status.text = "Split: " + result.Reason + ".";
                return;
            }
            _status.text = "No free slot to split into.";
        }
    }
}
