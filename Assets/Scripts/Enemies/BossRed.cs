using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class BossRed : Enemy
{
    [Header("General")]
    private int lastAttackIndex = -1;
    private int secondLastAttackIndex = -1;
    private float maxHealth;
    private bool aimAheadOfPlayer = false;
    private float healthThreshold = 66;
    private bool shouldStopCoroutine;

    [Header("Charged Laser")]
    public Transform laserFirePoint;
    [SerializeField]
    public GameObject explosionEffect;
    [SerializeField]
    public GameObject laserMuzzle;
    public ParticleSystem chargeUpEffectPrefab;
    private ParticleSystem chargeEffect;
    private GameObject muzzle;
    private GameObject explosion;
    public LineRenderer laserPrefab;
    private LineRenderer laser;
    public float damagePerTick;
    public float laserFireRate;
    public float chargeTime;
    public float laserDuration;
    private float initialTurningSpeed;

    [Header("Rotating Star")]
    public Transform starFirePoint;
    public int numberOfBullets = 5;
    public int numberOfWaves;
    public float starFireRate = .15f;
    public Bullet curveBulletPrefab;
    public float firePointRotationSpeed = 75;

    [Header("Slowing Bullets Pattern")]
    public Transform slowingFirePoint;
    public SlowingBullet slowingBulletPrefab;
    public float slowingFireRate;
    public int explosionAngle = 30;
    Vector3 bigDeviation;

    [Header("Bullet Hell")]
    public Transform bulletHellFirePoint;
    public CurveBullet oscilatingBulletPrefab;
    public float bulletHellFireRate;
    public int bulletHellNumberOfBullets;
    public int bulletHellNumberOfWaves;

    public AudioClip bulletHellSound, slowingBulletPatternSound, rotatingStarSound, laserSound, laserChargeSound;

    void Start()
    {
        MaterialSetup();
        shootTimer = Random.Range(1, shootCooldown);
        player = PlayerController.instance;
        curMoveSpeed = moveSpeed;

        maxHealth = health;

        LaserSetup();
        if (activeRoom)
            activeRoom.sizeMultiplier = .75f;
        source.clip = laserSound;
        source.loop = false;
        source.playOnAwake = false;
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
        shootTimer = Random.Range(0, shootCooldown * 1.75f);
        curMoveSpeed = moveSpeed;
        chargeEffect.Stop();
        laser.enabled = false;
        muzzle.SetActive(false);
        explosion.SetActive(false);
        hasRequestedPathToPlayer = false;
        shouldStopCoroutine = false;
        starFirePoint.rotation = Quaternion.Euler(0, 0, transform.rotation.eulerAngles.z - 90);
        laserFirePoint.rotation = Quaternion.Euler(0, 0, transform.rotation.eulerAngles.z - 90);
        slowingFirePoint.rotation = Quaternion.Euler(0, 0, transform.rotation.eulerAngles.z - 90);
        bulletHellFirePoint.rotation = Quaternion.Euler(0, 0, transform.rotation.eulerAngles.z - 90);
        shouldStopCoroutine = true;
        source.Stop();
        yield return null;
    }
    void LaserSetup()
    {
        laser = Instantiate(laserPrefab, transform.position, transform.rotation, transform);
        laser.enabled = false;
        explosion = Instantiate(explosionEffect, transform.position, transform.rotation, transform);
        explosion.SetActive(false);
        muzzle = Instantiate(laserMuzzle, transform.position, transform.rotation, transform);
        muzzle.SetActive(false);
        chargeEffect = Instantiate(chargeUpEffectPrefab, laserFirePoint.position, Quaternion.identity, transform);
        initialTurningSpeed = turningspeed;
        turningspeed = initialTurningSpeed;
        shootTimer = Random.Range(.5f, shootCooldown);

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
                StartCoroutine(ChargedLaser());
                break;
            case 1:
                StartCoroutine(RotatingStar());
                break;
            case 2:
                StartCoroutine(SlowingBulletsPattern());
                break;
            case 3:
                StartCoroutine(BulletHell());
                break;
        }
        secondLastAttackIndex = lastAttackIndex;
        lastAttackIndex = k;
    }

    IEnumerator ChargedLaser()
    {
        float shootCounter = .05f;
        shouldShoot = false;
        shootTimer = 15f;
        canMove = false;
        canBePushedBack = false;
        float elapsed = 0;
        chargeEffect.transform.localPosition = laserFirePoint.localPosition;
        chargeEffect.Play();
        PlaySound(laserChargeSound);
        while (elapsed < chargeTime)
        {
            elapsed += Time.deltaTime;
            yield return null;
        }
        turningspeed *= 1.38f;
        elapsed = 0;
        laser.enabled = true;
        explosion.SetActive(true);
        muzzle.SetActive(true);

        source.Play();
        while (elapsed < laserDuration)
        {
            RaycastHit2D hit = Physics2D.Raycast(laserFirePoint.position, laserFirePoint.right, 10f, roomLayermask | obstacleLayermask);
            laser.SetPosition(0, laserFirePoint.position);
            if (hit.collider)
                laser.SetPosition(1, hit.point);
            else
                laser.SetPosition(1, laserFirePoint.position + laserFirePoint.right * 10f);
            explosion.transform.position = laser.GetPosition(1);
            muzzle.transform.position = laserFirePoint.position;
            if (shootCounter <= 0)
            {
                RaycastHit2D[] hitPlayer = Physics2D.CircleCastAll(laserFirePoint.position, .05f, laserFirePoint.right, 10f, LayerMask.GetMask("Player") | roomLayermask | obstacleLayermask);
                for (int i = 0; i < hitPlayer.Length; i++)
                {
                    if (hitPlayer[i].collider != null)
                    {
                        if (hitPlayer[i].collider.gameObject.layer != 12)
                            break;
                        if (hitPlayer[i].collider.CompareTag("Player"))
                        {
                            player.TakeDamage(damagePerTick);
                        }
                    }
                }
                shootCounter = laserFireRate;
            }
            shootCounter -= Time.deltaTime;

            elapsed += Time.deltaTime;
            yield return null;
        }
        source.Stop();
        chargeEffect.Stop();
        laser.enabled = false;
        explosion.SetActive(false);
        muzzle.SetActive(false);
        rb.AddForce(transform.up * 4.25f, ForceMode2D.Impulse);
        turningspeed = initialTurningSpeed;
        yield return new WaitForSeconds(.35f);
        shootTimer = Random.Range(.5f, shootCooldown);
        canMove = true;
    }
    IEnumerator RotatingStar()
    {
        shouldShoot = false;
        shootTimer = 10f;
        canLook = false;
        canMove = false;
        shouldStopCoroutine = false;
        StartCoroutine(RotateFirePoint(starFirePoint));
        for (int i = 0; i < numberOfWaves; i++)
        {
            Vector3 deviation = new Vector3(0, 0, -360 / 2f);
            PlaySound(rotatingStarSound);
            for (int j = 0; j < numberOfBullets; j++)
            {
                Instantiate(muzzleFlash, starFirePoint.position, Quaternion.Euler(starFirePoint.rotation.eulerAngles + deviation));
                Bullet smallBullet = Instantiate(curveBulletPrefab, starFirePoint.position, Quaternion.Euler(starFirePoint.rotation.eulerAngles + deviation));
                deviation += Vector3.forward * (360f / numberOfBullets);
                smallBullet.whoShotIt = transform;
            }
            yield return new WaitForSeconds(starFireRate);
        }
        shouldStopCoroutine = true;
        starFirePoint.rotation = Quaternion.Euler(0, 0, transform.rotation.eulerAngles.z - 90);
        yield return new WaitForSeconds(.2f);
        shootTimer = Random.Range(.5f, shootCooldown);
        canLook = true;
        canMove = true;
        ChooseBehaviour(1);
    }
    IEnumerator RotateFirePoint(Transform firePoint)
    {
        while (!shouldStopCoroutine)
        {
            firePoint.Rotate(Vector3.forward * firePointRotationSpeed * Time.deltaTime);
            yield return null;
        }
        shouldStopCoroutine = false;
    }

    IEnumerator SlowingBulletsPattern()
    {
        shouldShoot = false;
        shootTimer = 10f;
        canLook = false;
        canMove = false;
        bigDeviation = Vector3.zero;
        for (int j = 0; j < 2; j++)
        {
            bool hasSound = true;
            for (int i = 0; i < 5; i++)
            {
                StartCoroutine(ShootBurst(bigDeviation, hasSound));
                bigDeviation += Vector3.forward * 72f;
                hasSound = false;
            }

            yield return new WaitForSeconds(slowingFireRate);
            bigDeviation += Vector3.forward * 36f;
        }

        yield return new WaitForSeconds(.5f);
        shootTimer = Random.Range(.5f, shootCooldown);
        canLook = true;
        canMove = true;
        ChooseBehaviour(1);
    }
    IEnumerator ShootBurst(Vector3 bigDeviation, bool hasSound)
    {
        if(hasSound)
            PlaySound(slowingBulletPatternSound);

        Instantiate(slowingBulletPrefab, slowingFirePoint.position, Quaternion.Euler(slowingFirePoint.rotation.eulerAngles + bigDeviation));
        yield return new WaitForSeconds(.15f);
        Vector3 deviation = new Vector3(0, 0, -explosionAngle / 2f);

        if(hasSound)
            PlaySound(slowingBulletPatternSound);

        for (int j = 0; j < 2; j++)
        {
            SlowingBullet smallBullet = Instantiate(slowingBulletPrefab, slowingFirePoint.position, Quaternion.Euler(slowingFirePoint.rotation.eulerAngles + deviation + bigDeviation));
            deviation += Vector3.forward * explosionAngle;
            smallBullet.whoShotIt = transform;
        }
    }
    IEnumerator BulletHell()
    {
        shouldShoot = false;
        shootTimer = 10f;
        canLook = false;
        canMove = false;
        for (int i = 0; i < bulletHellNumberOfWaves; i++)
        {
            Vector3 deviation = new Vector3(0, 0, -360 / 2f);
            PlaySound(bulletHellSound);
            for (int j = 0; j < bulletHellNumberOfBullets; j++)
            {
                Instantiate(muzzleFlash, bulletHellFirePoint.position, Quaternion.Euler(bulletHellFirePoint.rotation.eulerAngles + deviation));
                Bullet smallBullet = Instantiate(oscilatingBulletPrefab, bulletHellFirePoint.position, Quaternion.Euler(bulletHellFirePoint.rotation.eulerAngles + deviation));
                deviation += Vector3.forward * (360f / bulletHellNumberOfBullets);
                smallBullet.whoShotIt = transform;
            }
            yield return new WaitForSeconds(starFireRate);
        }
        yield return new WaitForSeconds(.35f);
        shootTimer = Random.Range(.5f, shootCooldown);
        canLook = true;
        canMove = true;
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
