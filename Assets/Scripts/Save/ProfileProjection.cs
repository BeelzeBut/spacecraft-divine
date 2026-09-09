using System.Collections.Generic;

namespace SpaceshipDivine.Save
{
    /// <summary>
    /// Translates between the profile (authoritative for meta-progression) and the legacy
    /// SaveData blob (authoritative for the current run only).
    ///
    /// Both directions are ADD-ONLY for unlocks and blueprints. A gameplay scene can build a
    /// SaveData holding fewer unlocks than the profile does — MainMenu is not loaded there, so
    /// nothing repopulates the array — and a symmetric copy would revoke ships the player paid
    /// real money for.
    ///
    /// Indices come from LegacyShipIdMap, which SdAutomation.VerifyShipPrefabOrder pins against
    /// the scene. Any id the map does not know is skipped rather than throwing: an unknown id
    /// is bad data, and a crash in the save path is a player who cannot launch the game at all.
    /// </summary>
    public static class ProfileProjection
    {
        public static void ApplyToSaveData(PlayerProfile profile, SaveData target)
        {
            if (profile == null || target == null) return;

            IReadOnlyList<string> order = LegacyShipIdMap.OrderedShipIds;
            for (int i = 0; i < order.Count; i++)
            {
                string id = order[i];

                if (i < target.isUnlocked.Length && profile.IsUnlocked(id))
                    target.isUnlocked[i] = true;

                if (i < target.shipLevel.Length)
                    target.shipLevel[i] = profile.GetShipLevel(id);

                // 1 is the legacy sentinel for "blueprint collected, ship claimable" — a
                // boolean wearing a float's clothes, read by MainMenu.UnlockShip's
                // canBeUnlockedInGame branch. It is not a price.
                if (i < target.priceToUnlock.Length && profile.IsBlueprintRedeemable(id))
                    target.priceToUnlock[i] = 1f;
            }

            foreach (int dropIndex in profile.collectedBlueprintIndices)
                if (dropIndex >= 0 && dropIndex < target.hasBeenUnlocked.Length)
                    target.hasBeenUnlocked[dropIndex] = true;

            target.gems = profile.gems;
            target.goldCoins = profile.goldCoins;
            target.hasCompletedTutorial = profile.hasCompletedTutorial;
            target.hasCompletedButtonsTutorial = profile.hasCompletedButtonsTutorial;
            target.levelsPlayed = profile.levelsPlayed;
            target.totalEnemiesKilled = profile.stats.totalEnemiesKilled;

            target.qualityIndex = profile.settings.qualityIndex;
            target.targetFps = profile.settings.targetFps;
            target.isMusicMuted = profile.settings.isMusicMuted;
            target.isSoundMuted = profile.settings.isSoundMuted;
            target.isHapticsMuted = profile.settings.isHapticsMuted;
            target.controllerOn = profile.settings.controllerOn;
        }

        public static void CaptureFromSaveData(SaveData source, PlayerProfile target)
        {
            if (source == null || target == null) return;

            IReadOnlyList<string> order = LegacyShipIdMap.OrderedShipIds;
            for (int i = 0; i < order.Count; i++)
            {
                string id = order[i];

                // Unlock, never Remove. See the add-only note on the class.
                if (i < source.isUnlocked.Length && source.isUnlocked[i])
                    target.Unlock(id);

                if (i < source.shipLevel.Length && source.shipLevel[i] > 0)
                    target.SetShipLevel(id, source.shipLevel[i]);

                if (i < source.priceToUnlock.Length && source.priceToUnlock[i] == 1f)
                    target.MarkBlueprintRedeemable(id);
            }

            for (int d = 0; d < source.hasBeenUnlocked.Length; d++)
                if (source.hasBeenUnlocked[d])
                    target.CollectBlueprint(d);

            target.gems = source.gems;
            target.goldCoins = source.goldCoins;
            target.hasCompletedTutorial = source.hasCompletedTutorial;
            target.hasCompletedButtonsTutorial = source.hasCompletedButtonsTutorial;
            target.levelsPlayed = source.levelsPlayed;
            target.stats.totalEnemiesKilled = source.totalEnemiesKilled;

            target.settings.qualityIndex = source.qualityIndex;
            target.settings.targetFps = source.targetFps;
            target.settings.isMusicMuted = source.isMusicMuted;
            target.settings.isSoundMuted = source.isSoundMuted;
            target.settings.isHapticsMuted = source.isHapticsMuted;
            target.settings.controllerOn = source.controllerOn;
        }
    }
}
