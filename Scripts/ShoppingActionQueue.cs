using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using Controllers;
using Helpers;
using UnityEngine;

using UnityEngine.Events;



namespace AutoShopping

{

    internal enum ShoppingActionType

    {

        AcquireBasket,

        AcquireHandTruck,

        AcquireShoppingCart,

        PickItem,

        DropItem,

        Pay,

        ReleaseBasket,

        ReleaseHandTruck,

        ReleaseShoppingCart

    }



    internal sealed class ShoppingAction

    {

        internal ShoppingActionType Type { get; set; }

        internal string ItemName { get; set; } = string.Empty;

    }



    internal sealed class ShoppingActionQueue

    {

        private readonly List<ShoppingAction> _pending = new List<ShoppingAction>();

        private Coroutine _runner;

        private MonoBehaviour _host;

        private bool _cancelRequested;

        private bool _interruptRequested;

        private ShoppingAction _executingAction;



        internal bool IsBusy => _runner != null;

        internal int PendingCount => _pending.Count;



        internal void Bind(MonoBehaviour host) => _host = host;



        internal void Enqueue(ShoppingAction action)

        {

            if (action == null)

                return;



            _pending.Add(action);

            TryStart();

        }



        internal void RequestPickCart(StoreSession session)
        {
            if (session?.Profile == null)
                return;

            if (session.Profile.HasRequiredContainer())
            {
                AutoShoppingPanel.SetStatus(ModUiText.StatusCartAlreadyHeld);
                return;
            }

            session.SelectedTarget = session.Profile.GetDefaultContainerTarget();
            if (!RequestAutoPickOnEnter(session))
                AutoShoppingPanel.SetStatus(ModUiText.ErrorNoCartAvailable);
        }

        internal void RequestReleaseContainer()
        {
            if (!ShoppingCargoHelper.HasContainerToRelease())
            {
                AutoShoppingPanel.SetStatus(ModUiText.ErrorNoCartToDrop);
                return;
            }

            if (HasPendingRelease())
                return;

            ShoppingAction release;
            if (ShoppingCargoHelper.IsUsingHandTruck())
                release = new ShoppingAction { Type = ShoppingActionType.ReleaseHandTruck };
            else if (ShoppingCargoHelper.IsUsingShoppingVehicle())
                release = new ShoppingAction { Type = ShoppingActionType.ReleaseShoppingCart };
            else
                release = new ShoppingAction { Type = ShoppingActionType.ReleaseBasket };

            ModLog.Info("Queue release cart: " + release.Type);
            _pending.Insert(0, release);
            TryStart();
        }

        /// <summary>Pick basket or hand truck when entering a store (no desired items required).</summary>

        internal bool RequestAutoPickOnEnter(StoreSession session)
        {
            if (session?.Profile == null || session.Profile.HasRequiredContainer())
                return false;

            if (HasPendingAcquire())
                return true;

            var acquire = CreateAcquireAction(session);
            if (acquire == null)
                return false;

            ModLog.Info("Queue auto-pick on enter: " + acquire.Type);
            _pending.Insert(0, acquire);
            TryStart();
            return true;
        }



        /// <summary>Sync pending pick/drop actions with desired vs picked quantities.</summary>

        internal void RequestFulfillment(StoreSession session)

        {

            if (session == null)

                return;

            using var perf = ModPerf.Measure("queue.request_fulfillment");

            PruneObsoletePicksAndDrops(session);

            EnsureContainerAcquisition(session);

            foreach (var product in session.Products)

            {

                var pendingPicks = CountPending(product.ItemName, ShoppingActionType.PickItem);

                var pickNeed = product.DesiredQuantity - product.PickedQuantity - pendingPicks;

                for (var i = 0; i < pickNeed; i++)

                {

                    _pending.Add(new ShoppingAction

                    {

                        Type = ShoppingActionType.PickItem,

                        ItemName = product.ItemName

                    });

                }



                var pendingDrops = CountPending(product.ItemName, ShoppingActionType.DropItem);

                var dropNeed = product.PickedQuantity - product.DesiredQuantity - pendingDrops;

                for (var i = 0; i < dropNeed; i++)

                {

                    _pending.Add(new ShoppingAction

                    {

                        Type = ShoppingActionType.DropItem,

                        ItemName = product.ItemName

                    });

                }

            }



            ModLog.Info("Queue fulfillment: pending=" + _pending.Count);

            if (_runner != null && IsExecutingActionObsolete(session))
                StopActiveRunner(restart: true);
            else
                TryStart();
        }



        private void EnsureContainerAcquisition(StoreSession session)

        {

            if (session.Profile == null || session.Profile.HasRequiredContainer())

                return;



            if (!HasAnyDesiredItems(session))

                return;



            if (HasPendingAcquire())

                return;



            var acquire = CreateAcquireAction(session);
            if (acquire == null)
                return;

            ModLog.Info("Queue auto-acquire: " + acquire.Type);
            _pending.Insert(0, acquire);
        }

        private static ShoppingAction CreateAcquireAction(StoreSession session)
        {
            if (session?.Profile == null)
                return null;

            switch (session.SelectedTarget)
            {
                case ShoppingContainerTarget.HandTruck
                    when session.Profile.AllowsHandTruck && ShoppingCargoHelper.HasHandTruckSpawnerInStore():
                    return new ShoppingAction { Type = ShoppingActionType.AcquireHandTruck };

                case ShoppingContainerTarget.ShoppingCart
                    when ShoppingCargoHelper.HasShoppingCartSpawnerInStore():
                    return new ShoppingAction { Type = ShoppingActionType.AcquireShoppingCart };

                case ShoppingContainerTarget.ShoppingBasket
                    when session.Profile.OffersShoppingBasket() && ShoppingCargoHelper.HasBasketProviderInStore():
                    return new ShoppingAction { Type = ShoppingActionType.AcquireBasket };
            }

            return null;
        }



        private static bool HasAnyDesiredItems(StoreSession session)

        {

            foreach (var product in session.Products)

            {

                if (product.DesiredQuantity > 0)

                    return true;

            }



            return false;

        }



        private bool HasPendingRelease()
        {
            foreach (var action in _pending)
            {
                if (action.Type == ShoppingActionType.ReleaseBasket ||
                    action.Type == ShoppingActionType.ReleaseHandTruck ||
                    action.Type == ShoppingActionType.ReleaseShoppingCart)
                    return true;
            }

            return false;
        }

        private bool HasPendingAcquire()

        {

            foreach (var action in _pending)

            {

                if (action.Type == ShoppingActionType.AcquireBasket ||
                    action.Type == ShoppingActionType.AcquireHandTruck ||
                    action.Type == ShoppingActionType.AcquireShoppingCart)
                    return true;

            }



            return false;

        }



        internal void PrependPay()

        {

            _pending.RemoveAll(a =>

                a.Type == ShoppingActionType.PickItem ||

                a.Type == ShoppingActionType.DropItem ||

                a.Type == ShoppingActionType.Pay);



            _pending.Insert(0, new ShoppingAction { Type = ShoppingActionType.Pay });

            TryStart();

        }



        internal void Cancel()

        {

            _cancelRequested = true;

            _pending.Clear();

            StopActiveRunner(restart: false);

            _cancelRequested = false;

            AutoShoppingPanel.SetStatus(ModUiText.StatusIdle);

            AutoShoppingPanel.RefreshAfterQueueAction();

        }



        private void TryStart()

        {

            if (_host == null || _runner != null || _pending.Count == 0)

                return;



            _cancelRequested = false;

            ModLog.Info("Queue start: actions=" + _pending.Count);

            _runner = _host.StartCoroutine(RunQueue());

        }



        private IEnumerator RunQueue()

        {

            try

            {

                while (_pending.Count > 0 && !_cancelRequested)

                {

                    var session = StoreSession.Current;

                    if (session != null)

                        PruneObsoletePicksAndDrops(session);



                    if (_pending.Count == 0)

                        break;



                    var action = _pending[0];

                    _pending.RemoveAt(0);

                    _executingAction = action;

                    ModLog.Info("Queue execute: " + action.Type +

                                (string.IsNullOrEmpty(action.ItemName) ? string.Empty : " " + action.ItemName));

                    var actionStart = Stopwatch.GetTimestamp();
                    yield return Execute(action);
                    _executingAction = null;
                    var actionMs = (Stopwatch.GetTimestamp() - actionStart) * 1000.0 / Stopwatch.Frequency;
                    ModPerf.Record("queue.action." + action.Type, actionMs);

                    StoreSession.Current?.RefreshPicked();

                    AutoShoppingPanel.RefreshAfterQueueAction();

                }

            }

            finally

            {

                _runner = null;

                _executingAction = null;

                _cancelRequested = false;

                _interruptRequested = false;

            }



            if (_pending.Count > 0)

            {

                TryStart();

                yield break;

            }



            AutoShoppingPanel.SetStatus(ModUiText.StatusIdle);

            AutoShoppingPanel.RefreshAll();

        }



        private int CountPending(string itemName, ShoppingActionType type)

        {

            var count = 0;

            foreach (var action in _pending)

            {

                if (action.Type == type && action.ItemName == itemName)

                    count++;

            }



            return count;

        }



        private void StopActiveRunner(bool restart)
        {
            _interruptRequested = true;
            InterruptPlayerMovement();

            if (_runner != null && _host != null)
            {
                _host.StopCoroutine(_runner);
                _runner = null;
            }

            _executingAction = null;
            _interruptRequested = false;

            if (restart)
                TryStart();
        }

        private static void InterruptPlayerMovement()
        {
            var player = InstanceBehavior<GameManager>.Instance?.playerController;
            player?.ResetNavigation();
        }

        private bool ShouldAbortMovement() => _cancelRequested || _interruptRequested;

        private bool IsExecutingActionObsolete(StoreSession session)
        {
            if (_executingAction == null)
                return false;

            switch (_executingAction.Type)
            {
                case ShoppingActionType.AcquireBasket:
                case ShoppingActionType.AcquireHandTruck:
                case ShoppingActionType.AcquireShoppingCart:
                    return !HasAnyDesiredItems(session);

                case ShoppingActionType.PickItem:
                {
                    var product = session.FindProduct(_executingAction.ItemName);
                    return product == null || product.PickedQuantity >= product.DesiredQuantity;
                }

                case ShoppingActionType.DropItem:
                {
                    var product = session.FindProduct(_executingAction.ItemName);
                    return product == null || product.PickedQuantity <= product.DesiredQuantity;
                }

                default:
                    return false;
            }
        }

        private void PruneObsoletePicksAndDrops(StoreSession session)

        {

            for (var i = _pending.Count - 1; i >= 0; i--)

            {

                var action = _pending[i];

                if (action.Type != ShoppingActionType.PickItem && action.Type != ShoppingActionType.DropItem)

                    continue;



                var product = session.FindProduct(action.ItemName);

                if (product == null)

                {

                    _pending.RemoveAt(i);

                    continue;

                }



                if (action.Type == ShoppingActionType.PickItem &&

                    product.PickedQuantity >= product.DesiredQuantity)

                {

                    _pending.RemoveAt(i);

                }

                else if (action.Type == ShoppingActionType.DropItem &&

                         product.PickedQuantity <= product.DesiredQuantity)

                {

                    _pending.RemoveAt(i);

                }

            }

        }



        private IEnumerator Execute(ShoppingAction action)

        {

            switch (action.Type)

            {

                case ShoppingActionType.AcquireBasket:

                    AutoShoppingPanel.SetStatus(ModUiText.StatusWalkingBasket);

                    yield return WalkAndInteract(ShoppingCargoHelper.FindNearestBasketProvider());

                    break;



                case ShoppingActionType.AcquireHandTruck:

                    AutoShoppingPanel.SetStatus(ModUiText.StatusWalkingHandtruck);

                    yield return WalkAndInteract(ShoppingCargoHelper.FindNearestHandTruckSpawner());

                    break;

                case ShoppingActionType.AcquireShoppingCart:

                    AutoShoppingPanel.SetStatus(ModUiText.StatusWalkingShoppingCart);

                    yield return WalkAndInteract(ShoppingCargoHelper.FindNearestShoppingCartSpawner());

                    break;



                case ShoppingActionType.PickItem:

                    yield return PickUntilFulfilled(action.ItemName);

                    break;



                case ShoppingActionType.DropItem:

                    yield return DropUntilFulfilled(action.ItemName);

                    break;



                case ShoppingActionType.Pay:

                    AutoShoppingPanel.CloseForCheckout();

                    AutoShoppingPanel.SetStatus(ModUiText.StatusPaying);

                    yield return PaymentService.PayAtRegister(ShoppingCargoHelper.FindNearestCashRegister());

                    break;

                case ShoppingActionType.ReleaseBasket:
                case ShoppingActionType.ReleaseHandTruck:
                case ShoppingActionType.ReleaseShoppingCart:

                    AutoShoppingPanel.SetStatus(ModUiText.StatusDroppingContainer);

                    if (!ShoppingCargoHelper.TryReleaseContainerAtFeet())
                        AutoShoppingPanel.SetStatus(ModUiText.ErrorContainerDropFailed);

                    yield return null;

                    break;

            }

        }



        private IEnumerator PickUntilFulfilled(string itemName)

        {

            ItemController lastShelf = null;

            var stalls = 0;



            while (stalls < 3)

            {
                if (ShouldAbortMovement())
                {
                    InterruptPlayerMovement();
                    yield break;
                }

                var product = StoreSession.Current?.FindProduct(itemName);

                if (product == null || product.PickedQuantity >= product.DesiredQuantity)

                    yield break;



                var before = product.PickedQuantity;

                var shelf = ShoppingCargoHelper.FindNearestPurchasableShelf(itemName);

                if (shelf == null)

                {

                    AutoShoppingPanel.SetStatus(ModUiText.ErrorUnreachable);

                    yield break;

                }



                if (lastShelf != shelf)

                {

                    var label = ShoppingCargoHelper.GetItemLabel(itemName);

                    AutoShoppingPanel.SetStatus(ModUiText.FormatStatusPicking(label));

                    yield return WalkAndInteract(shelf);

                    lastShelf = shelf;

                }

                else

                {

                    yield return InteractAtShelf(shelf);

                }



                StoreSession.Current?.RefreshPicked();

                product = StoreSession.Current?.FindProduct(itemName);

                if (product == null)

                    yield break;



                if (product.PickedQuantity > before)

                {

                    stalls = 0;

                    if (product.PickedQuantity >= product.DesiredQuantity)

                        yield break;



                    continue;

                }



                stalls++;

            }

        }



        private IEnumerator DropUntilFulfilled(string itemName)

        {
            if (ShouldAbortMovement())
            {
                InterruptPlayerMovement();
                yield break;
            }

            var product = StoreSession.Current?.FindProduct(itemName);

            if (product == null || product.PickedQuantity <= product.DesiredQuantity)

                yield break;



            var label = ShoppingCargoHelper.GetItemLabel(itemName);

            AutoShoppingPanel.SetStatus(ModUiText.FormatStatusDropping(label));

            if (!ShoppingCargoHelper.TryRemoveOneUnpaid(itemName))

            {

                ModLog.Warn("Drop failed for " + itemName);

                yield break;

            }



            yield return new WaitForSeconds(0.15f);

        }



        private static IEnumerator InteractAtShelf(ItemController shelf)

        {

            if (shelf == null)

                yield break;



            yield return null;

            shelf.Interact();

            yield return new WaitForSeconds(0.2f);

        }



        private IEnumerator WalkAndInteract(ItemController target)

        {

            if (target == null)

            {

                AutoShoppingPanel.SetStatus(ModUiText.ErrorUnreachable);

                yield break;

            }



            yield return WalkAndInteract((EntityController)target);

        }



        private IEnumerator WalkAndInteract(EntityController target)

        {

            var player = InstanceBehavior<GameManager>.Instance?.playerController;

            if (player == null || target == null)

            {

                AutoShoppingPanel.SetStatus(ModUiText.ErrorUnreachable);

                yield break;

            }



            var reached = false;

            player.SetGoal(target, (UnityAction)(() => reached = true));



            var timeout = Time.unscaledTime + 45f;

            while (!reached && Time.unscaledTime < timeout)

            {

                if (!GameState.IsInsideSupportedInterior() || ShouldAbortMovement())

                {
                    InterruptPlayerMovement();
                    yield break;
                }

                yield return null;

            }



            if (!reached)

            {

                if (!ShouldAbortMovement())
                    AutoShoppingPanel.SetStatus(ModUiText.ErrorUnreachable);

                InterruptPlayerMovement();
                yield break;

            }



            if (ShouldAbortMovement())
            {
                InterruptPlayerMovement();
                yield break;
            }

            yield return null;

            target.Interact();

            yield return new WaitForSeconds(0.2f);

        }

    }

}


