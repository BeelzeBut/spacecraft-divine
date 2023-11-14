using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class HeavyBasicEnemy : Enemy
{
    public bool isOrange;
    public Transform[] firePoints;
    public Bullet bigBulletPrefab;
    public Bullet smallBulletPrefab;
    public float fireRate;
    public float spread;
    public AudioClip bigAttackSound;

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
        shootTimer = 10f;
        if(!isOrange)
        {
            Instantiate(muzzleFlash, firePoints[1].position, firePoints[0].rotation);
            Instantiate(muzzleFlash, firePoints[2].position, firePoints[0].rotation);
            PlaySound(bigAttackSound);
            Bullet bullet = Instantiate(bigBulletPrefab, firePoints[0].position, firePoints[0].rotation);
            bullet.whoShotIt = this.transform;
        }
        else
        {
            canLook = false;
            PlaySound(attackSound);
            for (int i = 1; i < 3; i++)
            {
                Instantiate(muzzleFlash, firePoints[i].position, firePoints[i].rotation);
                Bullet smallBullet = Instantiate(smallBulletPrefab, firePoints[i].position, firePoints[i].rotation);
                smallBullet.whoShotIt = this.transform;
            }
            yield return new WaitForSeconds(fireRate / 4f);
            canLook = true;
            yield return new WaitForSeconds(fireRate * 3/4f);
            canLook = false;
            Instantiate(muzzleFlash, firePoints[1].position, firePoints[0].rotation);
            Instantiate(muzzleFlash, firePoints[2].position, firePoints[0].rotation);
            PlaySound(bigAttackSound);
            Bullet bullet = Instantiate(bigBulletPrefab, firePoints[0].position, firePoints[0].rotation);
            bullet.whoShotIt = this.transform;
        }

        yield return new WaitForSeconds(.5f);
        canLook = true;
        canMove = true;
        shootTimer = Random.Range(shootCooldown * 2 / 3f, shootCooldown * 4 / 3f);
        ChooseBehaviour(Random.Range(0, 2));
    }
}
