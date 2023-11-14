using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BoomerangEnemy : Enemy
{
    public Transform firePoint;
    public BoomerangBullet bulletPrefab;
    public float fireRate;
    public int bulletsShot = 3;
    public float spread;
    public bool isEnhanced;

    void Start()
    {
        MaterialSetup();
        shootTimer = Random.Range(1, shootCooldown);
        player = PlayerController.instance;

        transform.localScale = Vector3.zero;
        curMoveSpeed = moveSpeed;    
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
        if(!isEnhanced)
        {
            int k = 1;
            if (Random.Range(0, 2) == 1)
                k = -1;
            Vector3 deviation = Vector3.forward * 45f * k;
            PlaySound(attackSound);
            Instantiate(muzzleFlash, firePoint.transform.position, firePoint.transform.rotation);
            BoomerangBullet bullet = Instantiate(bulletPrefab, firePoint.transform.position, Quaternion.Euler(firePoint.rotation.eulerAngles + deviation));
            bullet.firePoint = firePoint;
            bullet.targetPos = player.transform.position;
            bullet.whoShotIt = transform;
        }
        else
        {
            bool k = true;
            for(int i = 0; i < Random.Range(bulletsShot, bulletsShot + 2); i++)
            {
                PlaySound(attackSound);
                Vector3 deviation = Vector3.forward * 45f * (k ? 1 : -1);
                Instantiate(muzzleFlash, firePoint.transform.position, firePoint.transform.rotation);
                BoomerangBullet bullet = Instantiate(bulletPrefab, firePoint.transform.position, Quaternion.Euler(firePoint.rotation.eulerAngles + deviation));
                bullet.firePoint = firePoint;
                bullet.targetPos = player.transform.position;
                bullet.whoShotIt = transform;
                k = !k;
                yield return new WaitForSeconds(fireRate);
            }
        }

        yield return new WaitForSeconds(.5f);
        ChooseBehaviour(Random.Range(0,2));
        shootTimer = Random.Range(shootCooldown * 2 / 3f, shootCooldown * 4 / 3f);
    }

}
