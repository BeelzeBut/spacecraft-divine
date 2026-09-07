using System.Collections;
using UnityEngine;

public class CannonEnemy : Enemy
{
    public float bulletDetonationTime = 2f;
    public int numberOfSmallBullets;
    public Transform[] firePoints;
    public FragBullet bulletPrefab;
    Vector2 velspeed;
   
    void Start()
    {
        MaterialSetup();

        player = PlayerController.instance;
        shootTimer = shootCooldown / 2;
        curMoveSpeed = moveSpeed;

        transform.localScale = Vector3.zero;
    }

    // Update is called once per frame
    void Update()
    {
        if (player.isAlive)
        {
            EnemyAI();

            if (shouldShoot)
            {
                StartCoroutine(ShootCannon());
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

    IEnumerator ShootCannon()
    {
        shouldShoot = false;
        shootTimer = Random.Range(shootCooldown * 1 / 2f, shootCooldown * 3 / 2f);
        curMoveSpeed = 0;
        FragBullet cannonBullet;
        //Instantiate(muzzleFlash, firePoints[0].transform.position, Quaternion.Euler(firePoints[0].rotation.eulerAngles + (Vector3.forward * 45f)));
        Instantiate(muzzleFlash, firePoints[0].transform.position, firePoints[0].rotation);
        cannonBullet = Instantiate(bulletPrefab, firePoints[0].position, firePoints[0].rotation);// Quaternion.Euler(firePoints[0].rotation.eulerAngles + (Vector3.forward * 45f)));
        cannonBullet.transform.localScale = Vector3.one * 1.25f;
        cannonBullet.explodeTimer = bulletDetonationTime;
        cannonBullet.numberOfSmallBullets = numberOfSmallBullets;
        cannonBullet.whoShotIt = this.transform;

        //Instantiate(muzzleFlash, firePoints[1].transform.position, Quaternion.Euler(firePoints[1].rotation.eulerAngles + (Vector3.forward * -45f)));
        Instantiate(muzzleFlash, firePoints[1].transform.position, firePoints[1].rotation);
        cannonBullet = Instantiate(bulletPrefab, firePoints[1].position, firePoints[1].rotation);// Quaternion.Euler(firePoints[1].rotation.eulerAngles + (Vector3.forward * -45f)));
        cannonBullet.GetComponent<CurveBullet>().direction = -1;
        cannonBullet.transform.localScale = Vector3.one * 1.25f;
        cannonBullet.explodeTimer = bulletDetonationTime;
        cannonBullet.numberOfSmallBullets = numberOfSmallBullets;
        cannonBullet.whoShotIt = this.transform;

        yield return new WaitForSeconds(.5f);

        curMoveSpeed = moveSpeed;
        ChooseBehaviour(1);
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

            if (Vector3.Distance(transform.position, player.transform.position) > .5f)
            {
                transform.position += new Vector3(moveDirection.x, moveDirection.y, 0) * Time.deltaTime * curMoveSpeed;
            }

            if (shootTimer > 0)
            {
                shootTimer -= Time.deltaTime;
            }
            if (shootTimer <= 0)
            {
                RaycastHit2D[] hits = new RaycastHit2D[1];
                Physics2D.RaycastNonAlloc(transform.position, (player.transform.position - transform.position), hits, Mathf.Infinity, layermask);

                if (Vector3.Distance(player.transform.position, transform.position) <= shootRange && hits[0].transform.CompareTag("Player"))
                {
                    foreach (Transform firePoint in firePoints)
                        StartCoroutine(ShootCannon(firePoint));
                    shootTimer = Random.Range(shootCooldown - shootCooldown / 7f, shootCooldown + shootCooldown / 10);
                }
            }
        
*/