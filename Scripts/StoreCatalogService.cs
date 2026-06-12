using System.Collections.Generic;
using BigAmbitions.Items;
using Buildings;
using Controllers;
using Helpers;
using UnityEngine;

namespace AutoShopping
{
    internal static class StoreCatalogService
    {
        private static readonly Dictionary<string, int> UnpaidCountScratch = new Dictionary<string, int>();

        internal static List<CatalogProduct> Scan(StoreProfile profile, BuildingManager bm = null)
        {
            var scanStart = System.Diagnostics.Stopwatch.GetTimestamp();

            bm ??= InstanceBehavior<BuildingManager>.Instance;
            var results = new Dictionary<string, CatalogProduct>();
            if (bm == null || profile == null || !profile.IsSupported)
                return new List<CatalogProduct>();

            var controllerCount = bm.allItemControllers?.Count ?? 0;

            var registration = bm.buildingRegistration;
            var isWholesale = profile.Kind == StoreKind.Wholesale;

            foreach (var controller in bm.allItemControllers)
            {
                if (controller == null)
                    continue;

                var settings = controller.playerItemPurchaserSettings;
                if (settings == null || !settings.enabled)
                    continue;

                var itemName = settings.itemName;
                if (string.IsNullOrEmpty(itemName))
                    itemName = controller.GetProducedItemName();

                if (string.IsNullOrEmpty(itemName))
                    continue;

                if (results.ContainsKey(itemName))
                    continue;

                var item = ItemsGetter.GetByName(itemName);
                if (item == null)
                    continue;

                var fillState = PlayerItemPurchaser.GetShelfFillState(itemName, registration);
                var inStock = fillState > 0f;

                float unitPrice;
                float linePrice;
                if (controller.PlayerItemPurchaser != null)
                {
                    controller.PlayerItemPurchaser.UpdatePriceInfo();
                    linePrice = controller.PlayerItemPurchaser.TotalPrice;
                    unitPrice = settings.isQuantityItem && item.boxSize > 0
                        ? linePrice / item.boxSize
                        : linePrice;
                }
                else
                {
                    unitPrice = isWholesale
                        ? item.GetWholesalePrice() * ProductMarketHelper.GetProductMarketEventMultiplier(itemName, registration.Neighborhood)
                        : ItemHelper.GetPriceOnCurrentBusiness(itemName);

                    var priceIndex = registration.GetPriceIndex();
                    unitPrice *= priceIndex / 100f;
                    linePrice = unitPrice * (settings.isQuantityItem ? item.boxSize : 1);
                }

                var packSize = settings.isQuantityItem ? item.boxSize : 1;
                if (packSize <= 0)
                    packSize = 1;

                results[itemName] = new CatalogProduct
                {
                    ItemName = itemName,
                    DisplayName = ShoppingCargoHelper.GetItemLabel(itemName),
                    UnitPrice = unitPrice,
                    LinePrice = linePrice,
                    PackSize = packSize,
                    IsQuantityItem = settings.isQuantityItem,
                    InStock = inStock,
                    Icon = ItemHelper.GetIconWithFallback(itemName),
                    DesiredQuantity = 0,
                    PickedQuantity = 0
                };
            }

            var list = new List<CatalogProduct>(results.Values);
            list.Sort((a, b) => string.CompareOrdinal(a.DisplayName, b.DisplayName));

            var elapsedMs = (System.Diagnostics.Stopwatch.GetTimestamp() - scanStart) * 1000.0
                              / System.Diagnostics.Stopwatch.Frequency;
            ModPerf.RecordSlow(
                "catalog.scan",
                elapsedMs,
                elapsedMs >= 8.0 ? "products=" + list.Count + " controllers=" + controllerCount : null);

            return list;
        }

        internal static void SyncPickedQuantities(IList<CatalogProduct> products)
        {
            if (products == null)
                return;

            using var scope = ModPerf.Measure("catalog.sync_picked");
            UnpaidCountScratch.Clear();
            var holder = ShoppingCargoHelper.GetActiveHolder();
            if (holder != null)
            {
                foreach (var cargo in holder.GetCargoInstances())
                {
                    if (cargo.paid || string.IsNullOrEmpty(cargo.itemName))
                        continue;

                    if (UnpaidCountScratch.TryGetValue(cargo.itemName, out var existing))
                        UnpaidCountScratch[cargo.itemName] = existing + 1;
                    else
                        UnpaidCountScratch[cargo.itemName] = 1;
                }
            }

            foreach (var product in products)
            {
                product.PickedQuantity = UnpaidCountScratch.TryGetValue(product.ItemName, out var count) ? count : 0;
            }
        }
    }
}
