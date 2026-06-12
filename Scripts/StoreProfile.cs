using BigAmbitions.Tags;
using Buildings;
using Entities;
using Helpers;

namespace AutoShopping
{
    internal enum StoreKind
    {
        Unsupported,
        RetailSelfService,
        Wholesale,
        CinemaTheater
    }

    internal sealed class StoreProfile
    {
        internal StoreKind Kind { get; private set; }
        internal bool NeedsShoppingBasket { get; private set; }
        internal bool AllowsHandTruck { get; private set; }
        internal bool IsSupported { get; private set; }
        internal string BusinessTypeName { get; private set; } = string.Empty;
        internal string BusinessDisplayName { get; private set; } = string.Empty;

        internal static StoreProfile Detect(BuildingManager buildingManager)
        {
            var profile = new StoreProfile();
            var registration = buildingManager.buildingRegistration;
            var businessType = buildingManager.businessType;

            if (registration == null || businessType == null)
                return profile;

            profile.BusinessTypeName = registration.businessTypeName ?? string.Empty;
            profile.BusinessDisplayName = registration.BusinessName ?? string.Empty;
            profile.NeedsShoppingBasket = businessType.CustomersNeedShoppingContainer;

            var customerType = businessType.customerType;
            var isWholesale = profile.BusinessTypeName == "ba:businesstype_wholesalestore";

            if (isWholesale)
            {
                profile.Kind = StoreKind.Wholesale;
                profile.AllowsHandTruck = true;
                profile.IsSupported = true;
                return profile;
            }

            if (customerType == CustomerType.SelfService)
            {
                profile.Kind = StoreKind.RetailSelfService;
                profile.AllowsHandTruck = false;
                profile.IsSupported = true;
                return profile;
            }

            if (customerType == CustomerType.CinemaTheater)
            {
                profile.Kind = StoreKind.CinemaTheater;
                profile.AllowsHandTruck = false;
                profile.IsSupported = true;
                return profile;
            }

            profile.Kind = StoreKind.Unsupported;
            profile.IsSupported = false;
            return profile;
        }

        internal bool OffersShoppingBasket() => NeedsShoppingBasket || Kind == StoreKind.RetailSelfService;

        internal bool OffersHandTruck() => AllowsHandTruck;

        internal bool OffersShoppingCart() => Kind != StoreKind.Wholesale;

        /// <summary>Default container: basket in retail, biggest vehicle in wholesale.</summary>
        internal ShoppingContainerTarget GetDefaultContainerTarget()
        {
            using var perf = ModPerf.Measure("profile.default_container");
            var basket = OffersShoppingBasket() && ShoppingCargoHelper.HasBasketProviderInStore()
                ? ShoppingCargoHelper.GetBasketCapacity()
                : 0;
            var cart = OffersShoppingCart() ? ShoppingCargoHelper.GetBestShoppingCartCapacityInStore() : 0;
            var truck = AllowsHandTruck ? ShoppingCargoHelper.GetBestHandTruckCapacityInStore() : 0;

            if (Kind == StoreKind.RetailSelfService && basket > 1)
                return ShoppingContainerTarget.ShoppingBasket;

            if (truck >= cart && truck >= basket && truck > 1)
                return ShoppingContainerTarget.HandTruck;
            if (cart >= basket && cart > 1)
                return ShoppingContainerTarget.ShoppingCart;
            if (basket > 1)
                return ShoppingContainerTarget.ShoppingBasket;
            return ShoppingContainerTarget.BareHands;
        }

        internal bool HasRequiredContainer()
        {
            if (Kind == StoreKind.Wholesale)
                return ShoppingCargoHelper.IsUsingHandTruck() || ShoppingCargoHelper.HasHandCargo();

            if (NeedsShoppingBasket)
                return PlayerHelper.IsHoldingShoppingBasket || ShoppingCargoHelper.IsUsingShoppingVehicle();

            return ShoppingCargoHelper.HasHandCargo() || ShoppingCargoHelper.IsUsingShoppingVehicle();
        }
    }
}
