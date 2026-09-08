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

        public SaveService(ISaveStore store, Func<string> legacyXmlProvider, Action<string> legacyCleared)
        {
            this.store = store;
            this.legacyXmlProvider = legacyXmlProvider;
            this.legacyCleared = legacyCleared;
        }

        public void Load()
        {
            MigratedThisLoad = false;

            if (store.Exists())
            {
                Profile = store.Read();
                return;
            }

            string legacyXml = legacyXmlProvider != null ? legacyXmlProvider() : null;
            if (!string.IsNullOrEmpty(legacyXml))
            {
                PlayerProfile migrated = MigrationV0ToV1.FromXml(legacyXml);
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
                Debug.LogWarning("Legacy save present but unreadable; starting fresh.");
            }

            Profile = store.Read();   // no file and no legacy -> default profile
        }

        public void Save()
        {
            if (Profile == null) return;
            store.Write(Profile);
        }
    }
}
