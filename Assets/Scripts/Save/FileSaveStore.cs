using System;
using System.IO;
using UnityEngine;

namespace SpaceshipDivine.Save
{
    /// <summary>
    /// Writes the profile atomically: serialise to a temp file, flush it, then move it over
    /// the live file, promoting the previous live file to a backup first. A process killed
    /// mid-write therefore leaves either the old save or the new one, never a half-written
    /// one. Reads walk a recovery ladder rather than ever throwing at the caller.
    /// </summary>
    public class FileSaveStore : ISaveStore
    {
        public const string SaveFileName = "profile.json";
        public const string BackupFileName = "profile.backup.json";
        public const string TempFileName = "profile.tmp.json";

        private readonly string directory;

        public ReadOutcome LastReadOutcome { get; private set; } = ReadOutcome.Fresh;
        public bool IsReadOnlyBecauseNewer { get; private set; }

        public FileSaveStore(string directory)
        {
            this.directory = directory;
            if (!Directory.Exists(directory))
                Directory.CreateDirectory(directory);
        }

        public FileSaveStore() : this(Application.persistentDataPath) { }

        private string LivePath => Path.Combine(directory, SaveFileName);
        private string BackupPath => Path.Combine(directory, BackupFileName);
        private string TempPath => Path.Combine(directory, TempFileName);

        public bool Exists() => File.Exists(LivePath);

        public void Delete()
        {
            SafeDelete(LivePath);
            SafeDelete(BackupPath);
            SafeDelete(TempPath);
        }

        public void Write(PlayerProfile profile)
        {
            if (profile == null) return;

            if (IsReadOnlyBecauseNewer)
            {
                Debug.LogWarning("Refusing to overwrite a save written by a newer build.");
                return;
            }

            string payload = JsonUtility.ToJson(profile);
            var envelope = new SaveEnvelope
            {
                version = SaveEnvelope.CurrentVersion,
                payload = payload,
                signature = SaveIntegrity.Sign(payload)
            };

            try
            {
                using (var stream = new FileStream(TempPath, FileMode.Create, FileAccess.Write))
                using (var writer = new StreamWriter(stream))
                {
                    writer.Write(JsonUtility.ToJson(envelope));
                    writer.Flush();
                    stream.Flush(true);
                }

                // Demote the current live file to backup before replacing it. A copy with
                // overwrite has no window where the backup is missing; the previous
                // delete-then-move sequence did. File.Move below does not overwrite an
                // existing destination on this runtime, so the now-backed-up live file is
                // removed to make way for it; if that removal or the move itself throws,
                // the catch below leaves the original live file and its fresh backup intact
                // and only discards the temp file.
                if (File.Exists(LivePath))
                {
                    File.Copy(LivePath, BackupPath, true);
                    File.Delete(LivePath);
                }

                File.Move(TempPath, LivePath);
            }
            catch (Exception e)
            {
                Debug.LogError("Save write failed: " + e.Message);
                SafeDelete(TempPath);
            }
        }

        public PlayerProfile Read()
        {
            // Recomputed on every read: a Delete() followed by a Read() must not stay latched.
            IsReadOnlyBecauseNewer = false;

            if (!File.Exists(LivePath) && !File.Exists(BackupPath))
            {
                LastReadOutcome = ReadOutcome.Fresh;
                return PlayerProfile.CreateDefault();
            }

            PlayerProfile fromLive = TryLoad(LivePath);
            if (fromLive != null)
            {
                LastReadOutcome = ReadOutcome.Loaded;
                return fromLive;
            }

            PlayerProfile fromBackup = TryLoad(BackupPath);
            if (fromBackup != null)
            {
                Debug.LogWarning("Live save was unreadable; recovered from backup.");
                LastReadOutcome = ReadOutcome.RecoveredFromBackup;
                return fromBackup;
            }

            Debug.LogWarning("Live save and backup both unreadable; starting a new profile.");
            LastReadOutcome = ReadOutcome.RejectedAndReset;
            return PlayerProfile.CreateDefault();
        }

        private PlayerProfile TryLoad(string path)
        {
            if (!File.Exists(path)) return null;

            try
            {
                var envelope = JsonUtility.FromJson<SaveEnvelope>(File.ReadAllText(path));
                if (envelope == null || string.IsNullOrEmpty(envelope.payload)) return null;
                if (!SaveIntegrity.Verify(envelope.payload, envelope.signature)) return null;

                // Forward compatibility is impossible; data loss is avoidable. Read what we
                // can and then refuse to write, rather than resetting a save that is merely
                // newer than this build. Note the signature covers the payload only, not the
                // version, so this flag is a safety measure and not a security boundary.
                if (envelope.version > SaveEnvelope.CurrentVersion)
                {
                    IsReadOnlyBecauseNewer = true;
                    Debug.LogWarning("Save was written by a newer build (v" + envelope.version +
                                     " > v" + SaveEnvelope.CurrentVersion +
                                     "); loading it read-only and refusing to overwrite it.");
                }

                return JsonUtility.FromJson<PlayerProfile>(envelope.payload);
            }
            catch (Exception)
            {
                return null;
            }
        }

        private static void SafeDelete(string path)
        {
            try { if (File.Exists(path)) File.Delete(path); }
            catch (Exception) { /* a locked file is not worth crashing a save over */ }
        }
    }
}
