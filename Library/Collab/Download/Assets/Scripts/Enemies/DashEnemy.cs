using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Pathfinding;

public class DashEnemy : Enemy
{
    public float chargeTime = 1, dashDuration;
    public int damage;

    private bool isDashing;

    [SerializeField]
    List<GameObject> lightnings = new List<GameObject>();
    [SerializeField]
    GameObject thruster;
    public TrailRenderer trail;

    void Start()
    {
        MaterialSetup();
        player = PlayerController.instance;
        curMoveSpeed = moveSpeed;
        initialScale = transform.localScale;
        transform.localScale = Vector3.zero;
        trail.enabled = false;

        //seeker = GetComponent<Seeker>();
        //InvokeRepeating("UpdatePath", 0f, .15f);
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
        transform.rotation = Quaternion.Euler(0, 0, Mathf.Atan2(moveDirection.y, moveDirection.x) * Mathf.Rad2Deg + 90);

        canBePushedBack = false;
        curMoveSpeed = 0;
        foreach (GameObject lightning in lightnings)
            lightning.SetActive(true);
        trail.enabled = true;

        yield return new WaitForSeconds(chargeTime);

        isDashing = true;
        thruster.SetActive(true);
        curMoveSpeed = moveSpeed * 6f;
        canTakeDamage = false;
        float elapsed = 0;

        while (elapsed < dashDuration)
        {
            transform.position += moveDirection * curMoveSpeed * Time.deltaTime;
         
            elapsed += Time.deltaTime;
            yield return null;
        }
        StartCoroutine(StopDash());
    }
    IEnumerator StopDash()
    {
        StopCoroutine("Dash");
        shootTimer = Random.Range(shootCooldown * 1 / 2f, shootCooldown * 3 / 2f);
        canLook = true;
        canMove = true;
        canTakeDamage = true;
        canBePushedBack = true;
        isDashing = false;
        curMoveSpeed = moveSpeed;
        foreach(GameObject lightning in lightnings)
            lightning.SetActive(false);
        thruster.SetActive(false);

        yield return new WaitForSeconds(.2f);

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
                StartCoroutine(StopDash());
            }

        }
    }

    private void OnCollisionEnter2D(Collision2D other)
    {
        if (isDashing)
        {
            StartCoroutine(StopDash());
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