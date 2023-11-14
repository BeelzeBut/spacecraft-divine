using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class VortexEnemy : Enemy
{
    public float vortexDuration;

    Vector3 direction;
    private float angle;

    float vortexShootTimer;

    public Transform firePoint;
    public Bullet bulletPrefab;
    public bool isEnhanced = false;
    public float rotationTime = .75f;
    public int numberOfBullets = 10;
    private List<Bullet> bullets = new List<Bullet>();
    public AudioClip chargeSound;

    void Start()
    {
        MaterialSetup();

        player = PlayerController.instance;
        curMoveSpeed = moveSpeed;

        transform.localScale = Vector3.zero;
        curMoveSpeed = moveSpeed;
    }

    void Update()
    {
        if (player.isAlive)
        {
            if (player.isAlive)
            {
                EnemyAI();

                if (shouldShoot)
                {
                    if (!isEnhanced)
                        StartCoroutine(Vortex());
                    else
                        StartCoroutine(BulletsExplosion());
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

    IEnumerator BulletsExplosion()
    {
        shouldShoot = false;
        shootTimer = 10f;
        canMove = false;
        canBePushedBack = false;
        canLook = false;

        float elapsed = 0;
        float angle = transform.rotation.eulerAngles.z;
        bullets.Clear();
        float initialSpeed = bulletPrefab.speed;
        float shootCounter = 0;
        float shootCooldown = rotationTime / numberOfBullets;
        PlaySound(chargeSound);
        WaitForFixedUpdate wait = new WaitForFixedUpdate();
        while(elapsed < rotationTime)
        {
            angle += 360 / rotationTime * Time.fixedDeltaTime;
            transform.rotation = Quaternion.Euler(0, 0, angle);
            if(shootCounter <= 0)
            {
                bullets.Add(Instantiate(bulletPrefab, firePoint.position - transform.up * .2f, firePoint.rotation));
                bullets[bullets.Count - 1].speed = 0;
                bullets[bullets.Count - 1].whoShotIt = transform;
                shootCounter = shootCooldown;
            }
            shootCounter -= Time.fixedDeltaTime;
            elapsed += Time.fixedDeltaTime;
            yield return wait;
        }
        yield return new WaitForSeconds(.25f);

        for (int i = 0; i < bullets.Count; i++)
        {
            if (bullets[i])
            {
                bullets[i].speed = initialSpeed;
            }
        }
        PlaySound(attackSound);
        bullets.Clear();
        yield return new WaitForSeconds(.2f);
        canMove = true;
        canBePushedBack = true;
        canLook = true;
        shootTimer = Random.Range(this.shootCooldown * 2 / 3f, this.shootCooldown * 4 / 3f) ;
    }

    public override IEnumerator StopAttack()
    {
        if(isEnhanced)
            for (int i = 0; i < bullets.Count; i++)
            {
                if (bullets[i])
                {
                    bullets[i].speed = bulletPrefab.speed;
                }
            }
        shootTimer = Random.Range(shootCooldown * 2 / 3f, shootCooldown * 4 / 3f) + vortexDuration;

        yield return base.StopAttack();
    }

    IEnumerator Vortex()
    {
        canMove = false;
        canLook = false;

        shouldShoot = false;
        shootTimer = Random.Range(shootCooldown * 2 / 3f, shootCooldown * 4 / 3f) + vortexDuration;

        canBePushedBack = false;
        vortexShootTimer = Random.Range(.075f, .1f);
        curMoveSpeed = 0;

        float elapsed = 0;
        WaitForFixedUpdate wait = new WaitForFixedUpdate();
        while (elapsed < vortexDuration)
        {
            transform.Rotate(0, 0, Random.Range(700, 900) * Time.fixedDeltaTime);
            vortexShootTimer -= Time.fixedDeltaTime;
            if (vortexShootTimer <= 0)
            {
                source.PlayOneShot(attackSound, 0.6f);
                Instantiate(muzzleFlash, firePoint.transform.position, firePoint.transform.rotation);
                Bullet bullet = Instantiate(bulletPrefab, firePoint.position, firePoint.transform.rotation);
                bullet.whoShotIt = this.transform;
                vortexShootTimer = Random.Range(.075f, .1f);
            }
            elapsed += Time.fixedDeltaTime;
            yield return wait;
        }

        curMoveSpeed = moveSpeed;
        canBePushedBack = true;
        canMove = true;
        canLook = true;
        ChooseBehaviour(1);
    }

    private void OnDestroy()
    {
        if(isEnhanced)
            foreach (Bullet bullet in bullets)
                if(bullet != null)
                    Destroy(bullet.gameObject);
    }
}
/*
 * if (rb.velocity != Vector2.zero)
            {
                curMoveSpeed = 0;
                Vector2 rbvel = rb.velocity;
                rb.velocity -= rbvel * 10f * Time.deltaTime;
                if (Vector2.Distance(rb.velocity, Vector2.zero) <= .5f)
                {
                    rb.velocity = Vector2.zero;
                    curMoveSpeed = moveSpeed;
                }
            }
            if (transform.localScale != Vector3.one)
                transform.localScale = Vector3.Lerp(transform.localScale, Vector3.one, 6 * Time.deltaTime);

            if (vortexTimer > 0)
            {
                vortexTimer -= Time.deltaTime;
            }
            if (vortexTimer <= 0)
            {
                vortexDuration = Random.Range(3, 4.5f);
                StartCoroutine(Vortex());
            }

            if (!isVortex)
            {
                moveDirection = player.transform.position - transform.position;
                moveDirection.Normalize();

                angle = Mathf.Atan2(moveDirection.y, moveDirection.x) * Mathf.Rad2Deg + 90;
                transform.rotation = Quaternion.Euler(0, 0, angle);

                if (Vector3.Distance(transform.position, player.transform.position) > .5f)
                {
                    transform.position += new Vector3(moveDirection.x, moveDirection.y, 0) * Time.deltaTime * curMoveSpeed;
                }
            }
            else
            {
                transform.Rotate(0, 0, Random.Range(500, 700) * Time.deltaTime);
                shootTimer -= Time.deltaTime;
                if (shootTimer <= 0)
                {
                    Instantiate(bulletPrefab, firePoint.position, firePoint.transform.rotation);
                    shootTimer = Random.Range(.075f, shootCooldown);
                }

            }*/
