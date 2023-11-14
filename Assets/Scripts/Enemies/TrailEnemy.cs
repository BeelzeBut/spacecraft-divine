using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TrailEnemy : Enemy
{

    public Transform[] firePoints;
    public TrailBullet bulletPrefab;
    public bool isEnhanced;
    public float fireRate;

    void Start()
    {
        MaterialSetup();

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

    public IEnumerator Shoot()
    {
        shootTimer = Random.Range(shootCooldown * 2 / 3f, shootCooldown * 4 / 3f);
        shouldShoot = false;
        canLook = false;
        if (!isEnhanced)
        {
            PlaySound(attackSound);
            Instantiate(muzzleFlash, firePoints[0].transform.position, firePoints[0].transform.rotation);
            Bullet bullet = Instantiate(bulletPrefab, firePoints[0].transform.position, firePoints[0].transform.rotation);
            bullet.whoShotIt = this.transform;
        }
        else
        {
            for (int i = 0; i < 3; i++)
            {
                PlaySound(attackSound);
                Instantiate(muzzleFlash, firePoints[i].transform.position, firePoints[i].rotation);
                Bullet bullet = Instantiate(bulletPrefab, firePoints[i].transform.position, firePoints[i].rotation);
                bullet.whoShotIt = this.transform;
                yield return new WaitForSeconds(fireRate);
            }
        }
        yield return new WaitForSeconds(.5f);
        canLook = true;
        ChooseBehaviour(1);
        shootTimer = Random.Range(shootCooldown * 2 / 3f, shootCooldown * 4 / 3f);
    }
}
