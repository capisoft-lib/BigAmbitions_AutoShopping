using Capisoft.Lib.BaUnifiedUI.Chrome;
using Capisoft.Lib.BaUnifiedUI.Core;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace AutoShopping
{
    /// <summary>Top-right lateral panel — visual parity with VoogleRoute RouteActionPanel.</summary>
    internal static class AutoShoppingToggleHud
    {
        private const string RootName = "AutoShopping_ToggleHud_v0135";

        private static GameObject _root;
        private static RectTransform _panelRect;
        private static RectTransform _headerRect;
        private static RectTransform _titleRect;
        private static RectTransform _toggleButtonRect;
        private static BaUiWidePanelChrome.HudPanelMetrics _metrics;
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

            BaUiWidePanelChrome.EnsureInitialized();
            _root = new GameObject(RootName);
            Object.DontDestroyOnLoad(_root);
            BaUiWidePanelChrome.SetupOverlayCanvas(_root, 9004, interactive: true);

            _metrics = new BaUiWidePanelChrome.HudPanelMetrics(1f);
            _panelRect = BaUiWidePanelChrome.BuildToggleHudPanel(_root.transform, out var header, out _);
            _headerRect = header;
            BaUiWidePanelChrome.ConfigureBottomLeftHudAnchor(_panelRect);

            var titleGo = new GameObject("Title", typeof(RectTransform));
            titleGo.transform.SetParent(header, false);
            _titleRect = titleGo.GetComponent<RectTransform>();
            _titleRect.anchorMin = Vector2.zero;
            _titleRect.anchorMax = Vector2.one;
            _titleLabel = titleGo.AddComponent<TextMeshProUGUI>();
            BaUiWidePanelChrome.ApplyHudTitleStyle(_titleLabel, _metrics.Scale);
            _titleLabel.text = ModUiText.PanelTitle;
            _titleLabel.overflowMode = TextOverflowModes.Ellipsis;

            var toggleButton = BaUiWidePanelChrome.CreateHudActionButton(
                _panelRect,
                ModUiText.BtnShow,
                _metrics.FullButtonWidth,
                BaUiWidePanelChrome.HudButtonHeight,
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

            var panelWidth = BuildingHudLayout.GetToggleHudWidth();
            var scale = panelWidth / BaUiWidePanelChrome.RefPanelWidth;
            var buttonWidth = panelWidth - BaUiWidePanelChrome.ToggleHudButtonMarginX * 2f;
            var panelHeight = BaUiWidePanelChrome.HeaderBlockHeight
                              + BaUiWidePanelChrome.HudBodyTopPadding
                              + BaUiWidePanelChrome.ToggleHudButtonHeight
                              + BaUiWidePanelChrome.HudBodyBottomPadding;
            var buttonTopY = -(BaUiWidePanelChrome.HeaderBlockHeight + BaUiWidePanelChrome.HudBodyTopPadding)
                             + BaUiWidePanelChrome.ToggleHudButtonLift;

            _panelRect.sizeDelta = new Vector2(panelWidth, panelHeight);
            _panelRect.anchoredPosition = BuildingHudLayout.GetToggleHudPosition(panelWidth, panelHeight);

            if (_headerRect != null)
                BaUiWidePanelChrome.UpdateToggleHudFrames(_panelRect, _headerRect, panelWidth);

            if (_titleRect != null)
                BaUiWidePanelChrome.ApplyHeaderTitleInsets(_titleRect, scale);

            if (_titleLabel != null)
                BaUiWidePanelChrome.ApplyHudTitleStyle(_titleLabel, scale);

            if (_toggleButtonRect != null)
            {
                _toggleButtonRect.anchoredPosition = new Vector2(0f, buttonTopY);
                _toggleButtonRect.sizeDelta = new Vector2(buttonWidth, BaUiWidePanelChrome.ToggleHudButtonHeight);
            }

            if (_toggleLabel != null)
                _toggleLabel.fontSize = BaUiWidePanelChrome.HudButtonFontSize * scale;
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
                         "AutoShopping_ToggleHud_v0126",
                         "AutoShopping_ToggleHud_v0127",
                         "AutoShopping_ToggleHud_v0128",
                         "AutoShopping_ToggleHud_v0129",
                         "AutoShopping_ToggleHud_v0130",
                         "AutoShopping_ToggleHud_v0131",
                         "AutoShopping_ToggleHud_v0132",
                         "AutoShopping_ToggleHud_v0133",
                         "AutoShopping_ToggleHud_v0134"
                     })
            {
                var legacy = GameObject.Find(legacyName);
                if (legacy != null)
                    Object.Destroy(legacy);
            }
        }
    }
}

