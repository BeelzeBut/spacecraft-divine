using DigitalRuby.LightningBolt;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class BossYellow : Enemy
{
    [Header("General")]
    private int lastAttackIndex = -1;
    private int secondLastAttackIndex = -1;
    private float maxHealth;
    private bool aimAheadOfPlayer = false;
    private float healthThreshold = 66;
    private float initialTurningSpeed;

    [Header("TripleDash")]
    public float damagePerDash;
    public float dashRange = 3f;
    public float dashSpeed;
    public float chargeTime;
    public bool isDashing;
    public GameObject lightning;
    public GameObject thruster;
    private Vector3 velSpeed;

    [Header("Boomerangs")]
    public Transform[] boomerangFirePoints;
    public BoomerangBullet boomerangPrefab;
    public float boomerangRange;
    public float boomerangFireRate;
    public int numberOfBoomerangs;

    [Header("Bullets Shield")]
    public CurveBullet curveBulletPrefab;
    public List<Bullet> bullets = new List<Bullet>();
    public Transform mainFirePoint;
    public int numberOfBullets;
    public float rotationTime;

    [Header("Orbiting Bullets")]
    public TargetedBullet orbitingBulletPrefab;
    public float bulletSpeed;
    public int numberOfOribtingBullets = 6;
    public float rotationSpeed;
    public float orbitingRadius;
    public float timeToShoot = 2f;
    public float orbitingBulletsFireRate;
    public Transform holderPrefab;
    private Transform holder;
    public List<TargetedBullet> components = new List<TargetedBullet>();
    public bool aliveOrbitingBullets;
    public Transform bulletStartExplosion;

    [Header("Enemy Spawner")]
    public Transform enemySpawnDirection;
    private bool isAttacking;
    public float spawnCooldown = 12.5f;
    private float spawnTimer;
    public float healthPerSecond = 4f;
    public Enemy[] possibleEnemies;
    private Enemy[] selectedEnemies = new Enemy[2];
    public Transform shieldPrefab;
    private Transform activeShield;
    public LineRenderer linePrefab;
    List<Enemy> aliveSmallEnemies = new List<Enemy>();
    List<LineRenderer> lines = new List<LineRenderer>();
    public int numberOfEnemiesSpawned;
    private bool hasSpawnedEnemies;

    public AudioClip boomerangSound, shieldSound, orbitingBulletDeploySound, dashSound, bulletsShieldRotateSound, bulletsShieldReleaseSound;

    private void Start()
    {
        shootTimer = Random.Range(1, shootCooldown);
        player = PlayerController.instance;
        curMoveSpeed = moveSpeed;
        MaterialSetup();
        maxHealth = health;
        if (activeRoom)
            activeRoom.sizeMultiplier = .5f;
        initialTurningSpeed = turningspeed;
        lightning.GetComponentInChildren<LightningBoltScript>().StartPosition = new Vector3(0.364f, -.419f, 0);
        lightning.GetComponentInChildren<LightningBoltScript>().EndPosition = new Vector3(-0.364f, -.419f, 0);
        spawnTimer = spawnCooldown * 1.5f;
        holder = Instantiate(holderPrefab, transform.position, transform.rotation);
    }

    private void Update()
    {
        holder.transform.position = transform.position;
        holder.Rotate(Vector3.forward * rotationSpeed * Time.deltaTime);
        if (player.isAlive)
        {
            EnemyAI();

            if (shouldShoot)
            {
                ChooseAttack();
            }

            if (spawnTimer > 0)
            {
                spawnTimer -= Time.deltaTime;
                if (spawnTimer <= 0 && !hasSpawnedEnemies)
                {
                    if (!isAttacking)
                    {
                        StartCoroutine(StopAttack());
                        shouldShoot = false;
                        shootTimer = 1000;
                        randomizeMovePosition = false;
                        movePosition = activeRoom.transform.position;
                        canLook = false;
                        ChooseBehaviour(1);
                        StartCoroutine(SpawnEnemies());
                    }
                    else
                        spawnTimer = 1f;
                }
            }
            if (hasSpawnedEnemies && health <= maxHealth)
                health += healthPerSecond * Time.deltaTime;
            CheckForSpawnedEnemies();
        }
        healthBar.value = health / maxHealth;

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
        hasRequestedPathToPlayer = false;
        lightning.SetActive(false);
        thruster.SetActive(false);
        if (bullets.Count > 0)
            foreach (Bullet bullet in bullets)
                if (bullet)
                    Destroy(bullet.gameObject);
        gameObject.tag = "Enemy";
        foreach(TargetedBullet bullet in components)
        {
            Instantiate(bulletStartExplosion, bullet.transform.position, bullet.transform.rotation);
            bullet.transform.SetParent(null);
            Vector3 lookDir = player.transform.position - bullet.transform.position;
            float angle = Mathf.Atan2(lookDir.y, lookDir.x) * Mathf.Rad2Deg;
            bullet.transform.rotation = Quaternion.Euler(0, 0, angle);
            bullet.speed = bulletSpeed;
            if (player.shouldBeAttacked)
            {
                bullet.target = player.transform;
                bullet.StopTargeting(1.25f);
            }
            bullet.ignoreCollisionLayer = 0;
            bullet.canPierce = false;
        }
        components.Clear();
        StopAllCoroutines();
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
                StartCoroutine(TripleDash());
                break;
            case 1:
                StartCoroutine(Boomerangs());
                break;
            case 2:
                StartCoroutine(BulletsShield());
                break;
            case 3:
                if (aliveOrbitingBullets)
                {
                    ChooseAttack();
                    return;
                }
                StartCoroutine(OrbitingBullets());
                break;
        }
        secondLastAttackIndex = lastAttackIndex;
        lastAttackIndex = k;
    }

    IEnumerator OrbitingBullets()
    {
        ChooseBehaviour(1);
        aliveOrbitingBullets = true;
        shouldShoot = false;
        shootTimer = Random.Range(0, shootCooldown / 2f);
        SpawnSmallBullets();
        float orbitingRadius = this.orbitingRadius;
        while(orbitingRadius > 0)
        {
            orbitingRadius -= 1.5f * Time.deltaTime;
            for (int i = 0; i < components.Count; i++)
            {
                if (components[i])
                {
                    components[i].transform.position += components[i].transform.up * 1.5f * Time.deltaTime;
                }
                else
                {
                    components.RemoveAt(i);
                    i--;
                }
            }
            yield return null;
        }
        yield return new WaitForSeconds(timeToShoot);

        for(int i = 0; i < numberOfOribtingBullets; i++)
        {
            int k = Random.Range(0, components.Count);
            if (components[k])
            {
                PlaySound(orbitingBulletDeploySound);
                TargetedBullet bullet = components[k];
                Instantiate(bulletStartExplosion, bullet.transform.position, bullet.transform.rotation);
                bullet.transform.SetParent(null);
                Vector3 lookDir = player.transform.position - bullet.transform.position;
                float angle = Mathf.Atan2(lookDir.y, lookDir.x) * Mathf.Rad2Deg;
                bullet.transform.rotation = Quaternion.Euler(0, 0, angle);
                bullet.speed = bulletSpeed;
                if (player.shouldBeAttacked)
                {
                    bullet.target = player.transform;
                    bullet.StopTargeting(1.25f);
                }
                bullet.ignoreCollisionLayer = 0;
                bullet.canPierce = false;
            }
            components.RemoveAt(k);
            yield return new WaitForSeconds(orbitingBulletsFireRate);
        }
        components.Clear();
        aliveOrbitingBullets = false;
    }
    public void SpawnSmallBullets()
    {
        Vector3 deviation = Vector3.forward * (180f / numberOfOribtingBullets - 180);
        for (int i = 0; i < numberOfOribtingBullets; i++)
        {
            TargetedBullet bullet = Instantiate(orbitingBulletPrefab, transform.position, Quaternion.Euler(transform.rotation.eulerAngles + deviation + Vector3.forward * 90), holder);
            bullet.transform.localPosition += bullet.transform.up * .01f;
            components.Add(bullet);
            deviation += Vector3.forward * 360f / numberOfOribtingBullets;
        }
    }
    IEnumerator TripleDash()
    {
        isAttacking = true;
        shouldShoot = false;
        canMove = false;
        shootTimer = 100f;
        RaycastHit2D hit;
        Vector2 dashPos;
        for(int i = 0; i < 3; i++)
        {         
            lightning.SetActive(true);
            yield return new WaitForSeconds(chargeTime);
            canLook = false;
            hit = Physics2D.Raycast(transform.position, -transform.up, dashRange, roomLayermask | obstacleLayermask);
            if (hit.collider == null)
                dashPos = transform.position - transform.up * dashRange;
            else
                dashPos = transform.position + ((Vector3)hit.point - transform.position) * .8f;
            float elapsed = 0;
            isDashing = true;
            thruster.SetActive(true);
            PlaySound(dashSound);
            while (elapsed < .6f && ((Vector2)transform.position - dashPos).sqrMagnitude > .05f)
            {
                transform.position = Vector3.SmoothDamp(transform.position, dashPos, ref velSpeed, dashSpeed);
                elapsed += Time.deltaTime;
                yield return null;
            }
            thruster.SetActive(false); ;
            isDashing = false;
            canLook = true;
        }
        lightning.SetActive(false);
        yield return new WaitForSeconds(.25f);
        canMove = true;
        isAttacking = false;
        shootTimer = Random.Range(0, shootCooldown * 1.25f);
        ChooseBehaviour(1);
    }

    IEnumerator Boomerangs()
    {
        isAttacking = true;
        shouldShoot = false;
        canMove = false;
        canLook = false;

        for(int i = 0; i < numberOfBoomerangs; i++)
        {
            Vector3 targetPos = transform.position + (transform.right * (i % 2 == 0 ? Random.Range(0f, 1f) : Random.Range(-1f, 0f)) + transform.up * Random.Range(-1f, 1f)).normalized * Random.Range(boomerangRange * .8f, boomerangRange * 1.2f);
            PlaySound(boomerangSound);
            BoomerangBullet bullet = Instantiate(boomerangPrefab, boomerangFirePoints[i % 2].position, boomerangFirePoints[i % 2].rotation);
            bullet.targetPos = targetPos;
            bullet.whoShotIt = transform;
            bullet.firePoint = transform;

            yield return new WaitForSeconds(boomerangFireRate);
        }

        yield return new WaitForSeconds(.25f);
        canLook = true;
        canMove = true;
        shootTimer = Random.Range(0, shootCooldown * 1.25f);
        ChooseBehaviour(1);
        isAttacking = false;
    }

    IEnumerator BulletsShield()
    {
        isAttacking = true;
        shouldShoot = false;
        shootTimer = 10f;
        canMove = false;
        canBePushedBack = false;
        canLook = false;

        float elapsed = 0;
        float angle = transform.rotation.eulerAngles.z;
        bullets.Clear();
        float initialSpeed = curveBulletPrefab.speed;
        float shootCounter = 0;
        float shootCooldown = rotationTime / numberOfBullets;
        curveBulletPrefab.direction = 1;
        WaitForFixedUpdate wait = new WaitForFixedUpdate();
        PlaySound(bulletsShieldRotateSound);
        while (elapsed < rotationTime)
        {
            angle += 360 / rotationTime * Time.fixedDeltaTime;
            transform.rotation = Quaternion.Euler(0, 0, angle);
            if (shootCounter <= 0)
            {
                bullets.Add(Instantiate(curveBulletPrefab, mainFirePoint.position - transform.up * .2f, mainFirePoint.rotation));
                bullets[bullets.Count - 1].speed = 0;
                bullets[bullets.Count - 1].whoShotIt = transform;
                bullets[bullets.Count - 1].GetComponent<CurveBullet>().startRotating = false;
                shootCounter = shootCooldown;
            }
            shootCounter -= Time.fixedDeltaTime;
            elapsed += Time.fixedDeltaTime;
            yield return wait;
        }
        yield return new WaitForSeconds(.25f);

        PlaySound(bulletsShieldReleaseSound);
        for (int i = 0; i < bullets.Count; i++)
        {
            if (bullets[i])
            {
                bullets[i].speed = initialSpeed;
                //bullets[i].gameObject.layer = 11;
                bullets[i].GetComponent<CurveBullet>().startRotating = true;
            }
        }
        angle = transform.rotation.eulerAngles.z;
        bullets.Clear();
        yield return new WaitForSeconds(.15f);

        elapsed = 0;

        initialSpeed = curveBulletPrefab.speed;
        shootCounter = 0;
        shootCooldown = rotationTime / numberOfBullets;
        curveBulletPrefab.direction = -1;
        PlaySound(bulletsShieldRotateSound);

        while (elapsed < rotationTime)
        {
            angle += 360 / rotationTime * Time.fixedDeltaTime;
            transform.rotation = Quaternion.Euler(0, 0, angle);
            if (shootCounter <= 0)
            {
                bullets.Add(Instantiate(curveBulletPrefab, mainFirePoint.position - transform.up * .2f, mainFirePoint.rotation));
                bullets[bullets.Count - 1].speed = 0;
                bullets[bullets.Count - 1].whoShotIt = transform;
                bullets[bullets.Count - 1].GetComponent<CurveBullet>().startRotating = false;
                //bullets[bullets.Count - 1].gameObject.layer = 19;
                shootCounter = shootCooldown;
            }
            shootCounter -= Time.fixedDeltaTime;
            elapsed += Time.fixedDeltaTime;
            yield return wait;
        }
        yield return new WaitForSeconds(.25f);
        PlaySound(bulletsShieldReleaseSound);

        for (int i = 0; i < bullets.Count; i++)
        {
            if (bullets[i])
            {
                bullets[i].speed = initialSpeed;
                //bullets[i].gameObject.layer = 11;
                bullets[i].GetComponent<CurveBullet>().startRotating = true;
            }
        }
        bullets.Clear();
        yield return new WaitForSeconds(.15f);
        canMove = true;
        canLook = true;
        shootTimer = Random.Range(0, shootCooldown * 1.25f);
        ChooseBehaviour(1);
        isAttacking = false;
    }

    IEnumerator SpawnEnemies()
    {
       //while((transform.position - activeRoom.transform.position).sqrMagnitude > .05f)
        while(!reachedEndOfPath)
        {
            canLook = false;
            Vector3 lookDir = (activeRoom.transform.position - transform.position);
            float angle = Mathf.Atan2(lookDir.y, lookDir.x) * Mathf.Rad2Deg + 90;
            transform.rotation = Quaternion.Lerp(transform.rotation, (Quaternion.AngleAxis(angle, Vector3.forward)), turningspeed * Time.deltaTime);

            yield return null;
        }

        canMove = false;

        while(Mathf.Abs(transform.rotation.eulerAngles.z) > 0.1f)
        {
            transform.rotation = Quaternion.Lerp(transform.rotation, (Quaternion.AngleAxis(0, Vector3.forward)), turningspeed * Time.deltaTime);
            yield return null;
        }
        //gameObject.tag = "Untagged";
        PlaySound(shieldSound);
        activeShield = Instantiate(shieldPrefab, transform.position, transform.rotation);
        canTakeDamage = false;
        selectedEnemies[0] = possibleEnemies[Random.Range(0, possibleEnemies.Length)];
        selectedEnemies[1] = possibleEnemies[Random.Range(0, possibleEnemies.Length)];
        float deviation = 0;
        for(int i = 0; i < numberOfEnemiesSpawned; i++)
        {
            enemySpawnDirection.rotation = Quaternion.Euler(0, 0, transform.rotation.eulerAngles.z + deviation);
            Vector3 spawnPos = transform.position + enemySpawnDirection.up * 1.75f;
            deviation += 360 / numberOfEnemiesSpawned;
            StartCoroutine(SpawnEnemy(spawnPos));
        }
    }

    IEnumerator SpawnEnemy(Vector3 spawnPos)
    {
        GameObject spawn = Instantiate(selectedEnemies[Random.Range(0, 2)].spawnEffect, spawnPos, Quaternion.identity);
        spawn.transform.localScale = new Vector3(selectedEnemies[Random.Range(0, 2)].transform.localScale.x * 2, selectedEnemies[Random.Range(0, 2)].transform.localScale.y, 1);
        
        yield return new WaitForSeconds(1f);
        PlaySound(SoundManager.instance.enemyTeleport);
        yield return new WaitForSeconds(0.5f);

        aliveSmallEnemies.Add(Instantiate(selectedEnemies[Random.Range(0, 2)], spawnPos, Quaternion.Euler(0, 0, Random.Range(0, 360))));
        lines.Add(Instantiate(linePrefab, transform.position, transform.rotation));
        hasSpawnedEnemies = true;
        healthBar.GetComponentsInChildren<Image>()[2].color = Color.green;
    }

    public void CheckForSpawnedEnemies()
    {
        if (hasSpawnedEnemies)
        {

            for (int i = 0; i < aliveSmallEnemies.Count; i++)
            {
                if (aliveSmallEnemies[i] == null)
                {
                    aliveSmallEnemies.RemoveAt(i);
                    Destroy(lines[i].gameObject);
                    lines.RemoveAt(i);
                    i--;
                }
                else
                {
                    lines[i].SetPosition(1, transform.position);
                    lines[i].SetPosition(0, aliveSmallEnemies[i].transform.position);
                }

            }
            if (aliveSmallEnemies.Count == 0)
            {
                Destroy(activeShield.gameObject);
                canTakeDamage = true;
                canMove = true;
                canLook = true;
                shootTimer = Random.Range(0, shootCooldown * 1.25f);
                hasSpawnedEnemies = false;
                healthBar.GetComponentsInChildren<Image>()[2].color = Color.red;
                spawnTimer = Random.Range(spawnCooldown, spawnCooldown * 1.5f);
                gameObject.tag = "Enemy";
            }
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (isDashing)
        {
            if (other.CompareTag("Player"))
            {
                if (PlayerController.instance.invincibility <= 0 && PlayerController.instance.canTakeDamage)
                {
                    Vector2 forceDir;
                    if (PlayerController.instance.reducePushBack)
                        forceDir = (other.transform.position - transform.position).normalized * .5f;
                    else
                        forceDir = (other.transform.position - transform.position).normalized;
                    PlayerController.instance.rb.AddForce(forceDir * damagePerDash * 2f, ForceMode2D.Impulse);
                }
                PlayerController.instance.TakeDamage(damagePerDash);
            }
        }
    }


    private void OnDestroy()
    {
        healthBar.gameObject.SetActive(false);
        activeRoom.roomValue = unitValue * 2;
        activeRoom.SpawnChestFunction(true);
        foreach (Enemy enemy in FindObjectsOfType<Enemy>())
            enemy.TakeDamage(enemy.health + 1);
        foreach(Bullet bullet in FindObjectsOfType<Bullet>())
        {
            if (bullet.gameObject.layer == 11)
            {
                bullet.Explode();
                Destroy(bullet.gameObject);
            }
        }
    }
}