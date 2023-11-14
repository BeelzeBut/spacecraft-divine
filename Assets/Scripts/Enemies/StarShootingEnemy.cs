using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class StarShootingEnemy : Enemy
{
    public float chargeTime;
    public Bullet bulletPrefab;
    public ParticleSystem chargeEffect;
    public Transform firePoint;
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
        shootTimer = Random.Range(shootCooldown * 2 / 3f, shootCooldown * 4 / 3f);

        chargeEffect.Play();
        PlaySound(attackSound);

        yield return new WaitForSeconds(.75f);

        chargeEffect.Stop();
        PatternBullet bullet = Instantiate(bulletPrefab, firePoint.position, firePoint.rotation) as PatternBullet;
        bullet.target = player.transform;
        bullet.whoShotIt = this.transform;

        shootTimer = Random.Range(shootCooldown * 1 / 2f, shootCooldown * 3 / 2f);
        ChooseBehaviour(Random.Range(0, 2));
    }

    public override IEnumerator StopAttack()
    {
        chargeEffect.Stop();
        return base.StopAttack();
    }
}
