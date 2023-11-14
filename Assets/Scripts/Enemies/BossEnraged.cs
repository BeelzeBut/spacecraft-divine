using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class BossEnraged : Enemy
{
    [Header("General")]
    public Sprite enragedSprite;
    public Sprite normalSprite;
    public float maxArmor = 175f;
    private float armor = 175f;
    public float armorRegenCooldown = 7f;
    private float armorTimer;
    public bool isEnraged;
    public bool canChange = true;
    private float armorPerSecond;
    private int lastAttackIndex = -1;
    private int secondLastAttackIndex = -1;
    private float maxHealth;
    private bool aimAheadOfPlayer = false;
    private float healthThreshold = 50;
    public GameObject[] beepingLights;

    [Header("Rotative Shooting")]
    public Bullet bulletPrefab;
    public Transform[] rotativeFirePoints;
    public float rotativeFireRate;
    public float rotationSpeed;
    public float rotationTime = 1.25f;

    [Header("Chains")]
    public Transform chainsMuzzleFlash;
    public Transform[] mainFirePoints;
    public Transform[] chainsFirePoints;
    public Bullet mainBulletPrefab;
    public Bullet chainsBulletPrefab;
    public float shootingTime;
    public float fireRate;
    public float spread;

    [Header("Swing")]
    public Transform swingFirePoint;
    public SlowingBullet swingBulletPrefab;
    public float dashRange = 4f;
    public float swingFireRate;
    private float swingTimer;
    public float chargeTime;
    Vector3 velSpeed;
    public float dashSpeed;
    public GameObject thruster;

    [Header("Electrified Bullets")]
    public ElectricArchBullet electricBullet;
    public Transform electricFirePoint;
    int k = 0;

    public AudioClip electricBulletSound, swingeExplosionSound, swingDashBulletSound, chainsSound, chainsBulletsSound, rotativeShootSound;

    void Start()
    {
        MaterialSetup();
        shootTimer = Random.Range(1, shootCooldown);
        player = PlayerController.instance;
        curMoveSpeed = moveSpeed;

        maxHealth = health;
        
        if (activeRoom)
            activeRoom.sizeMultiplier = .75f;
        armorPerSecond = maxArmor / armorRegenCooldown;
        armor = maxArmor;
        source.loop = true;
    }

    void Update()
    {
        if (player.isAlive)
        {
            EnemyAI();

            if (shouldShoot)
            {
                //Debug.Log(k++);
                shouldShoot = false;
                shootTimer = 10f;
                ChooseAttack();
            }
        }
        healthBar.value = health / maxHealth;
        armorSlider.value = armor / maxArmor;
        if (health / maxHealth * 100f <= healthThreshold && health > 0)
        {
            healthThreshold -= 100;
            StartCoroutine(activeRoom.SpawnWave());
        }
        if (aimAheadOfPlayer)
        {
            AimAheadOfPlayer();
        }
        if(isEnraged)
        {
            armor += armorPerSecond * Time.deltaTime;
            armorTimer -= Time.deltaTime;
            if(armorTimer <= 0 && canChange)
            {
                
                Unenraged();
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
        int k = isEnraged ? Random.Range(1, 4) : Random.Range(0, 3);
        while (k == lastAttackIndex && k == secondLastAttackIndex)
            k = isEnraged ? Random.Range(1, 4) : Random.Range(0, 3);

        switch (k)
        {
            case 0:
                StartCoroutine(Chains());
                break;
            case 1:
                StartCoroutine(RotativeShooting());
                break;
            case 2:
                StartCoroutine(ElectrifiedBullets());
                break;
            case 3:
                StartCoroutine(Swing());
                break;
        }
        secondLastAttackIndex = lastAttackIndex;
        lastAttackIndex = k;
    }

    IEnumerator ElectrifiedBullets()
    {
        shouldShoot = false;
        shootTimer = 10f;
        canLook = false;
        Instantiate(electricBullet, electricFirePoint.position, electricFirePoint.rotation);
        PlaySound(electricBulletSound);

        yield return new WaitForSeconds(.25f);
        shootTimer = Random.Range(0.1f, shootCooldown * 1.25f);
        ChooseBehaviour(1);
        canLook = true;
    }
    IEnumerator Swing()
    {
        shouldShoot = false;
        shootTimer = 10f;
        canChange = false;
        thruster.SetActive(true);
        yield return new WaitForSeconds(chargeTime);
        GetComponentInChildren<CircleCollider2D>().enabled = false;
        canMove = false;
        canLook = false;
        Vector2 dashPos;
        RaycastHit2D hit = Physics2D.Raycast(transform.position, -transform.up, dashRange, roomLayermask);
        if (hit.collider == null)
            dashPos = transform.position - transform.up * dashRange;
        else
            dashPos = transform.position + ((Vector3)hit.point - transform.position) * .8f;
        float elapsed = 0;
        thruster.SetActive(true);
        swingTimer = .01f;
        WaitForFixedUpdate wait = new WaitForFixedUpdate();
        while (elapsed < .8f && ((Vector2)transform.position - dashPos).sqrMagnitude > .05f)
        {
            swingTimer -= Time.fixedDeltaTime;
            if(swingTimer <= 0)
            {
                PlaySound(swingDashBulletSound);
                swingTimer = swingFireRate;
                Instantiate(swingBulletPrefab, swingFirePoint.position, Quaternion.Euler(swingFirePoint.rotation.eulerAngles + Vector3.forward * 75f));
                Instantiate(swingBulletPrefab, swingFirePoint.position, Quaternion.Euler(swingFirePoint.rotation.eulerAngles + Vector3.forward * -75f));
            }
            transform.position = Vector3.SmoothDamp(transform.position, dashPos, ref velSpeed, dashSpeed);
            elapsed += Time.fixedDeltaTime;
            yield return wait;
        }
        Vector3 deviation = new Vector3(0, 0, -360 / 2f);
        PlaySound(swingeExplosionSound);
        for (int i = 0; i < 12; i++)
        {
            Bullet smallBullet = Instantiate(swingBulletPrefab, transform.position, Quaternion.Euler(transform.rotation.eulerAngles + deviation));
            deviation += Vector3.forward * (360 / 12);
            smallBullet.whoShotIt = transform;
        }
        thruster.SetActive(false);
        yield return new WaitForSeconds(.25f);
        GetComponentInChildren<CircleCollider2D>().enabled = true;
        canMove = true;
        canLook = true;
        ChooseBehaviour(1);
        shootTimer = Random.Range(0.1f, shootCooldown * 1.25f);
        canChange = true;
    }

    IEnumerator Chains()
    {
        shouldShoot = false;
        canMove = false;
        shootTimer = 10f;
        canLook = false;

        PlaySound(chainsSound);
        for(int i = 0; i < 2; i++)
        {
            Instantiate(chainsMuzzleFlash, chainsFirePoints[i].position, chainsFirePoints[i].rotation);
            Instantiate(chainsBulletPrefab, chainsFirePoints[i].position, chainsFirePoints[i].rotation);
        }
        float elapsed = 0;
        WaitForFixedUpdate wait = new WaitForFixedUpdate();
        float shotCounter = .05f;
        int k = 0;
        while(elapsed < shootingTime)
        {
            shotCounter -= Time.fixedDeltaTime;
            if(shotCounter <= 0)
            {
                shotCounter = fireRate;
                float spread = this.spread * 7.5f * Random.Range(-1f, 1f);
                PlaySound(chainsBulletsSound);
                Instantiate(muzzleFlash, mainFirePoints[k % 2].position, mainFirePoints[k % 2].rotation);
                Bullet bullet = Instantiate(mainBulletPrefab, mainFirePoints[k % 2].position, Quaternion.Euler(mainFirePoints[k % 2].rotation.eulerAngles + Vector3.forward * spread));
                bullet.whoShotIt = transform;
                k++;
            }

            elapsed += Time.fixedDeltaTime;
            yield return wait;
        }

        yield return new WaitForSeconds(.25f);
        ChooseBehaviour(1);
        canMove = true;
        shootTimer = Random.Range(0, shootCooldown * 1.25f);
        canLook = true;
    }

    IEnumerator RotativeShooting()
    {
        canChange = false;
        shouldShoot = false;
        shootTimer = 10f;
        canMove = false;
        canLook = false;

        float elapsed = 0;
        WaitForFixedUpdate wait = new WaitForFixedUpdate();
        float shotCounter = 0.05f;
        while(elapsed < rotationTime)
        {
            for(int i = 0; i < 2; i++)
            {
                rotativeFirePoints[i].Rotate(Vector3.forward * rotationSpeed * Time.fixedDeltaTime * (i % 2 == 0 ? 1 : -1));
            }
            shotCounter -= Time.fixedDeltaTime;
            if (shotCounter <= 0)
            {
                shotCounter = rotativeFireRate;
                PlaySound(rotativeShootSound);
                for (int j = 0; j < 2; j++)
                {
                    Instantiate(muzzleFlash, rotativeFirePoints[j].position, rotativeFirePoints[j].rotation);
                    Bullet bullet = Instantiate(bulletPrefab, rotativeFirePoints[j].position, rotativeFirePoints[j].rotation);
                    bullet.whoShotIt = transform;
                }
            }
            elapsed += Time.fixedDeltaTime;
            yield return wait;
        }
        rotativeFirePoints[0].localRotation = Quaternion.Euler(0, 0, -180);
        rotativeFirePoints[1].localRotation = Quaternion.Euler(0, 0, 0);
        yield return new WaitForSeconds(.25f);
        canMove = true;
        canLook = true;
        canChange = true;
        shootTimer = Random.Range(0, shootCooldown * 1.25f);
    }

    public override void TakeDamage(float damage)
    {
        if (!isDead)
        {
            if (isEnraged)
                Mathf.Clamp(health -= damage, 0, 1000);
            else
            {
                armor -= damage;
                if(armor <= 0)
                {
                    Enraged();
                    source.Play();
                }
            }
            if (health <= 0)
            {
                isDead = true;
                GetComponent<Collider2D>().enabled = false;
                DataHolder.instance.enemiesKilled++;
                SoundManager.instance.soundSource.PlayOneShot(SoundManager.instance.explosionSounds[0]);
                Instantiate(deathEffect, transform.position, Quaternion.Euler(0, 0, Random.Range(0, 360)));
                if (isBoss)
                {
                    if (blueprint)
                        if (!DataHolder.instance.dataSaved.hasBeenUnlocked[blueprint.dropIndex])
                        {
                            Instantiate(blueprint, transform.position, Quaternion.identity);
                        }
                }
                else
                {
                    for (int i = 0; i < DataHolder.instance.drops.Count; i++)
                    {
                        if ((DataHolder.instance.enemiesKilled == DataHolder.instance.killMilestones[i] ||
                            (DataHolder.instance.enemiesKilled - DataHolder.instance.killMilestones[i]) % 100 == 0)
                            && (DataHolder.instance.drops[i].GetComponent<Blueprint>() &&
                            !DataHolder.instance.dataSaved.hasBeenUnlocked[DataHolder.instance.drops[i].GetComponent<Blueprint>().dropIndex]))
                        {
                            Instantiate(DataHolder.instance.drops[i], transform.position, Quaternion.identity);
                        }
                    }
                }
                GameManager.instance.ShouldDropAbility(transform.position);
                Destroy(gameObject);
                return;
            }
            if (damageFlash != null)
            {
                StopCoroutine(damageFlash);
                shipSprite.material.SetFloat("_FlashAmount", 0f);
                damageFlash = StartCoroutine(DamageFlash(true));
            }
            else
                damageFlash = StartCoroutine(DamageFlash(false));
        }
    }

    private void Enraged()
    {
        StopAllCoroutines();
        StartCoroutine(StopAttack());
        foreach (GameObject light in beepingLights)
            light.SetActive(true);

        armor = 0;
        isEnraged = true;
        shipSprite.sprite = enragedSprite;
        armorTimer = armorRegenCooldown;
        armorSlider.fillRect.GetComponent<Image>().color = Color.grey;

        moveSpeed *= 2;
        curMoveSpeed = moveSpeed;
        moveCooldown /= 2f;
        shootCooldown /= 1.5f;
        shootTimer = Random.Range(0, shootCooldown * 1.25f);
        //Rotative Shoot
        rotativeFireRate /= 1.35f;
        rotationTime /= 1.35f;
    }
    private void Unenraged()
    {
        StartCoroutine(StopAttack());
        foreach (GameObject light in beepingLights)
            light.SetActive(false);
        armor = maxArmor;
        isEnraged = false;
        shipSprite.sprite = normalSprite;
        armorTimer = -1;
        armorSlider.fillRect.GetComponent<Image>().color = new Color(255f / 163f, 0, 255f / 240f);

        moveSpeed /= 2;
        curMoveSpeed = moveSpeed;
        moveCooldown *= 2f;
        shootCooldown *= 1.5f;
        //Rotative Shoot
        rotativeFireRate *= 1.35f;
        rotationTime *= 1.35f;
        source.Stop();
    }

    public override IEnumerator StopAttack()
    {
        rotativeFirePoints[0].localRotation = Quaternion.Euler(0, 0, -180);
        rotativeFirePoints[1].localRotation = Quaternion.Euler(0, 0, 0);
        canLook = true;
        canMove = true;
        shootTimer = Random.Range(.75f, shootCooldown * 1.25f);
        curMoveSpeed = moveSpeed;
        hasRequestedPathToPlayer = false;
        yield return null;
    }
    private void OnDestroy()
    {
        healthBar.gameObject.SetActive(false);
        armorSlider.gameObject.SetActive(false);
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
