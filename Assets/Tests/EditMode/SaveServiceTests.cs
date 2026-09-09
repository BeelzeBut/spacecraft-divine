using System;
using System.IO;
using NUnit.Framework;

namespace SpaceshipDivine.Save.Tests
{
    public class SaveServiceTests
    {
        private string dir;

        [SetUp]
        public void SetUp()
        {
            dir = Path.Combine(Path.GetTempPath(), "sd_svc_tests_" + Path.GetRandomFileName());
            Directory.CreateDirectory(dir);
        }

        [TearDown]
        public void TearDown()
        {
            if (Directory.Exists(dir)) Directory.Delete(dir, true);
        }

        /// <summary>
        /// Makes both the live file and its backup fail verification, which is the only way to
        /// reach ReadOutcome.RejectedAndReset. Writing garbage rather than deleting matters: a
        /// deleted file gives Exists() == false and takes a completely different branch.
        /// </summary>
        private void CorruptBothCopies()
        {
            File.WriteAllText(Path.Combine(dir, FileSaveStore.SaveFileName), "not json");
            File.WriteAllText(Path.Combine(dir, FileSaveStore.BackupFileName), "not json either");
        }

        private static string LegacyXmlWithEverythingUnlocked()
        {
            var d = new LegacySaveDataV0 { gems = 2500, hasCompletedTutorial = true };
            for (int i = 0; i < LegacyShipIdMap.Count; i++) d.isUnlocked[i] = true;
            return LegacyXmlSerializer.ToXml(d);
        }

        [Test]
        public void FirstLaunchWithNoSaveAndNoLegacyCreatesDefault()
        {
            var svc = new SaveService(new FileSaveStore(dir), () => null, _ => { });
            svc.Load();

            Assert.IsFalse(svc.MigratedThisLoad);
            Assert.AreEqual(350, svc.Profile.gems);
            Assert.AreEqual(3, svc.Profile.unlockedShipIds.Count);
        }

        [Test]
        public void LegacySaveIsMigratedOnFirstLoad()
        {
            var svc = new SaveService(new FileSaveStore(dir), LegacyXmlWithEverythingUnlocked, _ => { });
            svc.Load();

            Assert.IsTrue(svc.MigratedThisLoad);
            Assert.AreEqual(2500, svc.Profile.gems);
            Assert.AreEqual(LegacyShipIdMap.Count, svc.Profile.unlockedShipIds.Count);
        }

        [Test]
        public void MigrationHappensOnceThenReadsTheV1File()
        {
            int legacyReads = 0;
            string legacy = LegacyXmlWithEverythingUnlocked();

            var first = new SaveService(new FileSaveStore(dir),
                () => { legacyReads++; return legacy; }, _ => { });
            first.Load();
            Assert.IsTrue(first.MigratedThisLoad);

            // Second launch: a v1 file now exists, so the legacy provider must not be consulted.
            var second = new SaveService(new FileSaveStore(dir),
                () => { legacyReads++; return legacy; }, _ => { });
            second.Load();

            Assert.IsFalse(second.MigratedThisLoad);
            Assert.AreEqual(1, legacyReads);
            Assert.AreEqual(LegacyShipIdMap.Count, second.Profile.unlockedShipIds.Count);
        }

        [Test]
        public void SaveThenLoadPreservesProfileChanges()
        {
            var svc = new SaveService(new FileSaveStore(dir), () => null, _ => { });
            svc.Load();
            svc.Profile.gems = 8888;
            svc.Profile.Unlock("valiant");
            svc.Save();

            var again = new SaveService(new FileSaveStore(dir), () => null, _ => { });
            again.Load();

            Assert.AreEqual(8888, again.Profile.gems);
            Assert.IsTrue(again.Profile.IsUnlocked("valiant"));
        }

        [Test]
        public void RunStateIsAvailableAndClearable()
        {
            var svc = new SaveService(new FileSaveStore(dir), () => null, _ => { });
            svc.Load();
            svc.Run.selectedShipId = "razor";
            svc.Run.level = 5;
            svc.Run.subLevel = 2;
            svc.Run.respawnsRemaining = 0;
            svc.Run.runInProgress = true;
            svc.Run.enemiesKilled = 42;
            svc.Run.timeSinceGameStarted = 123.4f;
            svc.Run.abilityName = "overdrive";
            svc.Run.abilityLevel = 3;
            svc.Run.upgradeIndices.Add(7);

            svc.Run.Clear();

            Assert.AreEqual("", svc.Run.selectedShipId);
            Assert.AreEqual(1, svc.Run.level);
            Assert.AreEqual(1, svc.Run.subLevel);
            Assert.AreEqual(1, svc.Run.respawnsRemaining);
            Assert.IsFalse(svc.Run.runInProgress);
            Assert.AreEqual(0, svc.Run.enemiesKilled);
            Assert.AreEqual(0f, svc.Run.timeSinceGameStarted);
            Assert.AreEqual("", svc.Run.abilityName);
            Assert.AreEqual(0, svc.Run.abilityLevel);
            Assert.IsEmpty(svc.Run.upgradeIndices);
        }

        [Test]
        public void ConstructorThrowsOnNullStore()
        {
            Assert.Throws<ArgumentNullException>(() => new SaveService(null, () => null, _ => { }));
        }

        [Test]
        public void NullLegacyProviderAndCallbackAreToleratedWithARealStore()
        {
            var svc = new SaveService(new FileSaveStore(dir), null, null);
            svc.Load();

            Assert.AreEqual(350, svc.Profile.gems);
            Assert.AreEqual(3, svc.Profile.unlockedShipIds.Count);
        }
    
        [Test]
        public void RejectedSaveFallsBackToLegacyInsteadOfResetting()
        {
            // Android Auto Backup restores persistentDataPath AND PlayerPrefs onto a new
            // device. The profile's HMAC is salted with the device id, so it fails there -
            // while the legacy blob is plain XML and still perfectly readable.
            var store = new FileSaveStore(dir);
            store.Write(PlayerProfile.CreateDefault());
            CorruptBothCopies();

            var svc = new SaveService(store, LegacyXmlWithEverythingUnlocked, _ => { });
            svc.Load();

            Assert.AreEqual(ReadOutcome.RejectedAndReset, store.LastReadOutcome,
                "the fixture must actually produce a rejection, or this test proves nothing");
            Assert.IsTrue(svc.RecoveredFromLegacyAfterRejection);
            Assert.AreEqual(LegacyShipIdMap.Count, svc.Profile.unlockedShipIds.Count);
        }

        [Test]
        public void RejectedSaveWithNoLegacyKeepsTheResetProfile()
        {
            var store = new FileSaveStore(dir);
            store.Write(PlayerProfile.CreateDefault());
            CorruptBothCopies();

            var svc = new SaveService(store, () => null, _ => { });
            svc.Load();

            Assert.IsFalse(svc.RecoveredFromLegacyAfterRejection);
            Assert.IsNotNull(svc.Profile, "a reset profile is still better than none");
        }

        [Test]
        public void HealthyLoadNeverConsultsTheLegacyBlob()
        {
            // Discriminating on purpose: the legacy provider unlocks every ship, so a guard in
            // the wrong order shows up immediately as an inflated unlock count.
            var store = new FileSaveStore(dir);
            store.Write(PlayerProfile.CreateDefault());   // three starters only

            bool legacyConsulted = false;
            var svc = new SaveService(store,
                () => { legacyConsulted = true; return LegacyXmlWithEverythingUnlocked(); },
                _ => { });
            svc.Load();

            Assert.IsFalse(legacyConsulted, "a healthy save must not read the legacy blob");
            Assert.AreEqual(3, svc.Profile.unlockedShipIds.Count);
        }
}
}
