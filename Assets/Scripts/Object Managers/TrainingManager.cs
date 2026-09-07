using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class TrainingManager : MonoBehaviour
{
    public static TrainingManager instance;
    PlayerController p;
    [Header("General")]
    public GameObject trainingButton;
    public TextMeshProUGUI title;
    public GameObject mapSelection, roomSizeSelection, enemiesSelection, spaceshipSelection, abilitySelection, modeSelection, bossSelection;
    public GameObject holder, randomizeCenterMenu;
    public GameObject backButton;
    public bool canBeCleared;
    public int waves;
    public bool startedGame;
    public Vector3 startPosition;
    public int menuNumber = 0;
    private Coroutine activeWaveCoroutine;

    [Header("Spaceship selection")]
    public string selectSpaceshipTitle = "Select your spaceship";
    public Spaceship[] ships;
    public List<Spaceship> unlockedShips = new List<Spaceship>();
    public GameObject[] spaceshipButtons;
    public Spaceship selectedShip;

    [Header("Ability selection")]
    public string abilityTitle;
    public Ability[] abilities = new Ability[30];
    public Ability[] freeAbilities;
    public List<Ability> unlockedAbilities = new List<Ability>();
    public GameObject[] abilityButtons;
    public Ability selectedAbility;

    [Header("Map selection")]
    public string mapTitle = "Select a map";
    public Sprite[] mapImages;
    public enum Map{
        crystal,
        swamp,
        moon,
        red
    };
    Map selectedMap;

    [Header("Mode Selection")]
    public string modeTitle;
    public bool classicMode;
    public Image[] modeImages;

    [Header("Boss selection")]
    public string bossTitle;
    public GameObject[] bossesButtons;
    public Enemy[][] bosses = new Enemy[4][];
    public Enemy[] crystalBosses;
    public Enemy[] swampBosses;
    public Enemy[] moonBosses;
    public Enemy[] redBosses;
    public Enemy selectedBoss;
    public Enemy activeBoss;

    [Header("Size Selection")]
    public string sizeTitle = "Select room size";
    public Image[] sizeButtonImages;
    public enum Size { 
        small,
        medium,
        large
    };
    Size selectedSize;
    public GameObject[] crystalRooms = new GameObject[3];
    public GameObject[] swampRooms = new GameObject[3];
    public GameObject[] moonRooms = new GameObject[3];
    public GameObject[] redRooms = new GameObject[3];
    public GameObject[][] rooms = new GameObject[4][];
    public GameObject activeRoom;

    [Header("Center Selection")]
    public string centerTitle = "Select room center";
    public int centerSize;
    public Centers centerPrefabs;
    public GameObject[] crystalSmall;
    public GameObject[] crystalMedium;
    public GameObject[] crystalLarge;
    public GameObject[] swampSmall;
    public GameObject[] swampMedium;
    public GameObject[] swampLarge;
    public GameObject[] moonSmall;
    public GameObject[] moonMedium;
    public GameObject[] moonLarge;
    public GameObject[] redSmall;
    public GameObject[] redMedium;
    public GameObject[] redLarge;
    public GameObject[][][] centers = new GameObject[4][][];
    public GameObject activeCenter;

    [Header("Enemy Selection")]
    public string enemyTitle;
    public GameObject[] enemyButtons;
    public Enemy[] crystalEnemies;
    public Enemy[] swampEnemies;
    public Enemy[] moonEnemies;
    public Enemy[] redEnemies;
    public Enemy[][] enemies = new Enemy[4][];
    public List<Enemy> activeEnemies = new List<Enemy>();
    public List<Enemy> selectedEnemies;
    public int difficultyIndex = 1;
    public TextMeshProUGUI difficultyText;
    public TMP_InputField wavesText;

    private void Awake()
    {
        instance = this;
    }

    private void Start()
    {
        CameraMovement.instance.enabled = false;
        CameraMovement.instance.transform.position = startPosition;
        p = PlayerController.instance;
        p.transform.position = new Vector3(50, 50);

        for(int i = 0; i < 4; i++)
        {
            rooms[i] = new GameObject[3];
            enemies[i] = new Enemy[20];
            centers[i] = new GameObject[3][];
            bosses[i] = new Enemy[5];
            for(int j = 0; j < 3; j++)
            {
                centers[i][j] = new GameObject[40];
            }
        }

        rooms[0] = crystalRooms;
        rooms[1] = swampRooms;
        rooms[2] = moonRooms;
        rooms[3] = redRooms;

        centers[0][0] = crystalSmall;
        centers[0][1] = crystalMedium;
        centers[0][2] = crystalLarge;
        centers[1][0] = swampSmall;
        centers[1][1] = swampMedium;
        centers[1][2] = swampLarge;
        centers[2][0] = moonSmall;
        centers[2][1] = moonMedium;
        centers[2][2] = moonLarge;
        centers[3][0] = redSmall;
        centers[3][1] = redMedium;
        centers[3][2] = redLarge;

        enemies[0] = crystalEnemies;
        enemies[1] = swampEnemies;
        enemies[2] = moonEnemies;
        enemies[3] = redEnemies;

        bosses[0] = crystalBosses;
        bosses[1] = swampBosses;
        bosses[2] = moonBosses;
        bosses[3] = redBosses;

        SelectSpaceships();
        for (int i = 0; i < ships.Length;i++)
        {
            abilities[i] = ships[i].signatureAbility;
            abilities[i].level = 3;
        }
        for(int i = 0; i < freeAbilities.Length; i++)
        {
            abilities[i + ships.Length] = freeAbilities[i];
            abilities[i + ships.Length].level = 3;
        }
    }
    public void ResetMenuFunction(float time)
    {
        StartCoroutine(ResetMenu(time));
    }

    public IEnumerator ResetMenu(float time)
    {
        startedGame = false;
        if (activeBoss != null)
            activeBoss.HideBossHealthbar();
        UIManager.instance.bossName.SetActive(false);
        if(activeWaveCoroutine != null)
            StopCoroutine(activeWaveCoroutine);
        foreach (Enemy enemy in FindObjectsOfType<Enemy>())
            Destroy(enemy.gameObject);
        foreach (Bullet bullet in FindObjectsOfType<Bullet>())
            Destroy(bullet.gameObject);

        yield return new WaitForSeconds(time);

        CameraMovement.instance.transform.position = startPosition;
        holder.SetActive(true);
        trainingButton.gameObject.SetActive(false);

        p.transform.position = new Vector3(50, 50);
        p.health = p.maxHealth;
        Camera.main.orthographicSize = (int)selectedSize == 0 ? 2.75f : ((int)selectedSize == 1 ? 3.5f : 4.5f);
    }

    private void FixedUpdate()
    {
        if (startedGame)
        {
            if (canBeCleared)
            {
                if (classicMode)
                {
                    for (int i = 0; i < activeEnemies.Count; i++)
                    {
                        if (activeEnemies[i] == null)
                        {
                            activeEnemies.RemoveAt(i);
                            i--;
                        }
                    }

                    if (activeEnemies.Count == 0)
                    {
                        if (waves > 0)
                        {
                            StartCoroutine(SpawnWave());
                        }
                        else
                        {
                            StartCoroutine(ResetMenu(.5f));
                        }
                    }
                }
                else
                {
                    if(activeBoss == null)
                    {
                        StartCoroutine(ResetMenu(.5f));
                    }

                }
            }
        }
        else
        {
            p.canMove = false;
            p.canShoot = false;
            CameraMovement.instance.enabled = false;
        }
    }

    public void SelectSpaceships()
    {
        title.text = selectSpaceshipTitle;
        DataHolder data = DataHolder.instance;
        foreach(GameObject button in spaceshipButtons)
        {
            button.SetActive(false);
        }
        int k = 0;
        unlockedShips.Clear();
        if (selectedShip)
            selectedShip.ability = null;
        p.ability = null;
        for(int i = 0; i < ships.Length; i++)
        {
            ships[i].ability = null;
            if(data.dataSaved.isUnlocked[i])
            {
                spaceshipButtons[k].SetActive(true);
                spaceshipButtons[k].GetComponentsInChildren<Image>()[1].sprite = ships[i].shipSprite;
                spaceshipButtons[k].GetComponentInChildren<TextMeshProUGUI>().text = ships[i].shipName;
                unlockedShips.Add(ships[i]);
                k++;
            }
        }
        backButton.SetActive(false);
    }

    public void SelectSpaceship(int index)
    {
        selectedShip = unlockedShips[index];
        selectedShip.ability = null;
        DataHolder.instance.selectedShip = selectedShip;
        ShipSelect.instance.s = selectedShip;
        ShipSelect.instance.SetSpaceship();
        spaceshipSelection.SetActive(false);
        abilitySelection.SetActive(true);
        menuNumber++;
        SelectAbilities();
    }

    public void SelectAbilities()
    {
        backButton.SetActive(true);
        title.text = abilityTitle;
        DataHolder data = DataHolder.instance;
        foreach (GameObject button in abilityButtons)
        {
            button.SetActive(false);
        }
        int k = 0;
        unlockedAbilities.Clear();
        for (int i = 0; i < ships.Length + freeAbilities.Length; i++)
        {
            if (i >= ships.Length || data.dataSaved.isUnlocked[i])
            {
                abilityButtons[k].SetActive(true);
                abilityButtons[k].GetComponentsInChildren<Image>()[1].sprite = abilities[i].abilitySprite;
                abilityButtons[k].GetComponentInChildren<TextMeshProUGUI>().text = abilities[i].abilityName;
                unlockedAbilities.Add(abilities[i]);
                k++;
            }
        }
    }

    public void SelectAbility(int index)
    {
        selectedAbility = unlockedAbilities[index];
        DataHolder.instance.selectedShip.ability = selectedAbility;
        p.ability = selectedAbility;
        p.abilityManager.Initialize(p.ability);
        abilitySelection.SetActive(false);
        mapSelection.SetActive(true);
        title.text = mapTitle;
        menuNumber++;
    }

    public void Back()
    {
        switch(menuNumber)
        {
            case 0: 
                break;
            case 1:
                abilitySelection.SetActive(false);
                spaceshipSelection.SetActive(true);
                title.text = selectSpaceshipTitle;
                SelectSpaceships();
                break;
            case 2:
                mapSelection.SetActive(false);
                abilitySelection.SetActive(true);
                title.text = abilityTitle;
                SelectAbilities();
                break;
            case 3:
                modeSelection.SetActive(false);
                mapSelection.SetActive(true);
                title.text = mapTitle;
                break;
            case 4:
                if (classicMode)
                    roomSizeSelection.SetActive(false);
                else
                    bossSelection.SetActive(false);
                modeSelection.SetActive(true);
                title.text = modeTitle;
                break;
            case 5:
                holder.SetActive(true);
                roomSizeSelection.SetActive(true);
                randomizeCenterMenu.SetActive(false);
                Destroy(activeCenter);
                title.text = sizeTitle;             
                break;
            case 6:
                holder.SetActive(false);
                randomizeCenterMenu.SetActive(true);
                enemiesSelection.SetActive(false);
                title.text = enemyTitle;
                break;
        }

        menuNumber--;
    }

    public void SelectBosses()
    {
        title.text = bossTitle;
        foreach(GameObject button in bossesButtons)
        {
            button.SetActive(false);
        }

        for(int i = 0; i < bosses[(int)selectedMap].Length;i++)
        {
            bossesButtons[i].SetActive(true);
            bossesButtons[i].GetComponentsInChildren<Image>()[2].sprite = bosses[(int)selectedMap][i].shipSprite.sprite;
            bossesButtons[i].GetComponentInChildren<TextMeshProUGUI>().text = bosses[(int)selectedMap][i].enemyName;
        }
    }

    public void SelectBoss(int index)
    {
        selectedBoss = bosses[(int)selectedMap][index];
        if (activeRoom)
            Destroy(activeRoom);
        if (activeCenter)
            Destroy(activeCenter);
        activeRoom = Instantiate(rooms[(int)selectedMap][1]);
        activeCenter = Instantiate(selectedBoss.bossCenter.gameObject);
        selectedBoss.activeRoom = activeRoom.GetComponent<Room>();
        GameManager.instance.possibleEnemies.Clear();
        GameManager.instance.damageScale = 1;
        GameManager.instance.enemyHealthScale = 1;
        foreach(Enemy enemy in enemies[(int)selectedMap])
        {
            GameManager.instance.possibleEnemies.Add(enemy);
        }
        StartGame();
    }

    public void ModeSelection(bool isClassicMode)
    {
        if (isClassicMode)
        {
            classicMode = true;
            modeSelection.SetActive(false);
            roomSizeSelection.SetActive(true);
            title.text = sizeTitle;
        }
        else
        {
            classicMode = false;
            modeSelection.SetActive(false);
            SelectBosses();
            bossSelection.SetActive(true);
        }
        menuNumber++;
    }

    public void SelectMap(int index)
    {
        selectedMap = (Map)index;
        mapSelection.SetActive(false);
        modeSelection.SetActive(true);
        title.text = modeTitle;
        foreach (Image img in sizeButtonImages)
        {
            img.sprite = mapImages[(int)selectedMap];
        }
        foreach (Image img in modeImages)
            img.sprite = mapImages[(int)selectedMap];
        menuNumber++;
    }

    public void SelectSize(int index)
    {
        selectedSize = (Size)index;
        holder.SetActive(false);
        randomizeCenterMenu.SetActive(true);
        if (activeRoom != null)
            Destroy(activeRoom);
        activeRoom = Instantiate(rooms[(int)selectedMap][(int)selectedSize]);
        menuNumber++;
        Camera.main.orthographicSize = (int)selectedSize == 0 ? 2.75f : ((int)selectedSize == 1 ? 3.5f : 4.5f);

    }

    public void RandomizeCenter()
    {
        if (activeCenter)
            Destroy(activeCenter);
        activeCenter = Instantiate(centers[(int)selectedMap][(int)selectedSize][Random.Range(0, centers[(int)selectedMap][(int)selectedSize].Length)]);
    }

    public void SelectEnemy(int index)
    {
        if (selectedEnemies.Contains(enemies[(int)selectedMap][index]))
        {
            selectedEnemies.Remove(enemies[(int)selectedMap][index]);
            enemyButtons[index].GetComponentsInChildren<Image>()[2].enabled = false;
        }
        else
        {
            selectedEnemies.Add(enemies[(int)selectedMap][index]);
            enemyButtons[index].GetComponentsInChildren<Image>()[2].enabled = true;
        }
    }

    public void SelectDificulty()
    {
        difficultyIndex++;
        if (difficultyIndex > 2)
            difficultyIndex = 0;

        switch(difficultyIndex)
        {
            case 0:
                difficultyText.text = "Easy";
                break;
            case 1:
                difficultyText.text = "Medium";
                break;
            case 2:
                difficultyText.text = "Hard";
                break;
        }
    }

    public void SelectEnemies()
    {
        bool succes = int.TryParse(wavesText.text, out waves);
        if (!succes || waves <= 0)
        {
            wavesText.GetComponent<Animator>().SetTrigger("blink");
            Debug.Log("Waves not correct");
            return;
        }
        title.text = enemyTitle;
        roomSizeSelection.SetActive(false);
        enemiesSelection.SetActive(true);
        holder.SetActive(true);
        randomizeCenterMenu.SetActive(false);
        selectedEnemies.Clear();

        for (int i = 0; i < enemyButtons.Length; i++)
        {
            enemyButtons[i].SetActive(false);
        }

        for(int i = 0; i < enemies[(int)selectedMap].Length;i++)
        {
            enemyButtons[i].SetActive(true);
            enemyButtons[i].GetComponentsInChildren<Image>()[1].sprite = enemies[(int)selectedMap][i].shipSprite.sprite;
            enemyButtons[i].GetComponentsInChildren<Image>()[2].enabled = false;
        }
        menuNumber++;

    }


    public void StartGame()
    {
        if (classicMode)
        {
            if (selectedEnemies.Count == 0)
            {
                Debug.Log("No enemies selected");
                return;
            }

            trainingButton.gameObject.SetActive(true);

            sizeX = difficultyIndex == 0 ? 3.5f : (difficultyIndex == 1 ? 5 : 6.5f);
            sizeY = difficultyIndex == 0 ? 1.5f : (difficultyIndex == 1 ? 3.5f : 4.25f);
            activeRoom.GetComponent<Room>().sizeX = sizeX;
            activeRoom.GetComponent<Room>().sizeY = sizeY;
            Grid1.instance.CreateGrid();

            holder.SetActive(false);

            Vector3 position = Vector3.zero;
            while (Physics2D.OverlapCircle(position, .225f, LayerMask.GetMask("Obstacles") | LayerMask.GetMask("Default")))
            {
                position = new Vector3(Random.Range(-sizeX, sizeX), Random.Range(-sizeY - 1, sizeY)); ;
            }

            p.transform.position = position;
            p.canMove = true;
            p.canShoot = true;
            p.isAlive = true;
            p.health = p.maxHealth;
            p.healthSlider.value = 1;
            p.healthText.text = "100";
            Camera.main.orthographicSize = 2.15f;
            CameraMovement.instance.enabled = true;
            activeWaveCoroutine = StartCoroutine(SpawnWave());
            startedGame = true;
        }
        else
        {
            activeBoss = Instantiate(selectedBoss, startPosition, Quaternion.identity);

            trainingButton.gameObject.SetActive(true);

            sizeX = difficultyIndex == 0 ? 3.5f : (difficultyIndex == 1 ? 5 : 6.5f);
            sizeY = difficultyIndex == 0 ? 1.5f : (difficultyIndex == 1 ? 3.5f : 4.25f);
            activeRoom.GetComponent<Room>().sizeX = sizeX;
            activeRoom.GetComponent<Room>().sizeY = sizeY;
            Grid1.instance.CreateGrid();

            holder.SetActive(false);

            Vector3 position = new Vector3(startPosition.x, -3.5f);
            while (Physics2D.OverlapCircle(position, .225f, LayerMask.GetMask("Obstacles") | LayerMask.GetMask("Default")))
            {
                position = new Vector3(Random.Range(-sizeX, sizeX), Random.Range(-sizeY - 1, sizeY));
            }

            p.transform.position = position;
            p.canMove = true;
            p.canShoot = true;
            p.isAlive = true;
            p.health = p.maxHealth;
            p.healthSlider.value = 1;
            p.healthText.text = "100";
            Camera.main.orthographicSize = 2.15f;
            CameraMovement.instance.enabled = true;
            startedGame = true;
            canBeCleared = true;
            activeBoss.StartCoroutine(activeBoss.BossHealthbar(activeBoss.hasArmor));
        }
    }

    float n = 0;
    float sizeX, sizeY;
    public IEnumerator SpawnWave()
    {
        canBeCleared = false;
        waves--;
        SoundManager.instance.soundSource.PlayOneShot(SoundManager.instance.generalSounds[0]);
        float sizeMultiplier = (0.75f + ((int)selectedSize * .25f)) * (0.5f * (difficultyIndex + 1));
        int enemyNumberPerWave = Mathf.CeilToInt(20 * sizeMultiplier);

        n = enemyNumberPerWave;
        activeEnemies.Clear();
        for (int i = 0; n > 0; i++)
        {
            StartCoroutine(SpawnEnemies());
            yield return new WaitForSeconds(.15f);
        }
        yield return new WaitForSeconds(1.5f);
        canBeCleared = true;
        
    }

    IEnumerator SpawnEnemies()
    {
        Enemy selectedEnemy = selectedEnemies[Random.Range(0, selectedEnemies.Count)];
        for (int i = 0; i < 20; i++)
        {
            if (selectedEnemy.unitValue <= n + 1)
                break;
            else
                selectedEnemy = selectedEnemies[Random.Range(0, selectedEnemies.Count)];
        }

        if (selectedEnemy.unitValue <= n + 1)
        {
            n -= selectedEnemy.unitValue;
            Vector3 spawnedEnemyPosition = new Vector3(Random.Range(-sizeX, sizeX), Random.Range(-sizeY - 1, sizeY));

            while (Physics2D.OverlapCircle(spawnedEnemyPosition, .225f, LayerMask.GetMask("Obstacles") | LayerMask.GetMask("Default")))
            {
                spawnedEnemyPosition = new Vector3(Random.Range(-sizeX, sizeX), Random.Range(-sizeY - 1, sizeY)); ;
            }
            GameObject spawn = Instantiate(selectedEnemy.spawnEffect, spawnedEnemyPosition, Quaternion.identity);
            spawn.transform.localScale = new Vector3(selectedEnemy.transform.localScale.x * 2, selectedEnemy.transform.localScale.y, 1);
            yield return new WaitForSeconds(1.5f);

            activeEnemies.Add(Instantiate(selectedEnemy, spawnedEnemyPosition, Quaternion.Euler(0, 0, Random.Range(0, 360))));
        }
        else n = 0;
    }

    public void DeleteCenter()
    {
        if (activeCenter)
            Destroy(activeCenter);
    }
}
