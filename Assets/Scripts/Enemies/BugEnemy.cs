using System.Collections;
using UnityEngine;

public class BugEnemy : Enemy
{
    public bool isBlue, isRed, isGreen;
    public Transform firePoint;
    public Bullet bulletPrefab;
    public float fireRate;
    public float spread;

    void Start()
    {
        MaterialSetup();
        shootTimer = Random.Range(1, shootCooldown);
        player = PlayerController.instance;
        curMoveSpeed = moveSpeed;
    }

    void Update()
    {
        if (player.isAlive)
        {
            EnemyAI();

            if(shouldShoot)
            {
                StartCoroutine(Shoot());
            }
        }
    }

    private void FixedUpdate()
    {
        if(player.isAlive)
        {
            EnemyAIFixedUpdate();
        }
    }

    public IEnumerator Shoot()
    {

        shouldShoot = false;
        shootTimer =  Random.Range(shootCooldown * 1 / 2f, shootCooldown * 3 / 2f);
        if (isBlue)
        {
            PlaySound(attackSound);
            Instantiate(muzzleFlash, firePoint.transform.position, firePoint.transform.rotation);
            Bullet bullet = Instantiate(bulletPrefab, firePoint.transform.position, firePoint.transform.rotation);
            bullet.whoShotIt = this.transform;
        }
        if(isGreen)
        {
            Vector3 deviation = Vector3.forward * -30f;
            Instantiate(muzzleFlash, firePoint.transform.position, firePoint.rotation);
            int direction = Random.Range(0, 2);
            if (direction == 0)
                direction = -1;
            for (int i = 0; i < 3; i++)
            {
                PlaySound(attackSound);
                CurveBullet bullet = Instantiate(bulletPrefab, firePoint.transform.position, Quaternion.Euler(firePoint.rotation.eulerAngles + deviation)).GetComponent<CurveBullet>();
                bullet.whoShotIt = this.transform;
                deviation += Vector3.forward * 30f;
                bullet.randomizeDirection = false;
                bullet.direction = direction;
            }
        }
        if(isRed)
        {
            for (int i = 0; i < Random.Range(5,9); i++)
            {
                PlaySound(attackSound);
                float bulletSpread = Random.Range(-1f, 1f) * spread * 7.5f;
                Instantiate(muzzleFlash, firePoint.position, Quaternion.Euler(firePoint.rotation.eulerAngles));
                Bullet bullet = Instantiate(bulletPrefab, firePoint.position, Quaternion.Euler(firePoint.rotation.eulerAngles + Vector3.forward * bulletSpread));
                bullet.whoShotIt = this.transform;
                yield return new WaitForSeconds(fireRate);
            }
        }
        shootTimer = Random.Range(shootCooldown * 1 / 2f, shootCooldown * 3 / 2f);
        ChooseBehaviour(Random.Range(0,2));
    }

}




/*
            if (rb.velocity != Vector2.zero)
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

            curMoveSpeed = moveSpeed;
            shotCounter -= Time.deltaTime;

            if ((transform.position - player.transform.position).sqrMagnitude > attackRange * attackRange)
            {
                //moveDirection = player.transform.position - transform.position;
                //Pathfinding
                if (path != null)
                {
                    if (currentWaypoint >= path.vectorPath.Count)
                    {
                        reachedEndOfPath = true;
                    }
                    else
                    {
                        reachedEndOfPath = false;

                        moveDirection = ((Vector3)path.vectorPath[currentWaypoint] - transform.position).normalized;

                        float distance = Vector2.Distance(transform.position, path.vectorPath[currentWaypoint]);

                        if (distance < nextWayPointDistance)
                        {
                            currentWaypoint++;
                        }
                    }
                }
            }
            else
            {
                moveDirection = Vector3.zero;

                RaycastHit2D[] hits = new RaycastHit2D[1];
                Physics2D.RaycastNonAlloc(transform.position, (player.transform.position - transform.position), hits, Mathf.Infinity, layermask);
                if (shotCounter <= 0 && hits[0].transform.CompareTag("Player"))
                {
                    shotCounter = fireRate;
                    Instantiate(bulletPrefab, firePoint.transform.position, firePoint.transform.rotation);
                }
                else
                {
                    //shotCounter = fireRate / 5;
                }
            }

            moveDirection.Normalize();

            angle = Mathf.Atan2(player.transform.position.y - transform.position.y, player.transform.position.x - transform.position.x) * Mathf.Rad2Deg + 90;
            transform.rotation = Quaternion.Euler(0, 0, angle);

            transform.position += new Vector3(moveDirection.x, moveDirection.y, 0) * Time.deltaTime * curMoveSpeed;

    */
