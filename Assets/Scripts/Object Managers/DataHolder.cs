using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;

public class DataHolder : MonoBehaviour
{
    public PlayerControls controls;
    public bool controllerOn;
    [SerializeField]private PlayerInput playerInput = null;
    public delegate void Touched();
    public event Touched OnTouch;

    public static DataHolder instance;
    public bool hasCompletedTutorial;
    public bool hasCompletedButtonsTutorial;
    //public bool hasCompletedStoryMode;
    public List<Ability> abilities = new List<Ability>();
    public SaveData dataSaved;
    public MainMenu mainMenu;
    public Spaceship selectedShip;
    public int level = 0, subLevel = 1;
    public int goldCoins, gems;
    public Button loadGame;
    public bool isTestBuild;

    public bool gameHasEnded = true;
    public int respawnsRemaining = 1;
    public bool isInMenu = true;
    public int videoRewardGems = 200;

    public float gameStartTime;
    public float timeSinceGameStarted;
    public int enemiesKilled;
    public float coinsMultiplier = 3f;

    public bool enableSaving;
    public int countToAd = 1;
    public int numberOfShips;
    public string levelsPlayed;
    public int cameraSpeedLevel = 3;

    [Header("Options Menu")]
    public TextMeshProUGUI qualityText;
    public int targetFps = 60;
    int i = 1;
    public int qualityIndex = 3;
    public bool isMusicMuted = false;
    public bool isSoundMuted = false;

    [Header("Drops Settings")]
    public List<Collectible> drops = new List<Collectible>();
    public List<int> killMilestones = new List<int>();

    [Header("Upgrades")]
    public List<Upgrade> possibleUpgrades = new List<Upgrade>();
    void Awake()
    {
        if (instance == null)
        {
            //PlayerPrefs.DeleteAll();
            instance = this;
            DontDestroyOnLoad(gameObject);
            controls = new PlayerControls();
            ControlSetup();
        }
        else if (instance != this)
        {
            Destroy(gameObject);
            return;
        }
        if (isTestBuild)
        {
            if (isInMenu)
            {
                //PlayerPrefs.DeleteAll();
                Load();
                hasCompletedTutorial = true;
                Save();
            }
        }
        else
            if (isInMenu)
                Load();
        QualitySettings.SetQualityLevel(qualityIndex, true);
        if (Application.isMobilePlatform)
            QualitySettings.vSyncCount = 0;
        else
            QualitySettings.vSyncCount = 1;
        Application.targetFrameRate = Screen.currentResolution.refreshRate;
        Time.fixedDeltaTime = 1f / Application.targetFrameRate;
    }

    public PlayerInput PlayerInput => playerInput;

    //Saving
    public void Save()
    {
        if (enableSaving)
        {
            SavePlayerData(PlayerController.instance);

            PlayerPrefs.SetString("save", Helper.Serialize<SaveData>(dataSaved));
        }
    }

    //Loading
    public void Load()
    {
        if (enableSaving)
        {
            if (PlayerPrefs.HasKey("save"))
            {
                dataSaved = Helper.Deserialize<SaveData>(PlayerPrefs.GetString("save"));
                LoadPlayerData();
            }
            else
            {
                dataSaved = new SaveData();
                // Only the starter ships begin unlocked. Everything else is earned or bought.
                //
                // This branch runs ONLY when there is no existing save. A returning 2021 player
                // has PlayerPrefs key "save" set (the legacy build used the same key), so they
                // take the deserialize branch above and never reach this loop — that branch
                // structure is what preserves their unlocks today.
                //
                // NOTE: SpaceshipDivine.Save.MigrationV0ToV1 and SaveService are built and
                // tested but are NOT yet called from any live code path. Do not restructure
                // this branch on the assumption that migration already runs — it does not.
                // Wiring DataHolder over to SaveService is a separate, later piece of work,
                // and whoever does it must re-verify this exact branch behaviour first.
                //
                // grey_byrd_tutorial is deliberately absent from shipPrefabs — the tutorial
                // ship is wired via MainMenu.tutorialSpaceship, so it needs no entry here.
                for (int i = 0; i < mainMenu.shipPrefabs.Count; i++)
                {
                    string id = mainMenu.shipPrefabs[i].shipId;
                    dataSaved.isUnlocked[i] =
                        SpaceshipDivine.Save.PlayerProfile.IsStarterShip(id);
                }

                dataSaved.orderNumber = -1;
                hasCompletedTutorial = false;
                hasCompletedButtonsTutorial = false;
                gems = 350;
                qualityIndex = 3;
                QualitySettings.SetQualityLevel(3, true);
                gameHasEnded = true;
                Save();
                LoadPlayerData();
                Debug.Log("No save file found, creating a new one!");
            }
           // if(mainMenu)
                //mainMenu.RefreshData();
        }
    }

    public void SavePlayerData(PlayerController p)
    {
        if (isInMenu)
        {
            numberOfShips = mainMenu.shipPrefabs.Count;
            dataSaved.numberOfShips = numberOfShips;
            for (int i = 0; i < numberOfShips; i++)
            {
                //if(dataSaved.priceToUnlock[i] != 0)
                dataSaved.priceToUnlock[i] = mainMenu.shipPrefabs[i].priceToUnlock;
                dataSaved.maxHealths[i] = mainMenu.shipPrefabs[i].maxHealth;
                dataSaved.spreads[i] = mainMenu.shipPrefabs[i].spread;
                dataSaved.damageMultipliers[i] = mainMenu.shipPrefabs[i].damageMultiplier;
                dataSaved.maxMoveSpeeds[i] = mainMenu.shipPrefabs[i].maxMoveSpeed;
                dataSaved.critChances[i] = mainMenu.shipPrefabs[i].critChance;
                dataSaved.attackMultipliers[i] = mainMenu.shipPrefabs[i].attackMultiplier;
                dataSaved.defenseMultipliers[i] = mainMenu.shipPrefabs[i].defenseMultiplier;
                dataSaved.speedMultipliers[i] = mainMenu.shipPrefabs[i].speedMultiplier;
                dataSaved.damageReductions[i] = mainMenu.shipPrefabs[i].damageReduction;
            }
        }
        //Save selected ships from dataHolder
        if (selectedShip)
        {
            if (selectedShip.ability)
            {
                dataSaved.abilityLevel = selectedShip.ability.level;
                dataSaved.abilityName = selectedShip.ability.abilityName;
            }
            else
            {
                dataSaved.abilityLevel = 0;
            }
            dataSaved.orderNumber = selectedShip.orderNumber;
            dataSaved.maxHealth = selectedShip.maxHealth;
            dataSaved.currentHealth = selectedShip.currentHealth;
            dataSaved.minHealth = selectedShip.minHealth;
            dataSaved.fireRate = selectedShip.fireRate;
            dataSaved.bulletsShot = selectedShip.bulletsShot;
            dataSaved.spread = selectedShip.spread;
            dataSaved.maxMoveSpeed = selectedShip.maxMoveSpeed;
            dataSaved.damagePerbullet = selectedShip.damagePerBullet;
            dataSaved.numberOfBursts = selectedShip.numberOfBursts;
            dataSaved.waitTimeBetweenBursts = selectedShip.waitTimeBetweenBursts;
            dataSaved.critChance = selectedShip.critChance;
            dataSaved.damageReduction = selectedShip.damageReduction;
            dataSaved.damageMultiplier = selectedShip.damageMultiplier;
            dataSaved.attackMultiplier = selectedShip.attackMultiplier;
            dataSaved.defenseMultiplier = selectedShip.defenseMultiplier;
            dataSaved.speedMultiplier = selectedShip.speedMultiplier;
            dataSaved.canBounce = selectedShip.canBounce;
            dataSaved.canPierce = selectedShip.canPierce;
            dataSaved.canExplode = selectedShip.canExplode;
            dataSaved.chanceToBlockAttack = selectedShip.chanceToBlockAttack;
            dataSaved.moveSpeedOnAbility = selectedShip.moveSpeedOnAbility;
            dataSaved.moveSpeedIncrease = selectedShip.moveSpeedIncrease;
            dataSaved.critMultiplier = selectedShip.critMultiplier;
            dataSaved.gunMaxWidth = selectedShip.gun.maxWidth;
            dataSaved.pushBackOnAbility = selectedShip.pushBackOnAbility;
            dataSaved.pushBackStrength = selectedShip.pushBackStrength;
            dataSaved.moveSpeedOnEnemyKill = selectedShip.moveSpeedOnEnemyKill;
            dataSaved.resetCdOnKill = selectedShip.resetCdOnKill;
            dataSaved.moveSpeedAmount = selectedShip.moveSpeedAmount;
            dataSaved.resetCdChance = selectedShip.resetCdChance;
            dataSaved.regenPerSecond = selectedShip.regenPerSecond;
            dataSaved.constantRegenPerSecond = selectedShip.constantRegenPerSecond;

            dataSaved.numberOfUpgrades = 0;
            for (int i = 0; i < selectedShip.upgradesIndex.Count; i++)
            {
                dataSaved.upgradesIndex[i] = selectedShip.upgradesIndex[i];
                dataSaved.numberOfUpgrades++;
            }

        }
        dataSaved.respawnsRemaining = respawnsRemaining;
        dataSaved.level[0] = level;
        dataSaved.level[1] = subLevel;
        dataSaved.goldCoins = goldCoins;
        dataSaved.gems = gems;
        dataSaved.gameHasEnded = gameHasEnded;
        dataSaved.timeSinceGameStarted = Time.time - timeSinceGameStarted;
        dataSaved.enemiesKilled = enemiesKilled;
        dataSaved.qualityIndex = qualityIndex;
        dataSaved.hasCompletedTutorial = hasCompletedTutorial;
        dataSaved.hasCompletedButtonsTutorial = hasCompletedButtonsTutorial;
        dataSaved.controllerOn = controllerOn;
        dataSaved.levelsPlayed = levelsPlayed;
        //dataSaved.hasCompletedStoryMode = hasCompletedStoryMode;
        dataSaved.isMusicMuted = isMusicMuted;
    }

    public void LoadPlayerData()
    {
        if (isInMenu)
        {
            // Unlock state is restored across the FULL current ship list, not just
            // dataSaved.numberOfShips. isUnlocked is a fixed bool[50] in the save and is always
            // present, whereas numberOfShips is whatever the build that last saved happened to
            // write. Since the ship assets now default to isUnlocked: 0, any index beyond that
            // bound would silently keep the locked default and strand a returning player's
            // ships — including ones they paid for. Also guards against a save from a later
            // build with more ships (rollback), which would otherwise index out of range.
            int unlockCount = Mathf.Min(mainMenu.shipPrefabs.Count, dataSaved.isUnlocked.Length);
            for (int i = 0; i < unlockCount; i++)
                mainMenu.shipPrefabs[i].isUnlocked = dataSaved.isUnlocked[i];

            int designCount = Mathf.Min(dataSaved.numberOfShips, mainMenu.shipPrefabs.Count);
            for (int i = 0; i < designCount; i++)
            {
                mainMenu.shipPrefabs[i].maxHealth = dataSaved.maxHealths[i];
                mainMenu.shipPrefabs[i].spread = dataSaved.spreads[i];
                mainMenu.shipPrefabs[i].damageMultiplier = dataSaved.damageMultipliers[i];
                mainMenu.shipPrefabs[i].maxMoveSpeed = dataSaved.maxMoveSpeeds[i];
                mainMenu.shipPrefabs[i].critChance = dataSaved.critChances[i];
                mainMenu.shipPrefabs[i].attackMultiplier = dataSaved.attackMultipliers[i];
                mainMenu.shipPrefabs[i].defenseMultiplier = dataSaved.defenseMultipliers[i];
                mainMenu.shipPrefabs[i].speedMultiplier = dataSaved.speedMultipliers[i];
                mainMenu.shipPrefabs[i].damageReduction = dataSaved.damageReductions[i];
                mainMenu.shipPrefabs[i].level = Mathf.Clamp(dataSaved.shipLevel[i], 1, 5);
                if (dataSaved.priceToUnlock[i] != 0)
                    mainMenu.shipPrefabs[i].priceToUnlock = dataSaved.priceToUnlock[i];
            }

            if (dataSaved.orderNumber >= 0 && !dataSaved.gameHasEnded)
            {
                selectedShip = mainMenu.shipInstances[dataSaved.orderNumber];
            }
        }
        if (selectedShip)
        {
            selectedShip.selected = true;
            if (dataSaved.abilityLevel != 0)
            {
                foreach (Ability ability in abilities)
                    if (ability.abilityName == dataSaved.abilityName)
                        selectedShip.ability = ability;
                selectedShip.ability.level = dataSaved.abilityLevel;
            }
            selectedShip.maxHealth = dataSaved.maxHealth;
            selectedShip.currentHealth = dataSaved.currentHealth;
            selectedShip.minHealth = dataSaved.minHealth;
            selectedShip.fireRate = dataSaved.fireRate;
            selectedShip.bulletsShot = dataSaved.bulletsShot;
            selectedShip.spread = dataSaved.spread;
            selectedShip.maxMoveSpeed = dataSaved.maxMoveSpeed;
            selectedShip.damagePerBullet = dataSaved.damagePerbullet;
            selectedShip.numberOfBursts = dataSaved.numberOfBursts;
            selectedShip.waitTimeBetweenBursts = dataSaved.waitTimeBetweenBursts;
            selectedShip.critChance = dataSaved.critChance;
            selectedShip.damageReduction = dataSaved.damageReduction;
            selectedShip.damageMultiplier = dataSaved.damageMultiplier;
            selectedShip.attackMultiplier = dataSaved.attackMultiplier;
            selectedShip.defenseMultiplier = dataSaved.defenseMultiplier;
            selectedShip.speedMultiplier = dataSaved.speedMultiplier;
            selectedShip.canBounce = dataSaved.canBounce;
            selectedShip.canPierce = dataSaved.canPierce;
            selectedShip.canExplode = dataSaved.canExplode;
            selectedShip.chanceToBlockAttack = dataSaved.chanceToBlockAttack;
            selectedShip.moveSpeedOnAbility = dataSaved.moveSpeedOnAbility;
            selectedShip.moveSpeedIncrease = dataSaved.moveSpeedIncrease;
            selectedShip.critMultiplier = dataSaved.critMultiplier;
            selectedShip.gun.maxWidth = dataSaved.gunMaxWidth;

            selectedShip.pushBackOnAbility = dataSaved.pushBackOnAbility;
            selectedShip.pushBackStrength = dataSaved.pushBackStrength;
            selectedShip.moveSpeedOnEnemyKill = dataSaved.moveSpeedOnEnemyKill;
            selectedShip.resetCdOnKill = dataSaved.resetCdOnKill;
            selectedShip.moveSpeedAmount = dataSaved.moveSpeedAmount;
            selectedShip.resetCdChance = dataSaved.resetCdChance;
            selectedShip.regenPerSecond = dataSaved.regenPerSecond;
            selectedShip.constantRegenPerSecond = dataSaved.constantRegenPerSecond;

            selectedShip.playerUpgrades.Clear();
            for(int i = 0; i < dataSaved.numberOfUpgrades; i++)
            {
                selectedShip.upgradesIndex[i] = dataSaved.upgradesIndex[i];
                selectedShip.playerUpgrades.Add(possibleUpgrades[dataSaved.upgradesIndex[i]]);
            }
        }

        gems = dataSaved.gems;
        goldCoins = dataSaved.goldCoins;
        gameHasEnded = dataSaved.gameHasEnded;
        level = dataSaved.level[0];
        subLevel = dataSaved.level[1];
        enemiesKilled = dataSaved.enemiesKilled;
        gameStartTime = Time.time;
        respawnsRemaining = dataSaved.respawnsRemaining;
        targetFps = dataSaved.targetFps;
        qualityIndex = dataSaved.qualityIndex;
        hasCompletedTutorial = dataSaved.hasCompletedTutorial;
        hasCompletedButtonsTutorial = dataSaved.hasCompletedButtonsTutorial;
        controllerOn = dataSaved.controllerOn;
        levelsPlayed = dataSaved.levelsPlayed;
        //hasCompletedStoryMode = dataSaved.hasCompletedStoryMode;
        isMusicMuted = dataSaved.isMusicMuted;
        isSoundMuted = dataSaved.isSoundMuted;
    }
    public void FpsSettings()
    {
        i++;
        if (i > 4)
            i = 1;
        switch (i)
        {
            case 1:
                targetFps = 30;
                break;
            case 2:
                targetFps = 60;
                break;
            case 3:
                targetFps = 120;
                break;
            case 4:
                targetFps = 144;
                break;
        }
        //Application.targetFrameRate = targetFps;
        Time.fixedDeltaTime = 1f / targetFps;
        dataSaved.targetFps = targetFps;
    }

    public void ChangeQuality()
    {
        qualityIndex = QualitySettings.GetQualityLevel();
        qualityIndex++;
        if (qualityIndex > 3)
            qualityIndex = 0;
        QualitySettings.SetQualityLevel(qualityIndex, true);
        dataSaved.qualityIndex = qualityIndex;
        qualityText.text = QualitySettings.names[QualitySettings.GetQualityLevel()];
       // Debug.Log(QualitySettings.names[QualitySettings.GetQualityLevel()]);
    }

    private void OnApplicationQuit()
    {
        if (isInMenu)
        {
            Save();
        }
        if (SceneManager.GetActiveScene().buildIndex == 1)
        {
            gameHasEnded = true;
            enableSaving = true;
            Save();
        }
    }

    public void ControlSetup()
    {
        controls.Gameplay.Touchpress.performed += ctx => TouchDown(ctx);
    }

    private void TouchDown(InputAction.CallbackContext context)
    {
        OnTouch?.Invoke();
    }
}

/*         string path = Application.persistentDataPath + "/player.save";
        if (File.Exists(path))
        {
            LoadPlayerData();
        }*/
