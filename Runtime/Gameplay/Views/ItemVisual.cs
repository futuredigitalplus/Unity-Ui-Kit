using UnityEngine.UIElements;

namespace FutureDigitalPlus.UIKit.Gameplay
{
    /// <summary>How an item kind looks in a slot. A <see cref="SlotGrid"/> asks the game for one per item id.</summary>
    public readonly struct ItemVisual
    {
        /// <summary>Creates a look.</summary>
        /// <param name="icon">The picture (a sprite, texture or vector image converts to <see cref="Background"/>).</param>
        /// <param name="rarity">One of <c>common</c>, <c>rare</c>, <c>epic</c>, <c>legendary</c> to colour the border with the design system rarity colour; null for none.</param>
        public ItemVisual(Background icon, string rarity = null)
        {
            Icon = icon;
            Rarity = rarity;
        }

        /// <summary>The picture.</summary>
        public Background Icon { get; }

        /// <summary>The rarity name that picks the border colour, or null.</summary>
        public string Rarity { get; }
    }
}
