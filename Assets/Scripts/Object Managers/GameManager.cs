using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class GameManager : MonoBehaviour
{
    public static GameManager instance;
    PlayerController p;
    ShipSelect shipSelect;
    SoundManager sound;
    public AudioClip levelMusic;
    private int oldPower;
    public Image[] powerMask = new Image[3];
    public Image checkboxX;

    public List<Enemy> possibleEnemies = new List<Enemy>();
    public List<Enemy> enemyTypes = new List<Enemy>();
    public Enemy boss;
    public Enemy[] bosses;
    public int basicEnemies, enhancedEnemies, numberOfEnemies;
    public int level = 1;
    public int subLevel = 1;
    public int goldCoins;
    [SerializeField]
    public Transform damagePopupPrefab;
    [SerializeField]
    public DamagePopup criticalText;
    public DamagePopup immuneText;
    public RoomChest chest;
    public TextMeshProUGUI levelText;
    public int chanceToDropAbility;
    public Transform abilityDrop;
    public UpgradeDrop upgradeDrop;
    public bool shouldDropBossBlueprint = false;

    [Header("Pause Menu")]
    public Image pauseMenuImage;
    [SerializeField]
    public GameObject pauseMenu;
    [SerializeField]
    public Image shipImage;
    public TextMeshProUGUI attackText, defenseText, speedText;
    public GameObject settingsMenu;
    public Transform settingsMenuBG;
    public TextMeshProUGUI qualityText;

    [Header("Respawn Menu")]
    [SerializeField]
    public GameObject respawnMenu;
    public TextMeshProUGUI gemsAmountToRevive;
    public int gemsAmountToReviveValue;

    [Header("Gem Shop")]
    [SerializeField]
    public GameObject gemShop;
    public TextMeshProUGUI gemsText;
    public TextMeshProUGUI coinText;

    [Header("Death Menu")]
    [SerializeField]
    public GameObject deathMenu;
    [SerializeField]
    public GameObject deathMenuGoHomeButton;
    public RectTransform playerArrow;
    public TextMeshProUGUI finalGems, totalGems, finalCoins, timeSurvived, enemiesKilledText;
    private float gems, coins, enemies;
    public TextMeshProUGUI[] levelCheckpoint = new TextMeshProUGUI[5];
    bool statsAnim = false;
    float animationTime = 1f;
    public Image clearImage;
    public bool hasWon = false;

    [Header("Level Scaling")]
    public float damageScale = 1f;
    public float enemyHealthScale = 1f;
    public int lastEnemyIndex;

    private void Awake()
    {
        instance = this;
        if (bosses.Length > 0)
            boss = bosses[Random.Range(0, bosses.Length)];
    }
    private void Start()
    {
        p = PlayerController.instance;
        level = DataHolder.instance.level;
        subLevel = DataHolder.instance.subLevel;
        sound = SoundManager.instance;
        shipSelect = GetComponent<ShipSelect>();
        gemsText = UIManager.instance.gemsText;
        switch (subLevel)
        {
            case 1: numberOfEnemies = basicEnemies;
                break;
            case 2: numberOfEnemies = basicEnemies;
                break;
            case 3: numberOfEnemies = enhancedEnemies;
                break;
            case 4: numberOfEnemies = enhancedEnemies + 1;
                break;
            case 5: numberOfEnemies = enhancedEnemies + 1;
                break;
        }

        gemsText = UIManager.instance.gemsText;
        if (levelText)
            levelText.text = level + "-" + subLevel;

        if (TutorialManager.instance == null)
        {
            damageScale = 1 + 0.125f * (level - 1);

            enemyHealthScale = 1 + .125f * (level - 1);
        }
        else
        {
            damageScale = 0.6f;
            enemyHealthScale = 0.7f;
        }

        if ((level == 1 && subLevel == 1) ||
            (level == 1 && subLevel == 4) ||
            (level == 2 && subLevel == 3) ||
            (level == 3 && subLevel == 2) ||
            (level == 4 && subLevel == 1))
        {
            StartCoroutine(DropUpgrades(p.transform.position, 1.25f));
        }

        if (sound.musicSource.clip != levelMusic || !sound.musicSource.isPlaying)
        {
            sound.musicSource.clip = levelMusic;
            sound.musicSource.Play();
        }

    }

    private void Update()
    {
        coinText.text = DataHolder.instance.goldCoins.ToString();
        gemsText.text = DataHolder.instance.gems.ToString();

        if(statsAnim)
        {
            if(Input.anyKeyDown)
            {
                DeathMenuStats();
                statsAnim = false;
            }
        }
    }

    public IEnumerator DropUpgrades(Vector3 pos, float time)
    {
        yield return new WaitForSeconds(time);
        Vector3 location1 = pos + new Vector3(0, 1f);
        Vector3 location2 = pos + new Vector3(-1.75f, -1f);
        Vector3 location3 = pos + new Vector3(1.75f, -1f);
        Instantiate(upgradeDrop, location1, Quaternion.identity);
        Instantiate(upgradeDrop, location2, Quaternion.identity);
        Instantiate(upgradeDrop, location3, Quaternion.identity);
    }
    public void RedirectPower(int i)
    {
        if (i != oldPower)
        {
            switch (i)
            {
                case 1:
                    StartCoroutine(AttackPower());
                    break;
                case 2:
                    StartCoroutine(DefensePower());
                    break;
                case 3:
                    StartCoroutine(SpeedPower());
                    break;
            }
            CancelRedirectPower(oldPower);
            oldPower = i;
        }
    }

    public void CancelRedirectPower(int i)
    {
        switch(i)
        {
            case 0: return;
            case 1:
                StartCoroutine(CancelAttackPower());
                break;
            case 2:
                StartCoroutine(CancelDefensePower());
                break;
            case 3:
                StartCoroutine(CancelSpeedPower());
                break;
        }
    }

    public IEnumerator AttackPower()
    {
        float fill = 0;
        float currentscroll = .6f;
        //p.maskMat.SetColor("_EmisColor", Color.red);
        p.mask.color = new Color(1,0,0, .75f);
        while (fill < 1)
        {
            currentscroll -= 2.2f * Time.deltaTime;
            p.maskMat.mainTextureOffset = new Vector2(0, currentscroll);
            powerMask[0].fillAmount = fill;
            fill += 2 * Time.deltaTime;
            yield return null;
        }
        powerMask[0].fillAmount = 1;
        p.attackMultiplier = DataHolder.instance.selectedShip.attackMultiplier;

        p.material.SetColor("_FlashColor", new Color(1, 0, 0, .75f));
        float flash = 0;
        while (flash < .7f)
        {
            flash += 6 * Time.deltaTime;
            p.material.SetFloat("_FlashAmount", flash);
            yield return null;
        }
        while (flash > 0)
        {
            flash -= 6 * Time.deltaTime;
            p.material.SetFloat("_FlashAmount", flash);
            yield return null;
        }
        p.material.SetFloat("_FlashAmount", 0);
    }
    public IEnumerator DefensePower()
    {
        float fill = 0;
        float currentscroll = .6f;
        bool ok = false;
        p.mask.color = new Color(0, .5f, 1, .75f);
        while (fill < 1)
        {
            if (currentscroll <= -.4f)
                ok = true;
            if (ok == false)
                currentscroll -= 4.4f * Time.deltaTime;
            else
                currentscroll += 4.4f * Time.deltaTime;
            p.maskMat.mainTextureOffset = new Vector2(0, currentscroll);
            powerMask[1].fillAmount = fill;
            fill += 2 * Time.deltaTime;
            yield return null;
        }
        powerMask[1].fillAmount = 1;
        p.defenseMultiplier = DataHolder.instance.selectedShip.defenseMultiplier;
        p.reducePushBack = true;
        p.healthSlider.fillRect.GetComponent<Image>().color = new Color(0, .5f, 1, 1);

        p.material.SetColor("_FlashColor", new Color(0, .5f, 1, .75f));
        float flash = 0;
        while (flash < .7f)
        {
            flash += 6 * Time.deltaTime;
            p.material.SetFloat("_FlashAmount", flash);
            yield return null;
        }
        while (flash > 0)
        {
            flash -= 6 * Time.deltaTime;
            p.material.SetFloat("_FlashAmount", flash);
            yield return null;
        }
        p.material.SetFloat("_FlashAmount", 0);
    }
    public IEnumerator SpeedPower()
    {
        float fill = 0;
        float currentscroll = -.4f;
        //p.maskMat.SetColor("_EmisColor", Color.yellow);
        p.mask.color = new Color(1, 1, 0, .75f);
        while (fill < 1)
        {
            currentscroll += 2.2f * Time.deltaTime;
            p.maskMat.mainTextureOffset = new Vector2(0, currentscroll);
            powerMask[2].fillAmount = fill;
            fill += 2 * Time.deltaTime;
            yield return null;
        }
        powerMask[2].fillAmount = 1;
        p.speedMultiplier = DataHolder.instance.selectedShip.speedMultiplier;
        p.slowDuration = 0;
        p.maxMoveSpeed *= (1 + p.speedMultiplier / 100);
        p.shouldSlowOnShoot = false;

        p.material.SetColor("_FlashColor", new Color(1, 1, 0, .75f));
        float flash = 0;
        while (flash < .7f)
        {
            flash += 6 * Time.deltaTime;
            p.material.SetFloat("_FlashAmount", flash);
            yield return null;
        }
        while (flash > 0)
        {
            flash -= 6 * Time.deltaTime;
            p.material.SetFloat("_FlashAmount", flash);
            yield return null;
        }
        p.material.SetFloat("_FlashAmount", 0);
    }
    public IEnumerator CancelAttackPower()
    {
        float fill = 1;
        while (fill > 0)
        {
            powerMask[0].fillAmount = fill;
            fill -= 2 * Time.deltaTime;
            yield return null;
        }
        powerMask[0].fillAmount = 0;
        p.attackMultiplier = 0;
    }
    public IEnumerator CancelDefensePower()
    {
        float fill = 1;
        while (fill > 0)
        {
            powerMask[1].fillAmount = fill;
            fill -= 2 * Time.deltaTime;
            yield return null;
        }
        powerMask[1].fillAmount = 0;
        p.defenseMultiplier = 0;
        p.reducePushBack = false;
        p.healthSlider.fillRect.GetComponent<Image>().color = Color.green;
    }

    public IEnumerator CancelSpeedPower()
    {
        float fill = 1;
        while (fill > 0)
        {
            powerMask[2].fillAmount = fill;
            fill -= 2 * Time.deltaTime;
            yield return null;
        }
        powerMask[2].fillAmount = 0;
        p.speedMultiplier = 0;
        p.maxMoveSpeed = p.ship.maxMoveSpeed;
        p.shouldSlowOnShoot = true;
    }

    public void PauseGame()
    {
        if (Time.timeScale != 0)
        {
            Time.timeScale = 0;
            pauseMenuImage.gameObject.SetActive(true);
            pauseMenu.SetActive(true);
            p.canShoot = false;
            ShipInformations();
            StopAllSounds();
        }
        else
        {
            Time.timeScale = 1;
            pauseMenuImage.gameObject.SetActive(false);
            pauseMenu.SetActive(false);
            p.canShoot = true;
            ResumeAllSounds();
        }
    }

    public void ShipInformations()
    {
        shipImage.sprite = p.ship.shipImage ? p.ship.shipImage : p.ship.shipSprite;
        attackText.text = "+" + p.ship.attackMultiplier + "% overall damage";
        defenseText.text = "-" + p.ship.defenseMultiplier + "% damage taken";
        speedText.text = "+" + p.ship.speedMultiplier + "% movement speed";
    }

    public void GoToRespawnMenu()
    {
        StartCoroutine(GoToRespawnMenuC());
    }

    public IEnumerator GoToRespawnMenuC()
    {
        yield return new WaitForSeconds(1f);
        if (DataHolder.instance.respawnsRemaining > 0)
        {
            gemsAmountToReviveValue = 200 + 100 * (level - 1) + 20 * subLevel;
            gemsAmountToRevive.text = gemsAmountToReviveValue.ToString();
            respawnMenu.SetActive(true);
        }
        else
        {
            GoToDeathMenu();
        }
    }

    public void CloseRespawnMenu()
    {
        respawnMenu.SetActive(false);
    }

    public void ReviveWithGems()
    {
        if (DataHolder.instance.gems >= gemsAmountToReviveValue)
        {
            DataHolder.instance.gems -= gemsAmountToReviveValue;
            RevivePlayer();
            DataHolder.instance.Save();
        }
        else
        {
            OpenGemShop();
        }
    }

    public void DisplayRewardedAd()
    {
        //Monetization.instance.rewardedAction = 0;
        //Monetization.instance.DisplayRewardedAd();
    }

    public void ReviveWithAd()
    {
        //Monetization.instance.rewardedAction = 1;
        //Monetization.instance.DisplayRewardedAd();
    }

    public void RevivePlayer()
    {
        PlayerController.instance.gameObject.SetActive(true);
        PlayerController.instance.ship.gun.Initialize();
        PlayerController.instance.ship.currentHealth = PlayerController.instance.ship.maxHealth;
        PlayerController.instance.SetupHealthbar();

        DataHolder.instance.respawnsRemaining--;
        DataHolder.instance.gameHasEnded = false;
        DataHolder.instance.Save();
        
        respawnMenu.SetActive(false);
    }

    public void OpenGemShop()
    {
        gemShop.SetActive(true);
    }

    public void CloseGemShop()
    {
        StartCoroutine(CloseGemShopC());
    }

    public IEnumerator CloseGemShopC()
    {
        gemShop.GetComponent<Animator>().SetTrigger("close");
        yield return new WaitForSeconds(gemShop.GetComponent<Animator>().GetCurrentAnimatorStateInfo(0).length);
        gemShop.SetActive(false);
    }

    public void GoToDeathMenu()
    {
        foreach(TextMeshProUGUI checkpoint in levelCheckpoint)
        {
            checkpoint.text = level + checkpoint.text;
        }
        totalGems = UIManager.instance.totalGems;
        totalGems.text = DataHolder.instance.gems.ToString();
        finalCoins.text = DataHolder.instance.goldCoins.ToString();
        DataHolder.instance.gameHasEnded = true;
        DataHolder.instance.timeSinceGameStarted = Time.time - DataHolder.instance.timeSinceGameStarted;
        if(hasWon)
        {
            UIManager.instance.gameOverText.text = "You won!";
        }

        statsAnim = true;
        DataHolder.instance.Save();
        deathMenu.SetActive(true);
    }

    void DeathMenuStats()
    {
        deathMenuGoHomeButton.SetActive(true);
        StartCoroutine(ArrowAnim());
        StartCoroutine(GemsAnim());
        StartCoroutine(CoinsAnim());
        StartCoroutine(TimeAnim());        
        StartCoroutine(EnemiesAnim());
        if (hasWon)
            StartCoroutine(PlayerWon());
    }

    IEnumerator PlayerWon()
    {
        yield return new WaitForSeconds(animationTime + .5f);
        UIManager.instance.playerWon.SetActive(true);
        DataHolder.instance.gems += 150;
        DataHolder.instance.Save();
        totalGems.text = (DataHolder.instance.gems + gems).ToString();
    }

    IEnumerator TimeAnim()
    {
        float seconds = 0;
        float minutes = 0;
        int timeSurvivedSeconds = (int)(DataHolder.instance.timeSinceGameStarted % 60);
        int timeSurvivedMinutes = (int)(DataHolder.instance.timeSinceGameStarted / 60);
        while (minutes < timeSurvivedMinutes)
        {
            minutes += (animationTime * Time.deltaTime) * timeSurvivedMinutes * 2f;
            timeSurvived.text = (minutes < 10 ? "0" + minutes.ToString("0") : minutes.ToString("0")) + ":" + (seconds < 10 ? "0" + seconds.ToString("0") : seconds.ToString("0"));
            yield return null;
        }
        timeSurvived.text = (timeSurvivedMinutes < 10 ? "0" + timeSurvivedMinutes : timeSurvivedMinutes.ToString()) + ":" + (seconds < 10 ? "0" + seconds.ToString("0") : seconds.ToString("0"));
        while (seconds < timeSurvivedSeconds)
        {
            seconds += (animationTime * Time.deltaTime) * timeSurvivedSeconds * 2f;
            timeSurvived.text = (minutes < 10 ? "0" + minutes.ToString("0") : minutes.ToString("0")) + ":" + (seconds < 10 ? "0" + seconds.ToString("0") : seconds.ToString("0")); yield return null;
            yield return null;
        }
        timeSurvived.text = (timeSurvivedMinutes < 10 ? "0" + timeSurvivedMinutes : timeSurvivedMinutes.ToString()) + ":" + (timeSurvivedSeconds < 10 ? "0" + timeSurvivedSeconds : timeSurvivedSeconds.ToString());
    }

    IEnumerator CoinsAnim()
    {
        float initialCoins = DataHolder.instance.goldCoins;
        coins = DataHolder.instance.goldCoins;
        finalCoins.text = coins.ToString("0");
        while (coins > 0)
        {
            coins -= (animationTime * Time.deltaTime) * initialCoins;
            finalCoins.text = coins.ToString("0");
            yield return null;
        }
        coins = 0;
        finalCoins.text = coins.ToString("0");
    }
    IEnumerator GemsAnim()
    {
        float endGems = DataHolder.instance.goldCoins * DataHolder.instance.coinsMultiplier;
        while(gems < endGems)
        {
            gems += (animationTime * Time.deltaTime) * endGems;
            finalGems.text = "+" + gems.ToString("0");
            yield return null;
        }
        gems = endGems;
        finalGems.text = "+" + gems.ToString("0");
        totalGems.text = (DataHolder.instance.gems + gems).ToString();
    }
    IEnumerator EnemiesAnim()
    {
        int enemiesKilled = DataHolder.instance.enemiesKilled;
        while(enemies < enemiesKilled)
        {
            enemies += (animationTime * Time.deltaTime) * enemiesKilled;
            enemiesKilledText.text = enemies.ToString("0");
            yield return null;
        }
        enemies = enemiesKilled;
        enemiesKilledText.text = enemies.ToString("0");
    }
    IEnumerator ArrowAnim()
    {
        while(playerArrow.position != levelCheckpoint[subLevel -1].transform.position)
        {
            playerArrow.position = Vector3.MoveTowards(playerArrow.position, new Vector3(levelCheckpoint[subLevel - 1].transform.position.x - 5, playerArrow.position.y, 0), subLevel * 150 * Time.deltaTime);
            yield return null;
        }
        playerArrow.position = new Vector3(levelCheckpoint[subLevel - 1].transform.position.x - 5, playerArrow.position.y, 0);
    }
    public void GoToMenu()
    {
        SoundManager.instance.musicSource.Stop();
        DataHolder data = DataHolder.instance;
        data.gems += (int)(data.goldCoins * data.coinsMultiplier);
        data.goldCoins = 0;
        data.dataSaved.totalEnemiesKilled += data.enemiesKilled;
        data.enemiesKilled = 0;
        data.gameHasEnded = true;
        data.countToAd--;
        data.Save();

        StartCoroutine(LevelLoader.instance.LoadLevel("Main Menu"));
        Time.timeScale = 1;
    }

    public void ChangeQuality()
    {
        DataHolder.instance.ChangeQuality();
    }

    public void FpsSettings()
    {
        DataHolder.instance.FpsSettings();
    }

    public void OptionsMenu()
    {
        settingsMenuBG.gameObject.SetActive(true);
        settingsMenu.SetActive(true);
        qualityText.text = QualitySettings.names[QualitySettings.GetQualityLevel()];
        p.controls.Disable();
    }
    IEnumerator CloseOptionsMenu()
    {
        settingsMenu.GetComponent<Animator>().SetTrigger("close");
        yield return new WaitForSecondsRealtime(settingsMenu.GetComponent<Animator>().GetCurrentAnimatorStateInfo(0).length + .05f);
        settingsMenu.SetActive(false);
        settingsMenuBG.gameObject.SetActive(false);
        p.controls.Enable();
    }

    public void CloseOptions()
    {
        StartCoroutine(CloseOptionsMenu());
    }

    public IEnumerator ClearRoom()
    {
        clearImage.gameObject.SetActive(true);
        Color color = clearImage.color;
        color.a = 1;
        clearImage.color = color;
        clearImage.GetComponentInChildren<TextMeshProUGUI>().color = color;
        CameraShake.instance.StartShake(.15f, .1f);
        yield return new WaitForSeconds(1.5f);
        float elapsed = 1;
        while(elapsed > 0)
        {
            color.a = elapsed;
            clearImage.color = color;
            clearImage.GetComponentInChildren<TextMeshProUGUI>().color = color;
            elapsed -= Time.deltaTime;
            yield return null;
        }
        clearImage.gameObject.SetActive(false);
    }

    public void ShouldDropAbility(Vector3 position)
    {
        int chance = Random.Range(0, 100);
        if(chance < chanceToDropAbility)
        {
            Instantiate(abilityDrop, position, Quaternion.identity);
        }
    }

    public void GamepadControls()
    {
        DataHolder data = DataHolder.instance;
        if (data.controllerOn)
        {
            data.controllerOn = false;
            checkboxX.enabled = false;
            UIManager.instance.lowerRight.SetTrigger("controlleroff");
        }
        else
        {
            data.controllerOn = true;
            checkboxX.enabled = true;
            UIManager.instance.lowerRight.SetTrigger("controlleron");
        }
    }

    public void StopAllSounds()
    {
        AudioSource[] allAudioSources = FindObjectsOfType<AudioSource>();
        foreach(var source in allAudioSources)
        {
            if(source != SoundManager.instance.musicSource)
            {
                source.Pause();
            }
        }
    }

    public void ResumeAllSounds()
    {
        AudioSource[] allAudioSources = FindObjectsOfType<AudioSource>();
        foreach (var source in allAudioSources)
        {
            if (source != SoundManager.instance.musicSource)
            {
                source.UnPause();
            }
        }
    }
}
