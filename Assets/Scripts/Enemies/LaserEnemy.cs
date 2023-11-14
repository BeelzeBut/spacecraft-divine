using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LaserEnemy : Enemy
{
    public Transform firePoint;
    public LineRenderer laserPrefab;
    private LineRenderer laser;
    public float fireRate;
    private float shootCounter;
    public float spread;
    public bool isOrange;
    [SerializeField]
    public GameObject explosionEffect;
    [SerializeField]
    public GameObject laserMuzzle;
    public ParticleSystem chargeUpEffectPrefab;
    private ParticleSystem chargeEffect;
    private GameObject muzzle;
    private GameObject explosion;
    public float shootTime;
    public float chargeTime;
    public float damagePerTick;
    float initialTurningSpeed;
    public AudioClip chargeSound;

    void Start()
    {
        MaterialSetup();
        shootTimer = Random.Range(1, shootCooldown);
        player = PlayerController.instance;

        initialTurningSpeed = turningspeed;
        transform.localScale = Vector3.zero;
        curMoveSpeed = moveSpeed;
        laser = Instantiate(laserPrefab, transform.position, transform.rotation, transform);
        laser.enabled = false;
        explosion = Instantiate(explosionEffect, transform.position, transform.rotation, transform);
        explosion.SetActive(false);
        muzzle = Instantiate(laserMuzzle, transform.position, transform.rotation, transform);
        muzzle.SetActive(false);
        chargeEffect = Instantiate(chargeUpEffectPrefab, firePoint.position, Quaternion.identity, firePoint);
        chargeEffect.transform.localPosition = Vector3.zero;
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
        shootTimer = 15f;
        canMove = false;
        canBePushedBack = false;
        float elapsed = 0;
        chargeEffect.Play();
        PlaySound(chargeSound);
        while(elapsed < chargeTime)
        {
            elapsed += Time.deltaTime;
            yield return null;
        }
        if(isOrange)
            turningspeed *= 1.425f;
        elapsed = 0;
        laser.enabled = true;
        explosion.SetActive(true);
        muzzle.SetActive(true);

        source.Play();
        while (elapsed < shootTime)
        {
            RaycastHit2D hit = Physics2D.Raycast(firePoint.position, firePoint.right, 10f, roomLayermask | obstacleLayermask);
            laser.SetPosition(0, firePoint.position);
            if (hit.collider)
                laser.SetPosition(1, hit.point);
            else
                laser.SetPosition(1, firePoint.position + firePoint.right * 10f);
            explosion.transform.position = laser.GetPosition(1);
            muzzle.transform.position = firePoint.position;
            if (shootCounter <= 0)
            {
                RaycastHit2D[] hitPlayer = Physics2D.RaycastAll(firePoint.position, firePoint.right, 10f, LayerMask.GetMask("Player") | roomLayermask | obstacleLayermask);
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
                shootCounter = fireRate;
            } 
            shootCounter -= Time.deltaTime;

            elapsed += Time.deltaTime;
            yield return null;
        }
        chargeEffect.Stop();
        source.Stop();
        laser.enabled = false;
        explosion.SetActive(false);
        muzzle.SetActive(false);
        canBePushedBack = true;
        rb.AddForce(transform.up * 3f, ForceMode2D.Impulse);
        turningspeed = initialTurningSpeed;
        yield return new WaitForSeconds(.5f);
        shootTimer = Random.Range(shootCooldown * 2 / 3f, shootCooldown * 4 / 3f);
        canMove = true;
    }

    public override IEnumerator StopAttack()
    {
        chargeEffect.Stop();
        turningspeed = initialTurningSpeed;
        laser.enabled = false;
        explosion.SetActive(false);
        muzzle.SetActive(false);
        canBePushedBack = true;
        shootTimer = Random.Range(shootCooldown * 2 / 3f, shootCooldown * 4 / 3f);
        canMove = true;
        hasRequestedPathToPlayer = false;
        source.Stop();
        yield return null;
    }
}
