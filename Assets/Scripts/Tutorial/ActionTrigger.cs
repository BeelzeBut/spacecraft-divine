using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class ActionTrigger : MonoBehaviour
{
    public Image indicationImage;
    public Image largeSquareImage;
    public Image raycastBlocker;
    public Room room;
    public Room nextRoom;
    TutorialManager tm;
    public string text;
    public List<Enemy> enemies = new List<Enemy>();
    public Transform locationToSpawn;
    public Transform blockObject;
    bool skipWhile = false;
    private enum FunctionOption
    {
        functionA,
        functionB,
        functionC,
        functionD
    };
    [SerializeField]
    private FunctionOption selectedFunction;
    private Dictionary<FunctionOption, string> functionLookup;
    DataHolder data;
    private void Awake()
    {
        functionLookup = new Dictionary<FunctionOption, string>()
        {
            { FunctionOption.functionA, "FunctionA"},
            { FunctionOption.functionB, "FunctionB"},
            { FunctionOption.functionC, "FunctionC"},
            { FunctionOption.functionD, "FunctionD"}
        };
        data = DataHolder.instance;
    }

    private void Start()
    {
        tm = TutorialManager.instance;
    }
    public void ActivateSelectedFunction()
    {
        StartCoroutine(functionLookup[selectedFunction]);//.Invoke();
    }
    private void OnEnable()
    {
        data.OnTouch += Touched;

    }
    private void OnDisable()
    {
        data.OnTouch -= Touched;
    }
    public void Touched()
    {
        skipWhile = true;
    }
    private IEnumerator FunctionA()
    {
        GetComponent<Collider2D>().enabled = false;

        tm.indicationText.text = text;
        tm.indicationTextBg.gameObject.SetActive(true);
        room.waves = 0;
        room.roomValue = 15;
        PlayerController p = PlayerController.instance;
        indicationImage.transform.position = p.fireButton.gameObject.activeSelf ? p.fireButton.transform.position : new Vector3(10000, 100000);
        indicationImage.enabled = true;
        p.movementJoystick.OnPointerUp(null);
        p.movementJoystick.gameObject.SetActive(false);
        p.moveDirection = Vector2.zero;

        //p.movementJoystick.gameObject.SetActive(false);

        WaitForSecondsRealtime wait = new WaitForSecondsRealtime(0);
        foreach (Enemy enemy in enemies)
        {
            enemy.transform.position -= Vector3.up * 3;
            enemy.GetComponentsInChildren<Collider2D>()[1].enabled = true;
        }
        while (true)
        {
            if (Time.timeScale > 0.1f)
                Mathf.Clamp(Time.timeScale -= Time.fixedUnscaledDeltaTime, 0, 1);
            else
                Time.timeScale = 0;
            if (p.isShooting)
            {
                Time.timeScale = 1;
                p.movementJoystick.gameObject.SetActive(true);
                StartCoroutine(CloseIndicationText());
                foreach (Enemy enemy in enemies)
                {
                    enemy.enabled = true;
                    enemy.ChooseBehaviour(1);
                }
                indicationImage.enabled = false;
                p.canMove = true;
                break;
            }
            yield return wait;
        }
    }
    private IEnumerator FunctionB()
    {
        GetComponent<Collider2D>().enabled = false;
        room.transform.GetChild(0).gameObject.SetActive(false); 

        foreach (GameObject door in room.doors)
        {
            door.SetActive(true);
        }
        Vector3 direction = Vector2.up;
        GameObject spawn = Instantiate(enemies[0].spawnEffect, locationToSpawn.position + direction * .35f, Quaternion.identity);
        spawn.transform.localScale = new Vector3(enemies[0].transform.localScale.x * 2, enemies[0].transform.localScale.y, 1);
        direction = Vector3.right;

        spawn = Instantiate(enemies[1].spawnEffect, locationToSpawn.position + direction * .35f, Quaternion.identity);
        spawn.transform.localScale = new Vector3(enemies[1].transform.localScale.x * 2, enemies[1].transform.localScale.y, 1);
        direction = -Vector3.up;
        spawn = Instantiate(enemies[0].spawnEffect, locationToSpawn.position + direction * .35f, Quaternion.identity);
        spawn.transform.localScale = new Vector3(enemies[0].transform.localScale.x * 2, enemies[0].transform.localScale.y, 1);
        direction = -Vector3.right;
        spawn = Instantiate(enemies[2].spawnEffect, locationToSpawn.position + direction * .35f, Quaternion.identity);
        spawn.transform.localScale = new Vector3(enemies[2].transform.localScale.x * 2, enemies[2].transform.localScale.y, 1);
        direction = Vector2.up;

        yield return new WaitForSeconds(1.5f);
        List<Enemy> localEnemies = new List<Enemy>();
        direction = Vector2.up;
        localEnemies.Add(Instantiate(enemies[0], locationToSpawn.position + direction * .35f, Quaternion.Euler(0, 0, Random.Range(0, 360))));
        direction = Vector2.right;
        localEnemies.Add(Instantiate(enemies[1], locationToSpawn.position + direction * .35f, Quaternion.Euler(0, 0, Random.Range(0, 360))));
        direction = -Vector2.up;
        localEnemies.Add(Instantiate(enemies[0], locationToSpawn.position + direction * .35f, Quaternion.Euler(0, 0, Random.Range(0, 360))));
        direction = -Vector2.right;
        localEnemies.Add(Instantiate(enemies[2], locationToSpawn.position + direction * .35f, Quaternion.Euler(0, 0, Random.Range(0, 360))));

        yield return new WaitForSeconds(.3f);

        tm.indicationText.text = text;
        tm.indicationTextBg.gameObject.SetActive(true);

        PlayerController p = PlayerController.instance;
        p.fireButton.enabled = false;
        p.secondaryFireButton.gameObject.SetActive(true);
        indicationImage.transform.position = p.secondaryFireButton.transform.position;
        indicationImage.enabled = true;
        p.movementJoystick.OnPointerUp(null);
        p.movementJoystick.gameObject.SetActive(false);
        p.moveDirection = Vector2.zero;
        WaitForSecondsRealtime wait = new WaitForSecondsRealtime(0);
        while (true)
        {
            if (Time.timeScale > 0.1f)
                Mathf.Clamp(Time.timeScale -= Time.unscaledDeltaTime * 3, 0, 1);
            else
                Time.timeScale = 0;
            if (p.isShooting)
            {
                Time.timeScale = 1;
                p.movementJoystick.gameObject.SetActive(true);
                StartCoroutine(CloseIndicationText());
                indicationImage.enabled = false;
                p.fireButton.enabled = true;
                break;
            }
            yield return wait;
        }
        wait = new WaitForSecondsRealtime(.5f);
        while (true)
        {
            for(int i = 0;i < localEnemies.Count; i++)
            {
                if (localEnemies[i] == null)
                    localEnemies.RemoveAt(i);
            }
            if (localEnemies.Count <= 0)
            {
                nextRoom.transform.GetChild(0).GetComponent<Collider2D>().enabled = true;
                room.enabled = true;
                room.transform.GetChild(0).transform.localScale = Vector3.one;
                room.transform.GetChild(0).gameObject.SetActive(true);
                room.roomValue += 20;
                break;
            }
            yield return wait;
        }
        yield return new WaitForSeconds(3.5f);
        bool hasWaited = false;
        while (true)
        {
            if (room.isCleared)
            {
                if (!hasWaited)
                {
                    yield return new WaitForSeconds(1f);
                    hasWaited = true;
                    p.GetComponentsInChildren<Collider2D>()[1].enabled = false;
                }
                CameraMovement.instance.enabled = false;
                Transform camera = CameraMovement.instance.transform;
                if (camera.position != room.transform.position)
                {
                    camera.position = Vector3.MoveTowards(camera.position, room.transform.position, 6 * Time.deltaTime);
                    tm.indicationText.text = "After clearing a room, a chest containing gold or other drops appears somewhere inside that room";
                    tm.indicationTextBg.gameObject.SetActive(true);
                }
                else
                {
                    yield return new WaitForSeconds(1.25f);
                    p.GetComponentsInChildren<Collider2D>()[1].enabled = true;
                    CameraMovement.instance.enabled = true;
                    //StartCoroutine(CloseIndicationText());
                    break;
                }
            }
            yield return null;
        }
        RoomChest activeChest = FindObjectOfType<RoomChest>();
        while (true)
        {
            if (activeChest.GetComponent<Collider2D>().enabled == false)
            {
                yield return new WaitForSeconds(.66f);

                tm.indicationText.text = "You have recieved an ability! Go near it and pick it up using the \"interact\" button >>(X)<<";
                indicationImage.transform.position = p.interactButton.transform.position;
                indicationImage.enabled = true;
                while(true)
                {
                    if(Input.anyKeyDown)
                    {
                        StartCoroutine(CloseIndicationText());
                        indicationImage.enabled = false;
                        skipWhile = false;
                        break;
                    }
                    yield return null;
                }
                break;
            }
            yield return null;
        }
    }

    private IEnumerator FunctionC()
    {
        GetComponent<Collider2D>().enabled = false;

        raycastBlocker.enabled = true;
        yield return new WaitForSeconds(1.75f);
        PlayerController p = PlayerController.instance;
        largeSquareImage.enabled = true;
        tm.indicationText.text = text;
        tm.indicationTextBg.gameObject.SetActive(true);

        while (true)
        {
            if (Input.anyKeyDown)//(p.moveDirection != Vector2.zero)
            {
                largeSquareImage.enabled = false;
                skipWhile = false;

                break;
            }
            yield return null;
        }
        yield return new WaitForSeconds(.025f);

        indicationImage.transform.position = GameObject.Find("Attack").transform.position;
        indicationImage.enabled = true;
        tm.indicationText.text = "Diverting power to <color=red>WEAPONS</color> will increase the spaceship's damage";
        while (true)
        {
            if (Input.anyKeyDown)//(p.moveDirection != Vector2.zero)
            {
                skipWhile = false;
                break;
            }
            yield return null;
        }
        yield return new WaitForSeconds(.025f);
        indicationImage.transform.position = GameObject.Find("Speed").transform.position;
        tm.indicationText.text = "Diverting power to <color=yellow>ENGINE</color> will increase the spaceship's speed";
        while (true)
        {
            if (Input.anyKeyDown)//(p.moveDirection != Vector2.zero)
            {
                skipWhile = false;
                break;
            }
            yield return null;
        }
        yield return new WaitForSeconds(.025f);

        indicationImage.transform.position = GameObject.Find("Defense").transform.position;
        tm.indicationText.text = "Diverting power to <color=blue>SHIELDS</color> will decrease damage taken and increase shields regeneration speed";
        while (true)
        {
            if (Input.anyKeyDown)//(p.moveDirection != Vector2.zero)
            {
                skipWhile = false;
                break;
            }
            yield return null;
        }

        raycastBlocker.enabled = false;
        indicationImage.enabled = false;
        StartCoroutine(CloseIndicationText());
    }

    private IEnumerator FunctionD()
    {
        room.transform.GetChild(0).gameObject.SetActive(false);
        room.minimapRooms[0].GetComponent<SpriteRenderer>().color = new Color(1, .5f, 0, 1);
        raycastBlocker.enabled = true;
        foreach (GameObject door in room.doors)
            door.SetActive(true);
        GetComponent<Collider2D>().enabled = false;
        Transform camera = CameraMovement.instance.transform;
        CameraMovement.instance.enabled = false;
        tm.indicationText.text = text;
        tm.indicationTextBg.gameObject.SetActive(true);
        PlayerController p = PlayerController.instance;
        p.canMove = false;
        while (true)
        {
            if (camera.position != enemies[0].transform.position)
            {
                camera.position = Vector3.MoveTowards(camera.position, enemies[0].transform.position, 6 * Time.deltaTime);
            }
            else
            {
                yield return new WaitForSeconds(1.25f);
                CameraMovement.instance.enabled = true;
                yield return new WaitForSeconds(.5f);
                p.canMove = true;
                raycastBlocker.enabled = false;
                blockObject.gameObject.SetActive(false);
                foreach (Enemy enemy in enemies)
                {
                    enemy.GetComponentInChildren<Indicator>(true).gameObject.SetActive(true);
                    enemy.enabled = true;
                }
                yield return new WaitForSeconds(1.25f);
                StartCoroutine(CloseIndicationText());
                break;
            }
            yield return null;
        }


        WaitForSeconds wait = new WaitForSeconds(.5f);
        while (true)
        {
            for (int i = 0; i < enemies.Count; i++)
                if (enemies[i] == null)
                {
                    enemies.RemoveAt(i);
                    i--;
                }
            if (enemies.Count == 0)
            {
                foreach (Enemy enemy in FindObjectsOfType<Enemy>())
                    enemy.TakeDamage(1000);
                Instantiate(room.nextLevelTeleporter, room.transform.position, Quaternion.identity);
                foreach (GameObject door in room.doors)
                    door.SetActive(false);
                CameraMovement.instance.enabled = false;
                p.canMove = false;
                blockObject.gameObject.SetActive(true);
                raycastBlocker.enabled = true;
                tm.indicationText.text = "Go to the teleporter to advance to the next level!";
                tm.indicationTextBg.gameObject.SetActive(true);
                while (true)
                {
                    if(CameraMovement.instance.transform.position != room.transform.position)
                    {
                        CameraMovement.instance.transform.position = Vector3.MoveTowards(CameraMovement.instance.transform.position, room.transform.position, 6 * Time.deltaTime);
                    }
                    else
                    {
                        yield return new WaitForSeconds(1.75f);
                        
                        while (true)
                        {
                            if (Input.anyKeyDown)
                            {
                                skipWhile = false;
                                break;
                            }
                            yield return null;
                        }
                        CameraMovement.instance.enabled = true;
                        p.canMove = true;
                        raycastBlocker.enabled = false;
                        blockObject.gameObject.SetActive(false);
                        StartCoroutine(CloseIndicationText());
                        break;
                    }
                    yield return null;
                }
                break;
            }

            yield return wait;
        }
    }
    IEnumerator CloseIndicationText()
    {
        tm.indicationTextBg.GetComponent<Animator>().SetTrigger("close");
        yield return new WaitForSeconds(tm.indicationTextBg.GetComponent<Animator>().GetCurrentAnimatorStateInfo(0).length + .05f);
        tm.indicationTextBg.gameObject.SetActive(false);
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if(other.CompareTag("Player"))
        {
            ActivateSelectedFunction();
        }
    }
}
