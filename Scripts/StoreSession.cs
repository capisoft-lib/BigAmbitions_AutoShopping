using System.Collections.Generic;

namespace AutoShopping
{
    internal sealed class StoreSession
    {
        internal static StoreSession Current { get; private set; }

        internal StoreProfile Profile { get; private set; }
        internal List<CatalogProduct> Products { get; } = new List<CatalogProduct>();
        internal bool IsActive { get; private set; }
        internal ShoppingContainerTarget SelectedTarget { get; set; } = ShoppingContainerTarget.BareHands;

        internal static void Begin(StoreProfile profile, List<CatalogProduct> products)
        {
            var selectedTarget = ShoppingContainerTarget.BareHands;
            if (AutoShoppingConfig.AutoPickCartEnabled && profile != null)
                selectedTarget = profile.GetDefaultContainerTarget();

            Current = new StoreSession
            {
                Profile = profile,
                IsActive = true,
                SelectedTarget = selectedTarget
            };
            Current.Products.Clear();
            if (products != null)
                Current.Products.AddRange(products);

            StoreCatalogService.SyncPickedQuantities(Current.Products);
            ModLog.Info("Store session started: " + (profile?.BusinessDisplayName ?? "?") +
                        " | products=" + Current.Products.Count);
        }

        internal static void End()
        {
            if (Current != null)
                ModLog.Info("Store session ended.");

            Current = null;
        }

        internal void RefreshPicked()
        {
            StoreCatalogService.SyncPickedQuantities(Products);
        }

        internal CatalogProduct FindProduct(string itemName)
        {
            foreach (var product in Products)
            {
                if (product.ItemName == itemName)
                    return product;
            }

            return null;
        }

        internal int GetMaxSlots() => ResolveCapacityLimit();

        internal int GetAvailableSpace() => ResolveCapacityLimit();

        private int ResolveCapacityLimit()
        {
            var activeCapacity = ShoppingCargoHelper.GetContainerCapacity();
            if (activeCapacity > 0)
                return activeCapacity;

            return ShoppingCargoHelper.GetCapacityForTarget(SelectedTarget);
        }

        internal int GetCurrentSlots()
        {
            return ShoppingCargoHelper.CountUnpaidSlots(ShoppingCargoHelper.GetActiveHolder());
        }

        internal float GetDesiredTotal()
        {
            float total = 0f;
            foreach (var product in Products)
            {
                if (product.DesiredQuantity <= 0)
                    continue;

                total += product.LinePrice * product.DesiredQuantity;
            }

            return total;
        }

        internal float GetPickedTotal() =>
            ShoppingCargoHelper.SumUnpaidTotal(ShoppingCargoHelper.GetActiveHolder());
    }
}
