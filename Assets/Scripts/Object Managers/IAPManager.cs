using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Purchasing;


public class IAPManager : MonoBehaviour, IStoreListener
{
    public static IAPManager instance;

    private static IStoreController m_StoreController;
    private static IExtensionProvider m_StoreExtensionProvider;

    // Product ids live in ProductIds, which is pinned against the store catalogs by tests.
    // They used to be private fields here, and the "removeads" one did not match any catalog.


    //************************** Adjust these methods **************************************
    public void InitializePurchasing()
    {
        if (IsInitialized()) { return; }
        var builder = ConfigurationBuilder.Instance(StandardPurchasingModule.Instance());

        // Registered from the one list, so a SKU cannot be published and then quietly never
        // offered - which is exactly what happened to gems10000.
        builder.AddProduct(ProductIds.RemoveAds, ProductType.NonConsumable);
        builder.AddProduct(ProductIds.Gems1000, ProductType.Consumable);
        builder.AddProduct(ProductIds.Gems2250, ProductType.Consumable);
        builder.AddProduct(ProductIds.Gems5000, ProductType.Consumable);
        builder.AddProduct(ProductIds.Gems10000, ProductType.Consumable);

        UnityPurchasing.Initialize(this, builder);
    }


    private bool IsInitialized()
    {
        return m_StoreController != null && m_StoreExtensionProvider != null;
    }


    //Step 3 Create methods
    public void BuyRemoveAds()
    {
        BuyProductID(ProductIds.RemoveAds);
    }

    public void SmallGems()
    {
        BuyProductID(ProductIds.Gems1000);
    }

    public void MediumGems()
    {
        BuyProductID(ProductIds.Gems2250);
    }

    public void LargeGems()
    {
        BuyProductID(ProductIds.Gems5000);
    }

    /// <summary>
    /// Starts a real-money ship purchase. Wired to store products in the IAP hardening plan;
    /// until then it fails closed and grants nothing.
    /// </summary>
    public void BuyShip(string shipId)
    {
        Debug.LogWarning("BuyShip not yet wired to a store product: " + shipId);
    }


    //Step 4 modify purchasing
    public PurchaseProcessingResult ProcessPurchase(PurchaseEventArgs args)
    {
        string purchasedId = args.purchasedProduct.definition.id;

        // One lookup instead of a chain of string comparisons. A new gem SKU now only has to
        // be added to ProductIds; previously it also had to be remembered here, and gems10000
        // proves that step gets forgotten.
        int gemsGranted = ProductIds.GemsFor(purchasedId);
        if (gemsGranted > 0)
        {
            DataHolder.instance.gems += gemsGranted;
        }
        else if (String.Equals(purchasedId, ProductIds.RemoveAds, StringComparison.Ordinal))
        {
            // Entitlements go to the profile, never to an array index in dataSaved.
            // CaptureFromSaveData does not touch removeAdsOwned, so it survives the Save below.
            SpaceshipDivine.Save.PlayerProfile profile = DataHolder.instance.Profile;
            if (profile != null)
                profile.removeAdsOwned = true;
            else
                Debug.LogError("removeads purchased but no profile is loaded; not granted.");
        }
        else
        {
            // Fail closed and keep the receipt pending, so a product this build does not
            // understand is retried after an update rather than being consumed for nothing.
            Debug.LogWarning("Unrecognised product purchased: " + purchasedId);
            return PurchaseProcessingResult.Pending;
        }

        DataHolder.instance.Save();
        return PurchaseProcessingResult.Complete;
    }










    //**************************** Dont worry about these methods ***********************************
    private void Awake()
    {
        TestSingleton();
    }

    void Start()
    {
        if (m_StoreController == null) { InitializePurchasing(); }
    }

    private void TestSingleton()
    {
        if (instance != null) { Destroy(gameObject); return; }
        instance = this;
        DontDestroyOnLoad(gameObject);
    }

    void BuyProductID(string productId)
    {
        if (IsInitialized())
        {
            Product product = m_StoreController.products.WithID(productId);
            if (product != null && product.availableToPurchase)
            {
                Debug.Log(string.Format("Purchasing product asychronously: '{0}'", product.definition.id));
                m_StoreController.InitiatePurchase(product);
            }
            else
            {
                Debug.Log("BuyProductID: FAIL. Not purchasing product, either is not found or is not available for purchase");
            }
        }
        else
        {
            Debug.Log("BuyProductID FAIL. Not initialized.");
        }
    }

    public void RestorePurchases()
    {
        if (!IsInitialized())
        {
            Debug.Log("RestorePurchases FAIL. Not initialized.");
            return;
        }

        if (Application.platform == RuntimePlatform.IPhonePlayer ||
            Application.platform == RuntimePlatform.OSXPlayer)
        {
            Debug.Log("RestorePurchases started ...");

            var apple = m_StoreExtensionProvider.GetExtension<IAppleExtensions>();
            apple.RestoreTransactions((result) =>
            {
                Debug.Log("RestorePurchases continuing: " + result + ". If no further messages, no purchases available to restore.");
            });
        }
        else
        {
            Debug.Log("RestorePurchases FAIL. Not supported on this platform. Current = " + Application.platform);
        }
    }

    public void OnInitialized(IStoreController controller, IExtensionProvider extensions)
    {
        Debug.Log("OnInitialized: PASS");
        m_StoreController = controller;
        m_StoreExtensionProvider = extensions;
        //foreach (var product in controller.products.all)
        StartCoroutine(InitPrices());
    }

    IEnumerator InitPrices()
    {
        yield return new WaitForSeconds(.25f);
        GemShop shop = GemShop.instance;
        if (shop == null)
            shop = FindObjectOfType<GemShop>();
        for (int i = 1; i < m_StoreController.products.all.Length; i++)
            shop.priceTexts[i].text = m_StoreController.products.all[i].metadata.localizedPriceString;
    }

    public void OnInitializeFailed(InitializationFailureReason error)
    {
        Debug.Log("OnInitializeFailed InitializationFailureReason:" + error);
    }

    public void OnPurchaseFailed(Product product, PurchaseFailureReason failureReason)
    {
        Debug.Log(string.Format("OnPurchaseFailed: FAIL. Product: '{0}', PurchaseFailureReason: {1}", product.definition.storeSpecificId, failureReason));
    }

    public void OnInitializeFailed(InitializationFailureReason error, string message)
    {
        Debug.Log("OnInitializeFailed: " + message);
    }
}