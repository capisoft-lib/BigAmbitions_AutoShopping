using System;
using System.Collections;
using Buildings;
using Helpers;
using UI.MiniMenu;
using UnityEngine;

namespace AutoShopping
{
    internal sealed class AutoShoppingDriver : MonoBehaviour
    {
        internal static AutoShoppingDriver Instance { get; private set; }

        private readonly ShoppingActionQueue _queue = new ShoppingActionQueue();
        private float _nextVisibilityPoll;
        private float _nextBalanceRefresh;
        private Action<string> _onGameEvent;
        private Coroutine _catalogLoad;

        internal ShoppingActionQueue ActionQueue => _queue;

        private void Awake()
        {
            Instance = this;
            _queue.Bind(this);
            AutoShoppingPanel.BindQueue(_queue);

            _onGameEvent = OnGameEvent;
            GameEvent.onGameEventTriggered = (Action<string>)Delegate.Combine(
                GameEvent.onGameEventTriggered,
                _onGameEvent);
            MiniMenu.OnToggled += OnPauseMenuToggled;
        }

        private void OnDestroy()
        {
            MiniMenu.OnToggled -= OnPauseMenuToggled;

            if (_onGameEvent != null)
            {
                GameEvent.onGameEventTriggered = (Action<string>)Delegate.Remove(
                    GameEvent.onGameEventTriggered,
                    _onGameEvent);
                _onGameEvent = null;
            }

            if (Instance == this)
                Instance = null;
        }

        private static void OnPauseMenuToggled(bool isOpen)
        {
            GameState.InvalidateUiBlockingCache();
            if (isOpen)
                Instance?.ActionQueue?.Cancel();

            AutoShoppingToggleHud.UpdateVisibility();
            AutoShoppingPanel.UpdateVisibility();
        }

        private void Update()
        {
            using (ModPerf.Measure("driver.update"))
            {
                ModUiText.PollLanguageChange();
                PollVisibility();
                PollToggleKey();
                StoreItemRouteService.Tick();
            }

            if (AutoShoppingConfig.LogPerf)
            {
                ModPerf.NotifyDriverUpdate();
                ModPerf.Tick();
            }
        }

        internal void BeginStoreSession(Address address)
        {
            if (_catalogLoad != null)
                StopCoroutine(_catalogLoad);

            _catalogLoad = StartCoroutine(LoadStoreSession(address));
        }

        private IEnumerator LoadStoreSession(Address address)
        {
            yield return null;

            _catalogLoad = null;
            if (!GameState.IsWorldReady() || !BuildingManager.IsInsideBuilding)
                yield break;

            var bm = InstanceBehavior<BuildingManager>.Instance;
            if (bm == null)
                yield break;

            StoreProfile profile;
            using (ModPerf.Measure("session.detect_profile"))
                profile = StoreProfile.Detect(bm);
            if (!profile.IsSupported)
            {
                ModLog.Info("Unsupported store at " + address + " type=" + profile.BusinessTypeName);
                StoreSession.End();
                AutoShoppingPanel.Hide();
                yield break;
            }

            var products = StoreCatalogService.Scan(profile, bm);
            if (products.Count == 0)
            {
                ModLog.Warn("No purchasable products found in " + profile.BusinessDisplayName);
                StoreSession.End();
                yield break;
            }

            StoreSession.Begin(profile, products);
            ModLog.Info("Catalog loaded: " + products.Count + " products");

            while (bm.enteringBuilding && BuildingManager.IsInsideBuilding)
                yield return null;

            var session = StoreSession.Current;
            if (session != null)
            {
                session.SyncContainerFromPlayer();
                session.RefreshPicked();
                session.SyncDesiredFromPicked();
            }

            if (AutoShoppingConfig.AutoPickCartEnabled)
                _queue.RequestAutoPickOnEnter(StoreSession.Current);

            if (AutoShoppingConfig.AutoOpenOnEnter)
                AutoShoppingPanel.Show();

            AutoShoppingToggleHud.UpdateVisibility();
        }

        private void PollVisibility()
        {
            var now = Time.unscaledTime;
            if (now < _nextVisibilityPoll)
                return;

            _nextVisibilityPoll = now + AutoShoppingConfig.VisibilityPollInterval;

            using (ModPerf.Measure("poll.visibility"))
            {
                if (IsExitingBuilding())
                {
                    _queue.Cancel();
                    AutoShoppingPanel.SuppressRestore();
                    AutoShoppingPanel.Hide();
                    AutoShoppingToggleHud.UpdateVisibility();
                    return;
                }

                if (!BuildingManager.IsInsideBuilding)
                {
                    if (!GameState.IsPedestrianInSupportedStore())
                    {
                        AutoShoppingPanel.UpdateVisibility();
                        AutoShoppingToggleHud.UpdateVisibility();
                    }

                    return;
                }

                AutoShoppingToggleHud.UpdateVisibility();
                AutoShoppingPanel.UpdateVisibility();

                if (AutoShoppingPanel.IsVisible && now >= _nextBalanceRefresh)
                {
                    _nextBalanceRefresh = now + 0.75f;
                    AutoShoppingPanel.RefreshFooterOnly();
                }
            }
        }

        private static bool IsExitingBuilding()
        {
            try
            {
                var bm = InstanceBehavior<BuildingManager>.Instance;
                return bm != null && bm.exitingBuilding;
            }
            catch
            {
                return false;
            }
        }

        private void PollToggleKey()
        {
            using (ModPerf.Measure("poll.toggle_key"))
            {
                PollToggleKeyCore();
            }
        }

        private void PollToggleKeyCore()
        {
            // GetKeyDown is true for one frame only — must check every Update, not on a timer.
            if (!Input.GetKeyDown(KeyCode.F8))
                return;

            if (!GameState.IsWorldReady())
                return;

            if (AutoShoppingPanel.IsSearchFocused)
                return;

            if (!GameState.ShouldShowStoreShoppingUi())
                return;

            AutoShoppingPanel.Toggle();
        }

        private void OnGameEvent(string gameEvent)
        {
            if (gameEvent != "ba:gameevent_itemcargochanged")
                return;

            StoreSession.Current?.SyncContainerFromPlayer();

            if (!AutoShoppingPanel.IsVisible)
                return;

            using (ModPerf.Measure("event.cargo_changed_refresh"))
                AutoShoppingPanel.RefreshFooterOnly();
        }
    }
}
