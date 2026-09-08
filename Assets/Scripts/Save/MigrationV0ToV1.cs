using System;
using System.IO;
using System.Xml.Serialization;
using UnityEngine;

namespace SpaceshipDivine.Save
{
    /// <summary>
    /// Turns a legacy XML save into a v1 PlayerProfile. Unlocks are only ever added, never
    /// removed: a returning player must not lose a ship because the gate turned on.
    /// </summary>
    public static class MigrationV0ToV1
    {
        public const string LegacyPlayerPrefsKey = "save";

        public static PlayerProfile FromXml(string xml)
        {
            LegacySaveDataV0 legacy = Deserialize(xml);
            if (legacy == null) return null;

            // Start from an empty profile, not CreateDefault(), so the result reflects the
            // legacy save exactly. Starters are added afterwards as a floor.
            var p = new PlayerProfile
            {
                gems = legacy.gems,
                goldCoins = legacy.goldCoins,
                hasCompletedTutorial = legacy.hasCompletedTutorial,
                hasCompletedButtonsTutorial = legacy.hasCompletedButtonsTutorial,
                levelsPlayed = legacy.levelsPlayed ?? ""
            };

            p.settings.qualityIndex = legacy.qualityIndex;
            p.settings.targetFps = legacy.targetFps == 0 ? 60 : legacy.targetFps;
            p.settings.isMusicMuted = legacy.isMusicMuted;
            p.settings.isSoundMuted = legacy.isSoundMuted;
            p.settings.controllerOn = legacy.controllerOn;

            p.stats.totalEnemiesKilled = legacy.totalEnemiesKilled;

            for (int i = 0; i < LegacyShipIdMap.Count && i < legacy.isUnlocked.Length; i++)
            {
                if (!legacy.isUnlocked[i]) continue;
                p.Unlock(LegacyShipIdMap.IndexToId(i));
            }

            for (int i = 0; i < LegacyShipIdMap.Count && i < legacy.shipLevel.Length; i++)
            {
                if (legacy.shipLevel[i] <= 1) continue;
                p.SetShipLevel(LegacyShipIdMap.IndexToId(i), legacy.shipLevel[i]);
            }

            // Blueprint pickups, so already-collected blueprints do not drop again.
            for (int i = 0; i < legacy.hasBeenUnlocked.Length; i++)
                if (legacy.hasBeenUnlocked[i])
                    p.CollectBlueprint(i);

            // The legacy build overloaded priceToUnlock[i] == 1 to mean "blueprint collected,
            // this ship is now claimable for free in the menu". That is earned progress, so it
            // migrates to an explicit flag.
            //
            // NOTE: this sentinel is not exclusive to blueprint pickups. Bubu.asset ships with
            // priceToUnlock: 1 as its own default (Vickers and Warspite both default to 0), and
            // DataHolder persisted that default verbatim — so this matches Bubu for essentially
            // every 2021 save. That is deliberate and correct: in the shipped 2021 build
            // MainMenu.UnlockShip() granted any canBeUnlockedInGame ship on one tap whenever
            // priceToUnlock was nonzero, so Bubu was already a one-tap freebie for those
            // players. Marking it redeemable preserves what they had; excluding it would take
            // something away, which grandfathering forbids.
            //
            // The comparison stays exact. Real gem prices (500, 650, 1000, 2000, 2250, 2600,
            // 3900, 4500) and the 9.99 real-money price are all far from 1.
            for (int i = 0; i < LegacyShipIdMap.Count && i < legacy.priceToUnlock.Length; i++)
            {
                if (Math.Abs(legacy.priceToUnlock[i] - 1f) > 0.0001f) continue;
                p.MarkBlueprintRedeemable(LegacyShipIdMap.IndexToId(i));
            }

            // Floor: never end up with fewer ships than a brand-new player gets.
            foreach (string id in PlayerProfile.StarterShipIds)
                p.Unlock(id);

            // Ship design arrays (maxHealths, spreads, damageMultipliers, ...) are
            // deliberately not read. They now live on the Spaceship ScriptableObjects, which
            // is what makes post-launch rebalancing reach existing players.

            return p;
        }

        /// <summary>
        /// Migrates the in-progress run from a legacy save. Separate from FromXml because run
        /// state and profile have different lifetimes: the profile persists, the run does not.
        /// Returns null if the legacy save has no run in progress or cannot be read.
        /// </summary>
        public static RunState RunFromXml(string xml)
        {
            LegacySaveDataV0 legacy = Deserialize(xml);
            if (legacy == null) return null;
            if (legacy.gameHasEnded) return null;   // nothing in progress to carry

            return new RunState
            {
                // selectedShipId resolves the legacy orderNumber (an index into
                // MainMenu.shipPrefabs) via LegacyShipIdMap. An out-of-range index yields null
                // from IndexToId, coalesced to "" rather than throwing.
                selectedShipId = LegacyShipIdMap.IndexToId(legacy.orderNumber) ?? "",
                level = legacy.level != null && legacy.level.Length > 0 ? legacy.level[0] : 1,
                subLevel = legacy.level != null && legacy.level.Length > 1 ? legacy.level[1] : 1,
                respawnsRemaining = legacy.respawnsRemaining,
                enemiesKilled = legacy.enemiesKilled,
                timeSinceGameStarted = legacy.timeSinceGameStarted,
                abilityName = legacy.abilityName ?? "",
                abilityLevel = legacy.abilityLevel,
                runInProgress = true
            };
        }

        private static LegacySaveDataV0 Deserialize(string xml)
        {
            if (string.IsNullOrEmpty(xml)) return null;
            try
            {
                // Real saves have <SaveData> as the root element, because that was the class
                // name when they were written. Override the root so the renamed mirror class
                // still deserialises them.
                var root = new XmlRootAttribute("SaveData");
                var serializer = new XmlSerializer(typeof(LegacySaveDataV0), root);
                return (LegacySaveDataV0)serializer.Deserialize(new StringReader(xml));
            }
            catch (Exception)
            {
                // Fall back to the natural root, which is what LegacyXmlSerializer.ToXml writes.
                try
                {
                    var serializer = new XmlSerializer(typeof(LegacySaveDataV0));
                    return (LegacySaveDataV0)serializer.Deserialize(new StringReader(xml));
                }
                catch (Exception e)
                {
                    Debug.LogWarning("Legacy save could not be read: " + e.Message);
                    return null;
                }
            }
        }
    }
}
