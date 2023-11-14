using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class BossGrey : Enemy
{
    [Header ("General")]
    private int lastAttackIndex = -1;
    private int secondLastAttackIndex = -1;
    private float maxHealth;
    private bool aimAheadOfPlayer = false;
    private float healthThreshold = 66;

    [Header("Homing Missiles")]
    public float missilesFireRate;
    public int missilesShot = 6;
    public TargetedBullet missilePrefab;
    public Transform missileMuzzleFlash;
    public List<Transform> missileFirePoints;
    private int currentFirePoint;

    [Header("Charged Up Shot")]
    public float chargeTime;
    public TrailBullet trailBulletPrefab;
    public ParticleSystem chargeUpEffect;
    public Transform trailMuzzleFlash;
    public Transform trailFirePoint;

    [Header("Barrage")]
    public float barrageFireRate;
    public int bulletsShot = 15;
    public float spread = 1.75f;
    public Bullet bulletPrefab;
    public Transform bulletMuzzleFlash;
    public List<Transform> barrageFirePoints;

    [Header("Falling Bombs")]
    public float bombsFireRate;
    public int bombsShot;
    public ArchedBullet bombPrefab;
    public Transform bombMuzzleFlash;
    public Transform bombFirePoint;
    public float goDownTimer;

    public AudioClip homingMissileSound, bigShotChargeSound, bigShotShootSound, barrageSound, bombSound;

    private void Start()
    {
        
        shootTimer = Random.Range(1, shootCooldown);
        player = PlayerController.instance;
        curMoveSpeed = moveSpeed;
        MaterialSetup();
        maxHealth = health;
        if(activeRoom)
            activeRoom.sizeMultiplier = .75f;
    }

    private void Update()
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
        if(aimAheadOfPlayer)
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
        hasRequestedPathToPlayer = false;

        yield return null;
    }

    void ChooseAttack()
    {
        shouldShoot = false;
        shootTimer = 10f;
        int k = Random.Range(0, 4);
        while(k == lastAttackIndex && k == secondLastAttackIndex)
            k = Random.Range(0, 4);

        switch (k)
        {
            case 0: StartCoroutine(HomingMissiles());
                break;
            case 1:
                StartCoroutine(ChargedUpShot());
                break;
            case 2:
                StartCoroutine(Barrage());
                break;
            case 3:
                StartCoroutine(FallingBombs());
                break;
        }
        secondLastAttackIndex = lastAttackIndex;
        lastAttackIndex = k;
    }

    IEnumerator HomingMissiles()
    {
        canMove = false;
        for(int i = 0; i < missilesShot; i++)
        {
            if (currentFirePoint >= missileFirePoints.Count)
                currentFirePoint = 0;
            PlaySound(homingMissileSound);
            Instantiate(missileMuzzleFlash, missileFirePoints[currentFirePoint].position, missileFirePoints[currentFirePoint].rotation).localScale = Vector3.one * 1.25f;
            TargetedBullet missile = Instantiate(missilePrefab, missileFirePoints[currentFirePoint].position, missileFirePoints[currentFirePoint].rotation);
            missile.target = player.transform;
            missile.whoShotIt = this.transform;
            missile.StopTargeting(1.75f);
            currentFirePoint++;
            yield return new WaitForSeconds(missilesFireRate);
        }
        yield return new WaitForSeconds(.35f);
        shootTimer = Random.Range(0, shootCooldown * 1.25f);
        canMove = true;
        ChooseBehaviour(1);
    }

    IEnumerator ChargedUpShot()
    {
        canMove = false;
        canLook = false;
        aimAheadOfPlayer = true;
        ParticleSystem effect = Instantiate(chargeUpEffect, trailFirePoint.position, trailFirePoint.rotation, transform);
        effect.Play();
        PlaySound(bigShotChargeSound);
        yield return new WaitForSeconds(chargeTime);

        effect.Stop();
        Destroy(effect.gameObject, 1.5f);
        PlaySound(bigShotShootSound);
        Instantiate(trailMuzzleFlash, trailFirePoint.position, trailFirePoint.rotation).localScale = Vector3.one * 1.75f; ;
        TrailBullet bullet = Instantiate(trailBulletPrefab, trailFirePoint.position, trailFirePoint.rotation);
        bullet.whoShotIt = this.transform;
        rb.AddForce(transform.up * 5f, ForceMode2D.Impulse);
        
        yield return new WaitForSeconds(.25f);
        shootTimer = Random.Range(0, shootCooldown * 1.25f);
        canMove = true;
        canLook = true;
        aimAheadOfPlayer = false;
        ChooseBehaviour(1);
    }

    IEnumerator Barrage()
    {
        canLook = false;
        canMove = false;
        aimAheadOfPlayer = true;
        for (int i = 0; i < Random.Range(bulletsShot - 2, bulletsShot + 3); i++)
        {
            PlaySound(barrageSound);
            foreach (Transform firePoint in barrageFirePoints)
            {
                Instantiate(bulletMuzzleFlash, firePoint.position, firePoint.rotation);
                float bulletSpread = Random.Range(-1f, 1f) * spread * 7.5f;
                Bullet bullet = Instantiate(bulletPrefab, firePoint.position, Quaternion.Euler(firePoint.rotation.eulerAngles + Vector3.forward * bulletSpread));
                bullet.whoShotIt = this.transform;
            }
            yield return new WaitForSeconds(barrageFireRate);
        }
     
        yield return new WaitForSeconds(.25f); 
        shootTimer = Random.Range(0, shootCooldown * 1.25f);
        canMove = true;
        canLook = true;
        aimAheadOfPlayer = false;

        ChooseBehaviour(1);
    }

    IEnumerator FallingBombs()
    {
        int n = Random.Range(bombsShot, bombsShot + 3);
        for (int i = 0; i < n; i++)
        {
            PlaySound(bombSound);
            Vector3 targetPos = player.transform.position + new Vector3(Random.Range(-.5f * (n - i - 1), .5f * (n - i - 1)), Random.Range(-.5f * (n - i - 1), .5f * (n - i - 1)));
            ArchedBullet bomb = Instantiate(bombPrefab, bombFirePoint.position, Quaternion.Euler(0, 0, 90));
            bomb.whoShotIt = transform;
            bomb.targetPosition = targetPos;
            bomb.goDownTimer = goDownTimer;
            yield return new WaitForSeconds(bombsFireRate);
        }


        yield return new WaitForSeconds(.35f);
        shootTimer = Random.Range(0f, shootCooldown * 1.25f);
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
