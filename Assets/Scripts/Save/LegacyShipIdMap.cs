using System;
using System.Collections.Generic;

namespace SpaceshipDivine.Save
{
    /// <summary>
    /// The legacy save stored unlocks as bool[50] indexed by position in
    /// MainMenu.shipPrefabs. This transcribes that ordering so migration can turn indices
    /// into stable IDs. THE ORDER HERE IS GROUND TRUTH AND MUST NOT BE CHANGED — it is the
    /// only thing tying a 2021 save to a ship.
    /// </summary>
    public static class LegacyShipIdMap
    {
        // Transcribed from Assets/Scenes/Main Menu.unity, MainMenu component, shipPrefabs
        // list (verified 2026-09-08). The tutorial ship (grey_byrd_tutorial) is intentionally
        // absent: it is wired to MainMenu.tutorialSpaceship, a separate field, not an element
        // of shipPrefabs, and DataHolder.LoadPlayerData/SavePlayerData only ever index
        // dataSaved.isUnlocked[i] by position in shipPrefabs. It never had a legacy slot.
        private static readonly string[] Ids =
        {
            "grey_byrd",           // 0
            "bubu",                // 1
            "apollo",              // 2
            "the_argon",           // 3
            "razor",               // 4
            "the_reaper",          // 5
            "white_ripper",        // 6
            "hot_talon",           // 7
            "bat_oh_no",           // 8
            "vickers",             // 9
            "lunar_hunter",        // 10
            "warspite",            // 11
            "valiant"              // 12
        };

        public static int Count => Ids.Length;

        // Array.AsReadOnly wraps rather than clones, so this stays allocation-cheap while
        // returning something callers cannot mutate. Same arrangement as PlayerProfile.
        public static IReadOnlyList<string> OrderedShipIds => Array.AsReadOnly(Ids);

        /// <summary>
        /// -1 when the id is not a ship this map knows. Callers must skip rather than throw:
        /// an unknown id in a save is bad data, and bad data must never crash the game.
        /// </summary>
        public static int IdToIndex(string shipId)
        {
            if (string.IsNullOrEmpty(shipId)) return -1;
            for (int i = 0; i < Ids.Length; i++)
                if (Ids[i] == shipId) return i;
            return -1;
        }

        public static string IndexToId(int index)
        {
            if (index < 0 || index >= Ids.Length) return null;
            return Ids[index];
        }
    }
}
