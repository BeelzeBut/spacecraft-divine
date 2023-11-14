using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DashAndShootEnemy : Enemy
{
    public Transform[] firePoint;
    public Bullet bulletPrefab;
    public float dashSpeed;
    public float timeBetweenDashes;
    public float minDashDistance = 1f, maxDashDistance = 1.75f;
    Vector3 velSpeed;
    public bool isEnhanced;

    [Header("Enhanced")]
    public CurveBullet enhancedBullet;
    public int numberOfEnhancedBullets = 8;
    public Transform enhancedFirePoint;
    public AudioClip secondarySound;
    void Start()
    {
        MaterialSetup();
        shootTimer = Random.Range(1, shootCooldown);
        player = PlayerController.instance;
        curMoveSpeed = moveSpeed;
        initialScale = transform.localScale;

    }

    void Update()
    {
        if (player.isAlive)
        {
            EnemyAI();

            if (shouldShoot)
            {
                StartCoroutine(DashAndShoot());
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

    IEnumerator DashAndShoot()
    {
        shouldShoot = false;
        shootTimer = 10f;
        canMove = false;
        canBePushedBack = false;
        int dashes = 0;
        if (!isEnhanced)
        {
            dashes = 2;
        }
        else
        {
            dashes = 3;
        }
        for (int i = 0; i < dashes; i++)
        {
            rb.velocity = Vector2.zero;
            Vector3 dashPosition = transform.position + new Vector3(Random.Range(-1f, 1f), Random.Range(-1f, 1f)).normalized * Random.Range(minDashDistance, maxDashDistance);
            while (Physics2D.Linecast(transform.position, dashPosition, roomLayermask | obstacleLayermask))
            {
                dashPosition = transform.position + new Vector3(Random.Range(-1f, 1f), Random.Range(-1f, 1f)).normalized * Random.Range(minDashDistance, maxDashDistance);
            }

            float elapsed = 0;
            Vector2 dir = (dashPosition - transform.position).normalized;
            Vector3 initialPos = transform.position;
            while ((transform.position - dashPosition).sqrMagnitude > .2f && elapsed < .5f)
            {
                transform.position = Vector3.SmoothDamp(transform.position, dashPosition, ref velSpeed, dashSpeed);
                elapsed += Time.deltaTime;
                yield return null;
            }
            yield return new WaitForSeconds(.075f);

            if (i != 2)
            {
                PlaySound(attackSound);
                for (int j = 0; j < firePoint.Length; j++)
                {
                    Instantiate(muzzleFlash, firePoint[j].position, firePoint[j].rotation);
                    Bullet bullet = Instantiate(bulletPrefab, firePoint[j].position, firePoint[j].rotation);
                    bullet.whoShotIt = transform;
                    if (!isEnhanced)
                        bullet.transform.localScale *= 1.25f;
                }

                rb.AddForce(transform.up * 3f, ForceMode2D.Impulse);
            }
            else
            {
                Vector3 deviation = new Vector3(0, 0, -360f / 2f);
                PlaySound(secondarySound);
                for (int j = 0; j < numberOfEnhancedBullets; j++)
                {
                    CurveBullet smallBullet = Instantiate(enhancedBullet, enhancedFirePoint.position, Quaternion.Euler(enhancedFirePoint.rotation.eulerAngles + deviation));
                    deviation += Vector3.forward * (360 / numberOfEnhancedBullets);
                    smallBullet.whoShotIt = transform;
                    smallBullet.direction = j % 2 == 0 ? 1 : -1;
                }
            }
            yield return new WaitForSeconds(timeBetweenDashes);
        }
        yield return new WaitForSeconds(.5f);
        shootTimer = Random.Range(shootCooldown * .75f, shootCooldown * 1.25f);
        canBePushedBack = true;
        canMove = true;
        ChooseBehaviour(1);
    }
}
