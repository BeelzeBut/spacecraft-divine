using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SpawnerEnemy : Enemy
{
    public Transform firePoint;
    public Bullet bulletPrefab;
    public Transform shieldPrefab;
    Transform activeShield;
    public float fireRate;
    public float spread;

    [Header("Spawning")]
    public Enemy smallEnemyPrefab;
    public LineRenderer line;
    public float spawnCooldown;
    private float spawnTimer;
    public List<Transform> spawnPoints = new List<Transform>();
    List<Enemy> aliveSmallEnemies = new List<Enemy>();
    List<LineRenderer> lines = new List<LineRenderer>();

    void Start()
    {
        MaterialSetup();
        player = PlayerController.instance;
        initialScale = transform.localScale;
        transform.localScale = Vector3.zero;
        curMoveSpeed = moveSpeed;
        spawnTimer = Random.Range(spawnCooldown / 5f, spawnCooldown / 2f);
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

            if (player.shouldBeAttacked)
            {
                spawnTimer -= Time.deltaTime;
                if (spawnTimer <= 0 && aliveSmallEnemies.Count < 6)
                {
                    StartCoroutine(Spawn());
                    spawnTimer = Random.Range(spawnCooldown * 2 / 3f, spawnCooldown * 4 / 3f);
                }
            }
            if (activeShield)
            {
                activeShield.transform.position = transform.position + shieldPrefab.transform.position;
            }
        }
    }
    private void FixedUpdate()
    {
        if (player.isAlive)
        {
            EnemyAIFixedUpdate();
            CheckForSpawnedEnemies();
        }
    }

    IEnumerator Spawn()
    {      
        int spawnedNumber = Random.Range(2, 4);
        for(int i = 0; i < spawnedNumber; i++)
        {
            Enemy spawnedEnemy = Instantiate(smallEnemyPrefab, spawnPoints[i % spawnPoints.Count].position, spawnPoints[i % spawnPoints.Count].rotation);
            spawnedEnemy.ChooseBehaviour(1);
            aliveSmallEnemies.Add(spawnedEnemy);
            LineRenderer line = Instantiate(this.line, transform.position, transform.rotation);
            lines.Add(line);
            if (!activeShield)
            {
                activeShield = Instantiate(shieldPrefab, transform.position + shieldPrefab.transform.position, transform.rotation);
                activeShield.transform.localScale = transform.localScale;
                canTakeDamage = false;
            }
            yield return new WaitForSeconds(.5f);
        }
        
    }

    public void CheckForSpawnedEnemies()
    {
        for (int i = 0; i < aliveSmallEnemies.Count; i++)
        {
            if (aliveSmallEnemies[i] == null)
            {
                aliveSmallEnemies.RemoveAt(i);
                Destroy(lines[i].gameObject);
                lines.RemoveAt(i);
                i--;
            }
            else
            {
                lines[i].SetPosition(1, transform.position);
                lines[i].SetPosition(0, aliveSmallEnemies[i].transform.position);
            }

        }

        if (aliveSmallEnemies.Count <= 0 && activeShield)
        {
            Destroy(activeShield.gameObject);
            canTakeDamage = true;
        }
    }

    public IEnumerator Shoot()
    {
        shouldShoot = false;
        shootTimer = Random.Range(shootCooldown * 1 / 2f, shootCooldown * 3 / 2f);

        for (int i = 0; i < Random.Range(5, 7); i++)
        {
            PlaySound(attackSound);
            float bulletSpread = Random.Range(-1f, 1f) * spread * 7.5f;
            Instantiate(muzzleFlash, firePoint.position, Quaternion.Euler(firePoint.rotation.eulerAngles));
            Bullet bullet = Instantiate(bulletPrefab, firePoint.position, Quaternion.Euler(firePoint.rotation.eulerAngles + Vector3.forward * bulletSpread));
            bullet.whoShotIt = this.transform;
            yield return new WaitForSeconds(fireRate);
        }

        shootTimer = Random.Range(shootCooldown * 1 / 2f, shootCooldown * 3 / 2f);
        ChooseBehaviour(Random.Range(0, 2));
    }

    private void OnDestroy()
    {
        foreach(LineRenderer line in lines)
        {
            if(line)
                Destroy(line.gameObject);
        }
        if (activeShield)
            Destroy(activeShield.gameObject);
    }
}
