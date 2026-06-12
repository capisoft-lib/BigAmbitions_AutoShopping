using System.Collections.Generic;
using Helpers;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace AutoShopping
{
    internal static class AutoShoppingPanel
    {
        private const string RootName = "AutoShopping_Panel_v0134";
        private const float StatusFontSize = 15f;
        private const float RowHeight = 50f;
        private const float IconColumnWidth = 48f;
        private const float WayToColumnWidth = 74f;
        private const float PriceColumnWidth = 84f;
        private const float QtyColumnWidth = 118f;
        private const float RightColumnsWidth = WayToColumnWidth + PriceColumnWidth + QtyColumnWidth + 4f;
        private const float CartBarHeight = 36f;
        private const float SearchBarTopMargin = 8f;
        private const float SearchBarHeight = 28f;
        private const float ListHeaderHeight = 24f;
        private const float VisibleRowCount = 5f;
        private const float ScrollContentPadding = 12f;
        private const float FixedScrollHeight = RowHeight * VisibleRowCount + ScrollContentPadding;
        private static readonly float FixedPanelHeight =
            BaGameUiChrome.HeaderBlockHeight
            + CartBarHeight
            + SearchBarTopMargin
            + SearchBarHeight
            + ListHeaderHeight
            + FixedScrollHeight
            + BaGameUiChrome.FooterHeight;

        private static float ListBlockTopOffset =>
            BaGameUiChrome.HeaderBlockHeight
            + CartBarHeight
            + SearchBarTopMargin
            + SearchBarHeight
            + ListHeaderHeight;

        private enum SortColumn
        {
            None,
            Item,
            Price
        }

        private enum SortDirection
        {
            None,
            Ascending,
            Descending
        }

        private static GameObject _root;
        private static RectTransform _panelRect;
        private static RectTransform _headerRect;
        private static RectTransform _closeButtonRect;
        private static RectTransform _titleRect;
        private static TextMeshProUGUI _titleLabel;
        private static TextMeshProUGUI _statusLabel;
        private static TextMeshProUGUI _colItemLabel;
        private static TextMeshProUGUI _colPriceLabel;
        private static TextMeshProUGUI _colQtyLabel;
        private static TextMeshProUGUI _cartStateLabel;
        private static TextMeshProUGUI _pickCartButtonLabel;
        private static TextMeshProUGUI _dropCartButtonLabel;
        private static TextMeshProUGUI _slotsLabel;
        private static TextMeshProUGUI _totalPrefixLabel;
        private static TextMeshProUGUI _totalValueLabel;
        private static TextMeshProUGUI _balanceLabel;
        private static RectTransform _statusRect;
        private static RectTransform _scrollContent;
        private static TMP_InputField _searchField;
        private static TextMeshProUGUI _searchPlaceholderLabel;
        private static readonly List<ProductRowUi> _rows = new List<ProductRowUi>();
        private static string _statusText = string.Empty;
        private static string _searchQuery = string.Empty;
        private static string _lastDisplayState = string.Empty;
        private static SortColumn _sortColumn = SortColumn.None;
        private static SortDirection _sortDirection = SortDirection.None;
        private static ShoppingActionQueue _queue;
        private static int _lastRowCount = -1;
        private static bool _restoreAfterUnblock;
        private static float _lastLayoutHeight = float.NaN;
        private static float _lastLayoutWidth = float.NaN;
        private static bool _legacyCleaned;

        private sealed class ProductRowUi
        {
            internal CatalogProduct Product;
            internal RectTransform Root;
            internal Image Icon;
            internal TextMeshProUGUI NameLabel;
            internal TextMeshProUGUI PriceLabel;
            internal TextMeshProUGUI QtyLabel;
            internal Button BtnWayTo;
            internal Image BtnWayToImage;
            internal Button BtnMinus;
            internal Button BtnPlus;
        }

        internal static void BindQueue(ShoppingActionQueue queue) => _queue = queue;

        internal static void EnsureCreated()
        {
            using var scope = ModPerf.Measure("panel.ensure_created");
            if (_root != null && _root.name != RootName)
                Destroy();

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
            BaGameUiChrome.SetupOverlayCanvas(_root, 9005, interactive: true);

            _panelRect = BaGameUiChrome.BuildPanel(
                _root.transform,
                BuildingHudLayout.GetMainPanelWidth(),
                FixedPanelHeight,
                "AutoShoppingPanel",
                out var header);
            _headerRect = header;
            BaGameUiChrome.ConfigureBottomLeftHudAnchor(_panelRect);

            _closeButtonRect = BaGameUiChrome.CreateHeaderCloseButton(header, Hide).GetComponent<RectTransform>();

            var titleGo = new GameObject("Title", typeof(RectTransform));
            titleGo.transform.SetParent(header, false);
            _titleRect = titleGo.GetComponent<RectTransform>();
            _titleLabel = titleGo.AddComponent<TextMeshProUGUI>();
            BaGameUiChrome.ApplyTitleStyle(_titleLabel, 1f);
            _titleLabel.overflowMode = TextOverflowModes.Ellipsis;
            ApplyHeaderTitleLayout(1f);
            BuildCartBar();
            BuildSearchBar();
            BuildListColumnHeaders();
            BuildScrollArea();
            BuildFooter();
            BaGameUiChrome.ApplyUiLayer(_root);
            ApplyLayoutIfNeeded(force: true);

            _root.SetActive(false);
        }

        private static void BuildCartBar()
        {
            var barGo = new GameObject("CartBar", typeof(RectTransform), typeof(HorizontalLayoutGroup));
            barGo.transform.SetParent(_panelRect, false);
            var barRect = barGo.GetComponent<RectTransform>();
            barRect.anchorMin = new Vector2(0f, 1f);
            barRect.anchorMax = new Vector2(1f, 1f);
            barRect.pivot = new Vector2(0.5f, 1f);
            barRect.anchoredPosition = new Vector2(0f, -BaGameUiChrome.HeaderBlockHeight);
            barRect.sizeDelta = new Vector2(-BaGameUiChrome.ContentInset * 2f, CartBarHeight);

            var layout = barGo.GetComponent<HorizontalLayoutGroup>();
            layout.spacing = 8f;
            layout.childAlignment = TextAnchor.MiddleCenter;
            layout.childControlWidth = true;
            layout.childForceExpandWidth = true;
            layout.padding = new RectOffset(2, 2, 2, 2);

            _pickCartButtonLabel = BaGameUiChrome.CreateButton(barGo.transform, ModUiText.BtnPickCart, 10f, 30f, OnPickCartClicked)
                .GetComponentInChildren<TextMeshProUGUI>();

            var stateGo = new GameObject("CartState", typeof(RectTransform), typeof(LayoutElement));
            stateGo.transform.SetParent(barGo.transform, false);
            var stateLayout = stateGo.GetComponent<LayoutElement>();
            stateLayout.minWidth = 100f;
            stateLayout.flexibleWidth = 1f;
            _cartStateLabel = stateGo.AddComponent<TextMeshProUGUI>();
            BaGameUiChrome.ApplyBodyStyle(_cartStateLabel, 0.95f);
            _cartStateLabel.fontStyle = FontStyles.Bold;
            _cartStateLabel.alignment = TextAlignmentOptions.Center;

            _dropCartButtonLabel = BaGameUiChrome.CreateButton(barGo.transform, ModUiText.BtnDropCart, 10f, 30f, OnDropCartClicked)
                .GetComponentInChildren<TextMeshProUGUI>();
        }

        private static void BuildSearchBar()
        {
            var searchGo = new GameObject("SearchBar", typeof(RectTransform));
            searchGo.transform.SetParent(_panelRect, false);
            var searchRect = searchGo.GetComponent<RectTransform>();
            searchRect.anchorMin = new Vector2(0f, 1f);
            searchRect.anchorMax = new Vector2(1f, 1f);
            searchRect.pivot = new Vector2(0.5f, 1f);
            searchRect.anchoredPosition = new Vector2(
                0f,
                -BaGameUiChrome.HeaderBlockHeight - CartBarHeight - SearchBarTopMargin);
            searchRect.sizeDelta = new Vector2(-BaGameUiChrome.ContentInset * 2f, SearchBarHeight);

            var bgGo = new GameObject("Background", typeof(RectTransform), typeof(Image));
            bgGo.transform.SetParent(searchGo.transform, false);
            var bgRect = bgGo.GetComponent<RectTransform>();
            BaGameUiChrome.Stretch(bgRect);
            bgGo.GetComponent<Image>().color = new Color(0f, 0f, 0f, 0.35f);

            var textAreaGo = new GameObject("TextArea", typeof(RectTransform));
            textAreaGo.transform.SetParent(searchGo.transform, false);
            var textAreaRect = textAreaGo.GetComponent<RectTransform>();
            textAreaRect.anchorMin = Vector2.zero;
            textAreaRect.anchorMax = Vector2.one;
            textAreaRect.offsetMin = new Vector2(8f, 4f);
            textAreaRect.offsetMax = new Vector2(-8f, -4f);

            var placeholderGo = new GameObject("Placeholder", typeof(RectTransform));
            placeholderGo.transform.SetParent(textAreaGo.transform, false);
            var placeholderRect = placeholderGo.GetComponent<RectTransform>();
            BaGameUiChrome.Stretch(placeholderRect);
            _searchPlaceholderLabel = placeholderGo.AddComponent<TextMeshProUGUI>();
            BaGameUiChrome.ApplyBodyStyle(_searchPlaceholderLabel, 0.9f, muted: true);
            _searchPlaceholderLabel.fontStyle = FontStyles.Italic;
            _searchPlaceholderLabel.text = ModUiText.SearchPlaceholder;

            var textGo = new GameObject("Text", typeof(RectTransform));
            textGo.transform.SetParent(textAreaGo.transform, false);
            var textRect = textGo.GetComponent<RectTransform>();
            BaGameUiChrome.Stretch(textRect);
            var textLabel = textGo.AddComponent<TextMeshProUGUI>();
            BaGameUiChrome.ApplyBodyStyle(textLabel, 0.9f);
            textLabel.alignment = TextAlignmentOptions.MidlineLeft;

            _searchField = searchGo.AddComponent<TMP_InputField>();
            _searchField.textViewport = textAreaRect;
            _searchField.textComponent = textLabel;
            _searchField.placeholder = _searchPlaceholderLabel;
            _searchField.lineType = TMP_InputField.LineType.SingleLine;
            _searchField.onValueChanged.AddListener(OnSearchChanged);
            _searchField.onSelect.AddListener(_ => OnSearchFieldSelected());

            var guard = searchGo.AddComponent<SearchInputHotkeyGuard>();
            guard.Bind(_searchField);
        }

        private static void OnSearchFieldSelected()
        {
            if (_searchField == null || EventSystem.current == null)
                return;

            EventSystem.current.SetSelectedGameObject(_searchField.gameObject);
        }

        private static void BuildListColumnHeaders()
        {
            var headerGo = new GameObject("ListHeaders", typeof(RectTransform));
            headerGo.transform.SetParent(_panelRect, false);
            var headerRect = headerGo.GetComponent<RectTransform>();
            headerRect.anchorMin = new Vector2(0f, 1f);
            headerRect.anchorMax = new Vector2(1f, 1f);
            headerRect.pivot = new Vector2(0.5f, 1f);
            headerRect.anchoredPosition = new Vector2(0f, -ListBlockTopOffset);
            headerRect.sizeDelta = new Vector2(-BaGameUiChrome.ContentInset * 2f, ListHeaderHeight);

            _colQtyLabel = CreateColumnHeader(
                headerGo.transform,
                ModUiText.ColQty,
                1f,
                0f,
                QtyColumnWidth,
                TextAlignmentOptions.Center);
            _colPriceLabel = CreateSortableColumnHeader(
                headerGo.transform,
                ModUiText.ColPrice,
                1f,
                QtyColumnWidth,
                PriceColumnWidth,
                TextAlignmentOptions.Center,
                () => CycleSort(SortColumn.Price));
            _colItemLabel = CreateSortableColumnHeader(
                headerGo.transform,
                ModUiText.ColItem,
                0f,
                IconColumnWidth,
                0f,
                TextAlignmentOptions.MidlineLeft,
                () => CycleSort(SortColumn.Item));
            UpdateSortHeaderLabels();
        }

        private static TextMeshProUGUI CreateColumnHeader(
            Transform parent,
            string text,
            float anchorX,
            float inset,
            float width,
            TextAlignmentOptions alignment)
        {
            var go = new GameObject("Col_" + text, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var rect = go.GetComponent<RectTransform>();
            if (anchorX < 0.5f)
            {
                rect.anchorMin = new Vector2(0f, 0f);
                rect.anchorMax = new Vector2(1f, 1f);
                rect.offsetMin = new Vector2(inset, 0f);
                rect.offsetMax = new Vector2(-RightColumnsWidth, 0f);
            }
            else
            {
                rect.anchorMin = new Vector2(1f, 0f);
                rect.anchorMax = new Vector2(1f, 1f);
                rect.pivot = new Vector2(1f, 0.5f);
                rect.anchoredPosition = new Vector2(-inset, 0f);
                rect.sizeDelta = new Vector2(width, 0f);
            }

            var label = go.AddComponent<TextMeshProUGUI>();
            BaGameUiChrome.ApplyColumnHeaderStyle(label);
            label.alignment = alignment;
            label.text = text;
            return label;
        }

        private static TextMeshProUGUI CreateSortableColumnHeader(
            Transform parent,
            string text,
            float anchorX,
            float inset,
            float width,
            TextAlignmentOptions alignment,
            UnityEngine.Events.UnityAction onClick)
        {
            var label = CreateColumnHeader(parent, text, anchorX, inset, width, alignment);
            var button = label.gameObject.AddComponent<Button>();
            button.targetGraphic = label;
            var colors = button.colors;
            colors.normalColor = Color.white;
            colors.highlightedColor = new Color(1f, 1f, 1f, 0.85f);
            colors.pressedColor = new Color(0.8f, 0.8f, 0.8f, 0.9f);
            colors.selectedColor = colors.highlightedColor;
            button.colors = colors;
            button.onClick.AddListener(onClick);
            return label;
        }

        private static void BuildScrollArea()
        {
            var scrollGo = new GameObject("Scroll", typeof(RectTransform), typeof(ScrollRect), typeof(Image));
            scrollGo.transform.SetParent(_panelRect, false);
            var scrollRect = scrollGo.GetComponent<RectTransform>();
            scrollRect.anchorMin = Vector2.zero;
            scrollRect.anchorMax = Vector2.one;
            scrollRect.offsetMin = new Vector2(BaGameUiChrome.ContentInset, BaGameUiChrome.FooterHeight + 8f);
            scrollRect.offsetMax = new Vector2(-BaGameUiChrome.ContentInset, -ListBlockTopOffset);
            var scrollImage = scrollGo.GetComponent<Image>();
            scrollImage.color = BaGameUiChrome.ListInsetColor;

            var viewportGo = new GameObject("Viewport", typeof(RectTransform), typeof(Mask), typeof(Image));
            viewportGo.transform.SetParent(scrollGo.transform, false);
            var viewport = viewportGo.GetComponent<RectTransform>();
            BaGameUiChrome.Stretch(viewport);
            var viewportImage = viewportGo.GetComponent<Image>();
            viewportImage.color = new Color(1f, 1f, 1f, 0.02f);

            var contentGo = new GameObject("Content", typeof(RectTransform), typeof(VerticalLayoutGroup), typeof(ContentSizeFitter));
            contentGo.transform.SetParent(viewportGo.transform, false);
            _scrollContent = contentGo.GetComponent<RectTransform>();
            _scrollContent.anchorMin = new Vector2(0f, 1f);
            _scrollContent.anchorMax = new Vector2(1f, 1f);
            _scrollContent.pivot = new Vector2(0.5f, 1f);
            _scrollContent.anchoredPosition = Vector2.zero;
            _scrollContent.sizeDelta = new Vector2(0f, 0f);

            var layout = contentGo.GetComponent<VerticalLayoutGroup>();
            layout.childAlignment = TextAnchor.UpperCenter;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;
            layout.spacing = 0f;
            layout.padding = new RectOffset(0, 0, 2, 2);

            var fitter = contentGo.GetComponent<ContentSizeFitter>();
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            var scroll = scrollGo.GetComponent<ScrollRect>();
            scroll.viewport = viewport;
            scroll.content = _scrollContent;
            scroll.horizontal = false;
            scroll.vertical = true;
            scroll.movementType = ScrollRect.MovementType.Clamped;
        }

        private static void BuildFooter()
        {
            var footer = new GameObject("Footer", typeof(RectTransform));
            footer.transform.SetParent(_panelRect, false);
            var footerRect = footer.GetComponent<RectTransform>();
            footerRect.anchorMin = Vector2.zero;
            footerRect.anchorMax = new Vector2(1f, 0f);
            footerRect.pivot = new Vector2(0.5f, 0f);
            footerRect.anchoredPosition = Vector2.zero;
            footerRect.sizeDelta = new Vector2(0f, BaGameUiChrome.FooterHeight);

            var inset = BaGameUiChrome.ContentInset;
            var padH = new Vector2(inset, inset);

            var dividerGo = new GameObject("SummaryDivider", typeof(RectTransform), typeof(Image));
            dividerGo.transform.SetParent(footer.transform, false);
            var dividerRect = dividerGo.GetComponent<RectTransform>();
            dividerRect.anchorMin = new Vector2(0f, 1f);
            dividerRect.anchorMax = new Vector2(1f, 1f);
            dividerRect.pivot = new Vector2(0.5f, 1f);
            dividerRect.anchoredPosition = Vector2.zero;
            dividerRect.sizeDelta = new Vector2(-inset * 2f, 1f);
            dividerGo.GetComponent<Image>().color = BaGameUiChrome.RowSeparatorColor;

            var metaRowGo = new GameObject("MetaRow", typeof(RectTransform));
            metaRowGo.transform.SetParent(footer.transform, false);
            var metaRowRect = metaRowGo.GetComponent<RectTransform>();
            metaRowRect.anchorMin = new Vector2(0f, 1f);
            metaRowRect.anchorMax = new Vector2(1f, 1f);
            metaRowRect.pivot = new Vector2(0.5f, 1f);
            metaRowRect.anchoredPosition = new Vector2(0f, -6f);
            metaRowRect.sizeDelta = new Vector2(-inset * 2f, 16f);

            _slotsLabel = CreateFooterLabel(metaRowGo.transform, TextAlignmentOptions.MidlineLeft, 0f, 0f, 0.78f, muted: true);
            _balanceLabel = CreateFooterLabel(metaRowGo.transform, TextAlignmentOptions.MidlineRight, 0f, 0f, 0.78f, muted: true);

            var totalRowGo = new GameObject("TotalRow", typeof(RectTransform));
            totalRowGo.transform.SetParent(footer.transform, false);
            var totalRowRect = totalRowGo.GetComponent<RectTransform>();
            totalRowRect.anchorMin = new Vector2(0f, 1f);
            totalRowRect.anchorMax = new Vector2(1f, 1f);
            totalRowRect.pivot = new Vector2(0.5f, 1f);
            totalRowRect.anchoredPosition = new Vector2(0f, -28f);
            totalRowRect.sizeDelta = new Vector2(-inset * 2f, 28f);

            _totalPrefixLabel = CreateFooterLabel(totalRowGo.transform, TextAlignmentOptions.MidlineLeft, 0f, 0f, 1f, muted: false);
            _totalPrefixLabel.text = ModUiText.TotalLabel;
            _totalPrefixLabel.fontStyle = FontStyles.Bold;

            _totalValueLabel = CreateFooterLabel(totalRowGo.transform, TextAlignmentOptions.MidlineRight, 0f, 0f, 1f, muted: false);
            BaGameUiChrome.ApplyTotalValueStyle(_totalValueLabel);

            var actionsRow = new GameObject("ActionsRow", typeof(RectTransform), typeof(HorizontalLayoutGroup));
            actionsRow.transform.SetParent(footer.transform, false);
            var actionsRect = actionsRow.GetComponent<RectTransform>();
            actionsRect.anchorMin = new Vector2(0f, 1f);
            actionsRect.anchorMax = new Vector2(1f, 1f);
            actionsRect.pivot = new Vector2(0.5f, 1f);
            actionsRect.anchoredPosition = new Vector2(0f, -64f);
            actionsRect.sizeDelta = new Vector2(-inset * 2f, BaGameUiChrome.PrimaryButtonHeight);
            var actionsLayout = actionsRow.GetComponent<HorizontalLayoutGroup>();
            actionsLayout.spacing = 8f;
            actionsLayout.childAlignment = TextAnchor.MiddleCenter;
            actionsLayout.childControlWidth = true;
            actionsLayout.childForceExpandWidth = true;
            actionsLayout.padding = new RectOffset((int)padH.x, (int)padH.x, 0, 0);

            BaGameUiChrome.CreateButton(actionsRow.transform, ModUiText.BtnClear, 10f, BaGameUiChrome.PrimaryButtonHeight, OnClearClicked);
            BaGameUiChrome.CreateRedButton(actionsRow.transform, ModUiText.BtnPay, 10f, BaGameUiChrome.PrimaryButtonHeight, OnPayClicked);
            BaGameUiChrome.CreateButton(actionsRow.transform, ModUiText.BtnCancelQueue, 10f, BaGameUiChrome.PrimaryButtonHeight, OnCancelQueueClicked);

            var statusGo = new GameObject("Status", typeof(RectTransform));
            statusGo.transform.SetParent(footer.transform, false);
            _statusRect = statusGo.GetComponent<RectTransform>();
            _statusLabel = statusGo.AddComponent<TextMeshProUGUI>();
            ApplyStatusStyle();
            _statusLabel.overflowMode = TextOverflowModes.Ellipsis;
            ApplyStatusLayout(inset);
        }

        private static void ApplyStatusLayout(float horizontalInset)
        {
            if (_statusRect == null)
                return;

            // Midpoint between Pay row bottom (incl. graphic bleed) and visible frame bottom.
            var payButtonBottom = BaGameUiChrome.FooterStatusZoneHeight - BaGameUiChrome.HudButtonGraphicBleedBottom;
            var frameBottom = BaGameUiChrome.FrameOffsetY;
            var gapTop = payButtonBottom;
            var gapBottom = frameBottom;
            var gapHeight = gapTop - gapBottom;
            var statusCenterY = (gapTop + gapBottom) * 0.5f - BaGameUiChrome.FooterStatusVerticalNudge;

            _statusRect.anchorMin = Vector2.zero;
            _statusRect.anchorMax = new Vector2(1f, 0f);
            _statusRect.pivot = new Vector2(0.5f, 0.5f);
            _statusRect.anchoredPosition = new Vector2(0f, statusCenterY);
            _statusRect.sizeDelta = new Vector2(-horizontalInset * 2f, Mathf.Max(gapHeight, 14f));
        }

        private static TextMeshProUGUI CreateFooterLabel(
            Transform parent,
            TextAlignmentOptions alignment,
            float x,
            float y,
            float scale,
            bool muted)
        {
            var go = new GameObject("Label", typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var rect = go.GetComponent<RectTransform>();
            if (Mathf.Approximately(y, 0f) && (parent.name == "TotalRow" || parent.name == "MetaRow"))
            {
                if (parent.name == "MetaRow")
                {
                    if (alignment == TextAlignmentOptions.MidlineRight)
                    {
                        rect.anchorMin = new Vector2(0.5f, 0f);
                        rect.anchorMax = Vector2.one;
                    }
                    else
                    {
                        rect.anchorMin = Vector2.zero;
                        rect.anchorMax = new Vector2(0.5f, 1f);
                    }
                }
                else
                {
                    rect.anchorMin = alignment == TextAlignmentOptions.MidlineRight
                        ? new Vector2(0.5f, 0f)
                        : Vector2.zero;
                    rect.anchorMax = Vector2.one;
                }

                rect.offsetMin = Vector2.zero;
                rect.offsetMax = Vector2.zero;
            }
            else
            {
                rect.anchorMin = new Vector2(0f, 0f);
                rect.anchorMax = new Vector2(1f, 0f);
                rect.pivot = new Vector2(0.5f, 0f);
                rect.anchoredPosition = new Vector2(x, y);
                rect.sizeDelta = new Vector2(0f, 16f);
            }

            var text = go.AddComponent<TextMeshProUGUI>();
            BaGameUiChrome.ApplyBodyStyle(text, scale, muted);
            text.alignment = alignment;
            return text;
        }

        internal static void Show()
        {
            if (!GameState.ShouldShowStoreShoppingUi())
                return;

            EnsureCreated();
            _root.SetActive(true);
            SetStatus(ModUiText.StatusIdle);
            RefreshAll();
            AutoShoppingToggleHud.RefreshLocalizedText();
        }

        internal static void Hide()
        {
            ReleaseUiFocus();
            if (_root != null)
                _root.SetActive(false);

            AutoShoppingToggleHud.RefreshLocalizedText();
        }

        /// <summary>Closes the panel for checkout and prevents auto-restore when PurchaseUI closes.</summary>
        internal static void CloseForCheckout()
        {
            SuppressRestore();
            Hide();
        }

        internal static void SuppressRestore() => _restoreAfterUnblock = false;

        /// <summary>Hides while vanilla menus (ESC, phone, map, etc.) are open; restores if it was open before.</summary>
        internal static void UpdateVisibility()
        {
            using var scope = ModPerf.Measure("panel.update_visibility");
            if (_root == null)
                return;

            if (!GameState.ShouldShowStoreShoppingUi())
            {
                if (IsVisible)
                {
                    // Checkout is temporary; keep the panel closed after payment completes.
                    if (!GameState.IsCheckoutUiBlocking() && GameState.IsPedestrianInSupportedStore())
                    {
                        _restoreAfterUnblock = true;
                        if (GameState.IsUiBlocking())
                            AutoShoppingDriver.Instance?.ActionQueue?.Cancel();
                    }

                    Hide();
                }

                return;
            }

            if (_restoreAfterUnblock)
            {
                _restoreAfterUnblock = false;
                Show();
            }
        }

        internal static void Toggle()
        {
            if (_root != null && _root.activeSelf)
            {
                Hide();
                return;
            }

            Show();
        }

        internal static bool IsVisible => _root != null && _root.activeSelf;

        internal static void SetStatus(string text)
        {
            _statusText = text ?? string.Empty;
            if (_statusLabel != null)
                _statusLabel.text = _statusText;
        }

        internal static void RefreshLocalizedText()
        {
            _lastDisplayState = string.Empty;
            if (_titleLabel != null)
                _titleLabel.text = ModUiText.FormatPanelTitle(StoreSession.Current?.Profile?.BusinessDisplayName);
            if (_searchPlaceholderLabel != null)
                _searchPlaceholderLabel.text = ModUiText.SearchPlaceholder;
            UpdateSortHeaderLabels();
            if (_colQtyLabel != null)
                _colQtyLabel.text = ModUiText.ColQty;
            if (_totalPrefixLabel != null)
                _totalPrefixLabel.text = ModUiText.TotalLabel;
            RefreshAll();
        }

        internal static void RefreshAll()
        {
            using var scope = ModPerf.Measure("panel.refresh_all");
            if (_root == null || !_root.activeSelf)
                return;

            var session = StoreSession.Current;
            if (session == null)
            {
                Hide();
                return;
            }

            session.SyncContainerFromPlayer();
            session.RefreshPicked();

            if (_titleLabel != null)
                _titleLabel.text = ModUiText.FormatPanelTitle(session.Profile?.BusinessDisplayName);

            if (_statusLabel != null && string.IsNullOrEmpty(_statusText))
                _statusLabel.text = ModUiText.StatusIdle;

            UpdateCartBar();
            UpdateFooterLabels(session);
            AutoShoppingToggleHud.RefreshVisual();

            var displayState = BuildDisplayState(session);
            if (_lastDisplayState != displayState)
            {
                _lastDisplayState = displayState;
                RebuildProductRows(session, GetDisplayProducts(session));
            }
            else
            {
                UpdateRowQuantities(session);
            }

            RefreshWayToButtons();
        }

        internal static void RefreshFooterOnly()
        {
            using var scope = ModPerf.Measure("panel.refresh_footer");
            if (_root == null || !_root.activeSelf)
                return;

            var session = StoreSession.Current;
            if (session == null)
                return;

            session.RefreshPicked();
            UpdateFooterLabels(session);
            UpdateRowQuantities(session);
            ApplyLayoutIfNeeded();
        }

        /// <summary>Lightweight refresh while the action queue is running (skip chrome/layout).</summary>
        internal static void RefreshAfterQueueAction()
        {
            using var scope = ModPerf.Measure("panel.refresh_queue_action");
            if (_root == null || !_root.activeSelf)
                return;

            var session = StoreSession.Current;
            if (session == null)
                return;

            session.RefreshPicked();
            UpdateCartBar();
            UpdateFooterLabels(session);
            UpdateRowQuantities(session);
        }

        private static void ApplyLayoutIfNeeded(bool force = false)
        {
            using var scope = ModPerf.Measure("panel.apply_layout");
            if (_panelRect == null || _headerRect == null)
                return;

            var panelPos = BuildingHudLayout.GetMainPanelPosition(FixedPanelHeight);
            var panelBottom = panelPos.y;
            var availableHeight = Screen.height - BuildingHudLayout.MainPanelTopScreenMargin - panelBottom;
            var panelHeight = Mathf.Clamp(
                FixedPanelHeight,
                BaGameUiChrome.MinPanelHeight,
                Mathf.Max(BaGameUiChrome.MinPanelHeight, availableHeight));
            var panelWidth = BuildingHudLayout.GetMainPanelWidth();

            if (!force
                && Mathf.Approximately(panelHeight, _lastLayoutHeight)
                && Mathf.Approximately(panelWidth, _lastLayoutWidth))
            {
                return;
            }

            _lastLayoutHeight = panelHeight;
            _lastLayoutWidth = panelWidth;

            _panelRect.sizeDelta = new Vector2(panelWidth, panelHeight);
            _panelRect.anchoredPosition = new Vector2(panelPos.x, panelBottom);

            var scale = BaGameUiChrome.GetScale(panelWidth);
            BaGameUiChrome.UpdatePanelFrames(_panelRect, _headerRect, panelWidth);
            BaGameUiChrome.ApplyHeaderCloseButtonLayout(_closeButtonRect, scale);
            ApplyHeaderTitleLayout(scale);
            ApplyStatusStyle();
        }

        private static void ApplyHeaderTitleLayout(float scale)
        {
            if (_titleRect == null || _titleLabel == null)
                return;

            var titleScale = Mathf.Clamp(scale, 0.85f, 1.15f);
            var padY = BaGameUiChrome.HeaderTextPaddingY * titleScale;
            var closeReserve = BaGameUiChrome.HeaderCloseButtonSize + 14f * titleScale;

            _titleRect.anchorMin = Vector2.zero;
            _titleRect.anchorMax = Vector2.one;
            BaGameUiChrome.ApplyHeaderTitleInsets(_titleRect, titleScale);
            _titleRect.offsetMax = new Vector2(-closeReserve, -padY);

            var storeName = StoreSession.Current?.Profile?.BusinessDisplayName;
            _titleLabel.text = ModUiText.FormatPanelTitle(storeName);
            BaGameUiChrome.ApplyTitleStyle(_titleLabel, titleScale);
        }

        private static void ApplyStatusStyle()
        {
            if (_statusLabel == null)
                return;

            BaGameUiChrome.ApplyBodyStyle(_statusLabel, StatusFontSize / 14f, muted: true);
            _statusLabel.alignment = TextAlignmentOptions.Center;
        }

        private static void OnSearchChanged(string value)
        {
            _searchQuery = value ?? string.Empty;
            RefreshProductList();
        }

        private static void CycleSort(SortColumn column)
        {
            ReleaseUiFocus();
            if (_sortColumn != column)
            {
                _sortColumn = column;
                _sortDirection = SortDirection.Ascending;
            }
            else if (_sortDirection == SortDirection.Ascending)
            {
                _sortDirection = SortDirection.Descending;
            }
            else
            {
                _sortColumn = SortColumn.None;
                _sortDirection = SortDirection.None;
            }

            UpdateSortHeaderLabels();
            RefreshProductList();
        }

        private static void UpdateSortHeaderLabels()
        {
            if (_colItemLabel != null)
            {
                _colItemLabel.text = FormatSortHeader(
                    ModUiText.ColItem,
                    _sortColumn == SortColumn.Item ? _sortDirection : SortDirection.None);
            }

            if (_colPriceLabel != null)
            {
                _colPriceLabel.text = FormatSortHeader(
                    ModUiText.ColPrice,
                    _sortColumn == SortColumn.Price ? _sortDirection : SortDirection.None);
            }
        }

        private static string FormatSortHeader(string baseLabel, SortDirection direction) =>
            direction switch
            {
                SortDirection.Ascending => baseLabel + " \u25b2",
                SortDirection.Descending => baseLabel + " \u25bc",
                _ => baseLabel
            };

        private static void RefreshProductList()
        {
            if (_root == null || !_root.activeSelf)
                return;

            var session = StoreSession.Current;
            if (session == null)
                return;

            _lastDisplayState = string.Empty;
            RefreshAll();
        }

        private static string BuildDisplayState(StoreSession session) =>
            (_searchQuery ?? string.Empty) + "|"
            + _sortColumn + "|"
            + _sortDirection + "|"
            + session.Products.Count;

        private static List<CatalogProduct> GetDisplayProducts(StoreSession session)
        {
            var list = new List<CatalogProduct>(session.Products);
            var query = _searchQuery?.Trim();
            if (!string.IsNullOrEmpty(query))
            {
                list.RemoveAll(product =>
                    product.DisplayName.IndexOf(query, System.StringComparison.OrdinalIgnoreCase) < 0);
            }

            if (_sortColumn != SortColumn.None && _sortDirection != SortDirection.None)
            {
                list.Sort(CompareForSort);
                if (_sortDirection == SortDirection.Descending)
                    list.Reverse();
            }

            return list;
        }

        private static int CompareForSort(CatalogProduct a, CatalogProduct b)
        {
            if (_sortColumn == SortColumn.Price)
            {
                var priceCompare = a.LinePrice.CompareTo(b.LinePrice);
                if (priceCompare != 0)
                    return priceCompare;

                return string.Compare(a.DisplayName, b.DisplayName, System.StringComparison.OrdinalIgnoreCase);
            }

            return string.Compare(a.DisplayName, b.DisplayName, System.StringComparison.OrdinalIgnoreCase);
        }

        private static void UpdateFooterLabels(StoreSession session)
        {
            if (_slotsLabel != null)
                _slotsLabel.text = ModUiText.FormatSlots(session.GetCurrentSlots(), session.GetAvailableSpace());

            var total = session.GetDesiredTotal();
            if (_totalValueLabel != null)
                _totalValueLabel.text = ModUiText.FormatTotalValue(total);

            try
            {
                var balance = SaveGameManager.Current?.Money ?? 0f;
                if (_balanceLabel != null)
                    _balanceLabel.text = ModUiText.FormatBalance(balance);
            }
            catch
            {
                if (_balanceLabel != null)
                    _balanceLabel.text = string.Empty;
            }
        }

        private static void UpdateRowQuantities(StoreSession session)
        {
            foreach (var row in _rows)
            {
                if (row?.QtyLabel == null || row.Product == null)
                    continue;

                var product = session.FindProduct(row.Product.ItemName);
                if (product == null)
                    continue;

                row.Product = product;
                row.QtyLabel.text = LocaleFormat.Integer(product.DesiredQuantity);
            }
        }

        private static void RebuildProductRows(StoreSession session, IList<CatalogProduct> displayProducts)
        {
            using var scope = ModPerf.Measure("panel.rebuild_rows");
            foreach (var row in _rows)
            {
                if (row?.Root != null)
                    Object.Destroy(row.Root.gameObject);
            }

            _rows.Clear();

            if (_scrollContent == null)
                return;

            foreach (var product in displayProducts)
                _rows.Add(CreateProductRow(product, session));

            _lastRowCount = displayProducts.Count;
        }

        private static ProductRowUi CreateProductRow(CatalogProduct product, StoreSession session)
        {
            var rowGo = new GameObject("Product_" + product.ItemName, typeof(RectTransform), typeof(LayoutElement));
            rowGo.transform.SetParent(_scrollContent, false);
            var layoutElement = rowGo.GetComponent<LayoutElement>();
            layoutElement.minHeight = RowHeight;
            layoutElement.preferredHeight = RowHeight;

            var rowRect = rowGo.GetComponent<RectTransform>();

            var separatorGo = new GameObject("Separator", typeof(RectTransform), typeof(Image));
            separatorGo.transform.SetParent(rowGo.transform, false);
            var separatorRect = separatorGo.GetComponent<RectTransform>();
            separatorRect.anchorMin = new Vector2(0f, 0f);
            separatorRect.anchorMax = new Vector2(1f, 0f);
            separatorRect.pivot = new Vector2(0.5f, 0f);
            separatorRect.anchoredPosition = Vector2.zero;
            separatorRect.sizeDelta = new Vector2(-8f, 1f);
            separatorGo.GetComponent<Image>().color = BaGameUiChrome.RowSeparatorColor;

            var iconGo = new GameObject("Icon", typeof(RectTransform), typeof(Image));
            iconGo.transform.SetParent(rowGo.transform, false);
            var iconRect = iconGo.GetComponent<RectTransform>();
            iconRect.anchorMin = new Vector2(0f, 0.5f);
            iconRect.anchorMax = new Vector2(0f, 0.5f);
            iconRect.pivot = new Vector2(0f, 0.5f);
            iconRect.anchoredPosition = new Vector2(6f, 0f);
            iconRect.sizeDelta = new Vector2(36f, 36f);
            var icon = iconGo.GetComponent<Image>();
            icon.sprite = product.Icon;
            icon.preserveAspect = true;

            var nameGo = new GameObject("Name", typeof(RectTransform));
            nameGo.transform.SetParent(rowGo.transform, false);
            var nameRect = nameGo.GetComponent<RectTransform>();
            nameRect.anchorMin = Vector2.zero;
            nameRect.anchorMax = Vector2.one;
            nameRect.offsetMin = new Vector2(IconColumnWidth, 0f);
            nameRect.offsetMax = new Vector2(-RightColumnsWidth, 0f);
            var nameLabel = nameGo.AddComponent<TextMeshProUGUI>();
            BaGameUiChrome.ApplyBodyStyle(nameLabel, 0.95f);
            nameLabel.alignment = TextAlignmentOptions.MidlineLeft;
            nameLabel.text = product.DisplayName;

            var wayToGo = new GameObject("WayTo", typeof(RectTransform));
            wayToGo.transform.SetParent(rowGo.transform, false);
            var wayToRect = wayToGo.GetComponent<RectTransform>();
            wayToRect.anchorMin = new Vector2(1f, 0.5f);
            wayToRect.anchorMax = new Vector2(1f, 0.5f);
            wayToRect.pivot = new Vector2(1f, 0.5f);
            wayToRect.anchoredPosition = new Vector2(-(QtyColumnWidth + PriceColumnWidth + 2f), 0f);
            wayToRect.sizeDelta = new Vector2(WayToColumnWidth, 28f);
            var btnWayTo = BaGameUiChrome.CreateVanillaButton(
                wayToGo.transform,
                ModUiText.BtnWayTo,
                WayToColumnWidth,
                28f,
                1f,
                null,
                VanillaButtonStyle.Blue,
                fontSize: 11f,
                bleedBottom: false);
            ConfigureSingleLineButtonLabel(btnWayTo);
            StretchRect(btnWayTo.GetComponent<RectTransform>());
            var btnWayToImage = BaGameUiChrome.GetVanillaButtonImage(btnWayTo);

            var priceGo = new GameObject("Price", typeof(RectTransform));
            priceGo.transform.SetParent(rowGo.transform, false);
            var priceRect = priceGo.GetComponent<RectTransform>();
            priceRect.anchorMin = new Vector2(1f, 0f);
            priceRect.anchorMax = new Vector2(1f, 1f);
            priceRect.pivot = new Vector2(1f, 0.5f);
            priceRect.anchoredPosition = new Vector2(-QtyColumnWidth, 0f);
            priceRect.sizeDelta = new Vector2(PriceColumnWidth, 0f);
            var priceLabel = priceGo.AddComponent<TextMeshProUGUI>();
            BaGameUiChrome.ApplyBodyStyle(priceLabel, 0.92f);
            priceLabel.alignment = TextAlignmentOptions.Center;
            priceLabel.text = product.InStock
                ? LocaleFormat.Money(product.LinePrice)
                : ModUiText.OutOfStock;
            if (!product.InStock)
                priceLabel.color = BaGameUiChrome.WarningTextColor;

            var qtyGo = new GameObject("Qty", typeof(RectTransform));
            qtyGo.transform.SetParent(rowGo.transform, false);
            var qtyRect = qtyGo.GetComponent<RectTransform>();
            qtyRect.anchorMin = new Vector2(1f, 0.5f);
            qtyRect.anchorMax = new Vector2(1f, 0.5f);
            qtyRect.pivot = new Vector2(1f, 0.5f);
            qtyRect.anchoredPosition = new Vector2(-4f, 0f);
            qtyRect.sizeDelta = new Vector2(QtyColumnWidth - 8f, 30f);

            var btnMinus = BaGameUiChrome.CreateQtyButton(qtyGo.transform, "-", 30f, 28f, null);
            var minusRect = btnMinus.GetComponent<RectTransform>();
            minusRect.anchorMin = new Vector2(0f, 0.5f);
            minusRect.anchorMax = new Vector2(0f, 0.5f);
            minusRect.pivot = new Vector2(0f, 0.5f);
            minusRect.anchoredPosition = Vector2.zero;

            var qtyLabelGo = new GameObject("Value", typeof(RectTransform));
            qtyLabelGo.transform.SetParent(qtyGo.transform, false);
            var qtyLabelRect = qtyLabelGo.GetComponent<RectTransform>();
            qtyLabelRect.anchorMin = new Vector2(0.5f, 0.5f);
            qtyLabelRect.anchorMax = new Vector2(0.5f, 0.5f);
            qtyLabelRect.sizeDelta = new Vector2(28f, 24f);
            var qtyLabel = qtyLabelGo.AddComponent<TextMeshProUGUI>();
            BaGameUiChrome.ApplyBodyStyle(qtyLabel, 0.95f);
            qtyLabel.alignment = TextAlignmentOptions.Center;
            qtyLabel.text = LocaleFormat.Integer(product.DesiredQuantity);

            var btnPlus = BaGameUiChrome.CreateQtyButton(qtyGo.transform, "+", 30f, 28f, null);
            var plusRect = btnPlus.GetComponent<RectTransform>();
            plusRect.anchorMin = new Vector2(1f, 0.5f);
            plusRect.anchorMax = new Vector2(1f, 0.5f);
            plusRect.pivot = new Vector2(1f, 0.5f);
            plusRect.anchoredPosition = Vector2.zero;

            var capturedItemName = product.ItemName;
            btnWayTo.onClick.AddListener(() => OnWayTo(capturedItemName));
            btnMinus.onClick.AddListener(() => OnDecrease(capturedItemName));
            btnPlus.onClick.AddListener(() => OnIncrease(capturedItemName));

            return new ProductRowUi
            {
                Product = product,
                Root = rowRect,
                Icon = icon,
                NameLabel = nameLabel,
                PriceLabel = priceLabel,
                QtyLabel = qtyLabel,
                BtnWayTo = btnWayTo,
                BtnWayToImage = btnWayToImage,
                BtnMinus = btnMinus,
                BtnPlus = btnPlus
            };
        }

        private static void StretchRect(RectTransform rect)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }

        private static void ConfigureSingleLineButtonLabel(Button button, float horizontalPadding = 3f)
        {
            var label = button.GetComponentInChildren<TextMeshProUGUI>(true);
            if (label == null)
                return;

            label.enableWordWrapping = false;
            label.overflowMode = TextOverflowModes.Overflow;
            var labelRect = label.rectTransform;
            labelRect.offsetMin = new Vector2(horizontalPadding, 0f);
            labelRect.offsetMax = new Vector2(-horizontalPadding, 0f);
        }

        private static void OnWayTo(string itemName)
        {
            ReleaseUiFocus();
            var session = StoreSession.Current;
            var product = session?.FindProduct(itemName);
            if (product == null)
                return;

            if (StoreItemRouteService.IsActiveItem(itemName))
            {
                StoreItemRouteService.Clear();
                SetStatus(ModUiText.StatusIdle);
                RefreshWayToButtons();
                return;
            }

            if (!StoreItemRouteService.TrySetRouteToItem(product.ItemName, out var error))
            {
                SetStatus(error ?? ModUiText.ErrorUnreachable);
                return;
            }

            SetStatus(ModUiText.FormatStatusWayTo(product.DisplayName));
            RefreshWayToButtons();
        }

        private static void RefreshWayToButtons()
        {
            var activeItem = StoreItemRouteService.ActiveItemName;
            foreach (var row in _rows)
            {
                if (row?.Product == null)
                    continue;

                var image = row.BtnWayToImage;
                if (image == null && row.BtnWayTo != null)
                    image = row.BtnWayToImage = BaGameUiChrome.GetVanillaButtonImage(row.BtnWayTo);
                if (image == null)
                    continue;

                var selected = !string.IsNullOrEmpty(activeItem) &&
                               row.Product.ItemName == activeItem;
                BaGameUiChrome.ApplyVanillaButtonImageStyle(
                    image,
                    selected ? VanillaButtonStyle.Green : VanillaButtonStyle.Blue);
            }
        }

        private static void OnIncrease(string itemName)
        {
            ReleaseUiFocus();
            var session = StoreSession.Current;
            var product = session?.FindProduct(itemName);
            if (session == null || product == null)
                return;

            if (!product.InStock)
                return;

            if (!session.CanIncreaseDesired())
            {
                SetStatus(ModUiText.FormatErrorFull(session.GetMaxSlots()));
                ModLog.Info("Increase blocked: " + product.ItemName +
                            " slots=" + session.GetCurrentSlots() + "/" + session.GetMaxSlots());
                return;
            }

            product.DesiredQuantity++;
            ModLog.Info("Desired +" + product.ItemName + " -> " + product.DesiredQuantity);
            _queue?.RequestFulfillment(session);
            RefreshAll();
        }

        private static void OnDecrease(string itemName)
        {
            ReleaseUiFocus();
            var session = StoreSession.Current;
            var product = session?.FindProduct(itemName);
            if (session == null || product == null)
                return;

            if (product.DesiredQuantity <= 0)
                return;

            product.DesiredQuantity--;
            _queue?.RequestFulfillment(session);
            RefreshAll();
        }

        private static void UpdateCartBar()
        {
            var session = StoreSession.Current;
            if (_pickCartButtonLabel != null)
                _pickCartButtonLabel.text = ModUiText.GetPickContainerButtonLabel(session?.Profile);
            if (_dropCartButtonLabel != null)
                _dropCartButtonLabel.text = ModUiText.GetDropContainerButtonLabel();
            if (_cartStateLabel != null)
                _cartStateLabel.text = ShoppingCargoHelper.GetActiveCartStateLabel();
        }

        private static void OnPickCartClicked()
        {
            ReleaseUiFocus();
            var session = StoreSession.Current;
            if (session == null || _queue == null)
                return;

            _queue.RequestPickCart(session);
            RefreshAll();
        }

        private static void OnDropCartClicked()
        {
            ReleaseUiFocus();
            _queue?.RequestReleaseContainer();
            RefreshAll();
        }

        private static void OnPayClicked()
        {
            ReleaseUiFocus();
            if (_queue == null)
                return;

            var holder = ShoppingCargoHelper.GetActiveHolder();
            if (ShoppingCargoHelper.GetUnpaidCargo(holder).Count == 0)
            {
                SetStatus(ModUiText.ErrorNothingToPay);
                ModLog.Warn("Pay blocked: no unpaid cargo (holder=" + (holder == null ? "null" : holder.GetType().Name) + ")");
                return;
            }

            ModLog.Info("Pay queued");
            _queue.PrependPay();
        }

        private static void OnClearClicked()
        {
            ReleaseUiFocus();
            var session = StoreSession.Current;
            if (session == null || _queue == null)
                return;

            foreach (var product in session.Products)
                product.DesiredQuantity = 0;

            _queue.RequestFulfillment(session);
            RefreshAll();
        }

        private static void OnCancelQueueClicked()
        {
            ReleaseUiFocus();
            _queue?.Cancel();
        }

        internal static void ReleaseUiFocus()
        {
            if (_searchField != null)
                _searchField.DeactivateInputField();

            if (EventSystem.current != null)
                EventSystem.current.SetSelectedGameObject(null);
        }

        private static void DestroyLegacyRoots()
        {
            foreach (var legacyName in new[]
                     {
                         "AutoShopping_Panel",
                         "AutoShopping_Panel_v0114",
                         "AutoShopping_Panel_v0115",
                         "AutoShopping_Panel_v0116",
                         "AutoShopping_Panel_v0117",
                         "AutoShopping_Panel_v0118",
                         "AutoShopping_Panel_v0119",
                         "AutoShopping_Panel_v0120",
                         "AutoShopping_Panel_v0121",
                         "AutoShopping_Panel_v0122",
                         "AutoShopping_Panel_v0123",
                         "AutoShopping_Panel_v0124",
                         "AutoShopping_Panel_v0125",
                         "AutoShopping_Panel_v0126",
                         "AutoShopping_Panel_v0127",
                         "AutoShopping_Panel_v0128",
                         "AutoShopping_Panel_v0129",
                         "AutoShopping_Panel_v0130",
                         "AutoShopping_Panel_v0131",
                         "AutoShopping_Panel_v0132",
                         "AutoShopping_Panel_v0133"
                     })
            {
                var legacy = GameObject.Find(legacyName);
                if (legacy != null)
                    Object.Destroy(legacy);
            }
        }

        internal static void Destroy()
        {
            foreach (var row in _rows)
            {
                if (row?.Root != null)
                    Object.Destroy(row.Root.gameObject);
            }

            _rows.Clear();

            if (_root == null)
                return;

            Object.Destroy(_root);
            _root = null;
            _panelRect = null;
            _headerRect = null;
            _closeButtonRect = null;
            _titleRect = null;
            _scrollContent = null;
            _searchField = null;
            _searchPlaceholderLabel = null;
            _searchQuery = string.Empty;
            _lastDisplayState = string.Empty;
            _sortColumn = SortColumn.None;
            _sortDirection = SortDirection.None;
            _lastLayoutWidth = float.NaN;
        }

        internal static bool IsSearchFocused => _searchField != null && _searchField.isFocused;
    }
}
