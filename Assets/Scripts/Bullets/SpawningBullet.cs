using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SpawningBullet : Bullet
{
    public Bullet smallBulletPrefab;
    public bool spawnAtPosition, spawnAfterTime;
    public Vector3 targetPos;
    public float startSpawningTimer;
    private bool startSpawningBullets;
    public float lifeTime;
    public float spawnRate = .375f;
    public int numberOfSmallBullets = 6;
    public bool randomizeDirection;
    public AudioClip waveSound, dissapearSound;

    void FixedUpdate()
    {
        if (!startSpawningBullets)
        {
            rb.MovePosition(rb.position + (Vector2)transform.right * speed * Time.fixedDeltaTime);

            if (spawnAtPosition && (targetPos - transform.position).sqrMagnitude <= .05f)
            {
                rb.velocity = Vector2.zero;
                startSpawningBullets = true;
                StartCoroutine(SpawnBullets());
            }

            if (spawnAfterTime)
            {
                startSpawningTimer -= Time.fixedDeltaTime;
                if (startSpawningTimer <= 0)
                {
                    rb.velocity = Vector2.zero;
                    startSpawningBullets = true;
                    StartCoroutine(SpawnBullets());
                }
            }
        }
        else
        {
            lifeTime -= Time.fixedDeltaTime;
            if(lifeTime <= 0)
            {
                Explode();
               // if (dissapearSound)
                    //SoundManager.instance.soundSource.PlayOneShot(dissapearSound);
                Destroy(gameObject);
            }
        }
    }
    IEnumerator SpawnBullets()
    {
        if (waveSound)
            SoundManager.instance.soundSource.PlayOneShot(waveSound);
        Vector3 deviation = Vector3.forward * (180f / numberOfSmallBullets - 180);
        if (randomizeDirection)
            deviation = Vector3.forward * Random.Range(0, 360);
        for (int i = 0; i < numberOfSmallBullets; i++)
        {
            Bullet bullet = Instantiate(smallBulletPrefab, transform.position, Quaternion.Euler(transform.rotation.eulerAngles + deviation));
            bullet.pushBack = damage / 2f;
            bullet.damage = damage;
            deviation += Vector3.forward * 360f / numberOfSmallBullets;
        }
        yield return new WaitForSeconds(spawnRate);
        StartCoroutine(SpawnBullets());
    }

    private void OnCollisionEnter2D(Collision2D other)
    {
        rb.velocity = Vector2.zero;
        startSpawningBullets = true;
        StartCoroutine(SpawnBullets());
    }
}
