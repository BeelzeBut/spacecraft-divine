using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Pathfinding;

public class ShockEnemy : Enemy
{
    [Header ("Shock")]  
    public Transform firePoint;
    public ExplosionBullet shockBulletPrefab;
    Vector3 direction;

    [Header("Triple Lasers")]
    public float fireRate;
    public Bullet laserPrefab;
    public Transform laserMuzzleFlash;

    [Header("Bullet Pattern")]
    public int numberOfWaves;
    public int numberOfBullets;
    public CurveBullet bulletPrefab;
    public float waveFireRate;

    public AudioClip shockSound, laserSound, bulletSound;

    void Start()
    {
        MaterialSetup();

        player = PlayerController.instance;
        curMoveSpeed = moveSpeed;

        initialScale = transform.localScale;
        transform.localScale = Vector3.zero;
    }

    // Update is called once per frame
    void Update()
    {
        if (player.isAlive)
        {
            EnemyAI();

            if (shouldShoot)
            {
                ChooseAttack();
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

    void ChooseAttack()
    {
        shouldShoot = false;
        shootTimer = 10f;
        int k = Random.Range(0, 3);

        switch (k)
        {
            case 0:
                Shock();
                break;
            case 1:
                StartCoroutine(TripleLasers());
                break;
            case 2:
                StartCoroutine(BulletPattern());
                break;
        }
    }
    IEnumerator BulletPattern()
    {
        shouldShoot = false;
        shootTimer = 10f;
        canLook = false;
        canMove = false;
        canBePushedBack = false;
        for(int i = 0; i < numberOfWaves ; i++)
        {
            Vector3 deviation = new Vector3(0, 0, -360 / 2f);
            PlaySound(bulletSound);
            for (int j = 0; j < numberOfBullets; j++)
            {
                Bullet smallBullet = Instantiate(bulletPrefab, firePoint.position, Quaternion.Euler(firePoint.rotation.eulerAngles + deviation));
                deviation += Vector3.forward * (360 / numberOfBullets);
                smallBullet.whoShotIt = transform;
            }
            yield return new WaitForSeconds(waveFireRate);
        }

        yield return new WaitForSeconds(.2f);
        canBePushedBack = true;
        shootTimer = Random.Range(shootCooldown * .5f, shootCooldown);
        canLook = true;
        canMove = true;
        ChooseBehaviour(1);
    }
    IEnumerator TripleLasers()
    {
        shouldShoot = false;
        shootTimer = 10;
        canBePushedBack = false;
        canMove = false;
        for (int i = 0; i < 3; i++)
        {
            PlaySound(laserSound);
            Instantiate(laserMuzzleFlash, firePoint.position, firePoint.rotation);
            Bullet bullet = Instantiate(laserPrefab, firePoint.position, firePoint.rotation);
            bullet.whoShotIt = transform;

            if(i < 2)
                yield return new WaitForSeconds(fireRate);
        }
        direction = transform.up;
        rb.AddForce(direction * 4f, ForceMode2D.Impulse);

        yield return new WaitForSeconds(.25f);
        canBePushedBack = true;
        shootTimer = Random.Range(shootCooldown * .5f, shootCooldown);
        canMove = true;
        ChooseBehaviour(1);
    }

    void Shock()
    {
        shootTimer = Random.Range(shootCooldown * .5f, shootCooldown);
        shouldShoot = false;

        direction = player.transform.position - transform.position;
        Vector3 deviation = Vector3.forward * -30f;
        float distance = direction.magnitude;
        PlaySound(shockSound);
        for (int i = 0; i < 3; i++)
        {
            ExplosionBullet shockBullet = Instantiate(shockBulletPrefab, firePoint.position, Quaternion.Euler(firePoint.rotation.eulerAngles + deviation));
            shockBullet.explodeAtLocation = true;
            shockBullet.locationToExplode = shockBullet.transform.position + shockBullet.transform.right * distance;
            shockBullet.whoShotIt = this.transform;
            deviation += Vector3.forward * 30;
            /*direction = (player.transform.position - transform.position).normalized;
            float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
            shockBullet.transform.rotation = Quaternion.Euler(0, 0, angle);*/
        }
        direction = transform.up;
        rb.AddForce(direction * 3f, ForceMode2D.Impulse);
        ChooseBehaviour(Random.Range(0, 2));
    }
}
