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

    [Test]
    public void EveryPublishedGemSkuMatchesTheCatalogExactly()
    {
        // Transcribed from GooglePlayProductCatalog.csv. If a SKU is ever renamed in Play
        // Console, this is the test that should fail rather than the purchase, in silence,
        // on a player's phone.
        Assert.AreEqual("com.kodagames.spaceshipdivine.gems1000",  ProductIds.Gems1000);
        Assert.AreEqual("com.kodagames.spaceshipdivine.gems2250",  ProductIds.Gems2250);
        Assert.AreEqual("com.kodagames.spaceshipdivine.gems5000",  ProductIds.Gems5000);
        Assert.AreEqual("com.kodagames.spaceshipdivine.gems10000", ProductIds.Gems10000);
    }

    [Test]
    public void IsKnownRejectsAnythingNotDeclared()
    {
        Assert.IsTrue(ProductIds.IsKnown(ProductIds.Gems5000));
        // The bare id IAPManager used to register. It is in no catalog and must not resolve.
        Assert.IsFalse(ProductIds.IsKnown("removeads"));
        Assert.IsFalse(ProductIds.IsKnown(null));
    }
}
