using System;
using Buildings;
using Helpers;

namespace AutoShopping
{
    internal static class StoreBuildingWatcher
    {
        private static Action<Address> _onEnter;
        private static Action<Address> _onExit;
        private static Action<Address> _onEnterDelayed;

        internal static void Subscribe()
        {
            Unsubscribe();

            _onEnter = OnEnterBuilding;
            _onExit = OnExitBuilding;
            _onEnterDelayed = OnEnterBuildingDelayed;

            GlobalEvents.onEnterBuilding = (Action<Address>)Delegate.Combine(GlobalEvents.onEnterBuilding, _onEnter);
            GlobalEvents.onExitBuilding = (Action<Address>)Delegate.Combine(GlobalEvents.onExitBuilding, _onExit);
            GlobalEvents.onEnterBuildingDelayed = (Action<Address>)Delegate.Combine(GlobalEvents.onEnterBuildingDelayed, _onEnterDelayed);
        }

        internal static void Unsubscribe()
        {
            if (_onEnter != null)
            {
                GlobalEvents.onEnterBuilding = (Action<Address>)Delegate.Remove(GlobalEvents.onEnterBuilding, _onEnter);
                _onEnter = null;
            }

            if (_onExit != null)
            {
                GlobalEvents.onExitBuilding = (Action<Address>)Delegate.Remove(GlobalEvents.onExitBuilding, _onExit);
                _onExit = null;
            }

            if (_onEnterDelayed != null)
            {
                GlobalEvents.onEnterBuildingDelayed = (Action<Address>)Delegate.Remove(GlobalEvents.onEnterBuildingDelayed, _onEnterDelayed);
                _onEnterDelayed = null;
            }
        }

        private static void OnEnterBuilding(Address address)
        {
            ModLog.Info("Enter building: " + address);
        }

        private static void OnEnterBuildingDelayed(Address address)
        {
            if (!GameState.IsWorldReady() || !BuildingManager.IsInsideBuilding)
                return;

            var bm = InstanceBehavior<BuildingManager>.Instance;
            if (bm == null)
                return;

            var profile = StoreProfile.Detect(bm);
            if (!profile.IsSupported)
            {
                ModLog.Info("Unsupported store at " + address + " type=" + profile.BusinessTypeName);
                StoreSession.End();
                AutoShoppingPanel.Hide();
                return;
            }

            var products = StoreCatalogService.Scan(profile);
            if (products.Count == 0)
            {
                ModLog.Warn("No purchasable products found in " + profile.BusinessDisplayName);
                StoreSession.End();
                return;
            }

            StoreSession.Begin(profile, products);
            ModLog.Info("Catalog loaded: " + products.Count + " products");

            if (AutoShoppingConfig.AutoPickCartEnabled)
                AutoShoppingDriver.Instance?.ActionQueue?.RequestAutoPickOnEnter(StoreSession.Current);

            if (AutoShoppingConfig.AutoOpenOnEnter)
                AutoShoppingPanel.Show();
        }

        private static void OnExitBuilding(Address address)
        {
            ModLog.Info("Exit building: " + address);
            AutoShoppingDriver.Instance?.ActionQueue?.Cancel();
            StoreSession.End();
            AutoShoppingPanel.Hide();
        }
    }
}
