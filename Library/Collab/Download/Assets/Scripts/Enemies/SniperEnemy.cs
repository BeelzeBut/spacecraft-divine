using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SniperEnemy : Enemy
{
    Vector3 movePoint, lookDirection, curVelocity;
    public Transform firePoint;
    public Bullet bulletPrefab;

    public float  chargeTime = 1.25f;

    public LineRenderer beam;

    void Start()
    {
        MaterialSetup();

        movePoint = transform.position + new Vector3((float)Random.Range(-4, 4), (float)Random.Range(-3, 3), 0f);

        transform.localScale = Vector3.zero;

        player = PlayerController.instance;

        curMoveSpeed = moveSpeed;

        beam.useWorldSpace = true;
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
                    StartCoroutine(Shoot());
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

    IEnumerator Shoot()
    {
        shouldShoot = false;
        canLook = false;
        shootTimer = 100f;
        ChooseBehaviour(0);

        float maxMoveSpeed = moveSpeed;
        moveSpeed = 0;
        curMoveSpeed = 0;

        beam.startColor = Color.red;
        beam.endColor = Color.red;
        beam.enabled = true;

        float elapsed = 0;
        while(elapsed < chargeTime)
        {
            lookDirection = (player.transform.position - transform.position).normalized;
            float angle = Mathf.Atan2(lookDirection.y, lookDirection.x) * Mathf.Rad2Deg + 90;
            transform.rotation = Quaternion.Euler(0, 0, angle);

            beam.SetPosition(0, firePoint.position);
            RaycastHit2D hit;
            if(hit = Physics2D.Linecast(firePoint.position, player.transform.position, obstacleLayermask))
            {
                beam.SetPosition(1, hit.point);
            }else
                beam.SetPosition(1, player.transform.position);

           /* Vector3 playerBackPos = Vector3.zero;
            playerBackPos.x = 2f * player.transform.position.x - firePoint.position.x;
            playerBackPos.x = (playerBackPos.x + player.transform.position.x) / 2;
            playerBackPos.y = 2f * player.transform.position.y - firePoint.position.y;
            playerBackPos.y = (playerBackPos.y + player.transform.position.y) / 2;

            beam.SetPosition(2, playerBackPos);
            */
            elapsed += Time.deltaTime;
            yield return null;
        }

        //isCharging = false;
        //shouldLock = true;
        elapsed = 0;
        while (elapsed < chargeTime / 6)
        {
            //beam.startWidth = Mathf.Lerp(beam.startWidth, 0f, 40 * Time.deltaTime);
            beam.startColor = Color.Lerp(Color.red, Color.white, elapsed / chargeTime * 6f);
            beam.endColor = beam.startColor;
            elapsed += Time.deltaTime;
            yield return null;
        }

        beam.enabled = false;
        Instantiate(muzzleFlash, firePoint.transform.position, firePoint.transform.rotation);
        Bullet bullet = Instantiate(bulletPrefab, firePoint.position, firePoint.transform.rotation);
        bullet.whoShotIt = this.transform;
        canLook = true;

        rb.AddForce(-lookDirection * 4, ForceMode2D.Impulse);

        yield return new WaitForSeconds(.4f);


        //beam.startWidth = .015f;

        moveSpeed = maxMoveSpeed;
        curMoveSpeed = moveSpeed;
        shootTimer = Random.Range(shootCooldown * 1 / 2f, shootCooldown * 3 / 2f);
        ChooseBehaviour(1);

    }

    
}

/*
 *  if (rb.velocity != Vector2.zero)
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

            if (!shouldLock)
            {
                lookDirection = player.transform.position - transform.position;
                lookDirection.Normalize();
            }

            if (isCharging)
            {
                beam.SetPosition(0, firePoint.position);
                beam.SetPosition(1, player.transform.position);

                Vector3 playerBackPos = Vector3.zero;
                playerBackPos.x = 2f * player.transform.position.x - firePoint.position.x;
                playerBackPos.x = (playerBackPos.x + player.transform.position.x) / 2;
                playerBackPos.y = 2f * player.transform.position.y - firePoint.position.y;
                playerBackPos.y = (playerBackPos.y + player.transform.position.y) / 2;

                beam.SetPosition(2, playerBackPos);
            }

            angle = Mathf.Atan2(lookDirection.y, lookDirection.x) * Mathf.Rad2Deg + 90;
            transform.rotation = Quaternion.Euler(0, 0, angle);

            if (beamTimer > 0)
            {
                beamTimer -= Time.deltaTime;
                if (beamTimer <= 0)
                    StartCoroutine(Shoot());
            }

            if (moveTimer > 0)
            {
                moveTimer -= Time.deltaTime;
                if (moveTimer <= 0)
                {
                    movePoint = transform.position + new Vector3((float)Random.Range(-4, 4), (float)Random.Range(-3, 3), 0f);
                    moveTimer = moveCooldown;
                    hasMoved = false;
                }

            }
            if (!isCharging && !shouldLock && !hasMoved)
            {
                transform.position = Vector3.SmoothDamp(transform.position, movePoint, ref curVelocity, 1);
                if (transform.position == movePoint)
                {
                    hasMoved = true;
                    moveTimer = moveCooldown;
                }


            }*/
