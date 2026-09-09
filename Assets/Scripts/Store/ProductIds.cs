using System;
using System.Collections.Generic;

/// <summary>
/// The store SKUs this build registers. These strings are CONTRACTS with Google Play and App
/// Store Connect: changing one orphans every purchase already made against it.
///
/// Global namespace on purpose, matching SaveData — it is referenced from Assembly-CSharp all
/// over, and lives in its own assembly only so it can be unit tested.
///
/// Verified against GooglePlayProductCatalog.csv and Assets/Resources/IAPProductCatalog.json
/// rather than assumed. Those two and IAPManager disagreed in three ways before this file:
///
///   1. IAPManager registered the bare id "removeads". Neither catalog contains that string;
///      IAPProductCatalog.json declares "com.kodagames.spaceshipdivine.removeads". A SKU the
///      store has never heard of can never be purchased, which is why BuyRemoveAds could not
///      have worked even with a body.
///   2. gems10000 is published in the Play CSV but was never registered in code, so the store
///      offered a product the app could not sell.
///   3. removeads appears in neither the Play CSV nor App Store Connect — it has no published
///      SKU anywhere. It must be created before it can be sold.
///
/// The fully-qualified form is used here because all four gem SKUs carry the reverse-DNS
/// prefix, making the bare "removeads" the outlier. Nothing has been purchased against either
/// spelling, so there is nothing to orphan — but the id created in Play Console must match
/// this string EXACTLY, or the purchase silently fails to resolve.
/// </summary>
public static class ProductIds
{
    private const string Prefix = "com.kodagames.spaceshipdivine.";

    public const string RemoveAds = Prefix + "removeads";
    public const string Gems1000  = Prefix + "gems1000";
    public const string Gems2250  = Prefix + "gems2250";
    public const string Gems5000  = Prefix + "gems5000";
    public const string Gems10000 = Prefix + "gems10000";

    private static readonly string[] all =
    {
        RemoveAds, Gems1000, Gems2250, Gems5000, Gems10000
    };

    public static IReadOnlyList<string> All => Array.AsReadOnly(all);

    /// <summary>
    /// How many gems a product grants, or 0 when it grants none. Returning an amount rather
    /// than branching per id is what stops a new gem SKU from being registered and then
    /// silently granting nothing — the failure mode gems10000 already had.
    /// </summary>
    public static int GemsFor(string productId)
    {
        if (string.IsNullOrEmpty(productId)) return 0;

        switch (productId)
        {
            case Gems1000:  return 1000;
            case Gems2250:  return 2250;
            case Gems5000:  return 5000;
            case Gems10000: return 10000;
            default:        return 0;
        }
    }

    public static bool IsKnown(string productId)
    {
        if (string.IsNullOrEmpty(productId)) return false;
        for (int i = 0; i < all.Length; i++)
            if (all[i] == productId) return true;
        return false;
    }
}
