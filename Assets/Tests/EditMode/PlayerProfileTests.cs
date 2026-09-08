using NUnit.Framework;
using UnityEngine;

namespace SpaceshipDivine.Save.Tests
{
    public class PlayerProfileTests
    {
        [Test]
        public void DefaultProfileUnlocksExactlyTheThreeStarters()
        {
            var p = PlayerProfile.CreateDefault();
            Assert.IsTrue(p.IsUnlocked("grey_byrd"));
            Assert.IsTrue(p.IsUnlocked("apollo"));
            Assert.IsTrue(p.IsUnlocked("the_argon"));
            Assert.IsFalse(p.IsUnlocked("valiant"));
            Assert.IsFalse(p.IsUnlocked("lunar_hunter"));
            Assert.AreEqual(3, p.unlockedShipIds.Count);
        }

        [Test]
        public void DefaultProfileStartsWith350Gems()
        {
            Assert.AreEqual(350, PlayerProfile.CreateDefault().gems);
        }

        [Test]
        public void UnlockIsIdempotent()
        {
            var p = PlayerProfile.CreateDefault();
            p.Unlock("valiant");
            p.Unlock("valiant");
            Assert.IsTrue(p.IsUnlocked("valiant"));
            Assert.AreEqual(4, p.unlockedShipIds.Count);
        }

        [Test]
        public void ShipLevelDefaultsToOneAndRoundTrips()
        {
            var p = PlayerProfile.CreateDefault();
            Assert.AreEqual(1, p.GetShipLevel("apollo"));
            p.SetShipLevel("apollo", 4);
            Assert.AreEqual(4, p.GetShipLevel("apollo"));
        }

        [Test]
        public void ShipLevelIsClampedToOneThroughFive()
        {
            var p = PlayerProfile.CreateDefault();
            p.SetShipLevel("apollo", 99);
            Assert.AreEqual(5, p.GetShipLevel("apollo"));
            p.SetShipLevel("apollo", -3);
            Assert.AreEqual(1, p.GetShipLevel("apollo"));
        }

        [Test]
        public void BlueprintCollectionIsRecordedAndIdempotent()
        {
            var p = PlayerProfile.CreateDefault();
            Assert.IsFalse(p.HasCollectedBlueprint(7));
            p.CollectBlueprint(7);
            p.CollectBlueprint(7);
            Assert.IsTrue(p.HasCollectedBlueprint(7));
            Assert.AreEqual(1, p.collectedBlueprintIndices.Count);
        }

        [Test]
        public void BlueprintRedeemableIsRecordedAndIdempotent()
        {
            var p = PlayerProfile.CreateDefault();
            Assert.IsFalse(p.IsBlueprintRedeemable("vickers"));
            p.MarkBlueprintRedeemable("vickers");
            p.MarkBlueprintRedeemable("vickers");
            Assert.IsTrue(p.IsBlueprintRedeemable("vickers"));
            Assert.AreEqual(1, p.blueprintRedeemableShipIds.Count);
        }

        [Test]
        public void RedeemableIsNotTheSameAsUnlocked()
        {
            // A collected blueprint makes a ship claimable; the player still has to claim it.
            var p = PlayerProfile.CreateDefault();
            p.MarkBlueprintRedeemable("warspite");
            Assert.IsTrue(p.IsBlueprintRedeemable("warspite"));
            Assert.IsFalse(p.IsUnlocked("warspite"));
        }

        [Test]
        public void ProfileSurvivesJsonRoundTrip()
        {
            var p = PlayerProfile.CreateDefault();
            p.gems = 4200;
            p.Unlock("razor");
            p.SetShipLevel("razor", 3);
            p.removeAdsOwned = true;
            p.stats.totalEnemiesKilled = 812;

            var back = JsonUtility.FromJson<PlayerProfile>(JsonUtility.ToJson(p));

            Assert.AreEqual(4200, back.gems);
            Assert.IsTrue(back.IsUnlocked("razor"));
            Assert.AreEqual(3, back.GetShipLevel("razor"));
            Assert.IsTrue(back.removeAdsOwned);
            Assert.AreEqual(812, back.stats.totalEnemiesKilled);
        }

        [Test]
        public void RunStateSurvivesJsonRoundTrip()
        {
            var r = new RunState
            {
                selectedShipId = "hot_talon",
                level = 2,
                subLevel = 3,
                respawnsRemaining = 1,
                runInProgress = true,
                abilityName = "Shield",
                abilityLevel = 2,
                enemiesKilled = 44
            };
            r.upgradeIndices.Add(7);
            r.upgradeIndices.Add(12);

            var back = JsonUtility.FromJson<RunState>(JsonUtility.ToJson(r));

            Assert.AreEqual("hot_talon", back.selectedShipId);
            Assert.AreEqual(2, back.level);
            Assert.AreEqual(3, back.subLevel);
            Assert.IsTrue(back.runInProgress);
            Assert.AreEqual(2, back.upgradeIndices.Count);
            Assert.AreEqual(12, back.upgradeIndices[1]);
        }
    }
}
