using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class BossPurple : Enemy
{
    [Header("General")]
    private int lastAttackIndex = -1;
    private int secondLastAttackIndex = -1;
    private float maxHealth;
    private bool aimAheadOfPlayer = false;
    private float healthThreshold = 66;

    [Header("Bullet Spawners")]
    public float spawnRate;
    public Transform spawnMuzzleFlash;
    public SpawningBullet spawningBulletPrefab;
    public List<Transform> bulletSpawnersFirePoints;
    public float startSpawningTimer = 1.25f;
    public float spawningBulletLifeTime;

    [Header("Helix Bullets")]
    public float helixFireRate;
    public Transform helixMuzzleFlash;
    public PatternBullet helixBulletPrefab;
    public TrailBullet bigBulletPrefab;
    public Transform helixFirePoint;
    public List<Transform> helixFirePoints;
    public float numberOfBulletsShot;

    [Header("Bouncing Balls")]
    public float bouncingFireRate;
    public Transform bouncingMuzzleFlash;
    public List<Transform> bouncingFirePoints;
    public Bullet bouncingBulletPrefab;

    [Header("Orbiting Bullet")]
    public float chargeTime;
    public ParticleSystem chargeUpEffect;
    public Transform orbitingFirePoint;
    public Transform orbitingMuzzleFlash;
    public OrbitingBullet orbitingBulletPrefab;

    [Header("Sounds")]
    public AudioClip bulletSpawnersDeploySound, helixBulletsSound, bigBulletSound, bouncingBallSound, orbitingBulletSound;
    
    void Start()
    {
        MaterialSetup();
        shootTimer = Random.Range(1, shootCooldown);
        player = PlayerController.instance;
        curMoveSpeed = moveSpeed;

        maxHealth = health;

        if (activeRoom)
            activeRoom.sizeMultiplier = .75f;
    }

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
        healthBar.value = health / maxHealth;
        if (health / maxHealth * 100f <= healthThreshold && health > 0)
        {
            healthThreshold -= 33;
            StartCoroutine(activeRoom.SpawnWave());
        }
        if (aimAheadOfPlayer)
        {
            AimAheadOfPlayer();
        }
    }
    private void FixedUpdate()
    {
        if (player.isAlive)
        {
            EnemyAIFixedUpdate();
        }
    }
    public override IEnumerator StopAttack()
    {
        aimAheadOfPlayer = false;
        shootTimer = Random.Range(0, shootCooldown * 1.75f);
        curMoveSpeed = moveSpeed;
        ParticleSystem effect = GetComponentInChildren<ParticleSystem>();
        if (effect)
            Destroy(effect.gameObject);
        OrbitingBullet bullet;
        if (bullet = GetComponentInChildren<OrbitingBullet>())
            Destroy(bullet.gameObject);
        hasRequestedPathToPlayer = false;

        yield return null;
    }
    void ChooseAttack()
    {
        shouldShoot = false;
        shootTimer = 10f;
        int k = Random.Range(0, 4);
        while (k == lastAttackIndex && k == secondLastAttackIndex)
            k = Random.Range(0, 4);

        switch (k)
        {
            case 0:
                StartCoroutine(BulletSpawners());
                break;
            case 1:
                StartCoroutine(HelixBullets());
                break;
            case 2:
                StartCoroutine(BouncingBalls());
                break;
            case 3:
                StartCoroutine(OrbitingBullet());
                break;
        }
        secondLastAttackIndex = lastAttackIndex;
        lastAttackIndex = k;
    }

    IEnumerator BulletSpawners()
    {
        canMove = false;
        canLook = false;
        PlaySound(bulletSpawnersDeploySound);
        foreach(Transform firePoint in bulletSpawnersFirePoints)
        {
            Instantiate(spawnMuzzleFlash, firePoint.position, firePoint.rotation);
            SpawningBullet bullet = Instantiate(spawningBulletPrefab, firePoint.position, firePoint.rotation);
            bullet.startSpawningTimer = startSpawningTimer;
            bullet.whoShotIt = transform;
            bullet.lifeTime = spawningBulletLifeTime;
            bullet.spawnRate = spawnRate;
        }

        yield return new WaitForSeconds(.5f);
        shootTimer = Random.Range(0, shootCooldown * 1.25f);
        canMove = true;
        canLook = true;
        ChooseBehaviour(1);
    }

    IEnumerator HelixBullets()
    {
        canMove = false;
        canLook = false;
        aimAheadOfPlayer = true;
        for (int i = 0; i < numberOfBulletsShot; i++)
        {
            PlaySound(helixBulletsSound);
            Instantiate(helixMuzzleFlash, helixFirePoints[0].position, helixFirePoints[0].rotation).localScale = Vector3.one * 1.5f;
            Instantiate(helixMuzzleFlash, helixFirePoints[1].position, helixFirePoints[1].rotation).localScale = Vector3.one * 1.5f;
            PatternBullet bullet = Instantiate(helixBulletPrefab, helixFirePoint.position, helixFirePoint.rotation);
            bullet.whoShotIt = transform;

            yield return new WaitForSeconds(helixFireRate);
        }
        yield return new WaitForSeconds(helixFireRate / 2f);
        {
            PlaySound(bigBulletSound);
            Instantiate(muzzleFlash, helixFirePoint.position, helixFirePoint.rotation).localScale = Vector3.one * 2.25f;
            TrailBullet bullet = Instantiate(bigBulletPrefab, helixFirePoint.position, helixFirePoint.rotation);
            bullet.whoShotIt = transform;
            rb.AddForce(transform.up * 5f, ForceMode2D.Impulse);
            yield return new WaitForSeconds(helixFireRate);
        }

        shootTimer = Random.Range(0, shootCooldown * 1.25f);
        canMove = true;
        canLook = true;
        aimAheadOfPlayer = false;
        ChooseBehaviour(1);
    }

    IEnumerator BouncingBalls()
    {
        canMove = false;
        for (int i = 0; i < 6; i++)
        {
            PlaySound(bouncingBallSound);
            Instantiate(bouncingMuzzleFlash, bouncingFirePoints[i].position, bouncingFirePoints[i].rotation);
            Bullet bullet = Instantiate(bouncingBulletPrefab, bouncingFirePoints[i].position, bouncingFirePoints[i].rotation);
            bullet.whoShotIt = transform;

            if ( i % 2 == 1)
                yield return new WaitForSeconds(bouncingFireRate);
        }     
        yield return new WaitForSeconds(.25f);
        shootTimer = Random.Range(0, shootCooldown * 1.25f);
        canMove = true;
        ChooseBehaviour(1);
    }

    IEnumerator OrbitingBullet()
    {
        canMove = false;
        ParticleSystem effect = Instantiate(chargeUpEffect, orbitingFirePoint.position, orbitingFirePoint.rotation, transform);
        effect.Play();
        OrbitingBullet bullet = Instantiate(orbitingBulletPrefab, orbitingFirePoint.position, orbitingFirePoint.rotation, transform);
        float bulletSpeed = bullet.speed;
        Vector3 bulletScale = bullet.transform.localScale;
        bullet.startMoving = false;
        bullet.transform.localScale = Vector3.zero;
        bullet.speed = 0;
        bullet.GetComponent<Collider2D>().enabled = false;

        float elapsed = 0;
        while(elapsed < chargeTime)
        {
            bullet.transform.localScale = Vector3.Lerp(Vector3.zero, bulletScale, elapsed / chargeTime);
            elapsed += Time.deltaTime;
            yield return null;
        }
        PlaySound(orbitingBulletSound);

        canLook = false;

        effect.Stop();
        Destroy(effect.gameObject, 1.5f);
        Instantiate(orbitingMuzzleFlash, orbitingFirePoint.position, orbitingFirePoint.rotation).localScale = Vector3.one * 1.75f; ;
        bullet.target = player.transform;
        bullet.startMoving = true;
        bullet.transform.SetParent(null);
        bullet.SpawnSmallBullets();
        bullet.speed = bulletSpeed;
        bullet.GetComponent<Collider2D>().enabled = true;
        bullet.whoShotIt = this.transform;
        bullet.StopTargeting(2.5f);

        yield return new WaitForSeconds(.25f);
        shootTimer = Random.Range(0, shootCooldown * 1.25f);
        canMove = true;
        canLook = true;
        ChooseBehaviour(1);
    }
    private void OnDestroy()
    {
        healthBar.gameObject.SetActive(false);
        activeRoom.roomValue = unitValue * 2;
        activeRoom.SpawnChestFunction(true);
        foreach (Enemy enemy in FindObjectsOfType<Enemy>())
            enemy.TakeDamage(enemy.health + 1);
        foreach (Bullet bullet in FindObjectsOfType<Bullet>())
        {
            if (bullet.gameObject.layer == 11)
            {
                bullet.Explode();
                Destroy(bullet.gameObject);
            }
        }
    }
}
