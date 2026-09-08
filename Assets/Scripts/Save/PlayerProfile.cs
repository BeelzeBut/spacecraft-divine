using System;
using System.Collections.Generic;

namespace SpaceshipDivine.Save
{
    [Serializable]
    public class ShipLevelEntry
    {
        public string shipId;
        public int level = 1;
    }

    [Serializable]
    public class SettingsData
    {
        public int qualityIndex = 3;
        public int targetFps = 60;
        public bool isMusicMuted;
        public bool isSoundMuted;
        public bool isHapticsMuted;
        public bool controllerOn;
        public int cameraSpeedLevel = 3;
    }

    [Serializable]
    public class LifetimeStats
    {
        public int totalEnemiesKilled;
        public int totalBossesDefeated;
        public int totalLevelsCleared;
        public int totalRunsCompleted;
    }

    /// <summary>
    /// Everything that persists across runs. Contains no ship design data — ship stats live
    /// on the Spaceship ScriptableObjects and are read fresh every launch, which is what
    /// makes post-launch rebalancing possible.
    /// </summary>
    [Serializable]
    public class PlayerProfile
    {
        public const int StartingGems = 350;

        // The three ships a brand-new player can select. "grey_byrd_tutorial" is NOT here:
        // it is forced by the tutorial rather than chosen, and is handled separately in
        // DataHolder so it can never be the reason a new player is soft-locked.
        private static readonly string[] starterShipIds = { "grey_byrd", "apollo", "the_argon" };

        // Array.AsReadOnly wraps (not clones) the backing array, so this stays allocation-cheap
        // while returning an object whose runtime type is ReadOnlyCollection<string>, not
        // string[] — that is what keeps the public surface un-mutable from outside the class.
        public static IReadOnlyList<string> StarterShipIds => Array.AsReadOnly(starterShipIds);

        public int gems;
        public int goldCoins;
        public bool hasCompletedTutorial;
        public bool hasCompletedButtonsTutorial;
        public bool removeAdsOwned;
        public string levelsPlayed = "";

        public List<string> unlockedShipIds = new List<string>();
        public List<ShipLevelEntry> shipLevels = new List<ShipLevelEntry>();

        // Blueprint progress. collectedBlueprintIndices mirrors the legacy hasBeenUnlocked[]
        // (which blueprint pickups have happened, so they do not drop twice).
        // blueprintRedeemableShipIds replaces the legacy overloading of priceToUnlock == 1
        // (which ships the player may now claim for free in the menu).
        public List<int> collectedBlueprintIndices = new List<int>();
        public List<string> blueprintRedeemableShipIds = new List<string>();

        public SettingsData settings = new SettingsData();
        public LifetimeStats stats = new LifetimeStats();

        public static PlayerProfile CreateDefault()
        {
            var p = new PlayerProfile { gems = StartingGems };
            foreach (string id in starterShipIds)
                p.Unlock(id);
            return p;
        }

        public static bool IsStarterShip(string shipId)
        {
            if (string.IsNullOrEmpty(shipId)) return false;
            for (int i = 0; i < starterShipIds.Length; i++)
                if (starterShipIds[i] == shipId)
                    return true;
            return false;
        }

        public bool IsUnlocked(string shipId)
        {
            return !string.IsNullOrEmpty(shipId) && unlockedShipIds.Contains(shipId);
        }

        public void Unlock(string shipId)
        {
            if (string.IsNullOrEmpty(shipId)) return;
            if (!unlockedShipIds.Contains(shipId))
                unlockedShipIds.Add(shipId);
        }

        public int GetShipLevel(string shipId)
        {
            ShipLevelEntry e = FindLevelEntry(shipId);
            return e == null ? 1 : e.level;
        }

        public void SetShipLevel(string shipId, int level)
        {
            if (string.IsNullOrEmpty(shipId)) return;

            if (level < 1) level = 1;
            if (level > 5) level = 5;

            ShipLevelEntry e = FindLevelEntry(shipId);
            if (e == null)
            {
                e = new ShipLevelEntry { shipId = shipId };
                shipLevels.Add(e);
            }
            e.level = level;
        }

        public bool HasCollectedBlueprint(int dropIndex)
        {
            return collectedBlueprintIndices.Contains(dropIndex);
        }

        public void CollectBlueprint(int dropIndex)
        {
            if (!collectedBlueprintIndices.Contains(dropIndex))
                collectedBlueprintIndices.Add(dropIndex);
        }

        public bool IsBlueprintRedeemable(string shipId)
        {
            return !string.IsNullOrEmpty(shipId) && blueprintRedeemableShipIds.Contains(shipId);
        }

        public void MarkBlueprintRedeemable(string shipId)
        {
            if (string.IsNullOrEmpty(shipId)) return;
            if (!blueprintRedeemableShipIds.Contains(shipId))
                blueprintRedeemableShipIds.Add(shipId);
        }

        private ShipLevelEntry FindLevelEntry(string shipId)
        {
            for (int i = 0; i < shipLevels.Count; i++)
                if (shipLevels[i].shipId == shipId)
                    return shipLevels[i];
            return null;
        }
    }
}
