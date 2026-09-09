using System;
using UnityEngine;

namespace SpaceshipDivine.Save
{
    /// <summary>
    /// The single entry point the game uses for persistence. Owns the profile and the run
    /// state, and performs the one-time legacy migration on first load.
    /// </summary>
    public class SaveService
    {
        private readonly ISaveStore store;
        private readonly Func<string> legacyXmlProvider;
        private readonly Action<string> legacyCleared;

        public PlayerProfile Profile { get; private set; }
        public RunState Run { get; private set; } = new RunState();
        public bool MigratedThisLoad { get; private set; }

        /// <summary>
        /// True when both save copies failed verification and the legacy blob rescued the
        /// profile. Distinct from MigratedThisLoad: that is the ordinary first-run upgrade,
        /// this is a recovery from a save that should have worked.
        /// </summary>
        public bool RecoveredFromLegacyAfterRejection { get; private set; }

        public SaveService(ISaveStore store, Func<string> legacyXmlProvider, Action<string> legacyCleared)
        {
            // A null store is a wiring bug, not a runtime condition. Tolerating it would mean
            // the game runs with saves silently not persisting — the worst possible failure
            // mode for a save system, and one nobody notices until a player loses progress.
            // Bad DATA must never crash the game; bad WIRING must be loud and immediate.
            this.store = store ?? throw new ArgumentNullException(nameof(store));
            this.legacyXmlProvider = legacyXmlProvider;   // genuinely optional
            this.legacyCleared = legacyCleared;           // genuinely optional
        }

        public void Load()
        {
            MigratedThisLoad = false;
            RecoveredFromLegacyAfterRejection = false;

            if (store.Exists())
            {
                Profile = store.Read();

                // A rejected save is not the same as no save. Both copies failed their
                // signature - most often because Android Auto Backup carried them to a device
                // whose id no longer salts the HMAC. Returning here would hand the player a
                // default profile while a perfectly readable legacy blob sits in PlayerPrefs.
                if (store.LastReadOutcome != ReadOutcome.RejectedAndReset)
                    return;

                PlayerProfile rescued = TryReadLegacy();
                if (rescued == null) return;   // keep the reset profile; nothing better exists

                Profile = rescued;
                RecoveredFromLegacyAfterRejection = true;
                store.Write(Profile);
                Debug.LogWarning("Profile failed verification; recovered " +
                                 Profile.unlockedShipIds.Count + " ships from the legacy save.");
                return;
            }

            PlayerProfile migrated = TryReadLegacy();
            if (migrated != null)
            {
                Profile = migrated;
                MigratedThisLoad = true;
                store.Write(Profile);
                // The legacy PlayerPrefs blob is deliberately left in place for one
                // release, as an escape hatch if migration turns out to be wrong.
                legacyCleared?.Invoke(MigrationV0ToV1.LegacyPlayerPrefsKey);
                Debug.Log("Migrated legacy save to v1: " +
                          Profile.unlockedShipIds.Count + " ships preserved.");
                return;
            }

            Profile = store.Read();   // no file and no legacy -> default profile
        }

        private PlayerProfile TryReadLegacy()
        {
            string xml = legacyXmlProvider != null ? legacyXmlProvider() : null;
            if (string.IsNullOrEmpty(xml)) return null;

            PlayerProfile parsed = MigrationV0ToV1.FromXml(xml);
            if (parsed == null)
                Debug.LogWarning("Legacy save present but unreadable.");
            return parsed;
        }

        public void Save()
        {
            if (Profile == null) return;
            store.Write(Profile);
        }
    }
}
