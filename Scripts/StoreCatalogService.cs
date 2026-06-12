using System.Collections.Generic;
using BigAmbitions.Items;
using Controllers;
using Helpers;
using UnityEngine;

namespace AutoShopping
{
    internal static class StoreCatalogService
    {
        internal static List<CatalogProduct> Scan(StoreProfile profile)
        {
            var results = new Dictionary<string, CatalogProduct>();
            var bm = InstanceBehavior<BuildingManager>.Instance;
            if (bm == null || profile == null || !profile.IsSupported)
                return new List<CatalogProduct>();

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
            return list;
        }

        internal static void SyncPickedQuantities(IList<CatalogProduct> products)
        {
            var holder = ShoppingCargoHelper.GetActiveHolder();
            if (products == null)
                return;

            foreach (var product in products)
            {
                product.PickedQuantity = ShoppingCargoHelper.CountUnpaidForItem(holder, product.ItemName);
            }
        }
    }
}
