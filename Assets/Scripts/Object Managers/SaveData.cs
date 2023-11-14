[System.Serializable]
public class SaveData
{
    //Tutorial
    public bool hasCompletedTutorial = false;
    public bool hasCompletedButtonsTutorial = false;

    //General
    public int[] level = new int[2];
    public int gems, goldCoins;
    public int targetFps;
    public int qualityIndex;
    public bool controllerOn;
    //public bool hasCompletedStoryMode = false;
    public bool isMusicMuted;
    public bool isSoundMuted;

    public bool[] isUnlocked = new bool[50];
    public int totalEnemiesKilled;
    public int respawnsRemaining = 1;
    public string levelsPlayed = "";

    //selected ship saves
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
    public bool canBounce, canPierce, canExplode, moveSpeedOnAbility, pushBackOnAbility, moveSpeedOnEnemyKill, resetCdOnKill;
    public float chanceToBlockAttack, moveSpeedIncrease, pushBackStrength, moveSpeedAmount, resetCdChance, regenPerSecond, constantRegenPerSecond;
    public float gunMaxWidth;

    //main menu ships save
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

    //In game chest and unlockables
    public bool[] hasBeenUnlocked = new bool[100];

    //Upgrades
    public int[] upgradesIndex = new int[5];
    public int numberOfUpgrades = 0;
}