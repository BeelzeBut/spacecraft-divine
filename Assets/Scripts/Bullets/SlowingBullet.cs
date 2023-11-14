using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SlowingBullet : Bullet
{
    public float decreaseRateMin, decreaseRateMax, decreaseRate;
    public float lifeTime;
    public float slowAfter = 0;
    void Start()
    {
        decreaseRate = Random.Range(decreaseRateMin, decreaseRateMax);
    }

    void FixedUpdate()
    {
        rb.MovePosition(rb.position + (Vector2)transform.right * speed * Time.fixedDeltaTime);
        if(slowAfter <= 0)
            speed -= speed * decreaseRate * Time.fixedDeltaTime;
        else
        {
            slowAfter -= Time.fixedDeltaTime;
        }

        lifeTime -= Time.fixedDeltaTime;
        if (lifeTime <= 0)
        {
            Explode();
            Destroy(gameObject);
        }
    }
}
