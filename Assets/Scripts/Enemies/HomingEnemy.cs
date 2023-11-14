using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class HomingEnemy : Enemy
{
    public TargetedBullet missile;
    public List<Transform> firePoints = new List<Transform>();
    void Start()
    {
        MaterialSetup();
        shootTimer = Random.Range(1, shootCooldown);
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

    public IEnumerator Shoot()
    {
        shouldShoot = false;
        shootTimer = Random.Range(shootCooldown * 2 / 3f, shootCooldown * 4 / 3f);
        PlaySound(attackSound);
        foreach(Transform firePoint in firePoints)
        {
            Instantiate(muzzleFlash, firePoint.position, firePoint.rotation);
            TargetedBullet bullet = Instantiate(missile, firePoint.position, firePoint.rotation);
            bullet.whoShotIt = this.transform;
            bullet.target = player.transform;
            bullet.StopTargeting(2.5f);
        }
        yield return new WaitForSeconds(.5f);
        ChooseBehaviour(1);
    }
}
