using System.Reflection;
using BlueprintsUI;
using Helpers;
using PlayerActivity;
using Scenes.MainMenu;
using UI;
using UI.InteriorDesigner;
using UI.MiniMenu;
using UI.Purchase;
using UI.PurchaseVehicle;
using UI.Smartphone;
using UI.Smartphone.Apps.Feedback;
using UnityEngine;

namespace AutoShopping
{
    internal static class GameState
    {
        private static readonly FieldInfo IsLoadingField = ResolveSceneLoadingField();
        private static bool? _cachedSceneLoading;
        private static float _nextSceneLoadingCheck;
        private static bool? _cachedUiBlocking;
        private static float _nextUiBlockingCheck;

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

                var session = StoreSession.Current;
                if (session != null && session.IsActive && session.Profile != null)
                    return session.Profile.IsSupported;

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
            if (!IsWorldReady() || StoreSession.Current == null || !StoreSession.Current.IsActive)
                return false;

            if (!BuildingManager.IsInsideBuilding)
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

        internal static bool IsBuildingTransitionInProgress()
        {
            try
            {
                var bm = InstanceBehavior<BuildingManager>.Instance;
                return bm != null && (bm.enteringBuilding || bm.exitingBuilding);
            }
            catch
            {
                return false;
            }
        }

        internal static bool ShouldShowStoreShoppingUi()
        {
            using (ModPerf.Measure("gamestate.should_show_ui"))
            {
                if (!IsPedestrianInSupportedStore())
                    return false;

                if (IsBuildingTransitionInProgress())
                    return false;

                return !IsUiBlocking();
            }
        }

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
            var now = Time.unscaledTime;
            if (_cachedUiBlocking.HasValue && now < _nextUiBlockingCheck)
                return _cachedUiBlocking.Value;

            _nextUiBlockingCheck = now + 0.15f;
            using (ModPerf.Measure("gamestate.ui_blocking_compute"))
                _cachedUiBlocking = ComputeUiBlocking();
            return _cachedUiBlocking.Value;
        }

        internal static void InvalidateUiBlockingCache()
        {
            _cachedUiBlocking = null;
            _nextUiBlockingCheck = 0f;
        }

        private static bool ComputeUiBlocking()
        {
            if (TryGetUiBlockingState(out var blocking))
                return blocking;

            return false;
        }

        private static bool TryGetUiBlockingState(out bool blocking)
        {
            blocking = false;

            try
            {
                if (CityMap.IsOpen || FullMenu.IsOpen || MiniMenu.IsOpen)
                {
                    blocking = true;
                    return true;
                }
            }
            catch
            {
                // ignore
            }

            try
            {
                if (Options.IsVisible || BlueprintsPanel.IsOpen || Feedback.IsOpen)
                {
                    blocking = true;
                    return true;
                }
            }
            catch
            {
                // ignore
            }

            try
            {
                if (InteriorDesignerUI.IsOpen || PurchaseUI.IsPanelOpen || PurchaseVehicleUI.IsPanelOpen)
                {
                    blocking = true;
                    return true;
                }
            }
            catch
            {
                // ignore
            }

            try
            {
                if (PlayerActivityUI.IsPanelOpen)
                {
                    blocking = true;
                    return true;
                }
            }
            catch
            {
                // ignore
            }

            try
            {
                if (InstanceBehavior<UIs>.IsInitialized)
                {
                    var hud = InstanceBehavior<UIs>.Instance?.playerHUD;
                    if (hud != null)
                    {
                        if (hud.dialogUI.isPanelOpen || hud.manageCargoUI.isPanelOpen || hud.jobOfferPanel.isPanelOpen)
                        {
                            blocking = true;
                            return true;
                        }
                    }

                    if (InstanceBehavior<UIs>.Instance.notificationsListUI != null
                        && InstanceBehavior<UIs>.Instance.notificationsListUI.isVisible)
                    {
                        blocking = true;
                        return true;
                    }
                }
            }
            catch
            {
                // ignore
            }

            try
            {
                if (BuildingPreview.isPreviewing)
                {
                    blocking = true;
                    return true;
                }
            }
            catch
            {
                // ignore
            }

            return true;
        }

        private static bool IsSceneLoading()
        {
            var now = Time.unscaledTime;
            if (_cachedSceneLoading.HasValue && now < _nextSceneLoadingCheck)
                return _cachedSceneLoading.Value;

            _nextSceneLoadingCheck = now + 0.2f;

            var loading = false;
            try
            {
                if (IsLoadingField != null && IsLoadingField.GetValue(null) is bool value)
                    loading = value;
            }
            catch
            {
                // ignore
            }

            _cachedSceneLoading = loading;
            return loading;
        }

        private static FieldInfo ResolveSceneLoadingField()
        {
            try
            {
                var asm = typeof(BuildingManager).Assembly;
                var loadScene = asm.GetType("LoadScene") ?? asm.GetType("UI.Load.LoadScene");
                return loadScene?.GetField("isLoading", BindingFlags.Public | BindingFlags.Static);
            }
            catch
            {
                return null;
            }
        }
    }
}
