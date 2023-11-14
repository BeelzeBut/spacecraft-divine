using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Pathfinding;

public class ShockEnemy : Enemy
{
    Vector3 moveDirection;
    public Transform firePoint;
    public ExplosionBullet bulletPrefab;
    void Start()
    {
        //HealthBarSetup();

        player = PlayerController.instance;
        curMoveSpeed = moveSpeed;

        transform.localScale = Vector3.zero;

        //seeker = GetComponent<Seeker>();
        //InvokeRepeating("UpdatePath", 0f, .15f);
    }

    // Update is called once per frame
    void Update()
    {
        if (player.isAlive)
        {
            EnemyAI();

            if (shouldShoot)
            {
                Shock(player.transform.position);
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

    void Shock(Vector3 playerPos)
    {
        shootTimer = Random.Range(shootCooldown * 1 / 2f, shootCooldown * 3 / 2f);
        shouldShoot = false;

        ExplosionBullet shockBullet = Instantiate(bulletPrefab, firePoint.position, firePoint.rotation);
        shockBullet.explodeAtLocation = true;
        shockBullet.locationToExplode = playerPos;
        shockBullet.whoShotIt = this.transform;

        moveDirection = (player.transform.position - transform.position).normalized;
        rb.AddForce(-moveDirection * 100f);
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

        moveDirection = player.transform.position - transform.position;
        moveDirection.Normalize();

        angle = Mathf.Atan2(moveDirection.y, moveDirection.x) * Mathf.Rad2Deg + 90;
        transform.rotation = Quaternion.Euler(0, 0, angle);

        float distance = Vector3.Distance(player.transform.position, transform.position);

        if (distance > shootRangeMax)
        { 
            transform.position += new Vector3(moveDirection.x, moveDirection.y, 0) * Time.deltaTime * curMoveSpeed;
        } else if(distance < shootRangeMin)
            transform.position -= new Vector3(moveDirection.x, moveDirection.y, 0) * Time.deltaTime * curMoveSpeed;


        if (shootTimer > 0)
        {
            shootTimer -= Time.deltaTime;
        }
        if (shootTimer <= 0)
        {
            RaycastHit2D[] hits = new RaycastHit2D[1];
            Physics2D.RaycastNonAlloc(transform.position, (player.transform.position - transform.position), hits, Mathf.Infinity, layermask);
            
            if (distance <= shootRangeMax && distance >= shootRangeMin && hits[0].transform.CompareTag("Player"))
            {
                rb.AddForce(-moveDirection * curMoveSpeed * 35f);
                StartCoroutine(Shock(player.transform.position));          
            }
        }
        */
