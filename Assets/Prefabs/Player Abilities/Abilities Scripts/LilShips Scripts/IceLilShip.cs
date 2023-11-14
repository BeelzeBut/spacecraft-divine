using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class IceLilShip : LilShip
{
    bool switchPosition = true;
    Vector3 localPos, movePos;
    private float moveTimer;
    private float shootTimer;
    public Bullet bulletPrefab;
    public List<Transform> firePoints = new List<Transform>();
    public Animator anim;
    GameObject targetEnemy;
    float invincibility = 0;

    void Start()
    {
        p = PlayerController.instance;
        moveTimer = moveCooldown;
        shootTimer = shootCooldown / 2f;
        Move();
        transform.localScale = Vector3.zero;
    }

    void Update()
    {
        if (transform.localScale.x < 1)
            transform.localScale += Vector3.one * 5 * Time.deltaTime;
        moveTimer -= Time.deltaTime;
        if(moveTimer <= 0)
        {
            Move();
            moveTimer = Random.Range(moveCooldown * 2f / 3f, moveCooldown * 4f / 3f);
        }

        shootTimer -= Time.deltaTime;
        targetEnemy = p.targetEnemy;
        if(shootTimer <= 0 && targetEnemy)
        {
            StartCoroutine(Shoot());
            shootTimer = Random.Range(shootCooldown * 2f / 3f, shootCooldown * 4f / 3f);
        }
        if (switchPosition )
        {
            transform.position = Vector3.MoveTowards(transform.position, p.transform.position + movePos,  5 * Time.deltaTime);
            if(transform.position == p.transform.position + movePos)
            {
                switchPosition = false;
                localPos = movePos;
            }
        }
    }

    private void LateUpdate()
    {
        if (!switchPosition)
            transform.position = p.transform.position + movePos;
        if(targetEnemy)
        {
            Vector3 targetEnemyPos = targetEnemy.transform.position;
            float angle = Mathf.Atan2(targetEnemyPos.y - transform.position.y, targetEnemyPos.x - transform.position.x) * Mathf.Rad2Deg - 90;
            transform.rotation = Quaternion.Euler(0, 0, angle);
        }
        else
        {
            transform.rotation = p.transform.rotation;
        }
    }
    void Move()
    {
        movePos.x = Random.Range(-1, 2) * .35f;
        movePos.y = Random.Range(-1, 2) * .35f;
        while (movePos.x == 0)  
            movePos.x = Random.Range(-1, 2) * .35f;
        while (movePos.y == 0)
            movePos.y = Random.Range(-1, 2) * .35f;
        switchPosition = true;
    }

    IEnumerator Shoot()
    {
        yield return new WaitForSeconds(.1f);
        anim.SetTrigger("Shoot");

        foreach (Transform firePoint in firePoints)
        {
            Bullet bullet = Instantiate(bulletPrefab, firePoint.position, firePoint.rotation);
            bullet.damage = damage;
        }
        yield return new WaitForSeconds(.35f);

        float elapsed = 0;
        while (elapsed < .35f)
        {
            transform.rotation = Quaternion.Euler(Vector3.Lerp(transform.rotation.eulerAngles, transform.rotation.eulerAngles + Vector3.forward * Random.Range(-30f, 30f), elapsed));
            elapsed += Time.deltaTime;
            yield return null;
        }
    }
   
}
