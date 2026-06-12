using System;
using System.Threading.Tasks;
using BAModAPI;
using UnityEngine;

[assembly: RegisterModClass(typeof(AutoShopping.AutoShoppingMod))]

namespace AutoShopping
{
    [ModEntryOnCityLoad]
    public sealed class AutoShoppingMod : IModBigAmbitions
    {
        private GameObject _driverObject;

        public string[] RelativeAssetBundlePaths => Array.Empty<string>();

        public Task OnLoadAsync(ModContext context)
        {
            ModLog.Info("AutoShopping city load | mod_id=" + context.ModId + " | version=0.11.10-vanilla-buttons");
            AutoShoppingConfig.Initialize(context);
            StoreBuildingWatcher.Subscribe();

            _driverObject = new GameObject("AutoShopping_Driver");
            UnityEngine.Object.DontDestroyOnLoad(_driverObject);
            _driverObject.AddComponent<AutoShoppingDriver>();

            ModLog.Info("AutoShopping ready. " + ModUiText.ToggleHint);
            return Task.CompletedTask;
        }

        public Task OnUnloadAsync()
        {
            StoreBuildingWatcher.Unsubscribe();
            AutoShoppingPanel.Destroy();
            AutoShoppingToggleHud.Destroy();

            if (_driverObject != null)
            {
                UnityEngine.Object.Destroy(_driverObject);
                _driverObject = null;
            }

            StoreSession.End();
            AutoShoppingConfig.Shutdown();
            ModLog.Info("AutoShopping unloaded.");
            return Task.CompletedTask;
        }
    }
}
