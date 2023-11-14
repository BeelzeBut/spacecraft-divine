using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MeteorEnemy : Enemy
{
    public float chargeTimeMax;
    private float chargeAmount, speed;

    Vector3 lookDirection;
    public Transform firePoint;
    public Bullet meteoritePrefab;
    Bullet meteorite;
    public bool isOrange;
    public AudioClip chargeSound;
    void Start()
    {
        MaterialSetup();
        player = PlayerController.instance;
        curMoveSpeed = moveSpeed;

        transform.localScale = Vector3.zero;
    }

    void Update()
    {
        if(player.isAlive)
        {
            if (player.isAlive)
            {
                if (player.isAlive)
                {
                    EnemyAI();

                    if (shouldShoot)
                    {
                        StartCoroutine(ChargeMeteorite());
                    }
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

    IEnumerator ChargeMeteorite()
    {
        rb.velocity = Vector2.zero;
        canBePushedBack = false;
        shouldShoot = false;
        shootTimer = 10f;
        meteorite = Instantiate(meteoritePrefab, firePoint.position, firePoint.rotation);
        chargeAmount = 0;
        speed = 1;
        canMove = false;
        PlaySound(chargeSound);
        //yield return new WaitForSeconds(chargeTimeMax - .1f);
        float elapsed = 0;
        while (elapsed < chargeTimeMax / 2f - .05f)
        {
            chargeAmount += 2 * Time.deltaTime;
            meteorite.transform.localScale = new Vector3(1, 1, 0) * chargeAmount;
            meteorite.transform.position = firePoint.position;
            meteorite.transform.rotation = firePoint.rotation;
            meteorite.transform.SetParent(transform);
            speed = chargeAmount * 1.25f;

            elapsed += Time.deltaTime;
            yield return null;
        }

        ThrowMeteorite(meteorite, speed);        
    }

    void ThrowMeteorite(Bullet meteorite, float speed)
    {
        StopCoroutine(ChargeMeteorite());
        if (meteorite.transform.localScale.x < 1f)
            meteorite.transform.localScale = Vector3.one;
        if (speed >= 1.5f)
            meteorite.speed = speed;
        else meteorite.speed = 1.5f;
        if (isOrange)
        {
            meteorite.speed *= 1.25f;
        }

        PlaySound(attackSound);
        meteorite.GetComponent<CircleCollider2D>().enabled = true;
        meteorite.damage = meteorite.transform.localScale.x;
        meteorite.transform.SetParent(null);
        meteorite.whoShotIt = this.transform;
        meteorite = null;

        lookDirection = (player.transform.position - transform.position).normalized;
        canBePushedBack = true;
        rb.AddForce(-lookDirection * 50f * speed);

        canMove = true;
        shouldShoot = false;
        shootTimer = Random.Range(shootCooldown * 1 / 2f, shootCooldown * 3 / 2f);
        ChooseBehaviour(1);
    }

    public override IEnumerator StopAttack()
    {
        if(meteorite)
            ThrowMeteorite(meteorite, speed);
        return base.StopAttack();
    }
}


/* 
 * if (rb.velocity != Vector2.zero)
            {
                if (isCharging)
                {
                    ThrowMeteorite(meteorite, speed);
                    shootTimer -= (chargeTimeMax - chargeAmount);
                }

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

            moveDirection = player.transform.position - transform.position;
            moveDirection.Normalize();

            angle = Mathf.Atan2(moveDirection.y, moveDirection.x) * Mathf.Rad2Deg + 90;
            transform.rotation = Quaternion.Euler(0, 0, angle);

            float distance = Vector3.Distance(player.transform.position, transform.position);

            if (distance > shootRangeMax)
            {
                transform.position += new Vector3(moveDirection.x, moveDirection.y, 0) * Time.deltaTime * curMoveSpeed;
            }
            else if (distance < shootRangeMin)
                transform.position -= new Vector3(moveDirection.x, moveDirection.y, 0) * Time.deltaTime * curMoveSpeed;


            if (shootTimer > 0)
            {
                shootTimer -= Time.deltaTime;
            }
            if (shootTimer <= 0)
            {
                RaycastHit2D[] hits = new RaycastHit2D[1];
                Physics2D.RaycastNonAlloc(transform.position, (player.transform.position - transform.position), hits, Mathf.Infinity, layermask);

                if (distance <= shootRangeMax && hits[0].transform.CompareTag("Player"))
                {
                    StartCoroutine(ChargeMeteorite());
                    shootTimer = Random.Range(shootCooldown * 6f / 7f, shootCooldown * 8f / 7f) + chargeTimeMax;
                }
            }

            if (isCharging)
            {
                chargeAmount += Time.deltaTime;
                meteorite.transform.localScale = new Vector3(2, 2, 0) * chargeAmount;
                meteorite.transform.position = firePoint.position;
                meteorite.transform.rotation = firePoint.rotation;
                meteorite.transform.parent = transform;
                speed = chargeAmount;
            }
*/