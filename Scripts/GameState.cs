using Helpers;
using PlayerActivity;
using UI;
using UI.InteriorDesigner;
using UI.MiniMenu;
using UI.Purchase;
using UI.PurchaseVehicle;
using UI.Smartphone;

namespace AutoShopping
{
    internal static class GameState
    {
        internal static bool IsWorldReady()
        {
            try
            {
                if (!GameManager.IsInitialized)
                    return false;

                var gm = GameManager.Instance;
                if (gm == null || gm.playerController == null)
                    return false;

                if (IsSceneLoading())
                    return false;

                var save = SaveGameManager.Current;
                if (save == null || !save.CityInitialized)
                    return false;

                if (!BuildingManager.IsInitialized)
                    return false;
            }
            catch
            {
                return false;
            }

            return true;
        }

        internal static bool IsInsideSupportedInterior()
        {
            try
            {
                if (!BuildingManager.IsInsideBuilding)
                    return false;

                var bm = InstanceBehavior<BuildingManager>.Instance;
                if (bm == null || bm.businessType == null)
                    return false;

                return StoreProfile.Detect(bm).IsSupported;
            }
            catch
            {
                return false;
            }
        }

        internal static bool IsPedestrianInSupportedStore()
        {
            if (!IsWorldReady() || StoreSession.Current == null)
                return false;

            if (!IsInsideSupportedInterior())
                return false;

            try
            {
                if (VehicleHelper.IsInsideVehicle())
                    return false;
            }
            catch
            {
                return false;
            }

            return true;
        }

        internal static bool ShouldShowStoreShoppingUi() =>
            IsPedestrianInSupportedStore() && !IsUiBlocking();

        internal static bool IsCheckoutUiBlocking()
        {
            try
            {
                return PurchaseUI.IsPanelOpen;
            }
            catch
            {
                return false;
            }
        }

        internal static bool IsUiBlocking()
        {
            try
            {
                if (CityMap.IsOpen || FullMenu.IsOpen || MiniMenu.IsOpen)
                    return true;

                if (InteriorDesignerUI.IsOpen || PurchaseUI.IsPanelOpen || PurchaseVehicleUI.IsPanelOpen)
                    return true;

                if (PlayerActivityUI.IsPanelOpen)
                    return true;

                if (InstanceBehavior<UIs>.IsInitialized)
                {
                    var hud = InstanceBehavior<UIs>.Instance?.playerHUD;
                    if (hud != null)
                    {
                        if (hud.dialogUI.isPanelOpen || hud.manageCargoUI.isPanelOpen || hud.jobOfferPanel.isPanelOpen)
                            return true;
                    }

                    if (InstanceBehavior<UIs>.Instance.notificationsListUI != null
                        && InstanceBehavior<UIs>.Instance.notificationsListUI.isVisible)
                        return true;
                }

                if (BuildingPreview.isPreviewing)
                    return true;
            }
            catch
            {
                return true;
            }

            return false;
        }

        private static bool IsSceneLoading()
        {
            try
            {
                var asm = typeof(BuildingManager).Assembly;
                var loadScene = asm.GetType("LoadScene") ?? asm.GetType("UI.Load.LoadScene");
                var field = loadScene?.GetField("isLoading",
                    System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static);
                if (field != null && field.GetValue(null) is bool loading && loading)
                    return true;
            }
            catch
            {
                // ignore
            }

            return false;
        }
    }
}
