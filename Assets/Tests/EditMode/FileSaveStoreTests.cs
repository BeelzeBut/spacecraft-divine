using System.IO;
using NUnit.Framework;
using UnityEngine;

namespace SpaceshipDivine.Save.Tests
{
    public class FileSaveStoreTests
    {
        private string dir;

        [SetUp]
        public void SetUp()
        {
            dir = Path.Combine(Path.GetTempPath(), "sd_save_tests_" + Path.GetRandomFileName());
            Directory.CreateDirectory(dir);
        }

        [TearDown]
        public void TearDown()
        {
            if (Directory.Exists(dir)) Directory.Delete(dir, true);
        }

        [Test]
        public void ReadWithNoFileReturnsDefaultProfile()
        {
            var store = new FileSaveStore(dir);
            PlayerProfile p = store.Read();

            Assert.AreEqual(ReadOutcome.Fresh, store.LastReadOutcome);
            Assert.AreEqual(350, p.gems);
            Assert.IsTrue(p.IsUnlocked("apollo"));
        }

        [Test]
        public void WriteThenReadRoundTrips()
        {
            var store = new FileSaveStore(dir);
            var p = PlayerProfile.CreateDefault();
            p.gems = 9001;
            p.Unlock("valiant");
            store.Write(p);

            var reader = new FileSaveStore(dir);
            PlayerProfile back = reader.Read();

            Assert.AreEqual(ReadOutcome.Loaded, reader.LastReadOutcome);
            Assert.AreEqual(9001, back.gems);
            Assert.IsTrue(back.IsUnlocked("valiant"));
        }

        [Test]
        public void TamperedSaveFallsBackToBackup()
        {
            var store = new FileSaveStore(dir);

            var first = PlayerProfile.CreateDefault();
            first.gems = 100;
            store.Write(first);

            var second = PlayerProfile.CreateDefault();
            second.gems = 200;
            store.Write(second);   // first write becomes the backup

            // Hand-edit the live file the way a cheat tool would. The envelope's payload
            // field holds the profile JSON as a nested, escaped string, so the quotes
            // around "gems" appear backslash-escaped on disk (\"gems\":200) rather than
            // bare — match that escaped form or the replacement silently no-ops.
            string live = Path.Combine(dir, FileSaveStore.SaveFileName);
            File.WriteAllText(live, File.ReadAllText(live).Replace("\\\"gems\\\":200", "\\\"gems\\\":999999"));

            var reader = new FileSaveStore(dir);
            PlayerProfile p = reader.Read();

            Assert.AreEqual(ReadOutcome.RecoveredFromBackup, reader.LastReadOutcome);
            Assert.AreEqual(100, p.gems);
        }

        [Test]
        public void CorruptSaveWithNoBackupResetsToDefault()
        {
            File.WriteAllText(Path.Combine(dir, FileSaveStore.SaveFileName), "this is not json");

            var store = new FileSaveStore(dir);
            PlayerProfile p = store.Read();

            Assert.AreEqual(ReadOutcome.RejectedAndReset, store.LastReadOutcome);
            Assert.AreEqual(350, p.gems);
        }

        [Test]
        public void PartialTempFileIsIgnoredAndLiveSaveSurvives()
        {
            var store = new FileSaveStore(dir);
            var p = PlayerProfile.CreateDefault();
            p.gems = 777;
            store.Write(p);

            // Simulate a write killed midway: a temp file left behind.
            File.WriteAllText(Path.Combine(dir, FileSaveStore.TempFileName), "{\"version\":1,\"pay");

            PlayerProfile back = new FileSaveStore(dir).Read();
            Assert.AreEqual(777, back.gems);
        }

        [Test]
        public void ExistsReflectsWhetherASaveIsPresent()
        {
            var store = new FileSaveStore(dir);
            Assert.IsFalse(store.Exists());
            store.Write(PlayerProfile.CreateDefault());
            Assert.IsTrue(store.Exists());
            store.Delete();
            Assert.IsFalse(store.Exists());
        }
    }
}
