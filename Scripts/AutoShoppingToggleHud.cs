using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace AutoShopping
{
    /// <summary>Top-right lateral HUD — visual parity with VoogleRoute RouteToggleHud.</summary>
    internal static class AutoShoppingToggleHud
    {
        private const string RootName = "AutoShopping_ToggleHud_v0127";

        private static GameObject _root;
        private static RectTransform _panelRect;
        private static RectTransform _headerRect;
        private static RectTransform _titleRect;
        private static RectTransform _toggleButtonRect;
        private static BaGameUiChrome.HudPanelMetrics _metrics;
        private static TextMeshProUGUI _titleLabel;
        private static Image _toggleButtonImage;
        private static TextMeshProUGUI _toggleLabel;
        private static bool _lastActive;
        private static bool _lastMainOpen;
        private static bool _forceApply = true;
        private static bool _legacyCleaned;

        internal static void EnsureCreated()
        {
            using var scope = ModPerf.Measure("toggle.ensure_created");
            if (!_legacyCleaned)
            {
                _legacyCleaned = true;
                DestroyLegacyRoots();
            }

            if (_root != null)
                return;

            BaGameUiChrome.EnsureInitialized();
            _root = new GameObject(RootName);
            Object.DontDestroyOnLoad(_root);
            BaGameUiChrome.SetupOverlayCanvas(_root, 9004, interactive: true);

            _metrics = new BaGameUiChrome.HudPanelMetrics(1f);
            _panelRect = BaGameUiChrome.BuildToggleHudPanel(_root.transform, out var header, out _);
            _headerRect = header;
            BaGameUiChrome.ConfigureBottomLeftHudAnchor(_panelRect);

            var titleGo = new GameObject("Title", typeof(RectTransform));
            titleGo.transform.SetParent(header, false);
            _titleRect = titleGo.GetComponent<RectTransform>();
            _titleRect.anchorMin = Vector2.zero;
            _titleRect.anchorMax = Vector2.one;
            _titleLabel = titleGo.AddComponent<TextMeshProUGUI>();
            BaGameUiChrome.ApplyHudTitleStyle(_titleLabel, _metrics.Scale);
            _titleLabel.text = ModUiText.PanelTitle;
            _titleLabel.overflowMode = TextOverflowModes.Ellipsis;

            var toggleButton = BaGameUiChrome.CreateHudActionButton(
                _panelRect,
                ModUiText.BtnShow,
                _metrics.FullButtonWidth,
                BaGameUiChrome.HudButtonHeight,
                _metrics.Scale,
                OnToggleClicked,
                blue: true);
            _toggleButtonRect = toggleButton.GetComponent<RectTransform>();
            _toggleButtonRect.anchorMin = _toggleButtonRect.anchorMax = new Vector2(0.5f, 1f);
            _toggleButtonRect.pivot = new Vector2(0.5f, 1f);
            _toggleButtonImage = toggleButton.GetComponentInChildren<Image>();
            _toggleLabel = toggleButton.GetComponentInChildren<TextMeshProUGUI>();

            ApplyFixedLayout();
            _forceApply = true;
            RefreshVisual();
        }

        internal static void UpdateVisibility()
        {
            using var scope = ModPerf.Measure("toggle.update_visibility");
            EnsureCreated();
            if (_root == null || _panelRect == null)
                return;

            var active = GameState.ShouldShowStoreShoppingUi();
            if (_forceApply || active != _lastActive)
            {
                _lastActive = active;
                _root.SetActive(active);
            }

            if (!active)
            {
                _forceApply = false;
                return;
            }

            var mainOpen = AutoShoppingPanel.IsVisible;
            if (_forceApply || mainOpen != _lastMainOpen)
            {
                _lastMainOpen = mainOpen;
                RefreshVisual();
            }

            _forceApply = false;
        }

        internal static void RefreshLocalizedText() => RefreshVisual();

        private static void ApplyFixedLayout()
        {
            if (_panelRect == null)
                return;

            var panelWidth = BuildingHudLayout.GetPanelWidth();
            _panelRect.sizeDelta = new Vector2(panelWidth, _metrics.PanelHeight);
            _panelRect.anchoredPosition = BuildingHudLayout.GetToggleHudPosition(panelWidth, _metrics.PanelHeight);

            if (_headerRect != null)
                BaGameUiChrome.UpdateToggleHudFrames(_panelRect, _headerRect, panelWidth);

            if (_titleRect != null)
                BaGameUiChrome.ApplyHeaderTitleInsets(_titleRect, _metrics.Scale);

            if (_titleLabel != null)
                BaGameUiChrome.ApplyHudTitleStyle(_titleLabel, _metrics.Scale);

            if (_toggleButtonRect != null)
            {
                _toggleButtonRect.anchoredPosition = new Vector2(0f, _metrics.ButtonTopY);
                _toggleButtonRect.sizeDelta = new Vector2(_metrics.FullButtonWidth, BaGameUiChrome.HudButtonHeight);
            }

            if (_toggleLabel != null)
                _toggleLabel.fontSize = BaGameUiChrome.HudButtonFontSize * _metrics.Scale;
        }

        internal static void RefreshVisual()
        {
            if (_titleLabel != null)
                _titleLabel.text = ModUiText.PanelTitle;

            var mainOpen = AutoShoppingPanel.IsVisible;
            if (_toggleLabel != null)
                _toggleLabel.text = mainOpen ? ModUiText.BtnHide : ModUiText.BtnShow;
        }

        private static void OnToggleClicked()
        {
            if (!GameState.ShouldShowStoreShoppingUi())
                return;

            if (AutoShoppingPanel.IsVisible)
                AutoShoppingPanel.Hide();
            else
                AutoShoppingPanel.Show();

            _lastMainOpen = AutoShoppingPanel.IsVisible;
            RefreshVisual();
        }

        internal static void Destroy()
        {
            if (_root == null)
                return;

            Object.Destroy(_root);
            _root = null;
            _panelRect = null;
            _headerRect = null;
            _titleRect = null;
            _toggleButtonRect = null;
            _titleLabel = null;
            _toggleButtonImage = null;
            _toggleLabel = null;
            _forceApply = true;
            _lastActive = false;
            _lastMainOpen = false;
        }

        private static void DestroyLegacyRoots()
        {
            foreach (var legacyName in new[]
                     {
                         "AutoShopping_ToggleHud",
                         "AutoShopping_ToggleHud_v0119",
                         "AutoShopping_ToggleHud_v0120",
                         "AutoShopping_ToggleHud_v0121",
                         "AutoShopping_ToggleHud_v0122",
                         "AutoShopping_ToggleHud_v0123",
                         "AutoShopping_ToggleHud_v0124",
                         "AutoShopping_ToggleHud_v0125",
                         "AutoShopping_ToggleHud_v0126"
                     })
            {
                var legacy = GameObject.Find(legacyName);
                if (legacy != null)
                    Object.Destroy(legacy);
            }
        }
    }
}
