using System.Collections.Generic;
using Helpers;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace AutoShopping
{
    internal static class AutoShoppingPanel
    {
        private const string RootName = "AutoShopping_Panel_v0126";
        private const float StatusRowHeight = 24f;
        private const float StatusBottomOffset = 34f;
        private const float StatusFontScale = 1.05f;
        private const float RowHeight = 50f;
        private const float IconColumnWidth = 48f;
        private const float PriceColumnWidth = 84f;
        private const float QtyColumnWidth = 118f;
        private const float CartBarHeight = 36f;
        private const float ListHeaderHeight = 24f;
        private const float MinScrollHeight = 200f;

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
        private static RectTransform _scrollContent;
        private static readonly List<ProductRowUi> _rows = new List<ProductRowUi>();
        private static string _statusText = string.Empty;
        private static ShoppingActionQueue _queue;
        private static int _lastRowCount = -1;
        private static bool _restoreAfterUnblock;

        private sealed class ProductRowUi
        {
            internal CatalogProduct Product;
            internal RectTransform Root;
            internal Image Icon;
            internal TextMeshProUGUI NameLabel;
            internal TextMeshProUGUI PriceLabel;
            internal TextMeshProUGUI QtyLabel;
            internal Button BtnMinus;
            internal Button BtnPlus;
        }

        internal static void BindQueue(ShoppingActionQueue queue) => _queue = queue;

        internal static void EnsureCreated()
        {
            if (_root != null && _root.name != RootName)
                Destroy();

            DestroyLegacyRoots();

            if (_root != null)
                return;

            BaGameUiChrome.EnsureInitialized();
            _root = new GameObject(RootName);
            Object.DontDestroyOnLoad(_root);
            BaGameUiChrome.SetupOverlayCanvas(_root, 9005, interactive: true);

            _panelRect = BaGameUiChrome.BuildPanel(_root.transform, BaGameUiChrome.MinFallbackPanelWidth, 400f, "AutoShoppingPanel", out var header);
            _headerRect = header;
            BaGameUiChrome.ConfigureBottomLeftHudAnchor(_panelRect);

            var titleGo = new GameObject("Title", typeof(RectTransform));
            titleGo.transform.SetParent(header, false);
            _titleRect = titleGo.GetComponent<RectTransform>();
            _titleRect.anchorMin = Vector2.zero;
            _titleRect.anchorMax = Vector2.one;
            _titleLabel = titleGo.AddComponent<TextMeshProUGUI>();
            BaGameUiChrome.ApplyTitleStyle(_titleLabel, 1f);
            _titleLabel.text = ModUiText.PanelTitle;
            _titleLabel.overflowMode = TextOverflowModes.Ellipsis;
            ApplyHeaderTitleLayout(1f);

            _closeButtonRect = BaGameUiChrome.CreateHeaderCloseButton(header, Hide).GetComponent<RectTransform>();
            BuildCartBar();
            BuildListColumnHeaders();
            BuildScrollArea();
            BuildFooter();

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

        private static void BuildListColumnHeaders()
        {
            var headerGo = new GameObject("ListHeaders", typeof(RectTransform));
            headerGo.transform.SetParent(_panelRect, false);
            var headerRect = headerGo.GetComponent<RectTransform>();
            headerRect.anchorMin = new Vector2(0f, 1f);
            headerRect.anchorMax = new Vector2(1f, 1f);
            headerRect.pivot = new Vector2(0.5f, 1f);
            headerRect.anchoredPosition = new Vector2(0f, -BaGameUiChrome.HeaderBlockHeight - CartBarHeight);
            headerRect.sizeDelta = new Vector2(-BaGameUiChrome.ContentInset * 2f, ListHeaderHeight);

            _colQtyLabel = CreateColumnHeader(
                headerGo.transform,
                ModUiText.ColQty,
                1f,
                0f,
                QtyColumnWidth,
                TextAlignmentOptions.Center);
            _colPriceLabel = CreateColumnHeader(
                headerGo.transform,
                ModUiText.ColPrice,
                1f,
                QtyColumnWidth,
                PriceColumnWidth,
                TextAlignmentOptions.Center);
            _colItemLabel = CreateColumnHeader(
                headerGo.transform,
                ModUiText.ColItem,
                0f,
                IconColumnWidth,
                0f,
                TextAlignmentOptions.MidlineLeft);
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
                rect.offsetMax = new Vector2(-(PriceColumnWidth + QtyColumnWidth + 4f), 0f);
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

        private static void BuildScrollArea()
        {
            var scrollGo = new GameObject("Scroll", typeof(RectTransform), typeof(ScrollRect), typeof(Image));
            scrollGo.transform.SetParent(_panelRect, false);
            var scrollRect = scrollGo.GetComponent<RectTransform>();
            scrollRect.anchorMin = Vector2.zero;
            scrollRect.anchorMax = Vector2.one;
            scrollRect.offsetMin = new Vector2(BaGameUiChrome.ContentInset, BaGameUiChrome.FooterHeight + 8f);
            scrollRect.offsetMax = new Vector2(
                -BaGameUiChrome.ContentInset,
                -BaGameUiChrome.HeaderBlockHeight - CartBarHeight - ListHeaderHeight);
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
            var statusRect = statusGo.GetComponent<RectTransform>();
            statusRect.anchorMin = new Vector2(0f, 0f);
            statusRect.anchorMax = new Vector2(1f, 0f);
            statusRect.pivot = new Vector2(0.5f, 0f);
            statusRect.anchoredPosition = new Vector2(0f, StatusBottomOffset);
            statusRect.sizeDelta = new Vector2(-inset * 2f, StatusRowHeight);
            _statusLabel = statusGo.AddComponent<TextMeshProUGUI>();
            BaGameUiChrome.ApplyBodyStyle(_statusLabel, StatusFontScale, muted: true);
            _statusLabel.alignment = TextAlignmentOptions.Center;
            _statusLabel.overflowMode = TextOverflowModes.Ellipsis;
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
            if (_root != null)
                _root.SetActive(false);

            AutoShoppingToggleHud.RefreshLocalizedText();
        }

        /// <summary>Closes the panel for checkout and prevents auto-restore when PurchaseUI closes.</summary>
        internal static void CloseForCheckout()
        {
            _restoreAfterUnblock = false;
            Hide();
        }

        /// <summary>Hides while vanilla menus (ESC, phone, map, etc.) are open; restores if it was open before.</summary>
        internal static void UpdateVisibility()
        {
            if (_root == null)
                return;

            if (!GameState.ShouldShowStoreShoppingUi())
            {
                if (IsVisible)
                {
                    // Checkout is temporary; keep the panel closed after payment completes.
                    if (!GameState.IsCheckoutUiBlocking())
                        _restoreAfterUnblock = true;

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
            if (_titleLabel != null)
                _titleLabel.text = ModUiText.FormatPanelTitle(StoreSession.Current?.Profile?.BusinessDisplayName);
            if (_colItemLabel != null)
                _colItemLabel.text = ModUiText.ColItem;
            if (_colPriceLabel != null)
                _colPriceLabel.text = ModUiText.ColPrice;
            if (_colQtyLabel != null)
                _colQtyLabel.text = ModUiText.ColQty;
            if (_totalPrefixLabel != null)
                _totalPrefixLabel.text = ModUiText.TotalLabel;
            RefreshAll();
        }

        internal static void RefreshAll()
        {
            if (_root == null || !_root.activeSelf)
                return;

            var session = StoreSession.Current;
            if (session == null)
            {
                Hide();
                return;
            }

            session.RefreshPicked();

            if (_titleLabel != null)
                _titleLabel.text = ModUiText.FormatPanelTitle(session.Profile?.BusinessDisplayName);

            if (_statusLabel != null && string.IsNullOrEmpty(_statusText))
                _statusLabel.text = ModUiText.StatusIdle;

            UpdateCartBar();
            UpdateFooterLabels(session);
            AutoShoppingToggleHud.RefreshVisual();

            if (_lastRowCount != session.Products.Count)
            {
                _lastRowCount = session.Products.Count;
                RebuildProductRows(session);
            }
            else
            {
                UpdateRowQuantities(session);
            }

            ApplyLayout();
        }

        internal static void RefreshFooterOnly()
        {
            if (_root == null || !_root.activeSelf)
                return;

            var session = StoreSession.Current;
            if (session == null)
                return;

            session.RefreshPicked();
            UpdateFooterLabels(session);
            UpdateRowQuantities(session);
            ApplyLayout();
        }

        private static void ApplyLayout()
        {
            if (_panelRect == null || _headerRect == null)
                return;

            var rowCount = _lastRowCount > 0
                ? _lastRowCount
                : StoreSession.Current?.Products.Count ?? 0;

            var cart = ShoppingCartLayout.ResolveCartRect();
            var desiredHeight = ComputeDesiredPanelHeight(rowCount);
            var panelBottom = cart.Top + BaGameUiChrome.PanelGapAboveCart;
            var availableHeight = Screen.height - BaGameUiChrome.TopScreenMargin - panelBottom;
            var panelHeight = Mathf.Clamp(
                desiredHeight,
                BaGameUiChrome.MinPanelHeight,
                Mathf.Max(BaGameUiChrome.MinPanelHeight, availableHeight));
            var panelWidth = cart.Width;

            _panelRect.sizeDelta = new Vector2(panelWidth, panelHeight);
            _panelRect.anchoredPosition = new Vector2(cart.Left, panelBottom);

            var scale = BaGameUiChrome.GetScale(panelWidth);
            BaGameUiChrome.UpdatePanelFrames(_panelRect, _headerRect, panelWidth);
            ApplyHeaderTitleLayout(scale);
            BaGameUiChrome.ApplyHeaderCloseButtonLayout(_closeButtonRect, scale);

            if (_statusLabel != null)
                BaGameUiChrome.ApplyBodyStyle(_statusLabel, StatusFontScale * scale, muted: true);
        }

        private static void ApplyHeaderTitleLayout(float scale)
        {
            if (_titleRect == null || _titleLabel == null)
                return;

            var padX = BaGameUiChrome.HeaderTextPaddingX * scale;
            var padY = BaGameUiChrome.HeaderTextPaddingY * scale;
            var closeReserve = BaGameUiChrome.HeaderCloseButtonSize + 14f * scale;
            _titleRect.offsetMin = new Vector2(padX, padY);
            _titleRect.offsetMax = new Vector2(-closeReserve, -padY);
            BaGameUiChrome.ApplyTitleStyle(_titleLabel, scale);
        }

        private static float ComputeDesiredPanelHeight(int rowCount)
        {
            var scrollHeight = Mathf.Max(MinScrollHeight, rowCount * RowHeight + 12f);
            return BaGameUiChrome.HeaderBlockHeight + CartBarHeight + ListHeaderHeight + scrollHeight + BaGameUiChrome.FooterHeight;
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

        private static void RebuildProductRows(StoreSession session)
        {
            foreach (var row in _rows)
            {
                if (row?.Root != null)
                    Object.Destroy(row.Root.gameObject);
            }

            _rows.Clear();

            if (_scrollContent == null)
                return;

            foreach (var product in session.Products)
                _rows.Add(CreateProductRow(product, session));

            _lastRowCount = session.Products.Count;
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
            nameRect.offsetMax = new Vector2(-(PriceColumnWidth + QtyColumnWidth + 4f), 0f);
            var nameLabel = nameGo.AddComponent<TextMeshProUGUI>();
            BaGameUiChrome.ApplyBodyStyle(nameLabel, 0.95f);
            nameLabel.alignment = TextAlignmentOptions.MidlineLeft;
            nameLabel.text = product.DisplayName;

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

            var captured = product;
            btnMinus.onClick.AddListener(() => OnDecrease(captured, session));
            btnPlus.onClick.AddListener(() => OnIncrease(captured, session));

            return new ProductRowUi
            {
                Product = product,
                Root = rowRect,
                Icon = icon,
                NameLabel = nameLabel,
                PriceLabel = priceLabel,
                QtyLabel = qtyLabel,
                BtnMinus = btnMinus,
                BtnPlus = btnPlus
            };
        }

        private static void OnIncrease(CatalogProduct product, StoreSession session)
        {
            if (!product.InStock)
                return;

            var maxSlots = session.GetMaxSlots();
            var totalDesired = 0;
            foreach (var entry in session.Products)
                totalDesired += entry.DesiredQuantity;

            if (totalDesired >= maxSlots)
            {
                SetStatus(ModUiText.FormatErrorFull(maxSlots));
                return;
            }

            product.DesiredQuantity++;
            ModLog.Info("Desired +" + product.ItemName + " -> " + product.DesiredQuantity);
            _queue?.RequestFulfillment(session);
            RefreshAll();
        }

        private static void OnDecrease(CatalogProduct product, StoreSession session)
        {
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
            var session = StoreSession.Current;
            if (session == null || _queue == null)
                return;

            _queue.RequestPickCart(session);
            RefreshAll();
        }

        private static void OnDropCartClicked()
        {
            _queue?.RequestReleaseContainer();
            RefreshAll();
        }

        private static void OnPayClicked()
        {
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
            var session = StoreSession.Current;
            if (session == null || _queue == null)
                return;

            foreach (var product in session.Products)
                product.DesiredQuantity = 0;

            _queue.RequestFulfillment(session);
            RefreshAll();
        }

        private static void OnCancelQueueClicked() => _queue?.Cancel();

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
                         "AutoShopping_Panel_v0125"
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
        }
    }
}
