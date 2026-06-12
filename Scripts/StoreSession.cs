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
            var selectedTarget = ResolveInitialContainerTarget(profile);

            Current = new StoreSession
            {
                Profile = profile,
                IsActive = true,
                SelectedTarget = selectedTarget
            };
            Current.Products.Clear();
            if (products != null)
                Current.Products.AddRange(products);

            Current.SyncContainerFromPlayer();
            StoreCatalogService.SyncPickedQuantities(Current.Products);
            Current.SyncDesiredFromPicked();
            ModLog.Info("Store session started: " + (profile?.BusinessDisplayName ?? "?") +
                        " | products=" + Current.Products.Count +
                        " | container=" + Current.SelectedTarget +
                        " | slots=" + Current.GetCurrentSlots() + "/" + Current.GetMaxSlots());
        }

        private static ShoppingContainerTarget ResolveInitialContainerTarget(StoreProfile profile)
        {
            var activeTarget = ShoppingCargoHelper.DetectActiveTarget();
            if (activeTarget != ShoppingContainerTarget.BareHands)
                return activeTarget;

            if (AutoShoppingConfig.AutoPickCartEnabled && profile != null)
                return profile.GetDefaultContainerTarget();

            return ShoppingContainerTarget.BareHands;
        }

        /// <summary>Keep session container/capacity in sync with what the player is already holding.</summary>
        internal void SyncContainerFromPlayer()
        {
            var activeTarget = ShoppingCargoHelper.DetectActiveTarget();
            if (activeTarget == ShoppingContainerTarget.BareHands)
                return;

            if (SelectedTarget == activeTarget)
                return;

            ModLog.Info("Synced active container: " + SelectedTarget + " -> " + activeTarget);
            SelectedTarget = activeTarget;
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

        /// <summary>Mirror picked counts so +/- starts from what is already in the container.</summary>
        internal void SyncDesiredFromPicked()
        {
            foreach (var product in Products)
                product.DesiredQuantity = product.PickedQuantity;
        }

        internal bool CanIncreaseDesired()
        {
            SyncContainerFromPlayer();
            var maxSlots = GetMaxSlots();
            var totalDesired = 0;
            foreach (var product in Products)
                totalDesired += product.DesiredQuantity;

            return totalDesired < maxSlots;
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

            var activeTarget = ShoppingCargoHelper.DetectActiveTarget();
            if (activeTarget != ShoppingContainerTarget.BareHands)
            {
                var activeTargetCapacity = ShoppingCargoHelper.GetCapacityForTarget(activeTarget);
                if (activeTargetCapacity > 0)
                    return activeTargetCapacity;
            }

            var selectedCapacity = ShoppingCargoHelper.GetCapacityForTarget(SelectedTarget);
            if (selectedCapacity > 0)
                return selectedCapacity;

            if (Profile != null)
            {
                var storeCapacity = ShoppingCargoHelper.GetBestStoreContainerCapacity(Profile);
                if (storeCapacity > 0)
                    return storeCapacity;
            }

            return 1;
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
