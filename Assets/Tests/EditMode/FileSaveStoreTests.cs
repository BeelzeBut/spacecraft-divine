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
        public void ValidTempFileIsNeverPreferredOverTheLiveSave()
        {
            var store = new FileSaveStore(dir);
            var live = PlayerProfile.CreateDefault();
            live.gems = 777;
            store.Write(live);

            // A fully valid, correctly signed envelope left at the temp path by a write that
            // was killed after serialising but before the rename. Read() must ignore it:
            // only the live file and the backup are authoritative.
            var orphan = PlayerProfile.CreateDefault();
            orphan.gems = 999999;
            string payload = JsonUtility.ToJson(orphan);
            var envelope = new SaveEnvelope
            {
                version = SaveEnvelope.CurrentVersion,
                payload = payload,
                signature = SaveIntegrity.Sign(payload)
            };
            File.WriteAllText(Path.Combine(dir, FileSaveStore.TempFileName),
                              JsonUtility.ToJson(envelope));

            var reader = new FileSaveStore(dir);
            PlayerProfile back = reader.Read();

            Assert.AreEqual(ReadOutcome.Loaded, reader.LastReadOutcome);
            Assert.AreEqual(777, back.gems, "a valid temp file must never win over the live save");
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
    
        /// <summary>
        /// Bumps the envelope's version in place. No re-signing needed: the signature covers
        /// the payload only, so the version rides outside it.
        /// </summary>
        private void RewriteEnvelopeVersion(int version)
        {
            string path = Path.Combine(dir, FileSaveStore.SaveFileName);
            var envelope = JsonUtility.FromJson<SaveEnvelope>(File.ReadAllText(path));
            envelope.version = version;
            File.WriteAllText(path, JsonUtility.ToJson(envelope));
        }

        [Test]
        public void AFutureVersionSaveIsLoadedButNotOverwritten()
        {
            // An older build must leave a newer save alone rather than reset it or write
            // stale fields over it. Losing this session beats losing the save.
            var writer = new FileSaveStore(dir);
            PlayerProfile original = PlayerProfile.CreateDefault();
            original.gems = 4242;
            writer.Write(original);

            RewriteEnvelopeVersion(SaveEnvelope.CurrentVersion + 1);
            string before = File.ReadAllText(Path.Combine(dir, FileSaveStore.SaveFileName));

            var older = new FileSaveStore(dir);
            PlayerProfile read = older.Read();

            Assert.AreEqual(4242, read.gems, "a newer save should still be readable");
            Assert.IsTrue(older.IsReadOnlyBecauseNewer);

            PlayerProfile overwrite = PlayerProfile.CreateDefault();
            overwrite.gems = 1;
            older.Write(overwrite);

            Assert.AreEqual(before, File.ReadAllText(Path.Combine(dir, FileSaveStore.SaveFileName)),
                "an older build must not overwrite a newer save");
        }

        [Test]
        public void ACurrentVersionSaveIsStillWritable()
        {
            // Guards the obvious over-correction: latching every save read-only.
            var store = new FileSaveStore(dir);
            store.Write(PlayerProfile.CreateDefault());
            store.Read();
            Assert.IsFalse(store.IsReadOnlyBecauseNewer);

            PlayerProfile p = PlayerProfile.CreateDefault();
            p.gems = 777;
            store.Write(p);

            Assert.AreEqual(777, new FileSaveStore(dir).Read().gems);
        }
}
}
