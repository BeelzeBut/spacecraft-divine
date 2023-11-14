using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ArrowShootingEnemy : Enemy
{
    public Transform[] firePoints;
    public Bullet bulletPrefab;
    public PatternBullet helixBulletPrefab;
    public float fireRate;
    public bool isEnhanced = false;

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
                StartCoroutine(Shoot());
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
        shootTimer = 10f;
        canLook = false;
        canMove = false;
        canBePushedBack = false;
        if (!isEnhanced)
        {
            PlaySound(attackSound);
            for (int i = 0; i < 3; i++)
            {
                if (i != 0)
                {
                    Instantiate(muzzleFlash, firePoints[0].position, firePoints[0].rotation);
                    Bullet bullet = Instantiate(bulletPrefab, firePoints[0].position, firePoints[0].rotation);
                    bullet.whoShotIt = transform;
                }
                else
                {
                    for (int j = 0; j < 3; j++)
                    {
                        Instantiate(muzzleFlash, firePoints[j].position, firePoints[j].rotation);
                        Bullet bullet = Instantiate(bulletPrefab, firePoints[j].position, firePoints[j].rotation);
                        bullet.whoShotIt = transform;
                    }
                }
                yield return new WaitForSeconds(fireRate);
            }
        }
        else
        {
            PlaySound(attackSound);
            Instantiate(muzzleFlash, firePoints[0].position, firePoints[0].rotation);
            Bullet bullet = Instantiate(bulletPrefab, firePoints[0].position, firePoints[0].rotation);
            bullet.whoShotIt = transform;
            bullet.damage = 1.75f;

            Instantiate(muzzleFlash, firePoints[1].position, firePoints[1].rotation);
            Instantiate(muzzleFlash, firePoints[2].position, firePoints[2].rotation);
            PatternBullet helixBullet = Instantiate(helixBulletPrefab, firePoints[0].position, firePoints[0].rotation);
            helixBullet.whoShotIt = transform;
            helixBullet.damage = 3.5f;
            helixBullet.target = player.transform;
            helixBullet.isHoming = true;
            helixBullet.StopTargeting(1f);
        }
        yield return new WaitForSeconds(.5f);
        canLook = true;
        canMove = true;
        canBePushedBack = true;
        ChooseBehaviour(1);
        shootTimer = Random.Range(shootCooldown * .75f, shootCooldown * 1.25f);
    }
}
