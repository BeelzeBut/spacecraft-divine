using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DashEnemy : Enemy
{
    public float chargeTime = 1, dashDuration;
    public int damage;
    float initialMoveSpeed;
    private bool isDashing;
    private SpriteRenderer spriteRenderer;
    [SerializeField]
    List<GameObject> lightnings = new List<GameObject>();
    [SerializeField]
    GameObject thruster;
    public TrailRenderer trail;
    public bool isOrange = false;
    public bool stopOnPlayerHit = true;
    public Mine minePrefab;

    void Start()
    {
        player = PlayerController.instance;
        MaterialSetup();
        initialMoveSpeed = moveSpeed;
        curMoveSpeed = moveSpeed;
        initialScale = transform.localScale;
        transform.localScale = Vector3.zero;
        trail.enabled = false;
        spriteRenderer = GetComponent<SpriteRenderer>();
        trail.material.SetTexture("_MainTex", spriteRenderer.sprite.texture);
    }


    void Update()
    {
        if (player.isAlive)
        {
            if (player.isAlive)
            {
                EnemyAI();

                if (shouldShoot)
                {
                    StartCoroutine(Dash(player.transform.position));
                }
            }
        }
    }
    private void FixedUpdate()
    {
        if (player.isAlive)
        {
            EnemyAIFixedUpdate();
        }
    }

    IEnumerator Dash(Vector3 playerPos)
    {
        rb.velocity = Vector2.zero;
        shootTimer = 10f;
        shouldShoot = false;
        canMove = false;
        canLook = false;
        curMoveSpeed = 0;
        Vector3 moveDirection = (playerPos - transform.position).normalized;
       // transform.rotation = Quaternion.Euler(0, 0, Mathf.Atan2(moveDirection.y, moveDirection.x) * Mathf.Rad2Deg + 90);

        canBePushedBack = false;
        curMoveSpeed = 0;
        foreach (GameObject lightning in lightnings)
            lightning.SetActive(true);
        trail.enabled = true;
        float elapsed = 0;
        while(elapsed < chargeTime)
        {
            Vector3 lookDir = (playerPos - transform.position).normalized;
            float angle = Mathf.Atan2(lookDir.y, lookDir.x) * Mathf.Rad2Deg + 90;
            transform.rotation = Quaternion.Lerp(transform.rotation, (Quaternion.AngleAxis(angle, Vector3.forward)), turningspeed * Time.fixedDeltaTime);
            elapsed += Time.fixedDeltaTime;
            yield return new WaitForFixedUpdate();
        }

        PlaySound(attackSound);
        GetComponent<Collider2D>().enabled = false;
        GetComponent<Collider2D>().enabled = true;
        isDashing = true;
        thruster.SetActive(true);
        curMoveSpeed = moveSpeed * 6f;
        if (isOrange)
            curMoveSpeed *= .75f;
        moveSpeed = curMoveSpeed;
        canTakeDamage = false;
        elapsed = 0;
        float timePassed = 0;
        while (elapsed < dashDuration)
        {
            //transform.position += moveDirection * curMoveSpeed * Time.deltaTime;
            rb.MovePosition(rb.position + (Vector2)moveDirection * curMoveSpeed * Time.fixedDeltaTime);
            elapsed += Time.fixedDeltaTime;
            if(isOrange)
            {
                timePassed -= Time.fixedDeltaTime;
                if (timePassed <= 0)
                {
                    Instantiate(minePrefab, transform.position, transform.rotation);
                    timePassed = dashDuration / 3f;
                }
            }
            yield return new WaitForFixedUpdate();
        }
        StartCoroutine(StopAttack());
    }
    public override IEnumerator StopAttack()
    {
        StopCoroutine("Dash");
        shootTimer = Random.Range(shootCooldown * 1 / 2f, shootCooldown * 3 / 2f);
        canLook = true;
        canMove = true;
        canTakeDamage = true;
        canBePushedBack = true;
        isDashing = false;
        moveSpeed = initialMoveSpeed;
        curMoveSpeed = moveSpeed;
        foreach(GameObject lightning in lightnings)
            lightning.SetActive(false);
        thruster.SetActive(false);
        hasRequestedPathToPlayer = false;

        yield return new WaitForSeconds(.2f);

        if(trail)
            trail.enabled = false;
        ChooseBehaviour(1);
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.gameObject.CompareTag("Player"))
        {
            if (isDashing)
            {
                if(player.invincibility <= 0 && player.canTakeDamage)
                {
                    Vector2 forceDir = (other.transform.position - transform.position).normalized;
                    player.rb.AddForce(forceDir * damage, ForceMode2D.Impulse);
                    player.slowDuration = 1f;
                    player.moveSpeed = player.maxMoveSpeed * 5f / 10f;
                }
                player.TakeDamage(damage);
                if(stopOnPlayerHit)
                    StartCoroutine(StopAttack());
            }

        }
    }

    private void OnCollisionEnter2D(Collision2D other)
    {
        if (isDashing)
        {
            StartCoroutine(StopAttack());
            rb.velocity = Vector2.zero;
        }
    }

}

/*
 * if (rb.velocity != Vector2.zero)
            {
                curMoveSpeed = 0;
                Vector2 rbvel = rb.velocity;
                rb.velocity -= rbvel * 10f * Time.deltaTime;
                if (Vector2.Distance(rb.velocity, Vector2.zero) <= .5f)
                {
                    rb.velocity = Vector2.zero;
                    curMoveSpeed = moveSpeed;
                }
            }

            if (transform.localScale != Vector3.one)
                transform.localScale = Vector3.Lerp(transform.localScale, Vector3.one, 6 * Time.deltaTime);

            if (dashTimer > 0)
            {
                dashTimer -= Time.deltaTime;
            }
            if (dashTimer <= 0)
            {
                RaycastHit2D[] hits = new RaycastHit2D[1];
                Physics2D.RaycastNonAlloc(transform.position, (player.transform.position - transform.position), hits, Mathf.Infinity, layermask);

                if (Vector3.Distance(player.transform.position, transform.position) <= dashRange && !isDashing && hits[0].transform.CompareTag("Player"))
                {
                    StartCoroutine(Dash(player.transform.position));
                }
            }

            if (!isDashing)
            {
                RaycastHit2D[] hits = new RaycastHit2D[1];
                Physics2D.RaycastNonAlloc(transform.position, (player.transform.position - transform.position), hits, Mathf.Infinity, layermask);
                if (hits[0].transform.CompareTag("Player"))
                {
                    moveDirection = (player.transform.position - transform.position).normalized;

                    angle = Mathf.Atan2(moveDirection.y, moveDirection.x) * Mathf.Rad2Deg + 90;
                    transform.rotation = Quaternion.Euler(0, 0, angle);

                    if (Vector3.Distance(transform.position, player.transform.position) > .5f)
                    {
                        transform.position += new Vector3(moveDirection.x, moveDirection.y, 0) * Time.deltaTime * curMoveSpeed;
                    }
                }
                else
                {
                    if (path != null)
                    {
                        if (currentWaypoint >= path.vectorPath.Count)
                        {
                            reachedEndOfPath = true;
                        }
                        else
                        {
                            reachedEndOfPath = false;

                            float distance = Vector2.Distance(transform.position, path.vectorPath[currentWaypoint]);

                            if (distance < nextWayPointDistance)
                            {
                                currentWaypoint++;
                            }
                            //Vector3 lookDirection = player.transform.position - transform.position;
                            if (currentWaypoint < path.vectorPath.Count - 1)
                                moveDirection = ((Vector3)path.vectorPath[currentWaypoint + 1] - transform.position).normalized;
                            else
                                moveDirection = ((Vector3)path.vectorPath[currentWaypoint] - transform.position).normalized;

                            angle = Mathf.Atan2(moveDirection.y, moveDirection.x) * Mathf.Rad2Deg + 90;
                            transform.rotation = Quaternion.Euler(0, 0, angle);

                            transform.position += new Vector3(moveDirection.x, moveDirection.y, 0) * Time.deltaTime * curMoveSpeed;

                        }
                    }
                }
            }
            else
            {
                angle = Mathf.Atan2(moveDirection.y, moveDirection.x) * Mathf.Rad2Deg + 90;
                transform.rotation = Quaternion.Euler(0, 0, angle);

                transform.position += new Vector3(moveDirection.x, moveDirection.y, 0) * Time.deltaTime * curMoveSpeed;

            }
*/