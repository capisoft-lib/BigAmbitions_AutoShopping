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
            AutoShoppingConfig.Initialize(context);
            BaGameUiChrome.EnsureInitialized();
            StoreBuildingWatcher.Subscribe();

            _driverObject = new GameObject("AutoShopping_Driver");
            UnityEngine.Object.DontDestroyOnLoad(_driverObject);
            _driverObject.AddComponent<AutoShoppingDriver>();

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
            if (AutoShoppingConfig.LogPerf)
                ModPerf.FlushSummary();

            AutoShoppingConfig.Shutdown();
            return Task.CompletedTask;
        }
    }
}
