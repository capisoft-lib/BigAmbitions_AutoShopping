using System.Collections.Generic;
using System.Linq;
using BigAmbitions.Items;
using BigAmbitions.Tags;
using Controllers;
using Entities;
using Helpers;
using UnityEngine;
using Vehicles.VehicleTypes;

namespace AutoShopping
{
    internal static class ShoppingCargoHelper
    {
        private const string HandTruckVehicleType = "ba:vehicletype_handtruck";

        internal static ICargoHolder GetActiveHolder()
        {
            if (PlayerHelper.IsUsingVehicle)
                return VehicleHelper.GetCurrentVehicle();

            if (PlayerHelper.IsHoldingItem)
                return PlayerHelper.ItemInstanceInHands;

            return null;
        }

        internal static bool HasHandCargo() => GetActiveHolder() != null;

        internal static bool IsUsingShoppingVehicle()
        {
            if (!PlayerHelper.IsUsingVehicle)
                return false;

            var vehicle = VehicleHelper.GetCurrentVehicle();
            return vehicle != null && vehicle.VehicleType.HasTag(TagRef.Vehicletag.ishandvehicle);
        }

        internal static bool IsUsingHandTruck() =>
            IsUsingShoppingVehicle() &&
            VehicleHelper.GetCurrentVehicle()?.vehicleTypeName == HandTruckVehicleType;

        internal static bool IsHandTruckVehicleType(string vehicleTypeName) =>
            vehicleTypeName == HandTruckVehicleType;

        internal static int GetContainerCapacity()
        {
            if (PlayerHelper.IsUsingVehicle)
            {
                var vehicle = VehicleHelper.GetCurrentVehicle();
                if (vehicle != null)
                    return vehicle.VehicleType.maxCargoCapacity;
            }

            if (PlayerHelper.IsHoldingItem)
                return PlayerHelper.ItemInstanceInHands.ItemCached.cargoCapacity;

            return 0;
        }

        internal static ShoppingContainerTarget DetectActiveTarget()
        {
            if (IsUsingHandTruck())
                return ShoppingContainerTarget.HandTruck;

            if (IsUsingShoppingVehicle())
                return ShoppingContainerTarget.ShoppingCart;

            if (PlayerHelper.IsHoldingShoppingBasket)
                return ShoppingContainerTarget.ShoppingBasket;

            return ShoppingContainerTarget.BareHands;
        }

        internal static string GetActiveCartStateLabel()
        {
            if (PlayerHelper.IsUsingVehicle)
            {
                var vehicle = VehicleHelper.GetCurrentVehicle();
                if (vehicle != null && vehicle.VehicleType.HasTag(TagRef.Vehicletag.ishandvehicle))
                {
                    return vehicle.VehicleType.vehicleTypeName == "ba:vehicletype_handtruck"
                        ? ModUiText.ContainerHandTruck
                        : ModUiText.ContainerBigCart;
                }
            }

            if (PlayerHelper.IsHoldingShoppingBasket)
                return ModUiText.ContainerBasket;

            return ModUiText.ContainerBareHands;
        }

        internal static bool HasContainerToRelease() =>
            IsUsingShoppingVehicle() || PlayerHelper.IsHoldingShoppingBasket;

        internal static bool TryReleaseContainerAtFeet()
        {
            if (IsUsingShoppingVehicle())
                return TryParkShoppingVehicle();

            if (PlayerHelper.IsHoldingShoppingBasket)
                return TryPlaceBasketOnGround();

            return false;
        }

        private static bool TryParkShoppingVehicle()
        {
            if (!IsUsingShoppingVehicle())
                return false;

            var vehicle = VehicleHelper.GetCurrentVehicleBase();
            if (vehicle == null)
                return false;

            vehicle.ExitVehicle();
            return true;
        }

        private static bool TryPlaceBasketOnGround()
        {
            if (!PlayerHelper.IsHoldingShoppingBasket)
                return false;

            var bm = InstanceBehavior<BuildingManager>.Instance;
            var itemInstance = PlayerHelper.ItemInstanceInHands;
            if (bm?.buildingRegistration == null || itemInstance == null)
                return false;

            var placePos = ComputeDropPosition();
            itemInstance.position = new SerializableVector3(placePos.x, placePos.y, placePos.z);

            var player = InstanceBehavior<GameManager>.Instance?.playerController;
            if (player != null)
                itemInstance.yRotation = ((Component)player).transform.eulerAngles.y;

            bm.buildingRegistration.AddItemInstanceToBuilding(itemInstance);
            bm.InstantiateSingleInstance(itemInstance);
            PlayerHelper.ItemInstanceInHands = null;
            GameEvent.Invoke("ba:gameevent_itemcargochanged");
            return true;
        }

        private static Vector3 ComputeDropPosition()
        {
            var placePos = PlayerHelper.GetPosition();
            var player = InstanceBehavior<GameManager>.Instance?.playerController;
            if (player != null)
            {
                var forward = ((Component)player).transform.forward;
                forward.y = 0f;
                if (forward.sqrMagnitude > 0.01f)
                    placePos += forward.normalized * 0.6f;
            }

            placePos.y = 0f;
            return placePos;
        }

        internal static int GetBasketCapacity()
        {
            var best = 1;
            try
            {
                foreach (var itemName in ItemsGetter.GetAllItemNamesByTag(TagRef.Itemtag.isshoppingcontainer))
                {
                    var item = ItemsGetter.GetByName(itemName);
                    if (item != null && item.cargoCapacity > best)
                        best = item.cargoCapacity;
                }
            }
            catch
            {
                // catalog not ready
            }

            return best;
        }

        internal static int GetHandTruckCapacity()
        {
            try
            {
                var handTruck = VehicleTypeHelper.GetVehicleType("ba:vehicletype_handtruck");
                if (handTruck != null)
                    return handTruck.maxCargoCapacity;
            }
            catch
            {
                // ignore
            }

            return 1;
        }

        internal static int GetCapacityForTarget(ShoppingContainerTarget target)
        {
            switch (target)
            {
                case ShoppingContainerTarget.ShoppingBasket:
                    return GetBasketCapacity();
                case ShoppingContainerTarget.ShoppingCart:
                    return GetBestShoppingCartCapacityInStore();
                case ShoppingContainerTarget.HandTruck:
                    return GetHandTruckCapacity();
                default:
                    return 1;
            }
        }

        /// <summary>Largest container capacity offered in this store (basket vs cart vs hand truck).</summary>
        internal static int GetBestStoreContainerCapacity(StoreProfile profile)
        {
            var best = 1;

            if (profile != null && profile.AllowsHandTruck)
                best = Mathf.Max(best, GetBestHandTruckCapacityInStore());

            best = Mathf.Max(best, GetBestShoppingCartCapacityInStore());

            if (profile == null || profile.OffersShoppingBasket())
                best = Mathf.Max(best, HasBasketProviderInStore() ? GetBasketCapacity() : 0);

            return best;
        }

        internal static bool HasBasketProviderInStore() =>
            FindNearestBasketProvider() != null;

        internal static bool HasShoppingCartSpawnerInStore() =>
            FindNearestShoppingCartSpawner() != null;

        internal static bool HasHandTruckSpawnerInStore() =>
            FindNearestHandTruckSpawner() != null;

        internal static int GetBestShoppingCartCapacityInStore() =>
            GetBestStoreVehicleCapacity(handTruckOnly: false);

        internal static int GetBestHandTruckCapacityInStore() =>
            GetBestStoreVehicleCapacity(handTruckOnly: true);

        private static int GetBestStoreVehicleCapacity(bool handTruckOnly)
        {
            var best = 0;
            var bm = InstanceBehavior<BuildingManager>.Instance;
            if (bm == null)
                return best;

            foreach (var controller in bm.allItemControllers)
            {
                if (controller is not VehicleSpawnerController spawner || spawner.Cost != 0f)
                    continue;

                var isHandTruck = IsHandTruckVehicleType(spawner.vehicleType);
                if (handTruckOnly != isHandTruck)
                    continue;

                var capacity = TryGetVehicleCapacity(spawner.vehicleType);
                if (capacity > best)
                    best = capacity;
            }

            return best;
        }

        private static int TryGetVehicleCapacity(string vehicleTypeName)
        {
            try
            {
                var vehicleType = VehicleTypeHelper.GetVehicleType(vehicleTypeName);
                if (vehicleType == null || !vehicleType.HasTag(TagRef.Vehicletag.ishandvehicle))
                    return 0;

                return vehicleType.maxCargoCapacity;
            }
            catch
            {
                return 0;
            }
        }

        internal static int CountUnpaidSlots(ICargoHolder holder)
        {
            if (holder == null)
                return 0;

            return holder.GetCargoInstances().Count(x => !x.paid);
        }

        internal static int CountUnpaidForItem(ICargoHolder holder, string itemName)
        {
            if (holder == null || string.IsNullOrEmpty(itemName))
                return 0;

            return holder.GetCargoInstances().Count(x => !x.paid && x.itemName == itemName);
        }

        internal static float SumUnpaidTotal(ICargoHolder holder)
        {
            if (holder == null)
                return 0f;

            float total = 0f;
            foreach (var cargo in holder.GetCargoInstances())
            {
                if (cargo.paid)
                    continue;

                total += cargo.pricePerUnit * cargo.amount;
            }

            return total;
        }

        internal static List<CargoInstance> GetUnpaidCargo(ICargoHolder holder)
        {
            if (holder == null)
                return new List<CargoInstance>();

            return holder.GetCargoInstances().Where(x => !x.paid).ToList();
        }

        internal static bool TryRemoveOneUnpaid(string itemName)
        {
            var holder = GetActiveHolder();
            if (holder == null)
                return false;

            var instances = holder.GetCargoInstances();
            for (var i = instances.Count - 1; i >= 0; i--)
            {
                var cargo = instances[i];
                if (cargo.paid || cargo.itemName != itemName)
                    continue;

                TryReturnCargoToStore(cargo, holder);
                holder.RemoveFromCargo(cargo);
                GameEvent.Invoke("ba:gameevent_itemcargochanged");
                return true;
            }

            return false;
        }

        private static void TryReturnCargoToStore(CargoInstance cargo, ICargoHolder holder)
        {
            try
            {
                var bm = InstanceBehavior<BuildingManager>.Instance;
                if (bm?.buildingRegistration == null)
                    return;

                cargo.ReturnToAShelf(bm.buildingRegistration.Address, holder as ItemInstance);
            }
            catch
            {
                // Best-effort restock when removing from basket.
            }
        }

        internal static ItemController FindNearestPurchasableShelf(string itemName)
        {
            var bm = InstanceBehavior<BuildingManager>.Instance;
            if (bm == null)
                return null;

            return bm.FindOptimalItemController(itemName, PlayerHelper.GetPosition());
        }

        internal static ItemController FindNearestBasketProvider()
        {
            var bm = InstanceBehavior<BuildingManager>.Instance;
            if (bm == null)
                return null;

            var position = PlayerHelper.GetPosition();
            ItemController best = null;
            var bestDist = float.MaxValue;

            foreach (var controller in bm.allItemControllers)
            {
                if (controller == null || controller.Item == null)
                    continue;

                if (!controller.Item.HasTag(TagRef.Itemtag.isshoppingcontainerprovider))
                    continue;

                var dist = Vector3.SqrMagnitude(controller.transform.position - position);
                if (dist >= bestDist)
                    continue;

                bestDist = dist;
                best = controller;
            }

            return best;
        }

        internal static VehicleSpawnerController FindNearestHandTruckSpawner() =>
            FindNearestStoreVehicleSpawner(handTruckOnly: true);

        internal static VehicleSpawnerController FindNearestShoppingCartSpawner() =>
            FindNearestStoreVehicleSpawner(handTruckOnly: false);

        private static VehicleSpawnerController FindNearestStoreVehicleSpawner(bool handTruckOnly)
        {
            var bm = InstanceBehavior<BuildingManager>.Instance;
            if (bm == null)
                return null;

            var position = PlayerHelper.GetPosition();
            VehicleSpawnerController best = null;
            var bestDist = float.MaxValue;

            foreach (var controller in bm.allItemControllers)
            {
                if (controller is not VehicleSpawnerController spawner || spawner.Cost != 0f)
                    continue;

                var isHandTruck = IsHandTruckVehicleType(spawner.vehicleType);
                if (handTruckOnly != isHandTruck)
                    continue;

                if (TryGetVehicleCapacity(spawner.vehicleType) <= 0)
                    continue;

                var dist = Vector3.SqrMagnitude(spawner.transform.position - position);
                if (dist >= bestDist)
                    continue;

                bestDist = dist;
                best = spawner;
            }

            return best;
        }

        internal static CashRegisterController FindNearestCashRegister()
        {
            var bm = InstanceBehavior<BuildingManager>.Instance;
            if (bm == null)
                return null;

            var position = PlayerHelper.GetPosition();
            var hasUnpaid = !PlayerHelper.HasPaidForAllItems();
            CashRegisterController best = null;
            var bestDist = float.MaxValue;

            foreach (var controller in bm.allItemControllers)
            {
                if (controller is not CashRegisterController register)
                    continue;

                if (register.playerItemPurchaserSettings != null && register.playerItemPurchaserSettings.enabled)
                    continue;

                if (hasUnpaid)
                {
                    var customerType = bm.businessType?.customerType ?? CustomerType.None;
                    if (customerType != CustomerType.SelfService && customerType != CustomerType.CinemaTheater)
                        continue;
                }
                else if (!register.CanOrder())
                {
                    continue;
                }

                var dist = Vector3.SqrMagnitude(register.transform.position - position);
                if (dist >= bestDist)
                    continue;

                bestDist = dist;
                best = register;
            }

            return best;
        }

        internal static string GetItemLabel(string itemName)
        {
            try
            {
                return LocalizationHelper.GetItemLabel(itemName).ToString();
            }
            catch
            {
                return itemName;
            }
        }
    }
}
