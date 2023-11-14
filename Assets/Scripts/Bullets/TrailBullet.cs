using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TrailBullet : Bullet
{
    public float spawnCooldown;
    [SerializeField]
    float spawnTimer;
    public int numberOfSmallBullets;
    public Bullet smallBulletPrefab;
    public float smallBulletDamage = 0;
    void Start()
    {
        if(spawnTimer == 0)
            spawnTimer = spawnCooldown;
    }

    // Update is called once per frame
    void FixedUpdate()
    {
        //transform.position += transform.right * Time.deltaTime * speed;
        rb.MovePosition(rb.position + (Vector2)transform.right * speed * Time.fixedDeltaTime);
        spawnTimer -= Time.fixedDeltaTime;
        if (spawnTimer <= 0)
        {
            SpawnSmallBullets();
            spawnTimer = spawnCooldown;
        }
    }

    void SpawnSmallBullets()
    {
        Vector3 deviation = Vector3.forward * (180f / numberOfSmallBullets - 180);
        for(int i = 0; i < numberOfSmallBullets; i++)
        {
            Bullet bullet = Instantiate(smallBulletPrefab, transform.position, Quaternion.Euler(transform.rotation.eulerAngles + deviation));
            bullet.gameObject.layer = gameObject.layer;
            if (smallBulletDamage != 0)
                bullet.damage = smallBulletDamage;
            deviation += Vector3.forward * 360f / numberOfSmallBullets;
        }
    }

    /*void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            if (PlayerController.instance.invincibility <= 0)
                DamagePopup.Create(other.transform.position, (int)damage, false, other.gameObject);
            PlayerController.instance.TakeDamage(damage);
        }
        Explode();
        Destroy(gameObject);
    }*/
}
