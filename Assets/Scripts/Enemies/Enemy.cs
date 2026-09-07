using System.Collections;
using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class Enemy : MonoBehaviour
{
    [Header("Sounds")]
    public AudioSource source;
    public AudioClip attackSound;

    public PlayerController player;
    public string enemyName;
    public Rigidbody2D rb;
    public bool isBoss;
    public Transform bossCenter;
    public Collectible blueprint;
    public SpriteRenderer shipSprite;
    public bool canBePushedBack = true;
    public bool isDead;
    public Room activeRoom;

    public float health = 10f;
    public Vector3 initialScale = Vector3.one;

    [SerializeField]
    public GameObject spawnEffect;
    [SerializeField]
    public GameObject deathEffect;

    public float curMoveSpeed,
             moveSpeed;
    public bool canLook = true;
    public bool canMove = true;

    public Vector2 velocity;

    //public HealthBar healthBar;

    public float unitValue;

    public LayerMask roomLayermask;
    public LayerMask obstacleLayermask;
    public bool canTakeDamage = true;


    [Header("Pathfinding")]
    Vector2[] path;
    int targetIndex;
    Vector2 targetPosition;
    public bool hasRequestedPathToPlayer, requestedEndOfPath;
    public bool reachedEndOfPath;

    [Header("Movement")]
    public float turningspeed = 9f;
    public float moveCooldown;
    public float moveRange = 4f;
    public float shootCooldown;
    public float shootTimer = 2f;
    public float attackRange;
    public bool shouldShoot = false;
    public Vector3 movePosition;
    [HideInInspector]
    public float moveTimer = .5f;
    public Transform muzzleFlash;
    public float actionIndex = 1;
    private bool startedEnemyShoot;
    private Vector3 lastPos;
    private float counter;
    public bool isMoving;
    public Vector2 moveDirection;
    private Color oldColor;
    public Transform iceExplosion;
    public Coroutine damageFlash;
    public bool randomizeMovePosition = true;
    public bool hasArmor = false;

    public Slider healthBar, armorSlider;
    public GameObject bossName;
    public void EnemyAI()
    {     
        if (rb.velocity.sqrMagnitude > 0f)
        {
            canMove = false;
            curMoveSpeed = 0;
            if (rb.velocity.sqrMagnitude <= .025f)
            {
                curMoveSpeed = moveSpeed;
                rb.velocity = Vector2.zero;
                if(isBoss || canBePushedBack)
                    canMove = true;
            }
        }

        if (transform.localScale != initialScale)
        {
            transform.localScale = Vector3.Lerp(transform.localScale, initialScale, 6 * Time.deltaTime);
            if (transform.localScale == initialScale)
                transform.localScale = initialScale;
        }

        shootTimer -= Time.deltaTime;

        if (canLook && player.shouldBeAttacked)
        {
            Vector3 lookDir = (player.transform.position - transform.position);
            float angle = Mathf.Atan2(lookDir.y, lookDir.x) * Mathf.Rad2Deg + 90;
            //transform.rotation = Quaternion.Euler(0, 0, angle);
            transform.rotation = Quaternion.Lerp(transform.rotation, (Quaternion.AngleAxis(angle, Vector3.forward)), turningspeed * Time.deltaTime);
        }

        if (!canMove)
            isMoving = false;
    }

    public void EnemyAIFixedUpdate()
    {
        if (canMove)
        {
            if (shootTimer <= 0 && !hasRequestedPathToPlayer)
            {
                hasRequestedPathToPlayer = true;
                GameManager.instance.StartCoroutine(ResetPathRequestToPlayer());
                ChooseBehaviour(2);
            }
        
            switch (actionIndex)
            {
                case 0:
                    Idle();
                    break;
                case 1:
                    MoveEnemy(movePosition);
                    if ((transform.position - lastPos).sqrMagnitude <= .1f)
                    {
                        counter += Time.fixedDeltaTime;
                        if (counter >= 1 )
                        {
                            ChooseBehaviour(1);
                            curMoveSpeed = moveSpeed;
                            Debug.Log("enemy is stuck case 1");
                            counter = 0;
                        }
                    }
                    else
                    {
                        lastPos = transform.position;
                        counter = 0;
                    }
                    break;
                case 2:
                    MoveInRange();
                    if ((transform.position - lastPos).sqrMagnitude <= .1f)
                    {
                        counter += Time.fixedDeltaTime;
                        if (counter >= 1)
                        {
                            targetPosition = player.transform.position;
                            curMoveSpeed = moveSpeed;
                            shootTimer = .75f;
                            ChooseBehaviour(1);
                            Debug.Log("enemy is stuck case 2");
                            counter = 0;
                        }
                    }
                    else
                    {
                        lastPos = transform.position;
                        counter = 0;
                    }
                    break;
            }
        }
    }
    IEnumerator ResetPathRequestToPlayer()
    {
        yield return new WaitForSeconds(2f);
        hasRequestedPathToPlayer = false;
    }

    public void MoveEnemy(Vector3 movePosition)
    {
        FollowPath();

        if (reachedEndOfPath)
        {
            ChooseBehaviour(0);
        }
    }

    public void Idle()
    {
        moveTimer -= Time.deltaTime;
        if (moveTimer <= 0)
        {
            ChooseBehaviour(1);
        }
    }

    public void MoveInRange()
    {
        if (reachedEndOfPath && !requestedEndOfPath)
        {
            requestedEndOfPath = true;
            targetPosition = (Vector2)player.transform.position;
            RequestPath("MoveInRange");
        }
        if (player.shouldBeAttacked)
        {
            if ((transform.position - player.transform.position).sqrMagnitude > attackRange * attackRange)
            {
                FollowPath();
            }
            else
            {
                if (Physics2D.CircleCast(transform.position, .075f, player.transform.position - transform.position, (player.transform.position - transform.position).magnitude, obstacleLayermask))
                {
                    FollowPath();
                }
                else
                {
                    if (!startedEnemyShoot)
                    {
                        startedEnemyShoot = true;
                        StartCoroutine(EnemyShoot());
                    }
                }
            }
        }
        else
        {
            shootTimer = Random.Range(1f, shootCooldown);
            hasRequestedPathToPlayer = false;
            ChooseBehaviour(Random.Range(0, 2));
        }
    }

    IEnumerator EnemyShoot()
    {
        float elapsed = Random.Range(.25f, .45f);
        WaitForFixedUpdate wait = new WaitForFixedUpdate();
        shootTimer = 1.5f;
        while (elapsed > 0)
        {
            FollowPath();
            elapsed -= Time.fixedDeltaTime;
            yield return wait;
        }
        hasRequestedPathToPlayer = false;
        shouldShoot = true;

        ChooseBehaviour(0);
    }

    public void ChooseBehaviour(int index)
    {
        switch (index)
        {
            case 0:
                actionIndex = 0;
                moveTimer = Random.Range(moveCooldown * .33f, moveCooldown * 1.5f);
                isMoving = false;
                break;
            case 1:
                actionIndex = 1;
                if (randomizeMovePosition)
                {
                    movePosition = transform.position + new Vector3(Random.Range(-moveRange, moveRange), Random.Range(-moveRange, moveRange), 0);
                    while (Physics2D.OverlapCircle(movePosition, .25f, obstacleLayermask + roomLayermask))
                    {
                        movePosition = transform.position + new Vector3(Random.Range(-moveRange, moveRange), Random.Range(-moveRange, moveRange), 0);
                    }
                    RaycastHit2D hit = Physics2D.Linecast(transform.position, movePosition, roomLayermask);
                    if (hit.collider != null)
                    {
                        movePosition = hit.point;
                    }
                }
                targetPosition = movePosition;
                RequestPath("Choose Behaviour case 1");
                break;
            case 2:
                targetPosition = player.transform.position;
                startedEnemyShoot = false;
                RequestPath("Choose Behaviour case 2");
                actionIndex = 2;
                break;
        }
    }

    public void MaterialSetup()
    {
        rb.drag = 5f;
        shipSprite.material = new Material(shipSprite.material);
        obstacleLayermask = LayerMask.GetMask("Obstacles");
        StartCoroutine(MoveOnSpawn());
        if (!player)
            player = PlayerController.instance;
        if (!GetComponentInChildren<Shadow>())
        {
            Shadow shadow = Instantiate(player.shadow.GetComponent<Shadow>(), transform.position, transform.rotation, shipSprite.transform);
            shadow.getSpriteFromParent = true;
            shadow.transform.localScale = Vector3.one;
            if (isBoss)
            {
                shadow.GetComponent<SpriteRenderer>().flipY = true;
                shadow.shouldBeLower = true;
            }
        }
        health *= GameManager.instance.enemyHealthScale;

        source = GetComponent<AudioSource>();
        source.loop = false;
        source.clip = attackSound;

        if (isBoss)
        {
            foreach (Slider go in GameObject.Find("UI Canvas").GetComponentsInChildren<Slider>(true))
                if (go.name == "BossHealthbar")
                    healthBar = go;
                else if (go.name == "BossArmor")
                    armorSlider = go;
        }
    }

    public IEnumerator BossHealthbar(bool withArmor)
    {
        bossName = UIManager.instance.bossName;
        bossName.GetComponentInChildren<TextMeshProUGUI>().text = enemyName;
        bossName.SetActive(false);
        bossName.SetActive(true);
        yield return null;
        bossName.GetComponentInChildren<TextMeshProUGUI>().ForceMeshUpdate();
        bossName.GetComponentInChildren<Image>().rectTransform.sizeDelta = new Vector2(bossName.GetComponentInChildren<TextMeshProUGUI>().rectTransform.sizeDelta.x + 100, bossName.GetComponentInChildren<Image>().rectTransform.sizeDelta.y);

        yield return new WaitForSeconds(2.5f);
        
        healthBar.gameObject.SetActive(true);

        if (withArmor)
        {                
            armorSlider.fillRect.GetComponent<Image>().color = new Color(255f / 163f, 0, 255f / 240f);
            armorSlider.gameObject.SetActive(true);
        }
    }

    public void HideBossHealthbar()
    {
        armorSlider.gameObject.SetActive(false);
        healthBar.gameObject.SetActive(false);
    }

    IEnumerator MoveOnSpawn()
    {
        if (!isBoss)
        {
            yield return new WaitForSeconds(.5f);
            canMove = true;
            ChooseBehaviour(1);
        }
    }

    public void PlaySound(AudioClip sound)
    {
        source.PlayOneShot(sound);
    }

    public virtual void TakeDamage(float damage)
    {
        if (!isDead)
        {
            health -= damage;
            if (health <= 0)
            {
                isDead = true;
                GetComponent<Collider2D>().enabled = false;
                player.ship.UpgradeOnEnemyKill();
                DataHolder.instance.enemiesKilled++;
                Instantiate(deathEffect, transform.position, Quaternion.Euler(0, 0, Random.Range(0, 360)));
                if (isBoss)
                {
                    if (DataHolder.instance.dataSaved.priceToUnlock[GameManager.instance.chest.bossBlueprint.dropIndex] == 0 && DataHolder.instance.level == 4)
                    {
                        GameManager.instance.shouldDropBossBlueprint = true;
                        Debug.Log("Should drop Vickers");
                    }
                    else
                    {
                        Debug.Log(DataHolder.instance.dataSaved.priceToUnlock[GameManager.instance.chest.bossBlueprint.dropIndex]);
                    }
                }
                else
                {
                    for (int i = 0; i < DataHolder.instance.drops.Count; i++)
                    {
                        if ((DataHolder.instance.enemiesKilled == DataHolder.instance.killMilestones[i] ||
                            (DataHolder.instance.enemiesKilled > DataHolder.instance.killMilestones[i] && (DataHolder.instance.enemiesKilled - DataHolder.instance.killMilestones[i]) % 100 == 0))
                            && (DataHolder.instance.drops[i].GetComponent<Blueprint>() &&
                            !DataHolder.instance.dataSaved.hasBeenUnlocked[DataHolder.instance.drops[i].GetComponent<Blueprint>().dropIndex]))
                        {
                            Instantiate(DataHolder.instance.drops[i], transform.position, Quaternion.identity);
                        }
                    }
                }
                //GameManager.instance.ShouldDropAbility(transform.position);
                Destroy(gameObject);
                return;

            }

            if (damageFlash != null)
            {
                StopCoroutine(damageFlash);
                shipSprite.material.SetFloat("_FlashAmount", 0f);
                damageFlash = StartCoroutine(DamageFlash(true));
            }
            else
                damageFlash = StartCoroutine(DamageFlash(false));

        }
    }
    public IEnumerator DamageFlash(bool shouldWait)
    {
        if (shouldWait)
            yield return new WaitForSeconds(.02f);
        shipSprite.material.SetFloat("_FlashAmount", .75f);
        if(!isBoss)
            yield return new WaitForSeconds(.1f);
        else
            yield return new WaitForSeconds(.06f);
        shipSprite.material.SetFloat("_FlashAmount", 0f);
    }

    public void RequestPath(string functionCalledFrom)
    {
        //Debug.Log("Requesting path from " + functionCalledFrom);
        PathRequestManager.RequestPath(rb.position, targetPosition, OnPathFound);
    }

    public void OnPathFound(Vector2[] newPath, bool pathSuccessful)
    {
        if (pathSuccessful)
        {
            reachedEndOfPath = false;
            path = new Vector2[0];
            path = newPath;
            targetIndex = 1;
            requestedEndOfPath = false;
        }
        else
        {
            if (path == null)
            {
                requestedEndOfPath = false;
                reachedEndOfPath = false;
            }
        }
    }

    void FollowPath()
    {
        if (!reachedEndOfPath)
        {
            if (path == null || !canMove)
            {
                reachedEndOfPath = true;
                return;
            }
            if (path.Length == 0)
            {
                return;
            }

            if (path.Length == 1)
                targetIndex = 0;
            isMoving = true;
            Vector2 currentWaypoint = path[targetIndex];

            if ((rb.position - currentWaypoint).sqrMagnitude <= .035f)
            {
                targetIndex++;
                if (targetIndex >= path.Length)
                {
                    //path = null;
                    reachedEndOfPath = true;
                    return;
                }
                currentWaypoint = path[targetIndex];
            }

            Vector2 dir = (currentWaypoint - rb.position).normalized;
            moveDirection = dir;
            rb.MovePosition(rb.position + dir * curMoveSpeed * Time.fixedDeltaTime);
        }
    }

    public void AimAheadOfPlayer()
    {
        Vector3 lookDir = ((player.transform.position + (Vector3)player.moveDirection * (player.transform.position - transform.position).magnitude / 3f * player.curMoveSpeed / 3f) - transform.position).normalized;
        float angle = Mathf.Atan2(lookDir.y, lookDir.x) * Mathf.Rad2Deg + 90;
        transform.rotation = Quaternion.Lerp(transform.rotation, (Quaternion.AngleAxis(angle, Vector3.forward)), turningspeed * Time.deltaTime);
    }

    private void OnDrawGizmos()
    {
        if(path != null)
        for (int i = 0; i < path.Length - 1; i++)
        {
            Gizmos.DrawLine(path[i], path[i + 1]);
        }
    }

    public void Freeze(float time, Color color)
    {
        ChooseBehaviour(0);
        //oldColor = shipSprite.color;
        shipSprite.color = color;
        StopAllCoroutines();
        StartCoroutine(StopAttack());
        if(time > 0)
            StartCoroutine(UnfreezeC(isBoss ? time / 2f : time));
    }

    public IEnumerator UnfreezeC(float time)
    {
        float elapsed = 0;
        while(elapsed < time)
        {
            canMove = false;
            canLook = false;
            elapsed += Time.deltaTime;
            yield return null;
        }
        shipSprite.color = Color.white;
        if(iceExplosion)
            Instantiate(iceExplosion, transform.position, transform.rotation);
        canMove = true;
        canLook = true;
        ChooseBehaviour(1);
    }
    
    public virtual IEnumerator StopAttack()
    {
        shootTimer = Random.Range(shootCooldown * 2 / 3f, shootCooldown * 4 / 3f);
        if(!isBoss)
            canBePushedBack = true;
        curMoveSpeed = moveSpeed;
        hasRequestedPathToPlayer = false;
        yield return null;
    }


}
