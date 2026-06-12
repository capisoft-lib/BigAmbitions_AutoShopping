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
            ModLog.Boot("AutoShopping city load | mod_id=" + context.ModId + " | version=0.11.14-perf"
                        + " | perf_log=" + AutoShoppingConfig.LogPerf);
            BaGameUiChrome.EnsureInitialized();
            StoreBuildingWatcher.Subscribe();

            _driverObject = new GameObject("AutoShopping_Driver");
            UnityEngine.Object.DontDestroyOnLoad(_driverObject);
            _driverObject.AddComponent<AutoShoppingDriver>();

            ModLog.Boot("AutoShopping ready. " + ModUiText.ToggleHint
                        + " | Perf logs: Logs/auto_shopping_perf.log (every 5s + SLOW >12ms)");
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
            ModPerf.FlushSummary();
            AutoShoppingConfig.Shutdown();
            ModLog.Boot("AutoShopping unloaded.");
            return Task.CompletedTask;
        }
    }
}
