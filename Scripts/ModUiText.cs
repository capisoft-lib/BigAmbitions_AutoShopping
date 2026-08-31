using System.Collections.Generic;
using Capisoft.Lib.BaUnifiedUI.Options;
using Capisoft.Lib.BaUnifiedUI.Shortcuts;
using Localizor;
using UnityEngine;

namespace AutoShopping
{
    internal static class ModUiText
    {
        private static string _activeLocale = string.Empty;
        private static float _nextLocalePoll;

        internal static string PanelTitle => Loc("autoshopping_panel_title", "Auto Shopping");

        internal static string FormatPanelTitle(string storeName)
        {
            if (string.IsNullOrWhiteSpace(storeName))
                return PanelTitle;

            return PanelTitle + " - " + storeName;
        }
        internal static string ColItem => Loc("autoshopping_col_item", "ITEM");
        internal static string ColPrice => Loc("autoshopping_col_price", "PRICE");
        internal static string ColQty => Loc("autoshopping_col_qty", "QTY");
        internal static string BtnWayTo => Loc("autoshopping_btn_way_to", "Way to");
        internal static string SearchPlaceholder => Loc("autoshopping_search_placeholder", "Search items…");
        internal static string TotalLabel => Loc("autoshopping_total_label", "Total:");
        internal static string StoreUnsupported => Loc("autoshopping_store_unsupported", "This store type is not supported.");
        internal static string OutOfStock => Loc("autoshopping_out_of_stock", "Out of stock");
        internal static string BtnInfo => Loc("autoshopping_btn_info", "Info");
        internal static string BtnDrop => Loc("autoshopping_btn_drop", "Drop");
        internal static string ContainerBareHands => Loc("autoshopping_container_bare_hands", "Bare hands");
        internal static string ContainerBasket => Loc("autoshopping_container_basket", "Basket");
        internal static string ContainerShoppingCart => Loc("autoshopping_container_shopping_cart", "Shopping cart");
        internal static string ContainerBigCart => Loc("autoshopping_container_big_cart", "Big cart");
        internal static string ContainerHandTruck => Loc("autoshopping_container_handtruck", "Hand truck");
        internal static string BtnPickCart => Loc("autoshopping_btn_pick_cart", "Pick container");
        internal static string BtnPickBasket => Loc("autoshopping_btn_pick_basket", "Pick basket");
        internal static string BtnDropCart => Loc("autoshopping_btn_drop_cart", "Put down");
        internal static string BtnHide => Loc("autoshopping_btn_hide", "Hide");
        internal static string BtnShow => Loc("autoshopping_btn_show", "Show");
        internal static string StatusCartAlreadyHeld => Loc("autoshopping_status_cart_already_held", "Already holding a container.");
        internal static string ErrorNoCartToDrop => Loc("autoshopping_error_no_cart_to_drop", "No container to put down.");
        internal static string StatusDroppingContainer => Loc("autoshopping_status_dropping_container", "Putting container down…");
        internal static string ErrorContainerDropFailed => Loc("autoshopping_error_container_drop_failed", "Could not put container down.");

        internal static string GetPickContainerButtonLabel(StoreProfile profile)
        {
            if (profile?.Kind == StoreKind.RetailSelfService && profile.OffersShoppingBasket())
                return BtnPickBasket;

            return BtnPickCart;
        }

        internal static string GetDropContainerButtonLabel()
        {
            return DetectActiveTarget() switch
            {
                ShoppingContainerTarget.ShoppingBasket => Loc("autoshopping_btn_drop_basket", "Put down basket"),
                ShoppingContainerTarget.HandTruck => Loc("autoshopping_btn_drop_handtruck", "Park hand truck"),
                ShoppingContainerTarget.ShoppingCart => Loc("autoshopping_btn_drop_vehicle_cart", "Park cart"),
                _ => BtnDropCart
            };
        }

        private static ShoppingContainerTarget DetectActiveTarget() =>
            ShoppingCargoHelper.DetectActiveTarget();

        internal static string FormatCartChip(ShoppingContainerTarget target, int capacity)
        {
            var name = target switch
            {
                ShoppingContainerTarget.ShoppingBasket => ContainerBasket,
                ShoppingContainerTarget.ShoppingCart => ContainerShoppingCart,
                ShoppingContainerTarget.HandTruck => ContainerHandTruck,
                _ => ContainerBareHands
            };

            return name + " " + LocaleFormat.Integer(capacity);
        }

        internal static string FormatContainerMode(ShoppingContainerTarget target)
        {
            return target switch
            {
                ShoppingContainerTarget.ShoppingBasket => ContainerBasket,
                ShoppingContainerTarget.ShoppingCart => ContainerShoppingCart,
                ShoppingContainerTarget.HandTruck => ContainerHandTruck,
                _ => ContainerBareHands
            };
        }
        internal static string BtnPay => Loc("autoshopping_btn_pay", "Pay");
        internal static string BtnClose => Loc("autoshopping_btn_close", "Close");
        internal static string BtnOpen => Loc("autoshopping_btn_open", "Open");
        internal static string BtnClear => Loc("autoshopping_btn_clear", "Clear list");
        internal static string BtnCancelQueue => Loc("autoshopping_btn_cancel_queue", "Cancel actions");
        internal static string StatusIdle => Loc("autoshopping_status_idle", "Ready");
        internal static string ErrorNoContainer => Loc("autoshopping_error_no_container", "Take a basket or cart first.");
        internal static string ErrorNoCartAvailable => Loc("autoshopping_error_no_cart_available", "No cart available in this store.");
        internal static string ErrorNoRegister => Loc("autoshopping_error_no_register", "No checkout found.");
        internal static string ErrorNothingToPay => Loc("autoshopping_error_nothing_to_pay", "Nothing to pay for.");
        internal static string ErrorNothingToDrop => Loc("autoshopping_error_nothing_to_drop", "Nothing to drop.");
        internal static string ToggleHint =>
            Loc("autoshopping_toggle_hint", "Use the configured shortcut to toggle Auto Shopping");
        internal static BaKeybindUiText CreateShortcutUiText() =>
            new BaKeybindUiText(
                Loc("autoshopping_shortcut_unbound", "Unbound"),
                Loc("autoshopping_shortcut_capture", "Press a key..."),
                Loc("autoshopping_shortcut_conflict", "Conflict"));
        internal static BaColorPickerUiText CreateColorPickerUiText() =>
            new BaColorPickerUiText(
                Loc("autoshopping_color_picker_unavailable", "Color picker unavailable"));
        internal static string OptionAutoPickCart => Loc("autoshopping_option_auto_pick_cart", "Auto pick cart");
        internal static string OptionAutoPickCartOn => Loc("autoshopping_option_auto_pick_cart_on", "Auto cart ON");

        internal static string FormatSlots(int current, int max) =>
            LocFormat("autoshopping_slots", "{current} / {max} items", new Dictionary<string, string>
            {
                { "current", LocaleFormat.Integer(current) },
                { "max", LocaleFormat.Integer(max) }
            });

        internal static string FormatTotalValue(float total) => LocaleFormat.Money(total);

        internal static string FormatBalance(float balance) =>
            LocFormat("autoshopping_balance", "Balance: {balance}", "balance", LocaleFormat.Money(balance));

        internal static string FormatErrorFull(int max) =>
            LocFormat("autoshopping_error_full", "Container is full (max {max}).", "max", LocaleFormat.Integer(max));

        internal static string FormatInfo(ShoppingContainerTarget selected, int currentCapacity, int storeMaxCapacity) =>
            LocFormat("autoshopping_info_capacity",
                "Selected {selected}. Capacity {current}. Store max {store}.",
                new Dictionary<string, string>
                {
                    { "selected", FormatContainerMode(selected) },
                    { "current", LocaleFormat.Integer(currentCapacity) },
                    { "store", LocaleFormat.Integer(storeMaxCapacity) }
                });

        internal static string StatusWalkingBasket =>
            Loc("autoshopping_status_walking_basket", "Walking to basket stack…");

        internal static string StatusWalkingHandtruck =>
            Loc("autoshopping_status_walking_handtruck", "Walking to hand truck…");

        internal static string StatusWalkingShoppingCart =>
            Loc("autoshopping_status_walking_shopping_cart", "Walking to shopping cart…");

        internal static string FormatStatusPicking(string itemLabel) =>
            LocFormat("autoshopping_status_picking", "Picking {item}…", "item", itemLabel);

        internal static string FormatStatusDropping(string itemLabel) =>
            LocFormat("autoshopping_status_dropping", "Dropping {item}…", "item", itemLabel);

        internal static string FormatStatusWayTo(string itemLabel) =>
            LocFormat("autoshopping_status_way_to", "Route to {item}", "item", itemLabel);

        internal static string StatusPaying =>
            Loc("autoshopping_status_paying", "Walking to checkout…");

        internal static string StatusQueueBusy =>
            Loc("autoshopping_status_queue_busy", "Action in progress…");

        internal static string ErrorUnreachable =>
            Loc("autoshopping_error_unreachable", "Cannot reach item.");

        internal static void PollLanguageChange()
        {
            var now = Time.unscaledTime;
            if (now < _nextLocalePoll)
                return;

            _nextLocalePoll = now + 0.5f;
            using (ModPerf.Measure("poll.locale"))
            {
                var locale = ResolveLoadedLocale();
                if (locale == _activeLocale)
                    return;

                _activeLocale = locale;
                AutoShoppingPanel.RefreshLocalizedText();
                AutoShoppingToggleHud.RefreshLocalizedText();
            }
        }

        private static string LocFormat(string key, string fallback, string token, string value) =>
            Loc(key, fallback).Replace("{" + token + "}", value);

        private static string LocFormat(string key, string fallback, Dictionary<string, string> tokens)
        {
            var text = Loc(key, fallback);
            foreach (var pair in tokens)
                text = text.Replace("{" + pair.Key + "}", pair.Value);
            return text;
        }

        private static string Loc(string key, string fallback)
        {
            try
            {
                var text = key.GetLocalization();
                if (!string.IsNullOrWhiteSpace(text) && text != key)
                    return text;
            }
            catch
            {
                // locale not ready
            }

            return fallback;
        }

        private static string ResolveLoadedLocale()
        {
            try
            {
                var locale = LocalizorManager.LoadedLocale;
                if (!string.IsNullOrWhiteSpace(locale))
                    return locale.Trim().Replace('_', '-');
            }
            catch
            {
                // ignore
            }

            return "en";
        }
    }
}
