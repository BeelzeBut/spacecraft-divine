using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

public class Room : MonoBehaviour
{
    public float sizeX = 5, sizeY = 3;
    public bool shouldCloseDoors = true, hasOpened = false;
    private bool isActiveRoom;
    public bool isLastRoom = false;
    public bool hasBeenDiscovered;
    Enemy boss;
    private bool normalClear = true;
    public bool shouldSpawnEnemies = true;
    public bool isCleared;
    public bool canBeCleared = false;
    public bool shouldUpdate = true;

    public GameObject[] doors;

    public LayerMask spawnLayermask;
    [SerializeField]
    private List<Enemy> activeEnemies = new List<Enemy>();
    public List<GameObject> minimapRooms = new List<GameObject>();
    float n;
    [HideInInspector]
    public float roomValue;
    public float sizeMultiplier;
    public RoomChest chest;
    public bool shouldSpawnChest = true;

    GameManager gm;
    public int waves;
    [SerializeField]
    public GameObject nextLevelTeleporter;
    public Transform chestAnim;
    private GameObject minimap;
    void Start()
    {
        GetComponentInChildren<TilemapCollider2D>().enabled = false;
        GetComponentInChildren<TilemapCollider2D>().enabled = true;
        GetComponentInChildren<TilemapCollider2D>().usedByComposite = true;
        GetComponentInChildren<CompositeCollider2D>().geometryType = CompositeCollider2D.GeometryType.Polygons;

        gm = GameManager.instance;
        if(chest == null)
            chest = gm.chest;
        if(waves == 0)
            waves = Random.Range(1, 3);
        minimap = FindObjectOfType<UnityEngine.UI.RawImage>().transform.parent.gameObject;
        if (isLastRoom)
        {
            if (gm.subLevel == 5)
            {
                boss = Instantiate(gm.boss, transform.position + new Vector3(.08f, -.3f), transform.rotation);
                boss.gameObject.SetActive(false);
                Instantiate(boss.bossCenter, transform.position, transform.rotation);
                FindObjectOfType<Grid1>().CreateGrid();
                //boss.GetComponent<Enemy>().enabled = false;
                //boss.transform.Find("Indicator").gameObject.SetActive(false);
                boss.activeRoom = this;
                normalClear = false;
            }
            else
            {
                shouldCloseDoors = true;
            }
            minimapRooms[0].GetComponent<SpriteRenderer>().color = new Color(1, .5f, 0);

        }
        else if (!shouldCloseDoors)   
            if(minimapRooms.Count > 0)
                minimapRooms[0].GetComponent<SpriteRenderer>().color = Color.cyan;

    }

    // Update is called once per frame
    void FixedUpdate()
    {
        if (shouldUpdate)
        {
            if (activeEnemies.Count > 0 && !hasOpened && !shouldCloseDoors)
            {
                for (int i = 0; i < activeEnemies.Count; i++)
                {
                    if (activeEnemies[i] == null)
                    {
                        activeEnemies.RemoveAt(i);
                        i--;
                    }
                }
            }
            if (activeEnemies.Count == 0 && normalClear && canBeCleared && !isCleared)
            {
                if (waves > 0)
                {
                    gm.StartCoroutine(SpawnWave());
                }
                else
                {
                    foreach (GameObject door in doors)
                        door.SetActive(false);

                    minimapRooms[0].GetComponent<SpriteRenderer>().color = Color.green;

                    StartCoroutine(gm.ClearRoom());
                    StartCoroutine(SpawnChest(false));

                    if (isLastRoom)
                    {
                        Vector2 position = transform.position;
                        while (Physics2D.OverlapCircle(position, .15f, LayerMask.GetMask("Obstacles")))
                        {
                            position = (Vector2)transform.position + new Vector2(Random.Range(-sizeX, sizeX), Random.Range(-sizeY - 1, sizeY));
                        }
                        Instantiate(nextLevelTeleporter, position, Quaternion.identity);
                        isLastRoom = false;
                    }
                    minimap.SetActive(true);
                    isCleared = true;
                }
            }
        }
        

        if (boss == null && isLastRoom && !shouldCloseDoors)
        {
            /*if (gm.subLevel != 5 || gm.level == 4)
            {
                SpawnTeleporter();
            }
            else
            {
                isLastRoom = false;
                StartCoroutine(gm.DropUpgrades(transform.position + new Vector3(.115f, 0), 0.375f));
            }*/

            SpawnTeleporter();
        }
    }

    public void SpawnTeleporter()
    {
        foreach (GameObject door in doors)
            door.SetActive(false);
        Vector2 position = transform.position + new Vector3(0.1149998f, -0.4189997f);
        while (Physics2D.OverlapCircle(position, .15f, LayerMask.GetMask("Obstacles")))
        {
            position = (Vector2)transform.position + new Vector2(Random.Range(-sizeX, sizeX), Random.Range(-sizeY - 1, sizeY));
        }
        Instantiate(nextLevelTeleporter, position, Quaternion.identity);
        isLastRoom = false;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.gameObject.CompareTag("Player"))
        {
            if (gm == null)
            {
                gm = GameManager.instance;
            }

            CameraMovement.instance.activeRoom = transform;
            CameraMovement.instance.roomSize = sizeX < 4 ? 1 : (sizeX > 6 ? 3 : 2);
            foreach (GameObject room in minimapRooms)
            { 
                room.SetActive(true);
                Color mapColor = room.GetComponent<SpriteRenderer>().color;
                mapColor.a = 1f;
                room.GetComponent<SpriteRenderer>().color = mapColor;
            }
            RevealAdjacentRooms();

            if (shouldCloseDoors)
            {
                shouldCloseDoors = false;
                if (!isLastRoom)
                {
                    foreach (GameObject door in doors)
                        door.SetActive(true);
                    if(shouldSpawnEnemies)
                        StartCoroutine(SpawnWave());
                    minimap.SetActive(false);
                }
                else
                {
                    if (gm.subLevel == 5)
                    {
                        foreach (GameObject door in doors)
                            door.SetActive(true);
                        StartCoroutine(EnableBoss());
                        minimap.SetActive(false); 
                    }
                }
            }
        }
    }

    void RevealAdjacentRooms()
    {
        int k = 1;
        Collider2D hit;
        if (hit = Physics2D.OverlapCircle(transform.position + Vector3.right * k * LevelGenerator.instance.xOffset, .2f, LayerMask.GetMask("Ignore Raycast")))
        {
            GameObject roomMap = hit.gameObject.GetComponentInParent<Room>().minimapRooms[0];
            if (!roomMap.activeSelf)
            {
                roomMap.SetActive(true);
                Color mapColor = roomMap.GetComponent<SpriteRenderer>().color;
                mapColor.a = .5f;
                roomMap.GetComponent<SpriteRenderer>().color = mapColor;
            }
        }
        if (hit = Physics2D.OverlapCircle(transform.position + Vector3.up * k * LevelGenerator.instance.yOffset, .2f, LayerMask.GetMask("Ignore Raycast")))
        {

            GameObject roomMap = hit.gameObject.GetComponentInParent<Room>().minimapRooms[0];
            if (!roomMap.activeSelf)
            {
                roomMap.SetActive(true);
                Color mapColor = roomMap.GetComponent<SpriteRenderer>().color;
                mapColor.a = .5f;
                roomMap.GetComponent<SpriteRenderer>().color = mapColor;
            }
        }
        k = -1;
        if (hit = Physics2D.OverlapCircle(transform.position + Vector3.right * k * LevelGenerator.instance.xOffset, .2f, LayerMask.GetMask("Ignore Raycast")))
        {
            GameObject roomMap = hit.gameObject.GetComponentInParent<Room>().minimapRooms[0];
            if (!roomMap.activeSelf)
            {
                roomMap.SetActive(true);
                Color mapColor = roomMap.GetComponent<SpriteRenderer>().color;
                mapColor.a = .5f;
                roomMap.GetComponent<SpriteRenderer>().color = mapColor;
            }
        }
        if (hit = Physics2D.OverlapCircle(transform.position + Vector3.up * k * LevelGenerator.instance.yOffset, .2f, LayerMask.GetMask("Ignore Raycast")))
        {
           
            GameObject roomMap = hit.gameObject.GetComponentInParent<Room>().minimapRooms[0];
            if (!roomMap.activeSelf)
            {
                roomMap.SetActive(true);
                Color mapColor = roomMap.GetComponent<SpriteRenderer>().color;
                mapColor.a = .5f;
                roomMap.GetComponent<SpriteRenderer>().color = mapColor;
            }
        }

    }
    public void SpawnChestFunction(bool spawnDown)
    {
        StartCoroutine(SpawnChest(spawnDown));
    }
    IEnumerator SpawnChest(bool spawnDown)
    {
        if (shouldSpawnChest)
        {
            Vector3 chestPos = transform.position + new Vector3(0.1149998f, -0.4189997f);
            if (spawnDown)
                chestPos -= Vector3.up;
            while (Physics2D.OverlapCircle(chestPos, .2f, spawnLayermask))
            {
                chestPos = transform.position + new Vector3(Random.Range(-sizeX, sizeX), Random.Range(-sizeY - 1, sizeY), 0);
            }
            Instantiate(chestAnim, chestPos, transform.rotation);
            yield return new WaitForSeconds(.2f);

            RoomChest chest = Instantiate(this.chest, chestPos, Quaternion.identity);
            chest.value = roomValue;
        }
    }
    IEnumerator EnableBoss()
    {
        yield return new WaitForSeconds(.15f);

        PlayerController p = PlayerController.instance;
        UIManager.instance.raycastBlocker.enabled = true;
        p.canMove = false;
        p.canShoot = false;

        Transform camera = CameraMovement.instance.transform;
        CameraMovement.instance.enabled = false;
        WaitForFixedUpdate wait = new WaitForFixedUpdate();
        while (camera.position != boss.transform.position)
        {
            camera.position = Vector3.MoveTowards(camera.position, boss.transform.position, 8 * Time.fixedDeltaTime);
            yield return wait;
        }

        yield return new WaitForSeconds(.15f);

        Instantiate(boss.spawnEffect, boss.transform.position, Quaternion.identity);

        yield return new WaitForSeconds(.9f);

        boss.initialScale = boss.transform.localScale;
        boss.transform.localScale = Vector3.zero;
        boss.gameObject.SetActive(true);

        boss.canMove = false;
        boss.canLook = false;
        boss.shootTimer = 10f;

        yield return new WaitForSeconds(.3f);

        StartCoroutine(boss.BossHealthbar(boss.hasArmor));

        yield return new WaitForSeconds(2.5f);

        CameraMovement.instance.enabled = true;

        p.canMove = true;
        p.canShoot = true;
        UIManager.instance.raycastBlocker.enabled = false;

        yield return new WaitForSeconds(.25f);
        boss.canMove = true;
        boss.canLook = true;
        boss.ChooseBehaviour(0);

    }

    public IEnumerator SpawnWave()
    {
        canBeCleared = false;
        waves--;
        yield return new WaitForSeconds(.5f);
        SoundManager.instance.soundSource.PlayOneShot(SoundManager.instance.generalSounds[0]);

        int enemyNumberPerWave = Mathf.CeilToInt((16 + (gm.subLevel - 1) * 1.5f ) * sizeMultiplier); // 16-22
        if(TutorialManager.instance != null)
        {
            enemyNumberPerWave = (int)(0.6f * enemyNumberPerWave);
        }

        n = (float)enemyNumberPerWave;
        roomValue += n;
        //gm.enemyTypes.Clear();
        activeEnemies.Clear();
        for(int i = 0; n > 0; i++)
        {
            StartCoroutine(SpawnEnemies());
            yield return new WaitForSeconds(.15f);
        }
        yield return new WaitForSeconds(1.5f);
        canBeCleared = true;
    }

    IEnumerator SpawnEnemies()
    {
        /*bool enemyInEnemyTypes = false;
        Enemy selectedEnemy = gm.possibleEnemies[Random.Range(0, gm.possibleEnemies.Count)];
        foreach (Enemy enemy in gm.enemyTypes)
            if (enemy == selectedEnemy)
            {
                enemyInEnemyTypes = true;
            }
        if(!enemyInEnemyTypes)
        {
            if (gm.enemyTypes.Count >= gm.maxEnemyTypes)
            { 
                selectedEnemy = gm.enemyTypes[Random.Range(0, gm.possibleEnemies.Count)];
            }
            else
            {
                gm.enemyTypes.Add(selectedEnemy);
            }
        }*/

        Enemy selectedEnemy = gm.possibleEnemies[Random.Range(0, gm.numberOfEnemies)];
        for (int i = 0; i < 20; i++)
        {
            if (selectedEnemy.unitValue <= n + 1)
                break;
            else
                selectedEnemy = gm.possibleEnemies[Random.Range(0, gm.numberOfEnemies)];
        }

        if (selectedEnemy.unitValue <= n + 1)
        {
            n -= selectedEnemy.unitValue;
            Vector3 spawnedEnemyPosition = transform.position + new Vector3(Random.Range(-sizeX, sizeX), Random.Range(-sizeY - 1, sizeY));
            while (Physics2D.OverlapCircle(spawnedEnemyPosition, .225f, spawnLayermask))
            {
                spawnedEnemyPosition = transform.position + new Vector3(Random.Range(-sizeX, sizeX), Random.Range(-sizeY - 1, sizeY)); ;
            }
            GameObject spawn = Instantiate(selectedEnemy.spawnEffect, spawnedEnemyPosition, Quaternion.identity);
            spawn.transform.localScale = new Vector3(selectedEnemy.transform.localScale.x * 2, selectedEnemy.transform.localScale.y, 1);

            yield return new WaitForSeconds(1f);
            
            SoundManager.instance.soundSource.PlayOneShot(SoundManager.instance.enemyTeleport);

            yield return new WaitForSeconds(0.5f);

            activeEnemies.Add(Instantiate(selectedEnemy, spawnedEnemyPosition, Quaternion.Euler(0, 0, Random.Range(0, 360))));
        }
        else n = 0;
    }
 

    private void OnTriggerExit2D(Collider2D other)
    {
        CameraMovement.instance.activeRoom = null;
    }

    //private void OnTriggerStay2D(Collider2D other)
    //{
    //    CameraMovement.instance.activeRoom = transform;
    //}
}
