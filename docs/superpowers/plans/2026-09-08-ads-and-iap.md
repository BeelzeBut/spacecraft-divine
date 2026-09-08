# Ads & IAP Hardening Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Make in-app purchases actually grant what players pay for, close the paths that give paid content away free, and rebuild ads from a commented-out shell into a working, consent-gated AdMob integration.

**Architecture:** Unity IAP 4.9.3 stays, but product lookup moves from array position to product ID, entitlements are written through `PlayerProfile`, and local receipt validation with Tangle obfuscation is added. Ads are rebuilt as an `AdService` on the existing `Monetization` GameObject wiring, with Google's UMP consent SDK gating initialisation and every ad unit preloaded before it is shown.

**Tech Stack:** Unity 2022.3.14f1, Unity Purchasing 4.9.3, Google Mobile Ads Unity plugin, UMP (User Messaging Platform) consent SDK, ATTrackingManager on iOS.

**Spec:** `docs/superpowers/specs/2026-09-08-production-release-design.md` §4.2, §4.3

## Global Constraints

- **Branch:** `production-release`. **Never push to any remote** until after the diploma defense on 11 Sep 2026. Local commits only.
- **`applicationIdentifier` stays `com.KodaGames.SpaceshipDivine`**, mixed case included. The three published gem SKUs belong to that app.
- **Paid content is granted in exactly one place: a confirmed store callback.** No UI path may set `isUnlocked` or add gems directly for a real-money product. This plan exists partly because that rule is currently broken.
- **No ad SDK call may happen before a consent decision exists.** UMP runs first; on iOS, ATT is sequenced relative to it.
- **Never busy-wait for an ad.** The removed 2021 code contained `while (!Advertisement.IsReady()) continue;`, which would hard-freeze the app. Preload, then show, with an explicit unavailable path.
- **Rewarded video stays available when `removeAds` is owned.** It is opt-in and grants the player something; only interstitials are suppressed.
- Depends on the save plan landing: entitlements are written to `PlayerProfile` (`removeAdsOwned`), so **do not start this plan until `SaveService` is wired into `DataHolder`.**
- Run the suite with `./run-tests.sh`. **Never modify it.** One Unity batchmode process at a time.

## Audit facts this plan responds to

Verified in the current code, not assumed:

| Fact | Location |
|---|---|
| `BuyRemoveAds()` has an **empty method body**; the button calls it and logs "Removed Ads" | `IAPManager.cs` |
| `removeads` has **no branch in `ProcessPurchase`** and is absent from `GooglePlayProductCatalog.csv` | `IAPManager.cs`, catalog |
| `gems10000` is **published in the catalog but unhandled** in code | catalog vs `ProcessPurchase` |
| `InitPrices()` loops `for (i = 1; ...)` over `products.all` and indexes `shop.priceTexts[i]` — skips product 0 and depends on store return order | `IAPManager.cs:174` |
| `GemShop.priceTexts` is a bare `Text[]`, positionally coupled to store order | `GemShop.cs` |
| `GemShop.Update()` writes `gemsText` every frame and will NRE if `DataHolder.instance` is null | `GemShop.cs` |
| `UnlockShip()`'s `canBeBoughtWithCurrency` branch sets `isUnlocked = true` with **no purchase call** — Bat-Oh-No (€9.99) is given away on tap | `MainMenu.cs` |
| No receipt validation; `Assets/Scripts/UnityPurchasing/` does not exist | — |
| `Monetization.cs` is entirely commented out, written against the Unity Ads **3.x** `IUnityAdsListener` API that no longer compiles | `Monetization.cs` |
| The `Monetization` component is attached in **8 places** (Main Menu, MainMenuCanvas, 4 level Managers, Ability Manager) | scenes/prefabs |
| `LevelLoader`'s `countToAd == 0` branch bypasses the loading screen entirely and never resets the counter | `LevelLoader.cs` |
| `RestorePurchases.cs` is fully commented out | — |

---

### Task 1: Product registry — look products up by ID, not position

**Files:**
- Create: `Assets/Scripts/Store/ProductIds.cs`
- Modify: `Assets/Scripts/Object Managers/IAPManager.cs`
- Modify: `Assets/Scripts/Main Menu/GemShop.cs`
- Create: `Assets/Tests/EditMode/ProductIdsTests.cs`

**Interfaces:**
- Produces:
  - `ProductIds.RemoveAds`, `.Gems1000`, `.Gems2250`, `.Gems5000`, `.Gems10000`
  - `ProductIds.GemsFor(string productId) -> int` (0 if not a gem product)
  - `ProductIds.All` — every product this build registers
  - `GemShop.SetPrice(string productId, string localizedPrice)`

- [ ] **Step 1: Write the failing tests**

```csharp
using NUnit.Framework;

public class ProductIdsTests
{
    [Test]
    public void GemProductsMapToTheirAdvertisedAmounts()
    {
        Assert.AreEqual(1000,  ProductIds.GemsFor(ProductIds.Gems1000));
        Assert.AreEqual(2250,  ProductIds.GemsFor(ProductIds.Gems2250));
        Assert.AreEqual(5000,  ProductIds.GemsFor(ProductIds.Gems5000));
        Assert.AreEqual(10000, ProductIds.GemsFor(ProductIds.Gems10000));
    }

    [Test]
    public void NonGemProductsYieldZeroGems()
    {
        Assert.AreEqual(0, ProductIds.GemsFor(ProductIds.RemoveAds));
        Assert.AreEqual(0, ProductIds.GemsFor("com.example.nonsense"));
        Assert.AreEqual(0, ProductIds.GemsFor(null));
        Assert.AreEqual(0, ProductIds.GemsFor(""));
    }

    [Test]
    public void AllContainsEveryDeclaredProductExactlyOnce()
    {
        Assert.AreEqual(5, ProductIds.All.Count);
        CollectionAssert.AllItemsAreUnique(ProductIds.All);
        CollectionAssert.Contains(ProductIds.All, ProductIds.RemoveAds);
        CollectionAssert.Contains(ProductIds.All, ProductIds.Gems10000);
    }

    [Test]
    public void ProductIdsAreLowercaseAsPublished()
    {
        // The store SKUs were published lowercase; a case mismatch silently fails lookup.
        foreach (string id in ProductIds.All)
            Assert.AreEqual(id.ToLowerInvariant(), id, "product id must be lowercase: " + id);
    }
}
```

- [ ] **Step 2: Run to confirm failure**

Run: `./run-tests.sh` — expect a compile error.

- [ ] **Step 3: Write the registry**

```csharp
using System.Collections.Generic;

/// <summary>
/// The published store SKUs. These strings are CONTRACTS with Google Play and App Store
/// Connect — changing one orphans every purchase already made against it.
/// </summary>
public static class ProductIds
{
    public const string RemoveAds  = "removeads";
    public const string Gems1000   = "com.kodagames.spaceshipdivine.gems1000";
    public const string Gems2250   = "com.kodagames.spaceshipdivine.gems2250";
    public const string Gems5000   = "com.kodagames.spaceshipdivine.gems5000";
    public const string Gems10000  = "com.kodagames.spaceshipdivine.gems10000";

    public static readonly IReadOnlyList<string> All = new[]
    {
        RemoveAds, Gems1000, Gems2250, Gems5000, Gems10000
    };

    /// <summary>Gems granted by a product, or 0 if it is not a gem product.</summary>
    public static int GemsFor(string productId)
    {
        switch (productId)
        {
            case Gems1000:  return 1000;
            case Gems2250:  return 2250;
            case Gems5000:  return 5000;
            case Gems10000: return 10000;
            default:        return 0;
        }
    }
}
```

- [ ] **Step 4: Replace positional price wiring**

In `GemShop.cs`, replace the bare `Text[] priceTexts` coupling with an explicit id→label map, and fix the per-frame `Update()`:

```csharp
    [System.Serializable]
    public class PriceLabel
    {
        public string productId;
        public Text label;
    }

    public List<PriceLabel> priceLabels = new List<PriceLabel>();

    /// <summary>Called per product by IAPManager once the store reports back.</summary>
    public void SetPrice(string productId, string localizedPrice)
    {
        foreach (PriceLabel entry in priceLabels)
            if (entry.productId == productId && entry.label != null)
                entry.label.text = localizedPrice;
    }

    /// <summary>Called when the gem balance changes — no longer polled every frame.</summary>
    public void RefreshGems()
    {
        if (gemsText != null && DataHolder.instance != null)
            gemsText.text = DataHolder.instance.gems.ToString();
    }
```

Delete `Update()`. Call `RefreshGems()` from `OnEnable()` and from wherever gems change.

In `IAPManager.InitPrices()`, replace the index loop with an id lookup:

```csharp
    IEnumerator InitPrices()
    {
        yield return new WaitForSeconds(.25f);

        GemShop shop = GemShop.instance ?? FindObjectOfType<GemShop>();
        if (shop == null) yield break;

        foreach (string id in ProductIds.All)
        {
            Product product = m_StoreController.products.WithID(id);
            if (product == null || !product.availableToPurchase)
            {
                Debug.LogWarning("Store did not return product: " + id);
                continue;
            }
            shop.SetPrice(id, product.metadata.localizedPriceString);
        }
    }
```

Register products from `ProductIds.All` in `InitializePurchasing()` — `RemoveAds` as `NonConsumable`, the rest as `Consumable`.

- [ ] **Step 5: Wire the Inspector references headlessly**

Extend `Assets/Editor/SdAutomation.cs` with an entry point that populates `GemShop.priceLabels` from the existing `priceTexts` array so the mapping is explicit and reviewable, then run it. Report the resulting mapping.

- [ ] **Step 6: Run tests and commit**

```bash
./run-tests.sh
git add Assets/Scripts/Store Assets/Scripts/Object\ Managers/IAPManager.cs \
        Assets/Scripts/Main\ Menu/GemShop.cs Assets/Tests Assets/Editor
git commit -m "Achizitii: identificarea produselor dupa ID, nu dupa pozitie in lista"
```

---

### Task 2: Make `removeads` actually do something

`BuyRemoveAds()` is an empty method behind a live button.

**Files:**
- Modify: `Assets/Scripts/Object Managers/IAPManager.cs`
- Modify: `GooglePlayProductCatalog.csv`
- Create: `Assets/Tests/EditMode/EntitlementTests.cs`

**Interfaces:**
- Consumes: `ProductIds` (Task 1); `PlayerProfile.removeAdsOwned` (save plan Task 3).
- Produces: `Entitlements.Grant(PlayerProfile, string productId) -> bool`

The grant logic is extracted into a pure function so it can be tested without a store.

- [ ] **Step 1: Write the failing tests**

```csharp
using NUnit.Framework;
using SpaceshipDivine.Save;

public class EntitlementTests
{
    [Test]
    public void RemoveAdsSetsTheOwnedFlag()
    {
        var p = PlayerProfile.CreateDefault();
        Assert.IsFalse(p.removeAdsOwned);
        Assert.IsTrue(Entitlements.Grant(p, ProductIds.RemoveAds));
        Assert.IsTrue(p.removeAdsOwned);
    }

    [Test]
    public void RemoveAdsIsIdempotent()
    {
        var p = PlayerProfile.CreateDefault();
        Entitlements.Grant(p, ProductIds.RemoveAds);
        int gemsBefore = p.gems;
        Assert.IsTrue(Entitlements.Grant(p, ProductIds.RemoveAds));
        Assert.IsTrue(p.removeAdsOwned);
        Assert.AreEqual(gemsBefore, p.gems, "re-granting must not also hand out gems");
    }

    [Test]
    public void EachGemProductAddsItsAdvertisedAmount()
    {
        var p = PlayerProfile.CreateDefault();
        int start = p.gems;
        Entitlements.Grant(p, ProductIds.Gems1000);
        Assert.AreEqual(start + 1000, p.gems);
        Entitlements.Grant(p, ProductIds.Gems10000);
        Assert.AreEqual(start + 11000, p.gems);
    }

    [Test]
    public void UnknownProductGrantsNothingAndReportsFailure()
    {
        var p = PlayerProfile.CreateDefault();
        int start = p.gems;
        Assert.IsFalse(Entitlements.Grant(p, "com.example.unknown"));
        Assert.IsFalse(Entitlements.Grant(p, null));
        Assert.AreEqual(start, p.gems);
        Assert.IsFalse(p.removeAdsOwned);
    }
}
```

- [ ] **Step 2: Run to confirm failure, then implement**

```csharp
using SpaceshipDivine.Save;

/// <summary>
/// The ONLY place a purchase turns into player value. Pure and store-agnostic so it can be
/// tested without a store connection, and so there is exactly one audited grant path.
/// </summary>
public static class Entitlements
{
    /// <returns>true if the product was recognised and granted.</returns>
    public static bool Grant(PlayerProfile profile, string productId)
    {
        if (profile == null || string.IsNullOrEmpty(productId)) return false;

        if (productId == ProductIds.RemoveAds)
        {
            profile.removeAdsOwned = true;
            return true;
        }

        int gems = ProductIds.GemsFor(productId);
        if (gems > 0)
        {
            profile.gems += gems;
            return true;
        }

        return false;
    }
}
```

Rewrite `ProcessPurchase` to call it, and `BuyRemoveAds()` to actually initiate the purchase:

```csharp
    public void BuyRemoveAds() => BuyProductID(ProductIds.RemoveAds);

    public PurchaseProcessingResult ProcessPurchase(PurchaseEventArgs args)
    {
        string id = args.purchasedProduct.definition.id;

        if (!Entitlements.Grant(DataHolder.instance.Profile, id))
        {
            Debug.LogWarning("Unrecognised product in ProcessPurchase: " + id);
            return PurchaseProcessingResult.Complete;   // do not retry forever
        }

        DataHolder.instance.Save();
        DataHolder.instance.RefreshAfterPurchase();
        return PurchaseProcessingResult.Complete;
    }
```

- [ ] **Step 3: Add the SKU to the catalog**

Append to `GooglePlayProductCatalog.csv`:

```
removeads,published,managed_by_android,false,en_US;Remove ads;Removes interstitial ads and grants an extra revive,false,,Remove ads
```

`removeads` is a **non-consumable** — `managed_by_android` is the managed-product type; confirm it is registered as `ProductType.NonConsumable` in code, and that the Play Console product is a one-time product, not consumable.

- [ ] **Step 4: Run tests and commit**

---

### Task 3: Close the free-ship path and add receipt validation

**Files:**
- Modify: `Assets/Scripts/Object Managers/IAPManager.cs`
- Modify: `Assets/Scripts/Main Menu/MainMenu.cs`
- Create: `Assets/Scripts/UnityPurchasing/generated/` (Tangle, generated by Unity)

- [ ] **Step 1: Generate Tangle obfuscation**

In the Unity Editor: **Services → In-App Purchasing → Receipt Validation Obfuscator**, supply the Google Play licence key from the Play Console, and generate. This writes `Assets/Scripts/UnityPurchasing/generated/`. It is an Editor-menu action with no batchmode equivalent — if it cannot be automated, say so and stop rather than skipping validation.

- [ ] **Step 2: Validate receipts before granting**

```csharp
    private bool ReceiptIsValid(PurchaseEventArgs args)
    {
#if UNITY_EDITOR
        return true;   // no real receipts in the editor
#else
        try
        {
            var validator = new CrossPlatformValidator(
                GooglePlayTangle.Data(), AppleTangle.Data(), Application.identifier);
            validator.Validate(args.purchasedProduct.receipt);
            return true;
        }
        catch (IAPSecurityException e)
        {
            Debug.LogWarning("Receipt rejected: " + e.Message);
            return false;
        }
#endif
    }
```

Call it at the top of `ProcessPurchase`; on failure, log and return `Complete` without granting.

- [ ] **Step 3: Route the real-money ship through IAP**

The save plan's Task 10 already replaced the free grant with `IAPManager.instance.BuyShip(shipId)`, a stub that fails closed. Implement it now: map `bat_oh_no` to a store product, purchase it, and grant only from `ProcessPurchase` via `MainMenu.GrantShipAfterPurchase(shipId)`.

**A decision is required before this task can complete:** Bat-Oh-No has no published SKU. Either create one in both stores, or convert the ship to a gem purchase. Report which is chosen and why; do not invent a SKU silently.

- [ ] **Step 4: Restore Purchases**

Rewrite `RestorePurchases.cs` (currently fully commented out) and surface the button. **App Store review requires this for non-consumables** — its absence is a rejection.

- [ ] **Step 5: Run tests and commit**

---

### Task 4: AdMob, consent, and the AdService

**Files:**
- Modify: `Packages/manifest.json` (add Google Mobile Ads; remove `com.unity.ads`)
- Rewrite: `Assets/Scripts/Object Managers/Monetization.cs` → `AdService`
- Modify: `Assets/Scripts/Object Managers/LevelLoader.cs`
- Modify: `Assets/Scripts/Object Managers/GameManager.cs`
- Modify: `Assets/Scripts/Main Menu/MainMenu.cs`

**Interfaces:**
- Produces:
  - `AdService.Instance`
  - `AdService.ConsentReady` → `bool`
  - `AdService.IsRewardedReady` / `IsInterstitialReady`
  - `AdService.ShowRewarded(Action onEarned, Action onUnavailable)`
  - `AdService.ShowInterstitial()` — no-op when `removeAdsOwned`

- [ ] **Step 1: Keep the class name to preserve scene wiring**

The `Monetization` component is referenced in 8 scenes and prefabs. Either keep the type name `Monetization` and rewrite its body, or rename and run a scene sweep via `SdAutomation`. **Keeping the name is strongly preferred** — a missed reference is a silent null on a level Manager prefab. State which you did.

- [ ] **Step 2: Consent first, always**

UMP must complete before any ad request. On iOS, request ATT after UMP and before the first ad load. No ad SDK initialisation may occur before a consent decision exists.

- [ ] **Step 3: Preload, never busy-wait**

Each unit is loaded ahead of time and reloaded after being shown. `ShowRewarded` takes an explicit `onUnavailable` callback. **Do not reproduce** the 2021 `while (!Advertisement.IsReady()) continue;` pattern.

- [ ] **Step 4: Wire the three call sites**

| Call site | Behaviour |
|---|---|
| `MainMenu.PlayRewardedAd()` | Rewarded → `profile.gems += DataHolder.videoRewardGems` (200) on the completion callback only |
| `GameManager.ReviveWithAd()` | Rewarded → `RevivePlayer()` on completion only |
| `LevelLoader.LoadLevel()` | Interstitial every 3rd load via `countToAd` |

- [ ] **Step 5: Fix the LevelLoader counter bug**

The `countToAd == 0` branch currently skips the loading screen entirely and never resets the counter. Both paths must show the loading screen; the counter must always reset after an interstitial attempt, whether or not an ad was available.

- [ ] **Step 6: Suppress interstitials for `removeAds` owners**

`ShowInterstitial()` returns immediately when `profile.removeAdsOwned`. **Rewarded video stays available** — it is opt-in and grants the player something.

- [ ] **Step 7: Run tests and commit**

---

### Task 5: On-device verification

Cannot be completed by an agent alone — ad fill, consent forms and the ATT prompt need a real device and a real network.

- [ ] **Step 1: Verify with AdMob test ad unit IDs first.** Never test against production units; Google suspends accounts for self-clicks.
- [ ] **Step 2: Confirm the UMP form appears** on a fresh install with an EEA locale, and that no ad request precedes it (check with a proxy or `adb logcat`).
- [ ] **Step 3: Confirm the ATT prompt** appears on iOS in the correct order relative to UMP.
- [ ] **Step 4: Confirm rewarded completion grants**, and that dismissing early grants nothing.
- [ ] **Step 5: Confirm a sandbox purchase** of each SKU grants the right amount, survives an app restart, and that Restore Purchases returns `removeads`.
- [ ] **Step 6: Confirm `removeAds` suppresses interstitials** but leaves rewarded video working.

---

## Out of scope for this plan

| Deferred | Why |
|---|---|
| Server-side receipt validation | Belongs to the online-services plan; a Cloud Function replaces local validation |
| Ad mediation / waterfall | No volume to justify it yet |
| Play Data Safety, ATT purpose strings, content ratings | Legal attestations — Marco is the declarant |
| Creating AdMob ad units | No create API; console UI, driven with Playwright when needed |
| Economy tuning of `videoRewardGems` and revive costs | Needs live analytics data |
