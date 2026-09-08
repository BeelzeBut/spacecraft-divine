using System;
using System.IO;
using System.Xml.Serialization;

namespace SpaceshipDivine.Save
{
    /// <summary>
    /// Mirror of the pre-v1 SaveData class, kept only so migration can read saves written by
    /// the shipped 2021 build. Field names and array sizes must match the original exactly or
    /// XmlSerializer silently drops values. Do not add to this class.
    /// </summary>
    [Serializable]
    public class LegacySaveDataV0
    {
        public bool hasCompletedTutorial = false;
        public bool hasCompletedButtonsTutorial = false;

        public int[] level = new int[2];
        public int gems, goldCoins;
        public int targetFps;
        public int qualityIndex;
        public bool controllerOn;
        public bool isMusicMuted;
        public bool isSoundMuted;

        public bool[] isUnlocked = new bool[50];
        public int totalEnemiesKilled;
        public int respawnsRemaining = 1;
        public string levelsPlayed = "";

        public int orderNumber;
        public float maxHealth;
        public float currentHealth;
        public float minHealth;
        public float fireRate;
        public int bulletsShot;
        public float spread;
        public float maxMoveSpeed;
        public float damagePerbullet;
        public int numberOfBursts;
        public float waitTimeBetweenBursts;
        public float critChance;
        public float critMultiplier;
        public bool gameHasEnded = true;
        public float timeSinceGameStarted;
        public int enemiesKilled;
        public float attackMultiplier, defenseMultiplier, speedMultiplier;
        public float damageReduction, damageMultiplier;
        public int abilityLevel;
        public string abilityName;
        public bool canBounce, canPierce, canExplode, moveSpeedOnAbility,
                    pushBackOnAbility, moveSpeedOnEnemyKill, resetCdOnKill;
        public float chanceToBlockAttack, moveSpeedIncrease, pushBackStrength,
                     moveSpeedAmount, resetCdChance, regenPerSecond, constantRegenPerSecond;
        public float gunMaxWidth;

        public int[] shipLevel = new int[50];
        public float[] priceToUnlock = new float[50];
        public float[] damageMultipliers = new float[50];
        public float[] maxHealths = new float[50];
        public float[] spreads = new float[50];
        public float[] maxMoveSpeeds = new float[50];
        public float[] critChances = new float[50];
        public float[] attackMultipliers = new float[50];
        public float[] defenseMultipliers = new float[50];
        public float[] speedMultipliers = new float[50];
        public float[] damageReductions = new float[50];
        public int numberOfShips;

        public bool[] hasBeenUnlocked = new bool[100];

        public int[] upgradesIndex = new int[5];
        public int numberOfUpgrades = 0;
    }

    public static class LegacyXmlSerializer
    {
        public static string ToXml(LegacySaveDataV0 data)
        {
            var xml = new XmlSerializer(typeof(LegacySaveDataV0));
            var writer = new StringWriter();
            xml.Serialize(writer, data);
            return writer.ToString();
        }

        public static LegacySaveDataV0 FromXml(string xml)
        {
            if (string.IsNullOrEmpty(xml)) return null;
            try
            {
                var serializer = new XmlSerializer(typeof(LegacySaveDataV0));
                return (LegacySaveDataV0)serializer.Deserialize(new StringReader(xml));
            }
            catch (Exception)
            {
                return null;
            }
        }
    }
}
