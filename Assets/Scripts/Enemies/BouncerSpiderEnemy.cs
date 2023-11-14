using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BouncerSpiderEnemy : Enemy
{
    public Transform firePoint;
    public FragBullet bulletPrefab;
    public float chargeTime = 1f;
    public int numberOfSmallBullets;
    public AudioClip detonateSound;

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
        canMove = false;
        canBePushedBack = false;
        shootTimer = 10f;
        FragBullet bullet = Instantiate(bulletPrefab, firePoint.position, firePoint.rotation, transform);
        Vector3 initialScale = bullet.transform.localScale;
        bullet.transform.localScale = Vector3.zero;
        bullet.whoShotIt = transform;
        float elapsed = 0;
        PlaySound(attackSound);
        while (elapsed < chargeTime)
        {
            bullet.transform.localScale = Vector3.Lerp(Vector3.zero, initialScale, elapsed / chargeTime);
            elapsed += Time.deltaTime;
            yield return null;
        }
        PlaySound(detonateSound);
        bullet.Detonate(Random.Range(numberOfSmallBullets, numberOfSmallBullets + 2));
        bullet.Explode();
        Destroy(bullet.gameObject);
        yield return new WaitForSeconds(.5f);
        canMove = true;
        canBePushedBack = true;
        shootTimer = Random.Range(shootCooldown * 2 / 3f, shootCooldown * 4 / 3f);
        ChooseBehaviour(1);
    }

    public override IEnumerator StopAttack()
    {
        if (GetComponentInChildren<FragBullet>())
            Destroy(GetComponentInChildren<FragBullet>().gameObject);
        yield return base.StopAttack();
    }
}
