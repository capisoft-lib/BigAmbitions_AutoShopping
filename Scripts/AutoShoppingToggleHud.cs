using Capisoft.Lib.BaUnifiedUI.Chrome;
using Capisoft.Lib.BaUnifiedUI.Controls;
using Capisoft.Lib.BaUnifiedUI.Core;
using Capisoft.Lib.BaUnifiedUI.Fluent;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace AutoShopping
{
    /// <summary>Top-right lateral panel — visual parity with VoogleRoute RouteActionPanel.</summary>
    internal static class AutoShoppingToggleHud
    {
        private const string RootName = "AutoShopping_ToggleHud_v0137";
        private const string DragPositionId = "AutoShopping.ToggleHud";

        private static GameObject _root;
        private static RectTransform _panelRect;
        private static BaUiDragState _dragState;
        private static TextMeshProUGUI _titleLabel;
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

            BaUi.EnsureReady();
            var metrics = BaUi.Layout.CreateMetrics(1f);
            var built = BaUi
                .Overlay(RootName, 9004)
                .Panel(BaPanelRecipe.ActionPanel, metrics.PanelWidth, height: metrics.PanelHeight)
                .Draggable(DragPositionId)
                .Header(header => header.TitleLeft(ModUiText.PanelTitle, upperCase: true))
                .SkipBody()
                .AfterPanelChildren(panel =>
                    BaUiControls.CreatePanelTopActionButton(
                        panel.Panel,
                        "Toggle",
                        new Vector2(0f, metrics.ButtonTopY),
                        metrics.ContentWidth,
                        metrics.ButtonHeight,
                        metrics.Scale,
                        BaButtonStyle.Blue,
                        OnToggleClicked,
                        out _,
                        out _toggleLabel))
                .Build();

            _root = built.Root;
            _panelRect = built.Panel;
            _dragState = built.Drag;
            BaUiWidePanelChrome.ConfigureBottomLeftHudAnchor(_panelRect);
            var titleTransform = built.Header.Find("Title");
            _titleLabel = titleTransform != null
                ? titleTransform.GetComponent<TextMeshProUGUI>()
                : null;
            if (_titleLabel != null)
                _titleLabel.overflowMode = TextOverflowModes.Ellipsis;
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
            var panelHeight = BaUi.Layout.CreateMetrics(1f).PanelHeight;
            if (_dragState == null || (!_dragState.HasSavedPosition && !_dragState.IsDragging))
                _panelRect.anchoredPosition = BuildingHudLayout.GetToggleHudPosition(panelWidth, panelHeight);
        }

        internal static void RefreshVisual()
        {
            if (_titleLabel != null)
                _titleLabel.text = ModUiText.PanelTitle;

            var mainOpen = AutoShoppingPanel.IsVisible;
            if (_toggleLabel != null)
            {
                var label = mainOpen ? ModUiText.BtnHide : ModUiText.BtnShow;
                _toggleLabel.text = AutoShoppingShortcuts.AddToggleButtonHint(label);
            }
        }

        private static void OnToggleClicked()
        {
            TryTogglePanel();
        }

        internal static void TryInvokeToggleShortcut()
        {
            if (AutoShoppingPanel.IsSearchFocused)
                return;

            TryTogglePanel();
        }

        private static void TryTogglePanel()
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
            _dragState = null;
            _titleLabel = null;
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
                         "AutoShopping_ToggleHud_v0134",
                         "AutoShopping_ToggleHud_v0135",
                         "AutoShopping_ToggleHud_v0136"
                     })
            {
                var legacy = GameObject.Find(legacyName);
                if (legacy != null)
                    Object.Destroy(legacy);
            }
        }
    }
}

