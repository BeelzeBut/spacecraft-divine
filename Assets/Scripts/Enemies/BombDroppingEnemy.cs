using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BombDroppingEnemy : Enemy
{
    public bool isEnhanced;
    public Transform firePoint;
    public ArchedBullet bombPrefab;
    public Bullet bulletPrefab;
    public float fireRate;
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
        ChooseBehaviour(0);

        PlaySound(attackSound);
        ArchedBullet bomb = Instantiate(bombPrefab, firePoint.position, Quaternion.Euler(0, 0, 90));
        bomb.targetPosition = player.transform.position + transform.right * .7f;
        bomb = Instantiate(bombPrefab, firePoint.position, Quaternion.Euler(0, 0, 90));
        bomb.targetPosition = player.transform.position - transform.right * .7f;

        if (isEnhanced)
        {
            yield return new WaitForSeconds(fireRate);
            Vector3 deviation = Vector3.forward * -30f;
            PlaySound(secondarySound);
            for(int i = 0; i < 4; i++)
            {
                Instantiate(muzzleFlash, firePoint.position, Quaternion.Euler(firePoint.rotation.eulerAngles + deviation));
                Bullet bullet = Instantiate(bulletPrefab, firePoint.position, Quaternion.Euler(firePoint.rotation.eulerAngles + deviation));
                bullet.whoShotIt = transform;
                deviation += Vector3.forward * 20f;
            }
        }

        yield return new WaitForSeconds(.5f);
        shootTimer = Random.Range(shootCooldown * .75f, shootCooldown * 1.25f);
        ChooseBehaviour(1);
    }
}
