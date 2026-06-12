using UnityEngine;

namespace AutoShopping
{
    internal sealed class AutoShoppingDriver : MonoBehaviour
    {
        internal static AutoShoppingDriver Instance { get; private set; }

        private readonly ShoppingActionQueue _queue = new ShoppingActionQueue();
        private float _nextTogglePoll;
        private float _nextFooterRefresh;

        internal ShoppingActionQueue ActionQueue => _queue;

        private void Awake()
        {
            Instance = this;
            _queue.Bind(this);
            AutoShoppingPanel.BindQueue(_queue);
        }

        private void OnDestroy()
        {
            if (Instance == this)
                Instance = null;
        }

        private void Update()
        {
            ModUiText.PollLanguageChange();
            AutoShoppingToggleHud.UpdateVisibility();
            AutoShoppingPanel.UpdateVisibility();
            PollToggleKey();

            if (AutoShoppingPanel.IsVisible && Time.unscaledTime >= _nextFooterRefresh)
            {
                _nextFooterRefresh = Time.unscaledTime + 0.25f;
                AutoShoppingPanel.RefreshFooterOnly();
            }
        }

        private void PollToggleKey()
        {
            if (!GameState.IsWorldReady() || GameState.IsUiBlocking())
                return;

            var now = Time.unscaledTime;
            if (now < _nextTogglePoll)
                return;

            _nextTogglePoll = now + AutoShoppingConfig.ToggleKeyPollInterval;

            if (!Input.GetKeyDown(KeyCode.F8))
                return;

            if (!GameState.ShouldShowStoreShoppingUi())
                return;

            AutoShoppingPanel.Toggle();
        }
    }
}
