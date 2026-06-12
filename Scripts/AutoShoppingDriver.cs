using System;
using System.Collections;
using Buildings;
using Helpers;
using UnityEngine;

namespace AutoShopping
{
    internal sealed class AutoShoppingDriver : MonoBehaviour
    {
        internal static AutoShoppingDriver Instance { get; private set; }

        private readonly ShoppingActionQueue _queue = new ShoppingActionQueue();
        private float _nextTogglePoll;
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
        }

        private void OnDestroy()
        {
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

        private void Update()
        {
            using (ModPerf.Measure("driver.update"))
            {
                ModUiText.PollLanguageChange();
                PollVisibility();
                PollToggleKey();
                StoreItemRouteService.Tick();
            }

            ModPerf.NotifyDriverUpdate();
            ModPerf.Tick();
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

            if (AutoShoppingConfig.AutoPickCartEnabled)
                _queue.RequestAutoPickOnEnter(StoreSession.Current);

            if (AutoShoppingConfig.AutoOpenOnEnter)
                AutoShoppingPanel.Show();
        }

        private void PollVisibility()
        {
            var now = Time.unscaledTime;
            if (now < _nextVisibilityPoll)
                return;

            _nextVisibilityPoll = now + AutoShoppingConfig.VisibilityPollInterval;

            if (!BuildingManager.IsInsideBuilding)
                return;

            using (ModPerf.Measure("poll.visibility"))
            {
                AutoShoppingToggleHud.UpdateVisibility();
                AutoShoppingPanel.UpdateVisibility();

                if (AutoShoppingPanel.IsVisible && now >= _nextBalanceRefresh)
                {
                    _nextBalanceRefresh = now + 0.75f;
                    AutoShoppingPanel.RefreshFooterOnly();
                }
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
            if (!GameState.IsWorldReady() || GameState.IsUiBlocking())
                return;

            var now = Time.unscaledTime;
            if (now < _nextTogglePoll)
                return;

            _nextTogglePoll = now + AutoShoppingConfig.ToggleKeyPollInterval;

            if (!Input.GetKeyDown(KeyCode.F8))
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

            if (!AutoShoppingPanel.IsVisible)
                return;

            using (ModPerf.Measure("event.cargo_changed_refresh"))
                AutoShoppingPanel.RefreshFooterOnly();
        }
    }
}
