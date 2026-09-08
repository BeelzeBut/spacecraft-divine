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
            svc.Run.runInProgress = true;
            svc.Run.Clear();

            Assert.IsFalse(svc.Run.runInProgress);
            Assert.AreEqual("", svc.Run.selectedShipId);
        }
    }
}
