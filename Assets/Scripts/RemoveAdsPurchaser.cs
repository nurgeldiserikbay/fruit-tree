using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Purchasing;
using UnityEngine.UI;

namespace FruitTree
{
    /// <summary>
    /// Backend-free Google Play non-consumable. Store orders, not PlayerPrefs,
    /// decide entitlement; no advertising while entitlement is unknown.
    /// </summary>
    public sealed class RemoveAdsPurchaser : MonoBehaviour
    {
        public const string ProductId = "remove_ads";
        public AdsAccessPolicy adsPolicy;
        public Button buyButton;
        public Button restoreButton;
        public Text statusText;
        public Text priceText;

        private StoreController controller;
        private Product product;
        private bool connected;
        private bool inventoryFetched;
        private bool transactionActive;
        private bool shuttingDown;

        private async void Start()
        {
            if (adsPolicy == null)
            {
                Debug.LogError("RemoveAdsPurchaser requires AdsAccessPolicy.");
                return;
            }
            adsPolicy.MarkEntitlementUnknown();
            SetStatus("Checking your Google Play purchases...");
            RefreshControls();

#if UNITY_ANDROID && !UNITY_EDITOR
            try
            {
                controller = UnityIAPServices.StoreController();
                controller.OnProductsFetched += OnProductsFetched;
                controller.OnPurchasesFetched += OnPurchasesFetched;
                controller.OnPurchasePending += OnPurchasePending;
                controller.OnPurchaseConfirmed += OnPurchaseConfirmed;
                controller.OnStoreDisconnected += failure => OnStoreUnavailable();
                controller.OnProductsFetchFailed += failure => OnStoreUnavailable();
                controller.OnPurchasesFetchFailed += failure => OnStoreUnavailable();
                controller.OnPurchaseFailed += failure => OnPurchaseFailed();
                controller.OnPurchaseDeferred += order => OnPurchaseDeferred();

                await controller.Connect();
                if (shuttingDown) return;
                connected = true;
                controller.FetchProducts(new List<ProductDefinition>
                {
                    new ProductDefinition(ProductId, ProductType.NonConsumable)
                });
            }
            catch (Exception exception)
            {
                Debug.LogWarning("Unable to initialize Google Play IAP: " + exception.Message);
                OnStoreUnavailable();
            }
#else
            SetStatus("Google Play purchases work in Android test builds.");
#endif
        }

        private void OnProductsFetched(List<Product> products)
        {
            if (shuttingDown) return;
            product = products.Find(p => p != null && p.definition.id == ProductId);
            if (product == null)
            {
                OnStoreUnavailable();
                return;
            }
            controller.FetchPurchases();
        }

        private void OnPurchasesFetched(Orders orders)
        {
            if (shuttingDown) return;
            inventoryFetched = true;
            bool owned = false;
            foreach (var confirmed in orders.ConfirmedOrders)
                if (ContainsRemoveAds(confirmed)) owned = true;
            // Paid but not yet acknowledged purchases must be fulfilled too.
            foreach (var pending in orders.PendingOrders)
                if (ContainsRemoveAds(pending)) owned = true;

            adsPolicy.SetStoreEntitlement(owned);
            transactionActive = false;
            SetStatus(owned ? "Ad-free purchase is owned." :
                adsPolicy.HasAdProvider ? "Ad-free option available." :
                "No advertising is enabled in this build.");
            RefreshControls();
        }

        private void OnPurchasePending(PendingOrder order)
        {
            if (shuttingDown || !ContainsRemoveAds(order)) return;
            // Non-consumable reward: granting twice is idempotent. This unlock
            // happens only after Google Play delivers a PAID pending order.
            adsPolicy.SetStoreEntitlement(true);
            transactionActive = false;
            SetStatus("Ad-free purchase activated.");
            RefreshControls();
            // Acknowledge or Google Play may refund the purchase.
            controller.ConfirmPurchase(order);
        }

        private void OnPurchaseConfirmed(Order order)
        {
            if (shuttingDown) return;
            if (order is ConfirmedOrder confirmed && ContainsRemoveAds(confirmed))
            {
                adsPolicy.SetStoreEntitlement(true);
                transactionActive = false;
                SetStatus("Purchase confirmed. Thank you!");
            }
            else if (order is FailedOrder)
            {
                // Pending paid entitlement remains enabled while acknowledgement
                // can be retried when the store redelivers the order.
                transactionActive = false;
                SetStatus("Payment received; confirmation will be retried.");
            }
            RefreshControls();
        }

        private static bool ContainsRemoveAds(Order order)
        {
            if (order == null || order.CartOrdered == null) return false;
            foreach (var item in order.CartOrdered.Items())
                if (item.Product != null && item.Product.definition.id == ProductId)
                    return true;
            return false;
        }

        public void BuyRemoveAds()
        {
            // Never take money for removing ads when no ads SDK is configured.
            if (shuttingDown || controller == null || !connected || !inventoryFetched ||
                product == null || transactionActive || adsPolicy.OwnsRemoveAds ||
                !adsPolicy.HasAdProvider) return;
            transactionActive = true;
            SetStatus("Opening Google Play...");
            RefreshControls();
            try { controller.PurchaseProduct(ProductId); }
            catch (Exception exception)
            {
                Debug.LogWarning("Google Play purchase failed: " + exception.Message);
                OnPurchaseFailed();
            }
        }

        public void RestorePurchases()
        {
            // Android uses FetchPurchases; it does not require a separate restore
            // transaction. Nothing is stored as a locally editable ownership flag.
            if (controller == null || !connected || transactionActive) return;
            adsPolicy.MarkEntitlementUnknown();
            inventoryFetched = false;
            SetStatus("Checking past purchases...");
            RefreshControls();
            try { controller.FetchPurchases(); }
            catch (Exception) { OnStoreUnavailable(); }
        }

        private void OnPurchaseDeferred()
        {
            transactionActive = false;
            SetStatus("Waiting for Google Play payment approval.");
            RefreshControls();
        }

        private void OnPurchaseFailed()
        {
            transactionActive = false;
            SetStatus("Payment did not complete. No charge was confirmed.");
            RefreshControls();
        }

        private void OnStoreUnavailable()
        {
            if (shuttingDown) return;
            // Fail closed: games remains playable without ads if Play is offline.
            adsPolicy.MarkEntitlementUnknown();
            connected = false;
            inventoryFetched = false;
            transactionActive = false;
            SetStatus("Store unavailable. You can keep playing.");
            RefreshControls();
        }

        private void SetStatus(string message)
        {
            if (statusText != null) statusText.text = message;
        }

        private void RefreshControls()
        {
            bool owned = adsPolicy != null && adsPolicy.OwnsRemoveAds;
            bool canBuy = connected && inventoryFetched && !transactionActive &&
                product != null && !owned && adsPolicy != null && adsPolicy.HasAdProvider;
            if (buyButton != null) buyButton.interactable = canBuy;
            if (restoreButton != null)
                restoreButton.interactable = connected && !transactionActive;
            if (priceText != null)
            {
                priceText.text = owned ? "OWNED" :
                    product != null ? product.metadata.localizedPriceString :
                    "Not available";
            }
        }

        private void OnDestroy()
        {
            shuttingDown = true;
            if (controller == null) return;
            controller.OnProductsFetched -= OnProductsFetched;
            controller.OnPurchasesFetched -= OnPurchasesFetched;
            controller.OnPurchasePending -= OnPurchasePending;
            controller.OnPurchaseConfirmed -= OnPurchaseConfirmed;
        }
    }
}
