using System.Collections;

using Controllers;

using Helpers;

using UI;

using UI.Purchase;

using UnityEngine;



namespace AutoShopping

{

    internal static class PaymentService

    {

        internal static IEnumerator PayAtRegister(CashRegisterController register)

        {

            if (register == null)

            {

                AutoShoppingPanel.SetStatus(ModUiText.ErrorNoRegister);

                ModLog.Warn("Pay blocked: no register found");

                yield break;

            }



            var unpaid = ShoppingCargoHelper.GetUnpaidCargo(ShoppingCargoHelper.GetActiveHolder());

            if (unpaid.Count == 0)

            {

                AutoShoppingPanel.SetStatus(ModUiText.ErrorNothingToPay);

                yield break;

            }



            if (!register.CanOrder())

            {

                AutoShoppingPanel.SetStatus(ModUiText.ErrorNothingToPay);

                ModLog.Warn("Pay blocked: register.CanOrder() is false");

                yield break;

            }



            ModLog.Info("Pay: opening checkout via register.Order()");

            register.Order();



            var timeout = Time.unscaledTime + 10f;

            while (!PurchaseUI.IsPanelOpen && Time.unscaledTime < timeout)

                yield return null;



            if (PurchaseUI.IsPanelOpen)

            {

                var purchaseUi = InstanceBehavior<UIs>.Instance.playerHUD.purchaseUI;

                purchaseUi.PlaceOrder();

                ModLog.Info("Pay: PurchaseUI.PlaceOrder() called");

            }

            else

            {

                ModLog.Warn("Pay: PurchaseUI did not open, falling back to OnPlaceOrder");

                register.OnPlaceOrder(unpaid);

            }



            timeout = Time.unscaledTime + 90f;

            while (!PlayerHelper.HasPaidForAllItems() && Time.unscaledTime < timeout)

            {

                if (!BuildingManager.IsInsideBuilding)

                    yield break;



                yield return null;

            }



            ResetDesiredQuantitiesAfterPay();

            StoreSession.Current?.RefreshPicked();

            AutoShoppingPanel.CloseForCheckout();

            AutoShoppingPanel.SetStatus(

                PlayerHelper.HasPaidForAllItems()

                    ? ModUiText.StatusIdle

                    : ModUiText.ErrorNothingToPay);

        }



        private static void ResetDesiredQuantitiesAfterPay()

        {

            var session = StoreSession.Current;

            if (session == null || !PlayerHelper.HasPaidForAllItems())

                return;



            foreach (var product in session.Products)

                product.DesiredQuantity = 0;

        }

    }

}


