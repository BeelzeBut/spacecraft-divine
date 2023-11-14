using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BouncingBallEnemy : Enemy
{
    public bool isEnhanced = false;
    public float chargeTime;
    public float dashDuration = 3f;
    public float dashSpeed;
    public float initialRotationSpeed;
    private float rotationSpeed;
    public float damage;
    private bool shouldRotate;
    public Transform impactExplosion;

    [Header("Enhanced")]
    public Transform firePoint;
    public Bullet bulletPrefab;
    public float fireRate;
    private Coroutine activeShootingCoroutine;
    public AudioClip chargeSound, bounceSound;
    void Start()
    {
        MaterialSetup();
        shootTimer = Random.Range(1, shootCooldown);
        player = PlayerController.instance;
        curMoveSpeed = moveSpeed;
        initialScale = transform.localScale;

    }

    void Update()
    {
        if (player.isAlive)
        {
            EnemyAI();

            if (shouldShoot)
            {
                StartCoroutine(DashAndBounce());
            }
        }

    }

    private void FixedUpdate()
    {
        if (player.isAlive)
        {
            EnemyAIFixedUpdate();
        }
        if (shouldRotate)
        {
            shipSprite.transform.position = transform.position;
            shipSprite.transform.Rotate(Vector3.forward * rotationSpeed * Time.fixedDeltaTime);
        }
    }
    public override IEnumerator StopAttack()
    {
        shipSprite.transform.SetParent(transform);
        shipSprite.transform.localRotation = Quaternion.Euler(Vector3.zero);
        shouldRotate = false;
        if(activeShootingCoroutine != null)
            StopCoroutine(activeShootingCoroutine);
        return base.StopAttack();
    }
    IEnumerator DashAndBounce()
    {
        shouldShoot = false;
        shootTimer = 15f;
        canMove = false;
        canLook = false;
        canBePushedBack = false;
        rotationSpeed = initialRotationSpeed;
        //GetComponentsInChildren<Collider2D>()[1].offset = Vector2.zero;
        float elapsed = 0;

        PlaySound(chargeSound);
        WaitForFixedUpdate wait = new WaitForFixedUpdate();
        while(elapsed < chargeTime)
        {
            shipSprite.transform.Rotate(Vector3.forward * rotationSpeed * elapsed / chargeTime * Time.fixedDeltaTime);
            elapsed += Time.fixedDeltaTime;
            yield return wait;
        }

        shouldRotate = true;
        shipSprite.transform.SetParent(null);
        if(isEnhanced)
            activeShootingCoroutine = StartCoroutine(Shoot());
        elapsed = 0;
        float dashSpeed = this.dashSpeed;
        while(elapsed < dashDuration)
        {
            rb.MovePosition(rb.position + (Vector2)(-transform.up) * dashSpeed * Time.fixedDeltaTime);
            rotationSpeed -= Time.fixedDeltaTime * initialRotationSpeed / dashDuration / 1.5f;
            dashSpeed -= Time.fixedDeltaTime * dashDuration / 1.5f;
            elapsed += Time.fixedDeltaTime;
            yield return wait;
        }
        shouldRotate = false;
        shipSprite.transform.SetParent(transform);
        if (isEnhanced)
        {
            StopCoroutine(activeShootingCoroutine);
            activeShootingCoroutine = null;
        }
        yield return new WaitForSeconds(.15f);
        while (Mathf.Abs(shipSprite.transform.localRotation.eulerAngles.z) > .25f)
        {
            shipSprite.transform.localRotation = Quaternion.Lerp(shipSprite.transform.localRotation, Quaternion.AngleAxis(0, Vector3.forward), 150 * Time.fixedDeltaTime);
            yield return wait;
        }

        yield return new WaitForSeconds(.1f);
        shootTimer = Random.Range(shootCooldown * 3 / 4f, shootCooldown * 5 / 4f);
        canMove = true;
        canLook = true;
        canBePushedBack = true;
    }

    IEnumerator Shoot()
    {
        WaitForSeconds wait = new WaitForSeconds(fireRate);
        while (true)
        {
            PlaySound(attackSound);
            Instantiate(muzzleFlash, firePoint.position, firePoint.rotation);
            Bullet bullet = Instantiate(bulletPrefab, firePoint.position, firePoint.rotation);
            bullet.whoShotIt = transform;
            yield return wait;
        }
    }
    private void OnTriggerEnter2D(Collider2D other)
    {
        if (shouldRotate)
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
                    PlayerController.instance.rb.AddForce(forceDir * damage * 2f, ForceMode2D.Impulse);
                }
                PlayerController.instance.TakeDamage(damage);
            }
            else
            if (other.gameObject.layer == 16 || other.gameObject.layer == 0)
            {
                Bounce();
            }
            RaycastHit2D hit = Physics2D.CircleCast(transform.position, GetComponent<CircleCollider2D>().radius + .05f, -transform.up, 2f, 1 << other.gameObject.layer);
            Instantiate(impactExplosion, hit.point, Quaternion.identity);
        }
    }

    void Bounce()
    {
        PlaySound(bounceSound);
        Vector2 pos = transform.position + transform.up * .1f;
        RaycastHit2D hit = Physics2D.Raycast(pos, -transform.up, 10f, roomLayermask | obstacleLayermask);
        Vector2 dir = -transform.up;
        transform.rotation = Quaternion.Euler(0, 0, transform.rotation.eulerAngles.z - ((-90 + Vector2.SignedAngle(hit.normal, dir)) * 2));
    }

    private void OnDestroy()
    {
        if (shipSprite.transform.parent == null)
            Destroy(shipSprite.gameObject);
    }
}
