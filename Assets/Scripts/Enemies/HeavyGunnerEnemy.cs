using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class HeavyGunnerEnemy : Enemy
{
    public StickyBullet stickyBullet;
    public SlowingBullet slowingBullet;
    public Bullet bullet;
    public float fireRate;
    public float spread;
    bool isShooting;
    public List<Transform> frontFirePoints = new List<Transform>();
    public List<Transform> backFirePoints = new List<Transform>();
    public List<Transform> normalFirePoints = new List<Transform>();
    public float mineCooldown;
    float mineTimer;
    public float normalShootCooldown;
    float normalShootTimer;
    bool startedGoingToPlayer;
    public float normalFireRate = 0.1f;
    public AudioClip shootSound, mineSound, bulletSound;

    void Start()
    {
        MaterialSetup();
        shootTimer = Random.Range(1, shootCooldown);
        normalShootTimer = Random.Range(1, normalShootCooldown);
        mineTimer = Random.Range(1, mineCooldown);
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
            if (player.shouldBeAttacked)
            {
                mineTimer -= Time.deltaTime;
                if (mineTimer <= 0)
                {
                    mineTimer = Random.Range(2f / 3f * mineCooldown, 4f / 3f * mineCooldown);
                    LeaveMine();
                }
                normalShootTimer -= Time.deltaTime;
                if (normalShootTimer <= 0 && !isShooting && canLook)
                {
                    normalShootTimer = Random.Range(2f / 3f * normalShootCooldown, 4f / 3f * normalShootCooldown);
                    StartCoroutine(NormalShoot());
                }
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
        startedGoingToPlayer = false;
        shouldShoot = false;
        isShooting = true;
        shootTimer = 10f;
        int k = 0;
        canLook = false;
        rb.velocity = Vector2.zero;
        canBePushedBack = false;
        canMove = false;
        shootTimer = 10f;

        for (int i = 0; i < Random.Range(7, 11); i++)
        {
            if (k >= frontFirePoints.Count)
                k = 0;
            float spread = Random.Range(-1f, 1f) * this.spread * 7.5f;
            Vector2 lookDir = player.transform.position - transform.position;
            float angle = Mathf.Atan2(lookDir.y, lookDir.x) * Mathf.Rad2Deg + 90;
            transform.rotation = Quaternion.Euler(0, 0, angle);
            transform.rotation = Quaternion.Euler(transform.rotation.eulerAngles + Vector3.forward * spread);

            PlaySound(bulletSound);
            Instantiate(muzzleFlash, frontFirePoints[k].position, frontFirePoints[k].rotation);
            StickyBullet bullet = Instantiate(stickyBullet, frontFirePoints[k].position, frontFirePoints[k].rotation);
            k++;
            bullet.whoShotIt = this.transform;
            yield return new WaitForSeconds(fireRate);
        }
        canLook = true;
        ChooseBehaviour(0);

        yield return new WaitForSeconds(1f);

        canMove = true;
        
        ChooseBehaviour(1);
        isShooting = false;
        canBePushedBack = true;

        shootTimer = Random.Range(shootCooldown * .75f, shootCooldown * 1.25f);
        startedGoingToPlayer = true;
        foreach(StickyBullet bullet in FindObjectsOfType<StickyBullet>())
        {
            bullet.goToPlayer = true;
        }
    }

    public override IEnumerator StopAttack()
    {
        canMove = true;
        canLook = true;
        isShooting = false;
        shootTimer = Random.Range(shootCooldown * .75f, shootCooldown * 1.25f);
        startedGoingToPlayer = true;
        foreach (StickyBullet bullet in FindObjectsOfType<StickyBullet>())
        {
            bullet.goToPlayer = true;
        }
        canBePushedBack = true;
        hasRequestedPathToPlayer = false;

        yield return null;
    }
    public void LeaveMine()
    {
        PlaySound(mineSound);
        foreach (Transform backFirePoint in backFirePoints)
        {
            SlowingBullet bullet = Instantiate(this.slowingBullet, backFirePoint.position, backFirePoint.rotation);
            bullet.whoShotIt = this.transform;
            bullet.lifeTime = Random.Range(3f, 4.5f);
        }
    }
    public IEnumerator NormalShoot()
    {
        canMove = false;
        for (int i = 0; i < 2; i++)
        {
            PlaySound(shootSound);
            foreach (Transform firePoint in normalFirePoints)
            {
                Vector2 lookDir = player.transform.position - firePoint.transform.position;
                float angle = Mathf.Atan2(lookDir.y, lookDir.x) * Mathf.Rad2Deg;
                firePoint.transform.rotation = Quaternion.Euler(0, 0, angle);
                Instantiate(muzzleFlash, firePoint.position, firePoint.rotation);
                Bullet bulet = Instantiate(this.bullet, firePoint.position, firePoint.rotation);
                bullet.whoShotIt = this.transform;
            }
            yield return new WaitForSeconds(normalFireRate);
        }
        yield return new WaitForSeconds(.25f);
        canMove = true;
    }

    private void OnDestroy()
    {
        if(!startedGoingToPlayer)
        {
            foreach (StickyBullet bullet in FindObjectsOfType<StickyBullet>())
            {
                bullet.goToPlayer = true;
            }
        }
    }
}
