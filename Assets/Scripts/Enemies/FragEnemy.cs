using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FragEnemy : Enemy
{
    public List<Transform> firePoints = new List<Transform>();
    public Transform mainFirePoint;
    public FragBullet bulletPrefab;

    public float bulletExplodeTimer;
    public bool isEnhanced;
    void Start()
    {
        MaterialSetup();
        player = PlayerController.instance;
        initialScale = transform.localScale;
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
        shootTimer = Random.Range(shootCooldown * 2 / 3f, shootCooldown * 4 / 3f) + .6f;
        //for (int i = 0; i < 2; i++) // bullets / firePoint
        foreach (Transform firePoint in firePoints)
        {
            PlaySound(attackSound);
            Instantiate(muzzleFlash, firePoint.transform.position, firePoint.transform.rotation);
            FragBullet bullet = Instantiate(bulletPrefab, firePoint.transform.position, firePoint.transform.rotation);
            bullet.explodeTimer = bulletExplodeTimer;
            yield return new WaitForSeconds(.3f);
        }

        if(isEnhanced)
        {
            yield return new WaitForSeconds(.1f);
            PlaySound(attackSound);
            Instantiate(muzzleFlash, mainFirePoint.transform.position, mainFirePoint.transform.rotation);
            FragBullet bullet = Instantiate(bulletPrefab, mainFirePoint.transform.position, mainFirePoint.transform.rotation);
            bullet.explodeTimer = bulletExplodeTimer;
            bullet.speed *= 1.7f;
            bullet.damage *= 1.25f;
            rb.AddForce(transform.up * 2f, ForceMode2D.Impulse);
            yield return new WaitForSeconds(.3f);
        }
        ChooseBehaviour(1);


    }

}
