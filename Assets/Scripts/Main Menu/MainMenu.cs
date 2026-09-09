using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using SpaceshipDivine.Haptics;

public class MainMenu : MonoBehaviour
{
    public Spaceship tutorialSpaceship;
    public TextMeshProUGUI resolutionText;
    public static MainMenu instance;
    public Image colorImage;
    public Button lastGameButton;
    public TextMeshProUGUI qualityText;
    public TextMeshProUGUI fpsText;
    public TextMeshProUGUI lastLevelText;
    [SerializeField]
    public GameObject optionsMenu, mainMenu, spacecraftMenu, infoMenu, gemShop, checkForNewGameMenu, unlockInstructionsMenu, feedbackForm, buttonsTutorial;
    public Slider cameraSpeedSlider;
    public Image spacecraftSelectImage;
    public List<Spaceship> shipInstances = new List<Spaceship>();
    public List<Spaceship> shipPrefabs = new List<Spaceship>();
    public List<Button> buttons = new List<Button>();
    public Transform buttonHolder;
    public Button startGameButton;
    public TextMeshProUGUI gemsText;
    public TextMeshProUGUI tapToStartText;
    public TextMeshProUGUI credits;
    public bool hasStarted = false;
    DataHolder data;
    public Image checkboxX;
    [SerializeField]
    public GameObject controllerLayout;

    [Header("Info Menu")]
    public Image shipImage;
    public TextMeshProUGUI nameInfo;
    public Button upgradeShipButton, unlockShipButton;
    public Image gemImage;

    public Image abilityImage;
    public TextMeshProUGUI abilityName;
    public TextMeshProUGUI abilityDescription;
    public List<Image> starsFill = new List<Image>();
    public List<GameObject> stars;
    public int infoMenuTrackedShip;
    private bool isSelected;
    private int selectedShipOrderNumber;

    public Image primaryGunImage, secondaryGunImage;
    public TextMeshProUGUI primaryGunCooldown, secondaryGunCooldown, primaryGunDescription, secondaryGunDescription;

    public Image musicImage, soundImage, hapticsImage;
    public Sprite musicSprite, musicMuteSprite, soundSprite, soundMuteSprite;
    public Sprite hapticsSprite, hapticsMuteSprite;
    public AudioClip mainMenuMusic;

    void Awake()
    {
        instance = this;
    }

    void Start()
    {
        data = DataHolder.instance;
        data.mainMenu = this;
        data.isInMenu = true;
        gemShop.SetActive(true);
        for(int i = 0; i < shipPrefabs.Count; i++)
        {
            shipInstances[i].selected = false;
            shipPrefabs[i].selected = false;
        }
        if (data.gameHasEnded || data.level == 0)
        {
            data.gameHasEnded = true;
            lastGameButton.gameObject.SetActive(false);
        }
        else
        {
            lastGameButton.gameObject.SetActive(true);
            lastLevelText.text = "Level " + data.level + "-" + data.subLevel;
        }

        data.qualityText = qualityText;
        qualityText.text = QualitySettings.names[QualitySettings.GetQualityLevel()];
        startGameButton.interactable = false;

        if (data.controllerOn)
            checkboxX.enabled = true;
        else
            checkboxX.enabled = false;

        // These used to set only the icon, so a player who muted saw a muted icon and still
        // heard the game — and the next press then unmuted the icon while muting the audio.
        if (data.isMusicMuted)
        {
            musicImage.sprite = musicMuteSprite;
            SoundManager.instance.musicSource.mute = true;
        }
        if (data.isSoundMuted)
        {
            soundImage.sprite = soundMuteSprite;
            SoundManager.instance.soundSource.mute = true;
        }
        if (hapticsImage != null && hapticsMuteSprite != null && data.isHapticsMuted)
            hapticsImage.sprite = hapticsMuteSprite;

        SoundManager.instance.musicSource.clip = mainMenuMusic;
        SoundManager.instance.musicSource.Play();
    }
    public void RefreshData()
    {
        qualityText.text = QualitySettings.names[QualitySettings.GetQualityLevel()];
        for (int i = 0; i < shipPrefabs.Count; i++)
        {
            if (shipInstances[i].selected == false)
            {
                shipInstances[i].ability = shipPrefabs[i].ability;
                shipInstances[i].signatureAbility = shipPrefabs[i].signatureAbility;
                shipInstances[i].gun = shipPrefabs[i].gun;
                shipInstances[i].shipSprite = shipPrefabs[i].shipSprite;
                shipInstances[i].maxHealth = shipPrefabs[i].maxHealth;
                shipInstances[i].damagePerBullet = shipPrefabs[i].damagePerBullet;
                shipInstances[i].bulletPushBack = shipPrefabs[i].bulletPushBack;
                shipInstances[i].maxMoveSpeed = shipPrefabs[i].maxMoveSpeed;
                shipInstances[i].fireRate = shipPrefabs[i].fireRate;
                shipInstances[i].spread = shipPrefabs[i].spread;
                shipInstances[i].numberOfBursts = shipPrefabs[i].numberOfBursts;
                shipInstances[i].waitTimeBetweenBursts = shipPrefabs[i].waitTimeBetweenBursts;
                shipInstances[i].critChance = shipPrefabs[i].critChance;
                shipInstances[i].critMultiplier = shipPrefabs[i].critMultiplier;
                shipInstances[i].bulletsShot = shipPrefabs[i].bulletsShot;
                shipInstances[i].isUnlocked = shipPrefabs[i].isUnlocked;
                shipInstances[i].priceToUnlock = shipPrefabs[i].priceToUnlock;
                shipInstances[i].audioIndex = shipPrefabs[i].audioIndex;
                shipInstances[i].bulletPrefab = shipPrefabs[i].bulletPrefab;
                shipInstances[i].defenseMultiplier = shipPrefabs[i].defenseMultiplier;
                shipInstances[i].attackMultiplier = shipPrefabs[i].attackMultiplier;
                shipInstances[i].speedMultiplier = shipPrefabs[i].speedMultiplier;               
                shipInstances[i].attackUpgradeMultiplier = shipPrefabs[i].attackUpgradeMultiplier;
                shipInstances[i].defenseUpgradeMultiplier = shipPrefabs[i].defenseUpgradeMultiplier;
                shipInstances[i].speedUpgradeMultiplier = shipPrefabs[i].speedUpgradeMultiplier;
                shipInstances[i].playerUpgrades = shipPrefabs[i].playerUpgrades;
                shipInstances[i].regenAmount = shipPrefabs[i].regenAmount;
                shipInstances[i].canBounce = shipPrefabs[i].canBounce;
                shipInstances[i].canPierce = shipPrefabs[i].canPierce;
                shipInstances[i].canExplode = shipPrefabs[i].canExplode;
                shipInstances[i].chanceToBlockAttack = shipPrefabs[i].chanceToBlockAttack;
                shipInstances[i].moveSpeedOnAbility = shipPrefabs[i].moveSpeedOnAbility;
                shipInstances[i].moveSpeedIncrease = shipPrefabs[i].moveSpeedIncrease; 
                shipInstances[i].pushBackOnAbility = shipPrefabs[i].pushBackOnAbility; 
                shipInstances[i].pushBackStrength = shipPrefabs[i].pushBackStrength; 
                shipInstances[i].moveSpeedOnEnemyKill = shipPrefabs[i].moveSpeedOnEnemyKill; 
                shipInstances[i].resetCdOnKill = shipPrefabs[i].resetCdOnKill; 
                shipInstances[i].moveSpeedAmount = shipPrefabs[i].moveSpeedAmount; 
                shipInstances[i].resetCdChance = shipPrefabs[i].resetCdChance; 
                shipInstances[i].regenPerSecond = shipPrefabs[i].regenPerSecond; 
                shipInstances[i].constantRegenPerSecond = shipPrefabs[i].constantRegenPerSecond;
                shipInstances[i].primarySound = shipPrefabs[i].primarySound;
                shipInstances[i].secondarySound = shipPrefabs[i].secondarySound;
            }
            shipInstances[i].secondaryGun = shipPrefabs[i].secondaryGun;
            // Below the guard, deliberately. Inside the `selected == false` block above,
            // this copy would skip the SELECTED ship - the one whose id is the only one
            // RunState.selectedShipId actually needs.
            shipInstances[i].shipId = shipPrefabs[i].shipId;
            shipInstances[i].playerUpgrades.Clear();
            shipInstances[i].upgradesIndex.Clear();

        }
        for (int i = 0; i < shipPrefabs.Count; i++)
        {
            Image[] buttonImages = buttons[i].GetComponentsInChildren<Image>();
            buttonImages[1].sprite = SetShipImage(i);
            if (!shipPrefabs[i].isUnlocked)
            {
                Color color = buttons[i].image.color;
                color.a = .45f;
                buttons[i].image.color = color;
                buttonImages[2].enabled = true;
                buttonImages[3].enabled = false;
                if (shipPrefabs[i].canBeUnlockedInGame && shipPrefabs[i].priceToUnlock != 0)
                    buttonImages[4].enabled = true;
            }
            else
            {
                buttons[i].image.color = Color.white;
                buttonImages[2].enabled = false;
                buttonImages[3].enabled = true;
                buttonImages[4].enabled = false;
            }
            buttons[i].GetComponentInChildren<TextMeshProUGUI>().text = shipPrefabs[i].shipName;
        }

        ButtonShipNumber[] shipButtons = buttonHolder.GetComponentsInChildren<ButtonShipNumber>();
        for(int i = 0; i < shipButtons.Length - 1; i++)
        {
            if (!shipPrefabs[shipButtons[i].shipOrderNumber].isUnlocked)
            {
                for (int j = i + 1; j < shipButtons.Length; j++)
                {
                    if (shipPrefabs[shipButtons[j].shipOrderNumber].isUnlocked)
                    {
                        shipButtons[j].transform.SetSiblingIndex(shipButtons[i].transform.GetSiblingIndex());
                        shipButtons = buttonHolder.GetComponentsInChildren<ButtonShipNumber>();
                        break;
                    }
                }
            }
        }
        RefreshGems();
}

    public void OpenCloseMainMenu()
    {
        if(!mainMenu.activeSelf)
        {
            StartCoroutine(ColorMenu());
            tapToStartText.gameObject.SetActive(false);
            StopCoroutine(CloseMainMenu());
            mainMenu.SetActive(true);
            mainMenu.GetComponent<Animator>().SetTrigger("open");
            if(!DataHolder.instance.hasCompletedButtonsTutorial)
            {
                StartCoroutine(OpenButtonsTutorial());
            }
        }
        else
        {
            StartCoroutine(DecolorMenu());
            StartCoroutine(CloseMainMenu());
            tapToStartText.gameObject.SetActive(true);
        }
    }
    IEnumerator ColorMenu()
    {
        StopCoroutine(DecolorMenu());
        float elapsed = 0;
        colorImage.color = Color.black;
        while(elapsed < .25f)
        {
            colorImage.color = Color.Lerp(Color.black, Color.white, elapsed / .25f);
            credits.color = colorImage.color;
            elapsed += Time.deltaTime;
            yield return null;
        }
        colorImage.color = Color.white;
        credits.color = colorImage.color;
    }
    IEnumerator OpenButtonsTutorial()
    {
        buttonsTutorial.GetComponent<ButtonTutorial>().raycastBlocker.enabled = true;
        yield return new WaitForSeconds(.75f);
        buttonsTutorial.SetActive(true);
    }
    IEnumerator DecolorMenu()
    {
        StopCoroutine(ColorMenu());
        float elapsed = 0;
        colorImage.color = Color.white;
        while (elapsed < .25f)
        {
            colorImage.color = Color.Lerp(Color.white, Color.black, elapsed / .25f);
            credits.color = colorImage.color;
            elapsed += Time.deltaTime;
            yield return null;
        }
        colorImage.color = Color.black;
        credits.color = colorImage.color;
    }

    IEnumerator CloseMainMenu()
    {
        mainMenu.GetComponent<Animator>().SetTrigger("close");
        yield return new WaitForSeconds(mainMenu.GetComponent<Animator>().GetCurrentAnimatorStateInfo(0).length + .05f);
        mainMenu.SetActive(false);
        //tapToStartText.gameObject.SetActive(true);
    }
    public void ChangeQuality()
    {
        data.ChangeQuality();
    }
    public void OpenFeedbackForm()
    {
        feedbackForm.SetActive(true);
        StartCoroutine(CloseMainMenu());
    }

    public void BackButton()
    {
        if (optionsMenu.activeSelf)
            StartCoroutine(CloseOptionsMenu());
        if (spacecraftMenu.activeSelf)
        {
            StartCoroutine(CloseSpacecraftSelectMenu());
            spacecraftSelectImage.gameObject.SetActive(false);
        }
        else
        {
            spacecraftSelectImage.gameObject.SetActive(false);
            mainMenu.SetActive(true);
        }
    }

    public void OptionsMenu()
    {
        StartCoroutine(CloseMainMenu());
        optionsMenu.SetActive(true);
        qualityText.text = QualitySettings.names[QualitySettings.GetQualityLevel()];
    }
    IEnumerator CloseOptionsMenu()
    {
        optionsMenu.GetComponent<Animator>().SetTrigger("close");
        yield return new WaitForSeconds(optionsMenu.GetComponent<Animator>().GetCurrentAnimatorStateInfo(0).length + .05f);
        optionsMenu.SetActive(false);
    }
    IEnumerator CloseSpacecraftSelectMenu()
    {
        spacecraftMenu.GetComponent<Animator>().SetTrigger("close");
        yield return new WaitForSeconds((spacecraftMenu.GetComponent<Animator>().GetCurrentAnimatorStateInfo(0).length + .05f) / 2f);
        mainMenu.SetActive(true);
        yield return new WaitForSeconds((spacecraftMenu.GetComponent<Animator>().GetCurrentAnimatorStateInfo(0).length + .05f) / 2f);
        spacecraftMenu.SetActive(false);

    }

    public void StartNewGame()
    {
        SoundManager.instance.musicSource.Stop();

        data.selectedShip.currentHealth = data.selectedShip.maxHealth;
        data.selectedShip.minHealth = data.selectedShip.maxHealth;
        data.selectedShip.ability = null;
        data.selectedShip.gun.Reset();
        data.isInMenu = false;
        data.respawnsRemaining = 1;
        data.levelsPlayed = Random.Range(1, 5).ToString();
        data.timeSinceGameStarted = Time.time;
        data.Save();
        StartCoroutine(LevelLoader.instance.LoadLevel(data.levelsPlayed.Substring(data.levelsPlayed.Length - 1)));
    }
    public void StartLastGame()
    {
        SoundManager.instance.musicSource.Stop();

        data.timeSinceGameStarted = Time.time - data.dataSaved.timeSinceGameStarted;
        StartCoroutine(LevelLoader.instance.LoadLevel(data.levelsPlayed.Substring(data.levelsPlayed.Length - 1)));
    }
    public void TrainingMode()
    {
        SoundManager.instance.soundSource.Stop();

        data.level = 0;
        data.subLevel = 1;
        foreach (Spaceship ship in shipPrefabs)
            ship.gun.Reset();
        data.selectedShip = shipPrefabs[0];
        data.selectedShip.currentHealth = data.selectedShip.maxHealth;
        data.selectedShip.minHealth = data.selectedShip.maxHealth;
        data.isInMenu = false;
        data.Save();
        StartCoroutine(LevelLoader.instance.LoadLevel("Training Mode"));
    }
    public void AddGems(int x)
    {
        data.gems += x;
        data.Save();
        RefreshGems();
    }

    public void CheckForNewGame()
    {
        if(data.gameHasEnded == false)
        {
            checkForNewGameMenu.SetActive(true);
        }
        else
        {
            SpacecraftSelectMenu();
        }
    }
    public void CloseCheckForNewGameMenu()
    {
        checkForNewGameMenu.SetActive(false);
    }
    public void SpacecraftSelectMenu()
    {
        if (data.hasCompletedTutorial)
        {
            checkForNewGameMenu.SetActive(false);
            spacecraftMenu.SetActive(true);
            StartCoroutine(CloseMainMenu());
            if (data.selectedShip)
                data.selectedShip.selected = false;
            data.selectedShip = null;
            data.dataSaved.orderNumber = -1;
            data.Load();
            for (int i = 0; i < shipPrefabs.Count; i++)
            {
                shipInstances[i].selected = false;
                shipPrefabs[i].selected = false;
            }
            if (selectedShipOrderNumber >= 0)
            {
                shipInstances[selectedShipOrderNumber].selected = false;
                startGameButton.interactable = false;
                buttons[selectedShipOrderNumber].GetComponentsInChildren<Image>()[0].color = new Color(.6415f, .6415f, .6415f, 1);
                isSelected = false;
                data.selectedShip = null;
            }
            RefreshData();
            data.gems += (int)(data.goldCoins * data.coinsMultiplier);
            data.goldCoins = 0;
            data.dataSaved.totalEnemiesKilled += data.enemiesKilled;
            data.enemiesKilled = 0;
            data.level = 1;
            data.subLevel = 1;
            lastGameButton.gameObject.SetActive(false);
            data.gameHasEnded = true;

            spacecraftSelectImage.gameObject.SetActive(true);
        }
        else
        {
            SoundManager.instance.musicSource.Stop();
            data.selectedShip = tutorialSpaceship;
            data.selectedShip.currentHealth = data.selectedShip.maxHealth;
            data.selectedShip.minHealth = data.selectedShip.maxHealth;
            data.selectedShip.ability = null;
            data.isInMenu = false;
            data.level = 0;
            data.subLevel = 1;
            data.Save();
            StartCoroutine(LevelLoader.instance.LoadLevel("Tutorial"));
        }
    }

    public void SelectShip(int orderNumber)
    {
        if (shipPrefabs[orderNumber].isUnlocked)
        {
            if (shipInstances[orderNumber].selected)
            {
                shipInstances[orderNumber].selected = false;
                startGameButton.interactable = false;
                buttons[orderNumber].GetComponentsInChildren<Image>()[0].color = new Color(.6415f, .6415f, .6415f, 1);
                isSelected = false;
                data.selectedShip = null;
            }
            else
            {
                if(isSelected)
                {
                    shipInstances[selectedShipOrderNumber].selected = false;
                    buttons[selectedShipOrderNumber].GetComponentsInChildren<Image>()[0].color = new Color(.6415f, .6415f, .6415f, 1);
                }
                shipInstances[orderNumber].selected = true;
                startGameButton.interactable = true;
                buttons[orderNumber].GetComponentsInChildren<Image>()[0].color = new Color(1, .5f, 0, 1);
                isSelected = true;
                selectedShipOrderNumber = orderNumber;
                data.selectedShip = shipInstances[orderNumber];
            }
        }
        else
        {
            ShipsInformation(orderNumber);
        }
    }

    public void ShipsInformation(int i)
    {
        shipInstances[selectedShipOrderNumber].selected = false;
        startGameButton.interactable = false;
        buttons[selectedShipOrderNumber].GetComponentsInChildren<Image>()[0].color = new Color(.6415f, .6415f, .6415f, 1);
        isSelected = false;
        data.selectedShip = null;

        infoMenu.SetActive(true);
        infoMenuTrackedShip = i;

        shipImage.sprite = SetShipImage(i);
        nameInfo.text = shipInstances[i].shipName;

        float dps = (shipPrefabs[i].damagePerBullet * shipPrefabs[i].bulletsShot * shipPrefabs[i].numberOfBursts * (shipPrefabs[i].alternateFirePoints ? 1 : shipPrefabs[i].firePoints.Count )) /
            (shipPrefabs[i].waitTimeBetweenBursts * shipPrefabs[i].numberOfBursts + shipPrefabs[i].fireRate) 
            * (1 + shipPrefabs[i].critChance / 100f);
        float defense = shipPrefabs[i].maxHealth * .8f;
        float speed = (shipPrefabs[i].maxMoveSpeed - 1.5f) < 0 ? 0 : (shipPrefabs[i].maxMoveSpeed - 1.5f) * 6f;
        
        dps += shipPrefabs[i].bonusAttackStat + (shipInstances[i].attackMultiplier - 12.5f) / 3f + shipInstances[i].damageMultiplier / 4f;
        defense += shipPrefabs[i].bonusDefenseStat + (shipInstances[i].defenseMultiplier - 12.5f) / 2f + shipInstances[i].damageReduction / 4f;
        speed += shipPrefabs[i].bonusSpeedStat + (shipInstances[i].speedMultiplier - 20f) / 4f;
        Stats stats = new Stats(dps, defense, speed);
        UIStatsRadarChart.instance.SetStats(stats);

        abilityImage.sprite = shipPrefabs[i].signatureAbility.abilitySprite;
        abilityDescription.text = shipPrefabs[i].signatureAbility.description;
        abilityName.text = shipPrefabs[i].signatureAbility.abilityName;

        if (shipPrefabs[i].isUnlocked)
        {
            for (int j = 0; j < 5; j++)
            {
                stars[j].SetActive(true);
                if (j >= shipPrefabs[i].level)
                    starsFill[j].gameObject.SetActive(false);
                else
                    starsFill[j].gameObject.SetActive(true);
            }
        }
        else
        {
            for (int j = 0; j < 5; j++)
            {
                stars[j].SetActive(false);
            }
        }
        

        if (!shipPrefabs[i].isUnlocked) // if ship is not unlocked
        {
            unlockShipButton.gameObject.SetActive(true);
            upgradeShipButton.gameObject.SetActive(false);
            if (shipPrefabs[i].canBeBoughtWithGems)
            {
                unlockShipButton.GetComponentInChildren<TextMeshProUGUI>().text = ((int)shipPrefabs[i].priceToUnlock).ToString();
                unlockShipButton.GetComponentsInChildren<Image>()[1].enabled = true;
                unlockShipButton.GetComponentsInChildren<Image>()[2].enabled = false;
                unlockShipButton.GetComponentsInChildren<Image>()[3].enabled = false;
            }
            else if (shipPrefabs[i].canBeBoughtWithCurrency)
            {
                unlockShipButton.GetComponentInChildren<TextMeshProUGUI>().text = shipPrefabs[i].priceToUnlock + " \u20AC";
                unlockShipButton.GetComponentsInChildren<Image>()[1].enabled = false;
                unlockShipButton.GetComponentsInChildren<Image>()[2].enabled = false;
                unlockShipButton.GetComponentsInChildren<Image>()[3].enabled = false;
            }
            else if (shipPrefabs[i].canBeUnlockedInGame)
            {
                unlockShipButton.GetComponentInChildren<TextMeshProUGUI>().text = "";
                unlockShipButton.GetComponentsInChildren<Image>()[1].enabled = false;
                unlockShipButton.GetComponentsInChildren<Image>()[2].enabled = true;
                unlockShipButton.GetComponentsInChildren<Image>()[3].enabled = true;
                unlockShipButton.GetComponentsInChildren<Image>()[3].sprite = SetShipImage(i);

            }
        }
        else // if ship is unlocked
        {
            unlockShipButton.gameObject.SetActive(false);
            upgradeShipButton.gameObject.SetActive(true);
            if (data.dataSaved.shipLevel[infoMenuTrackedShip] < 5)
            {
                gemImage.gameObject.SetActive(true);
                upgradeShipButton.interactable = true;
                upgradeShipButton.GetComponentInChildren<TextMeshProUGUI>().text = (500 * Mathf.Pow(2, (shipPrefabs[i].level - 1))).ToString();
            }
            else
            {
                gemImage.gameObject.SetActive(false);
                upgradeShipButton.interactable = false;
                upgradeShipButton.GetComponentInChildren<TextMeshProUGUI>().text = "Max level";
            }
        }

        //guns information
        primaryGunImage.sprite = shipPrefabs[i].primaryGunSprite;
        secondaryGunImage.sprite = shipPrefabs[i].secondaryGunSprite;
        float fireRate = shipPrefabs[i].fireRate;
        int numberOfDecimals = 1;
        fireRate *= 100;
        if ((int)fireRate % 10 != 0)
            numberOfDecimals = 2;
        primaryGunCooldown.text = shipPrefabs[i].fireRate.ToString(numberOfDecimals == 1 ? "F1" : "F2") + (shipPrefabs[i].fireRate != 1 ? " seconds" : " second");
        fireRate = shipPrefabs[i].secondaryGun.cooldown;
        fireRate *= 10;
        numberOfDecimals = 0;
        if ((int)fireRate % 10 != 0)
            numberOfDecimals = 1;
        secondaryGunCooldown.text = shipPrefabs[i].secondaryGun.cooldown.ToString(numberOfDecimals == 0 ? "F0" : "F1") + (shipPrefabs[i].secondaryGun.cooldown != 1 ? " seconds" : " second");
        primaryGunDescription.text = shipPrefabs[i].primaryGunDescription;
        secondaryGunDescription.text = shipPrefabs[i].secondaryGunDescription;

    }

    public void OpenControllerLayout()
    {
        controllerLayout.SetActive(true);
    }
    public void UnlockShip()
    {
        if(shipPrefabs[infoMenuTrackedShip].canBeBoughtWithGems)
        {
            if(data.gems >= shipPrefabs[infoMenuTrackedShip].priceToUnlock)
            {
                data.gems -= (int)shipPrefabs[infoMenuTrackedShip].priceToUnlock;
                SoundManager.instance.soundSource.PlayOneShot(SoundManager.instance.UISounds[0]);
                shipPrefabs[infoMenuTrackedShip].isUnlocked = true;
                data.dataSaved.isUnlocked[infoMenuTrackedShip] = true;
                data.Save();
                data.Load();
            }
            else
            {
                OpenGemShop();
            }
        } else if(shipPrefabs[infoMenuTrackedShip].canBeBoughtWithCurrency)
        {
            // Real-money ship. The grant happens only in GrantShipAfterPurchase, called from
            // IAPManager.ProcessPurchase once the store confirms. Never grant here.
            if (IAPManager.instance == null)
            {
                Debug.LogWarning("UnlockShip: IAPManager.instance is null, cannot start purchase.");
            }
            else
            {
                IAPManager.instance.BuyShip(shipPrefabs[infoMenuTrackedShip].shipId);
            }
        } else if(shipPrefabs[infoMenuTrackedShip].canBeUnlockedInGame)
        {
            if(shipPrefabs[infoMenuTrackedShip].priceToUnlock == 0)
            {
                unlockInstructionsMenu.SetActive(true);
                unlockInstructionsMenu.GetComponentInChildren<TextMeshProUGUI>().text = shipPrefabs[infoMenuTrackedShip].unlockInstructions;
            }
            else
            {
                SoundManager.instance.soundSource.PlayOneShot(SoundManager.instance.UISounds[0]);
                shipPrefabs[infoMenuTrackedShip].isUnlocked = true;
                data.dataSaved.isUnlocked[infoMenuTrackedShip] = true;
                data.Save();
                data.Load();
            }
        }
        RefreshData();
        ShipsInformation(infoMenuTrackedShip);
    }

    /// <summary>
    /// Called by IAPManager once a store purchase is confirmed. This is the ONLY path that
    /// may grant a real-money ship.
    /// </summary>
    public void GrantShipAfterPurchase(string shipId)
    {
        for (int i = 0; i < shipPrefabs.Count; i++)
        {
            if (shipPrefabs[i].shipId != shipId) continue;

            SoundManager.instance.soundSource.PlayOneShot(SoundManager.instance.UISounds[0]);
            shipPrefabs[i].isUnlocked = true;
            data.dataSaved.isUnlocked[i] = true;
            data.Save();
            data.Load();
            RefreshData();
            ShipsInformation(i);
            return;
        }
        Debug.LogWarning("GrantShipAfterPurchase: unknown shipId " + shipId);
    }

    public void UpgradeShip()
    {
        int upgradePrice = (int)(500 * Mathf.Pow(2, (shipPrefabs[infoMenuTrackedShip].level - 1)));
        if (data.gems >= upgradePrice)
        {
            data.gems -= upgradePrice;
            SoundManager.instance.soundSource.PlayOneShot(SoundManager.instance.UISounds[0]);
            shipPrefabs[infoMenuTrackedShip].level++;
            data.dataSaved.shipLevel[infoMenuTrackedShip] = shipPrefabs[infoMenuTrackedShip].level;
            ApplyShipUpgrades();
            data.Save();
            data.Load();
        }
        else
        {
            OpenGemShop();
        }
        RefreshData();
        ShipsInformation(infoMenuTrackedShip);

    }

    public void ApplyShipUpgrades()
    {
        shipPrefabs[infoMenuTrackedShip].damageMultiplier += 2f * shipPrefabs[infoMenuTrackedShip].attackUpgradeMultiplier;      // * (shipPrefabs[infoMenuTrackedShip].level - 1);
        shipPrefabs[infoMenuTrackedShip].maxHealth += .5f * shipPrefabs[infoMenuTrackedShip].defenseUpgradeMultiplier;         // * (shipPrefabs[infoMenuTrackedShip].level - 1);
        shipPrefabs[infoMenuTrackedShip].spread *= (1 - .125f) * shipPrefabs[infoMenuTrackedShip].attackUpgradeMultiplier;         // * (shipPrefabs[infoMenuTrackedShip].level - 1);
        shipPrefabs[infoMenuTrackedShip].maxMoveSpeed += .075f * shipPrefabs[infoMenuTrackedShip].speedUpgradeMultiplier;         // * (shipPrefabs[infoMenuTrackedShip].level - 1);
        shipPrefabs[infoMenuTrackedShip].critChance += 2.5f * shipPrefabs[infoMenuTrackedShip].attackUpgradeMultiplier;             // * (shipPrefabs[infoMenuTrackedShip].level - 1);
        shipPrefabs[infoMenuTrackedShip].attackMultiplier += 2f * shipPrefabs[infoMenuTrackedShip].attackUpgradeMultiplier;            // * (shipPrefabs[infoMenuTrackedShip].level - 1);
        shipPrefabs[infoMenuTrackedShip].defenseMultiplier += 2.25f * shipPrefabs[infoMenuTrackedShip].defenseUpgradeMultiplier;          //   * (shipPrefabs[infoMenuTrackedShip].level - 1);
        shipPrefabs[infoMenuTrackedShip].speedMultiplier += 2f * shipPrefabs[infoMenuTrackedShip].speedUpgradeMultiplier;            // * (shipPrefabs[infoMenuTrackedShip].level - 1);
        shipPrefabs[infoMenuTrackedShip].damageReduction += 1.25f * shipPrefabs[infoMenuTrackedShip].defenseUpgradeMultiplier;          // * (shipPrefabs[infoMenuTrackedShip].level - 1);
    }
    public void CloseInfoMenu()
    {
        
        StartCoroutine(CloseInfoMenuC());
    }
    IEnumerator CloseInfoMenuC()
    {
        infoMenu.GetComponent<Animator>().SetTrigger("close");
        yield return new WaitForSeconds(infoMenu.GetComponent<Animator>().GetCurrentAnimatorStateInfo(0).length + .05f);
        UIStatsRadarChart.instance.ClearMesh();
        infoMenu.SetActive(false);
    }
    public void OpenGemShop()
    {
        gemShop.SetActive(true);
    }

    public void CloseGemShop()
    {
        StartCoroutine(CloseGemShopC());
    }

    IEnumerator CloseGemShopC()
    {
        gemShop.GetComponent<Animator>().SetTrigger("close");
        yield return new WaitForSeconds(gemShop.GetComponent<Animator>().GetCurrentAnimatorStateInfo(0).length + .05f);
        gemShop.SetActive(false);
    }
    public void RefreshGems()
    {
        gemsText.text = DataHolder.instance.gems.ToString();
    }

    public void PlayRewardedAd()
    {
        //Monetization.instance.rewardedAction = 0;
        //Monetization.instance.DisplayRewardedAd();
    }

    public void FpsSettings()
    {
        data.FpsSettings();
    }

    public void CloseUnlockInstructionsMenu()
    {
        StartCoroutine(CloseUnlockInstructions());
    }

    IEnumerator CloseUnlockInstructions()
    {
        unlockInstructionsMenu.GetComponent<Animator>().SetTrigger("close");
        yield return new WaitForSeconds(unlockInstructionsMenu.GetComponent<Animator>().GetCurrentAnimatorStateInfo(0).length + .05f);
        unlockInstructionsMenu.SetActive(false);
    }
    public Sprite SetShipImage(int i)
    {
        if (shipPrefabs[i].shipImage)
            return shipPrefabs[i].shipImage;
        else
            return shipPrefabs[i].shipSprite;
    }

    public void GamepadControls()
    {
        if(data.controllerOn)
        {
            data.controllerOn = false;
            checkboxX.enabled = false;
        }
        else
        {
            data.controllerOn = true;
            checkboxX.enabled = true;
        }
    }
    public void ChangeCameraSpeed()
    {
        DataHolder.instance.cameraSpeedLevel = (int)cameraSpeedSlider.value;
        if(CameraMovement.instance)
            CameraMovement.instance.cameraSpeedLevel = 6 - DataHolder.instance.cameraSpeedLevel;
    }
    public void MusicOnOff()
    {
        SoundManager sound = SoundManager.instance;
        if (sound.musicSource.mute)
        {
            sound.musicSource.mute = false;
            musicImage.sprite = musicSprite;
            data.isMusicMuted = false;
        }
        else
        {
            sound.musicSource.mute = true;
            musicImage.sprite = musicMuteSprite;
            data.isMusicMuted = true;
        }
    }

    public void SoundOnOff()
    {
        SoundManager sound = SoundManager.instance;
        if (sound.soundSource.mute)
        {
            sound.soundSource.mute = false;
            soundImage.sprite = soundSprite;
            data.isSoundMuted = false;
        }
        else
        {
            sound.soundSource.mute = true;
            soundImage.sprite = soundMuteSprite;
            data.isSoundMuted = true;
        }
    }

    /// <summary>
    /// Third settings toggle, same shape as MusicOnOff/SoundOnOff. The icon fields are optional
    /// so the toggle can be wired into the settings menu later without a null reference here.
    /// </summary>
    public void HapticsOnOff()
    {
        bool nowMuted = !Haptics.Muted;
        Haptics.Muted = nowMuted;
        data.isHapticsMuted = nowMuted;

        if (hapticsImage != null && hapticsSprite != null && hapticsMuteSprite != null)
            hapticsImage.sprite = nowMuted ? hapticsMuteSprite : hapticsSprite;

        // Confirm by feel that it is back on. Nothing to play when switching off.
        if (!nowMuted)
            Haptics.Play(Haptic.Selection);
    }

    //public enum PurchaseType { removeAds, gems1000, gems2250, gems5000, gems10000};
    //PurchaseType purchaseType;

    public void ClickPurchaseButton(int purchaseTypeIndex) //0 - remove ads, 1 - small, 2 - medium, 3 - large, 4 - very large
    {
        switch (purchaseTypeIndex)
        {
            case 0:
                Debug.Log("Removed Ads");
                IAPManager.instance.BuyRemoveAds();
                break;
            case 1:
                IAPManager.instance.SmallGems();
                break;
            case 2:
                IAPManager.instance.MediumGems();
                break;
            case 3:
                IAPManager.instance.LargeGems();
                break;
        }
    }

}
