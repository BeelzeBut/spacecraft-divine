using NUnit.Framework;

namespace SpaceshipDivine.Save.Tests
{
    public class MigrationV0ToV1Tests
    {
        /// <summary>Builds legacy XML with the given unlock flags and gem count.</summary>
        private static string LegacyXml(bool[] unlocked, int gems, int shipLevelAtIndex1 = 1)
        {
            var data = new LegacySaveDataV0 { gems = gems, hasCompletedTutorial = true };
            for (int i = 0; i < unlocked.Length && i < data.isUnlocked.Length; i++)
                data.isUnlocked[i] = unlocked[i];
            data.shipLevel[1] = shipLevelAtIndex1;

            // Ship design values that must NOT survive migration.
            data.maxHealths[0] = 12345f;
            data.priceToUnlock[0] = 99999f;

            return LegacyXmlSerializer.ToXml(data);
        }

        [Test]
        public void EveryLegacyUnlockIsPreserved()
        {
            var flags = new bool[LegacyShipIdMap.Count];
            for (int i = 0; i < flags.Length; i++) flags[i] = true;   // the old "everything unlocked" save

            PlayerProfile p = MigrationV0ToV1.FromXml(LegacyXml(flags, 1200));

            Assert.IsNotNull(p);
            for (int i = 0; i < LegacyShipIdMap.Count; i++)
                Assert.IsTrue(p.IsUnlocked(LegacyShipIdMap.IndexToId(i)),
                    "lost unlock for " + LegacyShipIdMap.IndexToId(i));
        }

        [Test]
        public void PartialUnlocksMigrateAndStartersAreAddedAsAFloor()
        {
            var flags = new bool[LegacyShipIdMap.Count];
            flags[4] = true;   // razor (not a starter)
            flags[3] = true;   // the_argon (also a starter)

            PlayerProfile p = MigrationV0ToV1.FromXml(LegacyXml(flags, 500));

            // What the legacy save had.
            Assert.IsTrue(p.IsUnlocked("razor"));
            Assert.IsTrue(p.IsUnlocked("the_argon"));
            // Plus the starter floor, so a migrated player is never worse off than a new one.
            Assert.IsTrue(p.IsUnlocked("grey_byrd"));
            Assert.IsTrue(p.IsUnlocked("apollo"));
            // And nothing beyond that.
            Assert.AreEqual(4, p.unlockedShipIds.Count);
            Assert.IsFalse(p.IsUnlocked("valiant"));
        }

        [Test]
        public void GemsAndTutorialFlagsCarryForward()
        {
            PlayerProfile p = MigrationV0ToV1.FromXml(LegacyXml(new bool[LegacyShipIdMap.Count], 4321));
            Assert.AreEqual(4321, p.gems);
            Assert.IsTrue(p.hasCompletedTutorial);
        }

        [Test]
        public void ShipLevelsCarryForward()
        {
            PlayerProfile p = MigrationV0ToV1.FromXml(
                LegacyXml(new bool[LegacyShipIdMap.Count], 0, shipLevelAtIndex1: 4));
            Assert.AreEqual(4, p.GetShipLevel(LegacyShipIdMap.IndexToId(1)));
        }

        [Test]
        public void ShipDesignDataIsDiscarded()
        {
            // PlayerProfile has no field capable of holding ship stats. This test documents
            // that intent: if someone adds one, it fails to compile here first.
            PlayerProfile p = MigrationV0ToV1.FromXml(LegacyXml(new bool[LegacyShipIdMap.Count], 0));
            Assert.IsNotNull(p);
            Assert.IsNull(typeof(PlayerProfile).GetField("maxHealths"));
            Assert.IsNull(typeof(PlayerProfile).GetField("priceToUnlock"));
        }

        [Test]
        public void CollectedBlueprintsArePreserved()
        {
            // hasBeenUnlocked[] records which blueprint pickups already happened. Losing it
            // would make already-collected blueprints drop again.
            var d = new LegacySaveDataV0 { gems = 0 };
            d.hasBeenUnlocked[4] = true;
            d.hasBeenUnlocked[11] = true;

            PlayerProfile p = MigrationV0ToV1.FromXml(LegacyXmlSerializer.ToXml(d));

            Assert.IsTrue(p.HasCollectedBlueprint(4));
            Assert.IsTrue(p.HasCollectedBlueprint(11));
            Assert.IsFalse(p.HasCollectedBlueprint(5));
        }

        [Test]
        public void RedeemableBlueprintsArePreserved()
        {
            // The legacy build overloaded priceToUnlock[i] == 1 to mean "blueprint collected,
            // ship now claimable". Discarding it would revoke a ship the player earned.
            var d = new LegacySaveDataV0 { gems = 0 };
            int vickers = -1;
            for (int i = 0; i < LegacyShipIdMap.Count; i++)
                if (LegacyShipIdMap.IndexToId(i) == "vickers") vickers = i;
            Assert.AreNotEqual(-1, vickers, "vickers missing from LegacyShipIdMap");
            d.priceToUnlock[vickers] = 1f;

            PlayerProfile p = MigrationV0ToV1.FromXml(LegacyXmlSerializer.ToXml(d));

            Assert.IsTrue(p.IsBlueprintRedeemable("vickers"));
            Assert.IsFalse(p.IsBlueprintRedeemable("warspite"));
        }

        [Test]
        public void GarbageXmlReturnsNullRatherThanThrowing()
        {
            Assert.IsNull(MigrationV0ToV1.FromXml("not xml at all"));
            Assert.IsNull(MigrationV0ToV1.FromXml(""));
            Assert.IsNull(MigrationV0ToV1.FromXml(null));
        }

        [Test]
        public void MigratedProfileNeverHasFewerUnlocksThanTheLegacySave()
        {
            var flags = new bool[LegacyShipIdMap.Count];
            flags[2] = true;
            flags[5] = true;
            flags[9] = true;

            PlayerProfile p = MigrationV0ToV1.FromXml(LegacyXml(flags, 0));

            int legacyCount = 0;
            foreach (bool f in flags) if (f) legacyCount++;
            Assert.GreaterOrEqual(p.unlockedShipIds.Count, legacyCount);
        }
    }
}
