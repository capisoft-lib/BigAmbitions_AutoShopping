using UnityEngine;

namespace AutoShopping
{
    internal sealed class CatalogProduct
    {
        internal string ItemName { get; set; } = string.Empty;
        internal string DisplayName { get; set; } = string.Empty;
        internal float UnitPrice { get; set; }
        internal float LinePrice { get; set; }
        internal int PackSize { get; set; } = 1;
        internal bool IsQuantityItem { get; set; }
        internal bool InStock { get; set; } = true;
        internal Sprite Icon { get; set; }

        internal int DesiredQuantity { get; set; }
        internal int PickedQuantity { get; set; }

        internal bool CanAddMore(int maxSlots, int currentSlots) =>
            InStock && currentSlots < maxSlots;
    }
}
