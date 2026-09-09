using NUnit.Framework;

namespace SpaceshipDivine.Save.Tests
{
    public class ProfileProjectionTests
    {
        private static int IndexOf(string shipId)
        {
            int i = LegacyShipIdMap.IdToIndex(shipId);
            Assert.GreaterOrEqual(i, 0, "fixture names a ship the map does not know: " + shipId);
            return i;
        }

        [Test]
        public void UnlocksProjectOntoTheCorrectIndices()
        {
            var profile = PlayerProfile.CreateDefault();
            profile.Unlock("valiant");
            var data = new SaveData();

            ProfileProjection.ApplyToSaveData(profile, data);

            Assert.IsTrue(data.isUnlocked[IndexOf("valiant")]);
            Assert.IsFalse(data.isUnlocked[IndexOf("razor")], "an unowned ship must stay locked");
        }

        [Test]
        public void CaptureThenApplyRoundTripsUnlocks()
        {
            var profile = PlayerProfile.CreateDefault();
            var data = new SaveData();
            data.isUnlocked[IndexOf("hot_talon")] = true;

            ProfileProjection.CaptureFromSaveData(data, profile);

            Assert.IsTrue(profile.IsUnlocked("hot_talon"));
        }

        [Test]
        public void CaptureNeverRemovesAnUnlockTheProfileAlreadyHad()
        {
            // The rule the whole cutover rests on. A gameplay scene builds a SaveData with
            // fewer unlocks than the profile holds; capturing it must not revoke anything.
            var profile = PlayerProfile.CreateDefault();
            profile.Unlock("bat_oh_no");          // the EUR 9.99 ship
            var data = new SaveData();            // every isUnlocked false

            ProfileProjection.CaptureFromSaveData(data, profile);

            Assert.IsTrue(profile.IsUnlocked("bat_oh_no"),
                "a paid unlock must never be revoked by projection");
        }

        [Test]
        public void ShipLevelsSurviveBothDirections()
        {
            // 3, not 1: 1 is both the SaveData default and PlayerProfile's default, so a
            // fixture of 1 would pass even if nothing were copied at all.
            var profile = PlayerProfile.CreateDefault();
            profile.SetShipLevel("apollo", 3);
            var data = new SaveData();

            ProfileProjection.ApplyToSaveData(profile, data);
            Assert.AreEqual(3, data.shipLevel[IndexOf("apollo")]);

            var roundTripped = PlayerProfile.CreateDefault();
            ProfileProjection.CaptureFromSaveData(data, roundTripped);
            Assert.AreEqual(3, roundTripped.GetShipLevel("apollo"));
        }

        [Test]
        public void GemsAndSettingsProjectBothWays()
        {
            var profile = PlayerProfile.CreateDefault();
            profile.gems = 1234;
            profile.settings.isHapticsMuted = true;
            profile.settings.qualityIndex = 1;
            var data = new SaveData();

            ProfileProjection.ApplyToSaveData(profile, data);
            Assert.AreEqual(1234, data.gems);
            Assert.IsTrue(data.isHapticsMuted);
            Assert.AreEqual(1, data.qualityIndex);

            data.gems = 99;
            data.isHapticsMuted = false;
            var back = PlayerProfile.CreateDefault();
            ProfileProjection.CaptureFromSaveData(data, back);
            Assert.AreEqual(99, back.gems);
            Assert.IsFalse(back.settings.isHapticsMuted);
        }

        [Test]
        public void BlueprintRedeemableShipsBecomeThePriceSentinel()
        {
            // priceToUnlock == 1 is the legacy boolean "blueprint collected, ship claimable".
            var profile = PlayerProfile.CreateDefault();
            profile.MarkBlueprintRedeemable("vickers");
            var data = new SaveData();

            ProfileProjection.ApplyToSaveData(profile, data);

            Assert.AreEqual(1f, data.priceToUnlock[IndexOf("vickers")]);
        }

        [Test]
        public void CollectedBlueprintsRoundTrip()
        {
            var profile = PlayerProfile.CreateDefault();
            profile.CollectBlueprint(7);
            var data = new SaveData();

            ProfileProjection.ApplyToSaveData(profile, data);
            Assert.IsTrue(data.hasBeenUnlocked[7]);

            var back = PlayerProfile.CreateDefault();
            ProfileProjection.CaptureFromSaveData(data, back);
            Assert.IsTrue(back.HasCollectedBlueprint(7));
        }

        [Test]
        public void ProjectionToleratesAnIdTheMapDoesNotKnow()
        {
            // SaveData's arrays are fixed at 50/100. A profile naming a ship outside the map
            // must be skipped, not throw — a crash here is a player who cannot launch at all.
            var profile = PlayerProfile.CreateDefault();
            profile.Unlock("a_ship_that_does_not_exist");
            profile.CollectBlueprint(9999);
            var data = new SaveData();

            Assert.DoesNotThrow(() => ProfileProjection.ApplyToSaveData(profile, data));
        }

        [Test]
        public void NullArgumentsAreToleratedRatherThanThrowing()
        {
            var data = new SaveData();
            var profile = PlayerProfile.CreateDefault();
            Assert.DoesNotThrow(() => ProfileProjection.ApplyToSaveData(null, data));
            Assert.DoesNotThrow(() => ProfileProjection.ApplyToSaveData(profile, null));
            Assert.DoesNotThrow(() => ProfileProjection.CaptureFromSaveData(null, profile));
            Assert.DoesNotThrow(() => ProfileProjection.CaptureFromSaveData(data, null));
        }
    }
}
