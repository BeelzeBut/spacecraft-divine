using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Mine : Bullet
{
    public float speedDecrease;
    public float lifeTime, lifeTimeTimer;
    void Start()
    {
        StartCoroutine(ActivateCollider(Random.Range(0.1f, 0.8f)));
        explosion.GetComponent<Explosion>().damage = damage;
        explosion.GetComponent<Explosion>().hasPushBack = hasPushBack;
        lifeTimeTimer = lifeTime;
        speedDecrease = Random.Range(1.5f, speedDecrease + .5f);
    }

    void Update()
    {

    }

    void FixedUpdate()
    {
        if (speed > 0)
        {
            speed -= speedDecrease * Time.deltaTime;
            if (speed <= 0)
                speed = 0;

            transform.position += transform.right * Time.deltaTime * speed;
            rb.MovePosition(rb.position + (Vector2)transform.right * Time.fixedDeltaTime * speed);
        }
        lifeTimeTimer -= Time.deltaTime;
        if(lifeTimeTimer <= 0)
        {
            Explode();
            Destroy(this.gameObject);
        }
    }

    IEnumerator ActivateCollider(float time)
    {
        GetComponent<Collider2D>().enabled = false;
        yield return new WaitForSeconds(time);
        GetComponent<Collider2D>().enabled = true; 

    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if(other.CompareTag("Enemy"))
        {
            Explode();
            Destroy(this.gameObject);
        }
    }
}
