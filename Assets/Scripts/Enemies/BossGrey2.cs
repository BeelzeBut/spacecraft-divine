using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class BossGrey2 : Enemy
{
    [Header("General")]
    private int lastAttackIndex = -1;
    private int secondLastAttackIndex = -1;
    private float maxHealth;
    private bool aimAheadOfPlayer = false;
    private float healthThreshold = 66;

    [Header("Dash and Shoot")]
    public Transform mainFirePoint;
    public Bullet mainBulletPrefab;
    public float timeBetweenDashes = .25f;
    private Vector3 velSpeed;
    public float dashSpeed = .2f;

    [Header("Popping Balls")]
    public FragBullet poppingBallPrefab;

    [Header("Little Birds")]
    public Transform[] birdsFirePoints;
    public Transform birdsMuzzleFlash;
    public TargetedBullet littleBirdPrefab;
    public float fireRate;
    public int numberOfBulletsShot;
    public float spread;

    [Header("Charged Beam")]
    public BeamBullet beamPrefab;
    public Transform beamMuzzleFlash;
    public float chargeTime;
    public float beamDamage;
    public ParticleSystem chargeUpPrefab;
    private ParticleSystem chargeUp;
    public LineRenderer linePrefab;
    private LineRenderer[] lines = new LineRenderer[2];
    private float initialTurningSpeed;

    public AudioClip beamChargeSound, beamSound, birdSound, poppingBallDeploySound, poppingBallsExplodeSound, dashShootSound;

    private void Start()
    {
        shootTimer = Random.Range(1, shootCooldown);
        player = PlayerController.instance;
        curMoveSpeed = moveSpeed;
        MaterialSetup();
        maxHealth = health;
        
        if (activeRoom)
            activeRoom.sizeMultiplier = .5f;
        lines[0] = Instantiate(linePrefab, transform.position, transform.rotation, transform);
        lines[0].enabled = false;
        lines[1] = Instantiate(linePrefab, transform.position, transform.rotation, transform);
        lines[1].enabled = false;
        chargeUp = Instantiate(chargeUpPrefab, mainFirePoint.position, mainFirePoint.rotation, mainFirePoint);
        chargeUp.Stop();
        initialTurningSpeed = turningspeed;
        source.clip = beamChargeSound;
        source.loop = false;
        source.playOnAwake = false;
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
        hasRequestedPathToPlayer = false;
        chargeUp.Stop();
        lines[0].enabled = false;
        lines[1].enabled = false;
        mainFirePoint.rotation = Quaternion.Euler(0, 0, transform.rotation.eulerAngles.z - 90);
        source.Stop();

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
                StartCoroutine(DashAndShoot());
                break;
            case 1:
                StartCoroutine(PoppingBalls());
                break;
            case 2:
                StartCoroutine(LittleBirds());
                break;
            case 3:
                StartCoroutine(ChargedBeam());
                break;
        }
        secondLastAttackIndex = lastAttackIndex;
        lastAttackIndex = k;
    }

    IEnumerator DashAndShoot()
    {
        shouldShoot = false;
        shootTimer = 10f;
        canMove = false;
        int dashes = 3;
        for(int i = 0; i < dashes; i++)
        {
            rb.velocity = Vector2.zero;
            Vector3 dashPosition = transform.position + new Vector3(Random.Range(-1f, 1f), Random.Range(-1f, 1f)).normalized * Random.Range(1.75f, 2.5f);
            while (Physics2D.Linecast(transform.position, dashPosition, roomLayermask | obstacleLayermask))
            {
                dashPosition = transform.position + new Vector3(Random.Range(-1f, 1f), Random.Range(-1f, 1f)).normalized * Random.Range(1.75f, 2.5f);
            }

            float elapsed = 0;
            Vector2 dir = (dashPosition - transform.position).normalized;
            Vector3 initialPos = transform.position;
            while((transform.position - dashPosition).sqrMagnitude > .2f &&  elapsed < .5f)
            {
                transform.position = Vector3.SmoothDamp(transform.position, dashPosition, ref velSpeed, dashSpeed);
                elapsed += Time.deltaTime;
                yield return null;
            }
            yield return new WaitForSeconds(.075f);

            PlaySound(dashShootSound);
            Instantiate(muzzleFlash, mainFirePoint.position + transform.right * .07f, mainFirePoint.rotation);
            Bullet bullet = Instantiate(mainBulletPrefab, mainFirePoint.position + transform.right * .07f, mainFirePoint.rotation);
            bullet.whoShotIt = transform;
            Instantiate(muzzleFlash, mainFirePoint.position - transform.right * .07f, mainFirePoint.rotation);
            bullet = Instantiate(mainBulletPrefab, mainFirePoint.position - transform.right * .07f, mainFirePoint.rotation);
            bullet.whoShotIt = transform;

            rb.AddForce(transform.up * 3f, ForceMode2D.Impulse);

            yield return new WaitForSeconds(timeBetweenDashes);
        }
        yield return new WaitForSeconds(.5f);
        shootTimer = Random.Range(0, shootCooldown * 1.25f);
        canMove = true;
        ChooseBehaviour(1);
    }

    IEnumerator PoppingBalls()
    {
        shouldShoot = false;

        PlaySound(poppingBallDeploySound);
        Instantiate(muzzleFlash, mainFirePoint.position, mainFirePoint.rotation);
        FragBullet bullet = Instantiate(poppingBallPrefab, mainFirePoint.position, mainFirePoint.rotation);
        bullet.smallBulletPrefab = poppingBallPrefab;
        bullet.numberOfSmallBullets = 3;
        bullet.fragSound = poppingBallDeploySound;

        yield return new WaitForSeconds(.35f);
        shootTimer = Random.Range(0, shootCooldown * 1.25f);
        canMove = true;
        ChooseBehaviour(1);
    }

    IEnumerator LittleBirds()
    {
        shouldShoot = false;
        shootTimer = 10;
        canMove = false;

        for(int i = 0; i < Random.Range(numberOfBulletsShot - 2, numberOfBulletsShot + 3); i++)
        {
            PlaySound(birdSound);
            float spread = Random.Range(-1f, 1f) * this.spread * 7.5f;
            Instantiate(birdsMuzzleFlash, birdsFirePoints[i % 2].position, Quaternion.Euler(0, 0, birdsFirePoints[i % 2].rotation.eulerAngles.z + spread)).localScale *= .75f;
            TargetedBullet bullet = Instantiate(littleBirdPrefab, birdsFirePoints[i % 2].position, Quaternion.Euler(0, 0, birdsFirePoints[i % 2].rotation.eulerAngles.z + spread));
            bullet.locationToExplode = player.shouldBeAttacked ? player.transform.position : transform.position + new Vector3(Random.Range(-3f, 3f), Random.Range(-3f, 3f));
            bullet.StopTargeting(.75f);
            bullet.whoShotIt = transform;
            bullet.turningSpeed *= Random.Range(.8f, 1.2f);
            yield return new WaitForSeconds(fireRate);
        }

        yield return new WaitForSeconds(.75f);
        shootTimer = Random.Range(0, shootCooldown * 1.25f);
        canMove = true;
        ChooseBehaviour(1);
    }

    IEnumerator ChargedBeam()
    {
        shouldShoot = false;
        shootTimer = 10;
        canMove = false;
        chargeUp.Play();

        float elapsed = 0;
        
        lines[0].enabled = true;
        lines[1].enabled = true;
        lines[0].startColor = Color.red;
        lines[1].startColor = Color.red;
        lines[0].endColor = Color.red;
        lines[1].endColor = Color.red;
        RaycastHit2D hit;
        float deviation = 30;
        turningspeed *= 2f;
        source.Play();
        while(elapsed < chargeTime)
        {
           
            lines[0].SetPosition(0, mainFirePoint.position);
            lines[1].SetPosition(0, mainFirePoint.position);

            mainFirePoint.rotation = Quaternion.Euler(0, 0, transform.rotation.eulerAngles.z - 90 + deviation);
            hit = Physics2D.Raycast(mainFirePoint.position, mainFirePoint.right, 1000f, roomLayermask | obstacleLayermask);
            lines[0].SetPosition(1, hit.point);
            mainFirePoint.rotation = Quaternion.Euler(0, 0, transform.rotation.eulerAngles.z - 90 - deviation);
            hit = Physics2D.Raycast(mainFirePoint.position, mainFirePoint.right, 1000f, roomLayermask | obstacleLayermask);
            lines[1].SetPosition(1, hit.point);

            deviation -= 30 * 1/ chargeTime * Time.deltaTime;
            elapsed += Time.deltaTime;
            yield return null;
        }
        deviation = 0;
        mainFirePoint.rotation = Quaternion.Euler(0, 0, transform.rotation.eulerAngles.z - 90 + deviation);
        hit = Physics2D.Raycast(mainFirePoint.position, mainFirePoint.right, 1000f, roomLayermask | obstacleLayermask);
        lines[0].SetPosition(1, hit.point);
        mainFirePoint.rotation = Quaternion.Euler(0, 0, transform.rotation.eulerAngles.z - 90 - deviation);
        hit = Physics2D.Raycast(mainFirePoint.position, mainFirePoint.right, 1000f, roomLayermask | obstacleLayermask);
        lines[1].SetPosition(1, hit.point);
        lines[0].startColor = Color.white;
        lines[1].startColor = Color.white;
        lines[0].endColor = Color.white;
        lines[1].endColor = Color.white;
        canLook = false;
        yield return new WaitForSeconds(.2f);

        chargeUp.Stop();
        lines[0].enabled = false;
        lines[1].enabled = false;

        PlaySound(beamSound);
        Instantiate(beamMuzzleFlash, mainFirePoint.position, mainFirePoint.rotation);
        BeamBullet beam = Instantiate(beamPrefab, mainFirePoint.position, mainFirePoint.rotation);
        beam.charged = 1;
        beam.damage = beamDamage;
        beam.whoShotIt = transform;

        rb.AddForce(5f * transform.up, ForceMode2D.Impulse);

        yield return new WaitForSeconds(.5f);
        turningspeed = initialTurningSpeed;
        canLook = true;
        shootTimer = Random.Range(0, shootCooldown * 1.25f);
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
