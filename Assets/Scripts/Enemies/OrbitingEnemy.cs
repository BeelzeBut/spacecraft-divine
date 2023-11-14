using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class OrbitingEnemy : Enemy
{
    public Bullet bulletPrefab;
    public List<Transform> orbitingBullets;
    public int numberOfSmallBullets;
    public Transform rotativeObjectPrefab;
    private Transform rotativeObject;
    public float rotationSpeed;
    public float maxOrbitSize = 2f;

    void Start()
    {
        MaterialSetup();
        shootTimer = Random.Range(1, shootCooldown);
        player = PlayerController.instance;

        transform.localScale = Vector3.zero;
        curMoveSpeed = moveSpeed;
        attackRange = maxOrbitSize;
        rotativeObject = Instantiate(rotativeObjectPrefab, transform.position, Quaternion.identity);
        SpawnSmallBullets();
    }

    void Update()
    {
        if (player.isAlive)
        {
            EnemyAI();

            if (shouldShoot)
            {
                StartCoroutine(OrbitNearPlayer());
            }
        }

        rotativeObject.Rotate(Vector3.forward * rotationSpeed * Time.deltaTime);
        rotativeObject.position = transform.position;
        maxOrbitSize -= Time.deltaTime;
        if(maxOrbitSize > 0)
        {
            for(int i = 0; i < orbitingBullets.Count; i++)
            {
                orbitingBullets[i].transform.position += orbitingBullets[i].right * Time.deltaTime;
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

    IEnumerator OrbitNearPlayer()
    {
        shouldShoot = false;
        
        moveTimer = 2.5f;
        shootTimer = 10f;
        float elapsed = 0;
        while(elapsed < 1.75f)
        {
            if((player.transform.position - transform.position).sqrMagnitude > attackRange * attackRange +.25f)
            {
                rb.MovePosition(rb.position + (Vector2)(player.transform.position - transform.position).normalized * curMoveSpeed * Time.deltaTime);
            }
            else if((player.transform.position - transform.position).sqrMagnitude < attackRange * attackRange - .25f)
            {
                rb.MovePosition(rb.position - (Vector2)(player.transform.position - transform.position).normalized * curMoveSpeed * Time.deltaTime);
            }
            elapsed += Time.deltaTime;
            yield return null;
        }
        
        ChooseBehaviour(1);
        shootTimer = Random.Range(shootCooldown, shootCooldown * 1.75f);
    }

    public void SpawnSmallBullets()
    {
        Vector3 deviation = Vector3.forward * (180f / numberOfSmallBullets - 180);
        for (int i = 0; i < numberOfSmallBullets; i++)
        {
            Bullet bullet = Instantiate(bulletPrefab, transform.position, Quaternion.Euler(transform.rotation.eulerAngles + deviation), rotativeObject);
            bullet.transform.localPosition += bullet.transform.right * .01f;
            orbitingBullets.Add(bullet.transform);
            deviation += Vector3.forward * 360f / numberOfSmallBullets;
        }
    }

    private void OnDestroy()
    {
        if(rotativeObject)
            Destroy(rotativeObject.gameObject);
    }

}
